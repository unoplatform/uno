// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupHeader.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewGroupHeader
{
	private const string s_GridLineBorderPartName = "PART_GridLineBorder";

	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewGroupHeader"/> class.
	/// </summary>
	public TableViewGroupHeader()
	{
		this.SetTabularDefaultStyleKey();

		WeakReference<TableViewGroupHeader> weakThis = new(this);
		RegisterPropertyChangedCallback(UIElement.ContextFlyoutProperty,
			(DependencyObject _, DependencyProperty _) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_contextFlyoutOpeningRevoker.Disposable = TableViewDetails.HookContextFlyoutSuppression(strongThis);
				}
			});
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new TableViewGroupHeaderAutomationPeer(this);

	protected override Size MeasureOverride(Size availableSize)
	{
		var desired = MeasureBand(availableSize);

		// The base measure realizes the content template, so a header created disabled, or given a new
		// content template while disabled, is handled here.
		if (!IsEnabled && ContentTemplateRoot != GetInspectedContentRoot())
		{
			UpdateDefaultCountOpacity();
		}
		return desired;
	}

	// Uno specific: WeakReference<T> has no get() returning null.
	private UIElement? GetInspectedContentRoot()
		=> m_inspectedContentRoot is not null && m_inspectedContentRoot.TryGetTarget(out var root) ? root : null;

	private Size MeasureBand(Size availableSize)
	{
		// Same sum TableViewCellsPanel uses for a row, evaluated at the same point in the layout
		// pass, so the band and the rows can never disagree.
		double columnsTotal = 0.0;
		if (GetOwningTableView() is { } owner)
		{
			if (owner.Columns is { } columns)
			{
				foreach (var column in columns)
				{
					if (column is not null && column.Visibility == Visibility.Visible)
					{
						columnsTotal += Math.Max(0.0, column.ActualWidth);
					}
				}
			}
		}

		if (columnsTotal <= 0.0)
		{
			// Before any column has resolved (first pass, or no owner yet) fall back to the natural
			// measure rather than reporting zero, which would collapse the band.
			return base.MeasureOverride(availableSize);
		}

		// TODO Uno: Size is double-based in Uno; the float narrowing is kept to match C++ rounding.
		var width = (float)columnsTotal;
		var desired = base.MeasureOverride(new Size(width, availableSize.Height));
		return new Size(width, desired.Height);
	}

	internal void SetOwningTableViewInternal(TableView? owner)
	{
		// Weak: the TableView owns this container through the repeater, so a strong ref here would
		// be a cycle. Mirrors TableViewRow::SetOwningTableViewInternal.
		m_owningTableView = owner is not null ? new WeakReference<TableView>(owner) : null;
	}

	internal TableView? GetOwningTableView()
		=> m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner) ? owner : null;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_isEnabledChangedRevoker.Disposable = null;
		m_templateRootContextRequestedRevoker.Disposable = null;
		m_gridLineBorder = GetTemplateChild(s_GridLineBorderPartName) as Border;
		var weakThis = new WeakReference<TableViewGroupHeader>(this);

		// A group header's ContextFlyout (set through a TableViewGroupHeader Style) stays closed while
		// a cell is editing.
		m_templateRootContextRequestedRevoker.Disposable = TableViewDetails.HookTemplateRootContextRequested(this,
			(UIElement _, ContextRequestedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis) && strongThis.GetOwningTableView() is { } owner)
				{
					owner.SuppressContextRequestWhileEditing(args);
				}
			});

		// Keep CommonStates in sync with IsEnabled so Disabled activates when a consumer toggles it
		// at runtime, not only when it happens to be false at template time.
		DependencyPropertyChangedEventHandler handler = (object sender, DependencyPropertyChangedEventArgs args) =>
		{
			if (weakThis.TryGetTarget(out var strongThis))
			{
				strongThis.UpdateVisualStates(true /* useTransitions */);
				strongThis.UpdateDefaultCountOpacity();
			}
		};
		IsEnabledChanged += handler;
		m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= handler);

		UpdateVisualStates(false /* useTransitions */);
		UpdateTerminalBottomGridLineSuppression();
	}

	private void UpdateDefaultCountOpacity()
	{
		// The disabled brush already dims the default count; its Style opacity would dim it twice.
		if (IsEnabled)
		{
			if (m_defaultCount is { } enabledCount)
			{
				enabledCount.ClearValue(UIElement.OpacityProperty);
			}
			return;
		}

		TrackDefaultCount(ContentTemplateRoot);
		if (m_defaultCount is { } count)
		{
			count.Opacity = 1.0;
		}
	}

	private void TrackDefaultCount(UIElement? root)
	{
		if (root == GetInspectedContentRoot())
		{
			return;
		}

		if (m_defaultCount is { } previous)
		{
			previous.ClearValue(UIElement.OpacityProperty);
			m_defaultCount = null;
		}
		m_inspectedContentRoot = root is not null ? new WeakReference<UIElement>(root) : null;

		// Only the built-in template is touched: an app template's same-named element keeps its Opacity.
		if (root is FrameworkElement rootElement && rootElement.Name == "TableViewDefaultGroupHeaderContentRoot")
		{
			m_defaultCount = rootElement.FindName("TableViewGroupHeaderItemCountText") as TextBlock;
		}
	}

	internal void SetTerminalBottomGridLineSuppression(bool suppress)
	{
		// Re-applies on every call rather than returning early on an unchanged flag: the overlay is
		// also derived from BorderThickness, so a same-value push re-asserts it against the current
		// thickness.
		m_suppressBottomGridLine = suppress;
		UpdateTerminalBottomGridLineSuppression();
	}

	private void UpdateTerminalBottomGridLineSuppression()
	{
		if (m_gridLineBorder is { } gridLineBorder)
		{
			var thickness = BorderThickness;
			if (m_suppressBottomGridLine)
			{
				thickness.Bottom = 0.0;
			}
			if (gridLineBorder.BorderThickness != thickness)
			{
				gridLineBorder.BorderThickness = thickness;
			}
		}
	}

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// PrepareGroupHeaderElement assigns the projection, then pushes expansion via the DPs. A
		// projection swapped in later must not be left reporting stale expansion, so seed it here.
		SyncExpansionToContent();
	}

	private void SyncExpansionToContent()
	{
		if (Content is TableViewGroupInfo info)
		{
			info.SetExpansionInternal(IsExpandable, IsExpanded);
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == IsExpandedProperty || property == IsExpandableProperty)
		{
			// The DPs are authoritative: they drive the VisualStates and are mirrored onto the
			// projection here, so there is a single write path for expansion state.
			SyncExpansionToContent();
			UpdateVisualStates(true /* useTransitions */);
		}
	}

	internal void RaiseExpandCollapseStateChanged(ExpandCollapseState oldState, ExpandCollapseState newState)
	{
		if (!AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			return;
		}

		// FromElement only: no live peer means no listener, so creating one purely to announce would
		// materialize automation objects a client never asked for.
		var peer = FrameworkElementAutomationPeer.FromElement(this);

		if ((peer is not null ? peer as TableViewGroupHeaderAutomationPeer : null) is { } headerPeer)
		{
			headerPeer.RaiseExpandCollapseAutomationEvent(
				oldState, newState);
		}
	}

	private void UpdateVisualStates(bool useTransitions)
	{
		// Glyph, band chrome and chevron visibility are all declared in the ControlTemplate and
		// selected by state -- no visual property is assigned from here.
		VisualStateManager.GoToState(
			this,
			IsExpandable ? "Expandable" : "NotExpandable",
			useTransitions);

		VisualStateManager.GoToState(
			this,
			IsExpanded ? "Expanded" : "Collapsed",
			useTransitions);

		VisualStateManager.GoToState(
			this,
			!IsEnabled ? "Disabled" : (m_isPressed ? "Pressed" : (m_isPointerOver ? "PointerOver" : "Normal")),
			useTransitions);
	}

	internal void RequestToggle()
	{
		if (!IsExpandable)
		{
			return;
		}

		var info = Content as TableViewGroupInfo;
		var args = new TableViewGroupHeaderToggleRequestedEventArgs(info is not null ? info.Key : null);

		// Raised inline, deliberately. The owner resolves the target group synchronously and
		// defers only the structural reshape, so nothing is destroyed while XAML is still
		// unwinding pointer dispatch.
		//
		// Posting HERE is what created the bug it appeared to prevent: by the time a queued raise
		// ran, a scroll could have recycled this header onto another group, so the toggle hit
		// the wrong one -- or the header had left the viewport and it was dropped. Resolution has to
		// happen while the gesture's row index is still current, which means the raise cannot be
		// deferred; only the reshape can.
		//
		// Strong self across the raise keeps THIS object alive if a handler releases it mid-invoke.
		// It does not protect XAML's in-flight pointer dispatch -- that safety comes from the owner
		// deferring the reshape, and from RequestToggle being the last statement in
		// OnPointerReleased so nothing here touches a member after the raise.
		// `this` is already a strong GC reference for the duration of the call.
		// auto const strongThis = get_strong();
		// auto const self = strongThis.as<winrt::TableViewGroupHeader>();
		var self = this;

		ToggleRequested?.Invoke(self, args);
	}

	internal void RequestExpansion(bool expand)
	{
		if (!IsExpandable)
		{
			return;
		}

		// Mirror the ExpandCollapse peer: pass the direction through to the owner unresolved (the
		// mutation is idempotent and applied on a later turn) and resolve through the stored owner
		// rather than an ancestor walk, so a header that has been recycled/unparented still works.
		if (GetOwningTableView() is { } owner)
		{
			var self = this;
			owner.SetGroupExpansion(self, expand);
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		if (!args.Handled && IsExpandable)
		{
			var key = args.Key;
			bool isRtl = FlowDirection == FlowDirection.RightToLeft;

			switch (key)
			{
				case VirtualKey.Enter:
				case VirtualKey.Space:
					args.Handled = true;
					m_gestureFocusState = FocusState.Keyboard;
					RequestToggle();
					break;

				case VirtualKey.Right:
					// Right expands in LTR, collapses in RTL -- the TreeViewItem / Expander convention.
					args.Handled = true;
					m_gestureFocusState = FocusState.Keyboard;
					RequestExpansion(!isRtl);
					break;

				case VirtualKey.Left:
					args.Handled = true;
					m_gestureFocusState = FocusState.Keyboard;
					RequestExpansion(isRtl);
					break;

				default:
					break;
			}
		}

		base.OnKeyDown(args);
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
		// Only the pressing pointer may cancel the press (as TableViewRow): another contact or the mouse
		// leaving must not drop an in-flight tap.
		if (m_isPressed && args.Pointer.PointerId != m_pressedPointerId)
		{
			return;
		}
		m_isPointerOver = false;
		m_isPressed = false;
		m_pressedGroupIdentity = "";
		UpdateVisualStates(true /* useTransitions */);
	}

	internal void ResetPointerStateInternal()
	{
		// Hover is left alone: it describes where the pointer is, not which group it acted on.
		m_pressedGroupIdentity = "";
		if (m_isPressed)
		{
			m_isPressed = false;
			UpdateVisualStates(false /* useTransitions */);
		}
	}

	private string GetGroupIdentity()
	{
		if (GetOwningTableView() is { } owner)
		{
			return owner.GetContainerIdentityInternal(this);
		}
		return "";
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		base.OnPointerPressed(args);

		// Left-button only, matching TableViewRow. Pointer events fire for secondary and middle
		// buttons too, and the matching release would reach RequestToggle -- so without this a
		// right-click on the band would toggle the group and swallow the context-menu gesture.
		// Despite the name, IsLeftButtonPressed covers the primary action regardless of device.
		if (!args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			return;
		}

		m_isPressed = true;
		m_pressedPointerId = args.Pointer.PointerId;

		// A reshape between press and release can recycle this container onto another group.
		m_pressedGroupIdentity = GetGroupIdentity();

		// The band owns the gesture: mark it handled so a press on a group header never reaches the
		// row's selection handling.
		if (IsExpandable)
		{
			args.Handled = true;
		}

		UpdateVisualStates(true /* useTransitions */);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);

		bool wasPressed = m_isPressed;
		var pressedIdentity = m_pressedGroupIdentity;
		m_isPressed = false;
		m_pressedGroupIdentity = "";
		UpdateVisualStates(true /* useTransitions */);

		// Toggle on release-inside, the standard click semantic: a press that drags off the band
		// does not activate.
		if (!wasPressed || !m_isPointerOver || pressedIdentity != GetGroupIdentity())
		{
			return;
		}

		// Focus before the toggle so the toggle captures it for the restore after the reshape.
		// Focus on release, not press: leaving an open editor commits it and can reshape mid-gesture.
		if (Focus(FocusState.Pointer))
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.SetCellCursorActiveInternal(false);
			}
		}

		// The editor commit inside Focus() can reshape and recycle this header onto another group.
		if (pressedIdentity != GetGroupIdentity())
		{
			if (IsExpandable)
			{
				args.Handled = true;
			}
			return;
		}

		if (IsExpandable)
		{
			args.Handled = true;
			m_gestureFocusState = FocusState.Pointer;
			RequestToggle();
		}
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		base.OnPointerCaptureLost(args);
		if (m_isPressed && args.Pointer.PointerId != m_pressedPointerId)
		{
			return;
		}
		m_isPressed = false;
		m_isPointerOver = false;
		m_pressedGroupIdentity = "";
		UpdateVisualStates(true /* useTransitions */);
	}
}
