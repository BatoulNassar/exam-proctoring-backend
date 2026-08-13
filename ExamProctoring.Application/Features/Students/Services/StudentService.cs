using ExamProctoring.Application.Common.DTOs;
using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.Students.DTOs;
using ExamProctoring.Domain.Entities;
using System.IO.Compression;
using System.Text;
using System.Net.Mail;

namespace ExamProctoring.Application.Features.Students.Services
{
    public class StudentService : IStudentService
    {
        private const string DefaultImportPassword = "DefaultPassword123!";

        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICloudinaryService _cloudinaryService;

        public StudentService(
            IStudentRepository studentRepository,
            IPasswordHasher passwordHasher,
            ICloudinaryService cloudinaryService)
        {
            _studentRepository = studentRepository;
            _passwordHasher = passwordHasher;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<PagedResult<StudentDto>> GetAllStudentsAsync(int page, int pageSize)
        {
            var students = await _studentRepository.GetPagedAsync(page, pageSize);
            var totalCount = await _studentRepository.CountAsync();

            var items = students.Select(s => new StudentDto
            {
                Id = s.id,
                UserName = s.user_name,
                Email = s.email,
                PhoneNumber = s.phone_number,
                FirstName = s.first_name,
                MiddleName = s.middle_name,
                LastName = s.last_name,
                UniversityNumber = s.university_number
            }).ToList();

            return new PagedResult<StudentDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // The ZIP must contain:
        //   - One .csv file with header: university_number,first_name,middle_name,last_name,email,phone_number
        //   - Photo files named {university_number}.jpg / .jpeg / .png
        // A row is imported only if it is complete: university_number, first_name,
        // last_name, a valid email, and a matching photo. Anything missing fails the
        // row and is reported back, so a partial student never reaches the database.
        public async Task<ImportStudentsCsvResponse> ImportStudentsFromCsvAsync(Stream importZipStream, int adminId)
        {
            var response = new ImportStudentsCsvResponse();

            if (importZipStream == null || importZipStream.Length == 0)
            {
                response.Results.Add(new StudentImportResult { IsSuccess = false, Message = "ZIP file is empty or invalid" });
                return response;
            }

            string? csvContent = null;
            var photos = new Dictionary<string, (byte[] Data, string Extension)>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var zip = new ZipArchive(importZipStream, ZipArchiveMode.Read, leaveOpen: true);

                foreach (var entry in zip.Entries)
                {
                    var ext = Path.GetExtension(entry.Name).ToLowerInvariant();

                    if (ext == ".csv" && csvContent == null)
                    {
                        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                        csvContent = await reader.ReadToEndAsync();
                    }
                    else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                    {
                        var key = Path.GetFileNameWithoutExtension(entry.Name);
                        if (string.IsNullOrEmpty(key)) continue;

                        using var ms = new MemoryStream();
                        await entry.Open().CopyToAsync(ms);
                        photos[key] = (ms.ToArray(), ext);
                    }
                }
            }
            catch (Exception ex)
            {
                response.Results.Add(new StudentImportResult { IsSuccess = false, Message = $"Failed to read ZIP: {ex.Message}" });
                return response;
            }

            if (csvContent == null)
            {
                response.Results.Add(new StudentImportResult { IsSuccess = false, Message = "No CSV file found inside the ZIP" });
                return response;
            }

            if (photos.Count == 0)
            {
                response.Results.Add(new StudentImportResult { IsSuccess = false, Message = "No photo files found inside the ZIP" });
                return response;
            }

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var result = await ProcessStudentRowAsync(lines[i].TrimEnd('\r'), i + 1, photos, adminId);
                response.Results.Add(result);
                response.TotalRecords++;
                if (result.IsSuccess) response.SuccessfulImports++;
                else response.FailedImports++;
            }

            return response;
        }

