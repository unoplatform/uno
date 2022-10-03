using System;
using System.Threading.Tasks;
using FluentAssertions.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;
using ImageBrush = Windows.UI.Xaml.Media.ImageBrush;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImageBrushStretch
	{
		[DataRow(Stretch.Fill)]
		[DataRow(Stretch.UniformToFill)]
		#if !__SKIA__ //Stretch.Uniform create a Mosaic in Skia. See https://github.com/unoplatform/uno/issues/10021
		[DataRow(Stretch.Uniform)]
		#endif
		[DataRow(Stretch.None)]

		[TestMethod]
		public async Task When_Stretch(Stretch stretch)
		{
			const string Redish = "#FFEB1C24";
			const string Yellowish = "#FFFEF200";
			const string Greenish = "#FF0ED145";
			const string White = "#FFFFFFFF";
			#if __MACOS__
				Assert.Inconclusive(); // Colors are not interpreted the same way on MacOS
			#endif
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/colored-ellipse.jpg"))
			};

			var SUT = new Border
			{
				Width = 100,
				Height = 100,
				BorderThickness = new Thickness(2),
				BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
				Background = brush,
			};
			WindowHelper.WindowContent = SUT;
			WindowHelper.WaitForLoaded(SUT);

			var renderer = new RenderTargetBitmap();
			const float BorderOffset = 8;
			float width = (float)SUT.Width, height = (float)SUT.Height;
			float centerX = width / 2, centerY = height / 2;

			var expectations = stretch switch
			{
				// All edges are red-ish
				Stretch.Fill => (Top: Redish, Bottom: Redish, Left: Redish, Right: Redish),
				// Top and bottom are red-ish. Left and right are yellow-ish
				Stretch.UniformToFill => (Top: Redish, Bottom: Redish, Left: Yellowish, Right: Yellowish),
				// Top and bottom are same as backround. Left and right are red-ish
				Stretch.Uniform => (Top: White, Bottom: White, Left: Redish, Right: Redish),
				// Everything is green-ish
				Stretch.None => (Top: Greenish, Bottom: Greenish, Left: Greenish, Right: Greenish),

				_ => throw new ArgumentOutOfRangeException($"unexpected stretch: {stretch}"),
			};

			var bitmap = await UpdateStretchAndScreenshot(stretch);
			ImageAssert.HasColorAt(bitmap, centerX, BorderOffset, expectations.Top, tolerance: 5);
			ImageAssert.HasColorAt(bitmap, centerX, height - BorderOffset, expectations.Bottom, tolerance: 5);
			ImageAssert.HasColorAt(bitmap, BorderOffset, centerY, expectations.Left, tolerance: 5);
			ImageAssert.HasColorAt(bitmap, width - BorderOffset, centerY, expectations.Right, tolerance: 5);

			async Task<RawBitmap> UpdateStretchAndScreenshot(Stretch stretch)
			{
				brush.Stretch = stretch;

				await WindowHelper.WaitForIdle();
				await renderer.RenderAsync(SUT);

				return await RawBitmap.From(renderer, SUT);
			}
		}
	}
}
