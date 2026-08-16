using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace ExamProctoring.Application.Features.Students.Services
{
    public interface ICloudinaryService
    {
        Task<(bool Success, string Url, string ErrorMessage)> UploadImageAsync(Stream imageStream, string fileName);

        Task<(bool Success, string Url, string ErrorMessage)> UploadImageAsync(
            Stream imageStream, string fileName, string folder);

        Task<bool> DeleteImageAsync(string publicId);
    }

    public class CloudinaryService : ICloudinaryService
    {
        public const string StudentFacesFolder = "exam-proctoring/student-faces";
        public const string AlertSnapshotsFolder = "exam-proctoring/alert-snapshots";

        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                throw new InvalidOperationException("Cloudinary credentials not configured");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public Task<(bool Success, string Url, string ErrorMessage)> UploadImageAsync(
            Stream imageStream, string fileName) =>
            UploadImageAsync(imageStream, fileName, StudentFacesFolder);

        public async Task<(bool Success, string Url, string ErrorMessage)> UploadImageAsync(
            Stream imageStream, string fileName, string folder)
        {
            try
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileName, imageStream),
                    PublicId = $"{Path.GetFileNameWithoutExtension(fileName)}-{Guid.NewGuid()}",
                    Folder = string.IsNullOrWhiteSpace(folder) ? StudentFacesFolder : folder,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    return (false, null, uploadResult.Error.Message);

                return (true, uploadResult.SecureUrl.ToString(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