        private async Task<StudentImportResult> ProcessStudentRowAsync(
            string line, int lineNumber,
            Dictionary<string, (byte[] Data, string Extension)> photos,
            int adminId)
        {
            try
            {
                var parts = ParseCsvLine(line);

                if (parts.Length < 5)
                    return Fail(lineNumber, null, "Insufficient columns. Expected: university_number,first_name,middle_name,last_name,email[,phone_number]");

                var universityNumber = parts[0].Trim();
                var firstName        = parts[1].Trim();
                var middleName       = parts[2].Trim();
                var lastName         = parts[3].Trim();
                var email            = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                var phoneNumber      = parts.Length > 5 ? parts[5].Trim() : string.Empty;

                if (string.IsNullOrEmpty(universityNumber) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                    return Fail(lineNumber, universityNumber, "university_number, first_name, and last_name are required");

                // Every student must arrive with a real address. Deriving one from the
                // university number would look valid while going nowhere, so a row
                // without an email is rejected rather than quietly patched.
                if (string.IsNullOrEmpty(email))
                    return Fail(lineNumber, universityNumber, "email is required");

                if (!IsValidEmail(email))
                    return Fail(lineNumber, universityNumber, $"Invalid email format: {email}");

                if (!photos.TryGetValue(universityNumber, out var photo))
                    return Fail(lineNumber, universityNumber, $"Photo not found in ZIP (expected: {universityNumber}.jpg/png)");

                using var photoStream = new MemoryStream(photo.Data);
                var (uploadSuccess, photoUrl, uploadError) =
                    await _cloudinaryService.UploadImageAsync(photoStream, $"{universityNumber}{photo.Extension}");

                if (!uploadSuccess)
                    return Fail(lineNumber, universityNumber, $"Cloudinary upload failed: {uploadError}");

                var fullName = $"{firstName} {middleName} {lastName}".Replace("  ", " ").Trim();

                var existing = await _studentRepository.GetByUniversityNumberAsync(universityNumber);

                if (existing != null)
                {
                    existing.first_name  = firstName;
                    existing.middle_name = middleName;
                    existing.last_name   = lastName;
                    existing.photo_url   = photoUrl;
                    // email is guaranteed present and valid by the checks above.
                    existing.email       = email;
                    if (!string.IsNullOrEmpty(phoneNumber)) existing.phone_number = phoneNumber;
                    existing.updated_at = DateTime.UtcNow;
                    existing.updated_by = adminId;

                    await _studentRepository.UpdateAsync(existing);

                    return new StudentImportResult
                    {
                        StudentId = existing.id,
                        UniversityNumber = universityNumber,
                        FullName = fullName,
                        Email = existing.email,
                        PhoneNumber = existing.phone_number,
                        PhotoUrl = photoUrl,
                        IsSuccess = true,
                        Message = "Updated existing student"
                    };
                }
                else
                {
                    var student = new Student
                    {
                        university_number = universityNumber,
                        first_name        = firstName,
                        middle_name       = middleName,
                        last_name         = lastName,
                        email             = email,
                        phone_number      = phoneNumber,
                        user_name         = universityNumber,
                        password          = _passwordHasher.Hash(DefaultImportPassword),
                        photo_url         = photoUrl,
                        face_id           = string.Empty,
                        created_at        = DateTime.UtcNow,
                        created_by        = adminId
                    };

                    await _studentRepository.AddAsync(student);

                    return new StudentImportResult
                    {
                        StudentId = student.id,
                        UniversityNumber = universityNumber,
                        FullName = fullName,
                        Email = email,
                        PhoneNumber = phoneNumber,
                        PhotoUrl = photoUrl,
                        IsSuccess = true,
                        Message = "Created new student"
                    };
                }
            }
            catch (Exception ex)
            {
                return Fail(lineNumber, null, ex.InnerException?.Message ?? ex.Message);
            }
        }

        private static StudentImportResult Fail(int lineNumber, string? universityNumber, string reason) =>
            new() { IsSuccess = false, UniversityNumber = universityNumber ?? string.Empty, Message = $"Line {lineNumber}: {reason}" };

        /// <summary>
        /// Validates an address against the parts of RFC 5321/5322 that matter for a
        /// student roster. MailAddress on its own is too permissive — it accepts
        /// display-name forms like "Name &lt;a@b.com&gt;" and doubled dots — so the
        /// structural rules are enforced explicitly on top of it.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var trimmed = email.Trim();

            // RFC 5321 size limit for a whole address.
            if (trimmed.Length > 254)
                return false;

            string parsed;
            try
            {
                // Catches the obviously malformed: no @, empty local part, empty domain.
                parsed = new MailAddress(trimmed).Address;
            }
            catch
            {
                return false;
            }

            // MailAddress parses "Name <a@b.com>" down to "a@b.com", so require the
            // result to round-trip. Compared case-insensitively: addresses preserve
            // case but do not depend on it, and rosters routinely capitalise names.
            if (!string.Equals(parsed, trimmed, StringComparison.OrdinalIgnoreCase))
                return false;

            var at = trimmed.LastIndexOf('@');
            var localPart = trimmed.Substring(0, at);
            var domain = trimmed.Substring(at + 1);

            // RFC 5321 size limit for the local part.
            if (localPart.Length > 64)
                return false;

            // A dot separates parts; it may not lead, trail, or repeat on either side.
            if (!HasValidDotPlacement(localPart) || !HasValidDotPlacement(domain))
                return false;

            // Require a TLD. "student@vu" is legal on an intranet but is always a
            // typo on a university roster.
            if (!domain.Contains('.'))
                return false;

            foreach (var label in domain.Split('.'))
            {
                if (label.Length > 63)
                    return false;

                // A hyphen may join a label but may not start or end one.
                if (label[0] == '-' || label[label.Length - 1] == '-')
                    return false;

                foreach (var c in label)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True when <paramref name="part"/> is non-empty and its dots neither lead,
        /// trail, nor repeat. Empty labels are impossible once this passes, so callers
        /// splitting on '.' can index the result safely.
        /// </summary>
        private static bool HasValidDotPlacement(string part) =>
            part.Length > 0
            && part[0] != '.'
            && part[part.Length - 1] != '.'
            && !part.Contains("..");

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
