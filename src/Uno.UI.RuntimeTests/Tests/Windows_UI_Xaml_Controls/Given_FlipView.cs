using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FlipView
	{
		[TestMethod]
#if !__IOS__ && !__ANDROID__
		[Ignore] // Test fails on Skia and WASM: https://github.com/unoplatform/uno/issues/7671
#endif
		public async Task When_Observable_ItemsSource_And_Added()
		{
			var itemsSource = new ObservableCollection<string>();
			AddItem(itemsSource);
			AddItem(itemsSource);
			AddItem(itemsSource);

			var flipView = new FlipView
			{
				ItemsSource = itemsSource
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			flipView.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);

			AddItem(itemsSource);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);
		}

		[TestMethod]
		public async Task When_Inline_Items_SelectedIndex()
		{
			var flipView = new FlipView
			{
				Items =
				{
					new FlipViewItem {Content = "Inline item 1"},
					new FlipViewItem {Content = "Inline item 2"},
				}
			};

			WindowHelper.WindowContent = flipView;

			await WindowHelper.WaitForLoaded(flipView);

			Assert.AreEqual(0, flipView.SelectedIndex);

			flipView.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, flipView.SelectedIndex);
		}

		private static void AddItem(ObservableCollection<string> items)
		{
			items.Add($"Item {items.Count + 1}");
		}
	}
}
