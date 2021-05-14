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

		/// <inheritdoc />
		internal override void OnArrangeVisual(Rect rect, Rect? clip)
		{
			UpdateChromeIfNeeded(rect);

			base.OnArrangeVisual(rect, clip);
		}

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
			if (background is null
				|| background.Opacity == 0
				|| (background is SolidColorBrush solid && solid.Color.IsTransparent))
			{
				background = GetItemBackgroundBrush();
			}

			if (m_isToday && m_isSelected && GetItemInnerBorderBrush() is { } selectedBrush)
			{
				background = selectedBrush;
			}

			var borderThickness = GetItemBorderThickness();
			var borderBrush = GetItemBorderBrush(forFocus: false);

			_borderRenderer.UpdateLayer(this, background, borderThickness, borderBrush, CornerRadius.None, null);
		}
	}
}
