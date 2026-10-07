#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DevTools.Input;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public partial class Given_TableView
{
	private const double LayoutTolerance = 1.0;

	[TestMethod]
	public async Task When_Pixel_Width()
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		name.Width = new GridLength(150, GridUnitType.Pixel);
		await LoadAsync(table);

		Assert.AreEqual(150.0, name.ActualWidth, LayoutTolerance);

		// Clamped to MinWidth.
		name.MinWidth = 200;
		await WindowHelper.WaitForEqual(200.0, () => name.ActualWidth, LayoutTolerance);

		// Clamped to MaxWidth.
		name.MinWidth = 20;
		name.MaxWidth = 100;
		await WindowHelper.WaitForEqual(100.0, () => name.ActualWidth, LayoutTolerance);

		// The realized cells and the header cell are arranged at the resolved width.
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(100.0, GetHeaderCell(table, name).ActualWidth, LayoutTolerance);
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(100.0, GetCell(row, name).ActualWidth, LayoutTolerance);
		}

		// hi = max(lo, MaxWidth): MinWidth wins over a smaller MaxWidth.
		name.MinWidth = 200;
		await WindowHelper.WaitForEqual(200.0, () => name.ActualWidth, LayoutTolerance);
	}

	[TestMethod]
	public async Task When_Auto_Width_Tracks_Realized_Content()
	{
		// TableView_Layout.cpp: "Auto is shrink-capable: each measure pass re-derives the width from
		// the currently pulled measured max (header + realized rows)". The column follows the widest
		// realized cell in both directions (CGrid parity), it does not latch a grow-only maximum.
		var items = new ObservableCollection<Person>(People(4));
		var table = CreateTable(items);
		var name = table.Columns[0];
		name.Width = new GridLength(1, GridUnitType.Auto);
		await LoadAsync(table, width: 800);

		var initial = name.ActualWidth;
		Assert.IsTrue(initial > 0);

		var wide = new Person("Maximilian Alexander Fairbanks-Whittington", 40, "Oslo");
		items.Add(wide);
		await WindowHelper.WaitFor(() => name.ActualWidth > initial + 50, message: "Auto column did not grow to the wide cell");
		var grown = name.ActualWidth;

		// Cells are arranged at the resolved width.
		await WindowHelper.WaitForIdle();
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(grown, GetCell(row, name).ActualWidth, LayoutTolerance);
		}

		items.Remove(wide);
		await WindowHelper.WaitFor(() => name.ActualWidth < grown - 50, message: "Auto column did not follow the widest realized cell back down");
		Assert.AreEqual(initial, name.ActualWidth, LayoutTolerance);

		// A new data set re-resolves from scratch.
		table.ItemsSource = new ObservableCollection<Person> { wide };
		await WindowHelper.WaitFor(() => name.ActualWidth > initial + 50, message: "Auto column did not resolve against the new data set");
	}

	[TestMethod]
	public async Task When_Star_Width()
	{
		var table = new TableView();
		var fixedColumn = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		var oneStar = TextColumn(nameof(Person.Age), new GridLength(1, GridUnitType.Star));
		var twoStar = TextColumn(nameof(Person.City), new GridLength(2, GridUnitType.Star));
		table.Columns.Add(fixedColumn);
		table.Columns.Add(oneStar);
		table.Columns.Add(twoStar);
		table.ItemsSource = People(3);

		await LoadAsync(table, width: 500);

		var viewport = GetBodyScroller(table).ViewportWidth;
		Assert.IsTrue(viewport > 100);
		var leftover = viewport - 100;

		await WindowHelper.WaitForEqual(leftover / 3, () => oneStar.ActualWidth, LayoutTolerance);
		Assert.AreEqual(leftover * 2 / 3, twoStar.ActualWidth, LayoutTolerance);
		Assert.AreEqual(100.0, fixedColumn.ActualWidth, LayoutTolerance);

		// Fixed columns overflow the viewport: nothing is left to divide, so the Star columns clamp
		// to their MinWidth.
		fixedColumn.Width = new GridLength(2000, GridUnitType.Pixel);
		await WindowHelper.WaitForEqual(oneStar.MinWidth, () => oneStar.ActualWidth, LayoutTolerance);
		Assert.AreEqual(twoStar.MinWidth, twoStar.ActualWidth, LayoutTolerance);
	}

	[TestMethod]
	public async Task When_Auto_Width_Without_Content()
	{
		// No header (hidden) and no realized rows: the measured max is 0, so Auto falls back to
		// c_widthDefault (120), still clamped by MinWidth/MaxWidth.
		var table = CreateTable(new List<Person>());
		table.HeadersVisibility = TableViewHeadersVisibility.None;
		var name = table.Columns[0];
		name.Width = new GridLength(1, GridUnitType.Auto);
		await LoadAsync(table);

		await WindowHelper.WaitForEqual(120.0, () => name.ActualWidth, LayoutTolerance);

		name.MaxWidth = 90;
		await WindowHelper.WaitForEqual(90.0, () => name.ActualWidth, LayoutTolerance);

		name.MaxWidth = double.PositiveInfinity;
		name.MinWidth = 150;
		await WindowHelper.WaitForEqual(150.0, () => name.ActualWidth, LayoutTolerance);
	}

	[TestMethod]
	public async Task When_Star_Width_Zero_Factor()
	{
		// MinWidthForStarFactor: a 0* column gets a zero share and a zero lower bound, unless MinWidth
		// is set locally (the default MinWidth of 20 does not apply).
		var table = new TableView();
		var fixedColumn = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		var zeroStar = TextColumn(nameof(Person.Age), new GridLength(0, GridUnitType.Star));
		var oneStar = TextColumn(nameof(Person.City), new GridLength(1, GridUnitType.Star));
		table.Columns.Add(fixedColumn);
		table.Columns.Add(zeroStar);
		table.Columns.Add(oneStar);
		table.ItemsSource = People(3);

		await LoadAsync(table, width: 500);

		var viewport = GetBodyScroller(table).ViewportWidth;
		await WindowHelper.WaitForEqual(0.0, () => zeroStar.ActualWidth, LayoutTolerance);
		Assert.AreEqual(viewport - 100, oneStar.ActualWidth, LayoutTolerance);

		zeroStar.MinWidth = 30;
		await WindowHelper.WaitForEqual(30.0, () => zeroStar.ActualWidth, LayoutTolerance);
		Assert.AreEqual(viewport - 130, oneStar.ActualWidth, LayoutTolerance, "the 1* column re-divides what the clamped 0* column left");
	}

	[TestMethod]
	public async Task When_Star_Width_Clamped_Column_Redistributes()
	{
		// The WPF ComputeStarColumnWidths loop: a Star column that hits MaxWidth is fixed there and the
		// remaining Star columns re-divide what is left, instead of each taking its plain proportional share.
		var table = new TableView();
		var fixedColumn = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		var capped = TextColumn(nameof(Person.Age), new GridLength(1, GridUnitType.Star));
		capped.MaxWidth = 50;
		var free = TextColumn(nameof(Person.City), new GridLength(1, GridUnitType.Star));
		table.Columns.Add(fixedColumn);
		table.Columns.Add(capped);
		table.Columns.Add(free);
		table.ItemsSource = People(3);

		await LoadAsync(table, width: 500);

		var viewport = GetBodyScroller(table).ViewportWidth;
		Assert.IsTrue((viewport - 100) / 2 > 50, "the plain proportional share must exceed the cap");

		await WindowHelper.WaitForEqual(50.0, () => capped.ActualWidth, LayoutTolerance);
		Assert.AreEqual(viewport - 100 - 50, free.ActualWidth, LayoutTolerance);

		// Same for MinWidth: a Star column whose share is below its MinWidth is fixed at MinWidth.
		capped.MaxWidth = double.PositiveInfinity;
		// TableViewColumn::UpdateActualWidth clamps capped to MinWidth synchronously, so wait on the
		// column that only the next ResolveColumnWidths pass moves.
		capped.MinWidth = viewport - 100 - 40;
		await WindowHelper.WaitForEqual(40.0, () => free.ActualWidth, LayoutTolerance);
		Assert.AreEqual(viewport - 100 - 40, capped.ActualWidth, LayoutTolerance);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // UIElement.Translation
	public async Task When_Frozen_Leading_Prefix()
	{
		var table = new TableView();
		var frozen = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		frozen.FrozenEdge = TableViewFrozenEdge.Leading;
		var scrolling = TextColumn(nameof(Person.Age), new GridLength(300, GridUnitType.Pixel));
		// Leading, but not part of the contiguous prefix: it scrolls like any other column.
		var nonContiguous = TextColumn(nameof(Person.City), new GridLength(300, GridUnitType.Pixel));
		nonContiguous.FrozenEdge = TableViewFrozenEdge.Leading;
		table.Columns.Add(frozen);
		table.Columns.Add(scrolling);
		table.Columns.Add(nonContiguous);
		table.ItemsSource = People(5);

		await LoadAsync(table, width: 300);

		var scroller = GetBodyScroller(table);
		Assert.IsTrue(scroller.ScrollableWidth > 150);

		scroller.ChangeView(150, null, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(scroller.HorizontalOffset - 150) < 0.5);
		await WindowHelper.WaitForIdle();

		var offset = scroller.HorizontalOffset;
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(offset, GetCell(row, frozen).Translation.X, LayoutTolerance, "frozen cell is pinned");
			Assert.AreEqual(0.0, GetCell(row, scrolling).Translation.X, LayoutTolerance, "scrolling cell");
			Assert.AreEqual(0.0, GetCell(row, nonContiguous).Translation.X, LayoutTolerance, "non-contiguous Leading cell scrolls");

			// ApplyFrozenColumnLayout: the pinned cell paints above the scrolled ones, and each scrolled
			// cell is clipped where it slides under the band (left = frozenWidth + offset - panelX).
			Assert.AreEqual(1, Canvas.GetZIndex(GetCell(row, frozen)));
			Assert.IsNull(GetCell(row, frozen).Clip);

			Assert.AreEqual(0, Canvas.GetZIndex(GetCell(row, scrolling)));
			var clip = GetCell(row, scrolling).Clip as RectangleGeometry;
			Assert.IsNotNull(clip, "the scrolled cell under the band is clipped");
			Assert.AreEqual(100 + offset - 100, clip!.Rect.X, LayoutTolerance);
			Assert.AreEqual(300 - (100 + offset - 100), clip.Rect.Width, LayoutTolerance);

			// panelX = 400: nothing of it is under the band yet (clipLeft <= 0).
			Assert.AreEqual(0, Canvas.GetZIndex(GetCell(row, nonContiguous)));
			Assert.IsNull(GetCell(row, nonContiguous).Clip);
		}

		// Header and rows share the pinning so they stay aligned.
		Assert.AreEqual(offset, GetHeaderCell(table, frozen).Translation.X, LayoutTolerance);
		Assert.AreEqual(0.0, GetHeaderCell(table, nonContiguous).Translation.X, LayoutTolerance);

		// Unfreezing clears the pin on the next refresh.
		frozen.FrozenEdge = TableViewFrozenEdge.None;
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(0.0, GetCell(row, frozen).Translation.X, LayoutTolerance);
			foreach (var cell in GetCells(row))
			{
				Assert.AreEqual(0, Canvas.GetZIndex(cell));
				Assert.IsNull(cell.Clip, "with no frozen band nothing is clipped");
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // UIElement.Translation
	public async Task When_Frozen_Leading_Prefix_RightToLeft()
	{
		// ApplyFrozenColumnLayout pins in logical LTR coordinates; the RTL mirror at the FlowDirection
		// boundary flips the counter-translation and clip, so a Leading prefix pins on the visual right.
		var table = new TableView { FlowDirection = FlowDirection.RightToLeft };
		var frozen = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		frozen.FrozenEdge = TableViewFrozenEdge.Leading;
		var scrolling = TextColumn(nameof(Person.Age), new GridLength(300, GridUnitType.Pixel));
		table.Columns.Add(frozen);
		table.Columns.Add(scrolling);
		table.Columns.Add(TextColumn(nameof(Person.City), new GridLength(300, GridUnitType.Pixel)));
		table.ItemsSource = People(5);

		await LoadAsync(table, width: 300);

		var scroller = GetBodyScroller(table);
		scroller.ChangeView(150, null, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(scroller.HorizontalOffset - 150) < 0.5);
		await WindowHelper.WaitForIdle();

		var offset = scroller.HorizontalOffset;
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(offset, GetCell(row, frozen).Translation.X, LayoutTolerance, "frozen cell is pinned");
			Assert.AreEqual(1, Canvas.GetZIndex(GetCell(row, frozen)));
			Assert.IsNull(GetCell(row, frozen).Clip);

			Assert.AreEqual(0.0, GetCell(row, scrolling).Translation.X, LayoutTolerance, "scrolling cell");
			var clip = GetCell(row, scrolling).Clip as RectangleGeometry;
			Assert.IsNotNull(clip, "the scrolled cell under the band is clipped");
			Assert.AreEqual(offset, clip!.Rect.X, LayoutTolerance);
			Assert.AreEqual(300 - offset, clip.Rect.Width, LayoutTolerance);
		}

		var frozenHeader = GetHeaderCell(table, frozen);
		Assert.AreEqual(offset, frozenHeader.Translation.X, LayoutTolerance);

		// Visually the band sits on the table's right edge.
		var tableBounds = table.TransformToVisual(null).TransformBounds(new Rect(0, 0, table.ActualWidth, table.ActualHeight));
		var frozenBounds = GetCell(GetRealizedRows(table)[0], frozen).TransformToVisual(null).TransformBounds(new Rect(0, 0, 100, 1));
		Assert.AreEqual(tableBounds.Right, frozenBounds.Right, 2.0, "the Leading band pins to the visual right under RTL");
	}

	[TestMethod]
	public async Task When_Resize_Gripper_Requires_Both_Flags()
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		await LoadAsync(table);

		Assert.IsTrue(GetHeaderCells(table).All(c => FindGripper(c) is not null));
		Assert.IsTrue(GetHeaderCell(table, name).IsTabStop, "a resizable header is a tab stop");

		name.CanResize = false;
		await WindowHelper.WaitForIdle();

		Assert.IsNull(FindGripper(GetHeaderCell(table, name)));
		Assert.IsTrue(GetHeaderCell(table, name).IsTabStop, "every visible header is a tab stop, resizable or not");
		Assert.IsNotNull(FindGripper(GetHeaderCell(table, table.Columns[1])));

		table.CanUserResizeColumns = false;
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(GetHeaderCells(table).All(c => FindGripper(c) is null));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Resize_Drag()
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		name.MaxWidth = 160;
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

#if __SKIA__
		using var listener = RecordingAutomationListener.Install();
#endif

		// The drag is clamped to MaxWidth.
		var nameHeaderCell = GetHeaderCell(table, name);
		var start = Center(FindGripper(nameHeaderCell)!);
		mouse.Press(start);
		mouse.MoveTo(new Point(start.X + 80, start.Y), 8);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(GridUnitType.Pixel, name.Width.GridUnitType);
		Assert.AreEqual(160.0, name.Width.Value, LayoutTolerance);
		await WindowHelper.WaitForEqual(160.0, () => name.ActualWidth, LayoutTolerance);

#if __SKIA__
		// DragCompleted announces a pointer resize too ("a pointer resize was otherwise completely silent").
		var announcements = listener.TakeNotifications("TableViewColumnWidthChangedActivityId");
		Assert.AreEqual(1, announcements.Count, "one announcement per completed drag");
		Assert.AreSame(FrameworkElementAutomationPeer.FromElement(nameHeaderCell), announcements[0].Peer);
		Assert.AreEqual(
			StringUtil.FormatString(ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TableViewColumnWidthChanged), "Name", "160"),
			announcements[0].DisplayString);
