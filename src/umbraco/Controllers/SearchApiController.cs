using Examine;
using Examine.Search;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace UmbracoMonitoring.Controllers;

[ApiController]
[Route("umbraco/api/search")]
public class SearchApiController : ControllerBase
{
    private readonly IExamineManager _examineManager;

    public SearchApiController(IExamineManager examineManager)
    {
        _examineManager = examineManager;
    }

    [HttpGet]
    public IActionResult Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query parameter q is required." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            return StatusCode(503, new { error = "Search index not available." });
        }

        ISearcher searcher = index.Searcher;
        int skip = (page - 1) * pageSize;

        ISearchResults results = searcher
            .CreateQuery("content")
            .ManagedQuery(q)
            .Execute(new QueryOptions(skip, pageSize));

        var items = results
            .Select(r => new
            {
                id = r.Id,
                score = r.Score,
                name = GetField(r, "nodeName"),
                slug = GetField(r, "slug"),
            })
            .ToList();

        return Ok(new
        {
            query = q,
            page,
            pageSize,
            total = results.TotalItemCount,
            items,
        });
    }

    private static string? GetField(ISearchResult result, string field)
    {
        if (!result.AllValues.TryGetValue(field, out IReadOnlyList<string>? values))
        {
            return null;
        }

        return values.FirstOrDefault();
    }
}
