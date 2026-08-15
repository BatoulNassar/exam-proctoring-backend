using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.IO;

namespace ExamProctoring.Infrastructure.Identity
{
    /// Generates the trusted SFace reference embedding from an administrative student photo:
    ///
    ///     decode -> YuNet detect (box + 5 landmarks) -> similarity align to 112x112
    ///            -> SFace feature -> validate 128 finite non-degenerate -> L2 normalise
    ///
    /// Alignment is not optional and is the reason this class is more than a model call. SFace
    /// expects a 112x112 chip warped onto canonical landmark positions. Feeding it a raw
    /// portrait returns a perfectly well-formed 128-float vector that simply does not sit in
    /// the same region of the space as the client's probe - it would never error, it would just
    /// quietly never match anyone.
    ///
    /// ONE BINDING GAP WORTH KNOWING ABOUT: OpenCvSharp 4.13 wraps FaceDetectorYN but not
    /// FaceRecognizerSF. Detection therefore runs through the official wrapper - YuNet's anchor
    /// decoding and NMS are done by OpenCV itself, nothing is reimplemented - while the
    /// alignment and the SFace forward pass are performed here on OpenCV's own DNN module,
    /// using OpenCV's own alignment constants (see <see cref="SFaceAlignment"/>). No ONNX
    /// Runtime and no second OpenCV wrapper are involved.
    ///
    /// Thread safety: neither the detector nor the DNN net is thread-safe, and the detector's
    /// input size is fixed at construction, so the whole sequence runs under one lock. The
    /// import loop is sequential, so this costs nothing in practice.
    public sealed class OpenCvReferenceFaceEmbeddingGenerator : IReferenceFaceEmbeddingGenerator, IDisposable
    {
        /// YuNet emits one row per detection: x, y, w, h, five landmark x/y pairs, then score.
        private const int DetectionRowLength = 15;
        private const int LandmarkOffset = 4;

        /// Numerical sanity bound on the alignment fit, in pixels of the 112x112 chip.
        ///
        /// This is NOT a quality threshold, and it is deliberately loose. A similarity
        /// transform has four degrees of freedom and cannot map five arbitrary points exactly
        /// onto five targets - it is a least-squares fit - so every real face leaves some
        /// residual simply because its proportions differ from the canonical layout. Measured
        /// values for ordinary frontal photographs sit in the low single digits.
        ///
        /// OpenCV's own alignCrop applies no check at all. This one exists only to catch a
        /// transform that has gone numerically wrong, which would otherwise produce a
        /// well-formed but meaningless embedding, and it is set far above any plausible
        /// genuine face so it never rejects one.
        private const double MaxLandmarkResidualPixels = 25.0;

        private readonly FaceRecognitionSettings _settings;
        private readonly ILogger<OpenCvReferenceFaceEmbeddingGenerator> _logger;

        private readonly object _gate = new();

        private Net? _recognizer;
        private FaceDetectorYN? _detector;

        /// Cached so the ASCII-safe staging check runs once, not on every detector rebuild.
        private string? _nativeDetectorPath;

        private Size _detectorInputSize;
        private bool _initialised;
        private bool _unavailable;
        private bool _disposed;

        public OpenCvReferenceFaceEmbeddingGenerator(
            IOptions<FaceRecognitionSettings> settings,
            ILogger<OpenCvReferenceFaceEmbeddingGenerator> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public string DetectorModelPath => ResolvePath(_settings.DetectorModelFileName);

        public string RecognizerModelPath => ResolvePath(_settings.RecognizerModelFileName);

        public bool ModelFilesExist => File.Exists(DetectorModelPath) && File.Exists(RecognizerModelPath);

        public ReferenceFaceEmbeddingResult Generate(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.ImageUndecodable, "the photo file is empty");

            lock (_gate)
            {
                if (_disposed)
                    return ReferenceFaceEmbeddingResult.Failure(
                        ReferenceFaceEmbeddingStatus.GeneratorUnavailable,
                        "the face enrolment service has been shut down");

                if (!EnsureRecognizer())
                    return ReferenceFaceEmbeddingResult.Failure(
                        ReferenceFaceEmbeddingStatus.GeneratorUnavailable,
                        "the face recognition models could not be loaded on the server");

                try
                {
                    return GenerateCore(imageBytes);
                }
                catch (Exception ex)
                {
                    // Never let a native failure escape into the import loop's generic catch,
                    // where it would be reported as an unrelated error. The exception is logged
                    // but not returned - it can carry internal paths.
                    _logger.LogError(ex, "Reference face embedding generation failed unexpectedly.");

                    return ReferenceFaceEmbeddingResult.Failure(
                        ReferenceFaceEmbeddingStatus.InferenceFailed,
                        "face recognition failed while processing this photo");
                }
            }
        }

