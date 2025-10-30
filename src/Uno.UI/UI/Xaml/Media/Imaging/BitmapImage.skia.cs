using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private static readonly LRUCache<Uri, Task<ImageData>> _bitmapImageCache = new(FeatureConfiguration.Image.MaxBitmapImageCacheCount);
		// TODO: Introduce LRU caching if needed
		private static readonly Dictionary<string, string> _scaledBitmapPathCache = new();

		private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(ct);

			return true;
		}

		private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
		{
			try
			{
				var uri = UriSource;
				if (uri is null)
				{
					if (_stream is null)
					{
						return ImageData.Empty;
					}
					else
					{
						var clonedStream = _stream.CloneStream().AsStreamForRead();
						var tcs = new TaskCompletionSource<ImageData>();
						_ = Task.Run(async () =>
						{
							try
							{
								tcs.TrySetResult(await ImageSourceHelpers.ReadFromStreamAsCompositionSurface(clonedStream, ct));
							}
							catch (Exception e)
							{
								tcs.TrySetResult(ImageData.FromError(e));
							}
						}, ct);

						return await tcs.Task;
					}
				}
				else
				{
					if (!uri.IsAbsoluteUri)
					{
						return ImageData.FromError(new InvalidOperationException($"UriSource must be absolute"));
					}

					if (uri.IsLocalResource())
					{
						uri = await TryResolveLocalResource(uri);
					}

					var ignoreCache = CreateOptions.HasFlag(BitmapCreateOptions.IgnoreImageCache);

					if (ignoreCache
						|| !_bitmapImageCache.TryGetValue(uri, out var imageDataTask))
					{
						imageDataTask = Task.Run(async () =>
						{
							try
							{
								return await ImageSourceHelpers.GetImageDataFromUriAsCompositionSurface(uri, ct);
							}
							catch (Exception e)
							{
								return ImageData.FromError(e);
							}
						}, ct);

						if (FeatureConfiguration.Image.EnableBitmapImageCache)
						{
							_bitmapImageCache.Add(uri, imageDataTask);
							// if loading failed not because of an actual failure but because
							// the task was canceled (usually because the Uri changed), we
							// don't want to cache the failed task
							ct.Register(() => _bitmapImageCache.Remove(uri));
						}
					}

					var imageData = await imageDataTask;

					if (imageData.Kind == ImageDataKind.Error)
					{
						PixelWidth = 0;
						PixelHeight = 0;
						RaiseImageFailed(imageData.Error);
					}
					else if (imageData.Kind == ImageDataKind.CompositionSurface)
					{
						var image = imageData.CompositionSurface.Image;
						PixelWidth = image.Width;
						PixelHeight = image.Height;
						RaiseImageOpened();
					}

					return imageData;
				}
			}
			catch (Exception e)
			{
				return ImageData.FromError(e);
			}
		}

		private static readonly int[] KnownScales =
		{
			(int)ResolutionScale.Scale100Percent,
			(int)ResolutionScale.Scale120Percent,
			(int)ResolutionScale.Scale125Percent,
			(int)ResolutionScale.Scale140Percent,
			(int)ResolutionScale.Scale150Percent,
			(int)ResolutionScale.Scale160Percent,
			(int)ResolutionScale.Scale175Percent,
			(int)ResolutionScale.Scale180Percent,
			(int)ResolutionScale.Scale200Percent,
			(int)ResolutionScale.Scale225Percent,
			(int)ResolutionScale.Scale250Percent,
			(int)ResolutionScale.Scale300Percent,
			(int)ResolutionScale.Scale350Percent,
			(int)ResolutionScale.Scale400Percent,
			(int)ResolutionScale.Scale450Percent,
			(int)ResolutionScale.Scale500Percent
		};

		internal static async Task<Uri> TryResolveLocalResource(Uri uri, ResolutionScale? scaleOverride = null)
		{
			if (!uri.IsLocalResource())
			{
				return uri;
			}

			// GetScaledPath uses DisplayInformation so it needs to be called on the UI thread
			if (OperatingSystem.IsIOS())
			{
				// For iOS, [Uno.netcoremobile]\PlatformImageHelpers.GetScaledPath just returns the input uri back as is.
				// Presumably under the assumption that the native control will handle the resultion of @2x @3x assets accordingly.
				// However, on skia-ios, this is handled by uno (right here),
				// so we need to resolve the appropriate asset here, with the scale-XYZ qualifier if available.

				var path = uri.PathAndQuery;
				if (uri.Host is { Length: > 0 } host)
				{
					path = host + "/" + path.TrimStart('/');
				}

				return new Uri(GetScaledPath(path, scaleOverride));
			}
			else
			{
				return new Uri(await PlatformImageHelpers.GetScaledPath(uri, scaleOverride));
			}
		}

		internal static string GetScaledPath(string rawPath, ResolutionScale? scaleOverride = null)
		{
			// Avoid querying filesystem if we already seen this file
			if (_scaledBitmapPathCache.TryGetValue(rawPath, out var result))
			{
				return result;
			}

			var originalLocalPath =
				Path.Combine(Package.Current.InstalledPath,
					 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
				);

			var resolutionScale = (int)(scaleOverride ?? DisplayInformation.GetForCurrentView().ResolutionScale);

			var baseDirectory = Path.GetDirectoryName(originalLocalPath);
			var baseFileName = Path.GetFileNameWithoutExtension(originalLocalPath);
			var baseExtension = Path.GetExtension(originalLocalPath);

			var applicableScale = FindApplicableScale(true);
			if (applicableScale is null)
			{
				applicableScale = FindApplicableScale(false);
			}

			result = applicableScale ?? originalLocalPath;
			_scaledBitmapPathCache[rawPath] = result;
			return result;

			string FindApplicableScale(bool onlyMatching)
			{
				for (var i = KnownScales.Length - 1; i >= 0; i--)
				{
					var probeScale = KnownScales[i];

					if ((onlyMatching && resolutionScale >= probeScale) ||
						(!onlyMatching && resolutionScale < probeScale))
					{
						var filePath = Path.Combine(baseDirectory, $"{baseFileName}.scale-{probeScale}{baseExtension}");

						if (File.Exists(filePath))
						{
							return filePath;
						}
					}
				}

				return null;
			}
		}
	}
}
