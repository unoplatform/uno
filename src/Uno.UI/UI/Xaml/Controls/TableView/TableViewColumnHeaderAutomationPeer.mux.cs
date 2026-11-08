// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeaderAutomationPeer.cpp, tag winui3/main, commit 79209e041

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

		if (TableViewDetails.TryGetColumnHeaderText(m_column.Get()) is { } headerString)
		{
			return headerString;
		}

		// Only the content region names the header, never the grid line, glyph or gripper.
		if (GetHeaderElement() is TableViewColumnHeader header)
		{
			uint remaining = 64;
			var name = GetCellContentName(header.GetContentPresenterInternal(), true, 8, ref remaining);
			if (!string.IsNullOrEmpty(name))
			{
				return name;
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

		// The format supplies its own separator; a tooltip written as a sentence would otherwise read "..".
		var trimmedToolTip = toolTipText.AsSpan();
		while (!trimmedToolTip.IsEmpty &&
			(trimmedToolTip[trimmedToolTip.Length - 1] == '.' || trimmedToolTip[trimmedToolTip.Length - 1] == '\u3002' || char.IsWhiteSpace(trimmedToolTip[trimmedToolTip.Length - 1])))
		{
			trimmedToolTip = trimmedToolTip.Slice(0, trimmedToolTip.Length - 1);
		}
		if (trimmedToolTip.IsEmpty)
		{
			return sortText;
		}

		return StringUtil.FormatString(format, trimmedToolTip.ToString(), sortText);
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke && IsSortableColumn())
		{
			return this;
		}

		if (patternInterface == PatternInterface.Transform && IsResizableColumn())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override bool IsEnabledCore()
	{
		var table = m_table.Get();
		if (table is null || !table.IsEnabled)
		{
			return false;
		}

		// The header's own state, and that of any template control around it (for example the header
		// ScrollViewer), can be disabled independently of the table.
		var header = GetHeaderElement();
		if (header is Control headerControl && !headerControl.IsEnabled)
		{
			return false;
		}
		var ancestor = VisualTreeHelper.GetParent(header ?? Owner);
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

	protected override bool IsKeyboardFocusableCore()
	{
		var owner = m_table.Get();
		var header = GetHeaderElement();
		return owner is not null && header is not null && owner.IsHeaderCellActionableInternal(header);
	}

	// Only an actionable header takes focus; a lagging inactive stamp is dropped first.
	protected override void SetFocusCore()
	{
		var owner = m_table.Get();
		var header = GetHeaderElement();
		if (owner is null || header is null || !owner.IsHeaderCellActionableInternal(header))
		{
			// UIA_E_INVALIDOPERATION
			throw new InvalidOperationException();
		}
		owner.ClearHeaderTabStopStampInternal(header);
		if (!header.Focus(FocusState.Programmatic) || header.FocusState == FocusState.Unfocused)
		{
			// UIA_E_INVALIDOPERATION
			throw new InvalidOperationException();
		}
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

	/// <summary>
	/// Gets a value that indicates whether the header can be moved.
	/// </summary>
	public bool CanMove => false;

	/// <summary>
	/// Gets a value that indicates whether the column can be resized.
	/// </summary>
	public bool CanResize
	{
		get
		{
			if (!IsResizableColumn())
			{
				return false;
			}

			var owner = m_table.Get();
			return owner is not null && owner.CanResizeColumnFromAutomation(m_column.Get()!);
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the header can be rotated.
	/// </summary>
	public bool CanRotate => false;

	/// <summary>
	/// Not supported for a column header.
	/// </summary>
	public void Move(double x, double y)
	{
		// UIA_E_INVALIDOPERATION
		throw new InvalidOperationException();
	}

	/// <summary>
	/// Resizes the column to the given width.
	/// </summary>
	public void Resize(double width, double height)
	{
		if (!IsEnabled())
		{
			// UIA_E_ELEMENTNOTENABLED
			throw new ElementNotEnabledException();
		}
		if (!double.IsFinite(width) || width < 0.0)
		{
			// hresult_invalid_argument
			throw new ArgumentException();
		}
		if (!CanResize)
		{
			// UIA_E_INVALIDOPERATION
			throw new InvalidOperationException();
		}

		// Height is ignored: a column header only resizes along the column axis.
		var column = m_column.Get();
		var owner = m_table.Get();
		if (column is null || owner is null)
		{
			// UIA_E_ELEMENTNOTAVAILABLE
			throw new ElementNotAvailableException();
		}

		// UIA sizes are physical pixels, like BoundingRectangle; column widths are DIPs in the table's
		// own frame, below any scale applied above it.
		if (owner.XamlRoot is { } xamlRoot)
		{
			double scale = xamlRoot.RasterizationScale;
			try
			{
				var unit = owner.TransformToVisual(null).TransformBounds(new Rect(0.0, 0.0, 1.0, 0.0));
				if (unit.Width > 0.0)
				{
					scale *= unit.Width;
				}
			}
			catch (Exception)
			{
				// Not in the live tree; the display scale alone is the best answer.
			}
			if (scale > 0.0)
			{
				width /= scale;
			}
		}

		if (!owner.TryResizeColumnFromAutomation(column, width))
		{
			// UIA_E_INVALIDOPERATION
			throw new InvalidOperationException();
		}
	}

	/// <summary>
	/// Not supported for a column header.
	/// </summary>
	public void Rotate(double degrees)
	{
		// UIA_E_INVALIDOPERATION
		throw new InvalidOperationException();
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

	protected override void ShowContextMenuCore()
	{
		if (!IsTableViewOwned())
		{
			base.ShowContextMenuCore();
			return;
		}

		var column = m_column.Get();
		if (column is null || !IsVisibleColumn(column))
		{
			return;
		}

		if (GetHeaderElement() is { } header)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(header) is { } peer)
			{
				peer.ShowContextMenu();
			}
		}
	}

	internal bool IsTableViewOwned()
	{
		return Owner is TableView;
	}

	private bool IsSortableColumn()
	{
		var column = m_column.Get();
		if (column is null || !IsVisibleColumn(column))
		{
			return false;
		}

		// A column without a sort key cannot sort, so it does not advertise Invoke.
		var owner = m_table.Get();
		return owner is not null && owner.IsLoaded && owner.IsColumnUserSortableInternal(column);
	}

	// The static opt-in only; whether the gripper is usable right now is CanResize's job.
	private bool IsResizableColumn()
	{
		var column = m_column.Get();
		if (column is null || !column.CanResize || !IsVisibleColumn(column))
		{
			return false;
		}

		var owner = m_table.Get();
		return owner is not null && owner.CanUserResizeColumns;
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
