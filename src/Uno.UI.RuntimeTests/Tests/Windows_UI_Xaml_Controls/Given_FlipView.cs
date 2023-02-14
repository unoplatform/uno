using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FlipViewPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Flipview_DataTemplateSelector()
		{
			var dataContext = new When_Flipview_DataTemplateSelector_DataContext();

			var page = new FlipView_TemplateSelectorPage();
			page.DataContext = dataContext;

			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			FlipView SUT = page.MyFlipView;

			static string GetTextBlockName(object item)
			{
				return Convert.ToInt32(item) % 2 == 0 ? "TextEven" : "TextOdd";
			}

			static void AssertTextBlock(TextBlock textblock, object item)
			{
				Assert.IsNotNull(textblock, "The Template applied wasn't the expected");
				Assert.AreEqual(item.ToString(), textblock?.Text, "The TextBlock doesn't have the expected value");
			}


#if __WASM__ || __SKIA__
			var flipViewItems = (SUT as FrameworkElement)?.FindChildren<FlipViewItem>()?.ToArray() ?? new FlipViewItem[0];

			for (var i = 0; i < SUT.Items.Count; i++)
			{
				if (SUT.SelectedIndex != i)
				{
					SUT.SelectedIndex = i;
					await WindowHelper.WaitForIdle();
				}

				var item = SUT.Items[i];
				Assert.AreEqual(SUT.SelectedIndex, i, "SelectedIndex isn't the expected value");

				var textBlockName = GetTextBlockName(item);

				var textblock = flipViewItems[i].FindName(textBlockName) as TextBlock;
				AssertTextBlock(textblock, item);
			}
#else
			for (var i = 0; i < SUT.Items.Count; i++)
			{
				if (SUT.SelectedIndex != i)
				{
					SUT.SelectedIndex = i;
					await WindowHelper.WaitForIdle();
				}

				var item = SUT.Items[i];
				Assert.AreEqual(SUT.SelectedIndex, i, "SelectedIndex isn't the expected value");

				var textBlockName = GetTextBlockName(item);

				var textblock = SUT.FindName(textBlockName) as TextBlock;
				AssertTextBlock(textblock, item);
			}
#endif
		}

	}

#if __SKIA__ || __WASM__
	static class Extensions
	{
		internal static IEnumerable<T> FindChildren<T>(this FrameworkElement root) where T : FrameworkElement
		{
			return root.GetDescendants().OfType<T>().ToArray();
		}

		private static IEnumerable<FrameworkElement> GetDescendants(this FrameworkElement root)
		{
			foreach (var child in root._children)
			{
				yield return child as FrameworkElement;

				foreach (var descendant in (child as FrameworkElement).GetDescendants())
				{
					yield return descendant;
				}
			}
		}
	}
#endif

	public class When_Flipview_DataTemplateSelector_DataContext
	{
		public IEnumerable<int> Items { get; set; } = Enumerable.Range(1, 6);
	}
}
