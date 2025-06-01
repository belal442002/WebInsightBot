using Chatbot.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebScrapingService _webScrapingService;
        private readonly IOpenAIService _openAIService;
        private readonly IFileService _fileService;

        public HomeController(IWebScrapingService webScrapingService, IOpenAIService openAIService, IFileService fileService)
        {
            _webScrapingService = webScrapingService;
            _openAIService = openAIService;
            _fileService = fileService;
        }

        [HttpGet]
        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("ScrapeContent")]
        public IActionResult ScrapeContent()
        {
            return View();
        }

        [HttpPost]
        [Route("ExtractContent")]
        public async Task<IActionResult> ExtractContent(string websiteUrl)
        {
            var content = await _webScrapingService.CrawlAndExtractContentWithLinkFilteringAsync("https://www.mecc.gov.qa/english/pages/default.aspx");

            var jsonContent = string.Empty;
            foreach (var item in content)
            {
                jsonContent += (("URL: " + item.Key) + ("\nPage Content: " + item.Value + "\n"));
            }

            var structuredContent = await _openAIService.GetStructuredScrapedContent(jsonContent);

            try
            {
                await _fileService.File_WriteAllTextAsync(structuredContent);
            }catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return View("ExtractedContent", structuredContent);
        }
    }
}
