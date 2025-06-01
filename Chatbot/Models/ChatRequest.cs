namespace Chatbot.Models
{
    public class ChatRequest
    {
        public string UserMessage { get; set; }
        public List<Message> Messages { get; set; }
    }
}
