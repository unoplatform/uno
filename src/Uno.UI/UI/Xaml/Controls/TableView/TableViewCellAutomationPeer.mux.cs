// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;

using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewCellAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewCellAutomationPeer"/> class.
	/// </summary>
	/// <param name="cell">The realized cell element.</param>
	/// <param name="row">The row that hosts the cell.</param>
	/// <param name="column">The column that owns the cell.</param>
	/// <param name="columnIndex">The visible column index of the cell.</param>
	public TableViewCellAutomationPeer(
		FrameworkElement cell,
		TableViewRow? row,
		TableViewColumn? column,
		int columnIndex)
		: base(cell)
	{
		m_columnIndex = columnIndex;

		// The cell owner supplies bounds; weak refs avoid extending row/column lifetimes.
		if (row is not null)
		{
			m_row = new WeakReference<TableViewRow>(row);
		}
		if (column is not null)
		{
			m_column = new WeakReference<TableViewColumn>(column);
		}
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		// GridItem + TableItem are structural and always meaningful for realized cells.
		if (patternInterface == PatternInterface.GridItem ||
			patternInterface == PatternInterface.TableItem)
		{
			return this;
		}

		// Offered only where SetValue can honour it: the cell must be editable and its column must
		// produce a TextBox editor. Advertising it elsewhere tells assistive technology it can set a
		// value, then fails after opening an edit.
		if (patternInterface == PatternInterface.Value && SupportsValuePattern())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
	{
		// The cell is a Border, so report a stable TableView cell class name.
		return "TableViewCell";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// DataItem lets Narrator read the composed cell name instead of a generic container.
		return AutomationControlType.DataItem;
	}

	protected override string GetNameCore()
	{
		// Compose "{column header}, {cell value}", falling back to either part alone.
		var headerText = GetColumnHeaderText();
		var valueText = GetCellValueText();

		if (string.IsNullOrEmpty(headerText))
		{
			return valueText;
		}
		if (string.IsNullOrEmpty(valueText))
		{
			return headerText;
		}

		string composed = headerText + ", " + valueText;
		return composed;
	}

	private string GetColumnHeaderText()
	{
		// Non-string headers have no simple textual prefix, so let the value stand alone.
		if (TryGetColumnHeaderString(m_column.Get()) is { } headerString)
		{
			return headerString;
		}

		return string.Empty;
	}

	private string GetCellValueText()
	{
		var cell = Owner as FrameworkElement;
		if (cell is null)
		{
			return string.Empty;
		}

		// The cell wrapper's child is the column-generated content.
		FrameworkElement? content = null;
		if (cell is Border border)
		{
			content = border.Child as FrameworkElement;
		}
		if (content is null)
		{
			content = cell;
		}

		// Common text-column case: read the generated TextBlock.
		if (content is TextBlock textBlock)
		{
			return textBlock.Text;
		}

		// Template content uses the standard UIA name computation.
		if (FrameworkElementAutomationPeer.CreatePeerForElement(content) is { } peer)
		{
			return peer.GetName();
		}

		return string.Empty;
	}

	protected override string GetHelpTextCore()
	{
		var helpText = base.GetHelpTextCore();
		if (string.IsNullOrEmpty(helpText))
		{
			return helpText;
		}

		var record = TableViewDetails.GetRecord(Owner as FrameworkElement);

		// Resolved here, not at attach, where the cell's binding may not have produced a value yet.
		// Gated on the record so text the app set is never dropped.
		if (record is not null && !string.IsNullOrEmpty(record.PublishedHelpText) &&
			helpText == record.PublishedHelpText &&
			helpText == GetCellValueText())
		{
			return string.Empty;
		}

		return helpText;
	}

	private int GetRowIndex()
	{
		if (m_row.Get() is { } row)
		{
			// TableView exposes no public row-index API, so resolve it from ItemsRepeater.
			DependencyObject? parent = VisualTreeHelper.GetParent(row);
			while (parent is not null)
			{
				if (parent is ItemsRepeater repeater)
				{
					return repeater.GetElementIndex(row);
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
		}

		return -1;
	}

	/// <summary>
	/// Gets the ordinal number of the row that contains the cell.
	/// </summary>
	public int Row
	{
		get
		{
			// Returns -1 only while the row has no resolvable repeater index.
			return GetRowIndex();
		}
	}

	/// <summary>
	/// Gets the ordinal number of the visible column that contains the cell.
	/// </summary>
	public int Column
	{
		get
		{
			// Matches TableViewAutomationPeer::GetItem's cell-host child index.
			return m_columnIndex;
		}
	}

	/// <summary>
	/// Gets the number of rows spanned by the cell.
	/// </summary>
	public int RowSpan => 1;

	/// <summary>
	/// Gets the number of columns spanned by the cell.
	/// </summary>
	public int ColumnSpan => 1;

	/// <summary>
	/// Gets the UI Automation provider of the owning <see cref="TableView"/>.
	/// </summary>
	public IRawElementProviderSimple? ContainingGrid
	{
		get
		{
			// The containing grid is the owning TableView's automation peer.
			if (m_row.Get() is { } row)
			{
				if (row.GetOwningTableView() is { } owner)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(owner) is { } peer)
					{
						return ProviderFromPeer(peer);
					}
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Retrieves the row header items associated with the cell.
	/// </summary>
	/// <returns>An empty array, as TableView has no row headers.</returns>
	public IRawElementProviderSimple[] GetRowHeaderItems()
	{
		// TableView has no row-header concept.
		return Array.Empty<IRawElementProviderSimple>();
	}

	/// <summary>
	/// Retrieves the column header item associated with the cell.
	/// </summary>
	/// <returns>An array holding the column header provider, or an empty array.</returns>
	public IRawElementProviderSimple[] GetColumnHeaderItems()
	{
		// Return the corresponding column header provider using the same peer construction path
		// as the table-level header enumeration.
		List<IRawElementProviderSimple> headers = new();

		if (m_column.Get() is { } column)
		{
			if (m_row.Get() is { } row)
			{
				if (row.GetOwningTableView() is { } owner)
				{
					var headerPeer = new TableViewColumnHeaderAutomationPeer(owner, column);

					// A provider array must not contain nulls - UIA marshals every element. An empty
					// array correctly reports "this cell has no reachable column header".
					if (ProviderFromPeer(headerPeer) is { } provider)
					{
						headers.Add(provider);
					}
				}
			}
		}

		return headers.ToArray();
	}

	// ----- IValueProvider -----

	/// <summary>
	/// Gets the displayed text of the cell.
	/// </summary>
	public string Value => GetCellValueText();

	/// <summary>
	/// Gets a value that indicates whether the cell value cannot be set.
	/// </summary>
	public bool IsReadOnly => !SupportsValuePattern();

	/// <summary>
	/// Sets the cell value by driving the TableView edit lifecycle.
	/// </summary>
	/// <param name="value">The text to write to the cell.</param>
	public void SetValue(string value)
	{
		if (IsReadOnly)
		{
			// throw winrt::hresult_error(E_NOTIMPL, L"This cell is read-only.");
			throw new NotImplementedException("This cell is read-only.");
		}

		var row = m_row.Get();
		var column = m_column.Get();
		if (row is null || column is null)
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell is no longer realized.");
			throw new InvalidOperationException("The cell is no longer realized.");
		}

		var owner = column.GetOwningTableView();
		if (owner is null)
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell has no owning TableView.");
			throw new InvalidOperationException("The cell has no owning TableView.");
		}

		var item = row.DataContext;
		var ownerImpl = owner;

		// Drive the real edit lifecycle rather than writing the source directly, so a BeginningEdit
		// handler can still veto and CellEditEnding/validation still run - a programmatic set must not
		// be able to do what a user cannot.
		if (!ownerImpl.BeginEdit(item, column))
		{
			// throw winrt::hresult_error(E_FAIL, L"The cell could not be opened for editing.");
			throw new InvalidOperationException("The cell could not be opened for editing.");
		}

		bool wrote = false;
		if (ownerImpl.CurrentEditingElement() is { } editingElement)
		{
			if (editingElement is TextBox textBox)
			{
				textBox.Text = value;
				wrote = true;
			}
		}

		if (!wrote)
		{
			// Nothing we can type into - do not leave the editor open.
			ownerImpl.CancelEdit();
			// throw winrt::hresult_error(E_NOTIMPL, L"This cell's editor does not support setting a text value.");
			throw new NotImplementedException("This cell's editor does not support setting a text value.");
		}

		if (!ownerImpl.CommitEdit())
		{
			// Vetoed, rejected by validation, or waiting on a deferral; the edit stays open and the
			// caller must not be told the value was applied.
			// throw winrt::hresult_error(E_FAIL, L"The value was not accepted.");
			throw new InvalidOperationException("The value was not accepted.");
		}
	}

	private bool SupportsValuePattern()
	{
		var column = m_column.Get();
		if (column is null || column.IsReadOnly)
		{
			return false;
		}

		var owner = column.GetOwningTableView();
		if (owner is null || owner.IsReadOnly)
		{
			return false;
		}

		// SetValue writes text, so the column must produce a TextBox. A text column no longer implies
		// one: CellEditingTemplate lives on the base column now, so an app can replace any column's
		// editor with an arbitrary template. Advertising the pattern then tells assistive technology it
		// can set a value, and the attempt fails only after an edit has been opened on screen.
		var textColumn = column as TableViewTextColumn;
		return textColumn is not null && column.CellEditingTemplate is null;
	}
}
