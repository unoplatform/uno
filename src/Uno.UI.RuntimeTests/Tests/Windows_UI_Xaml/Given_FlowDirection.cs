using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
#if !SUPPORTS_RTL && !WINAPPSDK
[Ignore("RTL is not supported")]
#endif
public class Given_FlowDirection
{
	private static Rectangle CreateRectangle(Stretch stretch, Color fillColor)
	{
		return new Rectangle()
		{
			Stretch = stretch,
			UseLayoutRounding = false,
			Fill = new SolidColorBrush(fillColor),
		};
	}

	private static Grid CreateGrid(Color background)
	{
		return new Grid()
		{
			UseLayoutRounding = false,
			Background = new SolidColorBrush(background),
		};
	}

	private static Grid CreateRTLGrid200x200WithTwoRowsAndTwoColumns()
	{
		return new Grid()
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
			UseLayoutRounding = false,
		};
	}

	private static void AssertRects(Rect rect1, Rect rect2)
	{
		Assert.AreEqual(rect1.X, rect2.X);
		Assert.AreEqual(rect1.Y, rect2.Y);
		Assert.AreEqual(rect1.Width, rect2.Width, delta: 1);
		Assert.AreEqual(rect1.Height, rect2.Height, delta: 1);
	}

	private static Rect GetWindowBounds() =>
#if WINAPPSDK
		Window.Current.Bounds;
#else
		TestServices.WindowHelper.XamlRoot.Bounds;
#endif

	private static Rect Get100x100RectAt(double x, double y) => new Rect(new Point(x, y), new Size(100, 100));
	private static Rect Get50x50RectAt(double x, double y) => new Rect(new Point(x, y), new Size(50, 50));

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_RTL()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rootGrid = CreateGrid(Colors.WhiteSmoke);

		var grid = CreateRTLGrid200x200WithTwoRowsAndTwoColumns();

		rootGrid.Children.Add(grid);

		var red = CreateRectangle(Stretch.Fill, Colors.Red);
		Grid.SetRow(red, 0);
		Grid.SetColumn(red, 0);

		var green = CreateRectangle(Stretch.Fill, Colors.Green);
		Grid.SetRow(green, 0);
		Grid.SetColumn(green, 1);

		var blue = CreateRectangle(Stretch.Fill, Colors.Blue);
		Grid.SetRow(blue, 1);
		Grid.SetColumn(blue, 0);

		var yellow = CreateRectangle(Stretch.Fill, Colors.Yellow);
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

		TestServices.WindowHelper.WindowContent = rootGrid;
		await TestServices.WindowHelper.WaitForLoaded(rootGrid);

