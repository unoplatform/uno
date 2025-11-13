#if __WASM__ || __SKIA__
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_SystemFocusVisual
{
	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Focused_Element_Scrolled()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive($"Not supported under XAML islands");
		}
		var scrollViewer = new ScrollViewer()
		{
			Height = 200,
			Margin = ThicknessHelper.FromUniformLength(30)
		};
		var button = new Button()
		{
			Content = "Test",
			FocusVisualPrimaryThickness = ThicknessHelper.FromUniformLength(10),
			FocusVisualSecondaryThickness = ThicknessHelper.FromUniformLength(10),
		};
		var topBorder = new Border() { Height = 150, Width = 300, Background = SolidColorBrushHelper.Blue };
		var bottomBorder = new Border() { Height = 300, Width = 300, Background = SolidColorBrushHelper.Red };
		var stackPanel = new StackPanel()
		{
			Children =
			{
				topBorder,
				button,
				bottomBorder,
			}
		};

		scrollViewer.Content = stackPanel;

		TestServices.WindowHelper.WindowContent = scrollViewer;
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		var visualTree = TestServices.WindowHelper.XamlRoot.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		var focusVisual = focusVisualLayer.Children.First();

		var transform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var initialPoint = transform.TransformPoint(default);

		scrollViewer.ChangeView(null, 100, null, true);

		await TestServices.WindowHelper.WaitFor(() =>
		{
			transform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
			var currentPoint = transform.TransformPoint(default);

			return currentPoint.Y < initialPoint.Y;
		});

		await TestServices.WindowHelper.WaitForIdle();

		transform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var scrolledPoint = transform.TransformPoint(default);
		Assert.AreEqual(initialPoint.Y - 100, scrolledPoint.Y, 0.5);
	}

	[TestMethod]
#if __WASM__
	[Ignore("RenderTargetBitmap is not implemented")]
#endif
	public async Task When_Focused_Element_Scrolled_Clipping()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive($"Not supported under XAML islands");
		}
		var sp = new StackPanel();
		var sv = new ScrollViewer
		{
			Height = 70,
			Content = sp
		};
		var buttons = Enumerable.Range(0, 10).Select(i => new Button
		{
			Content = $"{i}"
		}).ToList();
		sp.Children.AddRange(buttons);

		var border = new Border
		{
			Height = 130,
			Padding = new Thickness(0, 30, 0, 0),
			Child = sv
		};

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForIdle();

		buttons[2].Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		var visualTree = TestServices.WindowHelper.XamlRoot.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		for (var i = 0; i < 15; i++)
		{
			sv.ChangeView(null, 10 * i, null, true);
			await TestServices.WindowHelper.WaitForIdle();

			var screenShot = await UITestHelper.ScreenShot(border, true);
			ImageAssert.DoesNotHaveColorInRectangle(screenShot, new Rectangle(0, 0, 5, 30), ((SolidColorBrush)buttons[1].FocusVisualPrimaryBrush).Color);
			ImageAssert.DoesNotHaveColorInRectangle(screenShot, new Rectangle(0, screenShot.Height - 30, 5, 30), ((SolidColorBrush)buttons[2].FocusVisualPrimaryBrush).Color);
		}
	}

	[TestMethod]
	[RequiresFullWindow]
#if __ANDROID__ || __APPLE_UIKIT__
	[Ignore("Disabled on iOS/Android https://github.com/unoplatform/uno/issues/9080")]
