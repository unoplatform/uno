// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItem.cpp, commit 65718e2813

using System.Numerics;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Core;
using System;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Uno.Disposables;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using XamlWindow = Microsoft.UI.Xaml.Window;
using System.Reflection;
using MUII = Microsoft.UI.Input;
using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a single tab within a TabView.
/// </summary>
public partial class TabViewItem : ListViewItem
{
	private const string c_overlayCornerRadiusKey = "OverlayCornerRadius";
	private const int c_targetRectWidthIncrement = 2;
	private const double c_tabViewItemMouseDragThresholdMultiplier = 2.0;

	/// <summary>
	/// Initializes a new instance of the TabViewItem class.
	/// </summary>
	public TabViewItem()
	{
		m_dispatcherHelper = new(this);
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_TabViewItem);

		DefaultStyleKey = typeof(TabViewItem);

		SetValue(TabViewTemplateSettingsProperty, new TabViewItemTemplateSettings());

		Loaded += OnLoaded;

		SizeChanged += OnSizeChanged;

		RegisterPropertyChangedCallback(SelectorItem.IsSelectedProperty, OnIsSelectedPropertyChanged);
		RegisterPropertyChangedCallback(Control.ForegroundProperty, OnForegroundPropertyChanged);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_selectedBackgroundPathSizeChangedRevoker.Dispose();
		m_closeButtonClickRevoker.Dispose();
		m_tabDragStartingRevoker.Dispose();
		m_tabDragCompletedRevoker.Dispose();

		m_selectedBackgroundPath = GetTemplateChild<Path>(s_selectedBackgroundPathName);

		if (m_selectedBackgroundPath is { } selectedBackgroundPath)
		{
			void OnSizeChanged(object sender, object args) => UpdateSelectedBackgroundPathTranslateTransform();
			selectedBackgroundPath.SizeChanged += OnSizeChanged;
			m_selectedBackgroundPathSizeChangedRevoker.Disposable = Disposable.Create(() => selectedBackgroundPath.SizeChanged -= OnSizeChanged);
		}

		m_headerContentPresenter = GetTemplateChild<ContentPresenter>(s_contentPresenterName);

		var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
		var internalTabView = tabView ?? null;

