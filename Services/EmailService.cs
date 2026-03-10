using KioskoAPI.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KioskoAPI.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Envía un correo con el código de verificación usando la API de Brevo (HTTPS).
        /// </summary>
        public async Task SendVerificationEmailAsync(string correoDestino, string codigo)
        {
            _logger.LogInformation("Intentando enviar correo de verificación a {correo} vía Brevo API", correoDestino);

            var requestBody = new
            {
                sender = new { name = _emailSettings.SenderName, email = _emailSettings.SenderEmail },
                to = new[] { new { email = correoDestino } },
                subject = "Kiosko Escolar - Código de Verificación",
                htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50; text-align: center;'>Kiosko Escolar</h2>
                        <p>Hola, gracias por registrarte en <strong>Kiosko Escolar</strong>.</p>
                        <p>Tu código de verificación es:</p>
                        <div style='background-color: #f0f0f0; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;'>
                            {codigo}
                        </div>
                        <p style='color: #7f8c8d; font-size: 14px;'>Este código expira en <strong>15 minutos</strong>.</p>
                        <p style='color: #7f8c8d; font-size: 12px;'>Si no solicitaste este código, ignora este correo.</p>
                    </div>"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Brevo usa header api-key en vez de Bearer
            _httpClient.DefaultRequestHeaders.Remove("api-key");
            _httpClient.DefaultRequestHeaders.Add("api-key", _emailSettings.BrevoApiKey);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Correo enviado exitosamente a {correo}. Response: {response}", correoDestino, responseBody);
                }
                else
                {
                    _logger.LogError("Brevo respondió con error {status}: {body}", response.StatusCode, responseBody);
                    throw new Exception($"Error al enviar correo: {responseBody}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Timeout al enviar correo a {correo} vía Brevo (15 segundos)", correoDestino);
                throw new Exception("El envío del correo tardó demasiado.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al enviar correo a {correo}: {mensaje}", correoDestino, ex.Message);
                throw;
            }
        }
    }
}
