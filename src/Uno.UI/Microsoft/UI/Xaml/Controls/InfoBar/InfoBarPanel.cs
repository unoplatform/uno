// MUX reference InfoBarPanel.cpp, commit 3125489

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBarPanel : Panel
	{
		private bool m_isVertical = false;

		protected override Size MeasureOverride(Size availableSize)
		{
			var desiredSize = new Size();

			double totalWidth = 0;
			double totalHeight = 0;
			double widthOfWidest = 0;
			double heightOfTallest = 0;
			double heightOfTallestInHorizontal = 0;
			int nItems = 0;

			var parent = this.Parent as FrameworkElement;
			float minHeight = parent == null ? 0.0f : (float)(parent.MinHeight - (Margin.Top + Margin.Bottom));

			foreach (UIElement child in Children)
			{
				child.Measure(availableSize);
				var childDesiredSize = child.DesiredSize;

				if (childDesiredSize.Width != 0 && childDesiredSize.Height != 0)
				{
					// Add up the width of all items if they were laid out horizontally
					var horizontalMargin = GetHorizontalMargin(child);
					totalWidth += childDesiredSize.Width + (nItems > 0 ? (float)horizontalMargin.Left : 0) + (float)horizontalMargin.Right;

					// Add up the height of all items if they were laid out vertically
					var verticalMargin = GetVerticalMargin(child);
					totalHeight += childDesiredSize.Height + (nItems > 0 ? (float)verticalMargin.Top : 0) + (float)verticalMargin.Bottom;

					if (childDesiredSize.Width > widthOfWidest)
					{
						widthOfWidest = childDesiredSize.Width;
					}

					if (childDesiredSize.Height > heightOfTallest)
					{
						heightOfTallest = childDesiredSize.Height;
					}

					double childHeightInHorizontal = childDesiredSize.Height + horizontalMargin.Top + horizontalMargin.Bottom;
					if (childHeightInHorizontal > heightOfTallestInHorizontal)
					{
						heightOfTallestInHorizontal = childHeightInHorizontal;
					}

					nItems++;
				}
			}

			// Since this panel is inside a *-sized grid column, availableSize.Width should not be infinite
			// If there is only one item inside the panel, we will count it as vertical (the margins work out better that way)
			// Also, if the height of any item is taller than the desired min height of the InfoBar,
			// the items should be laid out vertically even though they may seem to fit due to text wrapping.
			if (nItems == 1 || totalWidth > availableSize.Width || (minHeight > 0 && heightOfTallestInHorizontal > minHeight))
			{
				m_isVertical = true;
				var verticalMargin = GetVerticalMargin(this);

				desiredSize.Width = widthOfWidest;
				desiredSize.Height = totalHeight + (float)verticalMargin.Top + (float)verticalMargin.Bottom;
			}
			else
			{
				m_isVertical = false;
				var horizontalMargin = GetHorizontalMargin(this);

				desiredSize.Width = totalWidth + (float)horizontalMargin.Left + (float)horizontalMargin.Right;
				desiredSize.Height = heightOfTallest;
			}

			return desiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Size result = finalSize;

			if (m_isVertical)
			{
				// Layout elements vertically
				double verticalOffset = (float)GetVerticalMargin(this).Top;
				bool hasPreviousElement = false;
				foreach (UIElement child in Children)
				{
					var childAsFe = child as FrameworkElement;
					if (childAsFe != null)
					{
						var desiredSize = child.DesiredSize;
						if (desiredSize.Width != 0 && desiredSize.Height != 0)
						{
							var verticalMargin = GetVerticalMargin(child);

							verticalOffset += hasPreviousElement ? (float)verticalMargin.Top : 0;
							child.Arrange(new Rect((float)verticalMargin.Left, verticalOffset, desiredSize.Width, desiredSize.Height));
							verticalOffset += desiredSize.Height + (float)verticalMargin.Bottom;

							hasPreviousElement = true;
						}
					}
				}
			}
			else
			{
				// Layout elements horizontally
				double horizontalOffset = (float)GetHorizontalMargin(this).Left;
				bool hasPreviousElement = false;
				foreach (UIElement child in Children)
				{
					var childAsFe = child as FrameworkElement;
					if (childAsFe != null)
					{
						var desiredSize = child.DesiredSize;
						if (desiredSize.Width != 0 && desiredSize.Height != 0)
						{
							var horizontalMargin = GetHorizontalMargin(child);

							horizontalOffset += hasPreviousElement ? (float)horizontalMargin.Left : 0;
							child.Arrange(new Rect(horizontalOffset, (float)horizontalMargin.Top, desiredSize.Width, finalSize.Height));
							horizontalOffset += desiredSize.Width + (float)horizontalMargin.Right;

							hasPreviousElement = true;
						}
					}
				}
			}

			return result;
		}
	}
}
