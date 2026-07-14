using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
public class Given_XBindOneWayItemsSource_13433
{
	// Repro: Mode=OneWay x:Bind on ItemsSource was reported to clear the source property when it has a setter.
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/13433")]
	public async Task When_OneWay_xBind_To_ItemsSource_Items_Are_Visible_13433()
	{
		var page = new XBindOneWayItemsSourcePage_13433();
		await UITestHelper.Load(page);
		await UITestHelper.WaitForIdle();

		Assert.IsNotNull(page.Items, "Page.Items should not have been reset to null by the OneWay binding.");
		Assert.AreEqual(3, page.Items.Count, "Page.Items should still hold the 3 initial entries.");
		Assert.AreEqual(
			3,
			page.ItemsListView.Items.Count,
			"ListView should display the 3 items from the source. " +
			"See https://github.com/unoplatform/uno/issues/13433");
	}
}
