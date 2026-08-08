using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    /// TEMPORARY implementation of the identity gate, reading the verification state that
    /// already exists on StudentSession.
    ///
    /// The Identity Verification feature is being built separately and will replace this class
    /// wholesale. Nothing outside this file knows how identity is stored, so the replacement
    /// requires no change to ExamAttemptService or to any controller.
    ///
    /// The gate deliberately keys off verified_at rather than face_match_passed: a proctor
    /// override is a legitimate pass in which the face never matched, so requiring
    /// face_match_passed == true would wrongly block every overridden student.
    /// face_match_passed is used only to report which method was used.
    public class StudentSessionIdentityGate : IIdentityGate
    {
        private readonly AppDbContext _context;

        public StudentSessionIdentityGate(AppDbContext context)
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
