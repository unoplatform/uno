using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_AlphaMask
{
	[TestMethod]
	public async Task When_Shape_GetAlphaMask_Returns_Brush()
	{
		var ellipse = new Ellipse
		{
			Width = 100,
			Height = 100,
			Fill = new SolidColorBrush(Colors.Blue)
		};

		await UITestHelper.Load(ellipse);

		var brush = ellipse.GetAlphaMask();
		Assert.IsNotNull(brush);
		Assert.IsInstanceOfType(brush, typeof(CompositionBrush));
	}

	[TestMethod]
	public async Task When_TextBlock_GetAlphaMask_Returns_Brush()
	{
		var textBlock = new TextBlock
		{
			Text = "Test",
			FontSize = 24,
			Foreground = new SolidColorBrush(Colors.Blue)
		};

		await UITestHelper.Load(textBlock);

		var brush = textBlock.GetAlphaMask();
		Assert.IsNotNull(brush);
		Assert.IsInstanceOfType(brush, typeof(CompositionBrush));
	}

	[TestMethod]
	public async Task When_Image_GetAlphaMask_Returns_Brush()
	{
		var image = new Image
		{
			Width = 100,
			Height = 100,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/test_image_100_100.png"))
		};

		var tcs = new TaskCompletionSource<bool>();
		image.ImageOpened += (s, e) => tcs.TrySetResult(true);
		image.ImageFailed += (s, e) => tcs.TrySetResult(false);

		await UITestHelper.Load(image);
		var opened = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task && tcs.Task.Result;
		Assert.IsTrue(opened, "Image failed to load");

		var brush = image.GetAlphaMask();
		Assert.IsNotNull(brush);
		Assert.IsInstanceOfType(brush, typeof(CompositionBrush));
	}

	[TestMethod]
	public async Task When_Shape_AlphaMask_With_MaskBrush()
	{
		var ellipse = new Ellipse
		{
			Width = 100,
			Height = 100,
			Fill = new SolidColorBrush(Colors.Blue)
		};

		var host = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Yellow)
		};

		var canvas = new Canvas
		{
			Width = 100,
			Height = 100
		};
		host.Child = canvas;

		var container = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = { ellipse, host }
		};

		await UITestHelper.Load(container);
		await TestServices.WindowHelper.WaitForIdle();

		var compositor = ElementCompositionPreview.GetElementVisual(canvas).Compositor;

		var alphaMask = ellipse.GetAlphaMask();
		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = compositor.CreateColorBrush(Colors.Red);
		maskBrush.Mask = alphaMask;

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = maskBrush;
		spriteVisual.Size = new Vector2(100, 100);
		ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(200);

		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Red, tolerance: 25);
		ImageAssert.HasColorAt(screenshot, 2, 2, Colors.Yellow, tolerance: 25);
	}

	[TestMethod]
	public async Task When_TextBlock_AlphaMask_With_MaskBrush()
	{
		var textBlock = new TextBlock
		{
			Text = "XX",
			FontSize = 60,
			FontWeight = Microsoft.UI.Text.FontWeights.Black,
			Foreground = new SolidColorBrush(Colors.Blue),
			Width = 120,
			Height = 80
		};

		var host = new Border
		{
			Width = 120,
			Height = 80,
			Background = new SolidColorBrush(Colors.Yellow)
		};

		var canvas = new Canvas
		{
			Width = 120,
			Height = 80
		};
		host.Child = canvas;

		var container = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = { textBlock, host }
		};

		await UITestHelper.Load(container);
		await TestServices.WindowHelper.WaitForIdle();

		var compositor = ElementCompositionPreview.GetElementVisual(canvas).Compositor;

		var alphaMask = textBlock.GetAlphaMask();
		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = compositor.CreateColorBrush(Colors.Red);
		maskBrush.Mask = alphaMask;

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = maskBrush;
		spriteVisual.Size = new Vector2(120, 80);
		ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(200);

		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 60, 40, Colors.Red, tolerance: 40);
	}

	[TestMethod]
	public async Task When_Image_AlphaMask_With_MaskBrush()
	{
		var image = new Image
		{
			Width = 100,
			Height = 100,
			Stretch = Stretch.UniformToFill,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/test_image_100_100.png"))
		};

		var tcs = new TaskCompletionSource<bool>();
		image.ImageOpened += (s, e) => tcs.TrySetResult(true);
		image.ImageFailed += (s, e) => tcs.TrySetResult(false);

		var host = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Yellow)
		};

		var canvas = new Canvas
		{
			Width = 100,
			Height = 100
		};
		host.Child = canvas;

		var container = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = { image, host }
		};

		await UITestHelper.Load(container);
		var opened = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task && tcs.Task.Result;
		Assert.IsTrue(opened, "Image failed to load");
		await TestServices.WindowHelper.WaitForIdle();

		var compositor = ElementCompositionPreview.GetElementVisual(canvas).Compositor;

		var alphaMask = image.GetAlphaMask();
		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = compositor.CreateColorBrush(Colors.Green);
		maskBrush.Mask = alphaMask;

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = maskBrush;
		spriteVisual.Size = new Vector2(100, 100);
		ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(200);

		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Green, tolerance: 25);
	}

	[TestMethod]
	public async Task When_Image_AlphaMask_With_Transparent_Corners()
	{
		// PersonPicture.png is a circular image: opaque in the center, transparent corners
		var image = new Image
		{
			Width = 100,
			Height = 100,
			Stretch = Stretch.UniformToFill,
			Source = new BitmapImage(new Uri("ms-appx:///Assets/PersonPicture.png"))
		};

		var tcs = new TaskCompletionSource<bool>();
		image.ImageOpened += (s, e) => tcs.TrySetResult(true);
		image.ImageFailed += (s, e) => tcs.TrySetResult(false);

		var host = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Yellow)
		};

		var canvas = new Canvas
		{
			Width = 100,
			Height = 100
		};
		host.Child = canvas;

		var container = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = { image, host }
		};

		await UITestHelper.Load(container);
		var opened = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task && tcs.Task.Result;
		Assert.IsTrue(opened, "PersonPicture.png failed to load");
		await TestServices.WindowHelper.WaitForIdle();

		var compositor = ElementCompositionPreview.GetElementVisual(canvas).Compositor;

		var alphaMask = image.GetAlphaMask();
		var maskBrush = compositor.CreateMaskBrush();
		maskBrush.Source = compositor.CreateColorBrush(Colors.Red);
		maskBrush.Mask = alphaMask;

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = maskBrush;
		spriteVisual.Size = new Vector2(100, 100);
		ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(200);

		var screenshot = await UITestHelper.ScreenShot(host);

		// Center is opaque — alpha mask lets the red color through
		ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Red, tolerance: 25);

		// Corners are transparent — alpha mask blocks the color, yellow background shows
		ImageAssert.HasColorAt(screenshot, 2, 2, Colors.Yellow, tolerance: 25);
		ImageAssert.HasColorAt(screenshot, 97, 2, Colors.Yellow, tolerance: 25);
		ImageAssert.HasColorAt(screenshot, 2, 97, Colors.Yellow, tolerance: 25);
		ImageAssert.HasColorAt(screenshot, 97, 97, Colors.Yellow, tolerance: 25);
	}
}
