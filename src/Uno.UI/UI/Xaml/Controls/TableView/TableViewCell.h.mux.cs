// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCell.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TODO Uno: a COM interface (uuid 843c3f59-aab0-4bcb-a8a4-12327286a29f) in C++; HRESULT + out param become a
// return value and exceptions here.
internal interface ITableViewCellAutomationPeerAccess
{
	AutomationPeer? GetExistingPeer();
}

// Border is sealed; use a single-child Grid so framework peer discovery and Grid.GetItem return
// the same cell peer. The cell is the keyboard/UIA focus target, letting Narrator announce one
// "{column}, {value}" instead of the whole row.
internal partial class TableViewCell : Grid, ITableViewCellAutomationPeerAccess
{
	public TableViewCell(TableViewRow? row, TableViewColumn? column, int columnIndex)
	{
		m_threadAffinity = ReferenceTrackerThreadAffinity.ForCurrentThread();
		m_row = row is not null ? new WeakReference<TableViewRow>(row) : null;
		m_column = column is not null ? new WeakReference<TableViewColumn>(column) : null;
		m_columnIndex = columnIndex;
	}

	public static Grid Create(
		TableViewRow? row, TableViewColumn? column, int columnIndex)
	{
		Grid wrapper = new TableViewCell(row, column, columnIndex);

		// Cells need IsTabStop for UIA SetFocus to succeed; row policy later gates which cells are
		// in the tab order.
		wrapper.IsTabStop = true;

		// The focus rect manager honours this on any focusable UIElement, not just on a Control.
		wrapper.UseSystemFocusVisuals = true;

		return wrapper;
	}

	// TODO Uno: GetRuntimeClassName() reports the cell as a plain Grid
	// (winrt::hstring_name_of<winrt::Grid>()). Uno has no WinRT runtime class name to override.

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		if (m_automationPeer.Get() is { } existingPeer)
		{
			return existingPeer;
		}
		AutomationPeer peer = new TableViewCellAutomationPeer(
			this, m_row.Get(), m_column.Get(), m_columnIndex);
		m_automationPeer = new WeakReference<AutomationPeer>(peer);
		return peer;
	}

	public AutomationPeer? GetExistingPeer()
	{
		m_threadAffinity.CheckThread();
		return m_automationPeer.Get();
	}

	public static AutomationPeer? TryGetExistingPeer(UIElement? cell)
	{
		AutomationPeer? peer = null;
		if (cell is ITableViewCellAutomationPeerAccess access)
		{
			peer = access.GetExistingPeer();
		}
		return peer;
	}

	public static UIElement? Child(Grid cell)
	{
		var children = cell.Children;
		return children.Count > 0 ? children[0] : null;
	}

	public static void Child(Grid cell, UIElement? content)
	{
		var children = cell.Children;
		children.Clear();
		if (content is not null)
		{
			children.Add(content);
		}
	}

	private readonly ReferenceTrackerThreadAffinity m_threadAffinity;
	private readonly WeakReference<TableViewRow>? m_row;
	private readonly WeakReference<TableViewColumn>? m_column;
	private WeakReference<AutomationPeer>? m_automationPeer = null;
	private readonly int m_columnIndex;
}
