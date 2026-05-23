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

    public void EnsureDocumentTypes()
    {
        EnsureCategoryType();
        EnsureAuthorType();
        EnsureArticleType();
        EnsureSiteSettingsType();
    }

    private void EnsureCategoryType()
    {
        IContentType type = GetOrCreate(CategoryAlias, "Category", allowsAtRoot: true);
        var changed = false;
        changed |= EnsureText(type, "name", "Name");
        changed |= EnsureText(type, "slug", "Slug");
        changed |= EnsureTextarea(type, "description", "Description");
        SaveIfDirty(type, changed);
    }

    private void EnsureAuthorType()
    {
        IContentType type = GetOrCreate(AuthorAlias, "Author", allowsAtRoot: true);
        var changed = false;
        changed |= EnsureText(type, "name", "Name");
        changed |= EnsureText(type, "slug", "Slug");
        changed |= EnsureTextarea(type, "bio", "Bio");
        changed |= EnsureMediaPicker(type, "avatar", "Avatar");
        SaveIfDirty(type, changed);
    }

    private void EnsureArticleType()
    {
        IContentType type = GetOrCreate(ArticleAlias, "Article", allowsAtRoot: true);
        var changed = false;
        changed |= EnsureText(type, "title", "Title");
        changed |= EnsureText(type, "slug", "Slug");
        changed |= EnsureTextarea(type, "excerpt", "Excerpt");
        changed |= EnsureRichtext(type, "body", "Body");
        changed |= EnsureDate(type, "publishDate", "Publish date");
        changed |= EnsureMediaPicker(type, "featuredImage", "Featured image");
        changed |= EnsureContentPicker(type, "category", "Category");
        changed |= EnsureContentPicker(type, "author", "Author");
        SaveIfDirty(type, changed);
    }

    private void EnsureSiteSettingsType()
    {
        IContentType type = GetOrCreate(SiteSettingsAlias, "Site settings", allowsAtRoot: true);
        var changed = false;
        changed |= EnsureText(type, "siteName", "Site name");
        changed |= EnsureText(type, "tagline", "Tagline");
        changed |= EnsureRichtext(type, "aboutBody", "About body");
        changed |= EnsureText(type, "contactEmail", "Contact email");
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

    private bool EnsureText(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.TextBox);

    private bool EnsureTextarea(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.TextArea);

    private bool EnsureRichtext(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.RichText);

    private bool EnsureDate(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.DateTime);

    private bool EnsureContentPicker(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.ContentPicker);

    private bool EnsureMediaPicker(IContentType type, string alias, string name)
        => EnsureProperty(type, alias, name, Constants.PropertyEditors.Aliases.MediaPicker3);

    private bool EnsureProperty(IContentType type, string alias, string name, string editorAlias)
    {
        if (type.PropertyTypes.Any(p => p.Alias == alias))
        {
            return false;
        }

        IDataType dataType = _dataTypeService.GetByEditorAlias(editorAlias).First()
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
