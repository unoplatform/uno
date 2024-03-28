using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Extensions;
using System.Collections.ObjectModel;
using Windows.Foundation;
#if WINAPPSDK
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
		// Due to physical/logical pixel conversion on Android, measurements aren't exact
		private double Epsilon =>
#if __ANDROID__
			0.5
#else
			0
#endif
			;

		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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
			ScrollTo(SUT, scrollBy);
			var item = 10;
			await WindowHelper.WaitFor(() => (lvi = SUT.ContainerFromItem(item) as ListViewItem) != null);
			Assert.AreEqual(minWidth, lvi.ActualWidth);


			Assert.AreEqual(minWidth, SUT.ActualWidth);
		}

		[TestMethod]
#if __IOS__ || __ANDROID__
		[Ignore("ListView only supports HorizontalAlignment.Stretch - https://github.com/unoplatform/uno/issues/1133")]
#endif
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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
		[RunsOnUIThread]
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

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Margins_And_Scrolled()
		{
			var SUT = new ListView
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 250,
				Height = 200,
				ItemsSource = Enumerable.Range(0, 11).ToArray(),
				ItemContainerStyle = ContainerMarginStyle,
				ItemTemplate = FixedSizeItemTemplate,
				ItemsPanel = NoCacheItemsStackPanel
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);


			await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(0));
			VerifyItemHeight(0);

			await ScrollDownAndBackChecked();
			await ScrollDownAndBackChecked();
			await ScrollDownAndBackChecked();
			await ScrollDownAndBackChecked();

			async Task ScrollDownAndBackChecked()
			{
				await ScrollToInIncrements(SUT, 1000);
				await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(10));
				VerifyItemHeight(9);
				VerifyItemHeight(8);

				await ScrollToInIncrements(SUT, -100);
				await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(1));
				await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(0));
				VerifyItemHeight(0);
				VerifyItemHeight(1);
				VerifyItemHeight(2);
			}

			void VerifyItemHeight(int position)
			{
				var container = SUT.ContainerFromIndex(position) as ListViewItem;
				Assert.IsNotNull(container);
				const double ExpectedContainerHeight = 29;
				const double TopAndBottomMargin = 15;
				Assert.AreEqual(ExpectedContainerHeight, container.ActualHeight, 1);

				var containerNext = SUT.ContainerFromIndex(position + 1) as ListViewItem;
				Assert.IsNotNull(containerNext);

				var containerRect = container.GetOnScreenBounds();
				var containerNextRect = containerNext.GetOnScreenBounds();
				Assert.AreEqual(ExpectedContainerHeight + TopAndBottomMargin, containerNextRect.Y - containerRect.Y, Epsilon);
			}
		}

