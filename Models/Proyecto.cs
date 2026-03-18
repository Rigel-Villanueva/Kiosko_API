using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KioskoAPI.Models
{
    [BsonIgnoreExtraElements]
    public class Proyecto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; } = null!;

        [BsonElement("descripcion")]
        public string Descripcion { get; set; } = null!;

        [BsonElement("subido_por")]
        public string? SubidoPor { get; set; }

        [BsonElement("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [BsonElement("autores_correos")]
        public List<string> AutoresCorreos { get; set; } = new();

        [BsonElement("estatus")]
        public string Estatus { get; set; } = "pendiente"; // "pendiente", "aprobado", "rechazado", "evaluado"

        [BsonElement("promedio_general")]
        public double PromedioGeneral { get; set; } = 0;

        [BsonElement("evidencias")]
        public Evidencias? Evidencias { get; set; }

        [BsonElement("evaluaciones_docentes")]
        public List<EvaluacionDocente> EvaluacionesDocentes { get; set; } = new();
    }

    [BsonIgnoreExtraElements]
    public class Evidencias
    {
        [BsonElement("repositorio_git")]
        public string? RepositorioGit { get; set; }

        [BsonElement("videos")]
        public List<Video> Videos { get; set; } = new();

        [BsonElement("imagenes")]
        public List<string> Imagenes { get; set; } = new();

        [BsonElement("documentos_pdf")]
        public List<string> DocumentosPdf { get; set; } = new();

        [BsonElement("diapositivas")]
        public string? Diapositivas { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Video
    {
        [BsonElement("titulo")]
        public string Titulo { get; set; } = null!;

        [BsonElement("url")]
        public string Url { get; set; } = null!;
    }

    [BsonIgnoreExtraElements]
    public class EvaluacionDocente
    {
        [BsonElement("maestro_id")]
        public string? MaestroId { get; set; }

        [BsonElement("nombre_maestro")]
        public string? NombreMaestro { get; set; }

        [BsonElement("fecha_evaluacion")]
        public DateTime? FechaEvaluacion { get; set; }

        [BsonElement("promedio_por_maestro")]
        public double PromedioPorMaestro { get; set; }

        [BsonElement("retroalimentacion_final")]
        public string? RetroalimentacionFinal { get; set; }

        [BsonElement("rubrica")]
        public List<CriterioRubrica> Rubrica { get; set; } = new();
    }

    [BsonIgnoreExtraElements]
    public class CriterioRubrica
    {
        [BsonElement("criterio")]
        public string Criterio { get; set; } = null!;

        [BsonElement("puntos_max")]
        public double PuntosMax { get; set; }

        [BsonElement("obtenido")]
        public double Obtenido { get; set; }

        [BsonElement("obs")]
        public string? Obs { get; set; }
    }
}
