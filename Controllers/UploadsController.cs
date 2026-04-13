using KioskoAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace KioskoAPI.Controllers
{
    /// <summary>
    /// Manejador de Archivos: Permite la carga de evidencias físicas (Fotos, PDFs, Word, PPT) hacia Cloudinary.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly CloudinaryStorageService _storageService;
        private readonly ProyectosService _proyectosService;

        public UploadsController(CloudinaryStorageService storageService, ProyectosService proyectosService)
        {
            _storageService = storageService;
            _proyectosService = proyectosService;
        }

        /// <summary>
        /// Sube un archivo real a la nube (Cloudinary).
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

                // Llamamos a nuestro servicio de Cloudinary
                string publicUrl = await _storageService.UploadFileAsync(requestFile);

                // Devolvemos la URL pública recién generada para que el Frontend la guarde en el JSON del Proyecto
                return Ok(new { url = publicUrl });
            }
            catch (Exception ex)
            {
                // Envolvemos en un 500 para atrapar errores de red o permisos de Cloudinary
                return StatusCode(500, new { mensaje = "Error al subir el archivo a Storage", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un archivo de Cloudinary usando su URL pública.
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

        /// <summary>
        /// Endpoint temporal para migrar todos los archivos de Supabase a Cloudinary y actualizar Mongo.
        /// </summary>
        [HttpPost("migrate")]
        [AllowAnonymous]
        public async Task<IActionResult> MigrateSupabaseToCloudinary()
        {
            try
            {
                var proyectos = await _proyectosService.GetAsync();
                int totalMigrados = 0;
                int proyectosModificados = 0;
                int archivosFallidos = 0;

                using var httpClient = new HttpClient();

                foreach (var p in proyectos)
                {
                    bool modificado = false;

                    if (p.Evidencias != null)
                    {
                        // Migrar Videos
                        if (p.Evidencias.Videos != null)
                        {
                            foreach (var video in p.Evidencias.Videos)
                            {
                                if (video.Url.Contains("kurjiyahttouerwjoddm.supabase.co"))
                                {
                                    try 
                                    {
                                        var uri = new Uri(video.Url);
                                        var fileName = Path.GetFileName(uri.LocalPath);
                                        using var stream = await httpClient.GetStreamAsync(video.Url);
                                        var newUrl = await _storageService.UploadStreamAsync(stream, fileName);
                                        video.Url = newUrl;
                                        totalMigrados++;
                                        modificado = true;
                                    }
                                    catch (HttpRequestException ex)
                                    {
                                        Console.WriteLine($"Error descargando {video.Url}: {ex.Message}");
                                        archivosFallidos++;
                                    }
                                }
                            }
                        }

                        // Migrar Imagenes
                        if (p.Evidencias.Imagenes != null)
                        {
                            for (int i = 0; i < p.Evidencias.Imagenes.Count; i++)
                            {
                                if (p.Evidencias.Imagenes[i].Contains("kurjiyahttouerwjoddm.supabase.co"))
                                {
                                    try 
                                    {
                                        var uri = new Uri(p.Evidencias.Imagenes[i]);
                                        var fileName = Path.GetFileName(uri.LocalPath);
                                        using var stream = await httpClient.GetStreamAsync(p.Evidencias.Imagenes[i]);
                                        var newUrl = await _storageService.UploadStreamAsync(stream, fileName);
                                        p.Evidencias.Imagenes[i] = newUrl;
                                        totalMigrados++;
                                        modificado = true;
                                    }
                                    catch (HttpRequestException ex)
                                    {
                                        Console.WriteLine($"Error descargando {p.Evidencias.Imagenes[i]}: {ex.Message}");
                                        archivosFallidos++;
                                    }
                                }
                            }
                        }

                        // Migrar Documentos PDF
                        if (p.Evidencias.DocumentosPdf != null)
                        {
                            for (int i = 0; i < p.Evidencias.DocumentosPdf.Count; i++)
                            {
                                if (p.Evidencias.DocumentosPdf[i].Contains("kurjiyahttouerwjoddm.supabase.co"))
                                {
                                    try 
                                    {
                                        var uri = new Uri(p.Evidencias.DocumentosPdf[i]);
                                        var fileName = Path.GetFileName(uri.LocalPath);
                                        using var stream = await httpClient.GetStreamAsync(p.Evidencias.DocumentosPdf[i]);
                                        var newUrl = await _storageService.UploadStreamAsync(stream, fileName);
                                        p.Evidencias.DocumentosPdf[i] = newUrl;
                                        totalMigrados++;
                                        modificado = true;
                                    }
                                    catch (HttpRequestException ex)
                                    {
                                        Console.WriteLine($"Error descargando {p.Evidencias.DocumentosPdf[i]}: {ex.Message}");
                                        archivosFallidos++;
                                    }
                                }
                            }
                        }

                        // Migrar Diapositivas
                        if (!string.IsNullOrEmpty(p.Evidencias.Diapositivas) && p.Evidencias.Diapositivas.Contains("kurjiyahttouerwjoddm.supabase.co"))
                        {
                            try 
                            {
                                var uri = new Uri(p.Evidencias.Diapositivas);
                                var fileName = Path.GetFileName(uri.LocalPath);
                                using var stream = await httpClient.GetStreamAsync(p.Evidencias.Diapositivas);
                                var newUrl = await _storageService.UploadStreamAsync(stream, fileName);
                                p.Evidencias.Diapositivas = newUrl;
                                totalMigrados++;
                                modificado = true;
                            }
                            catch (HttpRequestException ex)
                            {
                                Console.WriteLine($"Error descargando {p.Evidencias.Diapositivas}: {ex.Message}");
                                archivosFallidos++;
                            }
                        }

                        if (modificado)
                        {
                            await _proyectosService.UpdateAsync(p.Id!, p);
                            proyectosModificados++;
                        }
                    }
                }

                return Ok(new
                {
                    mensaje = "Proceso terminado.",
                    archivosMigrados = totalMigrados,
                    archivosFallidos = archivosFallidos,
                    proyectosAfectados = proyectosModificados,
                    aviso = archivosFallidos > 0 ? "Algunos archivos no se pudieron descargar (ej. base de datos Supabase inactiva o error 402)." : "Todos descargados correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error durante la migración", detalle = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}
