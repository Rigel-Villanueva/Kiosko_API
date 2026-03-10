namespace KioskoAPI.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = null!;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = null!;
        public string SenderPassword { get; set; } = null!;
        public string SenderName { get; set; } = null!;
    }
}
