namespace ExamProctoring.Application.Features.ExamSessions.DTOs
{
    public class EditRestoreSessionRequest
    {
        /// <summary>
        /// Proctor IDs to assign to this session
        /// </summary>
        public int[]? AssignedProctorIds { get; set; }

        /// <summary>
        /// Student IDs to add to this session
        /// </summary>
        public int[]? StudentIdsToAdd { get; set; }

        /// <summary>
        /// Student IDs to remove from this session
        /// </summary>
        public int[]? StudentIdsToRemove { get; set; }
    }
}