#if __IOS__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Recycled_DuringScroll()
		{
			var lines = new[]
			{
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
				"Donec tristique metus vel aliquet malesuada.",
				"Quisque efficitur diam pulvinar sapien luctus cursus.",
				"Fusce in tortor vitae risus pretium malesuada quis vitae lacus.",
				"Quisque vitae viverra nunc, ut placerat libero.",
				"Etiam metus ligula, facilisis et odio vitae, placerat ornare nunc.",
				"Nulla facilisi. Cras id nisi elit.",
				"Mauris pharetra quam lacinia purus interdum, vitae suscipit lorem lobortis.",
			};
			var source = Enumerable.Range(0, 100)
				.Select(x => string.Join(" ", lines.Take(2 * (2 + x % 3)).Prepend($"#{x}:")))
				.ToArray();

			var SUT = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = WrappingTextBlockItemTemplate,
				ItemsSource = source,
				Height = 500,
				Width = 350,
			};

			SUT.ContainerContentChanging += (s, e) =>
			{
				Console.WriteLine($"@xy ContainerContentChanging: #{e.ItemIndex}, InRecycleQueue={e.InRecycleQueue}");
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForNonNull(() => SUT.FindFirstChild<ItemsPresenter>());
			await WindowHelper.WaitForIdle();

			var sv = SUT.FindFirstChild<ScrollViewer>();
			var itemHeights = Enumerable.Range(0, 3)
				.Select(x => (ListViewItem)SUT.ContainerFromIndex(x))
				.Select(x => x.LayoutSlot.Height)
				.ToArray();

			Assert.IsTrue(itemHeights[0] < itemHeights[1] && itemHeights[1] < itemHeights[2], "Set of item heights should be in ascending order: " + string.Join(" < ", itemHeights));

			var snapshots = new List<(int Index, Rect? ClippedFrame)>();
			(int Index, ListViewItem Container)? previousContainer = null;
			SUT.ContainerContentChanging += (s, e) =>
			{
				// The clean up occurs after this event, so we have to check the previous one when the next is being prepared.
				if (previousContainer is { } previous)
				{
					// We should not throw/assert here, as this event is coming straight from native without any exception-guard.
					// Throwing in this context will cause the app to crash directly.
					snapshots.Add((previous.Index, previous.Container.ClippedFrame));
				}

				previousContainer = (e.ItemIndex, (ListViewItem)e.ItemContainer);
			};

			await ScrollToAndWait(SUT, sv.ExtentHeight);
			await ScrollToAndWait(SUT, 0);

			string FormatRect(Rect? rect) => rect is { } x ? $"[{x.Width:0.#}x{x.Height:0.#}@{x.Left:0.#},{x.Top:0.#}]" : "null";

			foreach (var (index, frame) in snapshots)
			{
				var variantIndex = index % 3;
				var expectedHeight = itemHeights[variantIndex];

				Assert.IsTrue(frame is not { } cf || cf.Height == expectedHeight, $"Item's ClippedFrame shouldve been cleared, or be of expected height: Row={index} ('{variantIndex}), ClippedFrame={FormatRect(frame)}, ExpectedHeight={expectedHeight}");
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if __WASM__
		[Ignore("Test is flaky")]
#endif
		public async Task When_ItemsPresenter_MinHeight()
		{
			var SUT = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate,
				ItemsSource = Enumerable.Range(0, 3).ToArray(),
				Height = 250
			};

			WindowHelper.WindowContent = SUT;
			var itemsPresenter = await WindowHelper.WaitForNonNull(() => SUT.FindFirstChild<ItemsPresenter>());
			itemsPresenter.MinHeight = 310;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(250, SUT.ActualHeight, 1);

			var container = SUT.ContainerFromIndex(2) as ListViewItem;
			Assert.IsNotNull(container);
			var initialRect = container.GetRelativeBounds(SUT);
			const double HeightOfTwoItems = 29 * 2;
			Assert.AreEqual(HeightOfTwoItems, initialRect.Y, 1, "HeightOfTwoItems");

			await Task.Delay(2000);
			var sv = SUT.FindFirstChild<ScrollViewer>();
			ScrollTo(SUT, 40);
			double InitialScroll()
			{
#if WINAPPSDK
				// For some reason on UWP the initial ChangeView may not work
				ScrollTo(SUT, 40);
#endif
				return sv.VerticalOffset;
			}

			await WindowHelper.WaitForEqual(40, InitialScroll);

			var rectScrolledPartial = container.GetRelativeBounds(SUT);
			Assert.AreEqual(HeightOfTwoItems - 40, rectScrolledPartial.Y, 1, "HeightOfTwoItems - 40");

			await ScrollToInIncrements(SUT, 200);
			const double MaxPossibleScroll = 310 - 250;
			await WindowHelper.WaitForEqual(MaxPossibleScroll, () => sv.VerticalOffset);

			var rectScrollFinal = container.GetRelativeBounds(SUT);
			Assert.AreEqual(HeightOfTwoItems - MaxPossibleScroll, rectScrollFinal.Y, 1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_INCC_StartEmpty_AddOne()
		{
			var source = new ObservableCollection<string>();
			var SUT = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate, // height = 29
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;
			// cant use WaitForLoaded here, as the ListView is empty
			await WindowHelper.WaitForNonNull(() => SUT.FindFirstChild<ItemsPresenter>());
			await WindowHelper.WaitForIdle();

			source.Add("Apple");
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => Math.Abs(SUT.ActualHeight - 29) <= Epsilon, message: $"ListView failed to grow from adding item: (ActualHeight: {SUT.ActualHeight})");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ItemsSource_INCC_StartTwo_RemoveOne()
		{
			var source = new ObservableCollection<string>(new[] { "Apple", "Banana" });
			var SUT = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate, // height = 29
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			source.RemoveAt(source.Count - 1);
			await WindowHelper.WaitFor(() => Math.Abs(SUT.ActualHeight - 29) <= Epsilon, message: $"ListView failed to shrink from removing item: (ActualHeight: {SUT.ActualHeight})");
		}

		// Works around ScrollIntoView() not implemented for all platforms
		private static void ScrollTo(ListViewBase listViewBase, double vOffset)
		{
			var sv = listViewBase.FindFirstChild<ScrollViewer>();
			Assert.IsNotNull(sv);
			sv.ChangeView(null, vOffset, null);
		}

		/// <summary>
		/// Scroll and wait until at least 1s has elapsed since ScrollViewer.ViewChange or timed out after a given time.
		/// </summary>
		/// <param name="listViewBase"></param>
		/// <param name="vOffset"></param>
		/// <param name="timeoutInMs"></param>
		/// <returns></returns>
		private static async Task ScrollToAndWait(ListViewBase listViewBase, double vOffset, int timeoutInMs = 10000)
		{
			var sv = listViewBase.FindFirstChild<ScrollViewer>();

			var lastScrolled = DateTime.Now;
			sv.ViewChanged += (s, e) => lastScrolled = DateTime.Now;

			sv.ChangeView(null, vOffset, null);

			var timeout = DateTime.Now.AddMilliseconds(timeoutInMs);
			while (DateTime.Now < timeout)
			{
				await WindowHelper.WaitForIdle();
				if (lastScrolled.AddSeconds(1) < DateTime.Now)
				{
					return;
				}
			}
		}

		private static async Task ScrollToInIncrements(ListViewBase listViewBase, double vOffset)
		{
			var sv = listViewBase.FindFirstChild<ScrollViewer>();
			Assert.IsNotNull(sv);
			var current = sv.VerticalOffset;
			if (current == vOffset)
			{
				return;
			}
			var increment = sv.ActualHeight / 2;
			if (increment == 0)
			{
				Assert.Fail("ScrollViewer must have non-zero height");
			}
			if (vOffset > current)
			{
				for (double d = current + increment; d < vOffset; d += increment)
				{
					sv.ChangeView(null, d, null);
					await Task.Delay(10);
				}
			}
			else
			{
				for (double d = current - increment; d > vOffset; d -= increment)
				{
					sv.ChangeView(null, d, null);
					await Task.Delay(10);
				}
			}

			sv.ChangeView(null, vOffset, null);
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
