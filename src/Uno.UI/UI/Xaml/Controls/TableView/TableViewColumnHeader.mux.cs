// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeader.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewColumnHeader
{
	private const string s_ContentPresenterPartName = "PART_ContentPresenter";
	private const string s_GridLinePartName = "PART_GridLine";
	private const string s_SortIndicatorHostPartName = "PART_SortIndicatorHost";
	private const string s_ResizeGripperHostPartName = "PART_ResizeGripperHost";
	private const string s_SortIndicatorName = "TableViewSortIndicator";

	internal TableViewColumnHeader()
	{
		this.SetTabularDefaultStyleKey();
	}

	internal void SetOwnerInternal(TableView? table, TableViewColumn? column)
	{
		m_table = table is not null ? new WeakReference<TableView>(table) : null;
		m_column = column is not null ? new WeakReference<TableViewColumn>(column) : null;
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		var table = m_table.Get();
		var column = m_column.Get();
		if (table is null || column is null)
		{
			return base.OnCreateAutomationPeer();
		}

		AutomationPeer peer = new TableViewColumnHeaderAutomationPeer(this, table, column);
		m_automationPeer = new WeakReference<AutomationPeer>(peer);
		return peer;
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_isEnabledChangedRevoker.Disposable = null;
		m_templateRootContextRequestedRevoker.Disposable = null;

		if (m_resizeGripperHost is { } oldGripperHost)
		{
			oldGripperHost.Child = null;
		}
		if (m_sortIndicatorHost is { } oldIndicatorHost)
		{
			oldIndicatorHost.Child = null;
		}

		m_contentPresenter = GetTemplateChild(s_ContentPresenterPartName) as ContentPresenter;
		m_gridLine = GetTemplateChild(s_GridLinePartName) as Border;
		m_sortIndicatorHost = GetTemplateChild(s_SortIndicatorHostPartName) as Border;
		m_resizeGripperHost = GetTemplateChild(s_ResizeGripperHostPartName) as Border;

		m_templateContentMargin = default;
		if (m_contentPresenter is { } presenter)
		{
			m_templateContentMargin = presenter.Margin;
		}
		UpdateContentReserve();
		HostSortIndicator();

		if (m_resizeGripperHost is { } gripperHost)
		{
			if (m_resizeGripper is { } gripper)
			{
				gripperHost.Child = gripper;
			}
		}

		// Keeps a Style-set ContextFlyout closed while a cell is editing.
		WeakReference<TableViewColumnHeader> weakThis = new(this);
		m_templateRootContextRequestedRevoker.Disposable = TableViewDetails.HookTemplateRootContextRequested(this,
			(UIElement _, ContextRequestedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis) && strongThis.GetOwningTableView() is { } owner)
				{
					owner.SuppressContextRequestWhileEditing(args);
				}
			});

		DependencyPropertyChangedEventHandler handler = (object sender, DependencyPropertyChangedEventArgs args) =>
		{
			if (weakThis.TryGetTarget(out var strongThis))
			{
				strongThis.UpdateVisualStates(true /* useTransitions */);
			}
		};
		IsEnabledChanged += handler;
		m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= handler);

		UpdateVisualStates(false /* useTransitions */);

		if (GetOwningTableView() is { } tableView)
		{
			tableView.OnColumnHeaderTemplateApplied(this);
		}
	}

	internal void SetSortableInternal(bool sortable, double indicatorReserve)
	{
		m_isSortable = sortable;
		m_indicatorReserve = sortable ? indicatorReserve : 0.0;
		UpdateContentReserve();
		HostSortIndicator();
		UpdateVisualStates(false /* useTransitions */);
	}

	internal void SetSortDirectionInternal(SortDirection direction)
	{
		m_sortDirection = direction;
		if (m_sortIndicator is { } indicator)
		{
			indicator.Direction = TableView.ToSortIndicatorDirection(direction);
		}
		UpdateVisualStates(true /* useTransitions */);
	}

	internal void SetResizeGripperInternal(ResizeGripper? gripper)
	{
		if (m_resizeGripperHost is { } host)
		{
			host.Child = gripper;
		}
		m_resizeGripper = gripper;
	}

	internal ResizeGripper? GetResizeGripperInternal()
	{
		var gripper = m_resizeGripper;
		var host = m_resizeGripperHost;
		if (gripper is not null && host is not null && host.Child == gripper)
		{
			return gripper;
		}
		return null;
	}

	private void UpdateContentReserve()
	{
		if (m_contentPresenter is { } presenter)
		{
			var margin = m_templateContentMargin;
			if (m_isSortable)
			{
				margin.Right += m_indicatorReserve;
			}
			if (presenter.Margin != margin)
			{
				presenter.Margin = margin;
			}
		}
	}

	private void HostSortIndicator()
	{
		var host = m_sortIndicatorHost;
		if (host is null)
		{
			return;
		}

		if (!m_isSortable)
		{
			host.Child = null;
			return;
		}

		var indicator = m_sortIndicator;
		if (indicator is null)
		{
			indicator = new SortIndicator();
			indicator.Name = s_SortIndicatorName;
			indicator.VerticalAlignment = VerticalAlignment.Center;
			// The header peer already reports the sort state.
			AutomationProperties.SetAccessibilityView(indicator, AccessibilityView.Raw);
			m_sortIndicator = indicator;
		}
		indicator.Direction = TableView.ToSortIndicatorDirection(m_sortDirection);

		if (host.Child != indicator)
		{
			host.Child = indicator;
		}
	}

	private void SyncSortIndicatorForeground(bool followText, bool highlighted)
	{
		var indicator = m_sortIndicator;
		if (indicator is null)
		{
			return;
		}

		// The glyph's style sets Foreground, so it cannot inherit; when a state recolors the text
		// (Disabled, or a High Contrast highlight) the glyph must follow it. Bound, not copied, so it
		// also follows a re-resolved theme brush (High Contrast toggle) without a state change.
		var presenter = m_contentPresenter;
		if (followText && presenter is not null)
		{
			// Rebinding on every state change would rebuild an identical expression.
			var bound = indicator.GetBindingExpression(Control.ForegroundProperty);
			if (bound is null || !ReferenceEquals(bound.ParentBinding.Source, presenter))
			{
				Binding binding = new();
				binding.Source = presenter;
				binding.Path = new PropertyPath("Foreground");
				indicator.SetBinding(Control.ForegroundProperty, binding);
			}
		}
		else
		{
			indicator.ClearValue(Control.ForegroundProperty);
		}

		if (highlighted)
		{
			indicator.HighContrastAdjustment = ElementHighContrastAdjustment.None;
		}
		else
		{
			indicator.ClearValue(UIElement.HighContrastAdjustmentProperty);
		}
	}

	private void UpdateVisualStates(bool useTransitions)
	{
		// Hover and press advertise click-to-sort, so a header that cannot sort stays Normal.
		string commonState = "Normal";
		if (!IsEnabled)
		{
			commonState = "Disabled";
		}
		else if (m_isSortable && m_isPressed)
		{
			commonState = "Pressed";
		}
		else if (m_isSortable && m_isPointerOver)
		{
			commonState = "PointerOver";
		}

		string highContrastState = "HighContrastNormal";
		if (commonState != "Normal" && commonState != "Disabled")
		{
			if (GetOwningTableView() is { } owner && owner.IsHighContrast())
			{
				highContrastState = m_isPressed ? "HighContrastPressed" : "HighContrastPointerOver";
			}
		}

		// Both groups set the text Foreground, and leaving a state clears it whichever group set it,
		// so the group that sets it must transition last.
		if (IsEnabled)
		{
			VisualStateManager.GoToState(this, commonState, useTransitions);
			VisualStateManager.GoToState(this, highContrastState, useTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, highContrastState, useTransitions);
			VisualStateManager.GoToState(this, commonState, useTransitions);
		}
		bool highlighted = highContrastState != "HighContrastNormal";
		SyncSortIndicatorForeground(highlighted || !IsEnabled, highlighted);

		string sortState = "Unsorted";
		if (m_sortDirection == SortDirection.Ascending)
		{
			sortState = "SortAscending";
		}
		else if (m_sortDirection == SortDirection.Descending)
		{
			sortState = "SortDescending";
		}
		VisualStateManager.GoToState(this, sortState, useTransitions);
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);
		m_isPointerOver = true;
		UpdateVisualStates(true /* useTransitions */);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		base.OnPointerExited(args);
		m_isPointerOver = false;
		m_isPressed = false;
		UpdateVisualStates(true /* useTransitions */);
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		base.OnPointerPressed(args);
		m_isPressed = true;
		UpdateVisualStates(true /* useTransitions */);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);
		m_isPressed = false;
		UpdateVisualStates(true /* useTransitions */);
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		base.OnPointerCaptureLost(args);
		ResetPointerState(args);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		base.OnPointerCanceled(args);
		ResetPointerState(args);
	}

	private void ResetPointerState(PointerRoutedEventArgs args)
	{
		// The gripper captures the pointer for its drag; that capture ending bubbles here while the
		// pointer is still over the header, so it must not reset the hover state.
		if (IsInsideResizeGripper(args.OriginalSource))
		{
			return;
		}

		m_isPointerOver = false;
		m_isPressed = false;
		UpdateVisualStates(true /* useTransitions */);
	}

	private bool IsInsideResizeGripper(object? source)
	{
		DependencyObject self = this;
		for (var current = source as DependencyObject;
			current is not null && current != self;
			current = VisualTreeHelper.GetParent(current))
		{
			if (current is ResizeGripper)
			{
				return true;
			}
		}
		return false;
	}
}
