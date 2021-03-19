using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FaceCord.Common;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace FaceCord.Facebook
{
    public interface IFacebookBrowser : IAsyncDisposable
    {
        Task DownloadBrowser();
        Task<IList<FacebookPost>> GetTimelinePosts();
        Task Login();
    }

    public class FacebookBrowser : IFacebookBrowser
    {
        private Browser chromeBrowser;

        private const string FacebookUrl = "https://facebook.com";
        private const string UserAgentString = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36";

        private readonly BotSettings botSettings;
        private readonly ILogger<FacebookBrowser> logger;

        public FacebookBrowser(BotSettings botSettings, ILogger<FacebookBrowser> logger)
        {
            this.botSettings = botSettings;
            this.logger = logger;
        }

        private async Task<Browser> GetLaunchedBrowser()
        {
            if (chromeBrowser != null)
                return chromeBrowser;

            var launchedBrowser = await LaunchBrowser();

            chromeBrowser = launchedBrowser;

            return launchedBrowser;
        }

        private static async Task<Browser> LaunchBrowser()
        {
            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[] {"--no-sandbox"}
            };

            var browser = await Puppeteer.LaunchAsync(options);
            var permissions = new List<OverridePermission> { OverridePermission.Geolocation, OverridePermission.Notifications };
            await browser.DefaultContext.OverridePermissionsAsync(FacebookUrl, permissions);

            return browser;
        }

        private string FacebookUserUrl => $"{FacebookUrl}/{botSettings.Facebook.User}";

        public async Task DownloadBrowser()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            logger.LogInformation("Downloaded a browser");
        }

        public async Task Login()
        {
            var browser = await GetLaunchedBrowser();
            var page = (await browser.PagesAsync()).Single();
            await page.SetUserAgentAsync(UserAgentString);

            await page.GoToAsync(FacebookUrl, WaitUntilNavigation.Networkidle0);
            await page.ClickAsync("button[data-testid=cookie-policy-dialog-accept-button]");
            await page.WaitForSelectorAsync("button[data-testid=cookie-policy-dialog-accept-button]", new WaitForSelectorOptions { Hidden = true });

            await page.TypeAsync("#email", botSettings.Facebook.Login);
            await page.TypeAsync("#pass", botSettings.Facebook.Password);
            await page.ClickAsync("button[name=login]", new ClickOptions { ClickCount = 3 });
            await page.WaitForSelectorAsync("form[action^='/logout.php']");

            await page.GoToAsync("about:blank");
            logger.LogInformation("Logged to FB");
        }

        public async Task<IList<FacebookPost>> GetTimelinePosts()
        {
            var browser = await GetLaunchedBrowser();
            var page = (await browser.PagesAsync()).Single();
            await page.SetUserAgentAsync(UserAgentString);

            await page.GoToAsync(FacebookUserUrl);
            await page.WaitForSelectorAsync("div[data-pagelet='ProfileTimeline']");


            var sw = new Stopwatch();
            sw.Start();
            var loadedPostCount = 0;
            while (loadedPostCount < 10)
            {
                if (sw.Elapsed.Seconds > 120)
                {
                    sw.Stop();
                    await page.GoToAsync("about:blank");
                    throw new InvalidOperationException();
                }

                loadedPostCount = await LoadTimelinePosts(page);
            }

            await OpenTimelinePostsText(page);

            var timelinePosts = await ExtractTimelinePosts(page);

            await page.GoToAsync("about:blank");

            return timelinePosts;
        }

        private static async Task<IList<FacebookPost>> ExtractTimelinePosts(Page page)
        {
            var document = new HtmlDocument();
            document.LoadHtml(await page.GetContentAsync());

            var nodes = document.DocumentNode.QuerySelector("div[data-pagelet='ProfileTimeline']");
            var rootPosts = nodes.QuerySelectorAll("div[data-ad-comet-preview=message]").Select(x => x.ParentNode.ParentNode).ToList();

            var list = new List<FacebookPost>();
            foreach (var rootPost in rootPosts)
            {
                var fb = new FacebookPost
                {
                    Paragraphs = rootPost
                        .QuerySelectorAll("div[data-ad-comet-preview=message] div.qzhwtbm6.knvmm38d div.cxmmr5t8.oygrvhab.hcukyx3x")
                        .Select(x => WebUtility.HtmlDecode(x.InnerText))
                        .ToList(),
                    Link = TransformLink(rootPost.QuerySelector("div.l9j0dhe7 a.oajrlxb2.g5ia77u1")?.GetAttributeValue("href", null))
                };

                list.Add(fb);
            }

            return list;
        }

        private static string TransformLink(string link)
        {
            if (link == null)
                return null;

            var decodedLink = WebUtility.UrlDecode(WebUtility.HtmlDecode(link));

            var fbPhotoLinkMatch = Regex.Match(decodedLink, "https://www.facebook.com/photo/\\?fbid=[0-9]{15,17}");
            if (fbPhotoLinkMatch.Success)
                return fbPhotoLinkMatch.Value;


            var replacedLink = Regex.Replace(decodedLink, "(\\?|&)fbclid.+", string.Empty);
            var replacedLink2 = Regex.Replace(replacedLink, "https://l.facebook.com/l.php\\?u=", string.Empty);

            return replacedLink2;
        }

        private static async Task OpenTimelinePostsText(Page page)
        {
            const string openAllTexts =
                @"let elements = document.querySelectorAll(""div[data-ad-comet-preview=message] div.oajrlxb2""); for (let element of elements) { element.click(); }";
            await page.EvaluateExpressionAsync(openAllTexts);
            Thread.Sleep(3000);
        }

        private static async Task<int> LoadTimelinePosts(Page page)
        {
            await page.EvaluateExpressionAsync("window.scrollBy(0, 1000);");
            Thread.Sleep(200);

            var document = new HtmlDocument();
            document.LoadHtml(await page.GetContentAsync());

            var timelineNode = document.DocumentNode.QuerySelector("div[data-pagelet='ProfileTimeline']");
            var postText = timelineNode.QuerySelectorAll("div[data-ad-comet-preview=message]").ToList();
            return postText.Count;
        }

        public async ValueTask DisposeAsync()
        {
            if (chromeBrowser == null)
                return;

            await chromeBrowser.CloseAsync();
        }
    }
}
