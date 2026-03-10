using KioskoAPI.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace KioskoAPI.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Envía un correo con el código de verificación al usuario.
        /// </summary>
        public async Task SendVerificationEmailAsync(string correoDestino, string codigo)
        {
            var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
            {
                Port = _emailSettings.SmtpPort,
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                EnableSsl = true,
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

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
