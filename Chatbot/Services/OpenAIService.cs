
using Azure.AI.OpenAI;
using Chatbot.Helper;
using Chatbot.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace Chatbot.Services
{
    public class OpenAIService : IOpenAIService
    {
        private const string _SystemPrompt = """
            You are an AI trained to extract structured information from scraped website content. 
            Return ONLY a JSON object with the following schema:

            {
              "news": [
                {
                  "title": "News headline",
                  "date": "DD MMM YYYY",
                  "summary": "Brief description"
                }
              ],
              "statistics": [
                {
                  "metric": "Metric name",
                  "value": "Metric value"
                }
              ],
              "initiatives": [
                {
                  "name": "Project name",
                  "description": "Project details"
                }
              ]
            }

            Ignore irrelevant text (e.g., "Next123456", "en-US", UI elements).
            """;

        private readonly string _systemMessage = """
            You are a professional government document translator specialized in translating 
            English to Modern Standard Arabic for Qatar's Ministry of Environment and Climate Change.

            Translation Guidelines:
            1. Use formal Modern Standard Arabic (MSA/Fusha)
            2. Maintain exact technical/environmental terminology
            3. Preserve numbers, dates, and proper names
            4. Keep government agency names in original English (e.g., 'MECC' remains 'MECC')
            5. Use Arabic measurement units when applicable (e.g., 'km' → 'كم')
            6. Maintain formal tone suitable for government communications
            7. Translate environmental terms precisely:
               - 'climate change' → 'تغير المناخ'
               - 'sustainability' → 'الاستدامة'
               - 'biodiversity' → 'التنوع البيولوجي'

            Output Requirements:
            - Return ONLY the Arabic translation
            - No explanatory notes
            - No additional formatting
            """;

        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly string _location;

        public OpenAIService(IConfiguration configuration)
        {
            _configuration = configuration;
            var AzureOpenAIConfig = _configuration.GetSection("AzureOpenAI");
            _apiKey = AzureOpenAIConfig["Key"]!;
            _endpoint = AzureOpenAIConfig["Api"]!;
            _deploymentName = AzureOpenAIConfig["DeploymentName"]!;
            _location = AzureOpenAIConfig["Location"]!;
        }
        public async Task<string> GetChatCompletionAsync(List<Message> messages)
        {
            AzureOpenAIClient openAIClient = new(
               new Uri(_endpoint),
               new ApiKeyCredential(_apiKey));

            ChatClient chatClient = openAIClient.GetChatClient(_deploymentName);

            var messagesCount = messages.Count();
            var chatMessages = new ChatMessage[messagesCount];

            for(var i = 0; i < messagesCount; i++)
            {
                if (messages[i].Role.Equals(Roles.Assistant.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    chatMessages[i] = new AssistantChatMessage(messages[i].Content);
                }
                else if (messages[i].Role.Equals(Roles.User.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    chatMessages[i] = new UserChatMessage(messages[i].Content);
                }
                else
                {
                    chatMessages[i] = new SystemChatMessage(messages[i].Content);
                }
            }


            ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages);

            return completion.Content[0].Text;
        }

        public async Task<string> GetStructuredScrapedContent(string scrapedContent)
        {
            AzureOpenAIClient openAIClient = new(
               new Uri(_endpoint),
               new ApiKeyCredential(_apiKey));

            ChatClient chatClient = openAIClient.GetChatClient(_deploymentName);

            

            ChatCompletion completion = await chatClient.CompleteChatAsync([
                new SystemChatMessage(_SystemPrompt),
                new UserChatMessage(scrapedContent)
                ]);

            return completion.Content[0].Text;
        }

        public async Task<string> TranslateContentToArabicAsync(string englishContent)
        {
            AzureOpenAIClient openAIClient = new(
               new Uri(_endpoint),
               new ApiKeyCredential(_apiKey));

            ChatClient chatClient = openAIClient.GetChatClient(_deploymentName);



            ChatCompletion completion = await chatClient.CompleteChatAsync([
                new SystemChatMessage(_systemMessage),
                new UserChatMessage(englishContent)
                ]);

            return completion.Content[0].Text;
        }
    }
}
