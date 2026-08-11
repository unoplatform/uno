using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
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
#pragma warning disable CS0067 // The event is never used
		public event DownloadProgressEventHandler DownloadProgress;
#pragma warning restore CS0067 // The event is never used

		public event ExceptionRoutedEventHandler ImageFailed;
		public event RoutedEventHandler ImageOpened;

		#region UriSource DependencyProperty

		public Uri UriSource
		{
			get { return (Uri)GetValue(UriSourceProperty); }
			set { SetValue(UriSourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for UriSource.  This enables animation, styling, binding, etc...
		public static DependencyProperty UriSourceProperty { get; } =
			DependencyProperty.Register("UriSource", typeof(Uri), typeof(BitmapImage), new FrameworkPropertyMetadata(null, (s, e) => ((BitmapImage)s)?.OnUriSourceChanged(e)));

		private void OnUriSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!object.Equals(e.OldValue, e.NewValue))
			{
				UnloadImageData();
			}
			InitFromUri(e.NewValue as Uri);
#if UNO_REFERENCE_API
			InvalidateSource();
#endif
			InvalidateImageSource();
		}

		#endregion

		#region DecodePixelType DependencyProperty

		public DecodePixelType DecodePixelType
		{
			get { return (DecodePixelType)GetValue(DecodePixelTypeProperty); }
			set { SetValue(DecodePixelTypeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelType.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelTypeProperty { get; } =
			DependencyProperty.Register("DecodePixelType", typeof(DecodePixelType), typeof(BitmapImage), new FrameworkPropertyMetadata(DecodePixelType.Physical, (s, e) => ((BitmapImage)s)?.OnDecodePixelTypeChanged(e)));


		private void OnDecodePixelTypeChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region DecodePixelWidth DependencyProperty

		public int DecodePixelWidth
		{
			get { return (int)GetValue(DecodePixelWidthProperty); }
			set { SetValue(DecodePixelWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelWidth.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelWidthProperty { get; } =
			DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(BitmapImage), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapImage)s)?.OnDecodePixelWidthChanged(e)));


		private void OnDecodePixelWidthChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region DecodePixelHeight DependencyProperty

		public int DecodePixelHeight
		{
			get { return (int)GetValue(DecodePixelHeightProperty); }
			set { SetValue(DecodePixelHeightProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelHeight.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelHeightProperty { get; } =
			DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(BitmapImage), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapImage)s)?.OnDecodePixelHeightChanged(e)));


		private void OnDecodePixelHeightChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region CreateOptions DependencyProperty

		public BitmapCreateOptions CreateOptions
		{
			get { return (BitmapCreateOptions)GetValue(CreateOptionsProperty); }
			set { SetValue(CreateOptionsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CreateOptions.  This enables animation, styling, binding, etc...
		public static DependencyProperty CreateOptionsProperty { get; } =
			DependencyProperty.Register("CreateOptions", typeof(BitmapCreateOptions), typeof(BitmapImage), new FrameworkPropertyMetadata(BitmapCreateOptions.None, (s, e) => ((BitmapImage)s)?.OnCreateOptionsChanged(e)));


		private void OnCreateOptionsChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		public BitmapImage(Uri uriSource) : base(uriSource)
		{
			UriSource = uriSource;
		}

		public BitmapImage() { }

		internal void RaiseImageFailed(Exception ex)
		{
			ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, ex.Message));
		}

		internal void RaiseImageOpened()
		{
			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
		}

		private readonly record struct BitmapImageCacheKey(Uri Uri, int? DecodeWidth, int? DecodeHeight);

		private static readonly LRUCache<BitmapImageCacheKey, Task<ImageData>> _bitmapImageCache = new(FeatureConfiguration.Image.MaxBitmapImageCacheCount);
		// TODO: Introduce LRU caching if needed
		private static readonly Dictionary<string, string> _scaledBitmapPathCache = new();

		private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(ct);

			return true;
		}

		private (int? targetWidth, int? targetHeight) GetDecodePixelSize()
		{
			var width = DecodePixelWidth;
			var height = DecodePixelHeight;

			if (width == 0 && height == 0)
			{
				return (null, null);
			}

			if (DecodePixelType == DecodePixelType.Logical)
			{
				var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
				if (width > 0)
				{
					width = (int)Math.Round(width * scale);
				}
				if (height > 0)
				{
					height = (int)Math.Round(height * scale);
				}
			}

			return (width > 0 ? width : null, height > 0 ? height : null);
		}

		private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct)
		{
			try
			{
				var (decodeWidth, decodeHeight) = GetDecodePixelSize();
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
						ImageData imageData;
						try
						{
							imageData = await Task.Run(async () =>
							{
								try
								{
									using (clonedStream)
									{
										return await ImageSourceHelpers.ReadFromStreamAsCompositionSurface(clonedStream, ct, targetWidth: decodeWidth, targetHeight: decodeHeight);
									}
								}
								catch (Exception e)
								{
									return ImageData.FromError(e);
								}
							}, ct);
						}
						catch (OperationCanceledException)
						{
							// If ct is already canceled, Task.Run never executes the
							// delegate and clonedStream would leak.
							clonedStream.Dispose();
							throw;
						}

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
					var cacheKey = new BitmapImageCacheKey(uri, decodeWidth, decodeHeight);

					if (ignoreCache
						|| !_bitmapImageCache.TryGetValue(cacheKey, out var imageDataTask))
					{
						imageDataTask = Task.Run(async () =>
						{
							try
							{
								return await ImageSourceHelpers.GetImageDataFromUriAsCompositionSurface(uri, ct, decodeWidth, decodeHeight);
							}
							catch (Exception e)
							{
								return ImageData.FromError(e);
							}
						}, ct);

						if (FeatureConfiguration.Image.EnableBitmapImageCache)
						{
							_bitmapImageCache.Add(cacheKey, imageDataTask);
							// if loading failed not because of an actual failure but because
							// the task was canceled (usually because the Uri changed), we
							// don't want to cache the failed task
							ct.Register(() => _bitmapImageCache.Remove(cacheKey));
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
