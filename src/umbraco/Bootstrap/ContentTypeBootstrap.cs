using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace UmbracoMonitoring.Bootstrap;

public class ContentTypeBootstrap
{
    public const string CategoryAlias = "category";
    public const string AuthorAlias = "author";
    public const string ArticleAlias = "article";
    public const string SiteSettingsAlias = "siteSettings";

    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;

    public ContentTypeBootstrap(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
    }

    public async Task EnsureDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCategoryTypeAsync(cancellationToken);
        await EnsureAuthorTypeAsync(cancellationToken);
        await EnsureArticleTypeAsync(cancellationToken);
        await EnsureSiteSettingsTypeAsync(cancellationToken);
    }

    private async Task EnsureCategoryTypeAsync(CancellationToken cancellationToken)
    {
        IContentType type = GetOrCreate(CategoryAlias, "Category", allowsAtRoot: true);
        var changed = false;
        changed |= await EnsureTextAsync(type, "name", "Name", cancellationToken);
        changed |= await EnsureTextAsync(type, "slug", "Slug", cancellationToken);
        changed |= await EnsureTextareaAsync(type, "description", "Description", cancellationToken);
        SaveIfDirty(type, changed);
    }

    private async Task EnsureAuthorTypeAsync(CancellationToken cancellationToken)
    {
        IContentType type = GetOrCreate(AuthorAlias, "Author", allowsAtRoot: true);
        var changed = false;
        changed |= await EnsureTextAsync(type, "name", "Name", cancellationToken);
        changed |= await EnsureTextAsync(type, "slug", "Slug", cancellationToken);
        changed |= await EnsureTextareaAsync(type, "bio", "Bio", cancellationToken);
        changed |= await EnsureMediaPickerAsync(type, "avatar", "Avatar", cancellationToken);
        SaveIfDirty(type, changed);
    }

    private async Task EnsureArticleTypeAsync(CancellationToken cancellationToken)
    {
        IContentType type = GetOrCreate(ArticleAlias, "Article", allowsAtRoot: true);
        var changed = false;
        changed |= await EnsureTextAsync(type, "title", "Title", cancellationToken);
        changed |= await EnsureTextAsync(type, "slug", "Slug", cancellationToken);
        changed |= await EnsureTextareaAsync(type, "excerpt", "Excerpt", cancellationToken);
        changed |= await EnsureRichtextAsync(type, "body", "Body", cancellationToken);
        changed |= await EnsureDateAsync(type, "publishDate", "Publish date", cancellationToken);
        changed |= await EnsureMediaPickerAsync(type, "featuredImage", "Featured image", cancellationToken);
        changed |= await EnsureContentPickerAsync(type, "category", "Category", cancellationToken);
        changed |= await EnsureContentPickerAsync(type, "author", "Author", cancellationToken);
        SaveIfDirty(type, changed);
    }

    private async Task EnsureSiteSettingsTypeAsync(CancellationToken cancellationToken)
    {
        IContentType type = GetOrCreate(SiteSettingsAlias, "Site settings", allowsAtRoot: true);
        var changed = false;
        changed |= await EnsureTextAsync(type, "siteName", "Site name", cancellationToken);
        changed |= await EnsureTextAsync(type, "tagline", "Tagline", cancellationToken);
        changed |= await EnsureRichtextAsync(type, "aboutBody", "About body", cancellationToken);
        changed |= await EnsureTextAsync(type, "contactEmail", "Contact email", cancellationToken);
        SaveIfDirty(type, changed);
    }

    private IContentType GetOrCreate(string alias, string name, bool allowsAtRoot)
    {
        IContentType? existing = _contentTypeService.Get(alias);
        if (existing is not null)
        {
            return existing;
        }

        IContentType created = CreateType(alias, name, allowsAtRoot);
#pragma warning disable CS0618
        _contentTypeService.Save(created);
#pragma warning restore CS0618
        return created;
    }

    private IContentType CreateType(string alias, string name, bool allowsAtRoot)
    {
        return new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = "icon-document",
            AllowedAsRoot = allowsAtRoot,
            IsElement = false,
            Variations = ContentVariation.Nothing,
        };
    }

    private Task<bool> EnsureTextAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.TextBox, cancellationToken);

    private Task<bool> EnsureTextareaAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.TextArea, cancellationToken);

    private Task<bool> EnsureRichtextAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.RichText, cancellationToken);

    private Task<bool> EnsureDateAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.DateTime, cancellationToken);

    private Task<bool> EnsureContentPickerAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.ContentPicker, cancellationToken);

    private Task<bool> EnsureMediaPickerAsync(IContentType type, string alias, string name, CancellationToken cancellationToken)
        => EnsurePropertyAsync(type, alias, name, Constants.PropertyEditors.Aliases.MediaPicker3, cancellationToken);

    private async Task<bool> EnsurePropertyAsync(
        IContentType type,
        string alias,
        string name,
        string editorAlias,
        CancellationToken cancellationToken)
    {
        if (type.PropertyTypes.Any(p => p.Alias == alias))
        {
            return false;
        }

        IEnumerable<IDataType> dataTypes = await _dataTypeService.GetByEditorAliasAsync(editorAlias);
        IDataType dataType = dataTypes.First()
            ?? throw new InvalidOperationException($"Data type for editor {editorAlias} not found.");

        var propertyType = new PropertyType(_shortStringHelper, dataType, alias)
        {
            Name = name,
            Mandatory = false,
            Variations = ContentVariation.Nothing,
        };
        type.AddPropertyType(propertyType);
        return true;
    }

    private void SaveIfDirty(IContentType type, bool changed)
    {
        if (!changed)
        {
            return;
        }

#pragma warning disable CS0618
        _contentTypeService.Save(type);
#pragma warning restore CS0618
    }
}
