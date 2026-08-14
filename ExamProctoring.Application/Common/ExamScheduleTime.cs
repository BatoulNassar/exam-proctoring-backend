using System;

namespace ExamProctoring.Application.Common
{
    /// <summary>
    /// Turns the separate date and time an admin schedules an exam with into the
    /// single UTC instant the rest of the system compares against.
    /// </summary>
    /// <remarks>
    /// A bare "11:30" means nothing on its own — 11:30 where? Every scheduling
    /// comparison runs against <see cref="DateTime.UtcNow"/>, so the pair has to be
    /// anchored to a zone before it can be stored. It is anchored to Damascus
    /// explicitly rather than to whatever zone the server happens to run in, so the
    /// meaning of a scheduled time does not change when the API is deployed
    /// somewhere else.
    /// </remarks>
    public static class ExamScheduleTime
    {
        /// <summary>Windows and IANA ids for the same zone, so this resolves on either host.</summary>
        private static readonly string[] ZoneIds = { "Syria Standard Time", "Asia/Damascus" };

        private static TimeZoneInfo? _zone;

        private static TimeZoneInfo Zone
        {
            get
            {
                if (_zone != null)
                    return _zone;

                foreach (var id in ZoneIds)
                {
                    try
                    {
                        _zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                        return _zone;
                    }
                    catch (TimeZoneNotFoundException) { }
                    catch (InvalidTimeZoneException) { }
                }

                // Last resort so scheduling still works on a host with no zone database.
                _zone = TimeZoneInfo.CreateCustomTimeZone("ExamProctoring/Damascus", TimeSpan.FromHours(3), "Damascus", "Damascus");
                return _zone;
            }
        }

        /// <summary>
        /// Combines a local date and time-of-day into the matching UTC instant.
        /// </summary>
        public static DateTime ToUtc(DateOnly date, TimeOnly time)
        {
            var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);

            // A clock that springs forward can leave an hour that never happens; treat
            // that as the moment the jump lands on rather than rejecting the booking.
            if (Zone.IsInvalidTime(local))
                local = local.AddHours(1);

            return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
        }

        /// <summary>
        /// The reverse, for showing a stored instant back as the local wall clock.
        /// </summary>
        public static (DateOnly Date, TimeOnly Time) FromUtc(DateTime utc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
            return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
        }
    }
}
