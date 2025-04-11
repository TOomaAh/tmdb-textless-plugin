using Jellyfin.Plugin.TextlessImages.Tmdb;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.TextlessImages.Tmdb.People
{
    /// <summary>
    /// External id for a TMDb person.
    /// </summary>
    public class TmdbPersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public string Key => MediaBrowser.Model.Entities.MetadataProvider.Tmdb.ToString();

        /// <inheritdoc />
        public MediaBrowser.Model.Providers.ExternalIdMediaType? Type => MediaBrowser.Model.Providers.ExternalIdMediaType.Person;

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "person/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is MediaBrowser.Controller.Entities.Person;
        }
    }
}
