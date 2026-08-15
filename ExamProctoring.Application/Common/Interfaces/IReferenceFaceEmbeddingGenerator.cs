using System;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Why a trusted reference embedding could not be produced from an administrative photo.
    /// Each value maps to an admin-facing import message; none of them ever carries a vector.
    public enum ReferenceFaceEmbeddingStatus
    {
        Success = 0,

        /// The bytes are not a decodable image.
        ImageUndecodable,

        /// YuNet found no face. The photo cannot enrol this student.
        NoFaceDetected,

        /// YuNet found more than one face. Deliberately a failure rather than a choice:
        /// silently picking the largest face is how an invigilator standing behind a student
        /// becomes that student's reference identity.
        MultipleFacesDetected,

        /// SFace's 5-point similarity transform could not produce an aligned 112x112 chip.
        AlignmentFailed,

        /// SFace ran but threw or produced nothing usable.
        InferenceFailed,

        /// The feature was not exactly 128 finite, non-degenerate values.
        InvalidEmbedding,

        /// The generator itself could not start - missing model files, or the OpenCV native
        /// library failed to load. An operational fault, not a problem with this photograph.
        GeneratorUnavailable,
    }

    /// Outcome of generating one trusted reference embedding.
    public sealed class ReferenceFaceEmbeddingResult
    {
        public ReferenceFaceEmbeddingStatus Status { get; private init; }

        /// Canonical L2-normalised vector in storage form: exactly 512 bytes of
        /// little-endian float32. Null unless <see cref="Status"/> is Success.
        ///
        /// Biometric data. Never logged, never returned by an API, never placed in audit text.
        public byte[]? Embedding { get; private init; }

        /// Short admin-facing explanation, safe to show in an import result.
        /// Never contains any element of the vector.
        public string? FailureReason { get; private init; }

        public bool IsSuccess => Status == ReferenceFaceEmbeddingStatus.Success;

        public static ReferenceFaceEmbeddingResult Success(byte[] embedding) =>
            new() { Status = ReferenceFaceEmbeddingStatus.Success, Embedding = embedding };

        public static ReferenceFaceEmbeddingResult Failure(
            ReferenceFaceEmbeddingStatus status, string reason) =>
            new() { Status = status, FailureReason = reason };
    }

    /// Produces the trusted SFace reference embedding from an administratively imported
    /// student photograph.
    ///
    /// This is the ONLY way a reference identity enters the system. There is no student-facing
    /// enrolment path and no sidecar file: the trusted official photo is the single source of
    /// truth, so a student can never influence the vector they are matched against.
    ///
    /// Implementations run the canonical OpenCV pipeline - decode, YuNet detection with
    /// landmarks, SFace AlignCrop, SFace Feature - which is the same pipeline the Flutter
    /// client runs to produce the live probe. That symmetry is the whole reason matching works:
    /// two embeddings are only comparable when produced by the identical model and alignment.
    ///
    /// The Application layer deliberately knows none of that. It sees image bytes in and a
    /// canonical vector out, so no OpenCV type ever crosses this boundary.
    public interface IReferenceFaceEmbeddingGenerator
    {
        /// Synchronous because the work is CPU-bound native inference; wrapping it in a Task
        /// would add a thread hop without making anything concurrent.
        ReferenceFaceEmbeddingResult Generate(byte[] imageBytes);
    }
}
