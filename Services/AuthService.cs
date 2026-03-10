using KioskoAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace KioskoAPI.Services
{
    public class AuthService
    {
        private readonly UsuariosService _usuariosService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(UsuariosService usuariosService, EmailService emailService, IConfiguration configuration)
        {
            _usuariosService = usuariosService;
            _emailService = emailService;
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
            // Validar formato de correo con regex
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailRegex.IsMatch(nuevoUsuario.Correo))
                throw new Exception("El formato del correo no es válido.");

            // Verificar si ya existe un usuario con ese correo
            var existente = await _usuariosService.GetByCorreoAsync(nuevoUsuario.Correo);
            if (existente != null)
                throw new Exception("Ya existe un usuario registrado con ese correo.");

            // Cifrar la contraseña antes de guardar en Mongo
            nuevoUsuario.Password = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.Password);

            // Verificado por admin: estudiantes sí, maestros/admin no
            if (nuevoUsuario.Rol == "estudiante") nuevoUsuario.Verificado = true;
            else nuevoUsuario.Verificado = false;

            // Correo no verificado hasta que ingrese el código
            nuevoUsuario.CorreoVerificado = false;

            // Generar código de verificación de 6 dígitos
            var codigo = new Random().Next(100000, 999999).ToString();
            nuevoUsuario.CodigoVerificacion = codigo;
            nuevoUsuario.CodigoExpiracion = DateTime.UtcNow.AddMinutes(15);

            await _usuariosService.CreateAsync(nuevoUsuario);

            // Enviar correo en segundo plano (no bloquea la respuesta)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendVerificationEmailAsync(nuevoUsuario.Correo, codigo);
                }
                catch (Exception)
                {
                    // El error se registra en los logs del EmailService
                    // El usuario puede usar resend-code para solicitar otro código
                }
            });

            return nuevoUsuario;
        }

        /// <summary>
        /// Verifica el correo electrónico del usuario con el código de 6 dígitos.
        /// </summary>
        public async Task<bool> VerifyEmailAsync(string correo, string codigo)
        {
            var usuario = await _usuariosService.GetByCorreoAsync(correo);

            if (usuario == null)
                throw new Exception("No se encontró un usuario con ese correo.");

            if (usuario.CorreoVerificado)
                throw new Exception("Este correo ya fue verificado.");

            if (usuario.CodigoVerificacion == null || usuario.CodigoExpiracion == null)
                throw new Exception("No hay un código de verificación pendiente. Usa resend-code.");

            if (usuario.CodigoExpiracion < DateTime.UtcNow)
                throw new Exception("El código de verificación ha expirado. Usa resend-code para obtener uno nuevo.");

            if (usuario.CodigoVerificacion != codigo)
                throw new Exception("El código de verificación es incorrecto.");

            // Verificar correo exitosamente
            usuario.CorreoVerificado = true;
            usuario.CodigoVerificacion = null;
            usuario.CodigoExpiracion = null;

            await _usuariosService.UpdateAsync(usuario.Id!, usuario);
            return true;
        }

        /// <summary>
        /// Reenvía un nuevo código de verificación al correo del usuario.
        /// </summary>
        public async Task ResendCodeAsync(string correo)
        {
            var usuario = await _usuariosService.GetByCorreoAsync(correo);

            if (usuario == null)
                throw new Exception("No se encontró un usuario con ese correo.");

            if (usuario.CorreoVerificado)
                throw new Exception("Este correo ya fue verificado.");

            // Generar nuevo código
            var codigo = new Random().Next(100000, 999999).ToString();
            usuario.CodigoVerificacion = codigo;
            usuario.CodigoExpiracion = DateTime.UtcNow.AddMinutes(15);

            await _usuariosService.UpdateAsync(usuario.Id!, usuario);

            await _emailService.SendVerificationEmailAsync(correo, codigo);
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
                new Claim("Verificado", usuario.Verificado.ToString()),
                new Claim("CorreoVerificado", usuario.CorreoVerificado.ToString())
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
