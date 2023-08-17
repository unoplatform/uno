using System.Numerics;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class MonochromaticOverlayPresenter : Grid
	{
		private CompositionEffectFactory _effectFactory = null;
		private Color _replacementColor;
		private bool _needsBrushUpdate;

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
			if (SourceElement is { } targetElement)
			{
				var newColor = ReplacementColor;
				if (_replacementColor != newColor)
				{
					_replacementColor = newColor;
					_effectFactory = null;
				}

				var compositor = XamlRoot.Compositor;

				if (_effectFactory == null)
				{
					//// Build an effect that takes the source image and uses the alpha channel and replaces all other channels with
					//// the ReplacementColor's RGB.
					//var colorMatrixEffect = make_self<Microsoft.UI.Private.Composition.Effects.ColorMatrixEffect>();
					//colorMatrixEffect.Source(CompositionEffectSourceParameter{ "source" });
					//Microsoft.UI.Private.Composition.Effects.Matrix5x4 colorMatrix = default;

					//// If the ReplacementColor is not transparent then use the RGB values as the new color. Otherwise
					//// just show the target by using an Identity colorMatrix.
					//if (_replacementColor.A != 0)
					//{
					//	colorMatrix.M51 = (float)((_replacementColor.R / 255.0));
					//	colorMatrix.M52 = (float)((_replacementColor.G / 255.0));
					//	colorMatrix.M53 = (float)((_replacementColor.B / 255.0));
					//	colorMatrix.M44 = 1;
					//}
					//else
					//{
					//	colorMatrix.M11 = colorMatrix.M22 = colorMatrix.M33 = colorMatrix.M44 = 1;
					//}
					//colorMatrixEffect.ColorMatrix(colorMatrix);

					//_effectFactory = compositor.CreateEffectFactory(*colorMatrixEffect);
				}

				var actualSize = new Vector2((float)ActualWidth, (float)ActualHeight);
				var transform = TransformToVisual(targetElement);
				var offset = transform.TransformPoint(new Point(0, 0));

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
		}
	}
}
