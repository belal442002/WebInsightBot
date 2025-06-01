namespace Chatbot.Services
{
    public interface IWebScrapingService
    {
        Task<string> ScrapeWebsiteContentAsync(string url, int maxDepth = 2, int maxPages = 50);
        Task<string> GetCleanContent(string url);
        Task<string> ExtractSharePointContent(string url);
        Task<Dictionary<string, string>> CrawlAndExtractContentAsync(string baseUrl, int maxDepth = 1, int maxPages = 20);
        Task<Dictionary<string, string>> CrawlAndExtractContentWithLinkFilteringAsync(string baseUrl, int maxDepth = 1, int maxPages = 20);
        //Task<string> ScrapeWithBrowserAsync(string url);
    }
}
