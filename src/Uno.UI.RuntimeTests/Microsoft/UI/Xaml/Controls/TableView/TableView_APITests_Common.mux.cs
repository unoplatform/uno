// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_APITests_Common.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Base for every TableView API test class.
public class TableViewApiTestBase : MUXApiTestBase
{
	// Hosts the element as test content and runs a synchronous layout pass. Call on the UI thread.
	protected void LoadContent(UIElement element)
	{
		Content = element;
		Content.UpdateLayout();
	}
}

// Shared fixtures for the TableView API test suite.
// A custom column written the way TableView.idl:198-201 requires: it binds reactively against
// the inherited DataContext and never assigns a local DataContext or bakes dataItem in as
// static content. Mirrors Samples\TableViewSampleApp\ScoreBarColumn.cs.
internal partial class ProbeColumn : TableViewColumn
{
	internal const string CellName = "ProbeCell";

	public int GenerateCount { get; private set; }

	public string ValuePath { get; set; } = "Name";

	protected override FrameworkElement GenerateElementCore(object dataItem)
	{
		GenerateCount++;

		var text = new TextBlock { Name = CellName };
		text.SetBinding(TextBlock.TextProperty, new Binding
		{
			Path = new PropertyPath(ValuePath),
			Mode = BindingMode.OneWay,
		});

		return text;
	}

	// TODO Uno: GetSortMemberPathCore is protected internal in Uno (TableView calls it directly), and this
	// assembly sees Uno.UI internals, so the override has to be protected internal too.
	protected internal override string GetSortMemberPathCore() => ValuePath;
}

// Which axis ScrollBodyTo drives; the body scroller can scroll on either.
internal enum ScrollAxis
{
	Horizontal,
	Vertical,
}

internal static class TableViewTestHelpers
{
	// Resource init + the column-less TableView shell shared by every Create* variation. Each
	// builder decides how to attach its columns (bound/unbound, widths, template) afterward.
	internal static TableView CreateTableViewShell(
		object itemsSource,
		double width,
		double height,
		DataTemplate emptyTemplate = null)
	{
		EnsureTabularControlsResources();

		return new TableView
		{
			ItemsSource = itemsSource,
			Width = width,
			Height = height,
			EmptyTemplate = emptyTemplate,
		};
	}

	// The single builder behind every fixture TableView.
	//      itemsSource defaults to MakeItems() for bound tables, null for unbound tables.
	//      headers default to a single "Name" column (pass an empty array for a column-less table).
	//      bound: true binds each text column to the property named by its header
	//                  (a header with no matching property renders empty cells);
	//      bound: false leaves the columns purely structural.
	internal static TableView CreateTableView(
		object itemsSource = null,
		string[] headers = null,
		bool bound = true,
		DataTemplate emptyTemplate = null,
		double width = 500,
		double height = 300)
	{
		object source = itemsSource ?? (bound ? MakeItems() : null);
		var tableView = CreateTableViewShell(source, width, height, emptyTemplate);

		foreach (var header in headers ?? new[] { "Name" })
		{
			tableView.Columns.Add(MakeTextColumn(header, bound: bound));
		}

		return tableView;
	}

	// A text column bound one-way to bindingPath (defaulting to the header). Width is left at the
	// column default unless supplied. Pass bound: false for a header-only column whose cells stay
	// empty - the unbound structural column CreateTableView(bound: false) uses.
	internal static TableViewTextColumn MakeTextColumn(string header, string bindingPath = null, GridLength? width = null, bool bound = true)
	{
		var column = new TableViewTextColumn { Header = header };

		if (bound)
		{
			column.Binding = new Binding { Path = new PropertyPath(bindingPath ?? header), Mode = BindingMode.OneWay };
		}

		if (width.HasValue)
		{
			column.Width = width.Value;
		}

		return column;
	}

	// A template column whose cells inflate cellTemplate. Width is left at the column default
	// unless supplied.
	internal static TableViewTemplateColumn MakeTemplateColumn(string header, DataTemplate cellTemplate, GridLength? width = null)
	{
		var column = new TableViewTemplateColumn { Header = header, CellTemplate = cellTemplate };

		if (width.HasValue)
		{
			column.Width = width.Value;
		}

		return column;
	}

