#nullable enable

#if HAS_SKOTTIE

using System;
using SkiaSharp;
using SkiaSharp.SceneGraph;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using System.Numerics;

namespace Uno.UI.Lottie;

/// <summary>
/// Skottie-backed <see cref="ILottieRenderer"/> — the default Lottie renderer, resolved reflectively by the host
/// builder (by assembly-qualified name, so the framework keeps no compile-time dependency on this add-in or SkiaSharp)
/// when Uno.UI.Lottie is referenced. It renders through the neutral <see cref="IDrawingSession"/>: straight into the
/// backend's live SKCanvas when it exposes one, else via a session-native texture — so Lottie plays on WebGPU too.
/// </summary>
internal sealed class SkottieLottieRenderer : ILottieRenderer
{
	// Reflective bootstrap entry point (found by name from UnoPlatformHostBuilder); keep the type/method name stable.
	internal static ILottieRenderer CreateLottieRenderer() => new SkottieLottieRenderer();

	public ILottieAnimation? Load(string animationJson)
	{
		using var stream = new Utf8StringStream(animationJson);
		return SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation) && animation is not null
			? new SkottieLottieAnimation(animation)
			: null;
	}

	private sealed class SkottieLottieAnimation : ILottieAnimation
	{
		private readonly object _gate = new();
		private readonly InvalidationController _invalidationController = new();
		private SkiaSharp.Skottie.Animation? _animation;

		public SkottieLottieAnimation(SkiaSharp.Skottie.Animation animation)
		{
			_animation = animation;
			Size = new Vector2(animation.Size.Width, animation.Size.Height);
			Duration = animation.Duration;
			_invalidationController.Begin();
		}

		public Vector2 Size { get; }

		public TimeSpan Duration { get; }

		public void Render(IDrawingSession session, float progress, Rect area)
		{
			// Guards Render against a concurrent Dispose on desktop; a no-op on single-threaded WASM.
			lock (_gate)
			{
				if (_animation is not { } animation)
				{
					return;
				}

				var frameTime = TimeSpan.FromTicks((long)(Duration.Ticks * Math.Clamp(progress, 0f, 1f)));
				animation.SeekFrameTime(frameTime, _invalidationController);

				// Fast path: draw straight into the backend's live canvas at the session's current transform.
				if (session.NativeSurface is SKCanvas canvas)
				{
					animation.Render(canvas, new SKRect((float)area.X, (float)area.Y, (float)(area.X + area.Width), (float)(area.Y + area.Height)));
					_invalidationController.Reset();
					return;
				}

				// Cross-backend fallback (no SKCanvas, e.g. WebGPU): rasterize to an offscreen, then let the SESSION's
				// own backend mint a native texture from the pixels — a foreign texture wouldn't be accepted by DrawImage.
				var width = Math.Max(1, (int)Math.Ceiling(area.Width));
				var height = Math.Max(1, (int)Math.Ceiling(area.Height));
				var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
				using var surface = SKSurface.Create(info);
				if (surface is null)
				{
					return;
				}

				surface.Canvas.Clear(SKColors.Transparent);
				animation.Render(surface.Canvas, new SKRect(0, 0, width, height));
				surface.Canvas.Flush();
				_invalidationController.Reset();

				using var pixmap = surface.PeekPixels();
				if (pixmap is null)
				{
					return;
				}

				using var texture = session.Factory.CreateTexture(width, height, pixmap.GetPixelSpan());
				session.DrawImage(texture, (float)area.X, (float)area.Y, ImageSampling.Linear, antialias: true);
			}
		}

		public void Dispose()
		{
			lock (_gate)
			{
				if (_animation is null)
				{
					return;
				}

				_invalidationController.End();
				_animation.Dispose();
				_animation = null;
			}
		}
	}
}

#endif
