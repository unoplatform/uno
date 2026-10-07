// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupHeaderAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewGroupHeaderAutomationPeer
{
	// Pulls in the generated activation factory (CppWinRTActivatableClassWithBasicFactory), the
	// same way TableViewRowAutomationPeer does.

	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewGroupHeaderAutomationPeer"/> class.
	/// </summary>
	/// <param name="owner">The <see cref="TableViewGroupHeader"/> to create a peer for.</param>
	public TableViewGroupHeaderAutomationPeer(TableViewGroupHeader owner) : base(owner)
	{
	}

	private TableViewGroupHeader? GetHeader() => Owner as TableViewGroupHeader;

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		// Unconditional, unlike the old adaptive row peer: this container is only ever a group
		// header, so the pattern never has to be withdrawn. Non-expandable groups are reported as
		// LeafNode by ExpandCollapseState rather than by hiding the pattern, which is what the
		// in-box Expander / NavigationViewItem peers do.
		if (patternInterface == PatternInterface.ExpandCollapse ||
			patternInterface == PatternInterface.GridItem)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	protected override string GetClassNameCore() => "TableViewGroupHeader";

	protected override string GetNameCore()
	{
		// Read from the live projection rather than the visual tree, so the name reflects current
		// state instead of whatever was last rendered.
		//
		// ItemCountText already carries its own localized framing (SR_TableViewGroupHeaderCountFormat,
		// "({0})"), so the two parts are joined with the same single space the default template puts
		// between them. Anything richer belongs in a dedicated localized format string rather than
		// being assembled here.
		if (GetHeader() is { } header)
		{
			if (header.Content is TableViewGroupInfo info)
			{
				var keyText = info.KeyText;
				var countText = info.ItemCountText;
				if (!string.IsNullOrEmpty(keyText) && !string.IsNullOrEmpty(countText))
				{
					return keyText + " " + countText;
				}
				if (!string.IsNullOrEmpty(keyText))
				{
					return keyText;
				}
			}
		}

		return base.GetNameCore();
	}

	internal void RaiseExpandCollapseAutomationEvent(
		ExpandCollapseState oldState,
		ExpandCollapseState newState)
	{
		if (oldState == newState)
		{
			return;
		}

		if (!AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			return;
		}

		try
		{
			RaisePropertyChangedEvent(
				ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldState,
				newState);
		}
		catch (Exception)
		{
			// best-effort: the tree may be tearing down.
		}
	}

	/// <summary>
	/// Expands the group.
	/// </summary>
	public void Expand()
	{
		SetExpansion(true);
	}

	/// <summary>
	/// Collapses the group.
	/// </summary>
	public void Collapse()
	{
		SetExpansion(false);
	}

	private void SetExpansion(bool expand)
	{
		var header = GetHeader();
		if (header is null || !header.IsExpandable)
		{
			return;
		}

		// Direction is passed through rather than resolved here into a toggle. The mutation is
		// applied on a later turn, so a guard reading IsExpanded() (the last state pushed to this
		// container) cannot make a toggle directional -- two Expand() calls in one client turn
		// would both pass such a guard and queue two toggles, leaving the group collapsed.
		// ExpandCollapsePattern requires Expand/Collapse to be idempotent.
		//
		// Resolved through the header's stored owner, not an ancestor walk: AT clients routinely
		// hold a provider across a scroll, and a walk from an unparented container finds nothing
		// and silently no-ops.
		if (header.GetOwningTableView() is { } owner)
		{
			owner.SetGroupExpansion(header, expand);
		}
	}

	/// <summary>
	/// Gets the expand/collapse state of the group; a non-expandable group reports LeafNode.
	/// </summary>
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			if (GetHeader() is { } header)
			{
				if (!header.IsExpandable)
				{
					return ExpandCollapseState.LeafNode;
				}
				return header.IsExpanded
					? ExpandCollapseState.Expanded
					: ExpandCollapseState.Collapsed;
			}
			return ExpandCollapseState.LeafNode;
		}
	}

	private TableView? GetOwningTableView()
	{
		if (GetHeader() is { } header)
		{
			return header.GetOwningTableView();
		}
		return null;
	}

	// The band occupies one grid row and spans every visible column -- the merged-cell shape that
	// TableViewAutomationPeer::GetItem reports for a header row.

	/// <summary>
	/// Gets the ordinal number of the row occupied by the group header.
	/// </summary>
	public int Row
	{
		get
		{
			if (GetHeader() is { } header)
			{
				if (GetOwningTableView() is { } owner)
				{
					if (owner.GetRowsRepeaterForPeer() is { } repeater)
					{
						return repeater.GetElementIndex(header);
					}
				}
			}
			return -1;
		}
	}

	/// <summary>
	/// Gets the ordinal number of the first column spanned by the group header.
	/// </summary>
	public int Column => 0;

	/// <summary>
	/// Gets the number of rows spanned by the group header.
	/// </summary>
	public int RowSpan => 1;

	/// <summary>
	/// Gets the number of visible columns spanned by the group header.
	/// </summary>
	public int ColumnSpan
	{
		get
		{
			if (GetOwningTableView() is { } owner)
			{
				if (owner.Columns is { } columns)
				{
					int count = 0;
					foreach (var column in columns)
					{
						if (column is not null && column.Visibility == Visibility.Visible)
						{
							++count;
						}
					}
					return count > 0 ? count : 1;
				}
			}
			return 1;
		}
	}

	/// <summary>
	/// Gets the UI Automation provider of the owning <see cref="TableView"/>.
	/// </summary>
	public IRawElementProviderSimple? ContainingGrid
	{
		get
		{
			if (GetOwningTableView() is { } owner)
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(owner) is { } peer)
				{
					return ProviderFromPeer(peer);
				}
			}
			return null;
		}
	}
}
