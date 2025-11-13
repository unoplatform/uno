using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Media;

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

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20976")]
		public async Task When_First_Tab_Selected_And_Closed() => await When_Tab_Selected_And_Closed(0, 1);

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20976")]
		public async Task When_Middle_Tab_Selected_And_Closed() => await When_Tab_Selected_And_Closed(1, 2);

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20976")]
		public async Task When_Last_Tab_Selected_And_Closed() => await When_Tab_Selected_And_Closed(2, 1);

		private async Task When_Tab_Selected_And_Closed(int tabToClose, int expectedSelectedTab)
		{
			var tabView = new TabView();
			tabView.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			tabView.VerticalContentAlignment = VerticalAlignment.Stretch;
			tabView.HorizontalAlignment = HorizontalAlignment.Stretch;
			tabView.VerticalAlignment = VerticalAlignment.Stretch;
			tabView.Height = 300;

			var tabItem1 = CreateTabViewItem("Item 1");
			tabItem1.Content = new Border() { Background = new SolidColorBrush(Colors.Blue) };
			var tabItem2 = CreateTabViewItem("Item 2");
			tabItem2.Content = new Border() { Background = new SolidColorBrush(Colors.Green) };
			var tabItem3 = CreateTabViewItem("Item 3");
			tabItem3.Content = new Border() { Background = new SolidColorBrush(Colors.Red) };

			tabView.TabItems.Add(tabItem1);
			tabView.TabItems.Add(tabItem2);
			tabView.TabItems.Add(tabItem3);

			var tabToSelectAndClose = tabView.TabItems[tabToClose];
			var expectedSelectedTabAfterClose = (TabViewItem)tabView.TabItems[expectedSelectedTab];
			var expectedColor = ((SolidColorBrush)((Border)expectedSelectedTabAfterClose.Content).Background).Color;

			TestServices.WindowHelper.WindowContent = tabView;
			await TestServices.WindowHelper.WaitForLoaded(tabView);

			// Select the tab that will be closed
			tabView.SelectedItem = tabToSelectAndClose;
			await TestServices.WindowHelper.WaitForIdle();

			// Close the tab
			tabView.TabItems.Remove(tabToSelectAndClose);
			await TestServices.WindowHelper.WaitForIdle();

			// Verify that the last tab is closed and the second last tab is selected
			Assert.IsFalse(tabView.TabItems.Contains(tabToSelectAndClose), "Expected the tab to be removed from the TabItems collection.");
			Assert.AreEqual(expectedSelectedTabAfterClose, tabView.SelectedItem, "Expected different tab to be selected after closing the selected tab.");
			Assert.IsTrue(expectedSelectedTabAfterClose.IsSelected, "Expected tab is not selected.");

#if HAS_UNO
			var presenter = tabView.FindFirstDescendant<ContentPresenter>("TabContentPresenter");
			Assert.AreEqual(presenter.Content, expectedSelectedTabAfterClose.Content);
#endif
		}
	}
}
