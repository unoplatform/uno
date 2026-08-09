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
	internal partial class CompositionImageSurface : CompositionObject, ICompositionSurface
	{
		// Don't set this field directly. Use SetFrameProviderAndOnFrameChanged instead.
		private IFrameProvider? _frameProvider;

		// Unused: But intentionally kept!
		// This is here to keep the Action lifetime the same as CompositionImageSurface.
		// i.e, only cause the Action to be GC'ed if CompositionImageSurface is GC'ed.
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

		private IImage? _texturedImage;
		private IImageTexture? _texture;
		// When true, this surface retains a backend texture directly (e.g. a rendered SVG) with no CPU IImage /
		// decoder frame behind it; GetTexture returns it as-is and it is disposed with the surface.
		private readonly bool _retainedTexture;

		/// <summary>Pixel size of the surface's content — from the current frame's <see cref="IImage"/>, or from a
		/// directly-retained texture. Null when there is nothing to draw. Used by brushes for stretch/alignment.</summary>
		internal System.Numerics.Vector2? Size =>
			Image is { } img ? new System.Numerics.Vector2(img.PixelWidth, img.PixelHeight)
			: _retainedTexture && _texture is { } tex ? new System.Numerics.Vector2(tex.PixelWidth, tex.PixelHeight)
			: null;

		/// <summary>
		/// The framework-owned GPU texture for the current frame's image, created once via the active backend
		/// factory and reused across frames (recreated only when the frame changes). Disposed with the surface.
		/// </summary>
		internal IImageTexture? GetTexture()
		{
			// A directly-retained texture (e.g. rendered SVG) is already backend-resident — return it as-is,
			// no derive-from-IImage / no readback. Its lifetime is the surface's.
			if (_retainedTexture)
			{
				return _texture;
			}

			var img = Image;
			if (img is null)
			{
				DisposeTexture();
				return null;
			}
			if (!ReferenceEquals(img, _texturedImage))
			{
				DisposeTexture();
				_texture = DrawingFactory.Current.CreateImageTexture(img);
				_texturedImage = img;
			}
			return _texture;
		}

		private void DisposeTexture()
		{
			_texture?.Dispose();
			_texture = null;
			_texturedImage = null;
		}

		/// <summary>Wraps a backend-produced image (e.g. a rendered SVG, an offscreen snapshot, or raw pixels
		/// uploaded through the backend) as a surface.</summary>
		internal CompositionImageSurface(IImage image)
		{
			FrameProvider = FrameProviderFactory.Create(ImageDecoder.Current.CreateFrames(image), null);
		}

		/// <summary>Wraps a backend-resident texture (e.g. a rendered SVG) as a retained surface, with no CPU
		/// IImage / decoder frame behind it. The surface owns the texture and disposes it deterministically.</summary>
		internal CompositionImageSurface(IImageTexture texture)
		{
			_texture = texture;
			_retainedTexture = true;
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
			DisposeTexture();
			FrameProvider = provider;
			_onFrameChanged = onFrameChanged;
		}

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
		{
			try
			{
				var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => OnPropertyChanged(nameof(Image), isSubPropertyChange: false), NativeDispatcherPriority.High);
				if (!ImageDecoder.Current.TryDecode(imageStream, targetWidth, targetHeight, out var frames))
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
			var frames = ImageDecoder.Current.CreateFrames(ImageDecoder.Current.CreateImage(pixelWidth, pixelHeight, data.Span));
			SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(frames, null), null);
		}

		~CompositionImageSurface()
		{
			SetFrameProviderAndOnFrameChanged(null, null);
		}
	}
}
