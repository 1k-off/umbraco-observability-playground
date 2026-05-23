using System.Globalization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace UmbracoMonitoring.Bootstrap;

public class DemoLanguageBootstrap
{
    public const string DefaultCulture = "en-US";

    private readonly ILanguageService _languageService;
    private readonly ILogger<DemoLanguageBootstrap> _logger;

    public DemoLanguageBootstrap(ILanguageService languageService, ILogger<DemoLanguageBootstrap> logger)
    {
        _languageService = languageService;
        _logger = logger;
    }

    public async Task EnsureDefaultLanguageAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Guid userKey = Constants.Security.SuperUserKey;
        ILanguage? existing = await _languageService.GetAsync(DefaultCulture);
        if (existing is not null)
        {
            if (!existing.IsDefault)
            {
                existing.IsDefault = true;
                Attempt<ILanguage, LanguageOperationStatus> update =
                    await _languageService.UpdateAsync(existing, userKey);
                if (update.Success)
                {
                    _logger.LogInformation("Set {Culture} as default Umbraco language.", DefaultCulture);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not set default language {Culture}: {Status}",
                        DefaultCulture,
                        update.Status);
                }
            }

            return;
        }

        var language = new Language(DefaultCulture, "English (United States)")
        {
            IsDefault = true,
            IsMandatory = false,
        };

        Attempt<ILanguage, LanguageOperationStatus> create =
            await _languageService.CreateAsync(language, userKey);
        if (create.Success)
        {
            _logger.LogInformation("Created default Umbraco language {Culture}.", DefaultCulture);
        }
        else
        {
            _logger.LogWarning(
                "Could not create default language {Culture}: {Status}",
                DefaultCulture,
                create.Status);
        }
    }

    public static void SetThreadCultures()
    {
        var culture = CultureInfo.GetCultureInfo(DefaultCulture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
