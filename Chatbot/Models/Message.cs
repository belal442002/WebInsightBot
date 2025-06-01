namespace Chatbot.Models
{
    public class Message
    {
        public string Content { get; set; }
        public string Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
