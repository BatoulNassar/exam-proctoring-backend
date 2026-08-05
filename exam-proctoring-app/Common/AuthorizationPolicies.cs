namespace ExamProctoring.API.Common
{
    public static class AuthorizationPolicies
    {
        /// Any authenticated dashboard user (SuperAdmin, Admin, Proctor). Applied to dashboard endpoints
        /// that are not role-restricted, so that student desktop tokens cannot reach them.
        public const string DashboardOnly = "DashboardOnly";

        /// A student desktop token only: requires token_type=student and a positive
        /// integer student_id claim. Dashboard tokens are rejected.
        public const string StudentOnly = "StudentOnly";
    }
}
