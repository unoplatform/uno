#if __SKIA__ || __WASM__ || WINAPPSDK
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// BC38: <c>Background</c> is declared per-type to match WinUI (no longer on <see cref="FrameworkElement"/>).
/// These tests assert it still renders on the WinUI background-painter types.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Background
{
	[TestMethod]
	public async Task When_Border_Background_Renders()
	{
		var SUT = new Border
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(Colors.Red),
		};
		await UITestHelper.Load(SUT);

		var screenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(screenshot, new Point(25, 25), Colors.Red, tolerance: 5);
	}

	[TestMethod]
	public async Task When_Panel_Background_Renders()
	{
		var SUT = new Grid
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(Colors.Red),
		};
		await UITestHelper.Load(SUT);

		var screenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(screenshot, new Point(25, 25), Colors.Red, tolerance: 5);
	}

	[TestMethod]
	public async Task When_ContentPresenter_Background_Renders()
	{
		var SUT = new ContentPresenter
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(Colors.Red),
		};
		await UITestHelper.Load(SUT);

		var screenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(screenshot, new Point(25, 25), Colors.Red, tolerance: 5);
	}

	[TestMethod]
	public void When_Control_Background_Is_Declared()
	{
		// Background lives on Control, so every Control subclass shares the same backing property.
		var button = new Button();
		var brush = new SolidColorBrush(Colors.Red);
		button.Background = brush;

		Assert.AreEqual(brush, button.Background);
		Assert.AreEqual(brush, button.GetValue(Control.BackgroundProperty));
	}

	[TestMethod]
	public void When_Painters_Declare_Independent_Background()
	{
		// Each non-Control painter owns its Background property, independently of Control.
		Assert.AreNotSame(Control.BackgroundProperty, Panel.BackgroundProperty);
		Assert.AreNotSame(Control.BackgroundProperty, Border.BackgroundProperty);
		Assert.AreNotSame(Control.BackgroundProperty, ContentPresenter.BackgroundProperty);
		Assert.AreNotSame(Panel.BackgroundProperty, Border.BackgroundProperty);

		var brush = new SolidColorBrush(Colors.Blue);
		var grid = new Grid { Background = brush };
		Assert.AreEqual(brush, grid.GetValue(Panel.BackgroundProperty));
	}
}
#endif
