using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.ImageTests
{
	[TestClass]
	public class Given_ImageSizeHelper
	{
		private const double Inf = double.PositiveInfinity;

		[TestMethod]
		[DataRow(Stretch.None, 100, 100, 100, 100)]
		[DataRow(Stretch.None, 2000, 2000, 1000, 500)]
		[DataRow(Stretch.None, 100, Inf, 100, 500)]
		[DataRow(Stretch.None, 2000, Inf, 1000, 500)]
		[DataRow(Stretch.None, Inf, 100, 1000, 100)]
		[DataRow(Stretch.None, Inf, 1000, 1000, 500)]
		[DataRow(Stretch.None, Inf, Inf, 1000, 500)]

		[DataRow(Stretch.Fill, 100, 100, 100, 100)]
		[DataRow(Stretch.Fill, 2000, 2000, 2000, 2000)]
		[DataRow(Stretch.Fill, 100, Inf, 100, 50)]
		[DataRow(Stretch.Fill, 2000, Inf, 2000, 1000)]
		[DataRow(Stretch.Fill, Inf, 100, 200, 100)]
		[DataRow(Stretch.Fill, Inf, 2000, 4000, 2000)]
		[DataRow(Stretch.Fill, Inf, Inf, 1000, 500)]

		[DataRow(Stretch.Uniform, 100, 100, 100, 50)]
		[DataRow(Stretch.Uniform, 2000, 2000, 2000, 1000)]
		[DataRow(Stretch.Uniform, 100, Inf, 100, 50)]
		[DataRow(Stretch.Uniform, 2000, Inf, 2000, 1000)]
		[DataRow(Stretch.Uniform, Inf, 100, 200, 100)]
		[DataRow(Stretch.Uniform, Inf, 2000, 4000, 2000)]
		[DataRow(Stretch.Uniform, Inf, Inf, 1000, 500)]

		[DataRow(Stretch.UniformToFill, 100, 100, 100, 100)]
		[DataRow(Stretch.UniformToFill, 2000, 2000, 2000, 2000)]
		[DataRow(Stretch.UniformToFill, 100, Inf, 100, 50)]
		[DataRow(Stretch.UniformToFill, 2000, Inf, 2000, 1000)]
		[DataRow(Stretch.UniformToFill, Inf, 100, 200, 100)]
		[DataRow(Stretch.UniformToFill, Inf, 2000, 4000, 2000)]
		[DataRow(Stretch.UniformToFill, Inf, Inf, 1000, 500)]
		public void AdjustSize_Expected_Result(Stretch stretch, double availableWidth, double availableHeight, double expectedWidth, double expectedHeight)
		{
			var imageNaturalSize = new Size(1000, 500);
			var availableSize = new Size(availableWidth, availableHeight);
			var expectedOutputSize = new Size(expectedWidth, expectedHeight);

			ImageSizeHelper
				.AdjustSize(stretch, availableSize, imageNaturalSize)
				.Should()
				.Be(expectedOutputSize, 0.5, $"Invalid output for image size {imageNaturalSize} when available is {availableSize} using stretch {stretch}");
		}

		[TestMethod]
		[DataRow(Stretch.None, "lt", 1000, 500, 100, 100, 0, 0, 1000, 500)]
		[DataRow(Stretch.None, "cc", 1000, 500, 100, 100, -450, -200, 1000, 500)]
		[DataRow(Stretch.None, "rb", 1000, 500, 100, 100, -900, -400, 1000, 500)]
		[DataRow(Stretch.None, "lt", 1000, 500, 2000, 2000, 0, 0, 1000, 500)]

		[DataRow(Stretch.Fill, "lt", 1000, 500, 100, 100, 0, 0, 100, 100)]
		[DataRow(Stretch.Fill, "cc", 1000, 500, 100, 100, 0, 0, 100, 100)]
		[DataRow(Stretch.Fill, "cc", 1000, 500, 2000, 1000, 0, 0, 2000, 1000)]
		[DataRow(Stretch.Fill, "cc", 1000, 500, 1000, 2000, 0, 0, 1000, 2000)]
		[DataRow(Stretch.Fill, "rb", 1000, 500, 100, 100, 0, 0, 100, 100)]
		[DataRow(Stretch.Fill, "lt", 1000, 500, 2000, 2000, 0, 0, 2000, 2000)]

		[DataRow(Stretch.Uniform, "lt", 1000, 500, 100, 100, 0, 0, 100, 50)]
		[DataRow(Stretch.Uniform, "cc", 1000, 500, 100, 100, 0, 25, 100, 50)]
		[DataRow(Stretch.Uniform, "cc", 500, 1000, 100, 100, 25, 0, 50, 100)]
		[DataRow(Stretch.Uniform, "cc", 1000, 500, 2000, 1000, 0, 0, 2000, 1000)]
		[DataRow(Stretch.Uniform, "cc", 1000, 500, 1000, 2000, 0, 750, 1000, 500)]
		[DataRow(Stretch.Uniform, "rb", 1000, 500, 100, 100, 0, 50, 100, 50)]
		[DataRow(Stretch.Uniform, "rb", 500, 1000, 100, 100, 50, 0, 50, 100)]
		[DataRow(Stretch.Uniform, "lt", 1000, 500, 2000, 2000, 0, 0, 2000, 1000)]

		[DataRow(Stretch.UniformToFill, "lt", 1000, 500, 100, 100, 0, 0, 200, 100)]
		[DataRow(Stretch.UniformToFill, "lt", 200, 300, 250, 250, 0, 0, 250, 375)]
		[DataRow(Stretch.UniformToFill, "lc", 200, 300, 250, 250, 0, -62.5, 250, 375)]
		[DataRow(Stretch.UniformToFill, "cc", 1000, 500, 100, 100, -50, 0, 200, 100)]
		[DataRow(Stretch.UniformToFill, "cc", 1000, 500, 2000, 1000, 0, 0, 2000, 1000)]
		[DataRow(Stretch.UniformToFill, "cc", 1000, 500, 1000, 2000, -1500, 0, 4000, 2000)]
		[DataRow(Stretch.UniformToFill, "cc", 500, 1000, 1000, 2000, 0, 0, 1000, 2000)]
		[DataRow(Stretch.UniformToFill, "cc", 500, 1000, 1000, 1000, 0, -500, 1000, 2000)]
		[DataRow(Stretch.UniformToFill, "rb", 1000, 200, 100, 100, -400, 0, 500, 100)]
		[DataRow(Stretch.UniformToFill, "rb", 200, 1000, 100, 100, 0, -400, 100, 500)]
		public void MeasureSource_Expected_Result(
			Stretch stretch,
			string alignment,
			double imageNaturalWidth,
			double imageNaturalHeight,
			double finalWidth,
			double finalHeight,
			double expectedX,
			double expectedY,
			double expectedWidth,
			double expectedHeight)
		{
			var imageNaturalSize = new Size(imageNaturalWidth, imageNaturalHeight);
			var finalSize = new Size(finalWidth, finalHeight);
			var expectedRect = new Rect(expectedX, expectedY, expectedWidth, expectedHeight);
			HorizontalAlignment horizontal = default;
			VerticalAlignment vertical = default;

			switch (alignment[0])
			{
				case 'l':
					horizontal = HorizontalAlignment.Left;
					break;
				case 'c':
					horizontal = HorizontalAlignment.Center;
					break;
				case 'r':
					horizontal = HorizontalAlignment.Right;
					break;
				case 's':
					horizontal = HorizontalAlignment.Stretch;
					break;
			}
			switch (alignment[1])
			{
				case 't':
					vertical = VerticalAlignment.Top;
					break;
				case 'c':
					vertical = VerticalAlignment.Center;
					break;
				case 'b':
					vertical = VerticalAlignment.Bottom;
					break;
				case 's':
					vertical = VerticalAlignment.Stretch;
					break;
			}

			var image = new Image { Stretch = stretch, HorizontalAlignment = horizontal, VerticalAlignment = vertical };

			var containerRect = image.MeasureSource(finalSize, imageNaturalSize);
			var measuredRect = image.ArrangeSource(finalSize, containerRect);

			measuredRect.Should().Be(
				expectedRect,
				0.5,
				$"Invalid output for image size {imageNaturalSize} when finalSize is {finalSize} using stretch {stretch} alignment {horizontal}/{vertical}");
		}

	}
}
