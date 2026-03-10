using KioskoAPI.Models;
using KioskoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace KioskoAPI.Controllers
{
    /// <summary>
    /// Autenticación: Endpoints para registrar cuentas y generar tokens de inicio de sesión JWT.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Crea una nueva cuenta de usuario (Estudiante, Maestro o Admin).
        /// </summary>
        /// <param name="nuevoUsuario">JSON con nombre, correo, password y rol</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Usuario nuevoUsuario)
        {
            try
            {
                var usuario = await _authService.RegisterAsync(nuevoUsuario);
                return CreatedAtAction(nameof(Register), new { id = usuario.Id }, new { mensaje = "Usuario registrado exitosamente", usuario });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Inicia sesión validando el correo y la contraseña. Retorna un Token JWT válido por 24 horas.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.LoginAsync(request.Correo, request.Password);

            if (token == null)
                return Unauthorized(new { error = "Correo o contraseña incorrectos" });

            return Ok(new { Token = token });
        }
    }

    // DTO simple para recibir el login body
    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
