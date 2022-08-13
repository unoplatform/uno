using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FlipViewPages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FlipView
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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



		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Flipview_Items_Modified()
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

			await WindowHelper.WaitForResultEqual(0, () => flipView.SelectedIndex);

			flipView.SelectedItem = itemsSource[2];
			
			await WindowHelper.WaitForResultEqual(2, () => flipView.SelectedIndex);			 

			itemsSource.RemoveAt(2);

#if __ANDROID__
			await WindowHelper.WaitForResultEqual(0, () => flipView.SelectedIndex);
#else
			await WindowHelper.WaitForResultEqual(-1, () => flipView.SelectedIndex);
#endif
			itemsSource.Clear();
			  
			await WindowHelper.WaitForResultEqual(-1, () => flipView.SelectedIndex);

		}

		[TestMethod]
		public async Task When_Flipview_DataTemplateSelector()
		{
			var dataContext = new When_Flipview_DataTemplateSelector_DataContext();

			var page = new FlipView_TemplateSelectorPage();
			page.DataContext = dataContext;

			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);

			FlipView SUT = page.MyFlipView;

			for (var i = 0; i < SUT.Items.Count; i++)
			{
				var item = SUT.Items[i];
				Assert.AreEqual(SUT.SelectedIndex, i, "Has the SelectedIndex the expected value?");

				var textBlockName = Convert.ToInt32(item) % 2 == 0 ? "TextPair" : "TextOdd";

				var textblock = SUT.FindName(textBlockName) as TextBlock;
				Assert.IsNotNull(textblock, "Was the expected Template was applied?");
				Assert.AreEqual(item.ToString(), textblock?.Text, "Has the TextBlock the expected value?");

				if (i < SUT.Items.Count-1)
				{
					SUT.SelectedIndex = i + 1;
					await WindowHelper.WaitForIdle();
				}
			}
		}

		public class When_Flipview_DataTemplateSelector_DataContext
		{
			public IEnumerable<int> Items { get; set; } = Enumerable.Range(1, 6);
		}
	}
}