	internal static List<Person> MakeItems() => new List<Person>
	{
		new Person { Name = "Asha", Role = "Designer" },
		new Person { Name = "Diego", Role = "Engineer" },
		new Person { Name = "Mei", Role = "Architect" },
	};

	internal static void EnsureTabularControlsResources()
	{
		if (!Application.Current.Resources.MergedDictionaries.OfType<TabularControlsResources>().Any())
		{
			Application.Current.Resources.MergedDictionaries.Add(new TabularControlsResources());
		}
	}

	internal static DataTemplate CreateTextTemplate(string text) => (DataTemplate)XamlReader.Load(
		$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                   <TextBlock Text=""{text}"" />
               </DataTemplate>");

	// The columns backing the header cells in PART_HeaderHost, in rendered order.
	internal static List<TableViewColumn> GetHeaderColumns(TableView tableView)
	{
		var host = GetHeaderHost(tableView);

		return host.Children
			.OfType<FrameworkElement>()
			.Select(child => child.Tag as TableViewColumn)
			.Where(column => column != null)
			.ToList();
	}

	// The ContentPresenter RebuildHeaders puts Header / HeaderTemplate / HeaderTemplateSelector on.
	internal static ContentPresenter GetHeaderPresenter(TableView tableView, int index)
	{
		var host = GetHeaderHost(tableView);
		Verify.IsGreaterThan(host.Children.Count, index, "The header host should have a cell at the requested index.");

		var presenter = ((DependencyObject)host.Children[index]).FindVisualChildByType<ContentPresenter>();
		Verify.IsNotNull(presenter, $"Header cell {index} should host a ContentPresenter.");
		return presenter;
	}

	// The columns backing the cell wrappers in a row's PART_CellsHost, in rendered order.
	internal static List<TableViewColumn> GetRowCellColumns(TableViewRow row)
	{
		return GetCellsHost(row).Children
			.OfType<FrameworkElement>()
			.Select(child => child.Tag as TableViewColumn)
			.Where(column => column != null)
			.ToList();
	}

	internal static List<TableViewRow> GetRealizedRows(TableView tableView)
		=> FindVisualChildrenByType<TableViewRow>(tableView);

	internal static void VerifyHeaderColumns(TableView tableView, IList<TableViewColumn> expected, string context)
	{
		var actual = GetHeaderColumns(tableView);
		Verify.AreEqual(expected.Count, actual.Count, $"Header cell count should match the column count ({context}).");

		for (int i = 0; i < expected.Count; i++)
		{
			Verify.AreEqual(expected[i], actual[i], $"Header at index {i} should belong to the column at that index ({context}).");
		}
	}

	internal static void VerifyEveryRowMatchesHeaders(TableView tableView, string context)
	{
		var headerColumns = GetHeaderColumns(tableView);
		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"At least one row should be realized ({context}).");

		foreach (var row in rows)
		{
			var cellColumns = GetRowCellColumns(row);
			Verify.AreEqual(headerColumns.Count, cellColumns.Count,
				$"Row cell count should match header cell count ({context}).");

			for (int i = 0; i < headerColumns.Count; i++)
			{
				Verify.AreEqual(headerColumns[i], cellColumns[i],
					$"Row cell at index {i} should belong to the same column as the header at that index ({context}).");
			}
		}
	}

