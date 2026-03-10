using KioskoAPI.Models;
using KioskoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace KioskoAPI.Controllers
{
    /// <summary>
    /// Autenticación: Endpoints para registrar cuentas, iniciar sesión y verificar correo electrónico.
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
        /// Se envía un código de verificación al correo proporcionado.
        /// </summary>
        /// <param name="nuevoUsuario">JSON con nombre, correo, password y rol</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Usuario nuevoUsuario)
        {
            try
            {
                var usuario = await _authService.RegisterAsync(nuevoUsuario);
                return CreatedAtAction(nameof(Register), new { id = usuario.Id }, new
                {
                    mensaje = "Usuario registrado exitosamente. Revisa tu correo para verificar tu cuenta.",
                    usuario = new
                    {
                        usuario.Id,
                        usuario.Nombre,
                        usuario.Correo,
                        usuario.Rol,
                        usuario.Verificado,
                        usuario.CorreoVerificado
                    }
                });
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

        /// <summary>
        /// Verifica el correo electrónico del usuario con el código de 6 dígitos enviado al correo.
        /// </summary>
        /// <param name="request">JSON con correo y codigo</param>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                await _authService.VerifyEmailAsync(request.Correo, request.Codigo);
                return Ok(new { mensaje = "Correo verificado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reenvía un nuevo código de verificación al correo del usuario.
        /// </summary>
        /// <param name="request">JSON con correo</param>
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ResendCodeRequest request)
        {
            try
            {
                await _authService.ResendCodeAsync(request.Correo);
                return Ok(new { mensaje = "Se envió un nuevo código de verificación a tu correo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // DTOs para los endpoints de Auth
    public class LoginRequest
    {
        public string Correo { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class VerifyEmailRequest
    {
        public string Correo { get; set; } = null!;
        public string Codigo { get; set; } = null!;
    }

    public class ResendCodeRequest
    {
        public string Correo { get; set; } = null!;
    }
}
