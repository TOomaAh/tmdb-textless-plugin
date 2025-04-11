using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TextlessImages.Tmdb;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TMDbLib.Objects.Find;

namespace Jellyfin.Plugin.TextlessImages.Tmdb.Movies
{
    /// <summary>
    /// Movie image provider powered by TMDb.
    /// </summary>
    public class TmdbMovieImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TextlessClientManager _tmdbClientManager;
        private readonly ILogger<TmdbMovieImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbMovieImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TextlessClientManager"/>.</param>
        /// <param name="logger">The <see cref="ILogger{TmdbMovieImageProvider}"/>.</param>
        public TmdbMovieImageProvider(IHttpClientFactory httpClientFactory, TextlessClientManager tmdbClientManager, ILogger<TmdbMovieImageProvider> logger)
        {
            logger.LogWarning("TmdbMovieImageProvider constructor called");
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => 0;

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Movie || item is Trailer;
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
            var excludeTextLessImages = Plugin.Instance?.Configuration.ExcludeTextLessImages ?? false;

            _logger.LogWarning("GetImages called with {ExcludeTextLessImages}", excludeTextLessImages);

            var language = item.GetPreferredMetadataLanguage();

            var movieTmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            if (movieTmdbId <= 0)
            {
                var movieImdbId = item.GetProviderId(MetadataProvider.Imdb);
                if (string.IsNullOrEmpty(movieImdbId))
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var movieResult = await _tmdbClientManager.FindByExternalIdAsync(movieImdbId, FindExternalSource.Imdb, language, cancellationToken).ConfigureAwait(false);
                if (movieResult?.MovieResults is not null && movieResult.MovieResults.Count > 0)
                {
                    movieTmdbId = movieResult.MovieResults[0].Id;
                }
            }

            if (movieTmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var movie = await _tmdbClientManager
                .GetMovieAsync(movieTmdbId, null, null, cancellationToken)
                .ConfigureAwait(false);

            if (movie?.Images is null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = movie.Images.Posters;

            if (excludeTextLessImages && posters.Any(p => p.Iso_639_1 is not null))
            {
                posters = posters.Where(p => p.Iso_639_1 is not null).ToList();
            }

            var backdrops = movie.Images.Backdrops;
            var logos = movie.Images.Logos;
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
