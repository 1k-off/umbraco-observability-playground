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
        if (_contentTypeService.Get(ArticleAlias) is not null)
        {
            return;
        }

        IContentType category = CreateType(CategoryAlias, "Category", allowsAtRoot: true);
        AddText(category, "name", "Name");
        AddText(category, "slug", "Slug");
        AddTextarea(category, "description", "Description");
        Save(category);

        IContentType author = CreateType(AuthorAlias, "Author", allowsAtRoot: true);
        AddText(author, "name", "Name");
        AddText(author, "slug", "Slug");
        AddTextarea(author, "bio", "Bio");
        Save(author);

        IContentType article = CreateType(ArticleAlias, "Article", allowsAtRoot: true);
        AddText(article, "title", "Title");
        AddText(article, "slug", "Slug");
        AddTextarea(article, "excerpt", "Excerpt");
        AddRichtext(article, "body", "Body");
        AddDate(article, "publishDate", "Publish date");
        AddContentPicker(article, "category", "Category");
        AddContentPicker(article, "author", "Author");
        Save(article);

        IContentType siteSettings = CreateType(SiteSettingsAlias, "Site settings", allowsAtRoot: true);
        AddText(siteSettings, "siteName", "Site name");
        AddText(siteSettings, "tagline", "Tagline");
        AddRichtext(siteSettings, "aboutBody", "About body");
        AddText(siteSettings, "contactEmail", "Contact email");
        Save(siteSettings);
    }

    private IContentType CreateType(string alias, string name, bool allowsAtRoot)
    {
        var type = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = "icon-document",
            AllowedAsRoot = allowsAtRoot,
            IsElement = false,
            Variations = ContentVariation.Nothing,
        };
        return type;
    }

    private void AddText(IContentType type, string alias, string name)
        => AddProperty(type, alias, name, Constants.PropertyEditors.Aliases.TextBox);

    private void AddTextarea(IContentType type, string alias, string name)
        => AddProperty(type, alias, name, Constants.PropertyEditors.Aliases.TextArea);

    private void AddRichtext(IContentType type, string alias, string name)
        => AddProperty(type, alias, name, Constants.PropertyEditors.Aliases.RichText);

    private void AddDate(IContentType type, string alias, string name)
        => AddProperty(type, alias, name, Constants.PropertyEditors.Aliases.DateTime);

    private void AddContentPicker(IContentType type, string alias, string name)
        => AddProperty(type, alias, name, Constants.PropertyEditors.Aliases.ContentPicker);

    private void AddProperty(IContentType type, string alias, string name, string editorAlias)
    {
        IDataType dataType = _dataTypeService.GetByEditorAlias(editorAlias).First()
            ?? throw new InvalidOperationException($"Data type for editor {editorAlias} not found.");

        var propertyType = new PropertyType(_shortStringHelper, dataType, alias)
        {
            Name = name,
            Mandatory = false,
            Variations = ContentVariation.Nothing,
        };
        type.AddPropertyType(propertyType);
    }

    private void Save(IContentType type)
    {
#pragma warning disable CS0618
        _contentTypeService.Save(type);
#pragma warning restore CS0618
    }
}
