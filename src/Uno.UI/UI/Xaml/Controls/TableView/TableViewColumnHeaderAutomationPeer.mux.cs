// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeaderAutomationPeer.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumnHeaderAutomationPeer
{
	private static uint AutomationIdentityForColumn(TableViewColumn? column)
	{
		return column is not null ? column.AutomationIdentity() : 0;
	}

	private static TableView OwnerForPublicConstructor(TableView? table, TableViewColumn? column)
	{
		if (table is not null && column is not null &&
			column.GetOwningTableView() == table)
		{
			return table;
		}

		// hresult_invalid_argument
		throw new ArgumentException();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewColumnHeaderAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableView"/> that hosts the column header.</param>
	/// <param name="column">The column whose header the peer exposes.</param>
	public TableViewColumnHeaderAutomationPeer(
		TableView owner,
		TableViewColumn column)
		: base(OwnerForPublicConstructor(owner, column))
	{
		m_column = new WeakReference<TableViewColumn>(column);
		m_table = new WeakReference<TableView>(owner);
		m_columnAutomationIdentity = AutomationIdentityForColumn(column);
	}

	internal TableViewColumnHeaderAutomationPeer(
		FrameworkElement header,
		TableView? table,
		TableViewColumn? column)
		: base(header)
	{
		m_column = column is not null ? new WeakReference<TableViewColumn>(column) : null;
		m_table = table is not null ? new WeakReference<TableView>(table) : null;
		m_columnAutomationIdentity = AutomationIdentityForColumn(column);
	}

	protected override string GetClassNameCore() => "TableViewColumnHeader";

	protected override string GetNameCore()
	{
		if (GetHeaderElement() is { } headerElement)
		{
			var name = AutomationProperties.GetName(headerElement);
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			if (GetLabeledBy() is { } label)
			{
				var labelName = label.GetName();
				if (!string.IsNullOrEmpty(labelName))
				{
					return labelName;
				}
			}
		}

		if (TryGetColumnHeaderString(m_column.Get()) is { } headerString)
		{
			return headerString;
		}

		// Read template content, never re-enter this header's own peer.
		if (GetHeaderElement() is Panel header)
		{
			uint remaining = 64;
			foreach (var child in header.Children)
			{
				var childName = GetCellContentName(child as FrameworkElement, true, 8, ref remaining);
				if (!string.IsNullOrEmpty(childName))
				{
					return childName;
				}
			}
		}

		return string.Empty;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.HeaderItem;
	}

	protected override string GetAutomationIdCore()
	{
		// An author-supplied id on the realized header always wins.
		if (GetHeaderElement() is { } headerElement)
		{
			var authoredAutomationId = AutomationProperties.GetAutomationId(headerElement);
			if (!string.IsNullOrEmpty(authoredAutomationId))
			{
				return authoredAutomationId;
			}
		}

		string automationId = "TableViewColumnHeader_";
		automationId += m_columnAutomationIdentity.ToString(CultureInfo.InvariantCulture);
		return automationId;
	}

	protected override string GetHelpTextCore()
	{
		var column = m_column.Get();
		if (column is null)
		{
			return base.GetHelpTextCore();
		}

		// Only a column the control will actually sort reports a sort state; on any other column the
		// absence of a sort state is the honest answer.
		string sortText = string.Empty;
		if (IsSortableColumn())
		{
			switch (column.SortDirection)
			{
				case SortDirection.Ascending:
					sortText = TryGetLocalizedString(ResourceAccessor.SR_TableViewSortAscendingHelpText);
					break;
				case SortDirection.Descending:
					sortText = TryGetLocalizedString(ResourceAccessor.SR_TableViewSortDescendingHelpText);
					break;
				case SortDirection.None:
				default:
					sortText = TryGetLocalizedString(ResourceAccessor.SR_TableViewSortNoneHelpText);
					break;
			}
		}

		// From the column, not the realized header: the peer can be queried before the band exists.
		// String only; rich content is mouse-only, as with cells.
		string toolTipText = string.Empty;
		if (TableViewDetails.TryGetString(column.HeaderToolTip) is { } text)
		{
			toolTipText = text;
		}

		// Dropped when it repeats the header name, to avoid a double announcement.
		if (!string.IsNullOrEmpty(toolTipText) && toolTipText == GetNameCore())
		{
			toolTipText = string.Empty;
		}

		if (string.IsNullOrEmpty(toolTipText))
		{
			return string.IsNullOrEmpty(sortText) ? base.GetHelpTextCore() : sortText;
		}

		if (string.IsNullOrEmpty(sortText))
		{
			return toolTipText;
		}

		// A sighted user gets both at a glance, so neither is dropped.
		var format = TryGetLocalizedString(ResourceAccessor.SR_TableViewColumnHeaderHelpTextFormat);
		if (string.IsNullOrEmpty(format))
		{
			return toolTipText;
		}

		return StringUtil.FormatString(format, toolTipText, sortText);
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke && IsSortableColumn())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override bool IsEnabledCore()
	{
		// Grid is not a Control, so its base peer does not report inherited disabled state.
		var table = m_table.Get();
		if (table is null || !table.IsEnabled)
		{
			return false;
		}

		// A template control (for example the header ScrollViewer) can be disabled
		// independently of the table. Its coerced state applies to this header too.
		var ancestor = VisualTreeHelper.GetParent(Owner);
		while (ancestor is not null && ancestor != table)
		{
			if (ancestor is Control control && !control.IsEnabled)
			{
				return false;
			}
			ancestor = VisualTreeHelper.GetParent(ancestor);
		}
		return true;
	}

	/// <summary>
	/// Advances the column's sort direction through its sort cycle.
	/// </summary>
	public void Invoke()
	{
		if (!IsEnabled())
		{
			// UIA_E_ELEMENTNOTENABLED
			throw new ElementNotEnabledException();
		}
		if (!IsSortableColumn())
		{
			// UIA_E_INVALIDOPERATION
			throw new InvalidOperationException();
		}
		if (m_column.Get() is { } column)
		{
			if (m_table.Get() is { } owner)
			{
				owner.ToggleSortDirection(column);
				return;
			}
		}

		// UIA_E_ELEMENTNOTAVAILABLE
		throw new ElementNotAvailableException();
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (!IsTableViewOwned())
		{
			return base.GetChildrenCore();
		}

		return new List<AutomationPeer>();
	}

	protected override Rect GetBoundingRectangleCore()
	{
		if (IsTableViewOwned())
		{
			return default;
		}

		return base.GetBoundingRectangleCore();
	}

	protected override Point GetClickablePointCore()
	{
		if (IsTableViewOwned())
		{
			return new Point(float.NaN, float.NaN);
		}

		return base.GetClickablePointCore();
	}

	protected override bool IsOffscreenCore()
	{
		return IsTableViewOwned() ? true : base.IsOffscreenCore();
	}

	internal bool IsTableViewOwned()
	{
		return Owner is TableView;
	}

	private bool IsSortableColumn()
	{
		var column = m_column.Get();
		if (column is null || !column.CanSort || !IsVisibleColumn(column))
		{
			return false;
		}

		var owner = m_table.Get();
		return owner is not null && owner.IsLoaded && owner.CanUserSortColumns;
	}

	protected override int GetPositionInSetCore()
	{
		// The header's attached override wins over the visible-column position.
		if (GetHeaderElement() is { } header)
		{
			var provided = AutomationProperties.GetPositionInSet(header);
			if (provided > 0)
			{
				return provided;
			}
		}

		var index = GetColumnIndex();

		// 0 is UIA's "not specified"; valid values are 1-based, so -1 reached the client as a nonsense
		// position.
		return index >= 0 ? index + 1 : 0;
	}

	protected override int GetSizeOfSetCore()
	{
		if (GetHeaderElement() is { } header)
		{
			var provided = AutomationProperties.GetSizeOfSet(header);
			if (provided > 0)
			{
				return provided;
			}
		}

		// Total visible column count, so PositionInSet reads as "i of n".
		if (m_table.Get() is { } owner)
		{
			if (owner.Columns is { } columns)
			{
				int count = 0;
				foreach (var col in columns)
				{
					if (IsVisibleColumn(col)) { ++count; }
				}
				if (count > 0) { return count; }
			}
		}

		return 0;
	}

	private int GetColumnIndex()
	{
		// Logical visible column index, matching GetSizeOfSetCore's visible basis
		// and the rendered header order, so PositionInSet ("i") and SizeOfSet ("n") stay
		// consistent even when Columns contains null holes or collapsed columns.
		// Columns are matched by identity; a column instance is a single logical position
		// (single-owner model), so the same instance appearing twice in Columns is unsupported.
		if (m_table.Get() is { } owner)
		{
			if (m_column.Get() is { } col)
			{
				if (!IsVisibleColumn(col))
				{
					return -1;
				}

				if (owner.Columns is { } columns)
				{
					int logicalIndex = 0;
					foreach (var c in columns)
					{
						if (c == col) { return logicalIndex; }
						if (IsVisibleColumn(c)) { ++logicalIndex; }
					}
				}
			}
		}
		return -1;
	}

	private FrameworkElement? GetHeaderElement()
	{
		// Match by Tag so null Columns entries do not skew logical indexes.
		var owner = m_table.Get();
		var col = m_column.Get();
		if (owner is null || col is null)
		{
			return null;
		}

		var host = owner.GetHeaderHostInternal();
		if (host is null)
		{
			return null;
		}

		var header = TableViewCellsPanel.CellForColumn(host, col);
		if (!(Owner is TableView) && header != Owner)
		{
			return null;
		}
		return header;
	}
}
