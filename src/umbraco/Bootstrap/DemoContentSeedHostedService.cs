using Examine;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace UmbracoMonitoring.Bootstrap;

public class DemoContentSeedHostedService : IHostedService
{
    private const int ArticleCount = 100;
    private const int CategoryCount = 5;
    private const int AuthorCount = 10;

    private readonly IRuntimeState _runtimeState;
    private readonly IScopeProvider _scopeProvider;
    private readonly ContentTypeBootstrap _contentTypeBootstrap;
    private readonly IContentService _contentService;
    private readonly IExamineManager _examineManager;
    private readonly ILogger<DemoContentSeedHostedService> _logger;

    public DemoContentSeedHostedService(
        IRuntimeState runtimeState,
        IScopeProvider scopeProvider,
        ContentTypeBootstrap contentTypeBootstrap,
        IContentService contentService,
        IExamineManager examineManager,
        ILogger<DemoContentSeedHostedService> logger)
    {
        _runtimeState = runtimeState;
        _scopeProvider = scopeProvider;
        _contentTypeBootstrap = contentTypeBootstrap;
        _contentService = contentService;
        _examineManager = examineManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (_runtimeState.Level < RuntimeLevel.Run && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);

        _contentTypeBootstrap.EnsureDocumentTypes();

        if (_contentService.GetRootContent().Any(c => c.ContentType.Alias == ContentTypeBootstrap.ArticleAlias))
        {
            _logger.LogInformation("Demo content already seeded.");
            return;
        }

        _logger.LogInformation("Seeding demo content ({Articles} articles)...", ArticleCount);

        var categories = new List<IContent>();
        for (var i = 1; i <= CategoryCount; i++)
        {
            IContent category = _contentService.Create(
                $"Category {i}",
                Constants.System.Root,
                ContentTypeBootstrap.CategoryAlias);
            category.SetValue("name", $"Category {i}");
            category.SetValue("slug", $"category-{i}");
            category.SetValue("description", $"Description for category {i}");
            SaveAndPublish(category);
            categories.Add(category);
        }

        var authors = new List<IContent>();
        for (var i = 1; i <= AuthorCount; i++)
        {
            IContent author = _contentService.Create(
                $"Author {i}",
                Constants.System.Root,
                ContentTypeBootstrap.AuthorAlias);
            author.SetValue("name", $"Author {i}");
            author.SetValue("slug", $"author-{i}");
            author.SetValue("bio", $"Bio for author {i}");
            SaveAndPublish(author);
            authors.Add(author);
        }

        IContent settings = _contentService.Create(
            "Site settings",
            Constants.System.Root,
            ContentTypeBootstrap.SiteSettingsAlias);
        settings.SetValue("siteName", "Tech Blog Demo");
        settings.SetValue("tagline", "Umbraco monitoring sample");
        settings.SetValue("aboutBody", "<p>About page content for the monitoring demo.</p>");
        settings.SetValue("contactEmail", "hello@demo.local");
        SaveAndPublish(settings);

        var random = new Random(42);
        for (var i = 1; i <= ArticleCount; i++)
        {
            IContent category = categories[i % categories.Count];
            IContent author = authors[i % authors.Count];
            string slug = $"article-{i:D3}";

            IContent article = _contentService.Create(
                $"Article {i}",
                Constants.System.Root,
                ContentTypeBootstrap.ArticleAlias);
            article.SetValue("title", $"Article {i}: {category.GetValue<string>("name")}");
            article.SetValue("slug", slug);
            article.SetValue("excerpt", $"Short excerpt for article {i}.");
            article.SetValue("body", $"<p>Body content for article {i}. Monitoring demo paragraph.</p>");
            article.SetValue("publishDate", DateTime.UtcNow.AddDays(-random.Next(0, 90)));
            article.SetValue("category", category.GetUdi().ToString());
            article.SetValue("author", author.GetUdi().ToString());
            SaveAndPublish(article);
        }

        RebuildExternalIndex();
        _logger.LogInformation("Demo content seed completed.");
    }

    private void SaveAndPublish(IContent content)
    {
        _contentService.Save(content);
        _contentService.Publish(content, Array.Empty<string>());
    }

    private void RebuildExternalIndex()
    {
        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            _logger.LogWarning("External index not found; search may be empty until rebuild.");
            return;
        }

        if (index is UmbracoExamineIndex umbracoIndex)
        {
            umbracoIndex.CreateIndex();
            _logger.LogInformation("External Examine index rebuilt.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
