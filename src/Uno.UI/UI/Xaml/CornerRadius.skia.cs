using System;
using SkiaSharp;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Windows.UI.Xaml;

partial struct CornerRadius
{
	/// <summary>
	/// Retrieves the radii for a border.
	/// </summary>
	/// <param name="borderThickness">Border thickness.</param>
	/// <param name="outer">True to return outer corner radii, false for inner.</param>
	/// <returns>Radii.</returns>
	internal void GetRadii(ref SKPoint[] radii, Size elementSize, Thickness borderThickness, bool outer)
	{
		var halfLeft = borderThickness.Left * 0.5;
		var halfTop = borderThickness.Top * 0.5;
		var halfRight = borderThickness.Right * 0.5;
		var halfBottom = borderThickness.Bottom* 0.5;

		double leftTop, topLeft, topRight, rightTop, rightBottom, bottomRight, leftBottom, bottomLeft;
		leftTop = topLeft = topRight = rightTop = rightBottom = bottomRight = leftBottom = bottomLeft = 0;
		
		if (outer)
		{
			if (!MathHelpers.IsCloseReal(TopLeft, 0.0f))
			{
				leftTop = TopLeft + halfLeft;
				topLeft = TopLeft + halfTop;
			}
			
			if (!MathHelpers.IsCloseReal(TopRight, 0.0f))
			{
				topRight = TopRight + halfTop;
				rightTop = TopRight + halfRight;
			}
			
			if (!MathHelpers.IsCloseReal(BottomRight, 0.0f))
			{
				rightBottom = BottomRight + halfRight;
				bottomRight = BottomRight + halfBottom;
			}

			if (!MathHelpers.IsCloseReal(BottomLeft, 0.0f))
			{
				bottomLeft = BottomLeft + halfBottom;
				leftBottom = BottomLeft + halfLeft;
			}
		}
		else
		{
			leftTop = Math.Max(0.0f, TopLeft - halfLeft);
			topLeft = Math.Max(0.0f, TopLeft - halfTop);
			topRight = Math.Max(0.0f, TopRight - halfTop);
			rightTop = Math.Max(0.0f, TopRight - halfRight);
			rightBottom = Math.Max(0.0f, BottomRight - halfRight);
			bottomRight = Math.Max(0.0f, BottomRight - halfBottom);
			bottomLeft = Math.Max(0.0f, BottomLeft - halfBottom);
			leftBottom = Math.Max(0.0f, BottomLeft - halfLeft);
		}

		// Adjust the corner radius to fit element size
		// When neighboring corners "overlap", we distribute
		// them "fairly" along the side.
		double ratio;
		
		if (leftTop + rightTop > elementSize.Width)
		{
			ratio = leftTop / (leftTop + rightTop);
			leftTop = ratio * elementSize.Width;
			rightTop = elementSize.Width - leftTop;
		}
		
		if (topRight + bottomRight > elementSize.Height)
		{
			ratio = topRight / (topRight + bottomRight);
			topRight = ratio * elementSize.Height;
			bottomRight = elementSize.Height - topRight;
		}

		if (rightBottom + leftBottom > elementSize.Width)
		{
			ratio = rightBottom / (rightBottom + leftBottom);
			rightBottom = ratio * elementSize.Width;
			leftBottom = elementSize.Width - rightBottom;
		}

		if (bottomLeft + topLeft > elementSize.Height)
		{
			ratio = bottomLeft / (bottomLeft + topLeft);
			bottomLeft = ratio * elementSize.Height;
			topLeft = elementSize.Height - bottomLeft;
		}

		radii[0] = new((float)leftTop, (float)topLeft);
		radii[1] = new((float)rightTop, (float)topRight);
		radii[2] = new((float)rightBottom, (float)bottomRight);
		radii[3] = new((float)leftBottom, (float)bottomLeft);
	}
}
