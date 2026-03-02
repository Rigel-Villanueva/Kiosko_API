using KioskoAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace KioskoAPI.Services
{
    public class AuthService
    {
        private readonly UsuariosService _usuariosService;
        private readonly IConfiguration _configuration;

        public AuthService(UsuariosService usuariosService, IConfiguration configuration)
        {
            _usuariosService = usuariosService;
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(string correo, string password)
        {
            var usuario = await _usuariosService.GetByCorreoAsync(correo);

            if (usuario == null) return null;

            // Verificar la contraseña cifrada
            if (!BCrypt.Net.BCrypt.Verify(password, usuario.Password))
                return null;

            return GenerateJwtToken(usuario);
        }

        public async Task<Usuario> RegisterAsync(Usuario nuevoUsuario)
        {
            // Verificar si ya existe un usuario con ese correo
            var existente = await _usuariosService.GetByCorreoAsync(nuevoUsuario.Correo);
            if (existente != null)
                throw new Exception("Ya existe un usuario registrado con ese correo.");

            // Cifrar la contraseña antes de guardar en Mongo
            nuevoUsuario.Password = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.Password);
            
            // Si es estudiante o admin, o maestro, la verificación depende de reglas de negocio
            // Para asegurar seguridad, por defecto los verificamos a falso a menos que sea estudiante (no necesitan verificar mucho para subir)
            if (nuevoUsuario.Rol == "estudiante") nuevoUsuario.Verificado = true;
            else nuevoUsuario.Verificado = false;

            await _usuariosService.CreateAsync(nuevoUsuario);
            return nuevoUsuario;
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id!),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("Verificado", usuario.Verificado.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
