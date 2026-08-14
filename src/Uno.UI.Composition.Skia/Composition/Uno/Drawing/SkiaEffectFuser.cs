#nullable enable

using SkiaSharp;
using Microsoft.UI.Composition;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Fuses a neutral <see cref="EffectNode"/> tree into a single <see cref="SKImageFilter"/> DAG — the Skia backend's
/// realization of <see cref="IDrawingFactory.CreateEffectFilter(EffectNode, Windows.Foundation.Rect)"/>. A
/// <see cref="BackdropInput"/> fuses to a filter with a null (implicit) input, which
/// <see cref="SkiaDrawingSession.DrawEffectBackdrop(IEffectFilter, float)"/> feeds from the live backdrop via
/// <c>SaveLayerRec.Backdrop</c>. The backdrop-flag dance mirrors the legacy <c>SkiaEffectFactory</c> generators so
/// output is identical.
/// </summary>
internal sealed class SkiaEffectFuser
{
	// Set when the current subtree resolved to the implicit backdrop leaf (a null child filter that means "the
	// backdrop", not "failed to build"). A parent op keeps a null child in that case and clears the flag once consumed.
	private bool _isBackdrop;

	internal bool HasBackdrop { get; private set; }

	internal SKImageFilter? Fuse(EffectNode node, SKRect bounds)
	{
		switch (node)
		{
			case BackdropInput:
				_isBackdrop = true;
				HasBackdrop = true;
				return null;

			case TextureInput texture:
			{
				_isBackdrop = false;
				var img = ((SkiaImageTexture)texture.Texture).Image;
				// The source was rasterized in bounds-space at the offscreen origin; place it back at bounds.
				var src = new SKRect(0, 0, img.Width, img.Height);
				var dst = new SKRect(bounds.Left, bounds.Top, bounds.Left + img.Width, bounds.Top + img.Height);
				return SKImageFilter.CreateImage(img, src, dst, new SKSamplingOptions(SKFilterMode.Linear));
			}

			case ColorInput color:
				// ColorSource fills bounds; no input. Does not clear the backdrop flag (matches the legacy generator).
				return SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(color.Color.ToSKColor(), SKBlendMode.Src), null, bounds);

			case ColorMatrixEffectNode cm:
			{
				var source = Fuse(cm.Source, bounds);
				if (source is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(cm.Matrix), source, bounds);
			}

			case ModulateEffectNode modulate:
			{
				var source = Fuse(modulate.Source, bounds);
				if (source is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				// Tint: per-channel multiply by the colour, clamped to [0,1] — matches the legacy Tint realization.
				return SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(modulate.Color.ToSKColor(), SKBlendMode.Modulate), source, bounds);
			}

			case LuminanceToAlphaEffectNode luma:
			{
				var source = Fuse(luma.Source, bounds);
				if (source is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				return SKImageFilter.CreateColorFilter(SKColorFilter.CreateLumaColor(), source, bounds);
			}

			case BlurEffectNode blur:
			{
				var source = Fuse(blur.Source, bounds);
				if (source is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				return blur.ClampEdge
					? SKImageFilter.CreateBlur(blur.Sigma, blur.Sigma, SKShaderTileMode.Clamp, source, bounds)
					: SKImageFilter.CreateBlur(blur.Sigma, blur.Sigma, source, bounds);
			}

			case BlendEffectNode blend:
			{
				var background = Fuse(blend.Background, bounds);
				if (background is null && !_isBackdrop)
				{
					return null;
				}

				var foreground = Fuse(blend.Foreground, bounds);
				if (foreground is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				return SKImageFilter.CreateBlendMode(SkiaDrawingSession.ToSKBlendMode(blend.Mode), background, foreground, bounds);
			}

			case CompositeEffectNode composite:
			{
				if (composite.Sources.Count == 0)
				{
					return null;
				}

				var current = Fuse(composite.Sources[0], bounds);
				if (current is null && !_isBackdrop)
				{
					return null;
				}

				_isBackdrop = false;
				var mode = SkiaDrawingSession.ToSKBlendMode(composite.Mode);
				for (var i = 1; i < composite.Sources.Count; i++)
				{
					var next = Fuse(composite.Sources[i], bounds);
					if (next is not null && !_isBackdrop)
					{
						current = SKImageFilter.CreateBlendMode(mode, current, next, bounds);
					}

					_isBackdrop = false;
				}

				return current;
			}

			case UnsupportedEffectNode unsupported:
				return unsupported.Source is null ? null : Fuse(unsupported.Source, bounds);

			default:
				return null;
		}
	}
}
