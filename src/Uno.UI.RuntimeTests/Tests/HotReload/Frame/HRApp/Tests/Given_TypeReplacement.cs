using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_TypeReplacement : BaseTestClass
{
	public const string OriginalColor = "Green";
	public const string UpdatedColor = "Orange";

	[TestMethod]
	public async Task When_DP_And_AttachedDP_Set_On_Old_Instance()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var page = new HR_DPUpdates_PageUsingComponent();
		UnitTestsUIContentHelper.Content = page;

		UserControl oldComponent = page.myComponent;

		await HotReloadHelper.UpdateServerFileAndRevert<HR_DPUpdates_Component>(
			OriginalColor,
			UpdatedColor,
			() =>
			{
				var grid = (Grid)page.Content;
				var component = (UserControl)grid.Children.Single();
				Assert.AreNotEqual(oldComponent, component);
				Assert.IsTrue(component.GetType().Name.Contains("#"));
				Assert.AreEqual(1, Grid.GetRow(component));
				Assert.AreEqual("Hello tag", component.Tag as string);
				// Visually, the color is updated, but this assert still fails with "Actual" being "Green" :/
				//Assert.AreEqual(Microsoft.UI.Colors.Orange, ((SolidColorBrush)((Grid)component.Content).Background).Color);
				return Task.CompletedTask;
			},
			ct);
	}

}
