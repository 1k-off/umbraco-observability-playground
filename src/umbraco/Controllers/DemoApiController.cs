using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoMonitoring.Bootstrap;
using UmbracoMonitoring.Configuration;
using UmbracoMonitoring.Telemetry;

namespace UmbracoMonitoring.Controllers;

[ApiController]
[Route("umbraco/api/demo")]
public class DemoApiController : ControllerBase
{
    private readonly DemoOptions _options;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly ILogger<DemoApiController> _logger;
    private readonly Random _random = new();

    public DemoApiController(
        IOptions<DemoOptions> options,
        IContentService contentService,
        IMediaService mediaService,
        ILogger<DemoApiController> logger)
    {
        _options = options.Value;
        _contentService = contentService;
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpGet("slow")]
    public async Task<IActionResult> Slow([FromQuery] int delayMs = 100, CancellationToken cancellationToken = default)
    {
        if (!_options.SlowEndpoint)
        {
            return NotFound();
        }

        delayMs = Math.Clamp(delayMs, 50, 500);

        using Activity? activity = DemoTelemetry.ActivitySource.StartActivity("DemoSlowEndpoint", ActivityKind.Internal);
        activity?.SetTag("demo.delay_ms", delayMs);

        await Task.Delay(delayMs, cancellationToken);
        return Ok(new { delayedMs = delayMs });
    }

    [HttpGet("fault")]
    public IActionResult Fault([FromQuery] double? rate = null)
    {
        if (!_options.FaultEndpoint)
        {
            return NotFound();
        }

        double faultRate = rate ?? _options.FaultRate;
        faultRate = Math.Clamp(faultRate, 0, 1);

        if (_random.NextDouble() < faultRate)
        {
            return StatusCode(500, new { error = "Injected fault for demo." });
        }

        return Ok(new { status = "ok" });
    }

    /// <summary>
    /// Intentionally suboptimal: lists articles via ContentService (DB) and resolves
    /// category, author, featured image, and avatar with per-item GetById calls (N+1).
    /// Enable with Demo:NaiveArticlesEndpoint for load / observability demos.
    /// </summary>
    [HttpGet("articles-naive")]
    public IActionResult ArticlesNaive([FromQuery] int take = 10)
    {
        if (!_options.NaiveArticlesEndpoint)
        {
            return NotFound();
        }

        take = Math.Clamp(take, 1, 50);

        using Activity? parent = DemoTelemetry.ActivitySource.StartActivity("DemoNaiveArticles", ActivityKind.Internal);
        parent?.SetTag("demo.take", take);
        parent?.SetTag("demo.antipattern", "n_plus_one");

        // DB round-trip for root content instead of Delivery/published query cache.
        List<IContent> articles = _contentService.GetRootContent()
            .Where(c => c.ContentType.Alias == ContentTypeBootstrap.ArticleAlias)
            .OrderByDescending(c => c.UpdateDate)
            .Take(take)
            .ToList();

        parent?.SetTag("demo.article_count", articles.Count);

        var items = new List<object>(articles.Count);
        var relationLookups = 0;

        foreach (IContent article in articles)
        {
            using Activity? relationSpan = DemoTelemetry.ActivitySource.StartActivity(
                "ResolveArticleRelations",
                ActivityKind.Internal);
            relationSpan?.SetTag("article.id", article.Key.ToString());
            relationSpan?.SetTag("article.slug", article.GetValue<string>("slug"));

            // N+1: separate ContentService.GetById per relation (and media).
            object? category = ResolveContentRelation(article.GetValue<string>("category"), ref relationLookups);
            object? authorPayload = ResolveAuthorWithAvatar(article.GetValue<string>("author"), ref relationLookups);
            string? featuredName = ResolveMediaName(article.GetValue<string>("featuredImage"), ref relationLookups);

            items.Add(new
            {
                id = article.Key,
                title = article.GetValue<string>("title"),
                slug = article.GetValue<string>("slug"),
                excerpt = article.GetValue<string>("excerpt"),
                featuredImageName = featuredName,
                category,
                author = authorPayload,
            });
        }

        parent?.SetTag("demo.relation_lookups", relationLookups);
        _logger.LogInformation(
            "Naive articles endpoint returned {Count} items with {Lookups} relation lookups",
            items.Count,
            relationLookups);

        return Ok(new
        {
            take,
            count = items.Count,
            relationLookups,
            antipattern = "n_plus_one",
            items,
        });
    }

    private object? ResolveContentRelation(string? udiString, ref int relationLookups)
    {
        if (!TryGetGuid(udiString, out Guid key))
        {
            return null;
        }

        using Activity? span = DemoTelemetry.ActivitySource.StartActivity("ContentService.GetById", ActivityKind.Internal);
        span?.SetTag("content.key", key.ToString());
        relationLookups++;
        IContent? content = _contentService.GetById(key);
        if (content is null)
        {
            return null;
        }

        return new
        {
            id = content.Key,
            name = content.GetValue<string>("name") ?? content.Name,
            slug = content.GetValue<string>("slug"),
        };
    }

    private object? ResolveAuthorWithAvatar(string? authorUdi, ref int relationLookups)
    {
        if (!TryGetGuid(authorUdi, out Guid authorKey))
        {
            return null;
        }

        using Activity? span = DemoTelemetry.ActivitySource.StartActivity("ContentService.GetById", ActivityKind.Internal);
        span?.SetTag("content.key", authorKey.ToString());
        span?.SetTag("content.role", "author");
        relationLookups++;
        IContent? author = _contentService.GetById(authorKey);
        if (author is null)
        {
            return null;
        }

        string? avatarName = ResolveMediaName(author.GetValue<string>("avatar"), ref relationLookups);

        return new
        {
            id = author.Key,
            name = author.GetValue<string>("name") ?? author.Name,
            slug = author.GetValue<string>("slug"),
            avatarName,
        };
    }

    private string? ResolveMediaName(string? mediaUdi, ref int relationLookups)
    {
        if (!TryGetGuid(mediaUdi, out Guid mediaKey))
        {
            return null;
        }

        using Activity? span = DemoTelemetry.ActivitySource.StartActivity("MediaService.GetById", ActivityKind.Internal);
        span?.SetTag("media.key", mediaKey.ToString());
        relationLookups++;
        IMedia? media = _mediaService.GetById(mediaKey);
        return media?.Name;
    }

    private static bool TryGetGuid(string? udiString, out Guid key)
    {
        key = default;
        if (string.IsNullOrWhiteSpace(udiString))
        {
            return false;
        }

        if (!UdiParser.TryParse(udiString, out Udi? udi) || udi is not GuidUdi guidUdi)
        {
            return false;
        }

        key = guidUdi.Guid;
        return true;
    }
}
