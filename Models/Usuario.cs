using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace KioskoAPI.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; } = null!;

        [BsonElement("correo")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; } = null!;

        [BsonElement("rol")]
        public string Rol { get; set; } = null!; // "estudiante", "maestro", "admin"

        [BsonElement("verificado")]
        public bool Verificado { get; set; } = false;

        [BsonElement("proyectos_por_calificar")]
        public List<string> ProyectosPorCalificar { get; set; } = new();

        [BsonElement("password")]
        public string Password { get; set; } = null!; // Contraseña encriptada
    }
}
