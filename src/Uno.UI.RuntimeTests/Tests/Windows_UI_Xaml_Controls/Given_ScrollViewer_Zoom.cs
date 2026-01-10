using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer_Zoom
{
	[TestMethod]
	public async Task When_ChangeView_With_ZoomFactor()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Blue)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Initial zoom should be 1.0
		Assert.AreEqual(1.0f, sut.ZoomFactor, 0.01f, "Initial zoom factor should be 1.0");

		// Change to zoom factor 2.0
		var result = sut.ChangeView(null, null, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(result, "ChangeView should return true");
		Assert.AreEqual(2.0f, sut.ZoomFactor, 0.01f, "Zoom factor should be 2.0 after ChangeView");
	}

	[TestMethod]
	public async Task When_ZoomMode_Disabled_ZoomFactor_Stays_1()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Green)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Disabled,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Try to change zoom when disabled
		sut.ChangeView(null, null, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		// Zoom should remain at 1.0 when ZoomMode is Disabled
		Assert.AreEqual(1.0f, sut.ZoomFactor, 0.01f, "Zoom factor should stay 1.0 when ZoomMode is Disabled");
	}

	[TestMethod]
	public async Task When_Zoom_Respects_MinZoomFactor()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Red)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Try to zoom below minimum
		sut.ChangeView(null, null, 0.1f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		// Zoom should be clamped to minimum
		Assert.AreEqual(0.5f, sut.ZoomFactor, 0.01f, "Zoom factor should be clamped to MinZoomFactor");
	}

	[TestMethod]
	public async Task When_Zoom_Respects_MaxZoomFactor()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Yellow)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Try to zoom above maximum
		sut.ChangeView(null, null, 10.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		// Zoom should be clamped to maximum
		Assert.AreEqual(4.0f, sut.ZoomFactor, 0.01f, "Zoom factor should be clamped to MaxZoomFactor");
	}

	[TestMethod]
	public async Task When_Zoom_ScrollableExtent_Increases()
	{
		var content = new Border
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Colors.Purple)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		var initialScrollableWidth = sut.ScrollableWidth;
		var initialScrollableHeight = sut.ScrollableHeight;

		// Zoom to 2x
		sut.ChangeView(null, null, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		// Scrollable extent should increase with zoom
		// At 2x zoom, a 300px content in a 200px viewport should have more scrollable area
		Assert.IsTrue(sut.ScrollableWidth > initialScrollableWidth,
			$"ScrollableWidth should increase after zoom. Was {initialScrollableWidth}, now {sut.ScrollableWidth}");
		Assert.IsTrue(sut.ScrollableHeight > initialScrollableHeight,
			$"ScrollableHeight should increase after zoom. Was {initialScrollableHeight}, now {sut.ScrollableHeight}");
	}

	[TestMethod]
	public async Task When_ZoomMode_Changed_At_Runtime()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Orange)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Zoom to 2x while enabled
		sut.ChangeView(null, null, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(2.0f, sut.ZoomFactor, 0.01f, "Zoom should be 2.0 when enabled");

		// Disable zoom mode - current zoom should reset to 1
		sut.ZoomMode = ZoomMode.Disabled;
		await TestServices.WindowHelper.WaitForIdle();

		// When ZoomMode is Disabled, min and max are both set to 1,
		// which should clamp the current zoom to 1
		Assert.AreEqual(1.0f, sut.ZoomFactor, 0.01f, "Zoom should reset to 1.0 when ZoomMode is Disabled");
	}

	[TestMethod]
	public async Task When_ChangeView_With_Scroll_And_Zoom()
	{
		var content = new Border
		{
			Width = 1000,
			Height = 1000,
			Background = new SolidColorBrush(Colors.Cyan)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Change view with both scroll and zoom
		sut.ChangeView(100, 150, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(100, sut.HorizontalOffset, 1, "HorizontalOffset should be 100");
		Assert.AreEqual(150, sut.VerticalOffset, 1, "VerticalOffset should be 150");
		Assert.AreEqual(2.0f, sut.ZoomFactor, 0.01f, "ZoomFactor should be 2.0");
	}

	[TestMethod]
	public async Task When_Zoom_Visual_Scale_Applied()
	{
		var content = new Border
		{
			Width = 500,
			Height = 500,
			Background = new SolidColorBrush(Colors.Magenta)
		};

		var sut = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			ZoomMode = ZoomMode.Enabled,
			MinZoomFactor = 0.5f,
			MaxZoomFactor = 4.0f,
			Content = content
		};

		TestServices.WindowHelper.WindowContent = sut;
		await TestServices.WindowHelper.WaitForLoaded(sut);
		await TestServices.WindowHelper.WaitForIdle();

		// Zoom to 2x
		sut.ChangeView(null, null, 2.0f, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

#if HAS_UNO
		// Verify the visual scale was applied to the content
		var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(content);
		Assert.AreEqual(2.0f, visual.Scale.X, 0.01f, "Visual Scale.X should be 2.0");
		Assert.AreEqual(2.0f, visual.Scale.Y, 0.01f, "Visual Scale.Y should be 2.0");
#endif
	}
}
