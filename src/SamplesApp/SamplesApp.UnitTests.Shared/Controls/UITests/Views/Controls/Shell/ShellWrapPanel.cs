#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.Samples.Controls;

/// <summary>Lays children out left to right and wraps to a new row when the next one does not fit.</summary>
public sealed partial class ShellWrapPanel : Panel
{
	private const double Spacing = 4;

	protected override Size MeasureOverride(Size availableSize)
	{
		double x = 0, rowHeight = 0, usedWidth = 0, totalHeight = 0;
		foreach (var child in Children)
		{
			child.Measure(availableSize);
			var size = child.DesiredSize;
			if (x > 0 && x + size.Width > availableSize.Width)
			{
				totalHeight += rowHeight + Spacing;
				x = 0;
				rowHeight = 0;
			}

			x += size.Width + Spacing;
			rowHeight = Math.Max(rowHeight, size.Height);
			usedWidth = Math.Max(usedWidth, x - Spacing);
		}

		return new Size(usedWidth, totalHeight + rowHeight);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double x = 0, y = 0, rowHeight = 0;
		foreach (var child in Children)
		{
			var size = child.DesiredSize;
			if (x > 0 && x + size.Width > finalSize.Width)
			{
				y += rowHeight + Spacing;
				x = 0;
				rowHeight = 0;
			}

			child.Arrange(new Rect(x, y, size.Width, size.Height));
			x += size.Width + Spacing;
			rowHeight = Math.Max(rowHeight, size.Height);
		}

		return finalSize;
	}
}
