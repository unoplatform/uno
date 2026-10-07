// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Columns_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 3.1 of the TableView test plan: the Columns collection.
//
// These tests assert against two structures the control builds from Columns:
//   - PART_HeaderHost, whose children are header cell Grids, each tagged with its column
//     (TableView.cpp:1610, headerCell.Tag(column)).
//   - each realized TableViewRow's PART_CellsHost, whose children are Grid cell wrappers,
//     each tagged with its column (TableViewRow.cpp:762, cellWrapper.Tag(column)).
// The Tag is the supported way to map a rendered element back to the column that produced it,
// and it is what the control's own frozen-column and recycling code uses.
[TestClass]
public class TableViewColumnsTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies TableView.Columns is observable despite being typed as IVector, and raises VectorChanged.")]
	public void VerifyColumnsCollectionIsObservable()
	{
		RunOnUIThread.Execute(() =>
		{
			var tableView = new TableView();

			// TableView.idl:452 documents that the ABI exposes IVector but the backing vector is
			// observable, because the control relies on VectorChanged for live column updates.
			// If this cast ever stops working, every incremental column update silently becomes a
			// full rebuild or no rebuild at all, so pin it explicitly.
			var observable = tableView.Columns as IObservableVector<TableViewColumn>;
			Verify.IsNotNull(observable, "Columns should be castable to IObservableVector<TableViewColumn>.");

			var changes = new List<(CollectionChange Change, uint Index)>();
			void OnVectorChanged(IObservableVector<TableViewColumn> sender, IVectorChangedEventArgs args)
				=> changes.Add((args.CollectionChange, args.Index));

			observable.VectorChanged += OnVectorChanged;
			try
			{
				var first = new TableViewTextColumn { Header = "Name" };
				var second = new TableViewTextColumn { Header = "Role" };

				tableView.Columns.Add(first);
				tableView.Columns.Insert(0, second);
				tableView.Columns.RemoveAt(0);
				tableView.Columns.Clear();
			}
			finally
			{
				observable.VectorChanged -= OnVectorChanged;
			}

			Verify.AreEqual(4, changes.Count, "Add, Insert, RemoveAt, and Clear should each raise exactly one notification.");
			Verify.AreEqual(CollectionChange.ItemInserted, changes[0].Change, "Add should report ItemInserted.");
			Verify.AreEqual(0u, changes[0].Index, "Add to an empty collection should report index 0.");
			Verify.AreEqual(CollectionChange.ItemInserted, changes[1].Change, "Insert should report ItemInserted.");
			Verify.AreEqual(0u, changes[1].Index, "Insert at 0 should report index 0.");
			Verify.AreEqual(CollectionChange.ItemRemoved, changes[2].Change, "RemoveAt should report ItemRemoved.");
			Verify.AreEqual(0u, changes[2].Index, "RemoveAt(0) should report index 0.");
			Verify.AreEqual(CollectionChange.Reset, changes[3].Change, "Clear should report Reset.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies adding a column creates its header and a cell in every realized row.")]
	public async Task VerifyAddColumnAddsHeaderAndCells()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), "before the add");
			VerifyEveryRowMatchesHeaders(tableView, "before the add");

			tableView.Columns.Add(new TableViewTextColumn { Header = "Role" });
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(2, tableView.Columns.Count);
			VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), "after the add");
			VerifyEveryRowMatchesHeaders(tableView, "after the add");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies inserting a column places its header and cells at the requested index.")]
	public async Task VerifyInsertColumnPlacesHeaderAtIndex()
	{
		TableView tableView = null;
		TableViewColumn inserted = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name", "Role");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			inserted = new TableViewTextColumn { Header = "City" };
			tableView.Columns.Insert(1, inserted);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Order matters, not just membership: an insert that appends would still produce the
			// right set of headers while rendering the table in the wrong order.
			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(3, headerColumns.Count);
			Verify.AreEqual(inserted, headerColumns[1], "The inserted column's header should be at index 1.");

			VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), "after the insert");
			VerifyEveryRowMatchesHeaders(tableView, "after the insert");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a column removes its header and its cell from every row.")]
	public async Task VerifyRemoveColumnRemovesHeaderAndCells()
	{
		TableView tableView = null;
		TableViewColumn removed = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name", "Role", "City");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			removed = tableView.Columns[1];
			tableView.Columns.RemoveAt(1);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(2, headerColumns.Count);
			Verify.IsFalse(headerColumns.Contains(removed), "The removed column should have no header.");

			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.IsFalse(GetRowCellColumns(row).Contains(removed), "The removed column should have no cell in any row.");
			}

			VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), "after the remove");
			VerifyEveryRowMatchesHeaders(tableView, "after the remove");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Columns.Clear removes every header and leaves rows with no cells.")]
	public async Task VerifyClearColumnsRemovesAllHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name", "Role");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Columns.Clear();
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(0, tableView.Columns.Count);
			Verify.AreEqual(0, GetHeaderColumns(tableView).Count, "Clearing Columns should leave no header cells.");

			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.AreEqual(0, GetRowCellColumns(row).Count, "Clearing Columns should leave every row with no cells.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing a column at an index swaps the header and cells at that position only.")]
	public async Task VerifyReplaceColumnSwapsHeaderAndCells()
	{
		TableView tableView = null;
		TableViewColumn replaced = null;
		TableViewColumn replacement = null;
		TableViewColumn untouched = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name", "Role");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			replaced = tableView.Columns[0];
			untouched = tableView.Columns[1];
			replacement = new TableViewTextColumn { Header = "City" };

			tableView.Columns[0] = replacement;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(2, headerColumns.Count);
			Verify.AreEqual(replacement, headerColumns[0], "The replacement column should own index 0.");
			Verify.AreEqual(untouched, headerColumns[1], "The column at index 1 should be untouched by the replace.");
			Verify.IsFalse(headerColumns.Contains(replaced), "The replaced column should no longer have a header.");

			VerifyEveryRowMatchesHeaders(tableView, "after the replace");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column edits made before the control loads are reflected on first layout.")]
	public async Task VerifyColumnsChangedBeforeLoadAppliesOnLoad()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// Every mutation here happens with no template applied and no visual tree, so the
			// control cannot rebuild incrementally; it must reconcile on first layout instead.
			tableView = CreateTableViewShell(MakeItems(), 500, 300);

			var scratch = new TableViewTextColumn { Header = "Scratch" };
			tableView.Columns.Add(scratch);
			tableView.Columns.Add(new TableViewTextColumn { Header = "Name" });
			tableView.Columns.Insert(0, new TableViewTextColumn { Header = "Role" });
			tableView.Columns.Remove(scratch);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(2, tableView.Columns.Count);
			VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), "after loading with pre-load column edits");
			VerifyEveryRowMatchesHeaders(tableView, "after loading with pre-load column edits");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the header host and every row's cells host stay in sync across add, insert, remove, and clear.")]
	public async Task VerifyHeaderAndRowCellsStayInSyncAfterColumnMutation()
	{
		// Headers and row cells are rebuilt by two independent reactions to the same
		// Columns.VectorChanged: TableView::RebuildHeaders and TableViewRow::OnColumnsVectorChanged.
		// If one path runs and the other does not, the two hosts desynchronize. That is checked
		// structurally here rather than by comparing pixel offsets: both hosts are the same
		// TableViewCellsPanel type and arrange children from column ActualWidth in Columns order,
		// so identical column lists are what alignment actually reduces to.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name", "Role");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => VerifyEveryRowMatchesHeaders(tableView, "initial"));

		var mutations = new (string Name, Action<TableView> Apply)[]
		{
			("add", tv => tv.Columns.Add(new TableViewTextColumn { Header = "City" })),
			("insert", tv => tv.Columns.Insert(0, new TableViewTextColumn { Header = "Score" })),
			("remove", tv => tv.Columns.RemoveAt(1)),
			("replace", tv => tv.Columns[0] = new TableViewTextColumn { Header = "Notes" }),
			("clear", tv => tv.Columns.Clear()),
		};

		foreach (var mutation in mutations)
		{
			var current = mutation;

			RunOnUIThread.Execute(() =>
			{
				current.Apply(tableView);
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyHeaderColumns(tableView, tableView.Columns.ToArray(), $"after {current.Name}");
				VerifyEveryRowMatchesHeaders(tableView, $"after {current.Name}");
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the same column instance added twice to one TableView renders twice without corrupting layout.")]
	public async Task VerifySameColumnInstanceAddedTwiceBehavesPerContract()
	{
		// TableViewColumn::SetOwningTableViewInternal rejects only a re-own by a *different*
		// TableView; re-owning by the same control returns true. So a duplicate entry in one
		// Columns collection is owned, tracked, and rendered like any other entry. This pins that
		// documented-by-code behavior: two headers and two cells per row for one column object.
		TableView tableView = null;
		TableViewColumn shared = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLoadedTableView("Name");
			shared = tableView.Columns[0];
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Columns.Add(shared);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(2, tableView.Columns.Count, "The same instance should be accepted at two indices.");

			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(2, headerColumns.Count, "A duplicated column should produce one header per entry.");
			Verify.AreEqual(shared, headerColumns[0]);
			Verify.AreEqual(shared, headerColumns[1]);

			VerifyEveryRowMatchesHeaders(tableView, "after duplicating a column instance");
		});
	}


	// Builds a TableView with the given text column headers, loads it, and lays it out.
	private TableView CreateLoadedTableView(params string[] headers)
	{
		var tableView = CreateTableView(headers: headers);
		LoadContent(tableView);
		return tableView;
	}
}

// Category 3.2 of the TableView test plan: column ownership.
[TestClass]
public class TableViewColumnOwnershipTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a column instance removed from Columns can be added back and renders again.")]
	public async Task VerifyRemovedColumnCanBeReAdded()
	{
		TableView tableView = null;
		TableViewColumn column = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			column = tableView.Columns[0];
			tableView.Columns.RemoveAt(0);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, GetHeaderColumns(tableView).Count, "The removed column should be gone before it is re-added.");

			// The same instance goes back in. Ownership was released on removal and has to be
			// re-acquired here, which is the part that can regress.
			tableView.Columns.Insert(0, column);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(2, headerColumns.Count, "Re-adding the column should restore its header.");
			Verify.AreEqual(column, headerColumns[0], "The re-added column should own index 0 again.");
			VerifyEveryRowMatchesHeaders(tableView, "after re-adding a removed column");
		});
	}
}

