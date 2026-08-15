using System;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Compares a probe face embedding against a trusted reference and returns a similarity.
    ///
    /// The seam exists so the comparison is a named, testable, replaceable thing rather than
    /// arithmetic buried in a service method - and so nothing above it needs to know whether
    /// similarity is cosine, L2 or something else later.
    ///
    /// It deliberately returns a score and NOT a verdict. Threshold ownership stays in the
    /// Application service, because "did this pass" depends on configuration the matcher has
    /// no business reading.
    public interface IFaceMatcher
    {
        /// Similarity in 0..1 for two canonical L2-normalised vectors of equal length.
        /// Throws only for a programming error (mismatched lengths); callers validate first.
        double Similarity(ReadOnlySpan<float> reference, ReadOnlySpan<float> probe);
    }
}