#endif

		// A press without movement writes nothing.
		var star = new GridLength(1, GridUnitType.Star);
		var city = table.Columns[2];
		city.Width = star;
		await WindowHelper.WaitForIdle();

		var cityGripper = Center(FindGripper(GetHeaderCell(table, city))!);
		mouse.Press(cityGripper);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(star, city.Width, "a click on the gripper must not pin a Star column");
#if __SKIA__
		Assert.AreEqual(0, listener.TakeNotifications("TableViewColumnWidthChangedActivityId").Count, "a release without movement announces nothing");
#endif
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Resize_Drag_Escape_Restores_Authored_Width()
	{
		// A Star column only resizes against a later Star column (ResizeBoundsForColumn pins the
		// last one), so drag Age and let City yield.
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		var age = table.Columns[1];
		var city = table.Columns[2];
		var star = new GridLength(1, GridUnitType.Star);
		age.Width = star;
		city.Width = star;
		await LoadAsync(table);

		var nameWidth = name.ReadLocalValue(TableViewColumn.WidthProperty);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var start = Center(FindGripper(GetHeaderCell(table, age))!);
		mouse.Press(start);
		try
		{
			mouse.MoveTo(new Point(start.X - 40, start.Y), 8);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(GridUnitType.Pixel, age.Width.GridUnitType, "the drag writes a pixel width");

			await KeyboardHelper.PressKeySequence("$d$_esc#$u$_esc", table);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(star, age.Width, "Escape restores the authored GridLength, not the resolved pixels");
			Assert.AreEqual(star, city.Width, "the yielding column is never written");
			Assert.AreEqual(nameWidth, name.ReadLocalValue(TableViewColumn.WidthProperty), "a frozen predecessor is restored");
		}
		finally
		{
			mouse.Release();
			await WindowHelper.WaitForIdle();
		}

		Assert.AreEqual(star, age.Width, "releasing after a cancel must not write");
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Header_Keyboard_Resize(bool rightToLeft)
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		name.Width = new GridLength(120, GridUnitType.Pixel);
		if (rightToLeft)
		{
			table.FlowDirection = FlowDirection.RightToLeft;
		}

		await LoadAsync(table);

		var headerCell = GetHeaderCell(table, name);
		Assert.IsTrue(headerCell.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

#if __SKIA__
		using var listener = RecordingAutomationListener.Install();
#endif

		// Alt+Left/Right is the resize chord (WPF DataGrid's binding).
		await PressHeaderResizeChordAsync("right", headerCell);

		// ResizeGripper.KeyboardIncrement defaults to 8 (c_defaultKeyboardIncrement); RTL mirrors the axis.
		Assert.AreEqual(GridUnitType.Pixel, name.Width.GridUnitType);
		Assert.AreEqual(rightToLeft ? 112.0 : 128.0, name.Width.Value, LayoutTolerance);

#if __SKIA__
		AssertColumnWidthAnnouncement(listener, headerCell, rightToLeft ? 112 : 128);
#endif

		await PressHeaderResizeChordAsync("left", headerCell);

		Assert.AreEqual(120.0, name.Width.Value, LayoutTolerance);

#if __SKIA__
		AssertColumnWidthAnnouncement(listener, headerCell, 120);
#endif

		// Shift takes the large step: c_largeIncrementMultiplier (4) x 8.
		await PressHeaderResizeChordAsync("right", headerCell, shift: true);

		Assert.AreEqual(rightToLeft ? 88.0 : 152.0, name.Width.Value, LayoutTolerance);

#if __SKIA__
		AssertColumnWidthAnnouncement(listener, headerCell, rightToLeft ? 88 : 152);
#endif

		// A bare arrow navigates the header band: no resize, no announcement.
		await KeyboardHelper.PressKeySequence("$d$_right#$u$_right", headerCell);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(rightToLeft ? 88.0 : 152.0, name.Width.Value, LayoutTolerance);

#if __SKIA__
		Assert.AreEqual(0, listener.TakeNotifications("TableViewColumnWidthChangedActivityId").Count);

		static void AssertColumnWidthAnnouncement(RecordingAutomationListener listener, FrameworkElement headerCell, int width)
		{
			// AnnounceColumnWidthOn: attributed to the focused header's peer, whole pixels.
			var announcements = listener.TakeNotifications("TableViewColumnWidthChangedActivityId");
			Assert.AreEqual(1, announcements.Count, "one announcement per completed resize");
			var announcement = announcements[0];
			Assert.AreSame(FrameworkElementAutomationPeer.FromElement(headerCell), announcement.Peer);
			Assert.AreEqual(AutomationNotificationKind.Other, announcement.Kind);
			Assert.AreEqual(AutomationNotificationProcessing.MostRecent, announcement.Processing);
			Assert.AreEqual(
				StringUtil.FormatString(ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TableViewColumnWidthChanged), "Name", width.ToString(CultureInfo.InvariantCulture)),
				announcement.DisplayString);
		}
#endif
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)] // UIElement.Translation
	public async Task When_Frozen_Cell_Pressed_Over_Scrolled_Column()
	{
		var table = new TableView { IsReadOnly = false };
		var frozen = TextColumn(nameof(Person.Name), new GridLength(100, GridUnitType.Pixel));
		frozen.FrozenEdge = TableViewFrozenEdge.Leading;
		var scrolling = TextColumn(nameof(Person.Age), new GridLength(300, GridUnitType.Pixel));
		table.Columns.Add(frozen);
		table.Columns.Add(scrolling);
		table.Columns.Add(TextColumn(nameof(Person.City), new GridLength(300, GridUnitType.Pixel)));
		table.ItemsSource = People(5);

		await LoadAsync(table, width: 300);

		var scroller = GetBodyScroller(table);
		scroller.ChangeView(150, null, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(scroller.HorizontalOffset - 150) < 0.5);
		await WindowHelper.WaitForIdle();

		var beginning = new List<TableViewBeginningEditEventArgs>();
		table.BeginningEdit += (_, e) => beginning.Add(e);

		// The pinned frozen cell sits over the start of the scrolled Age column; ResolvePressedColumn
		// must pick the frozen cell on top, not the column laid out underneath it.
		var row = GetRow(table, 1)!;
		var viewportLeft = scroller.TransformToVisual(null).TransformPoint(new Point(0, 0)).X;
		var point = new Point(viewportLeft + 50, Center(row).Y);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		mouse.Press(point);
		mouse.Release();
		mouse.Press(point);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, beginning.Count);
		Assert.AreSame(frozen, beginning[0].Column);
		Assert.IsTrue(table.IsEditing);
		Assert.IsTrue(Descendants(GetCell(GetRow(table, 1)!, frozen)).OfType<TextBox>().Any(), "the editor is hosted in the frozen cell");
	}

	// KeyboardStateTracker is process-wide; never let a failed test leave a modifier down.
	private static async Task PressHeaderResizeChordAsync(string arrow, UIElement headerCell, bool shift = false)
	{
		try
		{
			var keys = shift
				? $"$d$_alt#$d$_shift#$d$_{arrow}#$u$_{arrow}#$u$_shift#$u$_alt"
				: $"$d$_alt#$d$_{arrow}#$u$_{arrow}#$u$_alt";
			await KeyboardHelper.PressKeySequence(keys, headerCell);
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			if (shift)
			{
				await ReleaseModifierAsync("shift");
			}

			await ReleaseModifierAsync("alt");
		}
	}

	private static async Task ReleaseModifierAsync(string key)
	{
		if (FocusManager.GetFocusedElement(WindowHelper.XamlRoot) is UIElement focused)
		{
			await KeyboardHelper.PressKeySequence($"$u$_{key}", focused);
		}
	}

	private static FrameworkElement? FindGripper(Grid headerCell)
		=> Descendants(headerCell).OfType<FrameworkElement>().FirstOrDefault(e => e.GetType().Name == "ResizeGripper");
}

#endif
