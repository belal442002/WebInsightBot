using Chatbot.Helper;
using Chatbot.Models;
using Chatbot.Services;
using Chatbot.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly string englishSystemMessage = @"You are a professional, knowledgeable chatbot for the Ministry of Environment and Climate Change. 
            Your role is to:
            1. Answer questions STRICTLY based on the provided content only
            2. Maintain a formal, professional tone
            3. Respond in English at all times
            4. If a question cannot be answered from the content, respond: 'I can only answer questions based on the provided environmental content.'
            5. For off-topic queries, politely decline to answer
            6. Structure responses clearly with proper formatting when needed

            Important: Never invent information or speculate beyond what's in the content.";

        private readonly string arabicSystemMessage = @"أنت مساعد ذكي محترف تابع لوزارة البيئة والتغير المناخي. 
            مهمتك هي:
            1. الإجابة على الأسئلة بناءً على المحتوى المقدم فقط
            2. الحفاظ على نغمة رسمية واحترافية
            3. الرد باللغة العربية حصراً
            4. إذا كان السؤال خارج نطاق المحتوى، قل: 'يمكنني الإجابة فقط على الأسئلة المتعلقة بالمحتوى البيئي المقدم'
            5. تجاهل أي أسئلة خارج الاختصاص
            6. تقديم الإجابات بشكل منظم وواضح مع استخدام التنسيق المناسب عند الحاجة

            هام: لا تختلق معلومات أو تجاوز ما ورد في المحتوى المقدم.";

        private readonly IFileService _fileService;
        private readonly IOpenAIService _openAIService;

        public ChatbotController(IFileService fileService, IOpenAIService openAIService)
        {
            _fileService = fileService;
            _openAIService = openAIService;
        }

        [HttpGet]
        [Route("Chatbot/ScrapedContent")]
        public async Task<IActionResult> Index()
        {
            var scrapedContent = string.Empty;
            try
            {
                scrapedContent = await _fileService.ReadScrapedContent();
            }catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var arabicContent = await _openAIService.TranslateContentToArabicAsync(scrapedContent);
            if(arabicContent == null)
            {
                return BadRequest("Can't translate to arabic content");
            }
            var scrapedContenViewModel = new ScrapedContentViewModel
            {
                EnglishContent = scrapedContent,
                ArabicContent = arabicContent
            };

            return View(scrapedContenViewModel);
        }

        [HttpPost]
        [Route("BuildChatbot")]
        public async Task<IActionResult> BuildChatbot(string content, string language)
        {
            var messages = new List<Message>();
            if (language.Equals(Languages.en.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new Message
                {
                    Content = englishSystemMessage,
                    Role = Roles.Assistant.ToString(),
                });

                await Task.Delay(1000);

                messages.Add(new Message
                {
                    Content = "The content: " + content,
                    Role = Roles.User.ToString(),
                });
            }

            else if (language.Equals(Languages.ar.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new Message
                {
                    Content = arabicSystemMessage,
                    Role = Roles.Assistant.ToString(),
                });

                await Task.Delay(1000);

                messages.Add(new Message
                {
                    Content = "The content: " + content,
                    Role = Roles.User.ToString(),
                });
            }

            else
                return BadRequest("Unknown language");

            return View("Chatbot", messages);
        }

        [HttpPost]
        [Route("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest chatRequest)
        {
            var botMessage = await _openAIService.GetChatCompletionAsync(chatRequest.Messages);

            return Json(new { botMessage = botMessage });
        }
    }
}
