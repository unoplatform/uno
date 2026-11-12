using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_ShapeVisual
{
	[RequiresFullWindow]
	[RequiresScaling(1f)]
	[TestMethod]
	// SkiaUIKit: test times out in CI https://github.com/unoplatform/uno-private/issues/805
	// NativeWinUI: the reference images are Uno renders (see the ShapeVisual.Paint clipping note below), and WinUI's output differs from them.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ShapeVisual_ViewBox_Shape_Combinations()
	{
		if (OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("this test is failing on browser, see https://github.com/unoplatform/uno-private/issues/704");
			return;
		}

		// runtime test version of the ShapeVisualClipping sample

		// The reference images are currently not very accurate due to the commented clipping line in ShapeVisual.Paint.
		// Restoring that line gives results identical to WinUI.
		var counter = 1;
		foreach (var shapeVisualSize in (int[])[20, 40, 80])
		{
			foreach (var spriteOffset in (int[])[-40, -20, 20, 40])
			{
				foreach (var viewboxSize in (int[])[20, 40, 80, 160])
				{
					foreach (var viewboxOffset in (int[])[20, 40, 80])
					{
						var element = new Border
						{
							Height = 40,
							Width = 40,
							Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
						};

						var elementVisual = ElementCompositionPreview.GetElementVisual(element);
						var compositor = elementVisual.Compositor;
						var shapeVisual = compositor.CreateShapeVisual();

						var spriteShape = compositor.CreateSpriteShape();
						spriteShape.Geometry = compositor.CreateRectangleGeometry();
						((CompositionRectangleGeometry)spriteShape.Geometry).Size = new Vector2(40, 40);
						spriteShape.Offset = new Vector2(spriteOffset, spriteOffset);
						spriteShape.FillBrush = compositor.CreateColorBrush(Microsoft.UI.Colors.Blue);
						shapeVisual.Shapes.Add(spriteShape);
						shapeVisual.Size = new Vector2(shapeVisualSize, shapeVisualSize);
						shapeVisual.ViewBox = compositor.CreateViewBox();
						shapeVisual.ViewBox.Size = new Vector2(viewboxSize, viewboxSize);
						shapeVisual.ViewBox.Offset = new Vector2(viewboxOffset, viewboxOffset);
						((ContainerVisual)elementVisual).Children.InsertAtTop(shapeVisual);

						var border = new Border
						{
							Width = 500,
							Height = 500,
							Background = new SolidColorBrush(Microsoft.UI.Colors.Green),
							Child = element
						};

						var filename = $"When_ShapeVisual_ViewBox_Shape_Combinations_{counter++}.png";
						var referenceImage = new Image
						{
							Width = 500,
							Height = 500,
							Source = new BitmapImage(new Uri($"ms-appx:/Assets/When_ShapeVisual_ViewBox_Shape_Combinations/{filename}"))
						};

						var imageOpened = false;
						referenceImage.ImageOpened += (s, e) => imageOpened = true;

						await UITestHelper.Load(new StackPanel()
						{
							Children = { border, referenceImage },
							Spacing = 10,
							Orientation = Orientation.Horizontal
						});

						await TestServices.WindowHelper.WaitFor(() => imageOpened);

						var screenShot1 = await UITestHelper.ScreenShot(border);
						var screenShot2 = await UITestHelper.ScreenShot(referenceImage);
						// To generate the images
						// await screenShot1.Save(filename);

						// there can be a very small _bit_ difference when drawing with metal on some platforms
						if (OperatingSystem.IsMacOS() || OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
						{
							await ImageAssert.AreSimilarAsync(screenShot1, screenShot2);
						}
						else
						{
							// with anti-aliasing in CompositionColorBrush, there can be some minor differences sometimes, but not always
							// so here we first check for pixel equality, and only if that fails we check for similarity,
							// to avoid the performance cost of the similarity check when not needed.
							var pixelEqual = await ImageAssert.AreRenderTargetBitmapsEqualAsync(screenShot1.Bitmap, screenShot2.Bitmap);
							if (!pixelEqual)
							{
								await ImageAssert.AreSimilarAsync(screenShot1, screenShot2);
							}
						}
					}
				}
			}
		}
	}

	// The ViewBox scales and positions the shapes only. Shapes that repaint every frame paint straight into the session the
	// children then draw in, so the ViewBox must not stay on it.
	[TestMethod]
	// NativeWinUI: the surface brush's source isn't in the visual tree, and repainting every frame is an Uno render path.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ShapeVisual_ViewBox_Then_Children_Not_Scaled(bool repaintsEveryFrame)
	{
		using var _ = UITestHelper.ResetWindowContent();
		var actual = CreateViewBoxShapes(repaintsEveryFrame, greenAsChild: true);
		var expected = CreateViewBoxShapes(repaintsEveryFrame, greenAsChild: false);
		await UITestHelper.Load(new StackPanel { Children = { actual, expected } });

		var expectedScreenshot = await UITestHelper.ScreenShot(expected);
		var bounds = new System.Drawing.Rectangle(0, 0, expectedScreenshot.Width, expectedScreenshot.Height);
		ImageAssert.HasColorInRectangle(expectedScreenshot, bounds, Microsoft.UI.Colors.Red, tolerance: 10);
		ImageAssert.HasColorInRectangle(expectedScreenshot, bounds, Microsoft.UI.Colors.Green, tolerance: 10);
		await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot(actual), expectedScreenshot);
	}

	// A ShapeVisual of thousands of moving shapes repaints every frame; bounding what it paints must not union their
	// geometries (each union costs more with every shape), or every frame takes minutes.
	[TestMethod]
	public async Task When_ShapeVisual_Has_Many_Moving_Shapes_Then_Frames_Stay_Fast()
	{
		using var _ = UITestHelper.ResetWindowContent();
		var host = new Border { Width = 400, Height = 400 };
		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2(400, 400);
		// A point cloud like the ScrollableShapesWinUI sample: small dots spread over a projected sphere.
		var dot = compositor.CreateEllipseGeometry();
		dot.Radius = new Vector2(0.7f, 0.7f);
		var brush = compositor.CreateColorBrush(Microsoft.UI.Colors.Blue);
		var shapes = new CompositionSpriteShape[3000];
		for (var i = 0; i < shapes.Length; i++)
		{
			var theta = 2f * MathF.PI * i / 1.618034f;
			var phi = MathF.Acos(1f - 2f * i / shapes.Length);
			shapes[i] = compositor.CreateSpriteShape(dot);
			shapes[i].FillBrush = brush;
			shapes[i].Offset = new Vector2(200 + 150 * MathF.Sin(phi) * MathF.Cos(theta), 200 + 150 * MathF.Cos(phi));
			shapeVisual.Shapes.Add(shapes[i]);
		}
		ElementCompositionPreview.SetElementChildVisual(host, shapeVisual);

		// Every frame moves every shape, as the sample's animation does.
		var step = 1f;
		EventHandler<object> move = (_, _) =>
		{
			step = -step;
			foreach (var shape in shapes)
			{
				shape.Offset += new Vector2(step, 0);
			}
		};
		CompositionTarget.Rendering += move;
		try
		{
			await UITestHelper.Load(host);
			await UITestHelper.WaitForRender(frameCount: 6, timeoutMS: 10_000, message: "six frames of 3000 moving shapes");
		}
		finally
		{
			CompositionTarget.Rendering -= move;
		}
	}

	// A red square the ViewBox scales 2x, then a green square drawn after it: as the ShapeVisual's child, or as its sibling,
	// which nothing the ShapeVisual paints can reach.
	private static Border CreateViewBoxShapes(bool repaintsEveryFrame, bool greenAsChild)
	{
		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		CompositionBrush fill = compositor.CreateColorBrush(Microsoft.UI.Colors.Red);
		if (repaintsEveryFrame)
		{
			var source = compositor.CreateSpriteVisual();
			source.Size = new Vector2(10, 10);
			source.Brush = fill;

			var surface = compositor.CreateVisualSurface();
			surface.SourceVisual = source;
			surface.SourceSize = new Vector2(10, 10);
			fill = compositor.CreateSurfaceBrush(surface);
		}

		var square = compositor.CreateSpriteShape();
		square.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)square.Geometry).Size = new Vector2(10, 10);
		square.FillBrush = fill;

		var shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2(100, 100);
		shapeVisual.ViewBox = compositor.CreateViewBox();
		shapeVisual.ViewBox.Size = new Vector2(50, 50);
		shapeVisual.Shapes.Add(square);

		var green = compositor.CreateSpriteVisual();
		green.Size = new Vector2(20, 20);
		green.Offset = new Vector3(60, 60, 0);
		green.Brush = compositor.CreateColorBrush(Microsoft.UI.Colors.Green);

		var root = compositor.CreateContainerVisual();
		root.Children.InsertAtTop(shapeVisual);
		(greenAsChild ? shapeVisual : root).Children.InsertAtTop(green);

		ElementCompositionPreview.SetElementChildVisual(host, root);
		return host;
	}
}
