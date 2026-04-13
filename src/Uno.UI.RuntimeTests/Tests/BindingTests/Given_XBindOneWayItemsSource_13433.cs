using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.XBindOneWayItemsSourceTests
{
	using Uno.UI.RuntimeTests.Tests;

	[TestClass]
	public class Given_XBindOneWayItemsSource_13433
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13433
		// {x:Bind Items, Mode=OneWay} on ListView.ItemsSource was reported to
		// reset/clear the source-side property if a setter is available, so the
		// list ends up empty even though Items had data. Removing Mode=OneWay
		// (the workaround) avoids the issue.
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_OneWay_xBind_To_ItemsSource_Items_Are_Visible_13433()
		{
			var page = new XBindOneWayItemsSourcePage_13433();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(page.Items, "Page.Items should not have been reset to null by the OneWay binding.");
			Assert.AreEqual(3, page.Items.Count, "Page.Items should still hold the 3 initial entries.");
			Assert.AreEqual(
				3,
				page.ItemsListView.Items.Count,
				"ListView should display the 3 items from the source. " +
				"See https://github.com/unoplatform/uno/issues/13433");
		}
	}
}
