using OpenCvSharp;
using System;

namespace ExamProctoring.Infrastructure.Identity
{
    /// The 5-point similarity alignment SFace requires, ported from OpenCV's own
    /// FaceRecognizerSF::alignCrop (modules/objdetect/src/face_recognize.cpp).
    ///
    /// WHY THIS EXISTS: OpenCvSharp 4.13 wraps FaceDetectorYN but does NOT wrap
    /// FaceRecognizerSF - the objdetect binding exposes only create/detect for the detector and
    /// nothing at all for the recognizer. The alignment step therefore has to be performed
    /// here, and it is reproduced exactly rather than approximated, because a slightly
    /// different warp puts the reference vector in a slightly different place to the client's
    /// probe and every student quietly stops matching.
    ///
    /// Two deliberate choices follow from that:
    ///
    /// 1. The destination landmarks are OpenCV's own constants, not a re-derivation. They are
    ///    the standard ArcFace 112x112 reference points that SFace was trained against.
    /// 2. The transform is the closed-form Umeyama similarity fit OpenCV uses, NOT
    ///    estimateAffinePartial2D. The latter is RANSAC-based and therefore not deterministic,
    ///    and "the same photo enrolled twice gives slightly different vectors" is exactly the
    ///    kind of defect that would only surface as unexplained match failures.
    ///
    /// <see cref="TryComputeTransform"/> is verifiable on its own: applying the returned matrix
    /// to the source landmarks must land them on <see cref="DestinationLandmarks"/>.
    internal static class SFaceAlignment
    {
        /// SFace's aligned input is always 112x112.
        public const int AlignedSize = 112;

        public const int LandmarkCount = 5;

        /// Canonical destination landmarks: right eye, left eye, nose tip, right mouth corner,
        /// left mouth corner - in the order YuNet emits them.
        public static readonly float[,] DestinationLandmarks =
        {
            { 38.2946f, 51.6963f },
            { 73.5318f, 51.5014f },
            { 56.0252f, 71.7366f },
            { 41.5493f, 92.3655f },
            { 70.7299f, 92.2041f },
        };

        /// Mean of the destination landmarks. OpenCV hard-codes this constant rather than
        /// recomputing it; kept identical so the arithmetic matches bit for bit.
        private static readonly double[] DestinationMean = { 56.0262d, 71.9008d };

