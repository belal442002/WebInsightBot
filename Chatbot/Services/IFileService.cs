namespace Chatbot.Services
{
    public interface IFileService
    {
        Task File_WriteAllTextAsync(string scrapedContent);
        Task<string> ReadScrapedContent();
    }
}