#if !__SKIA__ // RenderTargetBitmap doesn't handle MatrixTransformation properly, which is used in the case of RTL.
		var rendererGrid = new RenderTargetBitmap();
		await rendererGrid.RenderAsync(grid);
		var bitmapGrid = await RawBitmap.From(rendererGrid, grid);
		//await TestServices.WindowHelper.WaitForever();
		Assert.AreEqual(200, bitmapGrid.Width);
		Assert.AreEqual(200, bitmapGrid.Height);

		AssertRects(Get100x100RectAt(0, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(0, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Blue, tolerance: 10));
#endif

		var rendererRoot = new RenderTargetBitmap();
		await rendererRoot.RenderAsync(rootGrid);
		var bitmapRoot = await RawBitmap.From(rendererRoot, rootGrid);
		Assert.AreEqual(GetWindowBounds().Width, bitmapRoot.Width);
		Assert.AreEqual(GetWindowBounds().Height, bitmapRoot.Height);

		var dX = (bitmapRoot.Width - 200) / 2;
		var dY = (bitmapRoot.Height - 200) / 2;

		AssertRects(Get100x100RectAt(dX, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(dX + 100, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(dX, dY + 100), ImageAssert.GetColorBounds(bitmapRoot, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(dX + 100, dY + 100), ImageAssert.GetColorBounds(bitmapRoot, Colors.Blue, tolerance: 10));

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

		var m31 = GetWindowBounds().Width - (GetWindowBounds().Width - 200) / 2;
		var m32 = (GetWindowBounds().Height - 200) / 2;
		var gridTransformToRoot = (MatrixTransform)grid.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{m31},{m32}", gridTransformToRoot.Matrix.ToString());

		var redTransformToRoot = (MatrixTransform)red.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{m31},{m32}", redTransformToRoot.Matrix.ToString());
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_RTL_RenderTransform_Translation()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rootGrid = CreateGrid(Colors.WhiteSmoke);

		var grid = CreateRTLGrid200x200WithTwoRowsAndTwoColumns();
		grid.RenderTransform = new TranslateTransform()
		{
			X = 30,
			Y = 30,
		};

		rootGrid.Children.Add(grid);

		var red = CreateRectangle(Stretch.Fill, Colors.Red);
		Grid.SetRow(red, 0);
		Grid.SetColumn(red, 0);

		var green = CreateRectangle(Stretch.Fill, Colors.Green);
		Grid.SetRow(green, 0);
		Grid.SetColumn(green, 1);

		var blue = CreateRectangle(Stretch.Fill, Colors.Blue);
		Grid.SetRow(blue, 1);
		Grid.SetColumn(blue, 0);

		var yellow = CreateRectangle(Stretch.Fill, Colors.Yellow);
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

		TestServices.WindowHelper.WindowContent = rootGrid;
		await TestServices.WindowHelper.WaitForLoaded(rootGrid);

#if !__SKIA__ // RenderTargetBitmap doesn't handle MatrixTransformation properly, which is used in the case of RTL.
		var rendererGrid = new RenderTargetBitmap();
		await rendererGrid.RenderAsync(grid);
		var bitmapGrid = await RawBitmap.From(rendererGrid, grid);
		//await TestServices.WindowHelper.WaitForever();
		Assert.AreEqual(200, bitmapGrid.Width);
		Assert.AreEqual(200, bitmapGrid.Height);

		AssertRects(Get100x100RectAt(0, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(0, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Blue, tolerance: 10));
#endif

		var rendererRoot = new RenderTargetBitmap();
		await rendererRoot.RenderAsync(rootGrid);
		var bitmapRoot = await RawBitmap.From(rendererRoot, rootGrid);
		Assert.AreEqual(GetWindowBounds().Width, bitmapRoot.Width);
		Assert.AreEqual(GetWindowBounds().Height, bitmapRoot.Height);

		// We have a render transform of x=30 and y=30
		// Since the grid is RTL, the transform effect on x-axis will be negative.
		var dX = ((bitmapRoot.Width - 200) / 2) - 30;
		var dY = ((bitmapRoot.Height - 200) / 2) + 30;

		AssertRects(Get100x100RectAt(dX, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(dX + 100, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(dX, dY + 100), ImageAssert.GetColorBounds(bitmapRoot, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(dX + 100, dY + 100), ImageAssert.GetColorBounds(bitmapRoot, Colors.Blue, tolerance: 10));

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

		var m31 = GetWindowBounds().Width - (GetWindowBounds().Width - 200) / 2 - 30;
		var m32 = (GetWindowBounds().Height - 200) / 2 + 30;
		var gridTransformToRoot = (MatrixTransform)grid.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{m31},{m32}", gridTransformToRoot.Matrix.ToString());

		var redTransformToRoot = (MatrixTransform)red.TransformToVisual(null);
		Assert.AreEqual($"-1,0,0,1,{m31},{m32}", redTransformToRoot.Matrix.ToString());
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_RTL_RenderTransform_Scaling()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rootGrid = CreateGrid(Colors.WhiteSmoke);

		var grid = CreateRTLGrid200x200WithTwoRowsAndTwoColumns();
		grid.RenderTransform = new ScaleTransform()
		{
			ScaleX = 0.5,
			ScaleY = 0.5,
		};

		rootGrid.Children.Add(grid);

		var red = CreateRectangle(Stretch.Fill, Colors.Red);
		Grid.SetRow(red, 0);
		Grid.SetColumn(red, 0);

		var green = CreateRectangle(Stretch.Fill, Colors.Green);
		Grid.SetRow(green, 0);
		Grid.SetColumn(green, 1);

		var blue = CreateRectangle(Stretch.Fill, Colors.Blue);
		Grid.SetRow(blue, 1);
		Grid.SetColumn(blue, 0);

		var yellow = CreateRectangle(Stretch.Fill, Colors.Yellow);
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

		TestServices.WindowHelper.WindowContent = rootGrid;
		await TestServices.WindowHelper.WaitForLoaded(rootGrid);

#if !__SKIA__ // RenderTargetBitmap doesn't handle MatrixTransformation properly, which is used in the case of RTL.
		var rendererGrid = new RenderTargetBitmap();
		await rendererGrid.RenderAsync(grid);
		var bitmapGrid = await RawBitmap.From(rendererGrid, grid);
		//await TestServices.WindowHelper.WaitForever();
		Assert.AreEqual(200, bitmapGrid.Width);
		Assert.AreEqual(200, bitmapGrid.Height);

		AssertRects(Get100x100RectAt(0, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(0, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Blue, tolerance: 10));
#endif

		var rendererRoot = new RenderTargetBitmap();
		await rendererRoot.RenderAsync(rootGrid);
		var bitmapRoot = await RawBitmap.From(rendererRoot, rootGrid);
		Assert.AreEqual(GetWindowBounds().Width, bitmapRoot.Width);
		Assert.AreEqual(GetWindowBounds().Height, bitmapRoot.Height);

		// The "(bitmapRoot.Width - 200) / 2" part is the dX from the right direction (because of RTL)
		// So to get dX from the left, we get the total width, and subtract the space
		// occupied by grid (Grid width * ScaleX), and also subtract the dX from the right.
		var dX = bitmapRoot.Width - 100 - (bitmapRoot.Width - 200) / 2;
		var dY = (bitmapRoot.Height - 200) / 2;

		AssertRects(Get50x50RectAt(dX, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Green, tolerance: 10));
		AssertRects(Get50x50RectAt(dX + 50, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Red, tolerance: 10));
		AssertRects(Get50x50RectAt(dX, dY + 50), ImageAssert.GetColorBounds(bitmapRoot, Colors.Yellow, tolerance: 10));
		AssertRects(Get50x50RectAt(dX + 50, dY + 50), ImageAssert.GetColorBounds(bitmapRoot, Colors.Blue, tolerance: 10));

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

		var m31 = GetWindowBounds().Width - (GetWindowBounds().Width - 200) / 2;
		var m32 = (GetWindowBounds().Height - 200) / 2;
		var gridTransformToRoot = (MatrixTransform)grid.TransformToVisual(null);
		Assert.AreEqual($"-0.5,0,0,0.5,{m31},{m32}", gridTransformToRoot.Matrix.ToString());

		var redTransformToRoot = (MatrixTransform)red.TransformToVisual(null);
		Assert.AreEqual($"-0.5,0,0,0.5,{m31},{m32}", redTransformToRoot.Matrix.ToString());
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_RTL_RenderTransform_Composite()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		var rootGrid = CreateGrid(Colors.WhiteSmoke);

		var grid = CreateRTLGrid200x200WithTwoRowsAndTwoColumns();
		grid.RenderTransform = new CompositeTransform()
		{
			ScaleX = 0.5,
			ScaleY = 0.5,
			TranslateX = 30,
			TranslateY = 30,
		};

		rootGrid.Children.Add(grid);

		var red = CreateRectangle(Stretch.Fill, Colors.Red);
		Grid.SetRow(red, 0);
		Grid.SetColumn(red, 0);

		var green = CreateRectangle(Stretch.Fill, Colors.Green);
		Grid.SetRow(green, 0);
		Grid.SetColumn(green, 1);

		var blue = CreateRectangle(Stretch.Fill, Colors.Blue);
		Grid.SetRow(blue, 1);
		Grid.SetColumn(blue, 0);

		var yellow = CreateRectangle(Stretch.Fill, Colors.Yellow);
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

		TestServices.WindowHelper.WindowContent = rootGrid;
		await TestServices.WindowHelper.WaitForLoaded(rootGrid);

#if !__SKIA__ // RenderTargetBitmap doesn't handle MatrixTransformation properly, which is used in the case of RTL.
		var rendererGrid = new RenderTargetBitmap();
		await rendererGrid.RenderAsync(grid);
		var bitmapGrid = await RawBitmap.From(rendererGrid, grid);
		//await TestServices.WindowHelper.WaitForever();
		Assert.AreEqual(200, bitmapGrid.Width);
		Assert.AreEqual(200, bitmapGrid.Height);

		AssertRects(Get100x100RectAt(0, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Green, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 0), ImageAssert.GetColorBounds(bitmapGrid, Colors.Red, tolerance: 10));
		AssertRects(Get100x100RectAt(0, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Yellow, tolerance: 10));
		AssertRects(Get100x100RectAt(100, 100), ImageAssert.GetColorBounds(bitmapGrid, Colors.Blue, tolerance: 10));
#endif

		var rendererRoot = new RenderTargetBitmap();
		await rendererRoot.RenderAsync(rootGrid);
		var bitmapRoot = await RawBitmap.From(rendererRoot, rootGrid);
		Assert.AreEqual(GetWindowBounds().Width, bitmapRoot.Width);
		Assert.AreEqual(GetWindowBounds().Height, bitmapRoot.Height);

		// The "(bitmapRoot.Width - 200) / 2" part is the dX from the right direction (because of RTL)
		// So to get dX from the left, we get the total width, and subtract the space
		// occupied by grid (Grid width * ScaleX), and also subtract the dX from the right.
		// We then subtract 30 because of the translate part of transform.
		// It's subtraction, not addition, because we are RTL.
		var dX = bitmapRoot.Width - 100 - (bitmapRoot.Width - 200) / 2 - 30;
		var dY = (bitmapRoot.Height - 200) / 2 + 30;

		AssertRects(Get50x50RectAt(dX, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Green, tolerance: 10));
		AssertRects(Get50x50RectAt(dX + 50, dY), ImageAssert.GetColorBounds(bitmapRoot, Colors.Red, tolerance: 10));
		AssertRects(Get50x50RectAt(dX, dY + 50), ImageAssert.GetColorBounds(bitmapRoot, Colors.Yellow, tolerance: 10));
		AssertRects(Get50x50RectAt(dX + 50, dY + 50), ImageAssert.GetColorBounds(bitmapRoot, Colors.Blue, tolerance: 10));

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

		var m31 = GetWindowBounds().Width - (GetWindowBounds().Width - 200) / 2 - 30;
		var m32 = (GetWindowBounds().Height - 200) / 2 + 30;
		var gridTransformToRoot = (MatrixTransform)grid.TransformToVisual(null);
		Assert.AreEqual($"-0.5,0,0,0.5,{m31},{m32}", gridTransformToRoot.Matrix.ToString());

		var redTransformToRoot = (MatrixTransform)red.TransformToVisual(null);
		Assert.AreEqual($"-0.5,0,0,0.5,{m31},{m32}", redTransformToRoot.Matrix.ToString());
	}
}
