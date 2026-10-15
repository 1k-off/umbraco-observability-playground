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
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaService _mediaService;
    private readonly ILogger<DemoApiController> _logger;
    private readonly Random _random = new();

    public DemoApiController(
        IOptions<DemoOptions> options,
        IContentService contentService,
        IContentTypeService contentTypeService,
        IMediaService mediaService,
        ILogger<DemoApiController> logger)
    {
        _options = options.Value;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Demo probe: returns immediately. delayMs is accepted for API compatibility and
    /// echoed in the payload; artificial Task.Delay was removed (antipattern D fix).
    /// </summary>
    [HttpGet("slow")]
    public IActionResult Slow([FromQuery] int delayMs = 100)
    {
        if (!_options.SlowEndpoint)
        {
            return NotFound();
        }

        delayMs = Math.Clamp(delayMs, 50, 500);

        using Activity? activity = DemoTelemetry.ActivitySource.StartActivity("DemoSlowEndpoint", ActivityKind.Internal);
        activity?.SetTag("demo.delay_ms", 0);
        activity?.SetTag("demo.requested_delay_ms", delayMs);
        activity?.SetTag("demo.fixed", true);

        // delayedMs reports actual wait (none). delayMs query still accepted for callers/k6 URLs.
        return Ok(new { delayedMs = 0 });
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
    /// Articles enrichment: same JSON shape as before, but without GetRootContent + N+1 GetById.
    /// Uses typed paging + batched GetByIds for relations/media (antipattern A fix).
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
        parent?.SetTag("demo.fixed", true);

        IContentType? articleType = _contentTypeService.Get(ContentTypeBootstrap.ArticleAlias);
        if (articleType is null)
        {
            return Ok(new
            {
                take,
                count = 0,
                relationLookups = 0,
                antipattern = (string?)null,
                items = Array.Empty<object>(),
            });
        }

        using Activity? listSpan = DemoTelemetry.ActivitySource.StartActivity(
            "ContentService.GetPagedOfType",
            ActivityKind.Internal);
        listSpan?.SetTag("contentType.alias", ContentTypeBootstrap.ArticleAlias);

        List<IContent> articles = _contentService
            .GetPagedOfType(
                articleType.Id,
                pageIndex: 0,
                pageSize: take,
                out _,
                filter: null!,
                Ordering.By(nameof(IContent.UpdateDate), Direction.Descending))
            .ToList();

        parent?.SetTag("demo.article_count", articles.Count);

        HashSet<Guid> contentKeys = [];
        HashSet<Guid> mediaKeys = [];

        foreach (IContent article in articles)
        {
            if (TryGetGuid(article.GetValue<string>("category"), out Guid categoryKey))
            {
                contentKeys.Add(categoryKey);
            }

            if (TryGetGuid(article.GetValue<string>("author"), out Guid authorKey))
            {
                contentKeys.Add(authorKey);
            }

            if (TryGetGuid(article.GetValue<string>("featuredImage"), out Guid featuredKey))
            {
                mediaKeys.Add(featuredKey);
            }
        }

        Dictionary<Guid, IContent> contentByKey;
        using (Activity? batchContent = DemoTelemetry.ActivitySource.StartActivity(
                   "ContentService.GetByIds",
                   ActivityKind.Internal))
        {
            batchContent?.SetTag("demo.batch_size", contentKeys.Count);
            contentByKey = contentKeys.Count == 0
                ? []
                : _contentService.GetByIds(contentKeys).ToDictionary(c => c.Key);
        }

        foreach (IContent author in contentByKey.Values)
        {
            if (TryGetGuid(author.GetValue<string>("avatar"), out Guid avatarKey))
            {
                mediaKeys.Add(avatarKey);
            }
        }

        Dictionary<Guid, IMedia> mediaByKey;
        using (Activity? batchMedia = DemoTelemetry.ActivitySource.StartActivity(
                   "MediaService.GetByIds",
                   ActivityKind.Internal))
        {
            batchMedia?.SetTag("demo.batch_size", mediaKeys.Count);
            mediaByKey = mediaKeys.Count == 0
                ? []
                : _mediaService.GetByIds(mediaKeys).ToDictionary(m => m.Key);
        }

        int relationLookups = contentByKey.Count + mediaByKey.Count;
        parent?.SetTag("demo.relation_lookups", relationLookups);

        var items = new List<object>(articles.Count);
        foreach (IContent article in articles)
        {
            object? category = null;
            if (TryGetGuid(article.GetValue<string>("category"), out Guid categoryKey)
                && contentByKey.TryGetValue(categoryKey, out IContent? categoryContent))
            {
                category = new
                {
                    id = categoryContent.Key,
                    name = categoryContent.GetValue<string>("name") ?? categoryContent.Name,
                    slug = categoryContent.GetValue<string>("slug"),
                };
            }

            object? authorPayload = null;
            if (TryGetGuid(article.GetValue<string>("author"), out Guid authorKey)
                && contentByKey.TryGetValue(authorKey, out IContent? author))
            {
                string? avatarName = null;
                if (TryGetGuid(author.GetValue<string>("avatar"), out Guid avatarKey)
                    && mediaByKey.TryGetValue(avatarKey, out IMedia? avatar))
                {
                    avatarName = avatar.Name;
                }

                authorPayload = new
                {
                    id = author.Key,
                    name = author.GetValue<string>("name") ?? author.Name,
                    slug = author.GetValue<string>("slug"),
                    avatarName,
                };
            }

            string? featuredName = null;
            if (TryGetGuid(article.GetValue<string>("featuredImage"), out Guid featuredKey)
                && mediaByKey.TryGetValue(featuredKey, out IMedia? featured))
            {
                featuredName = featured.Name;
            }

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

        _logger.LogInformation(
            "Articles enrichment returned {Count} items with {Lookups} batched relation lookups",
            items.Count,
            relationLookups);

        return Ok(new
        {
            take,
            count = items.Count,
            relationLookups,
            antipattern = (string?)null,
            items,
        });
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
