using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;
using SixLabors.ImageSharp.Web.Resolvers.Azure;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob.ImageSharp;

/// <summary>
/// Implements an Azure Blob Storage based cache storing files in a <c>cache</c> subfolder.
/// </summary>
public sealed class AzureBlobFileSystemImageCache : IImageCache
{
    private readonly string? _containerRootPath;
    private BlobContainerClient _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="name">The name.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <c>null</c>.</exception>
    public AzureBlobFileSystemImageCache(IOptionsMonitor<AzureBlobFileSystemOptions> options, string name, string? containerRootPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(name);

        var fileSystemOptions = options.Get(name);
        _container = new BlobContainerClient(fileSystemOptions.ConnectionString, fileSystemOptions.ContainerName);

        options.OnChange((options, changedName) =>
        {
            if (changedName == name)
            {
                _container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
            }
        });

        if (!string.IsNullOrEmpty(containerRootPath))
        {
            _containerRootPath = containerRootPath.EnsureEndsWith('/');
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobFileSystemImageCache" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The blob container client.</param>
    /// <param name="containerRootPath">The container root path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="blobContainerClient" /> is <c>null</c>.</exception>
    public AzureBlobFileSystemImageCache(BlobContainerClient blobContainerClient, string? containerRootPath)
    {
        _container = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

        if (!string.IsNullOrEmpty(containerRootPath))
        {
            _containerRootPath = containerRootPath.EnsureEndsWith('/');
        }
    }

    /// <inheritdoc />
    public async Task<IImageCacheResolver?> GetAsync(string key)
    {
        var blob = _container.GetBlobClient(_containerRootPath + key);

        return !await blob.ExistsAsync().ConfigureAwait(false)
            ? null
            : new AzureBlobStorageCacheResolver(blob);
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {
        var blob = _container.GetBlobClient(_containerRootPath + key);

        await blob.UploadAsync(stream, new BlobUploadOptions()
        {
            Metadata = metadata.ToDictionary()
        }).ConfigureAwait(false);
    }
}
