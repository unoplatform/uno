#nullable enable


using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal partial class SkiaCompositionSurface : CompositionObject, ICompositionSurface
	{
		// Don't use field directly. Instead, use Image property.
		private SKImage? _image;

		public SKImage? Image
		{
			get => _image;
			set
			{
				_image = value;
				OnPropertyChanged(nameof(Image), isSubPropertyChange: false);
			}
		}

		internal SkiaCompositionSurface(SKImage image)
		{
			Image = image;
		}

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
		{
			using var stream = new SKManagedStream(imageStream);

			if (targetWidth is int actualTargetWidth && targetHeight is int actualTargetHeight)
			{
				using var codec = SKCodec.Create(stream);

				var bitmap = new SKBitmap(actualTargetWidth, actualTargetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

				var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Image load result {result}");
				}

				if (result == SKCodecResult.Success)
				{
					Image = SKImage.FromBitmap(bitmap);
				}

				return (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput, result);
			}
			else
			{
				try
				{
					Image = SKImage.FromEncodedData(stream);
					return Image is null
						? (false, "Failed to decode image")
						: (true, "Success");
				}
				catch (Exception e)
				{
					return (false, e.Message);
				}
			}
		}

		/// <summary>
		/// Copies the provided pixels to the composition surface
		/// </summary>
		internal unsafe void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
		{
			var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

			using (var pData = data.Pin())
			{
				Image = SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4);
			}
		}
	}
}
