using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    /// The identity gate, backed by the real Identity Verification feature.
    ///
    /// Replaces the temporary implementation that shipped with Start Exam. Nothing above this
    /// class changed: ExamAttemptService still asks only whether identity is settled for an
    /// attempt and by which method, and has never known that face embeddings exist.
    ///
    /// It reads StudentSession rather than IdentityVerificationSession on purpose. That row is
    /// written inside the same transaction as the successful attempt, it is what the proctor
    /// dashboard already displays, and it is the one place a future proctor override can also
    /// record a pass - so the gate keeps working for a method that does not exist yet.
    ///
    /// The gate deliberately keys off verified_at rather than face_match_passed: a proctor
    /// override is a legitimate pass in which the face never matched, so requiring
    /// face_match_passed == true would wrongly block every overridden student.
    /// face_match_passed is used only to report which method was used.
    public class PersistedIdentityGate : IIdentityGate
    {
        private readonly AppDbContext _context;

        public PersistedIdentityGate(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IdentityGateResult> GetForAttemptAsync(int studentSessionId)
        {
            var state = await _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.id == studentSessionId)
                .Select(ss => new { ss.verified_at, ss.face_match_passed })
                .FirstOrDefaultAsync();

            if (state?.verified_at == null)
                return IdentityGateResult.NotVerified();

            var method = state.face_match_passed
                ? IdentityVerificationMethods.FaceMatch
                : IdentityVerificationMethods.ProctorOverride;

            return IdentityGateResult.Verified(method, state.verified_at.Value);
        }
    }
}
