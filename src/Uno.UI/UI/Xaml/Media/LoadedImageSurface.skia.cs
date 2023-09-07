using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Composition;
using Windows.Graphics.Display;

namespace Windows.UI.Xaml.Media
{
	public partial class LoadedImageSurface : IDisposable, ICompositionSurface, ISkiaCompositionSurfaceProvider
	{
		private HttpClient _httpClient;
		private double _dpi = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

		internal SkiaCompositionSurface InternalSurface;

		SkiaCompositionSurface ISkiaCompositionSurfaceProvider.SkiaCompositionSurface => InternalSurface;

		internal LoadedImageSurface(Action<LoadedImageSurface> loadAction)
		{
			Task.Run(() => loadAction(this));
		}

		public static LoadedImageSurface StartLoadFromUri(Uri uri)
		{
			var retVal = new LoadedImageSurface(async (LoadedImageSurface imgSurf) =>
			{
				if (uri is not null)
				{
					Stream stream = null;

					if (!uri.IsAbsoluteUri)
						throw new InvalidOperationException($"uri must be absolute");

					try
					{
						if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
							uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
							uri.IsFile)
						{
							stream = await imgSurf.OpenStreamFromUriAsync(uri);
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
					}
					catch
					{
						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.NetworkError));
					}

					if (stream is not null)
					{
						var surface = new SkiaCompositionSurface();
						var result = surface.LoadFromStream(null, null, stream);

						if (result.success)
						{
							imgSurf.InternalSurface = surface;

							imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
							imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Value.Width * imgSurf._dpi, imgSurf._decodedSize.Value.Height * imgSurf._dpi);
							imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
						}

						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(result.success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat));

						stream.Dispose();
					}
					else
					{
						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Other));
					}
				}
				else
					throw new ArgumentNullException("uri");
			});

			return retVal;
		}

		public static LoadedImageSurface StartLoadFromUri(Uri uri, Size desiredMaxSize)
		{
			var retVal = new LoadedImageSurface(async (LoadedImageSurface imgSurf) =>
			{
				if (uri is not null)
				{
					Stream stream = null;

					if (!uri.IsAbsoluteUri)
						throw new InvalidOperationException($"uri must be absolute");

					try
					{
						if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
							uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
							uri.IsFile)
						{
							stream = await imgSurf.OpenStreamFromUriAsync(uri);
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
					}
					catch
					{
						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.NetworkError));
					}

					if (stream is not null)
					{
						var surface = new SkiaCompositionSurface();
						var result = surface.LoadFromStream((int)desiredMaxSize.Width, (int)desiredMaxSize.Height, stream);

						if (result.success)
						{
							imgSurf.InternalSurface = surface;

							imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
							imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Value.Width * imgSurf._dpi, imgSurf._decodedSize.Value.Height * imgSurf._dpi);
							imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
						}

						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(result.success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat));

						stream.Dispose();
					}
					else
					{
						if (imgSurf.LoadCompleted is not null)
							imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Other));
					}
				}
				else
					throw new ArgumentNullException("uri");
			});

			return retVal;
		}

		public static LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream)
		{
			var retVal = new LoadedImageSurface((LoadedImageSurface imgSurf) =>
			{
				var surface = new SkiaCompositionSurface();
				var result = surface.LoadFromStream(null, null, stream.AsStream());

				if (result.success)
				{
					imgSurf.InternalSurface = surface;

					imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
					imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Value.Width * imgSurf._dpi, imgSurf._decodedSize.Value.Height * imgSurf._dpi);
					imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
				}

				if (imgSurf.LoadCompleted is not null)
					imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(result.success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat));
			});

			return retVal;
		}

		public static LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream, Size desiredMaxSize)
		{
			var retVal = new LoadedImageSurface((LoadedImageSurface imgSurf) =>
			{
				var surface = new SkiaCompositionSurface();
				var result = surface.LoadFromStream((int)desiredMaxSize.Width, (int)desiredMaxSize.Height, stream.AsStream());

				if (result.success)
				{
					imgSurf.InternalSurface = surface;

					imgSurf._decodedSize = new Size((double?)surface.Image?.Width ?? 0, (double?)surface.Image?.Height ?? 0);
					imgSurf._decodedPhysicalSize = new Size(imgSurf._decodedSize.Value.Width * imgSurf._dpi, imgSurf._decodedSize.Value.Height * imgSurf._dpi);
					imgSurf._naturalPhysicalSize = imgSurf._decodedPhysicalSize;
				}

				if (imgSurf.LoadCompleted is not null)
					imgSurf.LoadCompleted(imgSurf, new LoadedImageSourceLoadCompletedEventArgs(result.success ? LoadedImageSourceLoadStatus.Success : LoadedImageSourceLoadStatus.InvalidFormat));
			});

			return retVal;
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
			InternalSurface?.Image?.Dispose();
		}

		private async Task<Stream> OpenStreamFromUriAsync(Uri uri)
		{
			if (uri.IsFile)
			{
				return File.Open(uri.LocalPath, FileMode.Open);
			}

			_httpClient ??= new HttpClient();
			var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
			return await response.Content.ReadAsStreamAsync();
		}
	}
}
