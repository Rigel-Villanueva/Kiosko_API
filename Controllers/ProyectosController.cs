using KioskoAPI.Models;
using KioskoAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KioskoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProyectosController : ControllerBase
    {
        private readonly ProyectosService _proyectosService;

        public ProyectosController(ProyectosService proyectosService)
        {
            _proyectosService = proyectosService;
        }

        /// <summary>
        /// Obtiene la lista pública de proyectos (solo los aprobados o evaluados). Soporta filtrado y búsqueda.
        /// </summary>
        /// <param name="nombre">Opcional. Busca coincidencias en el título del proyecto.</param>
        /// <param name="autor">Opcional. Busca por correo de algún autor.</param>
        /// <param name="minCal">Opcional. Calificación general mínima (ej: 80).</param>
        /// <param name="maxCal">Opcional. Calificación general máxima (ej: 100).</param>
        /// <returns>Una lista de proyectos que cumplen con los filtros.</returns>
        [HttpGet]
        public async Task<List<Proyecto>> GetAprobados(
            [FromQuery] string? nombre = null,
            [FromQuery] string? autor = null,
            [FromQuery] double? minCal = null,
            [FromQuery] double? maxCal = null)
        {
            return await _proyectosService.GetAprobadosAsync(nombre, autor, minCal, maxCal);
        }

        /// <summary>
        /// Solo Admin. Obtiene TODOS los proyectos, incluyendo los pendientes o rechazados.
        /// </summary>
        [HttpGet("todos")]
        [Authorize(Roles = "admin")]
        public async Task<List<Proyecto>> GetAll() =>
            await _proyectosService.GetAsync();

        // GET: api/Proyectos/mis-proyectos (Ver mis propios proyectos, solo logueados)
        [HttpGet("mis-proyectos")]
        [Authorize]
        public async Task<List<Proyecto>> GetMisProyectos()
        {
            var correo = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(correo)) return new List<Proyecto>();

            return await _proyectosService.GetMisProyectosAsync(correo);
        }

        // GET: api/Proyectos/{id} (Público, ver detalles de uno aprobado)
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Proyecto>> Get(string id)
        {
            var proyecto = await _proyectosService.GetAsync(id);
            if (proyecto is null) return NotFound();
            return proyecto;
        }

        // POST: api/Proyectos (Solo estudiantes pueden subir)
        [HttpPost]
        [Authorize(Roles = "estudiante")]
        public async Task<IActionResult> Post([FromBody] Proyecto nuevoProyecto)
        {
            // Setear el ID del creador leyendo el token JWT
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var correo = User.FindFirst(ClaimTypes.Email)?.Value;

            if (userId == null || correo == null) return Unauthorized();

            nuevoProyecto.SubidoPor = userId;
            nuevoProyecto.Estatus = "pendiente"; // Obligamos a que inicie pendiente
            nuevoProyecto.FechaCreacion = DateTime.UtcNow;

            // Aseguramos que el creador esté en la lista de autores
            if (!nuevoProyecto.AutoresCorreos.Contains(correo))
            {
                nuevoProyecto.AutoresCorreos.Add(correo);
            }

            // Validar tipos de video
            var errorVideo = ValidarTiposVideo(nuevoProyecto);
            if (errorVideo != null) return BadRequest(new { error = errorVideo });

            await _proyectosService.CreateAsync(nuevoProyecto);

            return CreatedAtAction(nameof(Get), new { id = nuevoProyecto.Id }, nuevoProyecto);
        }

        // PUT: api/Proyectos/{id}/aprobar (Solo Admin aprueba)
        [HttpPut("{id:length(24)}/aprobar")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Aprobar(string id)
        {
            var proyecto = await _proyectosService.GetAsync(id);
            if (proyecto is null) return NotFound();

            await _proyectosService.AprobarProyectoAsync(id);
            return Ok(new { mensaje = "Proyecto aprobado y publicado en el Kiosko" });
        }

        // DELETE: api/Proyectos/{id} (Admin o el propio estudiante que lo subió)
        [HttpDelete("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var proyecto = await _proyectosService.GetAsync(id);
            if (proyecto is null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "admin" && proyecto.SubidoPor != userId)
            {
                return Forbid(); // No eres admin y no subiste tú este proyecto
            }

            await _proyectosService.RemoveAsync(id);
            return NoContent();
        }

        // PUT: api/Proyectos/{id} (Actualizar detalles del proyecto)
        [HttpPut("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] Proyecto updatedProyecto)
        {
            var proyecto = await _proyectosService.GetAsync(id);
            if (proyecto is null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Solo puede editar el admin o el creador del proyecto
            if (userRole != "admin" && proyecto.SubidoPor != userId)
            {
                return Forbid();
            }

            // Mantenemos algunos datos críticos para que no los sobrescriban accidentalmente
            updatedProyecto.Id = proyecto.Id;
            updatedProyecto.SubidoPor = proyecto.SubidoPor;
            updatedProyecto.FechaCreacion = proyecto.FechaCreacion;
            
            // Si lo edita el estudiante, lo devolvemos a estado pendiente (opcional, dependiendo de tu regla de negocio)
            // Si no quieres requerir una re-aprobación, quita esta línea.
            if (userRole != "admin")
            {
                updatedProyecto.Estatus = "pendiente";
            }
            else
            {
                // Si es admin, respetamos el estatus que haya mandado
                updatedProyecto.Estatus = updatedProyecto.Estatus ?? proyecto.Estatus; 
            }

            // Validar tipos de video
            var errorVideo = ValidarTiposVideo(updatedProyecto);
            if (errorVideo != null) return BadRequest(new { error = errorVideo });

            await _proyectosService.UpdateAsync(id, updatedProyecto);

            return NoContent();
        }

        // PUT: api/Proyectos/{id}/evaluar (Solo Maestro evalúa)
        [HttpPut("{id:length(24)}/evaluar")]
        [Authorize(Roles = "maestro")]
        public async Task<IActionResult> EvaluarProyecto(string id, [FromBody] EvaluacionDocente evaluacion)
        {
            var proyecto = await _proyectosService.GetAsync(id);
            if (proyecto is null) return NotFound();

            var maestroId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var nombreMaestro = User.FindFirst(ClaimTypes.Name)?.Value;
            var verificadoClaim = User.FindFirst("Verificado")?.Value;

            if (maestroId == null || nombreMaestro == null) return Unauthorized();
            
            // Verificar si el maestro está verificado por el admin
            if (verificadoClaim != "True" && verificadoClaim != "true")
            {
                return Forbid(); // "No puedes evaluar si un admin no te ha verificado"
            }

            // Forzamos los datos del maestro desde el token por seguridad
            evaluacion.MaestroId = maestroId;
            evaluacion.NombreMaestro = nombreMaestro;
            evaluacion.FechaEvaluacion = DateTime.UtcNow;

            // Recalculamos la calificación total que dio este maestro sumando los puntos obtenidos en la rúbrica
            evaluacion.PromedioPorMaestro = evaluacion.Rubrica.Sum(r => r.Obtenido);

            // Buscar si este maestro ya había evaluado este proyecto antes para actualizar, o si es nueva
            var index = proyecto.EvaluacionesDocentes.FindIndex(e => e.MaestroId == maestroId);
            if (index != -1)
            {
                proyecto.EvaluacionesDocentes[index] = evaluacion;
            }
            else
            {
                proyecto.EvaluacionesDocentes.Add(evaluacion);
            }

            // Recalcular el promedio general del proyecto basado en TODOS los maestros que lo han evaluado
            if (proyecto.EvaluacionesDocentes.Count > 0)
            {
                 proyecto.PromedioGeneral = proyecto.EvaluacionesDocentes.Average(e => e.PromedioPorMaestro);
                 // Cambiamos el estatus a evaluado
                 proyecto.Estatus = "evaluado";
            }

            await _proyectosService.UpdateAsync(id, proyecto);

            return Ok(new { mensaje = "Evaluación enviada con éxito", promedioGeneral = proyecto.PromedioGeneral });
        }

        /// <summary>
        /// Valida que los tipos de video sean válidos y que no haya duplicados de intro o pitch.
        /// </summary>
        private string? ValidarTiposVideo(Proyecto proyecto)
        {
            if (proyecto.Evidencias?.Videos == null || proyecto.Evidencias.Videos.Count == 0)
                return null;

            var tiposValidos = new[] { "intro", "pitch", "demo", "otro" };

            foreach (var video in proyecto.Evidencias.Videos)
            {
                if (!tiposValidos.Contains(video.Tipo.ToLower()))
                    return $"Tipo de video '{video.Tipo}' no es válido. Usa: intro, pitch, demo u otro.";
            }

            // No puede haber más de 1 intro y más de 1 pitch
            var intros = proyecto.Evidencias.Videos.Count(v => v.Tipo.ToLower() == "intro");
            var pitches = proyecto.Evidencias.Videos.Count(v => v.Tipo.ToLower() == "pitch");

            if (intros > 1)
                return "Solo puede haber 1 video de tipo 'intro' por proyecto.";
            if (pitches > 1)
                return "Solo puede haber 1 video de tipo 'pitch' por proyecto.";

            return null;
        }
    }
}
