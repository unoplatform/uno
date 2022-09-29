#nullable disable

using System;
using Android.Graphics;
using Uno.UI;
using Uno.UI.Controls;

namespace Windows.UI.Xaml
{
	partial struct CornerRadius
	{
		internal Path GetOutlinePath(RectF rect)
		{
			var radii = GetRadii();

			var path = new Path();
			path.AddRoundRect(rect, radii, Path.Direction.Cw);

			return path;
		}

		internal Path GetInnerOutlinePath(RectF rect, Thickness borderThickness)
		{
			var radii = GetInnerRadii(borderThickness);

			var path = new Path();
			path.AddRoundRect(rect, radii, Path.Direction.Cw);

			return path;
		}

		internal float[] GetRadii()
		{
			return new float[]
			{
				ViewHelper.LogicalToPhysicalPixels(TopLeft),
				ViewHelper.LogicalToPhysicalPixels(TopLeft),
				ViewHelper.LogicalToPhysicalPixels(TopRight),
				ViewHelper.LogicalToPhysicalPixels(TopRight),
				ViewHelper.LogicalToPhysicalPixels(BottomRight),
				ViewHelper.LogicalToPhysicalPixels(BottomRight),
				ViewHelper.LogicalToPhysicalPixels(BottomLeft),
				ViewHelper.LogicalToPhysicalPixels(BottomLeft)
			};
		}

		internal float[] GetInnerRadii(Thickness borderThickness)
		{
			// For most cases :
			// ICR: Inner CornerRadius
			// OCR: Outer CornerRadius
			// BT: BorderThickness
			// ICR = OCR - BT

			//TODO : Manage the case where BorderThickness >= CornerRadius
			// See https://github.com/unoplatform/uno/issues/6891 for more details

			return new float[]
			{
				ViewHelper.LogicalToPhysicalPixels(TopLeft - borderThickness.Top),
				ViewHelper.LogicalToPhysicalPixels(TopLeft - borderThickness.Left),
				ViewHelper.LogicalToPhysicalPixels(TopRight - borderThickness.Top),
				ViewHelper.LogicalToPhysicalPixels(TopRight - borderThickness.Right),
				ViewHelper.LogicalToPhysicalPixels(BottomRight - borderThickness.Bottom),
				ViewHelper.LogicalToPhysicalPixels(BottomRight - borderThickness.Right),
				ViewHelper.LogicalToPhysicalPixels(BottomLeft - borderThickness.Bottom),
				ViewHelper.LogicalToPhysicalPixels(BottomLeft - borderThickness.Left)
			};
		}
	}
}
