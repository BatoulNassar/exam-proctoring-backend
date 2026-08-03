using System;
using System.Globalization;

namespace ExamProctoring.Application.Common
{
    /// A Flutter release version: major.minor.patch with an optional +buildNumber.
    /// Components are compared numerically, so 1.10.0 is newer than 1.9.0 and
    /// 1.0.0+10 is newer than 1.0.0+2. A missing build number is treated as 0.
    public readonly struct AppVersion : IComparable<AppVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Build { get; }

        private AppVersion(int major, int minor, int patch, int build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        public static bool TryParse(string? value, out AppVersion version)
        {
            version = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();

            var build = 0;
            var plusIndex = trimmed.IndexOf('+');
            if (plusIndex >= 0)
            {
                if (!TryParseComponent(trimmed.Substring(plusIndex + 1), out build))
                    return false;

                trimmed = trimmed.Substring(0, plusIndex);
            }

            var parts = trimmed.Split('.');
            if (parts.Length != 3)
                return false;

            if (!TryParseComponent(parts[0], out var major)
                || !TryParseComponent(parts[1], out var minor)
                || !TryParseComponent(parts[2], out var patch))
                return false;

            version = new AppVersion(major, minor, patch, build);
            return true;
        }

        /// Digits only: rejects signs, spaces, separators and any other numeric styles.
        private static bool TryParseComponent(string value, out int result)
        {
            result = 0;

            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var c in value)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        public int CompareTo(AppVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            return Build.CompareTo(other.Build);
        }

        public static bool operator <(AppVersion left, AppVersion right) => left.CompareTo(right) < 0;
        public static bool operator >(AppVersion left, AppVersion right) => left.CompareTo(right) > 0;
        public static bool operator <=(AppVersion left, AppVersion right) => left.CompareTo(right) <= 0;
        public static bool operator >=(AppVersion left, AppVersion right) => left.CompareTo(right) >= 0;

        public override string ToString() =>
            Build > 0
                ? $"{Major}.{Minor}.{Patch}+{Build}"
                : $"{Major}.{Minor}.{Patch}";
    }
}