// Categories 3.3 and 3.4 of the TableView test plan: header content and header visibility.
//
// Header content is read back from the ContentPresenter inside each header cell, which is where
// RebuildHeaders puts Header, HeaderTemplate, and HeaderTemplateSelector.
[TestClass]
public class TableViewHeaderTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies string, non-string, and null Header values all render a header cell.")]
	public async Task VerifyHeaderRendersStringObjectAndNull()
	{
		// Merged from three checklist items that differed only in the value assigned to Header.
		var objectHeader = new Person { Name = "Boxed", Role = "Header" };
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new string[0]);
			tableView.Columns.Add(new TableViewTextColumn { Header = "Name" });
			tableView.Columns.Add(new TableViewTextColumn { Header = objectHeader });
			tableView.Columns.Add(new TableViewTextColumn { Header = null });

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetHeaderColumns(tableView).Count, "Every column should get a header cell, including the null-header one.");

			Verify.AreEqual("Name", GetRenderedHeaderText(tableView, 0), "A string Header should be presented verbatim.");
			Verify.AreEqual(objectHeader, GetHeaderPresenter(tableView, 1).Content, "A non-string Header should be presented as the object itself, not stringified.");
			Verify.IsNull(GetHeaderPresenter(tableView, 2).Content, "A null Header should present null rather than a placeholder.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies changing Header on a loaded column updates the rendered header.")]
	public async Task VerifyHeaderChangeAfterLoadUpdatesLive()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Name", GetRenderedHeaderText(tableView, 0));

			tableView.Columns[0].Header = "Renamed";
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Renamed", GetRenderedHeaderText(tableView, 0),
				"Changing Header after load should update the rendered header.");
			Verify.AreEqual("Role", GetRenderedHeaderText(tableView, 1),
				"The other column's header should be untouched.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HeaderTemplate is applied to the header content presenter.")]
	public async Task VerifyHeaderTemplateApplies()
	{
		TableView tableView = null;
		DataTemplate template = null;

		RunOnUIThread.Execute(() =>
		{
			template = CreateTextTemplate("Templated");
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns[0].HeaderTemplate = template;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var presenter = GetHeaderPresenter(tableView, 0);
			Verify.AreEqual(template, presenter.ContentTemplate, "HeaderTemplate should be applied to the header presenter.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HeaderTemplateSelector is applied to the header content presenter.")]
	public async Task VerifyHeaderTemplateSelectorApplies()
	{
		TableView tableView = null;
		TestHeaderTemplateSelector selector = null;

		RunOnUIThread.Execute(() =>
		{
			selector = new TestHeaderTemplateSelector { Template = CreateTextTemplate("Selected") };
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns[0].HeaderTemplateSelector = selector;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var presenter = GetHeaderPresenter(tableView, 0);
			Verify.AreEqual(selector, presenter.ContentTemplateSelector, "HeaderTemplateSelector should be applied to the header presenter.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HeaderTemplateSelector wins over HeaderTemplate when both are set, per TableView.idl.")]
	public async Task VerifyHeaderTemplateSelectorTakesPrecedenceOverHeaderTemplate()
	{
		// TableView.idl:178 states "Optional header template; HeaderTemplateSelector takes
		// precedence." That is the OPPOSITE of the ContentControl convention, where
		// ContentTemplate wins over ContentTemplateSelector. The deviation is deliberate and is
		// pinned here because it is exactly the kind of thing that gets "fixed" by accident.
		TableView tableView = null;
		TestHeaderTemplateSelector selector = null;

		RunOnUIThread.Execute(() =>
		{
			selector = new TestHeaderTemplateSelector { Template = CreateTextTemplate("Selected") };

			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns[0].HeaderTemplate = CreateTextTemplate("Templated");
			tableView.Columns[0].HeaderTemplateSelector = selector;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var presenter = GetHeaderPresenter(tableView, 0);
			Verify.AreEqual(selector, presenter.ContentTemplateSelector, "The selector should be applied when both are set.");
			Verify.IsNull(presenter.ContentTemplate, "HeaderTemplate must not be applied when a selector is present.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing HeaderTemplate on a loaded column rebuilds the header.")]
	public async Task VerifyHeaderTemplateChangeAfterLoadUpdatesLive()
	{
		TableView tableView = null;
		DataTemplate replacement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns[0].HeaderTemplate = CreateTextTemplate("First");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			replacement = CreateTextTemplate("Second");
			tableView.Columns[0].HeaderTemplate = replacement;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(replacement, GetHeaderPresenter(tableView, 0).ContentTemplate,
				"Replacing HeaderTemplate after load should rebuild the header with the new template.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing HeaderTemplateSelector on a loaded column rebuilds the header.")]
	public async Task VerifyHeaderTemplateSelectorChangeAfterLoadUpdatesLive()
	{
		// Kept separate from the HeaderTemplate test above: these are different property-changed
		// callbacks, so a regression can plausibly break one and not the other.
		TableView tableView = null;
		TestHeaderTemplateSelector replacement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns[0].HeaderTemplateSelector = new TestHeaderTemplateSelector { Template = CreateTextTemplate("First") };

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			replacement = new TestHeaderTemplateSelector { Template = CreateTextTemplate("Second") };
			tableView.Columns[0].HeaderTemplateSelector = replacement;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(replacement, GetHeaderPresenter(tableView, 0).ContentTemplateSelector,
				"Replacing HeaderTemplateSelector after load should rebuild the header and re-run selection.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HeadersVisibility.Column renders a visible header row with non-zero height.")]
	public async Task VerifyHeadersVisibilityColumnShowsHeaderRow()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			Verify.AreEqual(TableViewHeadersVisibility.Column, tableView.HeadersVisibility, "Column is the documented default.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerRow = GetHeaderRow(tableView);
			Verify.AreEqual(Visibility.Visible, headerRow.Visibility);
			Verify.IsGreaterThan(headerRow.ActualHeight, 0.0, "A visible header row should take up height.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies HeadersVisibility.None collapses the header row and reclaims its height.")]
	public async Task VerifyHeadersVisibilityNoneCollapsesHeaderRow()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerRow = GetHeaderRow(tableView);

			// Collapsed rather than Hidden is the whole point of the value: the band must not
			// reserve space behind the rows.
			Verify.AreEqual(Visibility.Collapsed, headerRow.Visibility);
			Verify.AreEqual(0.0, headerRow.ActualHeight, "A collapsed header row must contribute no height.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies toggling HeadersVisibility on a loaded control updates the header row both ways.")]
	public async Task VerifyHeadersVisibilityChangeAfterLoadUpdatesLive()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(Visibility.Collapsed, GetHeaderRow(tableView).Visibility, "Switching to None should collapse the header row.");

			tableView.HeadersVisibility = TableViewHeadersVisibility.Column;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerRow = GetHeaderRow(tableView);
			Verify.AreEqual(Visibility.Visible, headerRow.Visibility, "Switching back to Column should show the header row.");
			Verify.IsGreaterThan(headerRow.ActualHeight, 0.0, "The restored header row should take up height again.");
		});
	}

	private static FrameworkElement GetHeaderRow(TableView tableView)
	{
		// TODO Uno: Uno's FindVisualChildByName returns DependencyObject rather than FrameworkElement.
		var headerRow = tableView.FindVisualChildByName("PART_HeaderRow") as FrameworkElement;
		Verify.IsNotNull(headerRow, "PART_HeaderRow should exist once the template has applied.");
		return headerRow;
	}

	private static string GetRenderedHeaderText(TableView tableView, int index)
	{
		var textBlock = GetHeaderPresenter(tableView, index).Content as TextBlock;
		Verify.IsNotNull(textBlock, $"String header {index} should render through a TextBlock.");
		return textBlock.Text;
	}

	private sealed class TestHeaderTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Template { get; set; }

		protected override DataTemplate SelectTemplateCore(object item) => Template;

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Template;
	}
}

