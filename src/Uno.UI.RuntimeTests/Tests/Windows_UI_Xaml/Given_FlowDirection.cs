using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
#if !SUPPORTS_RTL
[Ignore("RTL is not supported")]
#endif
public class Given_FlowDirection
{
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_RTL()
	{
		if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var grid = new Grid()
		{
			RowDefinitions =
			{
				new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) },
			},
			ColumnDefinitions =
			{
				new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
			},
			Width = 200,
			Height = 200,
			FlowDirection = FlowDirection.RightToLeft,
		};

		var red = new Rectangle()
		{
			Stretch = Stretch.Fill,
			Fill = new SolidColorBrush(Colors.Red),
		};
		Grid.SetRow(red, 0);
		Grid.SetColumn(red, 0);

		var green = new Rectangle()
		{
			Stretch = Stretch.Fill,
			Fill = new SolidColorBrush(Colors.Green),
		};
		Grid.SetRow(green, 0);
		Grid.SetColumn(green, 1);

		var blue = new Rectangle()
		{
			Stretch = Stretch.Fill,
			Fill = new SolidColorBrush(Colors.Blue),
		};
		Grid.SetRow(blue, 1);
		Grid.SetColumn(blue, 0);

		var yellow = new Rectangle()
		{
			Stretch = Stretch.Fill,
			Fill = new SolidColorBrush(Colors.Yellow),
		};
		Grid.SetRow(yellow, 1);
		Grid.SetColumn(yellow, 1);

		Assert.AreEqual(FlowDirection.LeftToRight, red.FlowDirection);
		Assert.AreEqual(FlowDirection.LeftToRight, green.FlowDirection);
		Assert.AreEqual(FlowDirection.LeftToRight, blue.FlowDirection);
		Assert.AreEqual(FlowDirection.LeftToRight, yellow.FlowDirection);

		grid.Children.Add(red);
		grid.Children.Add(green);
		grid.Children.Add(blue);
		grid.Children.Add(yellow);

		Assert.AreEqual(FlowDirection.RightToLeft, red.FlowDirection);
		Assert.AreEqual(FlowDirection.RightToLeft, green.FlowDirection);
		Assert.AreEqual(FlowDirection.RightToLeft, blue.FlowDirection);
		Assert.AreEqual(FlowDirection.RightToLeft, yellow.FlowDirection);

		TestServices.WindowHelper.WindowContent = grid;
		await TestServices.WindowHelper.WaitForLoaded(grid);

#if !__SKIA__ // RenderTargetBitmap doesn't handle MatrixTransformation properly, which is used in the case of RTL.
		var renderer = new RenderTargetBitmap();
		await renderer.RenderAsync(grid);
		var bitmap = await RawBitmap.From(renderer, grid);
		//await TestServices.WindowHelper.WaitForever();
		Assert.AreEqual(200, bitmap.Width);
		Assert.AreEqual(200, bitmap.Height);

		var firstRowLeft = bitmap.GetPixel(50, 50);
		Assert.AreEqual(Colors.Green, firstRowLeft);

		var firstRowRight = bitmap.GetPixel(150, 50);
		Assert.AreEqual(Colors.Red, firstRowRight);

		var secondRowLeft = bitmap.GetPixel(50, 150);
		Assert.AreEqual(Colors.Yellow, secondRowLeft);

		var secondRowRight = bitmap.GetPixel(50, 150);
		Assert.AreEqual(Colors.Blue, secondRowRight);
#endif

		var redTransformToGreen = (MatrixTransform)red.TransformToVisual(green);
		Assert.AreEqual("1,0,0,1,-100,0", redTransformToGreen.Matrix.ToString());
		var redTransformToYellow = (MatrixTransform)red.TransformToVisual(yellow);
		Assert.AreEqual("1,0,0,1,-100,-100", redTransformToYellow.Matrix.ToString());
		var redTransformToBlue = (MatrixTransform)red.TransformToVisual(blue);
		Assert.AreEqual("1,0,0,1,0,-100", redTransformToBlue.Matrix.ToString());

		var greenTransformToRed = (MatrixTransform)green.TransformToVisual(red);
		Assert.AreEqual("1,0,0,1,100,0", greenTransformToRed.Matrix.ToString());
		var greenTransformToYellow = (MatrixTransform)green.TransformToVisual(yellow);
		Assert.AreEqual("1,0,0,1,0,-100", greenTransformToYellow.Matrix.ToString());
		var greenTransformToBlue = (MatrixTransform)green.TransformToVisual(blue);
		Assert.AreEqual("1,0,0,1,100,-100", greenTransformToBlue.Matrix.ToString());

		var blueTransformToRed = (MatrixTransform)blue.TransformToVisual(red);
		Assert.AreEqual("1,0,0,1,0,100", blueTransformToRed.Matrix.ToString());
		var blueTransformToYellow = (MatrixTransform)blue.TransformToVisual(yellow);
		Assert.AreEqual("1,0,0,1,-100,0", blueTransformToYellow.Matrix.ToString());
		var blueTransformToGreen = (MatrixTransform)blue.TransformToVisual(green);
		Assert.AreEqual("1,0,0,1,-100,100", blueTransformToGreen.Matrix.ToString());

		var yellowTransformToGreen = (MatrixTransform)yellow.TransformToVisual(green);
		Assert.AreEqual("1,0,0,1,0,100", yellowTransformToGreen.Matrix.ToString());
		var yellowTransformToRed = (MatrixTransform)yellow.TransformToVisual(red);
		Assert.AreEqual("1,0,0,1,100,100", yellowTransformToRed.Matrix.ToString());
		var yellowTransformToBlue = (MatrixTransform)yellow.TransformToVisual(blue);
		Assert.AreEqual("1,0,0,1,100,0", yellowTransformToBlue.Matrix.ToString());

		var redTransformToGrid = (MatrixTransform)red.TransformToVisual(grid);
		Assert.IsTrue(redTransformToGrid.Matrix.IsIdentity);
		var greenTransformToGrid = (MatrixTransform)green.TransformToVisual(grid);
		Assert.AreEqual("1,0,0,1,100,0", greenTransformToGrid.Matrix.ToString());
		var yellowTransformToGrid = (MatrixTransform)yellow.TransformToVisual(grid);
		Assert.AreEqual("1,0,0,1,100,100", yellowTransformToGrid.Matrix.ToString());
		var blueTransformToGrid = (MatrixTransform)blue.TransformToVisual(grid);
		Assert.AreEqual("1,0,0,1,0,100", blueTransformToGrid.Matrix.ToString());

		var dX = Window.Current.Bounds.Width - (Window.Current.Bounds.Width - 200) / 2;
		var dY = (Window.Current.Bounds.Height - 200) / 2;
		var gridTransformToRoot = (MatrixTransform)grid.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{dX},{dY}", gridTransformToRoot.Matrix.ToString());

		var redTransformToRoot = (MatrixTransform)red.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{dX},{dY}", redTransformToRoot.Matrix.ToString());
	}
}
