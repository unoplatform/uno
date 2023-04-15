using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem : IBorderInfoProvider
	{
		private BorderLayerRenderer _borderRenderer;
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
			_borderRenderer ??= new BorderLayerRenderer(this);

			// DrawBackground			=> General background for all items
			// DrawControlBackground	=> Control.Background customized by the apps (can be customized in the element changing event)
			// DrawDensityBar			=> Not supported yet
			// DrawFocusBorder			=> Not supported yet
			// OR DrawBorder			=> Draws the border ...
			// DrawInnerBorder			=> The today / selected state

			_borderRenderer.Update();
		}


		Brush IBorderInfoProvider.Background
		{
			get
			{
				var background = Background;

				if (IsClear(background))
				{
					if (FindTodaySelectedBackgroundBrush() is { } todaySelectedBackground
						&& !IsClear(todaySelectedBackground))
					{
						background = todaySelectedBackground;
					}
					else if (FindSelectedBackgroundBrush() is { } selectedBackground
						&& !IsClear(selectedBackground))
					{
						background = selectedBackground;
					}
					else
					{
						background = GetItemBackgroundBrush();
					}
				}
				return background;
			}
		}

		BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

		Brush IBorderInfoProvider.BorderBrush
		{
			get
			{
				var borderBrush = GetItemBorderBrush(forFocus: false);
				if (m_isToday && m_isSelected && GetItemInnerBorderBrush() is { } selectedBrush)
				{
					// We don't support inner border yet, so even if not optimal we just use it as border.
					borderBrush = selectedBrush;
				}
				return borderBrush;
			}
		}


		Thickness IBorderInfoProvider.BorderThickness => GetItemBorderThickness();

		CornerRadius IBorderInfoProvider.CornerRadius => GetItemCornerRadius();

#if __ANDROID__
		bool IBorderInfoProvider.ShouldUpdateMeasures { get; set; }
#endif

		private bool IsClear(Brush brush)
			=> brush is null
				|| brush.Opacity == 0
				|| (brush is SolidColorBrush solid && solid.Color.IsTransparent);
	}
}
