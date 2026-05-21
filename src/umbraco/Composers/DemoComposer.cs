using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using UmbracoMonitoring.Bootstrap;
using UmbracoMonitoring.Configuration;

namespace UmbracoMonitoring.Composers;

public class DemoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<DemoOptions>(builder.Config.GetSection(DemoOptions.SectionName));
        builder.Services.AddSingleton<ContentTypeBootstrap>();
        builder.Services.AddHostedService<DemoContentSeedHostedService>();
    }
}
