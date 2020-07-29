using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Transactions;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private protected override bool TryOpenSourceAsync(int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(targetWidth, targetHeight);

			return true;
		}

		private async Task<ImageData> TryOpenSourceAsync(int? targetWidth, int? targetHeight)
		{
			var surface = new SkiaCompositionSurface();

			try
			{
				if (UriSource != null)
				{
					if (UriSource.Scheme == "http" || UriSource.Scheme == "https")
					{

						var client = new HttpClient();
						var response = await client.GetAsync(UriSource);

						var imageStream = await response.Content.ReadAsStreamAsync();
						return OpenFromStream(targetWidth, targetHeight, surface, imageStream);
					}
					else if (UriSource.Scheme == "ms-appx")
					{
						var path = UriSource.PathAndQuery;

						var filePath = global::System.IO.Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path, path.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar));

						using var fileStream = File.OpenRead(filePath);

						return OpenFromStream(targetWidth, targetHeight, surface, fileStream);
					}
				}
				else if (Stream != null)
				{
					return OpenFromStream(targetWidth, targetHeight, surface, Stream);
				}
			}
			catch (Exception e)
			{
				return new ImageData() { Error = e };
			}

			return default;
		}

		private static ImageData OpenFromStream(int? targetWidth, int? targetHeight, SkiaCompositionSurface surface, global::System.IO.Stream imageStream)
		{
			var result = surface.LoadFromStream(targetWidth, targetHeight, imageStream);

			if (result.success)
			{
				return new ImageData { Value = surface };
			}
			else
			{
				return new ImageData { Error = new InvalidOperationException($"Image load failed ({result.nativeResult})") };
			}
		}

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			if(Stream != null &&
				targetWidth is int width && targetHeight is int height && height < 100 && width < 100)
			{
				var surface = new SkiaCompositionSurface();
				image = OpenFromStream(targetWidth, targetHeight, surface, Stream);
				return image.Value != null;
			}

			return base.TryOpenSourceSync(targetWidth, targetHeight, out image);
		}
	}
}
