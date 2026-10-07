// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellAutomationPeer.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// UIA peer for a realized TableView cell; supplies the cell name and grid/table item coordinates.
// Weak row/column refs avoid extending recycled rows or removed columns.
partial class TableViewCellAutomationPeer
{
	// TableViewCellAutomationPeer(
	//     winrt::FrameworkElement const& cell,
	//     winrt::TableViewRow const& row,
	//     winrt::TableViewColumn const& column,
	//     int32_t columnIndex);

	// IAutomationPeerOverrides
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);
	// hstring GetClassNameCore();
	// hstring GetNameCore();
	// hstring GetHelpTextCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();

	// IGridItemProvider — per-cell coordinates in the owning TableView.
	// int32_t Row();
	// int32_t Column();
	// int32_t RowSpan();
	// int32_t ColumnSpan();
	// winrt::IRawElementProviderSimple ContainingGrid();

	// ITableItemProvider — returns this cell's column header; rows have no headers.
	// winrt::com_array<winrt::IRawElementProviderSimple> GetRowHeaderItems();
	// winrt::com_array<winrt::IRawElementProviderSimple> GetColumnHeaderItems();

	// IValueProvider — lets assistive technology read the cell text and set it without a pointer.
	// SetValue drives the same public edit lifecycle a user does (BeginEdit / write / CommitEdit),
	// so BeginningEdit / CellEditEnding handlers and validation all still run.
	// winrt::hstring Value();
	// bool IsReadOnly();
	// void SetValue(winrt::hstring const& value);

	// private:
	// True when this cell is editable AND its column produces a TextBox editor, which is the only
	// editor SetValue can drive today.
	// bool SupportsValuePattern();

	// Resolves the row index from the owning ItemsRepeater.
	// int32_t GetRowIndex();

	// Resolves the column's stringified Header.
	// winrt::hstring GetColumnHeaderText();

	// Resolves displayed text from the TextBlock or content peer name.
	// winrt::hstring GetCellValueText();

	private WeakReference<TableViewRow>? m_row = null;
	private WeakReference<TableViewColumn>? m_column = null;
	private int m_columnIndex = -1;
}
