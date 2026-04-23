#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO && !HAS_UNO_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
[RunsOnUIThread]
public class Given_ItemsRepeater_FastScroll
{
	private const double OffsetTolerance = 0.5;
	private const double OverlapTolerance = 0.5;

	private const string ItemTemplateXaml = """
		<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Border Height="{Binding Height}" Background="{Binding Background}" />
		</DataTemplate>
		""";

	// Horizontal variant reuses the model's Height property as the major-axis size (Width) so
	// vertical/horizontal tests can share the same ItemModel and helpers.
	private const string ItemTemplateHorizontalXaml = """
		<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Border Width="{Binding Height}" Background="{Binding Background}" />
		</DataTemplate>
		""";

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_FastScrollWithHighVarianceItems_Then_NoOverlap()
	{
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		var offsets = new[] { 5000.0, 0.0, 12000.0, 300.0 };
		foreach (var offset in offsets)
		{
			sut.Scroller.ChangeView(null, offset, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();

			AssertNoOverlap(sut);
			AssertAllMaterializedChildrenHaveFiniteOffsets(sut);
		}
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_ScrollIncrementallyToBottomAndBack_Then_FirstItemFlushAtZero()
	{
		// Regression for a chat-style scenario: after navigating up/down through a list of items
		// with high-variance heights, the top of the list becomes cropped / inaccessible.
		// Incremental ChangeView calls preserve realization-window state across scrolls, which is
		// what drives StackLayout's average-size estimation off-course (the path that exercises
		// the Uno workaround clamp in StackLayout.GetExtent).
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		// Walk down to the bottom, then back up to the top, in small increments. Each ChangeView is
		// followed by WaitForIdle so the realization window tracks the scroll progression.
		const int Steps = 30;
		var bottomTarget = sut.Scroller.ScrollableHeight;
		for (var i = 1; i <= Steps; i++)
		{
			sut.Scroller.ChangeView(null, bottomTarget * i / Steps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}
		for (var i = Steps - 1; i >= 1; i--)
		{
			sut.Scroller.ChangeView(null, bottomTarget * i / Steps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}
		// Final explicit scroll-to-top.
		sut.Scroller.ChangeView(null, 0, null, disableAnimation: true);
		await TestServices.WindowHelper.WaitForIdle();

		sut.Scroller.VerticalOffset.Should().Be(0);

		var firstItem = FindMaterializedElementForIndex(sut, 0);
		firstItem.Should().NotBeNull("Source[0] must be materialized after scrolling back to top");

		var firstItemY = firstItem!.TransformToVisual(sut.Scroller).TransformPoint(new Point(0, 0)).Y;
		firstItemY.Should().BeApproximately(0, OffsetTolerance,
			"Source[0] must be flush at VerticalOffset=0 after an incremental scroll down and back up — content must not be cropped off the top.");
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_TallItemReRealizedAfterScrollCycle_Then_PositionIsStable()
	{
		// Regression for the ItemsRepeater rendering corruption observed during fast scrolling
		// through high-variance lists: when a much-taller-than-average item is realized, then
		// virtualized out, then re-realized after a scroll cycle, its reported position must not
		// drift. StackLayout.GetExtent estimates first-realized-item position using average size;
		// after a tall item inflates the average, re-realizing that tall item should still
		// resolve to its true cumulative offset. The Uno clamp in GetExtent
		// (Math.Max(0, firstRealizedMajor)) breaks this because it prevents the compensating
		// negative origin shift WinUI applies.
		// Uses the same 200-item high-variance layout as Test A: every 10th item is 1200px tall,
		// others are 40px. Item 3 is 1200px tall at Y=120 (covers Y=[120,1320]).
		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		await ScrollInStepsAsync(sut, 400);

		var item3Element = FindMaterializedElementForIndex(sut, 3);
		item3Element.Should().NotBeNull("Item 3 (tall, spans Y=120..1320) must be materialized at viewport offset 400");
		var item3YBefore = item3Element!.TransformToVisual(sut.Repeater).TransformPoint(new Point(0, 0)).Y;

		// Scroll far enough that item 3 becomes dematerialized (well past its end at Y=1320).
		await ScrollInStepsAsync(sut, 6000);

		// Scroll back so item 3 is in the realization cache again.
		await ScrollInStepsAsync(sut, 400);

		var item3ElementAfter = FindMaterializedElementForIndex(sut, 3);
		item3ElementAfter.Should().NotBeNull("Item 3 must be re-materialized after scrolling back to viewport offset 400");
		var item3YAfter = item3ElementAfter!.TransformToVisual(sut.Repeater).TransformPoint(new Point(0, 0)).Y;

		item3YAfter.Should().BeApproximately(item3YBefore, 1,
			"Item 3 must not drift vertically across a dematerialize/re-materialize cycle.");
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_RealizedItemGrows_Then_LayoutRemainsConsistent()
	{
		var items = Enumerable.Range(0, 10)
			.Select(i => new ItemModel(i, 60, ColorForIndex(i)))
			.ToArray();
		// Viewport 600 shows all 10 items (10 * 60 = 600), so every item is realized.
		// This isolates the layout-consistency invariant from ItemsRepeater's virtualization cache.
		var sut = CreateSut(items, new Size(300, 600));
		await LoadAsync(sut);

		EnumerateRepeaterChildren(sut.Repeater).Count().Should().BeGreaterThanOrEqualTo(10,
			"All 10 items should be realized when the viewport is tall enough to show them all");

		const double GrownHeight = 1500;
		const double OriginalHeight = 60;
		sut.Source[5].Height = GrownHeight;
		sut.Repeater.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var grownItem5 = EnumerateRepeaterChildren(sut.Repeater)
			.First(c => c.DataContext is ItemModel m && m.Id == 5);
		grownItem5.ActualHeight.Should().BeApproximately(GrownHeight, 0.5,
			"Item 5 must reflect its new height after the layout pass");

		var expectedGrowth = GrownHeight - OriginalHeight;

		AssertNoOverlap(sut);

		// Items after index 5 must have shifted down by the growth delta.
		var item6 = EnumerateRepeaterChildren(sut.Repeater)
			.First(c => c.DataContext is ItemModel m && m.Id == 6);
		((double)item6.ActualOffset.Y).Should().BeApproximately(6 * OriginalHeight + expectedGrowth, 1,
			"Item 6 must shift down by the growth delta after item 5 grows");
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_FastScrollHorizontal_WithHighVarianceItems_Then_NoOverlap()
	{
		// Horizontal orientation counterpart of When_FastScrollWithHighVarianceItems_Then_NoOverlap.
		// The StackLayout.GetExtent clamp applied to the major axis regardless of orientation, so the
		// bug symptoms are equally reproducible horizontally. This guards against orientation-specific
		// regressions in the anchor-shift pipeline.
		var sut = CreateHighVarianceHorizontalSut(itemCount: 200, viewport: new Size(600, 300));
		await LoadAsync(sut);

		var offsets = new[] { 5000.0, 0.0, 12000.0, 300.0 };
		foreach (var offset in offsets)
		{
			sut.Scroller.ChangeView(offset, null, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();

			AssertNoOverlapHorizontal(sut);
			AssertAllMaterializedChildrenHaveFiniteOffsets(sut);
		}
	}

	[TestMethod]
#if __ANDROID__ || __IOS__ || __WASM__
	[Ignore("Fails due to async native scrolling.")]
#endif
	public async Task When_ScrolledThroughList_Then_ExtentAccommodatesRealContent()
	{
		// The pre-fix symptom: the clamp caused StackLayout to report an ExtentHeight smaller than
		// the real cumulative content size, making the last items unreachable via scroll. Without
		// asserting exact convergence (which varies across platforms — native WinUI can hold an
		// over-estimated extent during rapid scrolling), this test enforces the floor invariant:
		// after walking through the full list, ExtentHeight must be at least the real content size.
		// The test items are 20 × 1200 + 180 × 40 = 31 200 px cumulative.
		const double RealContentHeight = 31200;

		var sut = CreateHighVarianceSut(itemCount: 200, viewport: new Size(300, 600));
		await LoadAsync(sut);

		// Walk through the entire list so every item is measured at least once.
		const int WalkSteps = 250;
		for (var i = 1; i <= WalkSteps; i++)
		{
			sut.Scroller.ChangeView(null, sut.Scroller.ScrollableHeight * i / WalkSteps, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}

		sut.Scroller.ExtentHeight.Should().BeGreaterThanOrEqualTo(RealContentHeight - 1,
			"ExtentHeight must accommodate the real cumulative content size after every item is measured; "
			+ "the pre-fix clamp caused an under-estimated extent that left late items unreachable.");
	}

	// ----- helpers -----

	private static SutHandle CreateHighVarianceSut(int itemCount, Size viewport)
	{
		var items = Enumerable.Range(0, itemCount)
			.Select(i => new ItemModel(i, (i % 10 == 3) ? 1200 : 40, ColorForIndex(i)))
			.ToArray();
		return CreateSut(items, viewport);
	}

	private static SutHandle CreateHighVarianceHorizontalSut(int itemCount, Size viewport)
	{
		var items = Enumerable.Range(0, itemCount)
			.Select(i => new ItemModel(i, (i % 10 == 3) ? 1200 : 40, ColorForIndex(i)))
			.ToArray();

		var source = new ObservableCollection<ItemModel>(items);
		var template = (DataTemplate)XamlReader.Load(ItemTemplateHorizontalXaml);

		ItemsRepeater repeater = new()
		{
			ItemsSource = source,
			Layout = new StackLayout { Orientation = Orientation.Horizontal },
			ItemTemplate = template,
			// Horizontal mirror of the chat-style layout anchored to the trailing edge.
			HorizontalAlignment = HorizontalAlignment.Right,
		};

		ScrollViewer scroller = new()
		{
			Width = viewport.Width,
			Height = viewport.Height,
			Content = repeater,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
		};

		return new SutHandle(scroller, repeater, source);
	}

	private static SutHandle CreateSut(IReadOnlyList<ItemModel> items, Size viewport)
	{
		var source = new ObservableCollection<ItemModel>(items);

		var template = (DataTemplate)XamlReader.Load(ItemTemplateXaml);

		ItemsRepeater repeater = new()
		{
			ItemsSource = source,
			Layout = new StackLayout { Orientation = Orientation.Vertical },
			ItemTemplate = template,
			// Matches the chat-style layout that triggers the real-world bug: when short content
			// is anchored to the bottom, the realization window evolution during scroll-up is what
			// exercises the StackLayout.GetExtent clamp path.
			VerticalAlignment = VerticalAlignment.Bottom,
		};

		ScrollViewer scroller = new()
		{
			Width = viewport.Width,
			Height = viewport.Height,
			Content = repeater,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
		};

		return new SutHandle(scroller, repeater, source);
	}

	private static async Task LoadAsync(SutHandle sut)
	{
		TestServices.WindowHelper.WindowContent = sut.Scroller;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(sut.Repeater);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static async Task ScrollInStepsAsync(SutHandle sut, double targetOffset, int steps = 6)
	{
		var current = sut.Scroller.VerticalOffset;
		for (var i = 1; i <= steps; i++)
		{
			var next = current + (targetOffset - current) * i / steps;
			sut.Scroller.ChangeView(null, next, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
			sut.Repeater.UpdateLayout();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private static IEnumerable<FrameworkElement> EnumerateRepeaterChildren(ItemsRepeater repeater)
	{
		// Enumerate via VisualTreeHelper (captures all rendered children) AND TryGetElement
		// (captures materialized-but-possibly-recycled elements). Deduplicate by reference.
		var seen = new HashSet<FrameworkElement>();
		var vtCount = VisualTreeHelper.GetChildrenCount(repeater);
		for (var i = 0; i < vtCount; i++)
		{
			if (VisualTreeHelper.GetChild(repeater, i) is FrameworkElement fe && seen.Add(fe))
			{
				yield return fe;
			}
		}
		if (repeater.ItemsSource is System.Collections.IList list)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (repeater.TryGetElement(i) is FrameworkElement fe && seen.Add(fe))
				{
					yield return fe;
				}
			}
		}
	}

	private static FrameworkElement? FindMaterializedElementForIndex(SutHandle sut, int index)
	{
		var targetId = sut.Source[index].Id;
		return EnumerateRepeaterChildren(sut.Repeater)
			.FirstOrDefault(c => c.DataContext is ItemModel m && m.Id == targetId);
	}

	private static void AssertNoOverlap(SutHandle sut)
	{
		// ItemsRepeater parks recycled/unrealized elements at large negative offsets (Y ≈ -10000)
		// as a hide trick. Filter those out before checking overlap.
		var laidOut = EnumerateRepeaterChildren(sut.Repeater)
			.Where(c => c.ActualHeight > 0 && c.ActualOffset.Y > -1000)
			.Select(c => (Top: c.ActualOffset.Y, Bottom: c.ActualOffset.Y + c.ActualHeight))
			.OrderBy(t => t.Top)
			.ToArray();

		for (var i = 1; i < laidOut.Length; i++)
		{
			var prev = laidOut[i - 1];
			var curr = laidOut[i];
			(curr.Top - prev.Bottom).Should().BeGreaterThanOrEqualTo(-OverlapTolerance,
				$"Items must not overlap, but item@{curr.Top:F2} overlaps previous item bottom@{prev.Bottom:F2}");
		}
	}

	private static void AssertNoOverlapHorizontal(SutHandle sut)
	{
		var laidOut = EnumerateRepeaterChildren(sut.Repeater)
			.Where(c => c.ActualWidth > 0 && c.ActualOffset.X > -1000)
			.Select(c => (Left: c.ActualOffset.X, Right: c.ActualOffset.X + c.ActualWidth))
			.OrderBy(t => t.Left)
			.ToArray();

		for (var i = 1; i < laidOut.Length; i++)
		{
			var prev = laidOut[i - 1];
			var curr = laidOut[i];
			(curr.Left - prev.Right).Should().BeGreaterThanOrEqualTo(-OverlapTolerance,
				$"Items must not overlap, but item@{curr.Left:F2} overlaps previous item right@{prev.Right:F2}");
		}
	}

	private static void AssertAllMaterializedChildrenHaveFiniteOffsets(SutHandle sut)
	{
		foreach (var child in EnumerateRepeaterChildren(sut.Repeater))
		{
			if (child.ActualHeight <= 0)
			{
				continue;
			}

			double.IsFinite(child.ActualOffset.Y).Should().BeTrue(
				"Materialized child must have a finite Y offset");
			double.IsNaN(child.ActualHeight).Should().BeFalse(
				"Materialized child must have a finite height");
		}
	}

	private static Color ColorForIndex(int i)
	{
		var palette = new[]
		{
			Color.FromArgb(0xFF, 0xE5, 0x39, 0x35),
			Color.FromArgb(0xFF, 0xFB, 0x8C, 0x00),
			Color.FromArgb(0xFF, 0xFD, 0xD8, 0x35),
			Color.FromArgb(0xFF, 0x43, 0xA0, 0x47),
			Color.FromArgb(0xFF, 0x1E, 0x88, 0xE5),
			Color.FromArgb(0xFF, 0x5E, 0x35, 0xB1),
			Color.FromArgb(0xFF, 0xD8, 0x1B, 0x60),
		};
		return palette[i % palette.Length];
	}

	private sealed class ItemModel : INotifyPropertyChanged
	{
		private double _height;

		public ItemModel(int id, double height, Color background)
		{
			Id = id;
			_height = height;
			Background = new SolidColorBrush(background);
		}

		public int Id { get; }

		public Brush Background { get; }

		public double Height
		{
			get => _height;
			set
			{
				if (_height != value)
				{
					_height = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	private sealed record SutHandle(ScrollViewer Scroller, ItemsRepeater Repeater, ObservableCollection<ItemModel> Source);
}