#endif
	public async Task When_Focused_Element_Transformed()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive($"Not supported under XAML islands");
		}
		var button = new Button()
		{
			Content = "Transform Test",
			FocusVisualPrimaryThickness = ThicknessHelper.FromUniformLength(10),
			FocusVisualSecondaryThickness = ThicknessHelper.FromUniformLength(10),
			RenderTransform = new RotateTransform
			{
				Angle = -45
			},
			RenderTransformOrigin = new Windows.Foundation.Point(1, 1),
		};
		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		var visualTree = TestServices.WindowHelper.XamlRoot.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		var focusVisual = focusVisualLayer.Children.First();

		var focusTransform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var focusPoint = focusTransform.TransformPoint(default);

		var buttonTransform = button.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var buttonPoint = buttonTransform.TransformPoint(default);

		Assert.AreEqual(focusPoint.X, buttonPoint.X);
		Assert.AreEqual(focusPoint.Y, buttonPoint.Y);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Keyboard_Focus()
	{
		// This sequence on full window test previously caused infinite layout loop
		var button = new Button() { Content = "Test" };
		await UITestHelper.Load(button);
		button.Focus(FocusState.Keyboard);
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Focused_Element_In_Scaled_Viewbox()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive($"Not supported under XAML islands");
		}

		var button = new Button()
		{
			Content = "Test",
			FocusVisualPrimaryThickness = ThicknessHelper.FromUniformLength(2),
			FocusVisualSecondaryThickness = ThicknessHelper.FromUniformLength(2),
		};

		var innerViewbox = new Viewbox()
		{
			Width = 300,
			Height = 300,
			Child = button
		};

		var outerViewbox = new Viewbox()
		{
			Width = 100,
			Height = 100,
			Child = innerViewbox
		};

		TestServices.WindowHelper.WindowContent = outerViewbox;
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		var visualTree = TestServices.WindowHelper.XamlRoot.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		var focusVisual = focusVisualLayer.Children.First();

		// The focus visual should be positioned at the same location as the button
		var focusTransform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var focusPoint = focusTransform.TransformPoint(default);

		var buttonTransform = button.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var buttonPoint = buttonTransform.TransformPoint(default);

		// Allow small tolerance for rounding errors
		Assert.AreEqual(buttonPoint.X, focusPoint.X, 1.0, $"Focus visual X position {focusPoint.X} should match button X position {buttonPoint.X}");
		Assert.AreEqual(buttonPoint.Y, focusPoint.Y, 1.0, $"Focus visual Y position {focusPoint.Y} should match button Y position {buttonPoint.Y}");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Focused_Element_With_Multiple_Parent_Transforms()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive($"Not supported under XAML islands");
		}

		var button = new Button()
		{
			Content = "Test",
			Width = 80,
			Height = 30,
			FocusVisualPrimaryThickness = ThicknessHelper.FromUniformLength(2),
			FocusVisualSecondaryThickness = ThicknessHelper.FromUniformLength(2),
		};

		// Parent with ScaleTransform
		var scaleContainer = new Border()
		{
			Width = 200,
			Height = 200,
			RenderTransform = new ScaleTransform
			{
				ScaleX = 1.5,
				ScaleY = 1.5
			},
			Child = button
		};

		// Parent with RotateTransform
		var rotateContainer = new Border()
		{
			Width = 400,
			Height = 400,
			RenderTransform = new RotateTransform
			{
				Angle = 15
			},
			Child = scaleContainer
		};

		// Parent with TranslateTransform
		var translateContainer = new Border()
		{
			RenderTransform = new TranslateTransform
			{
				X = 50,
				Y = 30
			},
			Child = rotateContainer
		};

		TestServices.WindowHelper.WindowContent = translateContainer;
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		var visualTree = TestServices.WindowHelper.XamlRoot.VisualTree;
		var focusVisualLayer = visualTree?.FocusVisualRoot;

		Assert.IsNotNull(focusVisualLayer);
		Assert.AreEqual(1, focusVisualLayer.Children.Count);

		var focusVisual = focusVisualLayer.Children.First();

		// The focus visual should be positioned at the same location as the button
		// despite all the parent transforms (translate, rotate, scale)
		var focusTransform = focusVisual.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var focusPoint = focusTransform.TransformPoint(default);

		var buttonTransform = button.TransformToVisual(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement);
		var buttonPoint = buttonTransform.TransformPoint(default);

		// Allow tolerance for rounding errors, especially with rotation
		Assert.AreEqual(buttonPoint.X, focusPoint.X, 2.0, $"Focus visual X position {focusPoint.X} should match button X position {buttonPoint.X}");
		Assert.AreEqual(buttonPoint.Y, focusPoint.Y, 2.0, $"Focus visual Y position {focusPoint.Y} should match button Y position {buttonPoint.Y}");

		// validate bottom right point
		var focusBottomRight = focusTransform.TransformPoint(new Windows.Foundation.Point(button.ActualWidth, button.ActualHeight));
		var buttonBottomRight = buttonTransform.TransformPoint(new Windows.Foundation.Point(button.ActualWidth, button.ActualHeight));

		Assert.AreEqual(buttonBottomRight.X, focusBottomRight.X, 2.0, $"Focus visual bottom-right X position {focusBottomRight.X} should match button bottom-right X position {buttonBottomRight.X}");
		Assert.AreEqual(buttonBottomRight.Y, focusBottomRight.Y, 2.0, $"Focus visual bottom-right Y position {focusBottomRight.Y} should match button bottom-right Y position {buttonBottomRight.Y}");
	}
}
#endif
