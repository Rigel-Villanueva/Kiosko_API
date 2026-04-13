using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace KioskoAPI.Services
{
    public class CloudinaryStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        private string GetFolderForExtension(string extension)
        {
            var ext = extension.ToLower();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".webp")
                return "Kiosko/imagenes";
            if (ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv" || ext == ".webm")
                return "Kiosko/videos";
            if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".ppt" || ext == ".pptx" || ext == ".xls" || ext == ".xlsx")
                return "Kiosko/documentos";
            
            return "Kiosko/otros";
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo está vacío o es nulo");

            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            
            var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0,8)}";
            var cleanFileName = Regex.Replace(uniqueFileName, @"[^a-zA-Z0-9_\-\.]", "_");

            using var stream = file.OpenReadStream();
            
            var folder = GetFolderForExtension(extension);

            var autoUploadParams = new AutoUploadParams()
            {
                File = new FileDescription(cleanFileName + extension, stream),
                PublicId = $"{folder}/{cleanFileName}",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(autoUploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Error de Cloudinary: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadStreamAsync(Stream stream, string fileName)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("El stream está vacío o es nulo");

            var originalFileName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            
            var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0,8)}";
            var cleanFileName = Regex.Replace(uniqueFileName, @"[^a-zA-Z0-9_\-\.]", "_");

            var folder = GetFolderForExtension(extension);

            var autoUploadParams = new AutoUploadParams()
            {
                File = new FileDescription(cleanFileName + extension, stream),
                PublicId = $"{folder}/{cleanFileName}",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(autoUploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Error de Cloudinary: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("La URL del archivo es requerida.");

            // Extract PublicID from URL
            // Cloudinary URLs typically look like: https://res.cloudinary.com/<cloud_name>/<resource_type>/upload/v<version>/<folder>/<filename>.<ext>
            // For deleting, we need to extract the "folder/filename" part without extension.

            var uri = new Uri(fileUrl);
            var segments = uri.Segments;

            // Compatibilidad con los archivos migrados previamente en /evidencias/ y los nuevos en /Kiosko/
            int folderIndex = fileUrl.IndexOf("/Kiosko/");
            if (folderIndex == -1)
            {
                folderIndex = fileUrl.IndexOf("/evidencias/");
            }

            if (folderIndex == -1)
            {
                throw new ArgumentException("La URL no pertenece al storage de este proyecto de Cloudinary.");
            }

            // Extract the path after upload version /.../Kiosko/...
            string pathAfterFolder = fileUrl.Substring(folderIndex + 1); // e.g. "Kiosko/imagenes/archivo123.jpg"
            
            // For images/videos, Cloudinary removes extension for PublicId, but for raw files it keeps it.
            // Let's just remove extension as a standard assumption for AutoUpload:
            string publicId = Path.ChangeExtension(pathAfterFolder, null); 

            // Deletion needs to know the resource type (image, video, raw).
            // We can guess it from the URL:
            var resourceType = ResourceType.Image;
            if (fileUrl.Contains("/raw/")) resourceType = ResourceType.Raw;
            else if (fileUrl.Contains("/video/")) resourceType = ResourceType.Video;

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType
            };

            var destroyResult = await _cloudinary.DestroyAsync(deleteParams);

            if (destroyResult.Error != null)
            {
                throw new Exception($"Error al eliminar en Cloudinary: {destroyResult.Error.Message}");
            }
        }
    }
}
