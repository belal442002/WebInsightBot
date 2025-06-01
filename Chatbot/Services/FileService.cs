namespace Chatbot.Services
{
    public class FileService : IFileService
    {
        private readonly string _fileName;
        private readonly IConfiguration _configuration;

        public FileService(IConfiguration configuration)
        {
            _configuration = configuration;
            _fileName = _configuration.GetSection("FileSystem")["FileName"]!;

        }
        public async Task File_WriteAllTextAsync(string scrapedContent)
        {
            // Generate a unique filename based on the URL
            
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "scraped_content", _fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Write content to file
            await System.IO.File.WriteAllTextAsync(filePath, scrapedContent);
        }

        public async Task<string> ReadScrapedContent()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "scraped_content", _fileName);

            if (System.IO.File.Exists(filePath))
            {
                return await System.IO.File.ReadAllTextAsync(filePath);
            }

            throw new Exception("Can't read from file");
        }

    }
}
