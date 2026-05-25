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
		// Don't set this field directly. Use SetFrameProviderAndOnFrameChanged instead.
		private IFrameProvider? _frameProvider;

		// Unused: But intentionally kept!
		// This is here to keep the Action lifetime the same as SkiaCompositionSurface.
		// i.e, only cause the Action to be GC'ed if SkiaCompositionSurface is GC'ed.
		private Action? _onFrameChanged;

		// Don't set directly. Use SetFrameProviderAndOnFrameChanged instead
		private IFrameProvider? FrameProvider
		{
			get => _frameProvider;
			set
			{
				_frameProvider?.Dispose();
				_frameProvider = value;
				OnPropertyChanged(nameof(FrameProvider), isSubPropertyChange: false);
			}
		}

		public SKImage? Image => FrameProvider?.CurrentImage;

		internal SkiaCompositionSurface(SKImage image)
		{
			FrameProvider = FrameProviderFactory.Create(image);
		}

		private void SetFrameProviderAndOnFrameChanged(IFrameProvider? provider, Action? onFrameChanged)
		{
			FrameProvider = provider;
			_onFrameChanged = onFrameChanged;
		}

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
		{
			using var stream = new SKManagedStream(imageStream);

			try
			{
				var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => OnPropertyChanged(nameof(Image), isSubPropertyChange: false), NativeDispatcherPriority.High);
				if (!FrameProviderFactory.TryCreate(stream, onFrameChanged, targetWidth, targetHeight, out var provider))
				{
					SetFrameProviderAndOnFrameChanged(null, null);
					return (false, "Failed to decode image");
				}

				SetFrameProviderAndOnFrameChanged(provider, onFrameChanged);
				GC.KeepAlive(onFrameChanged);
				return (true, "Success");
			}
			catch (Exception e)
			{
				SetFrameProviderAndOnFrameChanged(null, null);
				return (false, e.Message);
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
				SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4)), null);
			}
		}

		~SkiaCompositionSurface()
		{
			SetFrameProviderAndOnFrameChanged(null, null);
		}
	}
}
