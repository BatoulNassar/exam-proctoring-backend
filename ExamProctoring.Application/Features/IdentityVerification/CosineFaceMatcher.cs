using ExamProctoring.Application.Common.Interfaces;
using System;

namespace ExamProctoring.Application.Features.IdentityVerification
{
    /// Cosine similarity for SFace embeddings.
    ///
    /// This backend runs no face inference at all - both vectors already exist by the time
    /// they reach here. The reference was produced by the trusted administrative import and
    /// the probe by the student client, and both were L2-normalised by
    /// <see cref="Common.FaceEmbedding.TryCanonicalise"/> before storage or comparison.
    /// For unit vectors cosine similarity is exactly the dot product, so there is no division
    /// and therefore no divide-by-zero path.
    ///
    /// Pure and dependency-free on purpose: no EF, no HTTP, no configuration, no logging.
    public sealed class CosineFaceMatcher : IFaceMatcher
    {
        public double Similarity(ReadOnlySpan<float> reference, ReadOnlySpan<float> probe)
        {
            if (reference.Length != probe.Length)
                throw new ArgumentException(
                    "Face embeddings must have the same number of dimensions to be compared.",
                    nameof(probe));

            // Accumulated in double: 128 float32 products summed in float32 drifts enough to
            // move a score across a threshold.
            var dot = 0d;

            for (var i = 0; i < reference.Length; i++)
                dot += (double)reference[i] * probe[i];

            // Both inputs are unit vectors, so the true range is [-1, 1] and floating-point
            // error can push a perfect match a hair past 1. The contract publishes matchScore
            // as 0..1, and a negative cosine means "opposite direction", which for identity
            // purposes is simply no similarity at all.
            return Math.Clamp(dot, 0d, 1d);
        }
    }
}
