#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Uno.UI.DevTools.Input;
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
		}
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
		Assert.IsFalse(GetHeaderCell(table, name).IsTabStop);
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

		// The drag is clamped to MaxWidth.
		var start = Center(FindGripper(GetHeaderCell(table, name))!);
		mouse.Press(start);
		mouse.MoveTo(new Point(start.X + 80, start.Y), 8);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(GridUnitType.Pixel, name.Width.GridUnitType);
		Assert.AreEqual(160.0, name.Width.Value, LayoutTolerance);
		await WindowHelper.WaitForEqual(160.0, () => name.ActualWidth, LayoutTolerance);

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
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Resize_Drag_Escape_Restores_Authored_Width()
	{
		var table = CreateTable(People(3));
		var city = table.Columns[2];
		var star = new GridLength(1, GridUnitType.Star);
		city.Width = star;
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		var start = Center(FindGripper(GetHeaderCell(table, city))!);
		mouse.Press(start);
		try
		{
			mouse.MoveTo(new Point(start.X - 40, start.Y), 8);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(GridUnitType.Pixel, city.Width.GridUnitType, "the drag writes a pixel width");

			await KeyboardHelper.PressKeySequence("$d$_esc#$u$_esc", table);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(star, city.Width, "Escape restores the authored GridLength, not the resolved pixels");
		}
		finally
		{
			mouse.Release();
			await WindowHelper.WaitForIdle();
		}

		Assert.AreEqual(star, city.Width, "releasing after a cancel must not write");
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

		await KeyboardHelper.PressKeySequence("$d$_right#$u$_right", headerCell);
		await WindowHelper.WaitForIdle();

		// ResizeGripper.KeyboardIncrement defaults to 8 (c_defaultKeyboardIncrement); RTL mirrors the axis.
		Assert.AreEqual(GridUnitType.Pixel, name.Width.GridUnitType);
		Assert.AreEqual(rightToLeft ? 112.0 : 128.0, name.Width.Value, LayoutTolerance);

		await KeyboardHelper.PressKeySequence("$d$_left#$u$_left", headerCell);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(120.0, name.Width.Value, LayoutTolerance);

		// TODO: assert the "Column %1 width: %2 pixels." announcement (AnnounceColumnWidth) once the
		// runtime tests can observe AutomationPeer.RaiseNotificationEvent.
	}

	private static FrameworkElement? FindGripper(Grid headerCell)
		=> Descendants(headerCell).OfType<FrameworkElement>().FirstOrDefault(e => e.GetType().Name == "ResizeGripper");
}

#endif