	internal static List<T> FindVisualChildrenByType<T>(DependencyObject root) where T : DependencyObject
	{
		var results = new List<T>();
		Collect(root);
		return results;

		void Collect(DependencyObject element)
		{
			int count = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(element, i);
				if (child is T match)
				{
					results.Add(match);
				}

				Collect(child);
			}
		}
	}

	internal static Panel GetCellsHost(TableViewRow row)
	{
		var host = row.FindVisualChildByName("PART_CellsHost") as Panel;
		Verify.IsNotNull(host, "PART_CellsHost should exist on a realized row.");
		return host;
	}

	// A cell wrapper is a single-child Grid (TableViewCell): the Grid is the cell's focus and UIA
	// target and draws its vertical separator; the column-generated element is its only child.
	internal static Grid GetRowCell(TableViewRow row, int index)
	{
		var host = GetCellsHost(row);
		Verify.IsGreaterThan(host.Children.Count, index, "The cells host should have a cell at the requested index.");

		var wrapper = host.Children[index] as Grid;
		Verify.IsNotNull(wrapper, "Every cell is hosted in a Grid cell wrapper.");
		return wrapper;
	}

	// The column-generated element a cell wrapper hosts, or null for an empty cell.
	internal static UIElement GetCellContent(Grid cellWrapper)
		=> cellWrapper.Children.Count > 0 ? cellWrapper.Children[0] : null;

	// TODO Uno: async because IdleSynchronizer.Wait maps to await TestServices.WindowHelper.WaitForIdle().
	internal static async Task ScrollBodyTo(TableView tableView, ScrollAxis axis, double offset)
	{
		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			var scrollableExtent = axis == ScrollAxis.Horizontal ? scroller.ScrollableWidth : scroller.ScrollableHeight;
			Verify.IsGreaterThan(scrollableExtent, offset,
				"Precondition: the source must be scrollable on the requested axis by more than the offset under test.");

			// disableAnimation so the offset lands synchronously rather than over a composition
			// animation the test would have to poll for.
			scroller.ChangeView(
				axis == ScrollAxis.Horizontal ? offset : (double?)null,
				axis == ScrollAxis.Vertical ? offset : (double?)null,
				null,
				true);
		});

		await SettleLayout(tableView);
	}

	internal static ScrollViewer GetBodyScroller(TableView tableView)
	{
		var scroller = tableView.FindVisualChildByName("PART_BodyScroller") as ScrollViewer;
		Verify.IsNotNull(scroller, "PART_BodyScroller should exist once the template has applied.");
		return scroller;
	}

	internal static FrameworkElement GetHeaderCell(TableView tableView, int index)
	{
		var host = GetHeaderHost(tableView);
		Verify.IsGreaterThan(host.Children.Count, index, "The header host should have a cell at the requested index.");
		return (FrameworkElement)host.Children[index];
	}

	internal static ResizeGripper FindGripper(DependencyObject headerCell)
		=> FindVisualChildrenByType<ResizeGripper>(headerCell).FirstOrDefault();

	internal static ResizeGripper RequireGripper(TableView tableView, int columnIndex)
	{
		var gripper = FindGripper(GetHeaderCell(tableView, columnIndex));
		Verify.IsNotNull(gripper, $"Column {columnIndex} should have a resize gripper in its header cell.");
		return gripper;
	}

	// The general fixture builder: a TableView over items (default sample rows) sized width x
	// height, with text columns each bound to "Name" at the given widths. Defaults to a single
	// 200px "Name" column when none are supplied.
	internal static TableView CreateTableViewWithColumns(
		object items = null,
		double width = 500,
		double height = 260,
		params (string Header, GridLength Width)[] columns)
	{
		var tableView = CreateTableViewShell(items ?? MakeItems(), width, height);

		if (columns.Length == 0)
		{
			columns = new[] { ("Name", new GridLength(200.0, GridUnitType.Pixel)) };
		}

		foreach (var (header, columnWidth) in columns)
		{
			tableView.Columns.Add(MakeTextColumn(header, "Name", columnWidth));
		}

		return tableView;
	}

	// Waits for the dispatcher, forces a layout pass, and waits again, so a source or template change
	// has been fully realized before the test reads the visual tree.
	// TODO Uno: async because IdleSynchronizer.Wait maps to await TestServices.WindowHelper.WaitForIdle().
	internal static async Task SettleLayout(TableView tableView)
	{
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => tableView.UpdateLayout());
		await TestServices.WindowHelper.WaitForIdle();
	}

	internal static Panel GetHeaderHost(TableView tableView)
	{
		var host = tableView.FindVisualChildByName("PART_HeaderHost") as Panel;
		Verify.IsNotNull(host, "PART_HeaderHost should exist once the template has applied.");
		return host;
	}

	internal static ItemsRepeater GetRowsRepeater(TableView tableView)
	{
		var repeater = tableView.FindVisualChildByName("PART_RowsRepeater") as ItemsRepeater;
		Verify.IsNotNull(repeater, "PART_RowsRepeater should exist once the template has applied.");
		return repeater;
	}

	// The repeater's view of the projected source: data rows and group header rows, in projection
	// order, independent of what happens to be realized.
	private static ItemsSourceView GetProjectionView(TableView tableView)
	{
		var view = GetRowsRepeater(tableView).ItemsSourceView;
		if (view == null)
		{
			Verify.Fail("PART_RowsRepeater should have an ItemsSourceView once the source is bound.");
		}

		return view;
	}

	internal static int GetProjectedCount(TableView tableView) => GetProjectionView(tableView)?.Count ?? 0;

	internal static object GetProjectedItem(TableView tableView, int index)
	{
		var view = GetProjectionView(tableView);
		if (view == null || index >= view.Count)
		{
			Verify.Fail($"No projected row at index {index}.");
			return null;
		}

		return view.GetAt(index);
	}

	internal static List<object> GetProjectedItems(TableView tableView)
	{
		var items = new List<object>();
		var view = GetProjectionView(tableView);

		for (var i = 0; i < (view?.Count ?? 0); i++)
		{
			items.Add(view.GetAt(i));
		}

		return items;
	}

	// The element realized for each projected index, in projection order rather than the
	// visual-child order, which is recycling order. Cleared containers stay parented to the
	// repeater's panel, so a visual-tree walk over-counts after any source mutation.
	//
	// requireAllRealized: true fails the test on the first unrealized index (the fixtures are sized
	// so every row realizes); false keeps a null entry for it.
	internal static List<UIElement> GetProjectedElements(TableView tableView, bool requireAllRealized = true)
	{
		var elements = new List<UIElement>();
		var repeater = GetRowsRepeater(tableView);
		var view = GetProjectionView(tableView);

		for (var i = 0; i < (view?.Count ?? 0); i++)
		{
			var element = repeater.TryGetElement(i);
			if (element == null && requireAllRealized)
			{
				Verify.Fail($"Projected element {i} of {view.Count} should be realized; the fixtures are sized so every one is.");
				return elements;
			}

			elements.Add(element);
		}

		return elements;
	}

	internal static TableViewRow GetProjectedRow(TableView tableView, int index)
	{
		var row = GetRowsRepeater(tableView).TryGetElement(index) as TableViewRow;
		if (row == null)
		{
			Verify.Fail($"Row {index} should be realized in these fixtures.");
		}

		return row;
	}

	// One label per projected row: the ShapedPerson's name for a data row, and "#Key" (or
	// "#Key(Count)" when includeCount) for a group header row.
	internal static List<string> GetProjectedLabels(TableView tableView, bool includeCount)
	{
		var labels = new List<string>();

		foreach (var element in GetProjectedElements(tableView))
		{
			if (element is TableViewGroupHeader header)
			{
				var info = header.Content as TableViewGroupInfo;
				if (info == null)
				{
					Verify.Fail($"A group header should carry a TableViewGroupInfo, saw '{header.Content}'.");
					return labels;
				}

				labels.Add(includeCount ? $"#{info.Key}({info.ItemCount})" : $"#{info.Key}");
			}
			else if (element is TableViewRow row)
			{
				var person = row.DataContext as ShapedPerson;
				if (person == null)
				{
					Verify.Fail($"A row should be bound to a ShapedPerson, saw '{row.DataContext}'.");
					return labels;
				}

				labels.Add(person.Name);
			}
			else
			{
				Verify.Fail($"A projected element realized as {element.GetType().Name}, which is neither a row nor a group header.");
				return labels;
			}
		}

		return labels;
	}

	// Logs both sequences on any failure, so an ordering bug is readable from the log alone.
	internal static void VerifySequence(IList<string> expected, IList<string> actual, string context)
	{
		var detail = $"Expected [{string.Join(", ", expected)}], saw [{string.Join(", ", actual ?? new List<string>())}].";

		if (actual == null)
		{
			Verify.Fail($"No rows were captured ({context}). {detail}");
			return;
		}

		Verify.AreEqual(expected.Count, actual.Count, $"Projected row count ({context}). {detail}");

		for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
		{
			Verify.AreEqual(expected[i], actual[i], $"Projected row {i} ({context}). {detail}");
		}
	}

	// Indexed in projection order, not visual-child order.
	internal static TableViewGroupHeader GetGroupHeader(TableView tableView, int index)
	{
		var headers = GetProjectedElements(tableView).OfType<TableViewGroupHeader>().ToList();
		if (headers.Count <= index)
		{
			Verify.Fail($"The test needs a projected group header at index {index}; saw {headers.Count}.");
			return null;
		}

		return headers[index];
	}

	internal static TableViewGroupInfo GetGroupInfo(TableView tableView, int index)
	{
		var header = GetGroupHeader(tableView, index);
		var info = header?.Content as TableViewGroupInfo;
		if (header != null && info == null)
		{
			Verify.Fail($"Group header {index} should carry a TableViewGroupInfo as its Content, saw '{header.Content}'.");
		}

		return info;
	}

	internal static TableViewGroupHeaderAutomationPeer GetGroupHeaderPeer(TableView tableView, int index)
	{
		var header = GetGroupHeader(tableView, index);
		var peer = header == null ? null : FrameworkElementAutomationPeer.CreatePeerForElement(header) as TableViewGroupHeaderAutomationPeer;
		Verify.IsNotNull(peer, "A TableViewGroupHeader must produce a TableViewGroupHeaderAutomationPeer.");
		return peer;
	}

	// Setting TableViewGroupHeader.IsExpanded only mirrors state onto the header; the reshape runs
	// through the owner, so the peer's ExpandCollapse pattern is the input-free way to toggle a group.
	internal static IExpandCollapseProvider GetExpandCollapseProvider(TableView tableView, int index)
	{
		var provider = GetGroupHeaderPeer(tableView, index).GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
		Verify.IsNotNull(provider, "A group header peer must advertise ExpandCollapse.");
		return provider;
	}

	internal static TableViewRow GetRowForItem(TableView tableView, object item)
	{
		var row = GetRealizedRows(tableView).FirstOrDefault(r => ReferenceEquals(r.DataContext, item));
		Verify.IsNotNull(row, "The test needs a realized row for the target item.");
		return row;
	}

	internal static TableViewRowAutomationPeer GetRowPeer(TableViewRow row)
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(row) as TableViewRowAutomationPeer;
		Verify.IsNotNull(peer, "A TableViewRow must produce a TableViewRowAutomationPeer.");
		return peer;
	}

	internal static TableViewCellAutomationPeer GetCellPeer(TableViewRowAutomationPeer rowPeer, int visibleColumnIndex)
	{
		var children = rowPeer.GetChildren();
		Verify.IsTrue(
			children != null && children.Count > visibleColumnIndex,
			$"The row peer must expose a cell peer for visible column {visibleColumnIndex}; saw {children?.Count ?? 0}.");

		var cellPeer = children[visibleColumnIndex] as TableViewCellAutomationPeer;
		Verify.IsNotNull(cellPeer, $"Child {visibleColumnIndex} of the row peer must be a TableViewCellAutomationPeer.");
		return cellPeer;
	}

	internal static TableViewCellAutomationPeer GetCellPeer(TableViewRow row, int columnIndex)
		=> GetCellPeer(GetRowPeer(row), columnIndex);

	internal static TableViewCellAutomationPeer GetCellPeer(TableView tableView, object item, int columnIndex)
		=> GetCellPeer(GetRowForItem(tableView, item), columnIndex);

	// The current state of a visual state group declared in the control's template. Templates may
	// wrap the element that owns the groups (e.g. an outer layout Grid around PART_RootBorder), so
	// search the template tree for the first element that declares the group.
	internal static string GetCurrentVisualState(Control control, string groupName)
	{
		var root = control != null && VisualTreeHelper.GetChildrenCount(control) > 0
			? VisualTreeHelper.GetChild(control, 0) as FrameworkElement
			: null;

		if (root == null)
		{
			Verify.Fail("The control's template should have applied.");
			return null;
		}

		var group = new[] { root }
			.Concat(FindVisualChildrenByType<FrameworkElement>(root))
			.SelectMany(element => VisualStateManager.GetVisualStateGroups(element))
			.FirstOrDefault(candidate => candidate.Name == groupName);
		if (group == null)
		{
			Verify.Fail($"The template should declare a '{groupName}' visual state group.");
			return null;
		}

		return group.CurrentState?.Name;
	}

	// The TextBlock a cell renders, whichever column type produced it: a text column generates one
	// directly, a template column generates a ContentPresenter that inflates one.
	internal static TextBlock GetCellTextBlock(Grid cellWrapper)
	{
		var textBlock = GetCellContent(cellWrapper) as TextBlock ?? FindVisualChildrenByType<TextBlock>(cellWrapper).FirstOrDefault();
		if (textBlock == null)
		{
			Verify.Fail("The cell should host a TextBlock.");
		}

		return textBlock;
	}

	internal static string GetCellText(Grid cellWrapper) => GetCellTextBlock(cellWrapper)?.Text;

	internal static string GetCellText(TableView tableView, int rowIndex, int columnIndex)
	{
		var row = GetProjectedRow(tableView, rowIndex);
		return row == null ? null : GetCellText(GetRowCell(row, columnIndex));
	}
}
// The default row item: two plain, non-notifying string properties. Tests that need change
// notification, validation, or grouping keys use the richer items defined by their own area.
internal sealed class Person
{
	public string Name { get; set; }

