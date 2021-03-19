using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FaceCord.Facebook
{
    public interface IFacebookService
    {
        Task Init();
        FacebookPost GetPost(int? postIndex);
        Task<bool> UpdateCachedPosts();
    }

    public class FacebookService : IFacebookService
    {
        private const string Key = nameof(Key);

        private readonly IFacebookBrowser facebookBrowser;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<FacebookService> logger;

        public FacebookService(IFacebookBrowser facebookBrowser,
            IMemoryCache memoryCache,
            ILogger<FacebookService> logger
            )
        {
            this.facebookBrowser = facebookBrowser;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task Init()
        {
            await facebookBrowser.DownloadBrowser();
            await facebookBrowser.Login();
            await LoadPostsToCache();
        }

        private async Task LoadPostsToCache()
        {
            memoryCache.Set(Key, await facebookBrowser.GetTimelinePosts(), CacheEntryOptions());
            logger.LogInformation("Loaded FB posts to cache");
        }

        public async Task<bool> UpdateCachedPosts()
        {
            var posts = await facebookBrowser.GetTimelinePosts();
            var post = posts.First();
            var cachedPosts = memoryCache.Get<IList<FacebookPost>>(Key);
            var cachedPost = cachedPosts.First();

            if (post.IsSameWith(cachedPost))
            {
                logger.LogInformation("Latest cached FB post valid");
                return false;
            }

            memoryCache.Set(Key, posts, CacheEntryOptions());
            logger.LogInformation("Updated cached FB posts");

            return true;
        }

        public FacebookPost GetPost(int? postIndex)
        {
            return GetPostByIndex(memoryCache.Get<IList<FacebookPost>>(Key), postIndex ?? 0);
        }

        private static FacebookPost GetPostByIndex(IList<FacebookPost> posts, int index) =>
            posts.Count > index ? posts[index] : null;


        private static MemoryCacheEntryOptions CacheEntryOptions() =>
            new() { Priority = CacheItemPriority.NeverRemove };
    }
}
