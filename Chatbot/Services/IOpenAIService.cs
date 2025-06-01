using Chatbot.Models;

namespace Chatbot.Services
{
    public interface IOpenAIService
    {
        Task<string> GetChatCompletionAsync(List<Message> messages);
        Task<string> GetStructuredScrapedContent(string scrapedContent);
        Task<string> TranslateContentToArabicAsync(string englishContent);
    }
}