        private ReferenceFaceEmbeddingResult GenerateCore(byte[] imageBytes)
        {
            using var decoded = Cv2.ImDecode(imageBytes, ImreadModes.Color);

            if (decoded.Empty())
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.ImageUndecodable,
                    "the file is not a readable image");

            using var image = Downscale(decoded);

            var detector = EnsureDetector(new Size(image.Width, image.Height));
            if (detector == null)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.GeneratorUnavailable,
                    "the face detector could not be initialised on the server");

            using var faces = new Mat();
            detector.Detect(image, faces);

            var faceCount = faces.Empty() ? 0 : faces.Rows;

            if (faceCount == 0)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.NoFaceDetected,
                    "no face was detected in the photo");

            // Deliberately a failure, not a selection. Picking the largest or most confident
            // face is how someone standing behind the student becomes that student's permanent
            // reference identity - an error nobody would notice until the wrong person passed.
            if (faceCount > 1)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.MultipleFacesDetected,
                    $"{faceCount} faces were detected; the photo must contain exactly one face");

            if (faces.Cols < DetectionRowLength)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.InferenceFailed,
                    "the face detector returned an unexpected result shape");

            var landmarks = ReadLandmarks(faces);

            if (!SFaceAlignment.TryComputeTransform(landmarks, out var transform))
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.AlignmentFailed,
                    "the detected face landmarks could not be aligned");

            using (transform)
            {
                // The alignment's own correctness check: a right fit lands the five landmarks
                // on the canonical layout. A large residual means the warp is wrong, and a
                // wrong warp produces a vector that is silently unusable rather than obviously
                // broken - so it is refused here instead of stored.
                var residual = SFaceAlignment.MaxLandmarkResidual(transform, landmarks);

                if (residual > MaxLandmarkResidualPixels)
                {
                    _logger.LogWarning(
                        "Face alignment residual {Residual:F2}px exceeded the {Limit:F2}px limit.",
                        residual, MaxLandmarkResidualPixels);

                    return ReferenceFaceEmbeddingResult.Failure(
                        ReferenceFaceEmbeddingStatus.AlignmentFailed,
                        "the detected face could not be aligned accurately enough");
                }

                using var aligned = new Mat();
                Cv2.WarpAffine(image, aligned, transform,
                    new Size(SFaceAlignment.AlignedSize, SFaceAlignment.AlignedSize),
                    InterpolationFlags.Linear);

                if (aligned.Empty())
                    return ReferenceFaceEmbeddingResult.Failure(
                        ReferenceFaceEmbeddingStatus.AlignmentFailed,
                        "the detected face could not be aligned");

                return RunRecognizer(aligned);
            }
        }

        /// SFace takes the aligned BGR chip with no scaling and no mean subtraction - the same
        /// preprocessing OpenCV's own FaceRecognizerSF::feature applies.
        private ReferenceFaceEmbeddingResult RunRecognizer(Mat aligned)
        {
            using var blob = CvDnn.BlobFromImage(
                aligned, 1.0,
                new Size(SFaceAlignment.AlignedSize, SFaceAlignment.AlignedSize),
                new Scalar(0, 0, 0), swapRB: false, crop: false);

            _recognizer!.SetInput(blob);

            using var feature = _recognizer.Forward();

            if (feature.Empty())
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.InferenceFailed,
                    "the face recognizer produced no feature vector");

            var values = ExtractFeature(feature);

            if (values == null || values.Length != FaceEmbedding.Dimensions)
                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.InvalidEmbedding,
                    $"the face recognizer returned {values?.Length ?? 0} values instead of {FaceEmbedding.Dimensions}");

            // The same validation and normalisation the live probe goes through, so reference
            // and probe end up canonical in exactly the same way.
            if (!FaceEmbedding.TryCanonicalise(values, out var canonical, out var error))
            {
                var reason = error switch
                {
                    FaceEmbeddingError.NonFinite => "the generated feature contains a non-finite value",
                    FaceEmbeddingError.Degenerate => "the generated feature is degenerate",
                    _ => $"the generated feature is not {FaceEmbedding.Dimensions} values",
                };

                return ReferenceFaceEmbeddingResult.Failure(
                    ReferenceFaceEmbeddingStatus.InvalidEmbedding, reason);
            }

            return ReferenceFaceEmbeddingResult.Success(FaceEmbedding.ToStorage(canonical));
        }

        private static float[,] ReadLandmarks(Mat faces)
        {
            var landmarks = new float[SFaceAlignment.LandmarkCount, 2];

            for (var i = 0; i < SFaceAlignment.LandmarkCount; i++)
            {
                landmarks[i, 0] = faces.At<float>(0, LandmarkOffset + i * 2);
                landmarks[i, 1] = faces.At<float>(0, LandmarkOffset + i * 2 + 1);
            }

            return landmarks;
        }

        private static double[]? ExtractFeature(Mat feature)
        {
            var count = (int)feature.Total();
            if (count <= 0 || feature.Type() != MatType.CV_32F)
                return null;

            using var flat = feature.Reshape(1, 1);

            flat.GetArray(out float[] raw);

            var values = new double[count];
            for (var i = 0; i < count; i++)
                values[i] = raw[i];

            return values;
        }

        /// Returns a copy when the image is already small enough, so the caller can dispose one
        /// object either way. INTER_AREA is the correct filter for shrinking.
        private Mat Downscale(Mat source)
        {
            var longest = Math.Max(source.Width, source.Height);

            if (longest <= _settings.MaxImageDimension || longest == 0)
                return source.Clone();

            var scale = (double)_settings.MaxImageDimension / longest;

            var resized = new Mat();
            Cv2.Resize(source, resized,
                new Size(Math.Max(1, (int)Math.Round(source.Width * scale)),
                         Math.Max(1, (int)Math.Round(source.Height * scale))),
                interpolation: InterpolationFlags.Area);

            return resized;
        }

        /// The OpenCvSharp binding fixes YuNet's input size at construction - there is no
        /// setInputSize export - so the detector is rebuilt whenever the image size changes.
        /// Cached because a cohort's photos are usually the same size, and the detector model
        /// is only 232 KB so an occasional rebuild is cheap.
        private FaceDetectorYN? EnsureDetector(Size inputSize)
        {
            if (_detector != null && _detectorInputSize == inputSize)
                return _detector;

            _detector?.Dispose();
            _detector = null;

            try
            {
                _detector = FaceDetectorYN.Create(
                    _nativeDetectorPath ??= ToNativeSafePath(DetectorModelPath), string.Empty, inputSize,
                    _settings.DetectionScoreThreshold,
                    _settings.DetectionNmsThreshold,
                    _settings.DetectionTopK);

                _detectorInputSize = inputSize;
                return _detector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create the YuNet face detector.");
                return null;
            }
        }

        /// Loads the SFace network once. Lazy so a missing model or an unloadable native
        /// library degrades to a clear per-row import error instead of preventing the whole API
        /// from starting - every other endpoint is unaffected by a broken enrolment pipeline.
        private bool EnsureRecognizer()
        {
            if (_initialised)
                return !_unavailable;

            _initialised = true;

            if (!File.Exists(DetectorModelPath))
            {
                _logger.LogError(
                    "Face detection model not found at {DetectorModelPath}. Reference enrolment is unavailable.",
                    DetectorModelPath);
                _unavailable = true;
                return false;
            }

            if (!File.Exists(RecognizerModelPath))
            {
                _logger.LogError(
                    "Face recognition model not found at {RecognizerModelPath}. Reference enrolment is unavailable.",
                    RecognizerModelPath);
                _unavailable = true;
                return false;
            }

            try
            {
                _recognizer = CvDnn.ReadNetFromOnnx(ToNativeSafePath(RecognizerModelPath));

                if (_recognizer == null || _recognizer.Empty())
                {
                    _logger.LogError("The SFace ONNX model loaded but produced an empty network.");
                    _unavailable = true;
                    return false;
                }

                _logger.LogInformation(
                    "Face recognition models loaded: detector={DetectorModel}, recognizer={RecognizerModel}.",
                    Path.GetFileName(DetectorModelPath), Path.GetFileName(RecognizerModelPath));

                return true;
            }
            catch (Exception ex)
            {
                // Typically a missing OpenCvSharpExtern.dll or an architecture mismatch - the
                // deployment failure this project has hit before. Logged loudly, but the API
                // still starts and every other endpoint keeps working.
                _logger.LogError(ex,
                    "Failed to initialise OpenCV face recognition. Reference enrolment is unavailable.");
                _unavailable = true;
                return false;
            }
        }

        private string ResolvePath(string fileName) =>
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, _settings.ModelDirectory, fileName));

        /// Returns a path the OpenCV native layer can actually open.
        ///
        /// FaceDetectorYN.Create marshals its path as ANSI, so any non-ASCII character in the
        /// application directory reaches OpenCV mangled and the model "cannot be read". This is
        /// not hypothetical: this project lives under an Arabic directory name, where the path
        /// arrived as "...\4th year\?...? 2\..." and detector creation failed while the SFace
        /// load through CvDnn - which marshals UTF-8 correctly - succeeded from the very same
        /// folder. Two bindings, two behaviours, one confusing failure.
        ///
        /// When the resolved path is pure ASCII, which is the normal case on the Plesk/IIS
        /// host, it is returned untouched and nothing is copied. Otherwise the model is staged
        /// once into an ASCII-safe cache directory and loaded from there. The copy is skipped
        /// when an identically-sized file is already staged, so restarts do not recopy 37 MB.
        private string ToNativeSafePath(string path)
        {
            if (IsAscii(path))
                return path;

            var cacheDirectory = Path.Combine(Path.GetTempPath(), "exam-proctoring-face-models");

            // If even the temp path is non-ASCII there is nowhere safe to stage to; returning
            // the original at least produces OpenCV's own error rather than a confusing one.
            if (!IsAscii(cacheDirectory))
            {
                _logger.LogError(
                    "The application path contains non-ASCII characters and no ASCII-safe cache " +
                    "directory is available, so the OpenCV native layer cannot open the model files.");

                return path;
            }

            try
            {
                Directory.CreateDirectory(cacheDirectory);

                var staged = Path.Combine(cacheDirectory, Path.GetFileName(path));
                var source = new FileInfo(path);

                if (!File.Exists(staged) || new FileInfo(staged).Length != source.Length)
                {
                    File.Copy(path, staged, overwrite: true);

                    _logger.LogInformation(
                        "Staged {ModelFile} to an ASCII-safe path because the application directory " +
                        "contains non-ASCII characters that the OpenCV native layer cannot open.",
                        Path.GetFileName(path));
                }

                return staged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to stage {ModelFile} to an ASCII-safe path.", Path.GetFileName(path));

                return path;
            }
        }

        private static bool IsAscii(string value)
        {
            foreach (var c in value)
            {
                if (c > 127)
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;

                _detector?.Dispose();
                _recognizer?.Dispose();
            }
        }
    }
}