	public string Role { get; set; }
}

internal static class TableViewRowTestHelpers
{
	internal const string VeryLongText = "A considerably longer piece of cell text than the column can possibly show";

	internal static TableView CreateTemplateColumnTable(List<Person> items)
	{
		var tableView = CreateTableViewShell(items, 500, 260);

		tableView.Columns.Add(MakeTemplateColumn("Name", CreateBoundTextTemplate(), new GridLength(200.0, GridUnitType.Pixel)));

		return tableView;
	}

	// One text column and one template column over the same property, so a recycle test can check
	// both content routes on the same row.
	internal static TableView CreateMixedColumnTable(List<Person> items)
	{
		var tableView = CreateTableViewWithColumns(items, columns: new[] { ("Text", new GridLength(180.0, GridUnitType.Pixel)) });

		tableView.Columns.Add(MakeTemplateColumn("Template", CreateBoundTextTemplate(), new GridLength(180.0, GridUnitType.Pixel)));

		return tableView;
	}

	internal static DataTemplate CreateBoundTextTemplate(string path = "Name") => (DataTemplate)XamlReader.Load(
		$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                  <TextBlock Text=""{{Binding {path}}}"" />
              </DataTemplate>");

	internal static List<Person> MakeManyItems(int count) => Enumerable
		.Range(0, count)
		.Select(i => new Person { Name = $"Person {i}", Role = $"Role {i}" })
		.ToList();

	internal static TableViewRow RequireFirstRow(TableView tableView)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, "At least one row should be realized.");
		return rows[0];
	}

	internal static void VerifyNoLocalDataContext(FrameworkElement element, string what)
	{
		Verify.AreEqual(DependencyProperty.UnsetValue, element.ReadLocalValue(FrameworkElement.DataContextProperty),
			$"{what} must not set a local DataContext, or it shadows inheritance and cells go stale after recycle.");
	}

	// The CommonStates state a row is currently in. Read by name rather than by brush because
	// several states share a brush, which would make a wrong state look correct.
	internal static string GetCommonState(TableViewRow row) => GetCurrentVisualState(row, "CommonStates");

	internal static void VerifyBanding(
		TableView tableView,
		List<Person> items,
		Brush baseBrush,
		Brush alternateBrush,
		string context)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"Rows should be realized ({context}).");

		foreach (var row in rows)
		{
			var index = items.IndexOf(row.DataContext as Person);
			Verify.IsGreaterThanOrEqual(index, 0, $"Every realized row should map to a source item ({context}).");

			var expected = (index % 2) == 0 ? baseBrush : alternateBrush;
			Verify.AreEqual(expected, row.Background,
				$"Row at index {index} should carry the brush for its parity ({context}).");
		}
	}
}
