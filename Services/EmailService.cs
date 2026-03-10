using KioskoAPI.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace KioskoAPI.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Envía un correo con el código de verificación al usuario.
        /// Tiene un timeout de 10 segundos para evitar que se cuelgue.
        /// </summary>
        public async Task SendVerificationEmailAsync(string correoDestino, string codigo)
        {
            _logger.LogInformation("Intentando enviar correo de verificación a {correo}", correoDestino);

            var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
            {
                Port = _emailSettings.SmtpPort,
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = true,
                Timeout = 10000 // 10 segundos timeout
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = "Kiosko Escolar - Código de Verificación",
                IsBodyHtml = true,
                Body = $@"
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

            mailMessage.To.Add(correoDestino);

            // Enviar con timeout usando CancellationToken
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await smtpClient.SendMailAsync(mailMessage, cts.Token);
                _logger.LogInformation("Correo de verificación enviado exitosamente a {correo}", correoDestino);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Timeout al enviar correo a {correo} (10 segundos)", correoDestino);
                throw new Exception("El envío del correo tardó demasiado. Intenta con resend-code.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {correo}: {mensaje}", correoDestino, ex.Message);
                throw;
            }
        }
    }
}
