using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_ListViewBase
	{
		[TestMethod]
#if __IOS__ || __ANDROID__
		[Ignore("ListView only supports HorizontalAlignment.Stretch - https://github.com/unoplatform/uno/issues/1133")]
#endif
		public async Task When_ListView_Parent_Unstretched()
		{
			var source = Enumerable.Range(0, 5).ToArray();
			var SUT = new ListView
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				ItemsSource = source
			};

			const int minWidth = 193;
			var border = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				MinWidth = minWidth,
				Child = SUT
			};

			WindowHelper.WindowContent = border;

			await WindowHelper.WaitForIdle();

			ListViewItem lvi = null;
			foreach (var item in source)
			{
				await WindowHelper.WaitFor(() => (lvi = SUT.ContainerFromItem(item) as ListViewItem) != null);
				Assert.AreEqual(minWidth, lvi.ActualWidth);
			}

			Assert.AreEqual(minWidth, SUT.ActualWidth);
		}

		[TestMethod]
#if __IOS__ || __ANDROID__
		[Ignore("ListView only supports HorizontalAlignment.Stretch - https://github.com/unoplatform/uno/issues/1133")]
#endif
		public async Task When_ListView_Parent_Unstretched_Scrolled()
		{
			var source = Enumerable.Range(0, 50).ToArray();
			var SUT = new ListView
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height = 200,
				ItemsPanel = NoCacheItemsStackPanel,
				ItemsSource = source
			};

			const int minWidth = 193;
			var border = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				MinWidth = minWidth,
				Child = SUT
			};

			WindowHelper.WindowContent = border;

			await WindowHelper.WaitForLoaded(SUT);

			ListViewItem lvi = null;

			const double scrollBy = 300;
			ScrollBy(SUT, scrollBy);
			var item = 10;
			await WindowHelper.WaitFor(() => (lvi = SUT.ContainerFromItem(item) as ListViewItem) != null);
			Assert.AreEqual(minWidth, lvi.ActualWidth);


			Assert.AreEqual(minWidth, SUT.ActualWidth);
		}

		[TestMethod]
#if __IOS__ || __ANDROID__
		[Ignore("ListView only supports HorizontalAlignment.Stretch - https://github.com/unoplatform/uno/issues/1133")]
#endif
		public async Task When_Item_Margins()
		{
			var SUT = new ListView
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				ItemsSource = Enumerable.Range(0, 3).ToArray(),
				ItemContainerStyle = ContainerMarginStyle,
				ItemTemplate = FixedSizeItemTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			Assert.AreEqual(188, SUT.ActualWidth);
			Assert.AreEqual(132, SUT.ActualHeight);
		}

		// Works around ScrollIntoView() not implemented for all platforms
		private static void ScrollBy(ListViewBase listViewBase, double scrollBy)
		{
			var sv = listViewBase.FindFirstChild<ScrollViewer>();
			Assert.IsNotNull(sv);
			sv.ChangeView(null, scrollBy, null);
		}
	}
}
