#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal partial class SkiaCompositionSurface : CompositionObject, ICompositionSurface
	{
		private ImageFrameProvider? _frameProvider;

		private ImageFrameProvider? FrameProvider
		{
			get => _frameProvider;
			set
			{
				_frameProvider?.Dispose();
				_frameProvider = value;
			}
		}

		public SKImage? Image => FrameProvider?.CurrentImage;

		internal SkiaCompositionSurface(SKImage image)
		{
			_frameProvider = ImageFrameProvider.Create(image);
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
					FrameProvider = ImageFrameProvider.Create(SKImage.FromBitmap(bitmap));
				}

				return (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput, result);
			}
			else
			{
				try
				{
					using var codec = SKCodec.Create(stream);
					var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => OnPropertyChanged(nameof(Image), isSubPropertyChange: false), NativeDispatcherPriority.High);
					if (!ImageFrameProvider.TryCreate(codec, onFrameChanged, out var provider))
					{
						FrameProvider = null;
						return (false, "Failed to decode image");
					}

					FrameProvider = provider;
					return (true, "Success");
				}
				catch (Exception e)
				{
					FrameProvider = null;
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
				FrameProvider = ImageFrameProvider.Create(SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4));
			}
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			FrameProvider = null;
		}
	}
}
