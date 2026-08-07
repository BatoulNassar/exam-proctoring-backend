namespace ExamProctoring.Domain.Enums
{
    /// Outcome reported by the student desktop client for a single device requirement.
    /// Persisted as a string, matching the project's enum convention.
    public enum DeviceCheckStatus
    {
        Passed = 1,
        Warning = 2,
        Failed = 3,
    }
}
