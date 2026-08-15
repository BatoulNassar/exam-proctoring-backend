using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace ExamProctoring.Application.Common
{
    /// Why an embedding was refused. The caller decides the HTTP shape; this type only says
    /// what was wrong, so the import path and the verification path cannot drift apart on
    /// what counts as a valid vector.
    public enum FaceEmbeddingError
    {
        None = 0,

        /// Null, or not exactly <see cref="FaceEmbedding.Dimensions"/> values.
        WrongLength,

        /// Contains NaN or +/-Infinity.
        NonFinite,

        /// L2 norm is zero or so close to zero that direction is meaningless - an all-zero
        /// vector has no defined cosine similarity against anything.
        Degenerate,
    }

    /// The single definition of what a valid SFace face embedding is, and the single place
    /// that converts between the wire form (a JSON array of numbers), the canonical in-memory
    /// form (L2-normalised float32) and the stored form (512 bytes of little-endian float32).
    ///
    /// Shared deliberately: the administrative import writes reference vectors and the identity
    /// endpoints read probe vectors, and if those two ever disagreed about length, finiteness
    /// or normalisation, the symptom would be every student failing verification at once with
    /// no error anywhere.
    ///
    /// Nothing here logs, and nothing here formats a vector for output. Values must never
    /// appear in a log line, an audit row, an exception message or an API response.
    public static class FaceEmbedding
    {
        /// SFace produces a 128-dimensional vector. Not configurable: a different width means
        /// a different model, which is a mismatch rather than a resizeable parameter.
        public const int Dimensions = 128;

        /// 128 * sizeof(float32).
        public const int StorageBytes = Dimensions * sizeof(float);

        /// Recognition model this backend accepts. Confirmed with the Flutter team.
        public const string Model = "sface";

        /// Pinned model release. Two embeddings from different releases are not comparable.
        public const string ModelVersion = "2021dec";

        /// Below this the vector carries no usable direction. Chosen far under any real SFace
        /// output (which is dominated by values around unit scale) so it only ever catches
        /// genuinely degenerate input such as an all-zero array.
        private const double MinimumNorm = 1e-6;

        public static bool IsSupportedModel(string? model) =>
            string.Equals(model, Model, StringComparison.OrdinalIgnoreCase);

        public static bool IsSupportedVersion(string? version) =>
            string.Equals(version, ModelVersion, StringComparison.OrdinalIgnoreCase);

        /// Validates a wire-form vector and returns its canonical L2-normalised form.
        ///
        /// Normalising on receipt rather than trusting the sender is what lets comparison be a
        /// plain dot product: both sides of every future comparison are unit vectors by
        /// construction, so nobody can forget to normalise one of them.
        public static bool TryCanonicalise(
            IReadOnlyList<double>? values, out float[] canonical, out FaceEmbeddingError error)
        {
            canonical = Array.Empty<float>();

            if (values == null || values.Count != Dimensions)
            {
                error = FaceEmbeddingError.WrongLength;
                return false;
            }

            // Accumulate in double: squaring 128 float32 values loses precision that matters
            // when the result divides every element.
            var sumOfSquares = 0d;

            for (var i = 0; i < Dimensions; i++)
            {
                var value = values[i];

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    error = FaceEmbeddingError.NonFinite;
                    return false;
                }

                sumOfSquares += value * value;
            }

            var norm = Math.Sqrt(sumOfSquares);

            // Also catches a sum that overflowed to infinity from finite-but-enormous inputs.
            if (!double.IsFinite(norm) || norm < MinimumNorm)
            {
                error = FaceEmbeddingError.Degenerate;
                return false;
            }

            var result = new float[Dimensions];
            for (var i = 0; i < Dimensions; i++)
                result[i] = (float)(values[i] / norm);

            canonical = result;
            error = FaceEmbeddingError.None;
            return true;
        }

        /// Canonical vector to its stored form. Little-endian regardless of host architecture,
        /// so a database restored onto a different machine still decodes identically.
        public static byte[] ToStorage(float[] canonical)
        {
            if (canonical == null || canonical.Length != Dimensions)
                throw new ArgumentException(
                    $"A stored embedding must have exactly {Dimensions} values.", nameof(canonical));

            var bytes = new byte[StorageBytes];

            for (var i = 0; i < Dimensions; i++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    bytes.AsSpan(i * sizeof(float), sizeof(float)), canonical[i]);

            return bytes;
        }

        /// Stored form back to a canonical vector. Returns false rather than throwing for a
        /// wrong-sized blob, so a corrupt row degrades to "not enrolled" instead of a 500.
        public static bool TryFromStorage(byte[]? stored, out float[] canonical)
        {
            canonical = Array.Empty<float>();

            if (stored == null || stored.Length != StorageBytes)
                return false;

            var result = new float[Dimensions];

            for (var i = 0; i < Dimensions; i++)
                result[i] = BinaryPrimitives.ReadSingleLittleEndian(
                    stored.AsSpan(i * sizeof(float), sizeof(float)));

            canonical = result;
            return true;
        }
    }
}
