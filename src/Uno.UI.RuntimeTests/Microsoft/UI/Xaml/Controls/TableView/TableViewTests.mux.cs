// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableViewTests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

using Common;
using System.Threading.Tasks;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
public class TableViewTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies TableView can be constructed and initializes its columns collection.")]
	public void VerifyTableViewConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var tableView = new TableView();

			Verify.IsNotNull(tableView);
			Verify.IsTrue(tableView is Control, "TableView derives from Control.");
			Verify.IsNotNull(tableView.Columns, "Columns is created by the constructor, not on first access of a template part.");
			Verify.AreEqual(0, tableView.Columns.Count);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTextColumn can be constructed.")]
	public void VerifyTextColumnConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var textColumn = new TableViewTextColumn();

			Verify.IsNotNull(textColumn);
			Verify.IsTrue(textColumn is TableViewColumn, "TableViewTextColumn derives from TableViewColumn.");
			Verify.IsNull(textColumn.Binding, "A text column starts unbound.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTemplateColumn can be constructed.")]
	public void VerifyTemplateColumnConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var templateColumn = new TableViewTemplateColumn();

			Verify.IsNotNull(templateColumn);
			Verify.IsTrue(templateColumn is TableViewColumn, "TableViewTemplateColumn derives from TableViewColumn.");
			Verify.IsNull(templateColumn.CellTemplate, "A template column starts without a cell template.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewColumn can be constructed directly for derivation scenarios.")]
	public void VerifyColumnConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewColumn();

			Verify.IsNotNull(column);
			Verify.IsTrue(column is DependencyObject, "TableViewColumn derives from DependencyObject.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewRow can be constructed standalone, outside an owning TableView.")]
	public void VerifyRowConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var row = new TableViewRow();

			Verify.IsNotNull(row);
			Verify.IsTrue(row is Control, "TableViewRow derives from Control.");
			Verify.IsFalse(row.IsSelected, "A row with no owning TableView is not selected.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewCellsPanel can be constructed standalone, outside a row or header host.")]
	public void VerifyCellsPanelConstructs()
	{
		RunOnUIThread.Execute(() =>
		{
			var cellsPanel = new TableViewCellsPanel();

			Verify.IsNotNull(cellsPanel);
			Verify.IsTrue(cellsPanel is Panel, "TableViewCellsPanel derives from Panel.");
			Verify.AreEqual(0, cellsPanel.Children.Count, "A cells panel with no owning column collection hosts no cells.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView default property values.")]
	public void VerifyDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			var tableView = new TableView();

			Log.Comment("Verifying TableView default property values.");
			Verify.IsNull(tableView.ItemsSource);
			Verify.IsNotNull(tableView.Columns);
			Verify.AreEqual(0, tableView.Columns.Count);
			Verify.AreEqual(TableViewHeadersVisibility.Column, tableView.HeadersVisibility);
			Verify.AreEqual(TableViewGridLinesVisibility.All, tableView.GridLinesVisibility);
			Verify.IsTrue(tableView.CanUserResizeColumns);
			Verify.IsNull(tableView.RowBackground);
			Verify.IsNull(tableView.AlternatingRowBackground);
			Verify.IsNull(tableView.EmptyTemplate);
			Verify.IsNull(tableView.GroupHeaderTemplate);
			Verify.AreEqual(TableViewDensity.Standard, tableView.Density);
			Verify.IsTrue(tableView.IsReadOnly);
			Verify.IsFalse(tableView.IsEditing);
			Verify.AreEqual(TableViewSelectionMode.Single, tableView.SelectionMode);
			Verify.IsNull(tableView.SelectedItem);
			Verify.AreEqual(-1, tableView.SelectedIndex);
			Verify.IsTrue(tableView.CanUserSortColumns);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewColumn base class default property values.")]
	public void VerifyColumnDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewColumn();

			Log.Comment("Verifying TableViewColumn default property values.");
			Verify.IsNull(column.Header);
			Verify.IsNull(column.HeaderTemplate);
			Verify.IsNull(column.HeaderTemplateSelector);
			Verify.IsNull(column.HeaderToolTip);
			Verify.AreEqual(120.0, column.Width.Value, "Width defaults to 120 pixels.");
			Verify.AreEqual(GridUnitType.Pixel, column.Width.GridUnitType, "Width defaults to a pixel GridLength, not Auto or Star.");
			Verify.AreEqual(20.0, column.MinWidth);
			Verify.IsTrue(double.IsPositiveInfinity(column.MaxWidth), "MaxWidth defaults to unbounded.");
			Verify.IsTrue(column.CanResize);
			Verify.AreEqual(120.0, column.ActualWidth, "ActualWidth resolves from the default Width before any layout pass.");
			Verify.AreEqual(TableViewFrozenEdge.None, column.FrozenEdge);
			Verify.AreEqual(Visibility.Visible, column.Visibility);
			Verify.IsFalse(column.IsReadOnly, "A column is writable by default; TableView.IsReadOnly is what gates editing.");
			Verify.IsNull(column.CellEditingTemplate);
			Verify.IsTrue(column.CanSort);
			Verify.AreEqual(TableViewSortCycle.AscendingDescending, column.SortCycle);
			Verify.AreEqual(string.Empty, column.SortMemberPath);
			Verify.IsNull(column.CustomSortComparer);
			Verify.AreEqual(SortDirection.None, column.SortDirection);
			Verify.IsNull(column.CellToolTipBinding, "CellToolTipBinding is a CLR property and defaults to null, meaning no cell tooltip.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTextColumn default property values.")]
	public void VerifyTextColumnDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			var textColumn = new TableViewTextColumn();

			Log.Comment("Verifying TableViewTextColumn-specific default property values.");
			Verify.IsNull(textColumn.Binding, "Binding is a CLR property so XAML passes the Binding through unevaluated; it starts null.");
			Verify.IsNull(textColumn.CellToolTipBinding);

			Log.Comment("Verifying TableViewTextColumn inherits the TableViewColumn defaults.");
			Verify.IsNull(textColumn.Header);
			Verify.AreEqual(120.0, textColumn.Width.Value);
			Verify.AreEqual(20.0, textColumn.MinWidth);
			Verify.AreEqual(120.0, textColumn.ActualWidth);
			Verify.IsTrue(textColumn.CanSort);
			Verify.AreEqual(SortDirection.None, textColumn.SortDirection);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewTemplateColumn default property values.")]
	public void VerifyTemplateColumnDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			var templateColumn = new TableViewTemplateColumn();

			Log.Comment("Verifying TableViewTemplateColumn-specific default property values.");
			Verify.IsNull(templateColumn.CellTemplate, "A template column with no CellTemplate renders an empty cell.");
			Verify.IsNull(templateColumn.CellEditingTemplate, "A template column with no CellEditingTemplate is not editable.");

			Log.Comment("Verifying TableViewTemplateColumn inherits the TableViewColumn defaults.");
			Verify.IsNull(templateColumn.Header);
			Verify.AreEqual(120.0, templateColumn.Width.Value);
			Verify.AreEqual(20.0, templateColumn.MinWidth);
			Verify.AreEqual(120.0, templateColumn.ActualWidth);
			Verify.AreEqual(Visibility.Visible, templateColumn.Visibility);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewRow default property values outside an owning TableView.")]
	public void VerifyRowDefaultPropertyValues()
	{
		RunOnUIThread.Execute(() =>
		{
			var row = new TableViewRow();

			Log.Comment("Verifying TableViewRow default property values.");
			Verify.IsFalse(row.IsSelected, "A row with no owning TableView is never selected.");
			Verify.AreEqual(false, row.GetValue(TableViewRow.IsSelectedProperty),
				"IsSelected reads through IsSelectedProperty.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView dependency property statics.")]
	public void VerifyDependencyProperties()
	{
		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Verifying TableView dependency properties.");
			Verify.IsNotNull(TableView.ItemsSourceProperty);
			Verify.IsNotNull(TableView.ColumnsProperty);
			Verify.IsNotNull(TableView.HeadersVisibilityProperty);
			Verify.IsNotNull(TableView.GridLinesVisibilityProperty);
			Verify.IsNotNull(TableView.CanUserResizeColumnsProperty);
			Verify.IsNotNull(TableView.RowBackgroundProperty);
			Verify.IsNotNull(TableView.AlternatingRowBackgroundProperty);
			Verify.IsNotNull(TableView.EmptyTemplateProperty);
			Verify.IsNotNull(TableView.GroupHeaderTemplateProperty);
			Verify.IsNotNull(TableView.DensityProperty);
			Verify.IsNotNull(TableView.IsReadOnlyProperty);
			Verify.IsNotNull(TableView.SelectionModeProperty);
			Verify.IsNotNull(TableView.SelectedItemProperty);
			Verify.IsNotNull(TableView.SelectedIndexProperty);
			Verify.IsNotNull(TableView.CanUserSortColumnsProperty);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewColumn, row, and group header dependency property statics.")]
	public void VerifyColumnDependencyProperties()
	{
		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Verifying TableViewColumn dependency properties.");
			Verify.IsNotNull(TableViewColumn.HeaderProperty);
			Verify.IsNotNull(TableViewColumn.HeaderTemplateProperty);
			Verify.IsNotNull(TableViewColumn.HeaderTemplateSelectorProperty);
			Verify.IsNotNull(TableViewColumn.HeaderToolTipProperty);
			Verify.IsNotNull(TableViewColumn.WidthProperty);
			Verify.IsNotNull(TableViewColumn.MinWidthProperty);
			Verify.IsNotNull(TableViewColumn.MaxWidthProperty);
			Verify.IsNotNull(TableViewColumn.CanResizeProperty);
			Verify.IsNotNull(TableViewColumn.ActualWidthProperty);
			Verify.IsNotNull(TableViewColumn.FrozenEdgeProperty);
			Verify.IsNotNull(TableViewColumn.VisibilityProperty);
			Verify.IsNotNull(TableViewColumn.IsReadOnlyProperty);
			Verify.IsNotNull(TableViewColumn.CellEditingTemplateProperty);
			Verify.IsNotNull(TableViewColumn.CanSortProperty);
			Verify.IsNotNull(TableViewColumn.SortCycleProperty);
			Verify.IsNotNull(TableViewColumn.SortMemberPathProperty);
			Verify.IsNotNull(TableViewColumn.SortDirectionProperty);

			Log.Comment("Verifying related dependency properties.");
			Verify.IsNotNull(TableViewTemplateColumn.CellTemplateProperty);
			Verify.IsNotNull(TableViewRow.IsSelectedProperty);
			Verify.IsNotNull(TableViewGroupHeader.IsExpandedProperty);
			Verify.IsNotNull(TableViewGroupHeader.IsExpandableProperty);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies public properties are genuinely backed by their dependency properties in both directions.")]
	public void VerifyDependencyPropertyBacking()
	{
		RunOnUIThread.Execute(() =>
		{
			var tableView = new TableView();

			Log.Comment("Verifying settable TableView dependency properties.");
			VerifySettableDependencyProperty(tableView, TableView.ItemsSourceProperty, "TableView.ItemsSource",
				new List<Person>(), new List<Person>(),
				() => tableView.ItemsSource, value => tableView.ItemsSource = value);
			VerifySettableDependencyProperty(tableView, TableView.HeadersVisibilityProperty, "TableView.HeadersVisibility",
				TableViewHeadersVisibility.None, TableViewHeadersVisibility.Column,
				() => tableView.HeadersVisibility, value => tableView.HeadersVisibility = (TableViewHeadersVisibility)value);
			VerifySettableDependencyProperty(tableView, TableView.GridLinesVisibilityProperty, "TableView.GridLinesVisibility",
				TableViewGridLinesVisibility.None, TableViewGridLinesVisibility.Horizontal,
				() => tableView.GridLinesVisibility, value => tableView.GridLinesVisibility = (TableViewGridLinesVisibility)value);
			VerifySettableDependencyProperty(tableView, TableView.CanUserResizeColumnsProperty, "TableView.CanUserResizeColumns",
				false, true,
				() => tableView.CanUserResizeColumns, value => tableView.CanUserResizeColumns = (bool)value);
			VerifySettableDependencyProperty(tableView, TableView.RowBackgroundProperty, "TableView.RowBackground",
				new SolidColorBrush(Microsoft.UI.Colors.Red), new SolidColorBrush(Microsoft.UI.Colors.Blue),
				() => tableView.RowBackground, value => tableView.RowBackground = (Brush)value);
			VerifySettableDependencyProperty(tableView, TableView.AlternatingRowBackgroundProperty, "TableView.AlternatingRowBackground",
				new SolidColorBrush(Microsoft.UI.Colors.Green), new SolidColorBrush(Microsoft.UI.Colors.Yellow),
				() => tableView.AlternatingRowBackground, value => tableView.AlternatingRowBackground = (Brush)value);
			VerifySettableDependencyProperty(tableView, TableView.EmptyTemplateProperty, "TableView.EmptyTemplate",
				CreateTextTemplate("Empty A"), CreateTextTemplate("Empty B"),
				() => tableView.EmptyTemplate, value => tableView.EmptyTemplate = (DataTemplate)value);
			VerifySettableDependencyProperty(tableView, TableView.GroupHeaderTemplateProperty, "TableView.GroupHeaderTemplate",
				CreateTextTemplate("Group A"), CreateTextTemplate("Group B"),
				() => tableView.GroupHeaderTemplate, value => tableView.GroupHeaderTemplate = (DataTemplate)value);
			VerifySettableDependencyProperty(tableView, TableView.DensityProperty, "TableView.Density",
				TableViewDensity.Compact, TableViewDensity.Comfortable,
				() => tableView.Density, value => tableView.Density = (TableViewDensity)value);
			VerifySettableDependencyProperty(tableView, TableView.IsReadOnlyProperty, "TableView.IsReadOnly",
				false, true,
				() => tableView.IsReadOnly, value => tableView.IsReadOnly = (bool)value);
			VerifySettableDependencyProperty(tableView, TableView.SelectionModeProperty, "TableView.SelectionMode",
				TableViewSelectionMode.None, TableViewSelectionMode.Single,
				() => tableView.SelectionMode, value => tableView.SelectionMode = (TableViewSelectionMode)value);
			VerifySettableDependencyProperty(tableView, TableView.CanUserSortColumnsProperty, "TableView.CanUserSortColumns",
				false, true,
				() => tableView.CanUserSortColumns, value => tableView.CanUserSortColumns = (bool)value);

			Log.Comment("Verifying read-only TableView dependency properties read through their dependency property.");
			var freshTableView = new TableView();
			VerifyReadOnlyDependencyProperty(freshTableView, TableView.SelectedItemProperty, "TableView.SelectedItem",
				null, () => freshTableView.SelectedItem);
			VerifyReadOnlyDependencyProperty(freshTableView, TableView.SelectedIndexProperty, "TableView.SelectedIndex",
				-1, () => freshTableView.SelectedIndex);

			Log.Comment("Verifying TableView.Columns is the collection exposed by its dependency property.");
			freshTableView.Columns.Add(new TableViewTextColumn { Header = "Name" });
			var columnsFromDependencyProperty = freshTableView.GetValue(TableView.ColumnsProperty) as IList<TableViewColumn>;
			Verify.IsNotNull(columnsFromDependencyProperty, "TableView.Columns: ColumnsProperty should hold the column collection.");
			Verify.AreEqual(1, columnsFromDependencyProperty.Count,
				"TableView.Columns: a column added through the CLR property should be visible through the dependency property.");

		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column, row, and group header properties are genuinely backed by their dependency properties in both directions.")]
	public void VerifyColumnDependencyPropertyBacking()
	{
		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Verifying settable TableViewColumn dependency properties.");
			var column = new TableViewTextColumn();
			VerifySettableDependencyProperty(column, TableViewColumn.HeaderProperty, "TableViewColumn.Header",
				"Header A", "Header B",
				() => column.Header, value => column.Header = value);
			VerifySettableDependencyProperty(column, TableViewColumn.HeaderTemplateProperty, "TableViewColumn.HeaderTemplate",
				CreateTextTemplate("Header A"), CreateTextTemplate("Header B"),
				() => column.HeaderTemplate, value => column.HeaderTemplate = (DataTemplate)value);
			VerifySettableDependencyProperty(column, TableViewColumn.HeaderTemplateSelectorProperty, "TableViewColumn.HeaderTemplateSelector",
				new TestTemplateSelector(), new TestTemplateSelector(),
				() => column.HeaderTemplateSelector, value => column.HeaderTemplateSelector = (DataTemplateSelector)value);
			VerifySettableDependencyProperty(column, TableViewColumn.HeaderToolTipProperty, "TableViewColumn.HeaderToolTip",
				"ToolTip A", "ToolTip B",
				() => column.HeaderToolTip, value => column.HeaderToolTip = value);
			VerifySettableDependencyProperty(column, TableViewColumn.WidthProperty, "TableViewColumn.Width",
				new GridLength(200), new GridLength(300),
				() => column.Width, value => column.Width = (GridLength)value);
			VerifySettableDependencyProperty(column, TableViewColumn.MinWidthProperty, "TableViewColumn.MinWidth",
				30.0, 40.0,
				() => column.MinWidth, value => column.MinWidth = (double)value);
			VerifySettableDependencyProperty(column, TableViewColumn.MaxWidthProperty, "TableViewColumn.MaxWidth",
				400.0, 500.0,
				() => column.MaxWidth, value => column.MaxWidth = (double)value);
			VerifySettableDependencyProperty(column, TableViewColumn.CanResizeProperty, "TableViewColumn.CanResize",
				false, true,
				() => column.CanResize, value => column.CanResize = (bool)value);
			VerifySettableDependencyProperty(column, TableViewColumn.FrozenEdgeProperty, "TableViewColumn.FrozenEdge",
				TableViewFrozenEdge.Leading, TableViewFrozenEdge.None,
				() => column.FrozenEdge, value => column.FrozenEdge = (TableViewFrozenEdge)value);
			VerifySettableDependencyProperty(column, TableViewColumn.VisibilityProperty, "TableViewColumn.Visibility",
				Visibility.Collapsed, Visibility.Visible,
				() => column.Visibility, value => column.Visibility = (Visibility)value);
			VerifySettableDependencyProperty(column, TableViewColumn.IsReadOnlyProperty, "TableViewColumn.IsReadOnly",
				true, false,
				() => column.IsReadOnly, value => column.IsReadOnly = (bool)value);
			VerifySettableDependencyProperty(column, TableViewColumn.CellEditingTemplateProperty, "TableViewColumn.CellEditingTemplate",
				CreateTextTemplate("Editor A"), CreateTextTemplate("Editor B"),
				() => column.CellEditingTemplate, value => column.CellEditingTemplate = (DataTemplate)value);
			VerifySettableDependencyProperty(column, TableViewColumn.CanSortProperty, "TableViewColumn.CanSort",
				false, true,
				() => column.CanSort, value => column.CanSort = (bool)value);
			VerifySettableDependencyProperty(column, TableViewColumn.SortCycleProperty, "TableViewColumn.SortCycle",
				TableViewSortCycle.DescendingAscending, TableViewSortCycle.AscendingDescendingNone,
				() => column.SortCycle, value => column.SortCycle = (TableViewSortCycle)value);
			VerifySettableDependencyProperty(column, TableViewColumn.SortMemberPathProperty, "TableViewColumn.SortMemberPath",
				"Name", "Role",
				() => column.SortMemberPath, value => column.SortMemberPath = (string)value);

			Log.Comment("Verifying read-only TableViewColumn dependency properties read through their dependency property.");
			var freshColumn = new TableViewTextColumn();
			VerifyReadOnlyDependencyProperty(freshColumn, TableViewColumn.ActualWidthProperty, "TableViewColumn.ActualWidth",
				120.0, () => freshColumn.ActualWidth);
			VerifyReadOnlyDependencyProperty(freshColumn, TableViewColumn.SortDirectionProperty, "TableViewColumn.SortDirection",
				SortDirection.None, () => freshColumn.SortDirection);

			Log.Comment("Verifying remaining Tabular dependency properties.");
			var templateColumn = new TableViewTemplateColumn();
			VerifySettableDependencyProperty(templateColumn, TableViewTemplateColumn.CellTemplateProperty, "TableViewTemplateColumn.CellTemplate",
				CreateTextTemplate("Cell A"), CreateTextTemplate("Cell B"),
				() => templateColumn.CellTemplate, value => templateColumn.CellTemplate = (DataTemplate)value);

			var groupHeader = new TableViewGroupHeader();
			VerifySettableDependencyProperty(groupHeader, TableViewGroupHeader.IsExpandedProperty, "TableViewGroupHeader.IsExpanded",
				false, true,
				() => groupHeader.IsExpanded, value => groupHeader.IsExpanded = (bool)value);
			VerifySettableDependencyProperty(groupHeader, TableViewGroupHeader.IsExpandableProperty, "TableViewGroupHeader.IsExpandable",
				false, true,
				() => groupHeader.IsExpandable, value => groupHeader.IsExpandable = (bool)value);

			var row = new TableViewRow();
			VerifyReadOnlyDependencyProperty(row, TableViewRow.IsSelectedProperty, "TableViewRow.IsSelected",
				false, () => row.IsSelected);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView can be activated from XAML with inline columns, honouring the [contentproperty] declarations on TableView and TableViewColumn.")]
	public async Task VerifyXamlActivationWithInlineColumns()
	{
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			// TableView declares [contentproperty("Columns")], so the columns need no <TableView.Columns>
			// wrapper. TableViewColumn declares [contentproperty("Header")], so the second column sets its
			// header through child content rather than the attribute.
			var tableView = (TableView)XamlReader.Load(
				@"<tabular:TableView
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					xmlns:tabular='using:Microsoft.UI.Xaml.Controls.Tabular'>
					<tabular:TableViewTextColumn Header='Name' />
					<tabular:TableViewTextColumn>Role</tabular:TableViewTextColumn>
				</tabular:TableView>");

			Verify.IsNotNull(tableView);
			Verify.AreEqual(2, tableView.Columns.Count, "Child elements should land in Columns with no property-element wrapper.");
			Verify.AreEqual("Name", tableView.Columns[0].Header as string);
			Verify.AreEqual("Role", tableView.Columns[1].Header as string, "Child content of a column should be assigned to Header.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a TableView with no columns declared in XAML activates and loads.")]
	public async Task VerifyXamlActivationWithoutColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			tableView = (TableView)XamlReader.Load(
				@"<tabular:TableView
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:tabular='using:Microsoft.UI.Xaml.Controls.Tabular'
					Width='400' Height='300' />");

			Verify.IsNotNull(tableView);
			Verify.IsNotNull(tableView.Columns, "Columns is created even when XAML declares no children.");
			Verify.AreEqual(0, tableView.Columns.Count);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"),
				"A column-less TableView should still apply its template.");
			Verify.AreEqual(0, tableView.Columns.Count);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a TableViewTemplateColumn with an inline CellTemplate activates from XAML.")]
	public async Task VerifyXamlActivationWithTemplateColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			tableView = (TableView)XamlReader.Load(
				@"<tabular:TableView
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:tabular='using:Microsoft.UI.Xaml.Controls.Tabular'
					Width='400' Height='300'>
					<tabular:TableViewTemplateColumn Header='Name'>
						<tabular:TableViewTemplateColumn.CellTemplate>
							<DataTemplate>
								<TextBlock Text='{Binding Name}' />
							</DataTemplate>
						</tabular:TableViewTemplateColumn.CellTemplate>
					</tabular:TableViewTemplateColumn>
				</tabular:TableView>");

			Verify.AreEqual(1, tableView.Columns.Count);

			var templateColumn = tableView.Columns[0] as TableViewTemplateColumn;
			Verify.IsNotNull(templateColumn, "The parsed column should be a TableViewTemplateColumn.");
			Verify.AreEqual("Name", templateColumn.Header as string);
			Verify.IsNotNull(templateColumn.CellTemplate, "The inline CellTemplate should survive parsing.");

			tableView.ItemsSource = new List<Person> { new Person { Name = "Ada" } };

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var templateColumn = (TableViewTemplateColumn)tableView.Columns[0];
			Verify.IsNotNull(templateColumn.CellTemplate);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView survives being unloaded, reloaded, and reparented, and restores its rows and selection.")]
	public async Task ValidateLoadUnload()
	{
		TableView tableView = null;
		Grid firstHost = null;
		Grid secondHost = null;
		int loadCount = 0;
		int unloadCount = 0;
		bool unorderedLoadEvent = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			tableView.Loaded += delegate { unorderedLoadEvent |= (++loadCount > unloadCount + 1); };
			tableView.Unloaded += delegate { unorderedLoadEvent |= (++unloadCount > loadCount); };

			firstHost = new Grid { Name = "FirstHost" };
			secondHost = new Grid { Name = "SecondHost" };

			firstHost.Children.Add(tableView);

			var root = new Grid();
			root.Children.Add(firstHost);
			root.Children.Add(secondHost);
			LoadContent(root);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"), "The control should be templated after the first load.");
			tableView.Select(1);
			Verify.AreEqual(1, tableView.SelectedIndex);
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Unloading. OnTableViewUnloaded drains the repeater source and stashes the selection for reload.");
		RunOnUIThread.Execute(() =>
		{
			firstHost.Children.Remove(tableView);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Reloading into the same host.");
		RunOnUIThread.Execute(() =>
		{
			firstHost.Children.Add(tableView);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"), "The control should still be templated after a reload.");
			Verify.AreEqual(2, tableView.Columns.Count, "Columns should survive an unload/reload cycle.");
			Verify.AreEqual(1, tableView.SelectedIndex,
				"OnTableViewUnloaded stashes the selection so the reload re-selects it instead of dropping it.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Reparenting to a different host.");

		RunOnUIThread.Execute(() =>
		{
			firstHost.Children.Remove(tableView);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			secondHost.Children.Add(tableView);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"), "The control should be usable under a new parent.");
			Verify.AreEqual(1, tableView.SelectedIndex, "Selection should survive a reparent.");

			tableView.Select(0);
			Verify.AreEqual(0, tableView.SelectedIndex, "The control should still respond to API calls after reparenting.");

			Verify.IsFalse(unorderedLoadEvent, "Loaded and Unloaded should alternate, never nest.");
			Verify.AreEqual(3, loadCount, "Expected one load for the initial add, one for the reload, and one for the reparent.");
			Verify.AreEqual(2, unloadCount, "Expected one unload for the removal and one for the reparent.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies repeated load/unload cycles do not accumulate subscriptions on the items source.")]
	public async Task VerifyRepeatedLoadUnloadDoesNotLeakSubscriptions()
	{
		TableView tableView = null;
		Grid host = null;
		CountingItemsSource itemsSource = null;
		int subscriberCountAfterFirstLoad = 0;

		RunOnUIThread.Execute(() =>
		{
			itemsSource = new CountingItemsSource(new List<Person>
			{
				new Person { Name = "Asha", Role = "Designer" },
				new Person { Name = "Diego", Role = "Engineer" },
			});

			tableView = CreateTableView(itemsSource, bound: false);

			host = new Grid();
			host.Children.Add(tableView);
			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			subscriberCountAfterFirstLoad = itemsSource.SubscriberCount;
			Log.Comment($"Subscriber count after the first load: {subscriberCountAfterFirstLoad}.");
			Verify.IsLessThanOrEqual(subscriberCountAfterFirstLoad, 1,
				"At most one ItemsSourceView should be listening to the source at a time.");
		});

		for (int i = 0; i < 5; i++)
		{
			int cycle = i;

			RunOnUIThread.Execute(() => host.Children.Remove(tableView));
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsLessThanOrEqual(itemsSource.SubscriberCount, 1,
					$"Subscriber count should never exceed one while unloaded (cycle {cycle}).");
				Verify.IsGreaterThanOrEqual(itemsSource.SubscriberCount, 0,
					$"Subscriber count should never go negative (cycle {cycle}).");
			});

			RunOnUIThread.Execute(() =>
			{
				host.Children.Add(tableView);
				Content.UpdateLayout();
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsLessThanOrEqual(itemsSource.SubscriberCount, 1,
					$"Subscriber count should never exceed one while loaded (cycle {cycle}).");
			});
		}

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Subscriber count after five load/unload cycles: {itemsSource.SubscriberCount}.");
			Verify.AreEqual(subscriberCountAfterFirstLoad, itemsSource.SubscriberCount,
				"Repeated load/unload cycles should not accumulate subscriptions on the items source.");
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"),
				"The control should still be usable after repeated cycles.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies unloading immediately after assigning ItemsSource, before layout settles, does not crash.")]
	public async Task VerifyUnloadWhileItemsSourcePendingDoesNotCrash()
	{
		// Create, load, and immediately drop several TableView instances. Tearing down while the
		// rows pipeline and repeater cache work may still be pending exercises OnTableViewUnloaded
		// against a control that never finished its first layout.
		for (int i = 0; i < 5; i++)
		{
			RunOnUIThread.Execute(() =>
			{
				var tableView = CreateTableView(new List<Person>
				{
					new Person { Name = "Asha", Role = "Designer" },
					new Person { Name = "Diego", Role = "Engineer" },
				}, bound: false);

				LoadContent(tableView);

				// Remove from the tree while the rows pipeline may still have queued work.
				Content = null;
			});

			await TestServices.WindowHelper.WaitForIdle();

			// Force collection so any callback that fires afterwards must observe a dropped weak
			// reference rather than a partially torn-down control.
			RunOnUIThread.Execute(() =>
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			});

			await TestServices.WindowHelper.WaitForIdle();
		}

		Log.Comment("Repeated create/load/unload/collect cycles completed without crashing.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView applies its default template and creates required template parts.")]
	public async Task VerifyTemplatePartsAfterTemplateApplication()
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
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_HeaderRow"));
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_HeaderHost"));
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_BodyScroller"));
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_BodyContent"));
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_RowsRepeater"));
			Verify.IsNotNull(tableView.FindVisualChildByName("PART_EmptyStatePresenter"));
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView shows EmptyTemplate for an empty ItemsSource.")]
	public async Task VerifyEmptyTemplateShowsForEmptyItemsSource()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(new List<Person>(), bound: false, emptyTemplate: CreateTextTemplate("No rows"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "empty ItemsSource");
			VerifyEmptyStateText(tableView, "No rows");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a null ItemsSource shows EmptyTemplate, matching the empty-collection case.")]
	public async Task VerifyNullItemsSourceShowsEmptyTemplate()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// UpdateEmptyState defaults isEmpty to true when there is no ItemsSourceView at all,
			// so a null source must reach the same state as an empty collection.
			tableView = CreateTableView(null, bound: false, emptyTemplate: CreateTextTemplate("No rows"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "null ItemsSource");
			VerifyEmptyStateText(tableView, "No rows");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a non-empty ItemsSource collapses the empty presenter and shows the rows repeater.")]
	public async Task VerifyEmptyTemplateHiddenWhenItemsPresent()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			tableView.EmptyTemplate = CreateTextTemplate("No rows");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "items present");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing the last item switches an already-loaded TableView to the empty presenter.")]
	public async Task VerifyEmptyTemplateAppearsWhenSourceBecomesEmpty()
	{
		TableView tableView = null;
		ObservableCollection<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<Person> { new Person { Name = "Asha", Role = "Designer" } };

			tableView = CreateTableView(items, bound: false, emptyTemplate: CreateTextTemplate("No rows"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "before removal");

			// Drives OnEmptyStateItemsSourceCollectionChanged, which is subscribed only while
			// EmptyTemplate is non-null.
			items.RemoveAt(0);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "after removing the last item");
			VerifyEmptyStateText(tableView, "No rows");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies adding an item to an empty observable source switches back from the empty presenter to rows.")]
	public async Task VerifyEmptyTemplateDisappearsWhenItemAdded()
	{
		TableView tableView = null;
		ObservableCollection<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<Person>();

			tableView = CreateTableView(items, bound: false, emptyTemplate: CreateTextTemplate("No rows"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "before the add");

			items.Add(new Person { Name = "Asha", Role = "Designer" });
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "after the add");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies assigning EmptyTemplate after load, while the source is already empty, shows it immediately.")]
	public async Task VerifySettingEmptyTemplateWhileEmptyShowsIt()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// No EmptyTemplate yet: the control loads empty but shows nothing.
			tableView = CreateTableView(new List<Person>(), bound: false);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "no EmptyTemplate set");

			// Exercises OnEmptyTemplatePropertyChanged, which both rewires the collection-changed
			// subscription and re-evaluates the empty state.
			tableView.EmptyTemplate = CreateTextTemplate("No rows");
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "EmptyTemplate assigned after load");
			VerifyEmptyStateText(tableView, "No rows");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies clearing EmptyTemplate while empty collapses the presenter, clears its ContentTemplate, and restores the repeater.")]
	public async Task VerifyClearingEmptyTemplateRestoresRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(new List<Person>(), bound: false, emptyTemplate: CreateTextTemplate("No rows"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: true, "EmptyTemplate set");

			tableView.EmptyTemplate = null;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "EmptyTemplate cleared");

			// The opt-out branch clears ContentTemplate as well, so the old template cannot be
			// left inflated behind a collapsed presenter.
			var presenter = (ContentControl)tableView.FindVisualChildByName("PART_EmptyStatePresenter");
			Verify.IsNull(presenter.ContentTemplate, "Clearing EmptyTemplate should null the presenter's ContentTemplate.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a null ItemsSource with no EmptyTemplate shows no empty presenter and does not crash.")]
	public async Task VerifyNullItemsSourceWithoutEmptyTemplateShowsNothing()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(null, bound: false);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(tableView.EmptyTemplate, "EmptyTemplate should default to null.");
			VerifyEmptyStateVisibility(tableView, expectEmptyStateVisible: false, "null source, no EmptyTemplate");

			var presenter = (ContentControl)tableView.FindVisualChildByName("PART_EmptyStatePresenter");
			Verify.IsNull(presenter.ContentTemplate, "No EmptyTemplate means the presenter never gets a ContentTemplate.");
		});
	}

	// The empty-state contract in TableView::UpdateEmptyState is two-sided: whenever the empty
	// presenter is visible the rows repeater is collapsed, and vice versa. Asserting only one
	// side would pass while the control showed both surfaces at once.
	private static void VerifyEmptyStateVisibility(TableView tableView, bool expectEmptyStateVisible, string context)
	{
		var presenter = tableView.FindVisualChildByName("PART_EmptyStatePresenter") as ContentControl;
		Verify.IsNotNull(presenter, $"PART_EmptyStatePresenter should exist ({context}).");

		var repeater = tableView.FindVisualChildByName("PART_RowsRepeater") as FrameworkElement;
		Verify.IsNotNull(repeater, $"PART_RowsRepeater should exist ({context}).");

		Verify.AreEqual(
			expectEmptyStateVisible ? Visibility.Visible : Visibility.Collapsed,
			presenter.Visibility,
			$"Empty-state presenter visibility ({context}).");

		Verify.AreEqual(
			expectEmptyStateVisible ? Visibility.Collapsed : Visibility.Visible,
			repeater.Visibility,
			$"Rows repeater visibility ({context}).");
	}

	private static void VerifyEmptyStateText(TableView tableView, string expectedText)
	{
		var presenter = (ContentControl)tableView.FindVisualChildByName("PART_EmptyStatePresenter");
		Verify.IsNotNull(presenter.ContentTemplate, "The empty presenter should be carrying the EmptyTemplate.");

		var emptyText = presenter.FindVisualChildByType<TextBlock>();
		Verify.IsNotNull(emptyText, "EmptyTemplate should have inflated into the presenter.");
		Verify.AreEqual(expectedText, emptyText.Text);
	}

	private static void VerifySettableDependencyProperty(
		DependencyObject target,
		DependencyProperty property,
		string name,
		object valueWrittenThroughDependencyProperty,
		object valueWrittenThroughClrProperty,
		Func<object> clrGetter,
		Action<object> clrSetter)
	{
		target.SetValue(property, valueWrittenThroughDependencyProperty);
		Verify.AreEqual(valueWrittenThroughDependencyProperty, clrGetter(),
			$"{name}: a value written with SetValue should be readable from the CLR property.");

		clrSetter(valueWrittenThroughClrProperty);
		Verify.AreEqual(valueWrittenThroughClrProperty, target.GetValue(property),
			$"{name}: a value written through the CLR property should be readable with GetValue.");
	}

	private static void VerifyReadOnlyDependencyProperty(
		DependencyObject target,
		DependencyProperty property,
		string name,
		object expectedValue,
		Func<object> clrGetter)
	{
		Verify.AreEqual(expectedValue, clrGetter(), $"{name}: unexpected CLR property value.");
		Verify.AreEqual(expectedValue, target.GetValue(property),
			$"{name}: the CLR property should read through the dependency property.");
	}

	private sealed class TestTemplateSelector : DataTemplateSelector
	{
	}

	// Instrumented ItemsSource that exposes how many listeners are attached to its
	// CollectionChanged event. XAML wraps the source in an ItemsSourceView, which subscribes on
	// construction and unsubscribes on disposal, so the count tracks live ItemsSourceView
	// instances: growth across load/unload cycles means a leaked view.
	private sealed class CountingItemsSource : IList, INotifyCollectionChanged
	{
		private readonly IList _inner;
		private NotifyCollectionChangedEventHandler _collectionChanged;

		public CountingItemsSource(IList inner)
		{
			_inner = inner;
		}

		public int SubscriberCount { get; private set; }

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add
			{
				_collectionChanged += value;
				SubscriberCount++;
			}

			remove
			{
				_collectionChanged -= value;
				SubscriberCount--;
			}
		}

		public int Count => _inner.Count;

		public bool IsFixedSize => _inner.IsFixedSize;

		public bool IsReadOnly => _inner.IsReadOnly;

		public bool IsSynchronized => _inner.IsSynchronized;

		public object SyncRoot => _inner.SyncRoot;

		public object this[int index]
		{
			get => _inner[index];
			set
			{
				var oldItem = _inner[index];
				_inner[index] = value;
				_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace, value, oldItem, index));
			}
		}

		public int Add(object value)
		{
			var index = _inner.Add(value);
			_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Add, value, index));
			return index;
		}

		public void Clear()
		{
			_inner.Clear();
			_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public bool Contains(object value) => _inner.Contains(value);

		public void CopyTo(Array array, int index) => _inner.CopyTo(array, index);

		public IEnumerator GetEnumerator() => _inner.GetEnumerator();

		public int IndexOf(object value) => _inner.IndexOf(value);

		public void Insert(int index, object value)
		{
			_inner.Insert(index, value);
			_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Add, value, index));
		}

		public void Remove(object value)
		{
			var index = _inner.IndexOf(value);
			if (index >= 0)
			{
				RemoveAt(index);
			}
		}

		public void RemoveAt(int index)
		{
			var oldItem = _inner[index];
			_inner.RemoveAt(index);
			_collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Remove, oldItem, index));
		}
	}
}
