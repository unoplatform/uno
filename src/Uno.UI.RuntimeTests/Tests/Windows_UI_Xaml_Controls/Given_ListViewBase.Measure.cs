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

		[TestMethod]
		public async Task When_Item_Changes_Measure_Count()
		{
			var template = (DataTemplate)_testsResources["When_Item_Changes_Measure_Count_Template"];
			const double baseWidth = 25;
			var itemsSource = Enumerable.Range(1, 20).Select(i => new When_Item_Changes_Measure_Count_ItemViewModel(i) { BadgeWidth = baseWidth + (2 * i) % 10 }).ToArray();

			var SUT = new ListView
			{
				ItemsSource = itemsSource,
				ItemTemplate = template,
				ItemContainerStyle = BasicContainerStyle,
				MaxHeight = 300
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var container = await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(3) as ListViewItem);

			var initialMeasureCount = CounterGrid.GlobalMeasureCount;
			var initialArrangeCount = CounterGrid.GlobalArrangeCount;
			var counterGrid = container.FindFirstChild<CounterGrid>();
			var badgeBorder = container.FindFirstChild<Border>(b => b.Name == "BadgeView");
			Assert.IsNotNull(counterGrid);
			Assert.IsNotNull(badgeBorder);
			var initialLocalMeasureCount = counterGrid.LocalMeasureCount;
			var initialLocalArrangeCount = counterGrid.LocalArrangeCount;

			itemsSource[3].BadgeWidth = 42;

			await WindowHelper.WaitForEqual(42, () => badgeBorder.ActualWidth);

			Assert.AreEqual(initialMeasureCount + 1, CounterGrid.GlobalMeasureCount);
			Assert.AreEqual(initialArrangeCount + 1, CounterGrid.GlobalArrangeCount);

			Assert.AreEqual(initialLocalMeasureCount + 1, counterGrid.LocalMeasureCount);
			Assert.AreEqual(initialLocalArrangeCount + 1, counterGrid.LocalArrangeCount);
		}

		[TestMethod]
		public async Task When_Available_Breadth_Changes()
		{
			var template = (DataTemplate)_testsResources["When_Available_Breadth_Changes_Template"];
			var itemsSource = Enumerable.Range(0, 15).Select(_ => WordGenerator.GetRandomWordSequence(17, new Random(6754))).ToArray();
			var SUT = new ListView
			{
				ItemTemplate = template,
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemsSource = itemsSource,
				MaxHeight = 500
			};

			var hostGrid = new Grid
			{
				Width = 196,
				Children =
				{
					SUT
				}
			};

			WindowHelper.WindowContent = hostGrid;
			await WindowHelper.WaitForLoaded(SUT);

			var container = SUT.ContainerFromIndex(2) as ListViewItem;

			var border = container.FindFirstChild<Border>(b => b.Name == "ContainerBorder");
			Assert.AreEqual(196, border.ActualWidth, delta: 1);

			hostGrid.Width = 244;
			await WindowHelper.WaitForEqual(244, () => border.ActualWidth);
		}


		// Works around ScrollIntoView() not implemented for all platforms
		private static void ScrollBy(ListViewBase listViewBase, double scrollBy)
		{
			var sv = listViewBase.FindFirstChild<ScrollViewer>();
			Assert.IsNotNull(sv);
			sv.ChangeView(null, scrollBy, null);
		}

		public class When_Item_Changes_Measure_Count_ItemViewModel : System.ComponentModel.INotifyPropertyChanged
		{

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public int Id { get; }

			public string Label => $"Item {Id}";

			private double _badgeWidth;
			public double BadgeWidth
			{
				get => _badgeWidth;
				set
				{
					if (value != _badgeWidth)
					{
						_badgeWidth = value;
						PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BadgeWidth)));
					}
				}
			}

			public When_Item_Changes_Measure_Count_ItemViewModel(int id)
			{
				Id = id;
			}
		}

		private static class WordGenerator
		{
			private static readonly string Vowels = "aeiou";
			private static readonly string Consonants = "bcdfghjklmnprstvwxyz";

			public static string GetRandomWordSequence(int count, Random random)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < count; i++)
				{
					sb.Append(GetRandomWord(random));
					sb.Append(" ");
				}
				sb.Remove(sb.Length - 1, 1);
				return sb.ToString();
			}

			private static string GetRandomWord(Random random)
			{
				const double startingvowelChance = 0.2;
				var sb = new StringBuilder();
				if (random.NextDouble() < startingvowelChance)
				{
					sb.Append(GetRandom(Vowels.ToCharArray(), random));
					sb.Append(GetRandom(Consonants.ToCharArray(), random));
					sb.Append(GetRandom(Vowels.ToCharArray(), random));
				}
				else
				{
					sb.Append(GetRandom(Consonants.ToCharArray(), random));
					sb.Append(GetRandom(Vowels.ToCharArray(), random));
					sb.Append(GetRandom(Consonants.ToCharArray(), random));
				}

				return sb.ToString();
			}

			private static T GetRandom<T>(IList<T> list, Random random)
			{
				var i = random.Next(list.Count);
				return list[i];
			}
		}
	}
}
