using KioskoAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KioskoAPI.Services
{
    public class ProyectosService
    {
        private readonly IMongoCollection<Proyecto> _proyectosCollection;

        public ProyectosService(IOptions<KioskoDatabaseSettings> kioskoDatabaseSettings)
        {
            var mongoClient = new MongoClient(kioskoDatabaseSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(kioskoDatabaseSettings.Value.DatabaseName);
            _proyectosCollection = mongoDatabase.GetCollection<Proyecto>(kioskoDatabaseSettings.Value.ProjectsCollectionName);
        }

        public async Task<List<Proyecto>> GetAsync()
        {
            var proyectos = await _proyectosCollection.Find(_ => true).ToListAsync();
            proyectos.ForEach(SortVideos);
            return proyectos;
        }

        public async Task<List<Proyecto>> GetAprobadosAsync(string? nombre = null, string? autor = null, double? minCal = null, double? maxCal = null)
        {
            var builder = Builders<Proyecto>.Filter;
            // Solo traer proyectos aprobados o evaluados
            var filter = builder.In(x => x.Estatus, new[] { "aprobado", "evaluado" });

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                // Búsqueda insensible a mayúsculas/minúsculas
                filter &= builder.Regex(x => x.Nombre, new MongoDB.Bson.BsonRegularExpression(nombre, "i"));
            }

            if (!string.IsNullOrWhiteSpace(autor))
            {
                // Buscar coincidencia en el array de autores_correos
                filter &= builder.Regex("autores_correos", new MongoDB.Bson.BsonRegularExpression(autor, "i"));
            }

            if (minCal.HasValue)
            {
                filter &= builder.Gte(x => x.PromedioGeneral, minCal.Value);
            }

            if (maxCal.HasValue)
            {
                filter &= builder.Lte(x => x.PromedioGeneral, maxCal.Value);
            }

            var proyectos = await _proyectosCollection.Find(filter).ToListAsync();
            proyectos.ForEach(SortVideos);
            return proyectos;
        }

        public async Task<List<Proyecto>> GetMisProyectosAsync(string correo)
        {
            var proyectos = await _proyectosCollection.Find(x => x.AutoresCorreos.Contains(correo)).ToListAsync();
            proyectos.ForEach(SortVideos);
            return proyectos;
        }

        public async Task<Proyecto?> GetAsync(string id)
        {
            var proyecto = await _proyectosCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (proyecto != null) SortVideos(proyecto);
            return proyecto;
        }

        public async Task CreateAsync(Proyecto nuevoProyecto) =>
            await _proyectosCollection.InsertOneAsync(nuevoProyecto);

        public async Task UpdateAsync(string id, Proyecto updatedProyecto) =>
            await _proyectosCollection.ReplaceOneAsync(x => x.Id == id, updatedProyecto);

        public async Task AprobarProyectoAsync(string id)
        {
            var filter = Builders<Proyecto>.Filter.Eq(p => p.Id, id);
            var update = Builders<Proyecto>.Update.Set(p => p.Estatus, "aprobado");
            await _proyectosCollection.UpdateOneAsync(filter, update);
        }

        public async Task RemoveAsync(string id) =>
            await _proyectosCollection.DeleteOneAsync(x => x.Id == id);

        private void SortVideos(Proyecto proyecto)
        {
            if (proyecto.Evidencias?.Videos != null && proyecto.Evidencias.Videos.Count > 1)
            {
                proyecto.Evidencias.Videos = proyecto.Evidencias.Videos.OrderBy(v => 
                {
                    var title = v.Titulo?.ToLower() ?? "";
                    if (title == "intro") return 1;
                    if (title == "pitch") return 2;
                    return 3;
                }).ToList();
            }
        }
    }
}
