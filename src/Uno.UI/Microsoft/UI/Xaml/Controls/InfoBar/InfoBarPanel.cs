// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBarPanel.cpp, tag winui3/release/1.4.2

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents a panel that arranges its items horizontally if there is available space, otherwise vertically.
/// </summary>
public partial class InfoBarPanel : Panel
{
	private bool m_isVertical = false;

	/// <summary>
	/// Initializes a new instance of the InfoBarPanel class.
	/// </summary>
	public InfoBarPanel()
	{
	}

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

		var children = Children;
		var childCount = (int)children.Count;
		foreach (UIElement child in children)
		{
			child.Measure(availableSize);
			var childDesiredSize = child.DesiredSize;

			if (childDesiredSize.Width != 0 && childDesiredSize.Height != 0)
			{
				// Add up the width of all items if they were laid out horizontally
				var horizontalMargin = GetHorizontalOrientationMargin(child);
				totalWidth += childDesiredSize.Width + (nItems > 0 ? (float)horizontalMargin.Left : 0) + (float)horizontalMargin.Right;
				// Ignore left margin of first and right margin of last child
				totalWidth += childDesiredSize.Width +
					(nItems > 0 ? (float)horizontalMargin.Left : 0) +
					(nItems < childCount - 1 ? (float)horizontalMargin.Right : 0);

				// Add up the height of all items if they were laid out vertically
				var verticalMargin = GetVerticalOrientationMargin(child);
				// Ignore top margin of first and bottom margin of last child
				totalHeight += childDesiredSize.Height +
					(nItems > 0 ? (float)verticalMargin.Top : 0) +
					(nItems < childCount - 1 ? (float)verticalMargin.Bottom : 0);

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
			var verticalPadding = VerticalOrientationPadding;

			desiredSize.Width = widthOfWidest + (float)verticalPadding.Left + (float)verticalPadding.Right;
			desiredSize.Height = totalHeight + (float)verticalPadding.Top + (float)verticalPadding.Bottom;
		}
		else
		{
			m_isVertical = false;
			var horizontalPadding = HorizontalOrientationPadding;

			desiredSize.Width = totalWidth + (float)horizontalPadding.Left + (float)horizontalPadding.Right;
			desiredSize.Height = heightOfTallest + (float)horizontalPadding.Top + (float)horizontalPadding.Bottom;
		}

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size result = finalSize;

		if (m_isVertical)
		{
			// Layout elements vertically
			var verticalOrientationPadding = VerticalOrientationPadding;
			double verticalOffset = verticalOrientationPadding.Top;

			bool hasPreviousElement = false;
			foreach (UIElement child in Children)
			{
				var childAsFe = child as FrameworkElement;
				if (childAsFe != null)
				{
					var desiredSize = child.DesiredSize;
					if (desiredSize.Width != 0 && desiredSize.Height != 0)
					{
						var verticalMargin = GetVerticalOrientationMargin(child);

						verticalOffset += hasPreviousElement ? (float)verticalMargin.Top : 0;
						child.Arrange(new Rect(verticalOrientationPadding.Left + verticalMargin.Left, verticalOffset, desiredSize.Width, desiredSize.Height));
						verticalOffset += desiredSize.Height + (float)verticalMargin.Bottom;

						hasPreviousElement = true;
					}
				}
			}
		}
		else
		{
			// Layout elements horizontally
			var horizontalOrientationPadding = HorizontalOrientationPadding;
			double horizontalOffset = horizontalOrientationPadding.Left;

			bool hasPreviousElement = false;

			var children = Children;
			var childCount = (int)children.Count;
			for (int i = 0; i < childCount; i++)
			{
				var child = children[i];
				var childAsFe = child as FrameworkElement;
				if (childAsFe != null)
				{
					var desiredSize = child.DesiredSize;
					if (desiredSize.Width != 0 && desiredSize.Height != 0)
					{
						var horizontalMargin = GetHorizontalOrientationMargin(child);

						horizontalOffset += hasPreviousElement ? (float)horizontalMargin.Left : 0;

						if (i < childCount - 1)
						{
							child.Arrange(new Rect(horizontalOffset, (float)horizontalOrientationPadding.Top + (float)horizontalMargin.Top, desiredSize.Width, desiredSize.Height));
						}
						else
						{
							// Give the rest of the horizontal space to the last child.
							child.Arrange(new Rect(horizontalOffset, (float)horizontalOrientationPadding.Top + (float)horizontalMargin.Top, Math.Max(desiredSize.Width, finalSize.Width - horizontalOffset), desiredSize.Height));
						}
						horizontalOffset += desiredSize.Width + (float)horizontalMargin.Right;

						hasPreviousElement = true;
					}
				}
			}
		}

		return result;
	}
}
