using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		private readonly BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();
		private Size _lastSize;

		private void Uno_InvalidateRender()
		{
			_lastSize = default;
			InvalidateArrange();
		}

		private void Uno_MeasureChrome(Size availableSize)
		{
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
			// DrawBackground			=> General background for all items
			// DrawControlBackground	=> Control.Background customized by the apps (can be customized in the element changing event)
			// DrawDensityBar			=> Not supported yet
			// DrawFocusBorder			=> Not supported yet
			// OR DrawBorder			=> Draws the border ...
			// DrawInnerBorder			=> The today / selected state

			var background = Background;
			var borderThickness = GetItemBorderThickness();
			var borderBrush = GetItemBorderBrush(forFocus: false);
			var cornerRadius = GetItemCornerRadius();

			if (IsClear(background))
			{
				if (FindTodaySelectedBackgroundBrush() is { } todaySelectedBackground
					&& !IsClear(todaySelectedBackground))
				{
					background = todaySelectedBackground;
				}
				else if(FindSelectedBackgroundBrush() is { } selectedBackground
					&& !IsClear(selectedBackground))
				{
					background = selectedBackground;
				}
				else
				{
					background = GetItemBackgroundBrush();
				}
			}

			if (m_isToday && m_isSelected && GetItemInnerBorderBrush() is { } selectedBrush)
			{
				// We don't support inner border yet, so even if not optimal we just use it as border.
				borderBrush = selectedBrush;
			}

			_borderRenderer.UpdateLayer(this, background, borderThickness, borderBrush, cornerRadius, default);
		}

		private bool IsClear(Brush brush)
			=> brush is null
				|| brush.Opacity == 0
				|| (brush is SolidColorBrush solid && solid.Color.IsTransparent);
	}
}
