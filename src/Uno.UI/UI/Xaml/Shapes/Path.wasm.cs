using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		private readonly SvgElement _path = new SvgElement("path");

		public Path()
		{
			SvgChildren.Add(_path);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _path;
		}

		partial void OnDataChanged()
		{
			switch (Data)
			{
				case GeometryData gd:
					_path.SetAttribute(
						("d", gd.Data),
						("fill-rule", gd.FillRule == FillRule.EvenOdd ? "evenodd" : "nonzero"));
					break;
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var bounds = GetBBox();

			var pathWidth = bounds.Width;
			var pathHeight = bounds.Height;

			if (ShouldPreserveOrigin)
			{
				pathWidth += bounds.X;
				pathHeight += bounds.Y;
			}

			var availableWidth = availableSize.Width;
			var availableHeight = availableSize.Height;

			var userWidth = Width;
			var userHeight = Height;

			var controlWidth = availableWidth <= 0 ? userWidth : availableWidth;
			var controlHeight = availableHeight <= 0 ? userHeight : availableHeight;

			// Default values
			var calculatedWidth = LimitWithUserSize(controlWidth, userWidth, pathWidth);
			var calculatedHeight = LimitWithUserSize(controlHeight, userHeight, pathHeight);

			var scaleX = (calculatedWidth - ActualStrokeThickness) / pathWidth;
			var scaleY = (calculatedHeight - ActualStrokeThickness) / pathHeight;

			// Make sure that we have a valid scale if both of them are not set
			if (double.IsInfinity(scaleX) &&
			   double.IsInfinity(scaleY))
			{
				scaleX = 1;
				scaleY = 1;
			}

			// Here we will override some of the default values
			switch (Stretch)
			{
				// If the Stretch is None, the drawing is not the same size as the control
				case Stretch.None:
					calculatedWidth = pathWidth;
					calculatedHeight = pathHeight;
					break;
				case Stretch.Fill:
					if (double.IsInfinity(scaleY))
					{
						scaleY = 1;
					}
					if (double.IsInfinity(scaleX))
					{
						scaleX = 1;
					}
					calculatedWidth = pathWidth * scaleX;
					calculatedHeight = pathHeight * scaleY;

					break;
				// Override the _calculated dimensions if the stretch is Uniform or UniformToFill
				case Stretch.Uniform:
					double scale = Math.Min(scaleX, scaleY);
					calculatedWidth = pathWidth * scale;
					calculatedHeight = pathHeight * scale;
					break;
				case Stretch.UniformToFill:
					scale = Math.Max(scaleX, scaleY);
					calculatedWidth = pathWidth * scale;
					calculatedHeight = pathHeight * scale;
					break;
			}

			calculatedWidth += ActualStrokeThickness;
			calculatedHeight += ActualStrokeThickness;

			return new Size(calculatedWidth, calculatedHeight);
		}
	}
}