        /// Computes the 2x3 similarity transform mapping the detected landmarks onto the
        /// canonical ones. Returns false for a degenerate landmark set, which is an alignment
        /// failure rather than a crash.
        public static bool TryComputeTransform(float[,] sourceLandmarks, out Mat transform)
        {
            transform = null!;

            if (sourceLandmarks.GetLength(0) != LandmarkCount || sourceLandmarks.GetLength(1) != 2)
                return false;

            // ----- centre both point sets -----
            double srcMeanX = 0, srcMeanY = 0;
            for (var i = 0; i < LandmarkCount; i++)
            {
                srcMeanX += sourceLandmarks[i, 0];
                srcMeanY += sourceLandmarks[i, 1];
            }

            srcMeanX /= LandmarkCount;
            srcMeanY /= LandmarkCount;

            var srcDemean = new double[LandmarkCount, 2];
            var dstDemean = new double[LandmarkCount, 2];

            for (var i = 0; i < LandmarkCount; i++)
            {
                srcDemean[i, 0] = sourceLandmarks[i, 0] - srcMeanX;
                srcDemean[i, 1] = sourceLandmarks[i, 1] - srcMeanY;
                dstDemean[i, 0] = DestinationLandmarks[i, 0] - DestinationMean[0];
                dstDemean[i, 1] = DestinationLandmarks[i, 1] - DestinationMean[1];
            }

            // ----- covariance of destination against source -----
            double a00 = 0, a01 = 0, a10 = 0, a11 = 0;
            for (var i = 0; i < LandmarkCount; i++)
            {
                a00 += dstDemean[i, 0] * srcDemean[i, 0];
                a01 += dstDemean[i, 0] * srcDemean[i, 1];
                a10 += dstDemean[i, 1] * srcDemean[i, 0];
                a11 += dstDemean[i, 1] * srcDemean[i, 1];
            }

            a00 /= LandmarkCount;
            a01 /= LandmarkCount;
            a10 /= LandmarkCount;
            a11 /= LandmarkCount;

            using var covariance = new Mat(2, 2, MatType.CV_64F);
            covariance.Set(0, 0, a00);
            covariance.Set(0, 1, a01);
            covariance.Set(1, 0, a10);
            covariance.Set(1, 1, a11);

            // ----- source variance -----
            double srcVariance = 0;
            for (var i = 0; i < LandmarkCount; i++)
                srcVariance += srcDemean[i, 0] * srcDemean[i, 0] + srcDemean[i, 1] * srcDemean[i, 1];

            srcVariance /= LandmarkCount;

            // Every landmark at the same point: no scale can be recovered.
            if (srcVariance <= double.Epsilon)
                return false;

            // ----- Umeyama: R = U * diag(d) * Vt, scale from the singular values -----
            // Mirrors OpenCV's getSimilarityTransformMatrix step for step, including the
            // reflection guard: a negative covariance determinant means the best plain fit
            // would mirror the face, and flipping the second singular direction is what keeps
            // the result a true rotation.
            using var w = new Mat();
            using var u = new Mat();
            using var vt = new Mat();
            Cv2.SVDecomp(covariance, w, u, vt);

            var detCovariance = a00 * a11 - a01 * a10;

            var d0 = 1.0;
            var d1 = detCovariance < 0 ? -1.0 : 1.0;

            using var d = new Mat(2, 2, MatType.CV_64F, Scalar.All(0));
            d.Set(0, 0, d0);
            d.Set(1, 1, d1);

            using var rotation = u * d * vt;

            var singular0 = w.At<double>(0, 0);
            var singular1 = w.At<double>(1, 0);

            // srcVariance is var1 + var2 in OpenCV's notation; it is strictly positive here
            // because the degenerate case was rejected above.
            var scale = (singular0 * d0 + singular1 * d1) / srcVariance;

            if (!double.IsFinite(scale) || Math.Abs(scale) <= double.Epsilon)
                return false;

            var rotationMat = rotation.ToMat();
            var r00 = rotationMat.At<double>(0, 0);
            var r01 = rotationMat.At<double>(0, 1);
            var r10 = rotationMat.At<double>(1, 0);
            var r11 = rotationMat.At<double>(1, 1);
            rotationMat.Dispose();

            var result = new Mat(2, 3, MatType.CV_64F);
            result.Set(0, 0, scale * r00);
            result.Set(0, 1, scale * r01);
            result.Set(1, 0, scale * r10);
            result.Set(1, 1, scale * r11);
            result.Set(0, 2, DestinationMean[0] - scale * (r00 * srcMeanX + r01 * srcMeanY));
            result.Set(1, 2, DestinationMean[1] - scale * (r10 * srcMeanX + r11 * srcMeanY));

            transform = result;
            return true;
        }

        /// Largest distance, in pixels, between a transformed source landmark and where the
        /// canonical layout says it should be.
        ///
        /// This is the alignment's own correctness check. If the warp is right the five points
        /// land essentially on top of the reference layout; a large residual means the fit has
        /// gone wrong and the resulting embedding would be silently unusable.
        public static double MaxLandmarkResidual(Mat transform, float[,] sourceLandmarks)
        {
            var worst = 0d;

            for (var i = 0; i < LandmarkCount; i++)
            {
                var x = transform.At<double>(0, 0) * sourceLandmarks[i, 0]
                        + transform.At<double>(0, 1) * sourceLandmarks[i, 1]
                        + transform.At<double>(0, 2);

                var y = transform.At<double>(1, 0) * sourceLandmarks[i, 0]
                        + transform.At<double>(1, 1) * sourceLandmarks[i, 1]
                        + transform.At<double>(1, 2);

                var dx = x - DestinationLandmarks[i, 0];
                var dy = y - DestinationLandmarks[i, 1];

                worst = Math.Max(worst, Math.Sqrt(dx * dx + dy * dy));
            }

            return worst;
        }
    }
}
