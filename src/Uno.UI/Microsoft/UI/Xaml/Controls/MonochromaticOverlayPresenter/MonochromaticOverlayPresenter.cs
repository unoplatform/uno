using System.Numerics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

/// <summary>
/// Enables visually overriding the colors of items in a grid with a monochromatic scheme or hue.
/// </summary>
public partial class MonochromaticOverlayPresenter : Grid
{
#if !__SKIA__
	private static bool _warned;
#endif

	private CompositionEffectFactory _effectFactory = null;
	private Color _replacementColor;
	private bool _needsBrushUpdate;

	/// <summary>
	/// Initializes a new instance of the MonochromaticOverlayPresenter class.
	/// </summary>
	public MonochromaticOverlayPresenter()
	{
		SizeChanged += (s, e) => InvalidateBrush();
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		InvalidateBrush();
	}

	private void InvalidateBrush()
	{
		// Delay brush updates until Tick to coalesce changes and avoid rebuilding the effect when we don't need to.
		if (!_needsBrushUpdate)
		{
			_needsBrushUpdate = true;
			SharedHelpers.QueueCallbackForCompositionRendering(() =>
			{
				{
					UpdateBrush();
				}
				_needsBrushUpdate = false;
			});
		}
	}

	private void UpdateBrush()
	{
#if !__SKIA__
		// TODO Uno: Required Composition APIs are not implemented yet, warn the user.
		if (!_warned)
		{
			_warned = true;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Log(
					LogLevel.Warning,
					"MonochromaticOverlayPresenter is not yet supported in Uno Platform and currently does not display any content");
			}
		}
#else
		if (SourceElement is { } targetElement)
		{
			var newColor = ReplacementColor;
			if (_replacementColor != newColor)
			{
				_replacementColor = newColor;
				_effectFactory = null;
			}

			var compositor = CompositionTarget.GetCompositorForCurrentThread();

			if (_effectFactory is null)
			{
				// Build an effect that takes the source image and uses the alpha channel and replaces all other channels with
				// the ReplacementColor's RGB.
				var colorMatrixEffect = new ColorMatrixEffect();
				colorMatrixEffect.Source = new CompositionEffectSourceParameter("source");
				Matrix5x4 colorMatrix = new();

				// If the ReplacementColor is not transparent then use the RGB values as the new color. Otherwise
				// just show the target by using an Identity colorMatrix.
				if (_replacementColor.A != 0)
				{
					colorMatrix.M51 = (float)((_replacementColor.R / 255.0));
					colorMatrix.M52 = (float)((_replacementColor.G / 255.0));
					colorMatrix.M53 = (float)((_replacementColor.B / 255.0));
					colorMatrix.M44 = 1;
				}
				else
				{
					colorMatrix.M11 = colorMatrix.M22 = colorMatrix.M33 = colorMatrix.M44 = 1;
				}
				colorMatrixEffect.ColorMatrix = colorMatrix;

				_effectFactory = compositor.CreateEffectFactory(colorMatrixEffect);
			}

			var actualSize = new Vector2((float)ActualWidth, (float)ActualHeight);
			var transform = TransformToVisual(targetElement);
			var offset = transform.TransformPoint(default);

			// Create a VisualSurface positioned at the same location as this control and feed that
			// through the color effect.
			var surfaceBrush = compositor.CreateSurfaceBrush();
			surfaceBrush.Stretch = CompositionStretch.None;
			var surface = compositor.CreateVisualSurface();

			// Select the source visual and the offset/size of this control in that element's space.
			surface.SourceVisual = ElementCompositionPreview.GetElementVisual(targetElement);
			surface.SourceOffset = new Vector2((float)offset.X, (float)offset.Y);
			surface.SourceSize = actualSize;
			surfaceBrush.Surface = surface;
			surfaceBrush.Stretch = CompositionStretch.None;
			var compBrush = _effectFactory.CreateBrush();
			compBrush.SetSourceParameter("source", surfaceBrush);

			var visual = compositor.CreateSpriteVisual();
			visual.Size = actualSize;
			visual.Brush = compBrush;

			ElementCompositionPreview.SetElementChildVisual(this, visual);
		}
#endif
	}
}
