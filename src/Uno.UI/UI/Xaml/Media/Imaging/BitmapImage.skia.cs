using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private static readonly int _cacheMaxEntries = Uno.UI.FeatureConfiguration.Image.MaxBitmapImageCacheCount;
		private static readonly Dictionary<string, LinkedListNode<Task<ImageData>>> _imageCache = new();
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
                        // GetScaledPath uses DisplayInformation so it needs to be called on the UI thread
                        uri = new Uri(await PlatformImageHelpers.GetScaledPath(uri, scaleOverride: null));
					}

					if ((CreateOptions & BitmapCreateOptions.IgnoreImageCache) != 0)
					{
						_imageCache.Remove(uri.PathAndQuery);
					}

					if (!_imageCache.TryGetValue(uri.PathAndQuery, out var cachedTaskNode))
					{
						Debug.Assert(_imageCache.Count <= _cacheMaxEntries);
						if (_imageCache.Count == _cacheMaxEntries)
						{
							_imageCache.Values.First().List!.RemoveLast();
						}
						var tcs = new TaskCompletionSource<ImageData>();
						var list = _imageCache.Count == 0 ? new LinkedList<Task<ImageData>>() : _imageCache.Values.First().List;
						cachedTaskNode = _imageCache[uri.PathAndQuery] = list!.AddFirst(tcs.Task);

						_ = Task.Run(async () =>
						{
							try
							{
								tcs.TrySetResult(await ImageSourceHelpers.GetImageDataFromUriAsCompositionSurface(uri, ct));
							}
							catch (Exception e)
							{
								tcs.TrySetResult(ImageData.FromError(e));
							}
						}, ct);
					}
					else
					{
						var list = cachedTaskNode.List;
						list!.Remove(cachedTaskNode);
						list.AddFirst(cachedTaskNode);
					}

					var imageData = await cachedTaskNode.Value;
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

		internal static string GetScaledPath(string rawPath)
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

			var resolutionScale = (int)DisplayInformation.GetForCurrentView().ResolutionScale;

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
