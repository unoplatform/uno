#nullable enable

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
using Uno.UI.Composition.Drawing;
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

		public IImage? Image => FrameProvider?.CurrentImage;

		/// <summary>Wraps a backend-produced image (e.g. a rendered SVG, an offscreen snapshot, or raw pixels
		/// uploaded through the backend) as a surface.</summary>
		internal SkiaCompositionSurface(IImage image)
		{
			FrameProvider = FrameProviderFactory.Create(DrawingBackend.Current.CreateImageFrames(image), null);
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			// Releases the frame provider (and the underlying image); previously this only happened at
			// finalization, so callers reached into the raw image to dispose it deterministically.
			SetFrameProviderAndOnFrameChanged(null, null);
		}

		private void SetFrameProviderAndOnFrameChanged(IFrameProvider? provider, Action? onFrameChanged)
		{
			FrameProvider = provider;
			_onFrameChanged = onFrameChanged;
		}

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
		{
			try
			{
				var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => OnPropertyChanged(nameof(Image), isSubPropertyChange: false), NativeDispatcherPriority.High);
				if (!DrawingBackend.Current.TryDecodeImage(imageStream, targetWidth, targetHeight, out var frames))
				{
					SetFrameProviderAndOnFrameChanged(null, null);
					return (false, "Failed to decode image");
				}

				SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(frames, onFrameChanged), onFrameChanged);
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
		internal void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
		{
			var frames = DrawingBackend.Current.CreateImageFrame(pixelWidth, pixelHeight, data.Span);
			SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(frames, null), null);
		}

		~SkiaCompositionSurface()
		{
			SetFrameProviderAndOnFrameChanged(null, null);
		}
	}
}
