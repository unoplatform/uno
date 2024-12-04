using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Style_Override
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PageLevel_Style_Overrides_AppLevel()
	{
		var page = new Style_Override();
		TestServices.WindowHelper.WindowContent = page;
		await UITestHelper.Load(page);

		var buttonWithoutBasedOn = page.ButtonWithoutBasedOn;
		var buttonWithBasedOn = page.ButtonWithBasedOn;

		Assert.IsNotNull(buttonWithoutBasedOn);
		var backgroundWithoutBasedOn = buttonWithoutBasedOn.Background as SolidColorBrush;
		Assert.IsNotNull(backgroundWithoutBasedOn);
		Assert.AreEqual(Microsoft.UI.Colors.Red, backgroundWithoutBasedOn.Color);
		Assert.AreEqual(new CornerRadius(8), buttonWithoutBasedOn.CornerRadius);

		Assert.IsNotNull(buttonWithBasedOn);
		var backgroundWithBasedOn = buttonWithBasedOn.Background as SolidColorBrush;
		Assert.IsNotNull(backgroundWithBasedOn);
		Assert.AreEqual(Microsoft.UI.Colors.Red, backgroundWithBasedOn.Color);
		Assert.AreEqual(new CornerRadius(8), buttonWithBasedOn.CornerRadius);
	}
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AppLevel_Style_Applies()
	{
		var page = new Style_Override();
		TestServices.WindowHelper.WindowContent = page;
		await UITestHelper.Load(page);

		var button = new Button { Style = page.Resources["AppLevelButtonStyle"] as Style };
		TestServices.WindowHelper.WindowContent = button;
		await UITestHelper.Load(button);

		Assert.IsNotNull(button.Style);
		var backgroundBrush = button.Background as SolidColorBrush;
		Assert.IsNotNull(backgroundBrush);
		Assert.AreEqual(Microsoft.UI.Colors.Green, backgroundBrush.Color);
		Assert.AreEqual(new CornerRadius(4), button.CornerRadius);
	}


	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ControlLevel_Style_Applies()
	{
		var button = new Button
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
			CornerRadius = new CornerRadius(8)
		};

		TestServices.WindowHelper.WindowContent = button;
		await UITestHelper.Load(button);

		var backgroundBrush = button.Background as SolidColorBrush;
		Assert.IsNotNull(backgroundBrush);
		Assert.AreEqual(Microsoft.UI.Colors.Red, backgroundBrush.Color);
		Assert.AreEqual(new CornerRadius(8), button.CornerRadius);
	}

}
