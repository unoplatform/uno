// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeaderAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumnHeaderAutomationPeer
{
	// Two 32-bit halves of the column's stable IUnknown, which is the cheapest per-column
	// identity available here. Widen to 64-bit before shifting so this stays correct on 32-bit,
	// where uintptr_t is 32-bit and `>> 32` would be an out-of-range shift; the high part is
	// simply 0 there.
	private static int[] RuntimeIdPartsForColumn(TableViewColumn? column)
	{
		if (column is null)
		{
			return new int[] { 0, 0 };
		}

		// TODO Uno: There is no IUnknown address in .NET; ObjectIdentityHelper hands out a stable id per live object.
		// Original C++:
		// const uint64_t identity = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(winrt::get_unknown(column)));
		ulong identity = ObjectIdentityHelper.GetId(column);
		return new int[]
		{
			unchecked((int)(identity & 0xffffffffUL)),
			unchecked((int)((identity >> 32) & 0xffffffffUL))
		};
	}

	// Help text is supplementary: degrade instead of letting a resource failure escape into UIA.
	private static string TryGetLocalizedString(string resourceName)
	{
		try
		{
			return ResourceAccessor.GetLocalizedStringResource(resourceName);
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewColumnHeaderAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableView"/> that hosts the column header.</param>
	/// <param name="column">The column whose header the peer exposes.</param>
	public TableViewColumnHeaderAutomationPeer(
		TableView owner,
		TableViewColumn column)
		: base(owner)
	{
		// TODO Uno: winrt::make_weak on a null column yields an empty weak_ref.
		m_column = column is not null ? new WeakReference<TableViewColumn>(column) : null;
		m_columnRuntimeIdParts = RuntimeIdPartsForColumn(column);
	}

	protected override string GetClassNameCore() => "TableViewColumnHeader";

	protected override string GetNameCore()
	{
		// Prefer string headers so screen readers announce a distinct column name.
		if (TryGetColumnHeaderString(GetTarget(m_column)) is { } headerString)
		{
			return headerString;
		}

		// For template headers, use the realized header cell and avoid the TableView owner's name.
		if (GetHeaderElement() is { } headerElement)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(headerElement) is { } peer)
			{
				var name = peer.GetName();
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
			}
		}

		return string.Empty;
	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		// Column headers are leaf HeaderItems; do not expose the TableView subtree.
		return new List<AutomationPeer>();
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// HeaderItem is the UIA control type for table column headers.
		return AutomationControlType.HeaderItem;
	}

	// TODO Uno: Uno's AutomationPeer has no overridable GetRuntimeIdCore, and IAutomationPeerOverrides
	// does not declare one in WinUI either, so this is not reachable through the base peer. Kept for parity.
#pragma warning disable IDE0051 // Unused private member
	private int[] GetRuntimeIdCore()
	{
		// Header peers are all owned by the TableView, so the owner-derived RuntimeId the base
		// would supply is identical for every column - a UIA protocol violation that makes the
		// headers indistinguishable to assistive technology. Build a self-contained id instead:
		// the UiaAppendRuntimeId prefix keeps it well-formed as a framework-appended runtime id,
		// the control-family tag namespaces it, and the column identity parts make it unique and
		// stable for the lifetime of the column.
		return new int[]
		{
			3, // UiaAppendRuntimeId
			0x54564348, // 'TVCH' control-family tag
			m_columnRuntimeIdParts[0],
			m_columnRuntimeIdParts[1]
		};
	}
#pragma warning restore IDE0051

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

		// Otherwise fall back to the same column identity backing the RuntimeId, so headers stay
		// addressable in UI automation before their templates realize.
		string automationId = "TableViewColumnHeader_";
		automationId += m_columnRuntimeIdParts[0].ToString(CultureInfo.InvariantCulture);
		automationId += '_';
		automationId += m_columnRuntimeIdParts[1].ToString(CultureInfo.InvariantCulture);
		return automationId;
	}

	protected override string GetHelpTextCore()
	{
		var column = GetTarget(m_column);
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

		// TODO Uno: StringUtil::FormatString (FormatMessage) returns an empty string on a malformed
		// format, while the Uno helper throws, so the failure is mapped back to an empty string.
		try
		{
			return StringUtil.FormatString(format, toolTipText, sortText);
		}
		catch (FormatException)
		{
			return string.Empty;
		}
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke && IsSortableColumn())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <summary>
	/// Advances the column's sort direction through its sort cycle.
	/// </summary>
	public void Invoke()
	{
		if (GetTarget(m_column) is { } column)
		{
			if (Owner is TableView owner)
			{
				owner.ToggleSortDirection(column);
			}
		}
	}

	private bool IsSortableColumn()
	{
		var column = GetTarget(m_column);
		if (column is null || !column.CanSort)
		{
			return false;
		}

		var owner = Owner as TableView;
		return owner is not null && owner.CanUserSortColumns;
	}

	protected override int GetPositionInSetCore()
	{
		// Complements the distinct RuntimeId and GetNameCore: expose the 1-based visible column
		// position so AT (Narrator) can announce "column i of n" as the user moves across headers.
		var index = GetColumnIndex();
		return index >= 0 ? index + 1 : -1;
	}

	protected override int GetSizeOfSetCore()
	{
		// Total visible column count, so PositionInSet reads as "i of n".
		if (Owner is TableView owner)
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
		return -1;
	}

	protected override Rect GetBoundingRectangleCore()
	{
		// Use this header's realized cell bounds; unrealized headers have no on-screen rect.
		if (GetHeaderElement() is { } headerElement)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(headerElement) is { } peer)
			{
				return peer.GetBoundingRectangle();
			}
		}
		return default;
	}

	protected override Point GetClickablePointCore()
	{
		if (GetHeaderElement() is { } headerElement)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(headerElement) is { } peer)
			{
				return peer.GetClickablePoint();
			}
		}
		// Unrealized headers have no clickable point (NaN per UIA convention).
		return new Point(float.NaN, float.NaN);
	}

	private int GetColumnIndex()
	{
		// Logical visible column index, matching GetSizeOfSetCore's visible basis
		// and the rendered header order, so PositionInSet ("i") and SizeOfSet ("n") stay
		// consistent even when Columns contains null holes or collapsed columns.
		// Columns are matched by identity; a column instance is a single logical position
		// (single-owner model), so the same instance appearing twice in Columns is unsupported.
		if (Owner is TableView owner)
		{
			if (GetTarget(m_column) is { } col)
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
		var owner = Owner as TableView;
		var col = GetTarget(m_column);
		if (owner is null || col is null)
		{
			return null;
		}

		var host = owner.GetHeaderHostInternal();
		if (host is null)
		{
			return null;
		}

		return TableViewCellsPanel.CellForColumn(host, col);
	}

	// TODO Uno: weak_ref<T>::get() equivalent.
	private static T? GetTarget<T>(WeakReference<T>? weakRef) where T : class
		=> weakRef is not null && weakRef.TryGetTarget(out var target) ? target : null;
}
