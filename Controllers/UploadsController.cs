using KioskoAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace KioskoAPI.Controllers
{
    /// <summary>
    /// Manejador de Archivos: Permite la carga de evidencias físicas (Fotos, PDFs, Word, PPT) hacia Supabase Storage.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;

        public UploadsController(SupabaseStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Sube un archivo real a la nube (Supabase Storage).
        /// </summary>
        /// <param name="requestFile">El archivo físico subido a través de multipart/form-data</param>
        /// <returns>La URL pública lista para ser adjuntada al JSON del Proyecto</returns>
        [HttpPost]
        [Authorize(Roles = "estudiante,admin")] // Estudiantes y admins pueden subir archivos de proyecto
        [RequestSizeLimit(150 * 1024 * 1024)] // 150 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 150 * 1024 * 1024)] // 150 MB
        public async Task<IActionResult> UploadFile(IFormFile requestFile)
        {
            try
            {
                if (requestFile == null || requestFile.Length == 0)
                {
                    return BadRequest(new { mensaje = "No se proporcionó ningún archivo o el archivo está vacío." });
                }

                // Llamamos a nuestro servicio de Supabase
                string publicUrl = await _storageService.UploadFileAsync(requestFile);

                // Devolvemos la URL pública recién generada para que el Frontend la guarde en el JSON del Proyecto
                return Ok(new { url = publicUrl });
            }
            catch (Exception ex)
            {
                // Envolvemos en un 500 para atrapar errores de red o permisos de Supabase
                return StatusCode(500, new { mensaje = "Error al subir el archivo a Storage", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un archivo del Storage de Supabase usando su URL pública.
        /// </summary>
        /// <param name="url">La URL pública del archivo a eliminar</param>
        [HttpDelete]
        [Authorize(Roles = "estudiante,admin")]
        public async Task<IActionResult> DeleteFile([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest(new { mensaje = "Se requiere la URL del archivo a eliminar." });
                }

                await _storageService.DeleteFileAsync(url);

                return Ok(new { mensaje = "Archivo eliminado correctamente" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar el archivo de Storage", detalle = ex.Message });
            }
        }
    }
}
