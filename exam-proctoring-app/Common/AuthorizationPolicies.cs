namespace ExamProctoring.API.Common
{
    public static class AuthorizationPolicies
    {
        /// Any authenticated dashboard user (SuperAdmin, Admin, Proctor). Applied to dashboard endpoints
        /// that are not role-restricted, so that student desktop tokens cannot reach them.
        public const string DashboardOnly = "DashboardOnly";
    }
}
