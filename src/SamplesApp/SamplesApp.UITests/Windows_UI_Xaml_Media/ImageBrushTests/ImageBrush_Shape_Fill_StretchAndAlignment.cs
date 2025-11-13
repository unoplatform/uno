using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	[TestFixture]
	public partial class ImageBrush_Shape_Fill_StretchAndAlignment : SampleControlUITestBase
	{
		private const string red = "#ED1C24";
		private const string green = "#008000";
		private const string white = "#FFFFFF";

		private const int SingleStretchTestTimeout = 3 * 60 * 1000;

		private readonly ExpectedColor[] _expectedColors = GetExpectedColors();

		[Test]
		[AutoRetry]
		[Timeout(SingleStretchTestTimeout)]
		public void When_StretchAndAlignment([Values] Stretch stretch)
		{
			try
			{
				Run("UITests.Windows_UI_Xaml_Media.ImageBrushTests.ImageBrushShapeStretchesAlignments");
				_app.SetOrientationPortrait();

				_app.WaitForElement("MyRectangle");

				SetProperty("MyStretch", "SelectedIndex", ((int)stretch).ToString());

				foreach (var expected in _expectedColors.Where(e => e.Stretch == stretch))
				{
					SetProperty("MyAlignmentX", "SelectedIndex", ((int)expected.AlignmentX).ToString());
					SetProperty("MyAlignmentY", "SelectedIndex", ((int)expected.AlignmentY).ToString());

					SetProperty("MyWidth", "Text", expected.Size.Width.ToString());
					SetProperty("MyHeight", "Text", expected.Size.Height.ToString());


					var greenContainer = _app.GetPhysicalRect("GreenBackground");

					var permutation = $"{expected.Size.Width}-{expected.Size.Height}-{Enum.GetName<Stretch>(expected.Stretch)}-X{Enum.GetName<AlignmentX>(expected.AlignmentX)}-Y{Enum.GetName<AlignmentY>(expected.AlignmentY)}";
					using var snapshot = TakeScreenshot($"ImageBrush-{permutation}");

					ImageAssert.HasPixels(
						snapshot,
						ExpectedPixels
							.At($"middle-left-{permutation}", greenContainer.X, greenContainer.CenterY)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[0]),
						ExpectedPixels
							.At($"top-left-{permutation}", greenContainer.X, greenContainer.Y)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[1]),
						ExpectedPixels
							.At($"middle-top-{permutation}", greenContainer.CenterX, greenContainer.Y)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[2]),
						ExpectedPixels
							.At($"top-right-{permutation}", greenContainer.Right, greenContainer.Y)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[3]),
						ExpectedPixels
							.At($"middle-right-{permutation}", greenContainer.Right, greenContainer.CenterY)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[4]),
						ExpectedPixels
							.At($"bottom-right-{permutation}", greenContainer.Right, greenContainer.Bottom)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[5]),
						ExpectedPixels
							.At($"middle-bottom-{permutation}", greenContainer.CenterX, greenContainer.Bottom)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[6]),
						ExpectedPixels
							.At($"bottom-left-{permutation}", greenContainer.X, greenContainer.Bottom)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[7]),
						ExpectedPixels
							.At($"middle-middle-{permutation}", greenContainer.CenterX, greenContainer.CenterY)
							.WithPixelTolerance(1, 1)
							.Pixel(expected.Colors[8])
					);
				}

			}
			finally
			{
				_app.SetOrientationLandscape();
			}
		}

		private void SetProperty(string name, string propertyName, string item)
		{
			var control = _app.Marked(name);
			var _ = control.SetDependencyPropertyValue(propertyName, item);
		}

		private struct ExpectedColor
		{
			public Stretch Stretch { get; set; }
			public Size Size { get; set; }
			public AlignmentX AlignmentX { get; set; }
			public AlignmentY AlignmentY { get; set; }


			//Expected colors at middle-left, top-left, middle-top, top-right, middle-right, bottom-right, middle-bottom, bottom-left, middle-middle
			public string[] Colors { get; set; }
		}

		private enum AlignmentX : int
		{
			Center = 0,
			Left,
			Right,
		}

		private enum AlignmentY : int
		{
			Center = 0,
			Top,
			Bottom,
		}

		public enum Stretch : int
		{
			Fill = 0,
			None,
			Uniform,
			UniformToFill,
		}
	}
}
