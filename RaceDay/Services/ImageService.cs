using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services
{
    public interface IImageService
    {
        string Validate(IFormFile file);
        Task<string> UploadAsync(IFormFile file, string folder);
    }

    public class ImageService : IImageService
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };

        private readonly BlobContainerClient _container;

        public ImageService(IConfiguration config)
        {
            _container = new BlobContainerClient(
                config["AzureStorage:ConnectionString"],
                config["AzureStorage:ContainerName"]);
        }

        // Returns an error message, or null if the file is acceptable
        public string Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image file.";
            }

            if (file.Length > MaxBytes)
            {
                return "The image must be 5 MB or smaller.";
            }

            if (!AllowedTypes.Contains(file.ContentType))
            {
                return "Only JPEG, PNG or WebP images are allowed.";
            }

            return null;
        }

        // Stores the file in Blob Storage and returns its public address
        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            string extension = Path.GetExtension(file.FileName).ToLower();
            string blobName = folder + "/" + Guid.NewGuid().ToString() + extension;

            BlobClient blob = _container.GetBlobClient(blobName);

            using (Stream stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return blob.Uri.ToString();
        }
    }
}