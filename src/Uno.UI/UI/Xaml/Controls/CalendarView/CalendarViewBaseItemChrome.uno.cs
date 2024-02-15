using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
#if __ANDROID__ || __IOS__ || __SKIA__ || __WASM__ || __MACOS__
		private BorderLayerRenderer _borderRenderer;
#endif

		private Size _lastSize;

		private void Uno_InvalidateRender()
		{
			_lastSize = default;
			InvalidateArrange();
#if __WASM__
			if (this.GetTemplateRoot() is UIElement templateRoot)
			{
				templateRoot.InvalidateArrange();
			}
#endif
		}

		private void Uno_MeasureChrome(Size availableSize)
		{
			// Uno Patch:
			// When CalendarDatePicker is closing we won't get a PointerExited, so we will stay flag as hovered.
			// If we re-open the picker, the flag is never cleared and we will still drawing the over state.
			// Here we patch this by syncing the local over state with the uno's internal over state.
			if (IsHovered() && !IsPointerOver)
			{
				SetIsHovered(false);
			}
		}

		private void Uno_ArrangeChrome(Rect finalBounds)
		{
			UpdateChromeIfNeeded(finalBounds);
		}

#if __SKIA__
		/// <inheritdoc />
		internal override void OnArrangeVisual(Rect rect, Rect? clip)
		{
			UpdateChromeIfNeeded(rect);

			base.OnArrangeVisual(rect, clip);
		}
#endif

		private void UpdateChromeIfNeeded(Rect rect)
		{
			if (rect.Width > 0 && rect.Height > 0 && _lastSize != rect.Size)
			{
				_lastSize = rect.Size;
				UpdateChrome();
			}
		}

		private void UpdateChrome()
		{
#if __SKIA__ || __WASM__
			_borderRenderer ??= new BorderLayerRenderer(this);

			// DrawBackground			=> General background for all items
			// DrawControlBackground	=> Control.Background customized by the apps (can be customized in the element changing event)
			// DrawDensityBar			=> Not supported yet
			// DrawFocusBorder			=> Not supported yet
			// OR DrawBorder			=> Draws the border ...
			// DrawInnerBorder			=> The today / selected state

#if __WASM__
			var borderInfoProvider = (IBorderInfoProvider)this;
			var borderThickness = borderInfoProvider.BorderThickness;
			var borderBrush = borderInfoProvider.BorderBrush;
			if (borderBrush is not null)
			{
				EffectiveBorderThickness = borderThickness;
			}
			else
			{
				EffectiveBorderThickness = default;
			}
#endif

			_borderRenderer.Update();
#else
			// DrawBackground			=> General background for all items
			// DrawControlBackground	=> Control.Background customized by the apps (can be customized in the element changing event)
			// DrawDensityBar			=> Not supported yet
			// DrawFocusBorder			=> Not supported yet
			// OR DrawBorder			=> Draws the border ...
			// DrawInnerBorder			=> The today / selected state

			var borderInfoProvider = (IBorderInfoProvider)this;
			var background = borderInfoProvider.Background;
			var borderThickness = borderInfoProvider.BorderThickness;
			var borderBrush = borderInfoProvider.BorderBrush;
			var cornerRadius = borderInfoProvider.CornerRadius;

#if __ANDROID__ || __IOS__ || __MACOS__
			_borderRenderer?.UpdateLayer(this, background, BackgroundSizing.InnerBorderEdge, borderThickness, borderBrush, cornerRadius, default);
#endif
#endif
		}


#if __WASM__
		internal Thickness EffectiveBorderThickness { get; set; }
#endif

		private bool IsClear(Brush brush)
			=> brush is null
				|| brush.Opacity == 0
				|| (brush is SolidColorBrush solid && solid.Color.IsTransparent);
	}
}
