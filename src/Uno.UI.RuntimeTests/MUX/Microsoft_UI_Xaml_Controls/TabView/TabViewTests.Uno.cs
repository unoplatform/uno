using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	public partial class TabViewTests
	{
#if HAS_UNO && !__IOS__
		[TestMethod]
		public void VerifyItemsAreCreatedOnlyOnce()
		{
			TabView tabView = null;
			RunOnUIThread.Execute(() =>
			{
				tabView = new TabView();
				TestServices.WindowHelper.WindowContent = tabView;

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

				TestServices.WindowHelper.WaitForIdle();

				// Only one container should be generated for the first item.
				Assert.AreEqual(1, containerContentChangingCounter);
			});
		}
#endif
	}
}
