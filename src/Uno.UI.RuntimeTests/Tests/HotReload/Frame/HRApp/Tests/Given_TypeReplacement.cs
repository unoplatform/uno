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
				Assert.AreEqual(Windows.UI.Colors.Orange, ((SolidColorBrush)((Grid)component.Content).Background).Color);
				return Task.CompletedTask;
			},
			ct);
	}

	[TestMethod]
	public async Task When_DP_Has_Binding()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var page = new HR_DPUpdates_Binding_PageUsingComponent();
		UnitTestsUIContentHelper.Content = page;

		UserControl oldComponent = page.myComponent;

		await HotReloadHelper.UpdateServerFileAndRevert<HR_DPUpdates_Binding_Component>(
			OriginalColor,
			UpdatedColor,
			() =>
			{
				var component = (UserControl)page.Content;
				Assert.AreNotEqual(oldComponent, component);
				Assert.IsTrue(component.GetType().Name.Contains("#"));

				Assert.IsNotNull(component.GetBindingExpression(FrameworkElement.TagProperty));

				page.Tag2 = "Updated tag";
				var componentGrid = (Grid)VisualTreeHelper.GetChild(component, 0);
				var tb = (TextBlock)componentGrid.Children.Single();
				Assert.AreEqual("Updated tag", component.Tag, "Tag should be updated on component");
				Assert.AreEqual("Updated tag", tb.Text, "Tag should be updated on component's TextBlock");
				return Task.CompletedTask;
			},
			ct);
	}
}
