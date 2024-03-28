#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
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
		private SKImage[]? _images;
		private SKCodecFrameInfo[]? _infos;
		private int _currentFrame;

		private int CurrentFrame
		{
			get => _currentFrame;
			set => SetProperty(ref _currentFrame, value);
		}

		public SKImage? Image => _images?[_currentFrame];

		internal SkiaCompositionSurface(SKImage image)
		{
			_images = [image];
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
					_images = [SKImage.FromBitmap(bitmap)];
				}

				return (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput, result);
			}
			else
			{
				try
				{
					using var codec = SKCodec.Create(stream);
					_infos = codec.FrameInfo;
					var info = codec.Info;
					info = new SKImageInfo(info.Width, info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
					var bitmap = new SKBitmap(info);
					_images = GC.AllocateUninitializedArray<SKImage>(_infos.Length);

					for (int i = 0; i < _infos.Length; i++)
					{
						var options = new SKCodecOptions(i);
						codec.GetPixels(info, bitmap.GetPixels(), options);
						var currentBitmap = SKImage.FromBitmap(bitmap);
						if (currentBitmap is null)
						{
							_images = null;
							return (false, "Failed to decode image");
						}

						_images[i] = currentBitmap;
					}

					_ = Task.Run(() =>
					{
						CurrentFrame = 0;
						while (_images is not null && _images.Length > 1)
						{
							Thread.Sleep(_infos[CurrentFrame].Duration);
							NativeDispatcher.Main.Enqueue(() => CurrentFrame = (CurrentFrame + 1) % _infos.Length, NativeDispatcherPriority.High);
						}
					});
					return (true, "Success");
				}
				catch (Exception e)
				{
					_images = null;
					return (true, e.Message);
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
				_currentFrame = 0;
				_images = [SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4)];
			}
		}
	}
}
