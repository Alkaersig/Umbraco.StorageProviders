using System;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;

namespace Umbraco.StorageProviders
{
    /// <summary>
    /// A <see cref="IMediaUrlProvider" /> that returns a CDN URL for a media item.
    /// </summary>
    /// <seealso cref="Umbraco.Cms.Core.Routing.DefaultMediaUrlProvider" />
    public sealed class CdnMediaUrlProvider : DefaultMediaUrlProvider
    {
        private Uri _cdnUrl;
        private bool _removeMediaFromPath;
        private string _mediaPath;

        /// <summary>
        /// Creates a new instance of <see cref="CdnMediaUrlProvider" />.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="globalSettings">The global settings.</param>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="mediaPathGenerators">The media path generators.</param>
        /// <param name="uriUtility">The URI utility.</param>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public CdnMediaUrlProvider(IOptionsMonitor<CdnMediaUrlProviderOptions> options, IOptionsMonitor<GlobalSettings> globalSettings, IHostingEnvironment hostingEnvironment, MediaUrlGeneratorCollection mediaPathGenerators, UriUtility uriUtility)
            : base(mediaPathGenerators, uriUtility)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(globalSettings);
            ArgumentNullException.ThrowIfNull(hostingEnvironment);

            _cdnUrl = options.CurrentValue.Url;
            _removeMediaFromPath = options.CurrentValue.RemoveMediaFromPath;
            _mediaPath = hostingEnvironment.ToAbsolute(globalSettings.CurrentValue.UmbracoMediaPath).TrimEnd('/');

            options.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _removeMediaFromPath = options.RemoveMediaFromPath;
                    _cdnUrl = options.Url;
                }
            });

            globalSettings.OnChange((options, name) =>
            {
                if (name == Options.DefaultName)
                {
                    _mediaPath = hostingEnvironment.ToAbsolute(options.UmbracoMediaPath).TrimEnd('/');
                }
            });
        }

        /// <inheritdoc />
        public override UrlInfo? GetMediaUrl(IPublishedContent content, string propertyAlias, UrlMode mode, string culture, Uri current)
        {
            var mediaUrl = base.GetMediaUrl(content, propertyAlias, UrlMode.Relative, culture, current);
            if (mediaUrl?.IsUrl == true)
            {
                string url = mediaUrl.Text;
                if (_removeMediaFromPath && url.StartsWith(_mediaPath, StringComparison.OrdinalIgnoreCase))
                {
                    url = url[_mediaPath.Length..];
                }

                return UrlInfo.Url(_cdnUrl + url, culture);
            }

            return mediaUrl;
        }
    }
}
