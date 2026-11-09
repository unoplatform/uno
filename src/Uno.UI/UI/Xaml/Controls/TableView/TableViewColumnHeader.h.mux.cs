// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeader.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// The owning TableView decides sortability/resizability, wires the gripper and drives the grid
// line; this type hosts those in its template parts and maps state onto VisualStates.
partial class TableViewColumnHeader
{
	// TableViewColumnHeader();

	// void OnApplyTemplate();
	// winrt::AutomationPeer OnCreateAutomationPeer();
	internal AutomationPeer? GetExistingAutomationPeerInternal() => m_automationPeer.Get();

	// None of these mark the event handled, so Tapped-to-sort and focus are unaffected.
	// void OnPointerEntered(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerExited(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerPressed(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerReleased(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCaptureLost(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCanceled(winrt::PointerRoutedEventArgs const& args);

	// void SetOwnerInternal(winrt::TableView const& table, winrt::TableViewColumn const& column);
	internal TableView? GetOwningTableView() => m_table.Get();

	/// <summary>
	/// Gets the column this header belongs to.
	/// </summary>
	public TableViewColumn? Column => m_column.Get();

	// void SetSortableInternal(bool sortable, double indicatorReserve);
	// void SetSortDirectionInternal(winrt::SortDirection direction);

	// Returns the gripper only while it is hosted: an unhosted gripper cannot drag.
	// void SetResizeGripperInternal(winrt::ResizeGripper const& gripper);
	// winrt::ResizeGripper GetResizeGripperInternal() const;

	internal ContentPresenter? GetContentPresenterInternal() => m_contentPresenter;
	internal Border? GetGridLineInternal() => m_gridLine;

	// True while Content is the TextBlock the TableView made for a string Header.
	internal void SetOwnsContentInternal(bool ownsContent) => m_ownsContent = ownsContent;
	internal bool OwnsContentInternal() => m_ownsContent;

	// True while the local IsTabStop=false is the TableView's inactive-header stamp, not the app's.
	internal void SetOwnsTabStopStampInternal(bool ownsStamp) => m_ownsTabStopStamp = ownsStamp;
	internal bool OwnsTabStopStampInternal() => m_ownsTabStopStamp;

	// private:
	// void UpdateVisualStates(bool useTransitions);
	// void UpdateContentReserve();
	// void HostSortIndicator();
	// void SyncSortIndicatorForeground(bool followText, bool highlighted);
	// void ResetPointerState(winrt::PointerRoutedEventArgs const& args);
	// bool IsInsideResizeGripper(winrt::IInspectable const& source);

	private WeakReference<TableView>? m_table = null;
	private WeakReference<TableViewColumn>? m_column = null;
	private WeakReference<AutomationPeer>? m_automationPeer = null;

	private ContentPresenter? m_contentPresenter;
	private Border? m_gridLine;
	private Border? m_sortIndicatorHost;
	private Border? m_resizeGripperHost;
	private SortIndicator? m_sortIndicator;
	private ResizeGripper? m_resizeGripper;

	private readonly SerialDisposable m_isEnabledChangedRevoker = new();
	private readonly SerialDisposable m_templateRootContextRequestedRevoker = new();

	private Thickness m_templateContentMargin = default;
	private double m_indicatorReserve = 0.0;
	private bool m_isSortable = false;
	private SortDirection m_sortDirection = SortDirection.None;
	private bool m_isPointerOver = false;
	private bool m_isPressed = false;
	private bool m_ownsContent = false;
	private bool m_ownsTabStopStamp = false;
}