// Category 3.5 of the TableView test plan: column visibility.
[TestClass]
public class TableViewColumnVisibilityTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a collapsed column contributes no width to the header or to rows.")]
	public async Task VerifyCollapsedColumnRemovesCellsFromLayout()
	{
		TableView tableView = null;
		double headerHostWidthBefore = 0.0;
		double collapsedColumnWidth = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role", "City" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(GetHeaderCellWidth(tableView, 1), 0.0, "The column should occupy width before it is collapsed.");

			headerHostWidthBefore = GetHeaderHost(tableView).ActualWidth;
			collapsedColumnWidth = tableView.Columns[1].ActualWidth;

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyCellTakesNoWidth(GetHeaderHost(tableView), 1, headerHostWidthBefore, collapsedColumnWidth, "header");

			foreach (var row in GetRealizedRows(tableView))
			{
				// Every row's cells host is the same TableViewCellsPanel type as the header host, so
				// the collapsed column has to drop out of its track the same way. The row's own
				// pre-collapse width is not recorded because a row is arranged to the same total as
				// the header; comparing against the header's total keeps one source of truth.
				VerifyCellTakesNoWidth(GetCellsHost(row), 1, headerHostWidthBefore, collapsedColumnWidth, "row");
			}

			Verify.AreEqual(3, tableView.Columns.Count, "Collapsing must not remove the column from Columns.");
			Verify.IsGreaterThan(GetHeaderCellWidth(tableView, 2), 0.0, "The following column should still occupy width.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a collapsed column keeps its authored state while hidden.")]
	public async Task VerifyCollapsedColumnPreservesColumnState()
	{
		TableView tableView = null;
		TableViewColumn column = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			tableView.Columns[1].Width = new GridLength(180.0, GridUnitType.Pixel);
			tableView.Columns[1].MinWidth = 40.0;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			column = tableView.Columns[1];
			column.Visibility = Visibility.Collapsed;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Collapsing must be a presentation change, not a disguised remove: the authored
			// intent has to survive so showing the column again restores the user's sizing.
			Verify.AreEqual(new GridLength(180.0, GridUnitType.Pixel), column.Width, "Authored Width should survive collapse.");
			Verify.AreEqual(40.0, column.MinWidth, "MinWidth should survive collapse.");
			Verify.AreEqual("Role", column.Header as string, "Header should survive collapse.");
			Verify.AreEqual(column, tableView.Columns[1], "The column should stay at its index while collapsed.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies restoring Visibility brings a collapsed column's header and cells back.")]
	public async Task VerifyRestoringColumnVisibilityRestoresCells()
	{
		// Kept separate from the collapse test: hide and show are different transitions and a
		// regression can break only one of them.
		TableView tableView = null;
		double widthBeforeCollapse = 0.0;
		double headerHostWidthBefore = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role", "City" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			widthBeforeCollapse = GetHeaderCellWidth(tableView, 1);
			headerHostWidthBefore = GetHeaderHost(tableView).ActualWidth;

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyCellTakesNoWidth(GetHeaderHost(tableView), 1, headerHostWidthBefore, widthBeforeCollapse, "header");

			tableView.Columns[1].Visibility = Visibility.Visible;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(widthBeforeCollapse, GetHeaderCellWidth(tableView, 1),
				"Restoring Visibility should restore the column's previous width.");
			Verify.AreEqual(headerHostWidthBefore, GetHeaderHost(tableView).ActualWidth,
				"Restoring Visibility should give the header host its full width back.");

			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.IsGreaterThan(GetCellWidth(row, 1), 0.0, "Restored cells should occupy width again.");
			}

			VerifyEveryRowMatchesHeaders(tableView, "after restoring column visibility");
		});
	}

	// A collapsed column is asserted through the panel's arranged total and its neighbour's offset,
	// never through the collapsed element's own ActualWidth.
	//
	// ActualWidth is RenderSize, and XAML does not run a layout pass on a Collapsed element, so the
	// element keeps the width it last rendered at forever. Reading it reports the pre-collapse width
	// and looks like the control reserved a blank track, which it does not: TableViewCellsPanel
	// measures a non-visible column's cell at zero width (TableViewCellsPanel.cpp:156) and arranges
	// it at w = 0, accumulating only visible widths into the total it returns
	// (TableViewCellsPanel.cpp:264-272). Those two effects are what this checks.
	private static void VerifyCellTakesNoWidth(Panel host, int index, double hostWidthBefore, double columnWidth, string context)
	{
		var cell = (FrameworkElement)host.Children[index];

		Verify.AreEqual(Visibility.Collapsed, cell.Visibility,
			$"A collapsed column's {context} cell should itself be collapsed.");
		Verify.AreEqual(0.0, cell.DesiredSize.Width,
			$"A collapsed column's {context} cell must desire no width.");

		Verify.AreEqual(hostWidthBefore - columnWidth, host.ActualWidth,
			$"The {context} host must give back exactly the collapsed column's width.");

		var next = (FrameworkElement)host.Children[index + 1];
		Verify.AreEqual(cell.ActualOffset.X, next.ActualOffset.X,
			$"The following {context} cell must close up into the collapsed column's slot, leaving no gap.");
	}

	private static double GetHeaderCellWidth(TableView tableView, int index)
		=> ((FrameworkElement)GetHeaderHost(tableView).Children[index]).ActualWidth;

	private static double GetCellWidth(TableViewRow row, int index)
		=> ((FrameworkElement)GetCellsHost(row).Children[index]).ActualWidth;
}

