using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace UmbracoMonitoring.Bootstrap;

public class DemoMediaImporter
{
    public const string SeedMediaFolderName = "Demo Seed Media";

    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<DemoMediaImporter> _logger;

    public DemoMediaImporter(
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IWebHostEnvironment webHostEnvironment,
        ILogger<DemoMediaImporter> logger)
    {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public DemoMediaSets ImportSeedFiles()
    {
        string seedRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "seed-media");
        if (!Directory.Exists(seedRoot))
        {
            throw new DirectoryNotFoundException($"Seed media folder not found: {seedRoot}");
        }

        IMedia folder = EnsureSeedFolder();

        var featured = ImportFiles(folder, Path.Combine(seedRoot, "featured"), "featured").ToList();
        var avatars = ImportFiles(folder, Path.Combine(seedRoot, "avatars"), "avatar").ToList();

        _logger.LogInformation(
            "Imported {Featured} featured and {Avatars} avatar media items.",
            featured.Count,
            avatars.Count);

        return new DemoMediaSets(featured, avatars);
    }

    private IMedia EnsureSeedFolder()
    {
        IEnumerable<IMedia> rootMedia = _mediaService.GetRootMedia();
        IMedia? existing = rootMedia.FirstOrDefault(m =>
            m.Name == SeedMediaFolderName && m.ContentType.Alias == Constants.Conventions.MediaTypes.Folder);

        if (existing is not null)
        {
            return existing;
        }

        IMedia folder = _mediaService.CreateMedia(
            SeedMediaFolderName,
            Constants.System.Root,
            Constants.Conventions.MediaTypes.Folder);
        _mediaService.Save(folder);
        return folder;
    }

    private IEnumerable<IMedia> ImportFiles(IMedia parent, string directory, string namePrefix)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Seed media directory missing: {Directory}", directory);
            yield break;
        }

        foreach (string filePath in Directory.GetFiles(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
            {
                continue;
            }

            string fileName = Path.GetFileName(filePath);
            string mediaName = $"{namePrefix}-{Path.GetFileNameWithoutExtension(filePath)}";

            using FileStream stream = System.IO.File.OpenRead(filePath);
            IMedia media = _mediaService.CreateMedia(mediaName, parent, Constants.Conventions.MediaTypes.Image);
            media.SetValue(
                _mediaFileManager,
                _mediaUrlGenerators,
                _shortStringHelper,
                _contentTypeBaseServiceProvider,
                Constants.Conventions.Media.File,
                fileName,
                stream);
            _mediaService.Save(media);
            yield return media;
        }
    }
}

public sealed record DemoMediaSets(IReadOnlyList<IMedia> Featured, IReadOnlyList<IMedia> Avatars);
