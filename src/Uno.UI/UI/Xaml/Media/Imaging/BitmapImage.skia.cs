using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private const int MIN_DIMENSION_SYNC_LOADING = 100;

		private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = TryOpenSourceAsync(ct, targetWidth, targetHeight);

			return true;
		}

		private async Task<ImageData> TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight)
		{
			var surface = new SkiaCompositionSurface();

			try
			{
				if (UriSource != null)
				{
					if (UriSource.Scheme == "http" || UriSource.Scheme == "https")
					{
						var client = new HttpClient();
						var response = await client.GetAsync(UriSource, HttpCompletionOption.ResponseContentRead, ct);
						var imageStream = await response.Content.ReadAsStreamAsync();

						return OpenFromStream(targetWidth, targetHeight, surface, imageStream);
					}
					else if (UriSource.Scheme == "ms-appx")
					{
						var path = UriSource.PathAndQuery;
						var filePath = GetScaledPath(path);
						using var fileStream = File.OpenRead(filePath);

						return OpenFromStream(targetWidth, targetHeight, surface, fileStream);
					}
				}
				else if (_stream != null)
				{
					return OpenFromStream(targetWidth, targetHeight, surface, _stream.AsStream());
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
			if(_stream != null &&
				targetWidth is { } width &&
				targetHeight is { } height &&
				height < MIN_DIMENSION_SYNC_LOADING &&
				width < MIN_DIMENSION_SYNC_LOADING)
			{
				var surface = new SkiaCompositionSurface();
				image = OpenFromStream(targetWidth, targetHeight, surface, _stream.AsStream());
				return image.Value != null;
			}

			return base.TryOpenSourceSync(targetWidth, targetHeight, out image);
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

		private static string GetScaledPath(string rawPath)
		{
			var originalLocalPath =
				Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path,
					 rawPath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
				);

			var resolutionScale = (int)DisplayInformation.GetForCurrentView().ResolutionScale;

			var baseDirectory = Path.GetDirectoryName(originalLocalPath);
			var baseFileName = Path.GetFileNameWithoutExtension(originalLocalPath);
			var baseExtension = Path.GetExtension(originalLocalPath);

			for (var i = KnownScales.Length - 1; i >= 0; i--)
			{
				var probeScale = KnownScales[i];

				if (resolutionScale >= probeScale)
				{
					var filePath = Path.Combine(baseDirectory, $"{baseFileName}.scale-{probeScale}{baseExtension}");

					if (File.Exists(filePath))
					{
						return filePath;
					}
				}
			}

			return originalLocalPath;
		}
	}
}
