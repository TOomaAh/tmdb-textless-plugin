using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TextlessImages.Tmdb.TV
{
    /// <summary>
    /// TV series image provider powered by TheMovieDb.
    /// </summary>
    public class TmdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly ILogger<TmdbSeriesImageProvider> _logger;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TextlessClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbSeriesImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TextlessClientManager"/>.</param>
        /// <param name="logger">The <see cref="ILogger{TmdbSeriesImageProvider}"/>.</param>
        public TmdbSeriesImageProvider(IHttpClientFactory httpClientFactory, TextlessClientManager tmdbClientManager, ILogger<TmdbSeriesImageProvider> logger)
        {
            logger.LogWarning("TmdbSeriesImageProvider constructor called");
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public int Order => 2;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
        [
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Logo,
            ImageType.Thumb
        ];

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            bool excludeTextLessImages = Plugin.Instance?.Configuration.ExcludeTextLessImages ?? false;
            _logger.LogWarning("GetImages called with {ExcludeTextLessImages}", excludeTextLessImages);
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var series = await _tmdbClientManager
                .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), null, null, cancellationToken)
                .ConfigureAwait(false);

            if (series?.Images is null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = series.Images.Posters;
            if (excludeTextLessImages && posters.Any(p => p.Iso_639_1 is not null))
            {
                posters = [.. posters.Where(p => p.Iso_639_1 is not null)];
            }

            var backdrops = series.Images.Backdrops;
            var logos = series.Images.Logos;
            var remoteImages = new List<RemoteImageInfo>(posters.Count + backdrops.Count + logos.Count);

            remoteImages.AddRange(_tmdbClientManager.ConvertPostersToRemoteImageInfo(posters, language));
            remoteImages.AddRange(_tmdbClientManager.ConvertBackdropsToRemoteImageInfo(backdrops, language));
            remoteImages.AddRange(_tmdbClientManager.ConvertLogosToRemoteImageInfo(logos, language));

            return remoteImages;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
