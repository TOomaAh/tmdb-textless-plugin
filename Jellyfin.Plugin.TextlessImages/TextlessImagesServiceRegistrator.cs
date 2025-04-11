using Jellyfin.Plugin.TextlessImages.Tmdb;
using Jellyfin.Plugin.TextlessImages.Tmdb.Movies;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TextlessImages
{
    /// <summary>
    /// The plugin service registrator.
    /// </summary>
    public class TextlessImagesServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<TextlessClientManager>();
        }
    }
}
