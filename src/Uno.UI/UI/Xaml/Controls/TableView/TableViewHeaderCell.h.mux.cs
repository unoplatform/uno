// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewHeaderCell.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Keep the existing Grid layout/input behavior, but attach the header peer to the
// actual focus and hit-test target. This implementation-only type needs no WinRT API.
internal partial class TableViewHeaderCell : Grid
{
	public TableViewHeaderCell(TableView? table, TableViewColumn? column)
	{
		m_table = table is not null ? new WeakReference<TableView>(table) : null;
		m_column = column is not null ? new WeakReference<TableViewColumn>(column) : null;
	}

	// TODO Uno: GetRuntimeClassName has no Uno equivalent; WinUI reports this type as Grid
	// (hstring_name_of<winrt::Grid>()) so it stays indistinguishable from the plain Grid it replaced.

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new TableViewColumnHeaderAutomationPeer(
			this, m_table.Get(), m_column.Get());
	}

	private readonly WeakReference<TableView>? m_table;
	private readonly WeakReference<TableViewColumn>? m_column;
}