// Category 3.6 of the TableView test plan: the cell element factory and custom columns.
//
// TableView.idl:198-201 states the contract these tests pin: the element returned by
// GenerateElementCore "must bind reactively to its inherited DataContext (the row data item) and
// must not set a local DataContext: rows are recycled and their DataContext is updated in place
// ... baking it in as static content (or a local DataContext) will show stale data after
// recycle."
[TestClass]
public class TableViewCellFactoryTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTextColumn.GenerateElement returns a TextBlock.")]
	public void VerifyTextColumnGenerateElementProducesTextBlock()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewTextColumn { Header = "Name", Binding = CreateBinding("Name") };
			var element = column.GenerateElement(new Person { Name = "Asha", Role = "Designer" });

			Verify.IsNotNull(element, "GenerateElement should produce a cell element.");
			Verify.IsTrue(element is TextBlock, $"The built-in text cell should be a TextBlock, was {element.GetType().Name}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GenerateElement returns a distinct element on every call.")]
	public void VerifyGenerateElementReturnsNewElementPerCall()
	{
		RunOnUIThread.Execute(() =>
		{
			var item = new Person { Name = "Asha", Role = "Designer" };
			var column = new TableViewTextColumn { Header = "Name", Binding = CreateBinding("Name") };

			var first = column.GenerateElement(item);
			var second = column.GenerateElement(item);

			// A FrameworkElement cannot have two parents. If a column cached one element, rows
			// would steal it from each other as they realize.
			Verify.IsFalse(ReferenceEquals(first, second), "Each call must return a new element.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies built-in columns do not set a local DataContext on generated cells.")]
	public void VerifyGenerateElementDoesNotSetLocalDataContext()
	{
		RunOnUIThread.Execute(() =>
		{
			var item = new Person { Name = "Asha", Role = "Designer" };

			var textElement = new TableViewTextColumn { Binding = CreateBinding("Name") }.GenerateElement(item);
			var templateElement = new TableViewTemplateColumn { CellTemplate = CreateTextTemplate("Cell") }.GenerateElement(item);

			// The IDL imposes this rule on third-party columns; the built-ins are its reference
			// implementation and must follow it too.
			Verify.AreEqual(DependencyProperty.UnsetValue, textElement.ReadLocalValue(FrameworkElement.DataContextProperty),
				"A text cell must not carry a local DataContext.");
			Verify.AreEqual(DependencyProperty.UnsetValue, templateElement.ReadLocalValue(FrameworkElement.DataContextProperty),
				"A template cell must not carry a local DataContext.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTemplateColumn.GenerateElement carries CellTemplate rather than falling back to text.")]
	public void VerifyTemplateColumnGenerateElementUsesCellTemplate()
	{
		RunOnUIThread.Execute(() =>
		{
			var cellTemplate = CreateTextTemplate("FromTemplate");
			var column = new TableViewTemplateColumn { CellTemplate = cellTemplate };
			var element = column.GenerateElement(new Person { Name = "Asha", Role = "Designer" });

			Verify.IsNotNull(element, "A template column should produce a cell element.");

			// Asserted on the returned element's ContentTemplate, not on inflated content.
			// GenerateElement is a factory: it hands back a presenter carrying the template, and the
			// content is wired later by TableViewRow when the cell is attached to a row. A presenter
			// that is not in a tree and has no Content has nothing to inflate, so looking for a
			// TextBlock here asserts a stage of the pipeline this method is not responsible for.
			// TableViewCellContentTests.VerifyTemplateCellRendersRowItem covers actual inflation.
			var presenter = element as ContentPresenter;
			Verify.IsNotNull(presenter, "A template column should produce a ContentPresenter.");
			Verify.AreEqual(cellTemplate, presenter.ContentTemplate,
				"The presenter should carry the column's CellTemplate.");

			// The regression the test name is really about: falling back to rendering the item's
			// ToString() in a TextBlock instead of honouring CellTemplate.
			Verify.IsNull(element as TextBlock,
				"A template column must not fall back to a plain text cell when CellTemplate is set.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GenerateElement with a null data item does not throw into row realization.")]
	public void VerifyGenerateElementWithNullDataItemDoesNotCrash()
	{
		RunOnUIThread.Execute(() =>
		{
			// Null items are legal in a List<T> of reference types, so this has to survive.
			// The IDL does not say whether the return may be null, so nothing is asserted about
			// the returned value -- only that the call does not throw.
			new TableViewTextColumn { Binding = CreateBinding("Name") }.GenerateElement(null);
			new TableViewTemplateColumn { CellTemplate = CreateTextTemplate("Cell") }.GenerateElement(null);
			new ProbeColumn().GenerateElement(null);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a C#-derived column's GenerateElementCore override is invoked for realized cells.")]
	public async Task VerifyCustomColumnGenerateElementCoreIsCalled()
	{
		TableView tableView = null;
		ProbeColumn probe = null;

		RunOnUIThread.Execute(() =>
		{
			probe = new ProbeColumn { Header = "Probe" };
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns.Add(probe);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowCount = GetRealizedRows(tableView).Count;
			Verify.IsGreaterThan(rowCount, 0, "At least one row should be realized.");
			Verify.IsGreaterThanOrEqual(probe.GenerateCount, rowCount,
				"GenerateElementCore should be invoked at least once per realized row.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the element a custom column returns is what appears in the row's cell.")]
	public async Task VerifyCustomColumnElementRendersInCell()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name" });
			tableView.Columns.Add(new ProbeColumn { Header = "Probe" });

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "At least one row should be realized.");

			foreach (var row in rows)
			{
				var probeCells = FindVisualChildrenByType<TextBlock>(row)
					.Where(t => t.Name == ProbeColumn.CellName)
					.ToList();

				Verify.AreEqual(1, probeCells.Count, "Each row should host exactly one element from the custom column.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies custom cells bound to the inherited DataContext show the new row's data after recycling.")]
	public async Task VerifyCustomColumnElementUpdatesOnRecycle()
	{
		// This is the contract from TableView.idl:198-201. The failure mode it guards is silent
		// and data-corrupting: cells keep showing a previous row's values after a scroll.
		TableView tableView = null;
		ScrollViewer scroller = null;

		RunOnUIThread.Execute(() =>
		{
			var items = Enumerable.Range(0, 200)
				.Select(i => new Person { Name = $"Person {i:D3}", Role = $"Role {i:D3}" })
				.ToList();

			tableView = CreateTableViewShell(items, 500, 300);
			tableView.Columns.Add(new ProbeColumn { Header = "Probe" });

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyProbeCellsMatchRowData(tableView, "before scrolling");

			scroller = GetBodyScroller(tableView);

			// Far enough that every row realized above has been recycled.
			scroller.ChangeView(null, 2000.0, null, true);
		});

		// A second layout pass and wait. ChangeView returns before the repeater has finished
		// re-assigning DataContext to the containers it recycles, and a row sampled in that window
		// is parented but not yet bound. Without this settle the test reads a transient state and
		// reports it as a product bug.
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(scroller.VerticalOffset, 0.0, "The body should have actually scrolled.");
			VerifyProbeCellsMatchRowData(tableView, "after scrolling");
		});
	}

	// Every realized row's custom cell must show that row's current data item.
	private static void VerifyProbeCellsMatchRowData(TableView tableView, string context)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"At least one row should be realized ({context}).");

		foreach (var row in rows)
		{
			var item = row.DataContext as Person;
			if (item == null)
			{
				// Report and move on rather than dereferencing. TAEF's Verify logs and continues, so
				// falling through to item.Name raises a NullReferenceException that crosses the WinRT
				// boundary as 0xC000027B - a test-host crash with no managed stack, which hides the
				// assertion that actually failed and looks like a product assert.
				Verify.Fail($"A realized row should carry its data item as DataContext ({context}).");
				continue;
			}

			var cell = FindVisualChildrenByType<TextBlock>(row).FirstOrDefault(t => t.Name == ProbeColumn.CellName);
			if (cell == null)
			{
				Verify.Fail($"The custom column's cell should be present ({context}).");
				continue;
			}

			Verify.AreEqual(item.Name, cell.Text, $"The cell should show its own row's data ({context}).");
		}
	}

	private static Binding CreateBinding(string path) => new Binding
	{
		Path = new PropertyPath(path),
		Mode = BindingMode.OneWay,
	};
}
