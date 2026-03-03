using Supabase;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace KioskoAPI.Services
{
    public class SupabaseStorageService
    {
        private readonly Client _supabaseClient;
        private readonly string _supabaseUrl;
        private readonly string _bucketName = "evidencias"; // El nombre exacto del bucket que creaste

        public SupabaseStorageService(IConfiguration configuration)
        {
            var url = configuration["Supabase:Url"];
            var key = configuration["Supabase:Key"];

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            
            _supabaseUrl = url!;
            _supabaseClient = new Client(url, key, options);
            _supabaseClient.InitializeAsync().Wait();
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo está vacío o es nulo");

            // Limpiar el nombre del archivo para evitar caracteres raros en la URL
            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName);
            
            // Generar un nombre único basado en la fecha y un guid para que no se sobreescriban
            var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0,8)}{extension}";
            var cleanFileName = Regex.Replace(uniqueFileName, @"[^a-zA-Z0-9_\-\.]", "_");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var bytes = stream.ToArray();

            // Subir el arreglo de bytes al Storage de Supabase
            await _supabaseClient.Storage
                .From(_bucketName)
                .Upload(bytes, cleanFileName, new Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = false });

            // Construir la URL pública manualmente
            // La estructura típica de Supabase es: {URL_BASE}/storage/v1/object/public/{BUCKET}/{NOMBRE_ARCHIVO}
            var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{cleanFileName}";

            return publicUrl;
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("La URL del archivo es requerida.");

            // Extraer el nombre del archivo de la URL pública
            // URL formato: {URL_BASE}/storage/v1/object/public/{BUCKET}/{NOMBRE_ARCHIVO}
            var prefix = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/";
            if (!fileUrl.StartsWith(prefix))
                throw new ArgumentException("La URL no pertenece al storage de este proyecto.");

            var fileName = fileUrl.Substring(prefix.Length);

            await _supabaseClient.Storage
                .From(_bucketName)
                .Remove(new List<string> { fileName });
        }
    }
}
