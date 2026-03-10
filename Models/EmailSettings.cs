namespace KioskoAPI.Models
{
    public class EmailSettings
    {
        public string BrevoApiKey { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string SenderName { get; set; } = null!;
    }
}
