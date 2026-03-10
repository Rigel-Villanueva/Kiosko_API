namespace KioskoAPI.Models
{
    public class EmailSettings
    {
        public string ResendApiKey { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string SenderName { get; set; } = null!;
    }
}
