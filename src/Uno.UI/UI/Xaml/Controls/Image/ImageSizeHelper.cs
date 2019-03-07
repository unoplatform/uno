using System;
using System.Collections.Generic;
using System.Text;
using static Windows.UI.Xaml.Media.Stretch;

namespace Windows.UI.Xaml.Controls
{
	internal static class ImageSizeHelper
	{
		public static void MeasureSource(this Image image, Windows.Foundation.Rect parent, ref Windows.Foundation.Rect child)
		{
			switch (image.Stretch)
			{
				case UniformToFill:
					var uniformToFillScale = (child.Width * parent.Height >= child.Height * parent.Width)
						? parent.Height / child.Height // child is flatter than parent
						: parent.Width / child.Width; // child is taller than parent
					child.Width *= uniformToFillScale;
					child.Height *= uniformToFillScale;
					break;

				case Uniform:
					var uniformScale = (child.Width * parent.Height > child.Height * parent.Width)
						? parent.Width / child.Width // child is taller than parent
						: parent.Height / child.Height; // child is flatter than parent
					child.Width *= uniformScale;
					child.Height *= uniformScale;
					break;

				case Fill:
					child.Width = parent.Width;
					child.Height = parent.Height;
					break;

				case None:
					break;
			}
		}

		public static void ArrangeSource(this Image image, Windows.Foundation.Rect parent, ref Windows.Foundation.Rect child)
		{
			switch (image.HorizontalAlignment)
			{
				case HorizontalAlignment.Left:
					child.X = 0;
					break;
				case HorizontalAlignment.Right:
					child.X = parent.Width - child.Width;
					break;
				case HorizontalAlignment.Center:
				case HorizontalAlignment.Stretch:
					child.X = (parent.Width * 0.5f) - (child.Width * 0.5f);
					break;
			}

			switch (image.VerticalAlignment)
			{
				case VerticalAlignment.Top:
					child.Y = 0;
					break;
				case VerticalAlignment.Bottom:
					child.Y = parent.Height - child.Height;
					break;
				case VerticalAlignment.Center:
				case VerticalAlignment.Stretch:
					child.Y = (parent.Height * 0.5f) - (child.Height * 0.5f);
					break;
			}

			// Clamp the results. A non image larger that its size, even if aligned bottom
			// must be aligned at the top.
			child.X = Math.Max(child.X, 0);
			child.Y = Math.Max(child.Y, 0);
		}

	}
}
