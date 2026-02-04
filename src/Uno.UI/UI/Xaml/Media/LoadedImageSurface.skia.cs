#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Composition;
using Windows.Graphics.Display;
using Uno.UI.Dispatching;
using Uno.Helpers;
using System.Threading;

namespace Microsoft.UI.Xaml.Media
{
	public partial class LoadedImageSurface : IDisposable, ICompositionSurface, ISkiaCompositionSurfaceProvider
	{
		private double _dpi = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

		internal SkiaCompositionSurface? InternalSurface;

		SkiaCompositionSurface? ISkiaCompositionSurfaceProvider.SkiaCompositionSurface => InternalSurface;

		internal LoadedImageSurface(Action<LoadedImageSurface> loadAction)
		{
			Task.Run(() => loadAction(this));
		}

		public static LoadedImageSurface StartLoadFromUri(Uri uri) => StartLoadFromUri(uri, null, null);
		public static LoadedImageSurface StartLoadFromUri(Uri uri, Size desiredMaxSize) => StartLoadFromUri(uri, (int)desiredMaxSize.Width, (int)desiredMaxSize.Height);

		private void RaiseLoadCompleted(LoadedImageSourceLoadStatus status)
		{
			if (LoadCompleted is not null)
			{
				NativeDispatcher.Main.Enqueue(() => LoadCompleted.Invoke(this, new(status)));
			}
		}

		private static LoadedImageSurface StartLoadFromUri(Uri uri, int? width, int? height)
		{
			var retVal = new LoadedImageSurface(async (LoadedImageSurface imgSurf) =>
			{
				if (uri is not null)
				{
					Stream? stream = null;

					if (!uri.IsAbsoluteUri)
					{
						throw new InvalidOperationException($"uri must be absolute");
					}

					try
					{
						if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
							uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
							uri.IsFile)
						{
							stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
						}
						else if (uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
						{
							var path = uri.PathAndQuery;

							if (uri.Host is { Length: > 0 } host)
							{
								path = host + "/" + path.TrimStart('/');
							}

							var filePath = BitmapImage.GetScaledPath(path);
							stream = File.OpenRead(filePath);
						}
						else if (uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase) && uri.IsFile)
						{
							stream = File.OpenRead(uri.PathAndQuery);
						}

						if (stream is not null)
						{
							var surface = new SkiaCompositionSurface();
							var (success, _) = surface.LoadFromStream(width, height, stream);

							if (success)
							{
								imgSurf.InternalSurface = surface;

								imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
								imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Width * imgSurf._dpi, imgSurf._decodedSize.Height * imgSurf._dpi);
								imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
							}

							imgSurf.RaiseLoadCompleted(success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat);
						}
						else
						{
							imgSurf.RaiseLoadCompleted(LoadedImageSourceLoadStatus.Other);
						}
					}
					catch
					{
						imgSurf.RaiseLoadCompleted(LoadedImageSourceLoadStatus.NetworkError);
					}
					finally
					{
						stream?.Dispose();
					}
				}
				else
				{
					throw new ArgumentNullException("uri");
				}
			});

			return retVal;
		}

		public static LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream) => StartLoadFromStream(stream, null, null);
		public static LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream, Size desiredMaxSize) => StartLoadFromStream(stream, (int)desiredMaxSize.Width, (int)desiredMaxSize.Height);

		private static LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream, int? width, int? height)
		{
			var retVal = new LoadedImageSurface((LoadedImageSurface imgSurf) =>
			{
				var surface = new SkiaCompositionSurface();
				var result = surface.LoadFromStream(width, height, stream.AsStream());

				if (result.success)
				{
					imgSurf.InternalSurface = surface;

					imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
					imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Width * imgSurf._dpi, imgSurf._decodedSize.Height * imgSurf._dpi);
					imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
				}

				imgSurf.RaiseLoadCompleted(result.success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat);
			});

			return retVal;
		}

		public void Dispose()
		{
			InternalSurface?.Image?.Dispose();
		}
	}
}
