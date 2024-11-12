using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_ShapeVisual
{
#if __SKIA__
	[RequiresFullWindow]
	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/18752")]
	public async Task When_ShapeVisual_ViewBox_Shape_Combinations()
	{
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
						await UITestHelper.Load(border);

						var screenShot1 = await UITestHelper.ScreenShot(border);
						var filename = $"When_ShapeVisual_ViewBox_Shape_Combinations_{counter++}.png";
						// To generate the images
						// await screenShot1.Save(filename);

						var referenceImage = new Image
						{
							Width = 500,
							Height = 500,
							Source = new Uri($"ms-appx:/Assets/When_ShapeVisual_ViewBox_Shape_Combinations/{filename}")
						};

						await UITestHelper.Load(referenceImage);
						var screenShot2 = await UITestHelper.ScreenShot(referenceImage);
						await ImageAssert.AreEqualAsync(screenShot1, screenShot2);
					}
				}
			}
		}
	}
#endif
}
