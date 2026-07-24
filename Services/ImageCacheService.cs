using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TTXEquipamentos.Services
{
    /// <summary>
    /// Service for lazy-loading and caching images to optimize memory usage.
    /// Prevents loading all images at once, which causes memory bloat on weak PCs.
    /// </summary>
    public interface IImageCacheService
    {
        Task<BitmapImage?> GetImageAsync(string? filePath);
        Task<BitmapImage?> GetThumbnailAsync(string? filePath, int maxWidth = 150, int maxHeight = 150);
        void ClearCache();
        void RemoveFromCache(string? filePath);
    }

    public class ImageCacheService : IImageCacheService
    {
        private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
        private readonly ConcurrentDictionary<string, BitmapImage> _thumbnailCache = new();
        private const int MaxCacheSize = 50; // Max 50 images in memory
        private int _cacheHits = 0;

        /// <summary>
        /// Get image with lazy loading - only loads when requested
        /// </summary>
        public async Task<BitmapImage?> GetImageAsync(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            // Return from cache if already loaded
            if (_imageCache.TryGetValue(filePath, out var cachedImage))
            {
                _cacheHits++;
                return cachedImage;
            }

            try
            {
                // Load image on background thread to avoid UI freezing
                var image = await Task.Run(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load into memory, release file lock
                    bitmap.DecodePixelWidth = 800; // Limit resolution to reduce memory
                    bitmap.EndInit();
                    bitmap.Freeze(); // Make thread-safe and improve performance
                    return bitmap;
                });

                // Add to cache, but remove oldest if cache is too large
                if (_imageCache.Count >= MaxCacheSize)
                {
                    var firstKey = _imageCache.Keys.FirstOrDefault();
                    if (firstKey != null)
                        _imageCache.TryRemove(firstKey, out _);
                }

                _imageCache[filePath] = image;
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get thumbnail for UI display - much smaller than full image
        /// </summary>
        public async Task<BitmapImage?> GetThumbnailAsync(string? filePath, int maxWidth = 150, int maxHeight = 150)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            string cacheKey = $"{filePath}_thumb_{maxWidth}x{maxHeight}";

            if (_thumbnailCache.TryGetValue(cacheKey, out var cachedThumb))
                return cachedThumb;

            try
            {
                var thumbnail = await Task.Run(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = maxWidth;
                    bitmap.DecodePixelHeight = maxHeight;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                });

                if (_thumbnailCache.Count >= MaxCacheSize * 2)
                {
                    var firstKey = _thumbnailCache.Keys.FirstOrDefault();
                    if (firstKey != null)
                        _thumbnailCache.TryRemove(firstKey, out _);
                }

                _thumbnailCache[cacheKey] = thumbnail;
                return thumbnail;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading thumbnail {filePath}: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            _imageCache.Clear();
            _thumbnailCache.Clear();
            GC.Collect(); // Force garbage collection to free memory
        }

        public void RemoveFromCache(string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _imageCache.TryRemove(filePath, out _);
                // Also remove any thumbnails of this image
                var thumbKeys = _thumbnailCache.Keys.Where(k => k.StartsWith(filePath));
                foreach (var key in thumbKeys)
                    _thumbnailCache.TryRemove(key, out _);
            }
        }
    }
}
