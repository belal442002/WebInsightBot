using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using PuppeteerSharp;
using System.Text;
using System.Text.RegularExpressions;

namespace Chatbot.Services
{
    public class WebScrapingService : IWebScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly HashSet<string> _visitedUrls = new HashSet<string>();
        private readonly string _baseDomain;

        public WebScrapingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WebsiteChatbot/1.0)");
            //_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            //_httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            //_httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            var scrapingConfig = configuration.GetSection("Scraping");
            var baseUrl = scrapingConfig["BaseUrl_English"];
            _baseDomain = new Uri(baseUrl!).Host;

            _retryPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        //---------------------------------------------------------
        public async Task<Dictionary<string, string>> CrawlAndExtractContentWithLinkFilteringAsync(string baseUrl, int maxDepth = 1, int maxPages = 20)
        {
            var results = new Dictionary<string, string>();
            var urlsToCrawl = new Queue<(string Url, int Depth)>();
            var visitedUrls = new HashSet<string>();

            // Social media domains to exclude
            var socialMediaDomains = new HashSet<string>
            {
                "facebook.com", "twitter.com", "youtube.com",
                "instagram.com", "linkedin.com", "snapchat.com",
                "tiktok.com", "threads.net"
            };

            urlsToCrawl.Enqueue((baseUrl, 0));

            while (urlsToCrawl.Count > 0 && results.Count < maxPages)
            {
                var (currentUrl, currentDepth) = urlsToCrawl.Dequeue();

                if (visitedUrls.Contains(currentUrl) || currentDepth > maxDepth)
                    continue;

                try
                {
                    // Fetch the page
                    var htmlContent = await ScrapeWithBrowserAsync(currentUrl);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    // Extract clean text
                    var pageContent = ExtractSharePointContentFromHtml(htmlDoc);
                    results[currentUrl] = pageContent;

                    // Find and queue internal links (if depth allows)
                    if (currentDepth < maxDepth)
                    {
                        var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                            ?.Select(a => a.GetAttributeValue("href", ""))
                            .Where(href => !string.IsNullOrEmpty(href))
                            .Select(href => NormalizeUrl(baseUrl, href))
                            .Where(url => IsInternalUrl(url, baseUrl))
                            .Where(url => !IsSocialMediaUrl(url, socialMediaDomains)) // Skip social media
                            .Where(url => !IsFileLink(url)) // Skip PDFs/Word docs
                            .Where(url => !IsDuplicateAnchor(url, currentUrl)) // Skip #anchor links
                            .Distinct()
                            .ToList() ?? new List<string>();

                        foreach (var link in links)
                        {
                            if (!visitedUrls.Contains(link))
                                urlsToCrawl.Enqueue((link, currentDepth + 1));
                        }
                    }

                    visitedUrls.Add(currentUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error crawling {currentUrl}: {ex.Message}");
                }
            }

            return results;
        }
        private bool IsSocialMediaUrl(string url, HashSet<string> socialMediaDomains)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            return socialMediaDomains.Any(domain => uri.Host.Contains(domain));
        }
        private bool IsFileLink(string url)
        {
            var fileExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".ppt" };
            return fileExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
        private bool IsDuplicateAnchor(string url, string baseUrl)
        {
            // Skip links like "/page.aspx#section1" (same page anchors)
            return url.StartsWith(baseUrl) && url.Contains('#');
        }
        //---------------------------------------------------------
        public async Task<Dictionary<string, string>> CrawlAndExtractContentAsync(string baseUrl, int maxDepth = 1, int maxPages = 20)
        {
            var results = new Dictionary<string, string>();
            var urlsToCrawl = new Queue<(string Url, int Depth)>();
            var visitedUrls = new HashSet<string>();

            urlsToCrawl.Enqueue((baseUrl, 0));

            while (urlsToCrawl.Count > 0 && results.Count < maxPages)
            {
                var (currentUrl, currentDepth) = urlsToCrawl.Dequeue();

                if (visitedUrls.Contains(currentUrl) || currentDepth > maxDepth)
                    continue;

                try
                {
                    // Fetch the page
                    var htmlContent = await ScrapeWithBrowserAsync(currentUrl);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    // Extract clean text
                    var pageContent = ExtractSharePointContentFromHtml(htmlDoc);
                    results[currentUrl] = pageContent;

                    // Find and queue internal links
                    if (currentDepth < maxDepth)
                    {
                        var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                            ?.Select(a => a.GetAttributeValue("href", ""))
                            .Where(href => !string.IsNullOrEmpty(href))
                            .Select(href => NormalizeUrl(baseUrl, href))
                            .Where(url => IsInternalUrl(url, baseUrl))
                            .Distinct()
                            .ToList() ?? new List<string>();

                        foreach (var link in links)
                        {
                            if (!visitedUrls.Contains(link))
                                urlsToCrawl.Enqueue((link, currentDepth + 1));
                        }
                    }

                    visitedUrls.Add(currentUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error crawling {currentUrl}: {ex.Message}");
                }
            }

            return results;
        }
        private string NormalizeUrl(string baseUrl, string href)
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out var absoluteUri))
                return absoluteUri.ToString();

            if (Uri.TryCreate(new Uri(baseUrl), href, out var relativeUri))
                return relativeUri.ToString();

            return href;
        }
        private bool IsInternalUrl(string url, string baseUrl)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            var baseDomain = new Uri(baseUrl).Host;
            return uri.Host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase);
        }
        private string ExtractSharePointContentFromHtml(HtmlDocument htmlDoc)
        {
            var contentBuilder = new StringBuilder();

            // SharePoint-specific content selectors
            var contentSelectors = new[]
            {
                "//*[contains(@class, 'ms-rtestate-field')]", // Rich text
                "//*[contains(@class, 'ms-WPBody')]",        // Web parts
                "//article",                                 // News/articles
                "//div[contains(@class, 'news-item')]",      // News items
                "//div[contains(@class, 'event-details')]",  // Events
                "//main//p",                                 // Paragraphs in main content
            };

            foreach (var selector in contentSelectors)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var text = System.Web.HttpUtility.HtmlDecode(node.InnerText);
                        text = Regex.Replace(text, @"\s+", " ").Trim();

                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 30)
                        {
                            contentBuilder.AppendLine(text);
                            contentBuilder.AppendLine(); // Spacing
                        }
                    }
                }
            }

            return contentBuilder.ToString();
        }

        //------------------------------------------------------
        public async Task<string> GetCleanContent(string url)
        {
            var htmlContent = await ScrapeWithBrowserAsync(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Remove unwanted elements
            RemoveUnwantedElements(htmlDoc);

            // Extract and clean text
            var text = htmlDoc.DocumentNode.InnerText;
            return CleanTextContent(text);
        }
        private async Task<string> ScrapeWithBrowserAsync(string url)
        {
            await new BrowserFetcher().DownloadAsync();
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            // Set user agent to mimic a real browser
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);

            try
            {
                await page.WaitForSelectorAsync(".ms-rtestate-field", new WaitForSelectorOptions { Timeout = 5000 });
            }
            catch (Exception)
            {
                // Continue even if specific selector isn't found
            }
            // Wait for specific content to load if needed
            // await page.WaitForSelectorAsync(".main-content", new WaitForSelectorOptions { Timeout = 5000 });

            var content = await page.GetContentAsync();
            return content;
        }
        private void RemoveUnwantedElements(HtmlDocument htmlDoc)
        {
            var unwantedTags = new[] { "script", "style", "meta", "link", "noscript", "svg", "path",
                                     "form", "input", "select", "textarea", "button", "iframe",
                                     "nav", "footer", "header" };

            foreach (var tag in unwantedTags)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes($"//{tag}");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Remove();
                    }
                }
            }

            // Remove SharePoint-specific non-content elements
            var sharePointClasses = new[] { "ms-core-navigation", "ms-siteactions-main",
                                          "ms-commandBar", "ms-footer" };

            foreach (var cssClass in sharePointClasses)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes($"//*[contains(@class, '{cssClass}')]");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Remove();
                    }
                }
            }
        }
        private string CleanTextContent(string text)
        {
            // Decode HTML entities
            text = System.Web.HttpUtility.HtmlDecode(text);

            // Remove HTML comments
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"<!--.*?-->", string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Remove JavaScript snippets
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"<script[^>]*>[\s\S]*?</script>", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Collapse whitespace and clean up
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"^\s+$[\r\n]*", string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return text.Trim();
        }

        //-----------------------------------------------------------------------------------
        public async Task<string> ExtractSharePointContent(string url)
        {
            var htmlContent = await ScrapeWithBrowserAsync(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var contentBuilder = new StringBuilder();

            // SharePoint specific content selectors
            var contentSelectors = new[]
            {
                "//*[contains(@class, 'ms-rtestate-field')]", // Rich text fields
                "//*[contains(@class, 'ms-WPBody')]", // Web parts
                "//*[contains(@class, 'ms-webpart-chrome-content')]",
                "//div[contains(@class, 'content')]",
                "//div[contains(@class, 'article')]",
                "//main",
                "//article"
            };

            foreach (var selector in contentSelectors)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var text = node.InnerText;
                        text = CleanTextContent(text);

                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 50)
                        {
                            contentBuilder.AppendLine(text);
                            contentBuilder.AppendLine(); // Add spacing between sections
                        }
                    }
                }
            }

            return contentBuilder.ToString();
        }
        //-----------------------------------------------------------------------------------
        public async Task<string> ScrapeWebsiteContentAsync(string url, int maxDepth = 2, int maxPages = 50)
        {
            //var response = await _httpClient.GetAsync(url);
            //var content = await response.Content.ReadAsStringAsync();

            //if (content.Contains("access denied") || content.Contains("bot detected"))
            //{
            //    throw new Exception("Anti-scraping protection detected");
            //}

            _visitedUrls.Clear();
            var Content = await CrawlAndScrapeAsync(url, maxDepth, maxPages);
            return Content;
        }
        private async Task<string> CrawlAndScrapeAsync(string url, int maxDepth, int maxPages, int currentDepth = 0)
        {
            if (currentDepth > maxDepth || _visitedUrls.Count >= maxPages || _visitedUrls.Contains(url))
                return string.Empty;

            _visitedUrls.Add(url);

            try
            {
                var htmlContent = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                });

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                // Remove unwanted elements (scripts, styles, etc.)
                htmlDoc.DocumentNode.Descendants()
                    .Where(n => n.Name == "script" || n.Name == "style" || n.Name == "nav" || n.Name == "footer")
                    .ToList()
                    .ForEach(n => n.Remove());

                // Extract main content
                var mainContent = ExtractMainContent(htmlDoc);

                // Find and follow links
                if (currentDepth < maxDepth)
                {
                    var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]")?
                        .Select(a => a.GetAttributeValue("href", string.Empty))
                        .Where(href => !string.IsNullOrEmpty(href) && IsInternalLink(href))
                        .Select(href => NormalizeUrl(url, href))
                        .Distinct()
                        .ToList() ?? new List<string>();

                    foreach (var link in links.Take(maxPages - _visitedUrls.Count))
                    {
                        if (!_visitedUrls.Contains(link))
                        {
                            var linkedContent = await CrawlAndScrapeAsync(link, maxDepth, maxPages, currentDepth + 1);
                            mainContent += $"\n\n[Linked Content from: {link}]\n{linkedContent}";
                        }
                    }
                }

                return $"[Content from: {url}]\n{mainContent}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {url}: {ex.Message}");
                return $"[Failed to retrieve content from: {url}]";
            }
        }
        private string ExtractMainContent(HtmlDocument htmlDoc)
        {
            // Try to find main content areas
            var mainContentNodes = new List<HtmlNode>();

            // Common content selectors
            var selectors = new[] { "//main", "//article", "//div[contains(@class, 'content')]", "//div[contains(@class, 'main')]" };

            foreach (var selector in selectors)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    mainContentNodes.AddRange(nodes);
                }
            }

            // If no specific content areas found, use body
            if (mainContentNodes.Count == 0)
            {
                mainContentNodes.Add(htmlDoc.DocumentNode.SelectSingleNode("//body"));
            }

            // Combine and clean content
            var content = string.Join("\n", mainContentNodes
                .Where(n => n != null)
                .Select(n => n.InnerText.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            return content;


            //// Try more specific selectors first
            //var contentSelectors = new[]
            //{
            //    "//*[contains(@class, 'main-content')]",
            //    "//*[contains(@class, 'content-area')]",
            //    "//*[contains(@class, 'article-body')]",
            //    "//main",
            //    "//article",
            //    "//div[contains(@class, 'content')]",
            //    "//div[contains(@class, 'main')]",
            //    "/html/body"
            //};

            //foreach (var selector in contentSelectors)
            //{
            //    var node = htmlDoc.DocumentNode.SelectSingleNode(selector);
            //    if (node != null)
            //    {
            //        // Clean up the content
            //        var text = System.Web.HttpUtility.HtmlDecode(node.InnerText);
            //        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            //        if (!string.IsNullOrWhiteSpace(text) && text.Length > 100) // Ensure meaningful content
            //        {
            //            return text;
            //        }
            //    }
            //}

            //return "Could not extract meaningful content from the page.";
        }
        private bool IsInternalLink(string href)
        {
            try
            {
                var uri = new Uri(href, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri) return true;
                return uri.Host.Equals(_baseDomain, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

    }
}