		Button GetCloseButton(TabView internalTabView)
		{
			var closeButton = GetTemplateChild<Button>(s_closeButtonName);
			if (closeButton != null)
			{
				// Do localization for the close button automation name
				if (string.IsNullOrEmpty(AutomationProperties.GetName(closeButton)))
				{
					var closeButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewCloseButtonName);
					AutomationProperties.SetName(closeButton, closeButtonName);
				}

				if (internalTabView != null)
				{
					// Setup the tooltip for the close button
					var tooltip = new ToolTip();
					tooltip.Content = internalTabView.GetTabCloseButtonTooltipText();
					ToolTipService.SetToolTip(closeButton, tooltip);
				}

				closeButton.Click += OnCloseButtonClick;
			}
			return closeButton;
		}
		m_closeButton = GetCloseButton(internalTabView);

		OnHeaderChanged();
		OnIconSourceChanged();

		if (tabView != null)
		{
			tabView.TabDragStarting += OnTabDragStarting;
			tabView.TabDragCompleted += OnTabDragCompleted;
		}

		UpdateCloseButton();
		UpdateForeground();
		UpdateWidthModeVisualState();
		UpdateTabGeometry();
	}

	private void UpdateTabGeometry()
	{
		var templateSettings = TabViewTemplateSettings;
		var height = ActualHeight;
		var popupRadius = (CornerRadius)ResourceAccessor.ResourceLookup(this, c_overlayCornerRadiusKey);
		var leftCorner = popupRadius.TopLeft;
		var rightCorner = popupRadius.TopRight;

		// Assumes 4px curving-out corners, which are hardcoded in the markup
		var data = "<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>F1 M0,{0}  a 4,4 0 0 0 4,-4  L 4,{1}  a {2},{3} 0 0 1 {4},-{5}  l {6},0  a {7},{8} 0 0 1 {9},{10}  l 0,{11}  a 4,4 0 0 0 4,4 Z</Geometry>";

		var strOut = string.Format(
			CultureInfo.InvariantCulture,
			data,
			height,
			leftCorner,
			leftCorner,
			leftCorner,
			leftCorner,
			leftCorner,
			ActualWidth - (leftCorner + rightCorner),
			rightCorner,
			rightCorner,
			rightCorner,
			rightCorner,
			height - (4.0 + rightCorner));

		var geometry = XamlReader.Load(strOut) as Geometry;

		templateSettings.TabGeometry = geometry;
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		if (GetParentTabView() is TabView tabView)
		{
			var internalTabView = tabView;
			var index = internalTabView.IndexFromContainer(this);
			internalTabView.SetTabSeparatorOpacity(index);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		m_dispatcherHelper.RunAsync(() => UpdateTabGeometry());
	}

	private void OnIsSelectedPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
		if (peer != null)
		{
			peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
		}

		if (IsSelected)
		{
			SetValue(Canvas.ZIndexProperty, 20);
			StartBringTabIntoView();
		}
		else
		{
			SetValue(Canvas.ZIndexProperty, 0);
		}

		UpdateWidthModeVisualState();
		UpdateCloseButton();
		UpdateForeground();
	}

	private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty property)
	{
		UpdateForeground();
	}

	private void UpdateForeground()
	{
		// We only need to set the foreground state when the TabViewItem is in rest state and not selected.
		if (!IsSelected && !m_isPointerOver)
		{
			// If Foreground is set, then change icon and header foreground to match.
			VisualStateManager.GoToState(
				this,
				ReadLocalValue(ForegroundProperty) == DependencyProperty.UnsetValue ? s_foregroundSetStateName : s_foregroundNotSetStateName,
				false /*useTransitions*/);
		}
	}

	private void UpdateSelectedBackgroundPathTranslateTransform()
	{
		MUX_ASSERT(SharedHelpers.Is19H1OrHigher());

		if (m_selectedBackgroundPath is { } selectedBackgroundPath)
		{
			var selectedBackgroundPathActualOffset = selectedBackgroundPath.ActualOffset;
			var roundedSelectedBackgroundPathActualOffsetY = Math.Round(selectedBackgroundPathActualOffset.Y);

			if (roundedSelectedBackgroundPathActualOffsetY > selectedBackgroundPathActualOffset.Y)
			{
				// Move the SelectedBackgroundPath element down by a fraction of a pixel to avoid a faint gap line
				// between the selected TabViewItem and its content.
				TranslateTransform translateTransform = new();

				translateTransform.Y = (double)roundedSelectedBackgroundPathActualOffsetY - selectedBackgroundPathActualOffset.Y;

				selectedBackgroundPath.RenderTransform = translateTransform;
			}
			else if (selectedBackgroundPath.RenderTransform is not null)
			{
				// Reset any TranslateTransform that may have been set above.
				selectedBackgroundPath.RenderTransform = null;
			}
		}
	}

	private void OnTabDragStarting(object sender, TabViewTabDragStartingEventArgs args)
	{
		m_isBeingDragged = true;
	}

	private void OnTabDragCompleted(object sender, TabViewTabDragCompletedEventArgs args)
	{
		m_isBeingDragged = false;

		StopCheckingForDrag(m_dragPointerId);
		UpdateDragDropVisualState(false);
		UpdateForeground();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new TabViewItemAutomationPeer(this);
	}

	internal void OnCloseButtonOverlayModeChanged(TabViewCloseButtonOverlayMode mode)
	{
		m_closeButtonOverlayMode = mode;
		UpdateCloseButton();
	}

	internal TabView GetParentTabView()
	{
		return m_parentTabView;
	}

	internal void SetParentTabView(TabView tabView)
	{
		m_parentTabView = tabView;
	}

	internal void OnTabViewWidthModeChanged(TabViewWidthMode mode)
	{
		m_tabViewWidthMode = mode;
		UpdateWidthModeVisualState();
	}


	private void UpdateCloseButton()
	{
		if (!IsClosable)
		{
			VisualStateManager.GoToState(this, s_closeButtonCollapsedStateName, false);
		}
		else
		{
			switch (m_closeButtonOverlayMode)
			{
				case TabViewCloseButtonOverlayMode.OnPointerOver:
					{
						// If we only want to show the button on hover, we also show it when we are selected, otherwise hide it
						if (IsSelected || m_isPointerOver)
						{
							VisualStateManager.GoToState(this, s_closeButtonVisibleStateName, false);
						}
						else
						{
							VisualStateManager.GoToState(this, s_closeButtonCollapsedStateName, false);
						}
						break;
					}
				default:
					{
						// Default, use "Auto"
						VisualStateManager.GoToState(this, s_closeButtonVisibleStateName, false);
						break;
					}
			}
		}
	}

	private void UpdateWidthModeVisualState()
	{
		// Handling compact/non compact width mode
		if (!IsSelected && m_tabViewWidthMode == TabViewWidthMode.Compact)
		{
			VisualStateManager.GoToState(this, s_compactStateName, false);
		}
		else
		{
			VisualStateManager.GoToState(this, s_standardWidthStateName, false);
		}
	}

	private void UpdateDragDropVisualState(bool isVisible)
	{
		if (isVisible)
		{
			VisualStateManager.GoToState(this, s_dragDropVisualVisibleStateName, false);
		}
		else
		{
			VisualStateManager.GoToState(this, s_dragDropVisualNotVisibleStateName, false);
		}
	}

	private void RequestClose()
	{
		var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
		if (tabView != null)
		{
			var internalTabView = tabView;
			if (internalTabView != null)
			{
				internalTabView.RequestCloseTab(this, false);
			}
		}
	}

	internal void RaiseRequestClose(TabViewTabCloseRequestedEventArgs args)
	{
		// This should only be called from TabView, to ensure that both this event and the TabView TabRequestedClose event are raised
		CloseRequested?.Invoke(this, args);
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs args)
	{
		RequestClose();
	}

	private void OnIsClosablePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateCloseButton();
	}


	private void OnHeaderPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		OnHeaderChanged();
	}

	private void OnHeaderChanged()
	{
		if (m_headerContentPresenter is { } headerContentPresenter)
		{
			headerContentPresenter.Content = Header;
		}

		if (m_firstTimeSettingToolTip)
		{
			m_firstTimeSettingToolTip = false;

			if (ToolTipService.GetToolTip(this) == null)
			{
				// App author has not specified a tooltip; use our own
				ToolTip CreateToolTip()
				{
					var toolTip = new ToolTip();
					toolTip.Placement = PlacementMode.Mouse;
					ToolTipService.SetToolTip(this, toolTip);
					return toolTip;
				}

				m_toolTip = CreateToolTip();
			}
		}

		var toolTip = m_toolTip;
		if (toolTip != null)
		{
			// Update tooltip text to new header text
			var headerContent = Header;

			// Only show tooltip if header is a non-empty string.
			if (headerContent is string headerString && !string.IsNullOrEmpty(headerString))
			{
				toolTip.Content = headerString;
				toolTip.IsEnabled = true;
			}
			else
			{
				toolTip.Content = null;
				toolTip.IsEnabled = false;
			}
		}
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		var pointer = args.Pointer;
		var pointerDeviceType = pointer.PointerDeviceType;
		var pointerPoint = args.GetCurrentPoint(this);

		if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
		{
			if (pointerPoint.Properties.IsLeftButtonPressed)
			{
				m_lastPointerPressedPosition = pointerPoint.Position;

				BeginCheckingForDrag(pointer.PointerId);

				bool ctrlDown = (args.KeyModifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;

				if (ctrlDown)
				{
					IsSelected = true;

					// Return here so the base class will not pick it up, but let it remain unhandled so someone else could handle it.
					return;
				}
			}
		}
		else if (pointerDeviceType == PointerDeviceType.Touch)
		{
			m_lastPointerPressedPosition = pointerPoint.Position;

			BeginCheckingForDrag(pointer.PointerId);
		}

		base.OnPointerPressed(args);

		if (args.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
		{
			if (CapturePointer(pointer))
			{
				m_hasPointerCapture = true;
				m_isMiddlePointerButtonPressed = true;
			}
		}
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs args)
	{
		base.OnPointerMoved(args);

		if (ShouldStartDrag(args))
		{
			UpdateDragDropVisualState(true);
		}
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);

		var pointer = args.Pointer;

		StopCheckingForDrag(pointer.PointerId);
		UpdateDragDropVisualState(false);

		if (m_hasPointerCapture)
		{
			if (args.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
			{
				bool wasPressed = m_isMiddlePointerButtonPressed;
				m_isMiddlePointerButtonPressed = false;
				ReleasePointerCapture(pointer);

				if (wasPressed)
				{
					if (IsClosable)
					{
						RequestClose();
					}
				}
			}
		}
	}

	private bool IsOutsideDragRectangle(Point testPoint, Point dragRectangleCenter)
	{
		double dx = Math.Abs(testPoint.X - dragRectangleCenter.X);
		double dy = Math.Abs(testPoint.Y - dragRectangleCenter.Y);

		double maxDx = 4; //4 is default for GetSystemMetrics(SM_CXDRAG);
		double maxDy = 4; //4 is default for GetSystemMetrics(SM_CYDRAG);

		maxDx *= c_tabViewItemMouseDragThresholdMultiplier;
		maxDy *= c_tabViewItemMouseDragThresholdMultiplier;

		return (dx > maxDx || dy > maxDy);
	}

	private bool ShouldStartDrag(PointerRoutedEventArgs args) => m_isCheckingforDrag &&
			IsOutsideDragRectangle(args.GetCurrentPoint(this).Position, m_lastPointerPressedPosition) &&
			m_dragPointerId == args.Pointer.PointerId;

	private void BeginCheckingForDrag(uint pointerId)
	{
		m_dragPointerId = pointerId;
		m_isCheckingforDrag = true;
	}

	private void StopCheckingForDrag(uint pointerId)
	{
		if (m_isCheckingforDrag && m_dragPointerId == pointerId)
		{
			m_dragPointerId = 0;
			m_isCheckingforDrag = false;
		}
	}

	private void HideLeftAdjacentTabSeparator()
	{
		if (GetParentTabView() is TabView tabView)
		{
			var internalTabView = tabView;
			var index = internalTabView.IndexFromContainer(this);
			internalTabView.SetTabSeparatorOpacity(index - 1, 0);
		}
	}

	private void RestoreLeftAdjacentTabSeparatorVisibility()
	{
		if (GetParentTabView() is TabView tabView)
		{
			var internalTabView = tabView;
			var index = internalTabView.IndexFromContainer(this);
			internalTabView.SetTabSeparatorOpacity(index - 1);
		}
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);

		m_isPointerOver = true;

		if (m_hasPointerCapture)
		{
			m_isMiddlePointerButtonPressed = true;
		}

		UpdateCloseButton();
		HideLeftAdjacentTabSeparator();
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		base.OnPointerExited(args);

		m_isPointerOver = false;
		m_isMiddlePointerButtonPressed = false;

		UpdateCloseButton();
		UpdateForeground();
		RestoreLeftAdjacentTabSeparatorVisibility();
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		base.OnPointerCanceled(args);

		var pointer = args.Pointer;

		StopCheckingForDrag(pointer.PointerId);

		if (m_hasPointerCapture)
		{
			ReleasePointerCapture(pointer);
			m_isMiddlePointerButtonPressed = false;
		}

		RestoreLeftAdjacentTabSeparatorVisibility();
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		base.OnPointerCaptureLost(args);

		m_hasPointerCapture = false;
		m_isMiddlePointerButtonPressed = false;
		RestoreLeftAdjacentTabSeparatorVisibility();
	}

	// Note that the ItemsView will handle the left and right arrow keys if we don't do so before it does,
	// so this needs to be handled below the items view. That's why we can't put this in TabView's OnKeyDown.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		if (!args.Handled && (args.Key == VirtualKey.Left || args.Key == VirtualKey.Right))
		{
			// Alt+Shift+Arrow reorders tabs, so we don't want to handle that combination.
			// ListView also handles Alt+Arrow  (no Shift) by just doing regular XY focus,
			// same as how it handles Arrow without any modifier keys, so in that case
			// we do want to handle things so we get the improved keyboarding experience.
			var isAltDown = (MUII.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
			var isShiftDown = (MUII.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			if (!isAltDown || !isShiftDown)
			{
				var moveForward =
					(FlowDirection == FlowDirection.LeftToRight && args.Key == VirtualKey.Right) ||
					(FlowDirection == FlowDirection.RightToLeft && args.Key == VirtualKey.Left);

				args.Handled = GetParentTabView().MoveFocus(moveForward);
			}
		}

		if (!args.Handled)
		{
			base.OnKeyDown(args);
		}
	}

	private void OnIconSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		OnIconSourceChanged();
	}

	private void OnIconSourceChanged()
	{
		var templateSettings = TabViewTemplateSettings;
		var source = this.IconSource;
		if (source != null)
		{
			templateSettings.IconElement = SharedHelpers.MakeIconElementFrom(source);
			VisualStateManager.GoToState(this, s_iconStateName, false);
		}

		else
		{
			templateSettings.IconElement = null;
			VisualStateManager.GoToState(this, s_noIconStateName, false);
		}
	}

	internal void StartBringTabIntoView()
	{
		// we need to set the TargetRect to be slightly wider than the TabViewItem size in order to avoid cutting off the end of the Tab
		BringIntoViewOptions options = new();
		options.TargetRect = new Rect(0, 0, DesiredSize.Width + c_targetRectWidthIncrement, DesiredSize.Height);
		StartBringIntoView(options);
	}
}
