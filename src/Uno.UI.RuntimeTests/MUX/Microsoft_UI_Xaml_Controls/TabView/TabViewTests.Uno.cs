using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	public partial class TabViewTests
	{
#if HAS_UNO
		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("Currently fails on iOS")]
#endif
		public async Task VerifyItemsAreCreatedOnlyOnce()
		{
			TabView tabView = null;
			await RunOnUIThread.ExecuteAsync(async () =>
			{
				tabView = new TabView();
				await UITestHelper.Load(tabView);

				var items = new ObservableCollection<int>()
				{
					1, 2, 3
				};

				int containerContentChangingCounter = 0;

				var listView = tabView.GetTemplateChild<TabViewListView>("TabListView");
				listView.ContainerContentChanging += (s, e) =>
				{
					if (e.ItemIndex == 0)
					{
						containerContentChangingCounter++;
					}
				};

				tabView.TabItemsSource = items;

				tabView.UpdateLayout();

				await TestServices.WindowHelper.WaitForIdle();

				// Only one container should be generated for the first item.
				Assert.AreEqual(1, containerContentChangingCounter);
			});
		}
#endif
	}
}
