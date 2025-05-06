// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBar_Partial.h, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Uno.UI.Helpers.WinUI.LocalizedResource;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.System;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
{
	public AppBar()
	{
		m_Mode = AppBarMode.Inline;
		m_onLoadFocusState = FocusState.Unfocused;
		m_savedFocusState = FocusState.Unfocused;
		m_isInOverlayState = false;
		m_isChangingOpenedState = false;
		m_hasUpdatedTemplateSettings = false;
		m_minCompactHeight = 0d;
		m_compactHeight = 0d;
		m_minimalHeight = 0d;
		m_openedWithExpandButton = false;
		m_contentHeight = 0d;
		m_isOverlayVisible = false;

		DefaultStyleKey = typeof(AppBar);

#if HAS_UNO // Prepare state is not called automatically yet.
		PrepareState();
#endif
	}

	// TODO:MZ: Cleanup to unloaded
	//~AppBar()
	//{
	//	auto xamlRoot = XamlRoot::GetForElementStatic(this);
	//	if (m_xamlRootChangedEventHandler && xamlRoot)
	//	{
	//		VERIFYHR(m_xamlRootChangedEventHandler.DetachEventHandler(xamlRoot.Get()));
	//	}

	//	if (DXamlCore::GetCurrent() != nullptr)
	//	{
	//		VERIFYHR(BackButtonIntegration_UnregisterListener(this));
	//	}
	//}

	protected virtual void PrepareState()
	{
		//base.PrepareState();

		Loaded += OnLoaded;
		m_loadedEventHandler.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
		Unloaded += OnUnloaded;
		m_unloadedEventHandler.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);
		SizeChanged += OnSizeChanged;
		m_sizeChangedEventHandler.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		TemplateSettings = new AppBarTemplateSettings();
	}

	// Note that we need to wait for OnLoaded event to set focus.
	// When we get the on opened event children of AppBar will not be populated
	// yet which will prevent them from getting focus.
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (m_layoutUpdatedEventHandler.Disposable is null)
		{
			this.LayoutUpdated += OnLayoutUpdated;
			m_layoutUpdatedEventHandler.Disposable = Disposable.Create(() => LayoutUpdated -= OnLayoutUpdated);
		}

		//register for XamlRoot.Changed events
		var xamlRoot = XamlRoot.GetForElement(this);
		if (m_xamlRootChangedEventHandler.Disposable is null && xamlRoot is not null)
		{
			xamlRoot.Changed += OnXamlRootChanged;
			m_xamlRootChangedEventHandler.Disposable = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
		}

		// register the app bar if it is floating
		if (m_Mode == AppBarMode.Floating)
		{
			var xamlRootImpl = xamlRoot;
			var applicationBarService = xamlRootImpl.GetApplicationBarService();
			MUX_ASSERT(applicationBarService is not null);
			applicationBarService.RegisterApplicationBar(this, m_Mode);
		}

		// If it's a top or bottom bar, make sure the bounds are correct if we haven't set them yet
		if (m_Mode == AppBarMode.Top || m_Mode == AppBarMode.Bottom)
		{
			var xamlRootImpl = xamlRoot;
			var applicationBarService = xamlRootImpl.GetApplicationBarService();
			MUX_ASSERT(applicationBarService is not null);
			applicationBarService.OnBoundsChanged();
		}

		// OnIsOpenChanged handles focus and other changes
		bool isOpen = IsOpen;
		if (isOpen)
		{
			OnIsOpenChanged(true);
		}

		// Update the visual state to make sure our calculations for what
		// direction to open in are correct.
		UpdateVisualState();
	}

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		if (m_layoutUpdatedEventHandler.Disposable is not null)
		{
			m_layoutUpdatedEventHandler.Disposable = null;
		}

		if (m_isInOverlayState)
		{
			TeardownOverlayState();
		}

		if (m_Mode == AppBarMode.Floating)
		{
			if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
			{
				var applicationBarService = xamlRoot.GetApplicationBarService(applicationBarService);
				applicationBarService.UnregisterApplicationBar(this);
			}
		}

		// Make sure we're not still registered for back button events when no longer
		// in the tree.
		BackButtonIntegration.UnregisterListener(this);
	}

	private void OnLayoutUpdated(object sender, object args)
	{
		if (m_layoutTransitionElement is not null)
		{
			PositionLTEs();
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		RefreshContentHeight(null /*didChange*/);
		UpdateTemplateSettings();

		Page pageOwner;
		if (SUCCEEDED(GetOwner(&pageOwner)) && pageOwner)
		{
			pageOwner.AppBarClosedSizeChanged();
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == IsOpenProperty)
		{
			bool isOpen = (bool)args.NewValue;
			OnIsOpenChanged(isOpen);

			if (EventEnabledAppBarOpenBegin() && isOpen)
			{
				TraceAppBarOpenBegin((uint)(m_Mode));
			}
			if (EventEnabledAppBarClosedBegin() && !isOpen)
			{
				TraceAppBarClosedBegin((uint)(m_Mode));
			}

			OnIsOpenChangedForAutomation(args);

			if (EventEnabledAppBarOpenEnd() && isOpen)
			{
				TraceAppBarOpenEnd();
			}
			if (EventEnabledAppBarClosedEnd() && !isOpen)
			{
				TraceAppBarClosedEnd();
			}

			UpdateVisualState();
		}
		else if (args.Property == IsStickyProperty)
		{
			OnIsStickyChanged();
		}
		else if (args.Property == ClosedDisplayModeProperty)
		{
			if (m_Mode != AppBarMode.Inline)
			{
				if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
				{
					var applicationBarService = xamlRoot.GetApplicationBarService(applicationBarService);
					applicationBarService.HandleApplicationBarClosedDisplayModeChange(this, m_Mode);
				}
			}

			InvalidateMeasure();
			UpdateVisualState();
		}
		else if (args.Property == LightDismissOverlayModeProperty)
		{
			ReevaluateIsOverlayVisible();
		}
		else if (args.Property == IsEnabledProperty)
		{
			UpdateVisualState();
		}
	}

	private protected override void OnVisibilityChanged()
	{
		Page pageOwner;
		if (SUCCEEDED(GetOwner(&pageOwner)) && pageOwner)
		{
			pageOwner.AppBarClosedSizeChanged();
		}
	}

	protected override void OnApplyTemplate()
	{
		//TODO:MZ
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var returnValue = base.MeasureOverride(availableSize);

#if HAS_UNO
		if (_isNativeTemplate)
		{
			return returnValue;
		}
#endif

		if (m_Mode == AppBarMode.Top || m_Mode == AppBarMode.Bottom)
		{
			// regardless of what we desire, settings of alignment or fixed size content, we will always take up full width
			returnValue.Width = availableSize.Width;
		}

		// Make sure our returned height matches the configured state.
		var closedDisplayMode = ClosedDisplayMode;

		switch (closedDisplayMode)
		{
			case AppBarClosedDisplayMode.Compact:
				{
					double oldCompactHeight = CompactHeight;

					bool hasRightLabelDynamicPrimaryCommand = HasRightLabelDynamicPrimaryCommand;
					bool hasNonLabeledDynamicPrimaryCommand = HasNonLabeledDynamicPrimaryCommand;

					if (hasRightLabelDynamicPrimaryCommand || hasNonLabeledDynamicPrimaryCommand)
					{
						bool isOpen = IsOpen;
						if (!isOpen)
						{
							CompactHeight = Math.Max(MinCompactHeight, returnValue.Height);
						}
					}
					else
					{
						CompactHeight = MinCompactHeight;
					}

					double newCompactHeight = CompactHeight;

					if (oldCompactHeight != newCompactHeight)
					{
						UpdateTemplateSettings();
					}

					returnValue.Height = newCompactHeight;
					break;
				}
			case AppBarClosedDisplayMode.Minimal:
				{
					returnValue.Height = MinimalHeight;
					break;
				}
			default:
			case AppBarClosedDisplayMode.Hidden:
				{
					returnValue.Height = 0.0;
					break;
				}
		}

		return returnValue;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var arrangeSize = finalSize;
		var layoutRootDesiredSize = new Size();
		if (m_tpLayoutRoot is { })
		{
			layoutRootDesiredSize = m_tpLayoutRoot.DesiredSize;
		}
		else
		{
			layoutRootDesiredSize = arrangeSize;
		}

		var returnValue = base.ArrangeOverride(new Size(finalSize.Width, layoutRootDesiredSize.Height));

		returnValue.Height = arrangeSize.Height;

		return returnValue;
	}

	/// <summary>
	/// Invoked when the AppBar starts to change from hidden to visible, or starts to be first displayed.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnOpening(object e)
	{
		TryQueryDisplayModesStatesGroup();

		if (m_Mode == AppBarMode.Inline)
		{
			// If we're in a popup that is light-dismissable, then we don't want to set up
			// a light-dismiss layer - the popup will have its own light-dismiss layer,
			// and it can interfere with ours.
			var popupAncestor = Popup.GetClosestPopupAncestor(this);
			if (!popupAncestor || !popupAncestor.IsSelfOrAncestorLightDismiss())
			{
				if (!m_isInOverlayState)
				{
					if (IsInLiveTree)
					{
						// Setup our LTEs and light-dismiss layer.
						SetupOverlayState();

						if (m_isOverlayVisible)
						{
							PlayOverlayOpeningAnimation();
						}
					}
				}
			}

			bool isSticky = IsSticky;
			if (!isSticky)
			{
				SetFocusOnAppBar();
			}
		}
		else
		{
			// Pre-Threshold AppBars were hidden and would get added to the tree upon opening which
			// would invoke their loaded handlers to set focus.
			// In threshold, hidden appbars are always in the tree, so we have to simulate the same
			// behavior by focusing the appbar whenever it opens.
			var closedDisplayMode = ClosedDisplayMode;
			if (closedDisplayMode == AppBarClosedDisplayMode.Hidden)
			{
				ApplicationBarService applicationBarService;
				if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
				{
					applicationBarService = xamlRoot.GetApplicationBarService();
				}
				MUX_ASSERT(applicationBarService is not null);

				// Determine the focus state
				var focusState = (m_onLoadFocusState != FocusState.Unfocused ? m_onLoadFocusState : FocusState.Programmatic);
				applicationBarService.FocusApplicationBar(this, focusState);

				// Reset the saved focus state
				m_onLoadFocusState = FocusState.Unfocused;
			}
		}

		if (m_tpExpandButton is not null)
		{
			ButtonBase expandButtonBase = m_tpExpandButton;

			// Set a tooltip with "See Less" for the expand button.
			SetExpandButtonToolTip(expandButtonBase, true /*isAppBarExpanded*/);

			if (!m_hasExpandButtonCustomAutomationName)
			{
				// Update the localized accessibility name for expand button with the less app bar button.
				SetExpandButtonAutomationName(expandButtonBase, true /*isAppBarExpanded*/);
			}
		}

		// Request a play show sound for opened AppBar
		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Show, this);

		// Raise the event
		Opening?.Invoke(this, e);
	}

	/// <summary>
	/// Invoked when the AppBar changes from hidden to visible, or is first displayed.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnOpened(object e)
	{
		Opened?.Invoke(this, e);

		if (DXamlCore.Current.BackButtonSupported)
		{
			BackButtonIntegration.RegisterListener(this);
		}
	}

	/// <summary>
	/// Invoked when the AppBar starts to change from visible to hidden.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnClosing(object e)
	{
		m_openedWithExpandButton = false;

		if (m_Mode == AppBarMode.Inline)
		{
			// Only restore focus if this AppBar isn't in a flyout - if it is,
			// then focus will be restored when the flyout closes.
			// We'll interfere with that if we restore focus before that time.
			var popupAncestor = Popup.GetClosestPopupAncestor(this);
			if (!popupAncestor || !popupAncestor.IsFlyout())
			{
				RestoreSavedFocus();
			}

			if (m_isOverlayVisible && m_isInOverlayState)
			{
				PlayOverlayClosingAnimation();
			}
		}

		if (m_tpExpandButton is not null)
		{
			ButtonBase expandButtonBase = m_tpExpandButton;

			// Set a tooltip with "More options" for the expand button.
			SetExpandButtonToolTip(expandButtonBase, false /*isAppBarExpanded*/);

			if (!m_hasExpandButtonCustomAutomationName)
			{
				// Update the localized accessibility name for expand button with the more app bar button.
				SetExpandButtonAutomationName(expandButtonBase, false /*isAppBarExpanded*/);
			}
		}

		// Request a play hide sound for closed
		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Hide, this);

		// Raise the event
		Closing?.Invoke(this, e);
	}

	/// <summary>
	/// Invoked when the AppBar changes from visible to hidden.
	/// </summary>
	/// <param name="e">Event data for the event.</param>
	protected virtual void OnClosed(object e)
	{
		if (m_Mode == AppBarMode.Inline && m_isInOverlayState)
		{
			TeardownOverlayState();
		}

		// Raise the event
		Closed?.Invoke(this, e);

		BackButtonIntegration.UnregisterListener(this);
	}

	internal override TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject? focusedElement,
		DependencyObject? candidateTabStopElement,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope)
	{
		var result = new TabStopProcessingResult()
		{
			NewTabStop = null,
			IsOverriden = false,
		};

		if (m_Mode == AppBarMode.Inline)
		{
			var isOpen = IsOpen;
			var isSticky = IsSticky;

			// We don't override tab-stop behavior for closed or sticky appbars.
			if (!isOpen || isSticky)
			{
				return result;
			}

			var isAncestorOfFocusedElement = this.IsAncestorOf(focusedElement);
			var isAncestorOfCandidateElement = this.IsAncestorOf(candidateTabStopElement);

			// If the element losing focus is a child of the appbar and the element
			// we're losing focus to is not, then we override tab-stop to keep the
			// focus within the appbar.
			if (isAncestorOfFocusedElement && !isAncestorOfCandidateElement)
			{
				var newTabStop = isBackward ? FocusManager.FindLastFocusableElement(this) : FocusManager.FindFirstFocusableElement(this);

				if (newTabStop is { })
				{
					result.NewTabStop = newTabStop;
					result.IsOverriden = true;
				}
			}
		}

		return result;
	}

	private void OnContentRootSizeChanged(object sender, SizeChangedEventArgs args)
	{
		var didChange = RefreshContentHeight();

		if (didChange)
		{
			UpdateTemplateSettings();
		}
	}

	private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs e)
	{
		if (m_Mode == AppBarMode.Inline && !m_isChangingOpenedState)
		{
			TryDismissInlineAppBar();
		}
	}

	// floating appbars are managed through vsm. System appbars (as set by page) use
	// transitions that are triggered by layout to load, unload and move around.
	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);

		bool ignored = false;
		bool isEnabled = false;
		bool isOpen = false;

		var closedDisplayMode = AppBarClosedDisplayMode.Hidden;
		bool shouldOpenUp = false;

		isEnabled = IsEnabled;
		isOpen = IsOpen;
		closedDisplayMode = ClosedDisplayMode;

		// We only need to check this if we're going to an opened state.
		if (isOpen)
		{
			shouldOpenUp = GetShouldOpenUp();
		}

		// CommonStates
		GoToState(useTransitions, isEnabled ? "Normal" : "Disabled", out ignored);

		// FloatingStates
		if (m_Mode == AppBarMode.Floating)
		{
			GoToState(useTransitions, isOpen ? "FloatingVisible" : "FloatingHidden", out ignored);
		}

		// DockPositions
		switch (m_Mode)
		{
			case AppBarMode.Top:
				GoToState(useTransitions, "Top", out ignored);
				break;

			case AppBarMode.Bottom:
				GoToState(useTransitions, "Bottom", out ignored);
				break;

			default:
				GoToState(useTransitions, "Undocked", out ignored);
				break;
		}

		// DisplayModeStates
		var displayMode = closedDisplayMode switch
		{
			AppBarClosedDisplayMode.Compact => "Compact",
			AppBarClosedDisplayMode.Minimal => "Minimal",
			_ => "Hidden",
		};

		var placement = shouldOpenUp ? "Up" : "Down";
		var openState = string.Empty;

		if (isOpen)
		{
			openState = "Open";
		}
		else
		{
			openState = "Closed";
			placement = string.Empty;
		}

		ignored = GoToState(useTransitions, $"{displayMode}{openState}{placement}");
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		base.OnPointerPressed(e);

		var isOpen = IsOpen;
		if (isOpen)
		{
			var isSticky = IsSticky;

			if (!isSticky)
			{
				// If the app bar is in a modal-like state, then don't propagate pointer
				// events.
				e.Handled = true;
			}
		}
		else
		{
			var closedDisplayMode = ClosedDisplayMode;
			if (closedDisplayMode == AppBarClosedDisplayMode.Minimal)
			{
				IsOpen = true;
				e.Handled = true;
			}
		}
	}

	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);

		if (m_Mode != AppBarMode.Inline)
		{
			var pointerDeviceType = e.PointerDeviceType;
			if (pointerDeviceType != PointerDeviceType.Mouse)
			{
				return;
			}

			var isOpen = IsOpen;
			var isHandled = e.Handled;

			if (isOpen && !isHandled)
			{
				var applicationBarService;
				if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
				{
					applicationBarService = xamlRoot.GetApplicationBarService();
				}
				MUX_ASSERT(applicationBarService is not null);

				applicationBarService.SetFocusReturnState(xaml::FocusState_Pointer);
				IFC_RETURN(applicationBarService.ToggleApplicationBars());

				applicationBarService.ResetFocusReturnState();
				e.Handled = true;
			}
		}
	}

	private void OnIsStickyChanged()
	{
		if (m_Mode != AppBarMode.Inline)
		{
			var applicationBarService;
			if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
			{
				applicationBarService = xamlRoot.GetApplicationBarService();
			}

			// this function can be called before OnLoaded and therefore
			// it is possible (and OK) not to have appbarservice at this time
			if (applicationBarService is not null)
			{
				applicationBarService.UpdateDismissLayer();
			}
		}

		if (m_overlayElement is { })
		{
			var isSticky = IsSticky;
			m_overlayElement.IsHitTestVisible = !isSticky;
		}
	}

	private void OnIsOpenChanged(bool isOpen)
	{
		// If the AppBar is not live, then wait until it's loaded before
		// responding to changes to opened state and firing our Opening/Opened events.
		// Uno Specific: using IsLoaded instead of IsInLiveTree, which makes more sense because OnOpening (called below) -> SetupOverlayState expects OnApplyTemplate to have already been called
		if (!IsInLiveTree)
		{
			return;
		}

		if (m_Mode != AppBarMode.Inline)
		{
			ApplicationBarService? applicationBarService;
			if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
			{
				applicationBarService = xamlRoot.GetApplicationBarService();
			}

			MUX_ASSERT(applicationBarService is not null);
			var hasFocus = this.HasFocus();

			if (isOpen)
			{
				applicationBarService.SaveCurrentFocusedElement(this);
				applicationBarService.OpenApplicationBar(this, m_Mode);

				// If the AppBar does not already have focus (i.e. it was opened programmatically),
				// then focus the AppBar.
				if (!hasFocus)
				{
					applicationBarService.FocusApplicationBar(this, FocusState.Programmatic);
				}
			}
			else
			{
				applicationBarService.CloseApplicationBar(this, m_Mode);

				// Only restore the focus to the saved element if we have the focus just before closing.
				// For CommandBar, we also check if the Overflow has focus in the override method "HasFocus"
				if (hasFocus)
				{
					applicationBarService.FocusSavedElement(this);
				}
			}

			applicationBarService.UpdateDismissLayer();
		}

		// Flag that we're transitions between opened & closed states.
		m_isChangingOpenedState = true;

		// Fire our Opening/Closing events.  If we're a legacy app or a badly
		// re-templated app, then fire the Opened/Closed events as well.
		{
			var routedEventArgs = new RoutedEventArgs(this);

			if (isOpen)
			{
				OnOpening(routedEventArgs);
			}
			else
			{
				OnClosing(routedEventArgs);
			}

			// We only query the display modes visual state group for post-WinBlue AppBars
			// so in cases where we don't have it (either via re-templating or legacy apps)
			// fire the Opening/Closing & Opened/Closed events immediately.
			// For WinBlue apps, firing the Opening/Closing events as well doesn't
			// matter because Blue apps wouldn't have had access to them.
			// For post-WinBlue AppBars, we fire the Opening/Closing & Opened/Closed
			// events based on our display mode state transitions.
			if (m_tpDisplayModesStateGroup is null)
			{
				if (isOpen)
				{
					OnOpened(routedEventArgs);
				}
				else
				{
					OnClosed(routedEventArgs);
				}
			}
		}
	}

	private void OnIsOpenChangedForAutomation(DependencyPropertyChangedEventArgs args)
	{
		var isOpen = (bool)args.NewValue;
		bool bAutomationListener = false;

		if (isOpen)
		{
			AutomationPeer.RaiseEventIfListener(this, AutomationEvents.MenuOpened);
		}
		else
		{
			AutomationPeer.RaiseEventIfListener(this, AutomationEvents.MenuClosed);
		}

		// Raise ToggleState Property change event for Automation clients if they are listening for property changed events.
		bAutomationListener = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
		if (bAutomationListener)
		{
			var automationPeer = GetOrCreateAutomationPeer();
			if (automationPeer is AppBarAutomationPeer applicationBarAutomationPeer)
			{
				applicationBarAutomationPeer.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
				applicationBarAutomationPeer.RaiseExpandCollapseAutomationEvent(isOpen);
			}

		}
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new AppBarAutomationPeer(this);

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		//Ignore already handled events
		bool isHandled = args.Handled;
		if (isHandled)
		{
			return;
		}

		var key = args.Key;
		if (key == VirtualKey.Escape)
		{
			bool isAnyAppBarClosed = false;

			if (m_Mode == AppBarMode.Inline)
			{
				isAnyAppBarClosed = TryDismissInlineAppBar();
			}
			else
			{
				bool isSticky = IsSticky;

				// If we have focus and the app bar is not sticky close all light-dismiss app bars on ESC
				ApplicationBarService? applicationBarService;
				if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
				{
					applicationBarService = xamlRoot.GetApplicationBarService();
				}
				MUX_ASSERT(applicationBarService is not null);

				bool hasFocus = this.HasFocus();
				if (hasFocus)
				{
					isAnyAppBarClosed = applicationBarService.CloseAllNonStickyAppBars();

					if (isSticky)
					{
						// If the appbar is sticky restore focus to the saved element without closing the appbar
						applicationBarService.SetFocusReturnState(FocusState.Keyboard);
						applicationBarService.FocusSavedElement(this);
						applicationBarService.ResetFocusReturnState();
					}
				}
			}

			args.Handled = isAnyAppBarClosed;
		}
	}

	public void SetOwner(Page pOwner)
	{
		if (m_wpOwner is not null)
		{
			WeakReferencePool.ReturnWeakReference(this, m_wpOwner);
			m_wpOwner = null;
		}
		m_wpOwner = WeakReferencePool.RentWeakReference(this, pOwner);
	}

	public Page? GetOwner()
	{
		if (m_wpOwner != null && m_wpOwner.IsAlive && m_wpOwner.Target is Page pageOwner)
		{
			return pageOwner;
		}

		return null;
	}

	protected virtual bool ContainsElement(DependencyObject pElement)
	{
		// For AppBar, ContainsElement is equivalent to IsAncestorOf.
		// However, ContainsElement is a virtual method, and CommandBar's
		// implementation of it also checks the overflow popup separately from
		// IsAncestorOf since the popup isn't part of the same visual tree.
		return this.IsAncestorOf(pElement);
	}

	protected bool IsExpandButton(UIElement element) => 
		m_tpExpandButton is { } && element == m_tpExpandButton;

	private void OnExpandButtonClick(object sender, RoutedEventArgs e)
	{
		bool bIsOpen = IsOpen;

		if (!bIsOpen)
		{
			m_openedWithExpandButton = true;
		}

		IsOpen = !bIsOpen;
	}

	private void OnDisplayModesStateChanged(object sender, VisualStateChangedEventArgs pArgs)
	{
		// We only fire the opened/closed events if we're changing our opened state (either
		// from open to closed or closed to open).  We don't fire the event if we changed
		// between 2 opened states or 2 closed states such as might happen when changing
		// closed display mode.
		if (m_isChangingOpenedState)
		{
			// Create the event args we'll use for our Opened/Closed events.
			var routedEventArgs = new RoutedEventArgs(this);

			var isOpen = IsOpen;

			if (isOpen)
			{
				OnOpened(routedEventArgs);
			}
			else
			{
				OnClosed(routedEventArgs);
			}

			m_isChangingOpenedState = false;
		}
	}

	protected virtual void UpdateTemplateSettings()
	{
		//
		// AppBar/CommandBar TemplateSettings and how they're used.
		//
		// The template settings are core to acheiving the desired experience
		// for AppBar/CommandBar at least to how it relates to the various
		// ClosedDisplayModes.
		//
		// This comment block will describe how the code uses TemplateSettings
		// to achieve the desired bar interation experience which is controlled
		// via the ClosedDisplayMode property.
		//
		// Anatomy of the bar component of an AppBar/CommandBar:
		//
		//  !==================================================!
		//  !                  Clip Rectangle                  !
		//  !                                                  !
		//  ! |----------------------------------------------| !
		//  ! |                                              | !
		//  ! |                 Content Root                 | !
		//  ! |                                              | !
		//  !=|==============================================|=!
		//    |::::::::::::::::::::::::::::::::::::::::::::::|
		//    |::::::::::::::::::::::::::::::::::::::::::::::|
		//    |----------------------------------------------|
		//
		// * The region covered in '::' is clipped away.
		//
		// ** The diagram shows the clip rect wider than the content, but
		//    that is just done to make it more readable.  In reality, they
		//    are the same width.
		//
		// When we measure and arrange an AppBar/CommandBar, the size we return
		// as our desired sized (in the case of measure) and the final size
		// (in the case of arrange) depends on the closed display mode.  We
		// measure our sub-tree normally but we modify the returned height to
		// make it match our closed display mode.
		//
		// This causes the control to get arranged such that the top portion
		// of the content root that is within our closed display mode height
		// will be visible, while the rest that is below will get covered up
		// by other content.  It's similar to if we had a negative margin on
		// the bottom.
		//
		// The clip rectangle is then used to make sure this bottom portion does
		// not get rendered; so we are left with just the top portion representing
		// our closed display mode.
		//
		// This is where the template settings start to play a part.  We need
		// to make sure to translate the clip rectangle up by a value that is equal
		// to the difference between the content's height and our closed display
		// mode height.  Since we want to translate up, we have to make that value
		// negative, which results in this equation:
		//
		//      VerticalDelta = ClosedDisplayMode_Height - ContentHeight
		//
		// This value is calculated for each of our supported ClosedDisplayModes
		// and is then used in our template & VSM to create the Closed/OpenUp/OpenDown
		// experiences.
		//
		// We apply it in the following ways to achieve our various states:
		//
		//     Closed:
		//      - Clip Rectangle translated by VerticalDelta (essentially translated up).
		//      - Content Root not translated.
		//
		//     OpenUp:
		//      - Clip Rectangle translated by VerticalDelta (essentially translated up).
		//      - Content Root translated by VerticalDelta (essentially translated up).
		//
		//     OpenDown:
		//      - Clip Rectangle not translated.
		//      - Content Root not translated.
		//

		var templateSettings = TemplateSettings;

		var actualWidth = ActualWidth;

		var contentHeight = ContentHeight;

		templateSettings.ClipRect = new Rect(0, 0, actualWidth, contentHeight);

		double compactVerticalDelta = CompactHeight - contentHeight;
		templateSettings.CompactVerticalDelta = compactVerticalDelta;
		templateSettings.NegativeCompactVerticalDelta = -compactVerticalDelta;

		double minimalVerticalDelta = MinimalHeight - contentHeight;
		templateSettings.MinimalVerticalDelta = minimalVerticalDelta;
		templateSettings.NegativeMinimalVerticalDelta = -minimalVerticalDelta;

		templateSettings.HiddenVerticalDelta = -contentHeight;
		templateSettings.NegativeHiddenVerticalDelta = contentHeight;

		if (m_hasUpdatedTemplateSettings)
		{
			UpdateVisualState();

			// We wait until after the first call to update template settings to query DisplayModesStates VSG
			// to to prevent a performance hit on app startup
			TryQueryDisplayModesStatesGroup();

			// Force animations that reference our template settings in the current visual state
			// to update their bindings.
			if (m_tpDisplayModesStateGroup is not null)
			{
				var currentState = m_tpDisplayModesStateGroup.CurrentState;

				if (currentState is { })
				{
					var storyboard = currentState.Storyboard;
					if (storyboard is { })
					{
						storyboard.SkipToFill();
					}
				}
			}
		}
		m_hasUpdatedTemplateSettings = true;
	}

	protected bool GetShouldOpenUp()
	{
		// Bottom appbars always open up. All other appbars by default open down
		bool shouldOpenUp = m_Mode == AppBarMode.Bottom;

		/// If the appbar is inline, check to see if opening in the default direction would cause
		// the appbar to appear partially offscreen and if so switch to opening the other way instead.
		if (m_Mode == AppBarMode.Inline)
		{
			bool hasSpaceToOpenDown = HasSpaceForAppBarToOpenDown();

			// Since we open down by default, we'll open up only if we *don't* have space in the down direction.
			shouldOpenUp = !hasSpaceToOpenDown;
		}

		return shouldOpenUp;
	}

	private protected virtual bool HasRightLabelDynamicPrimaryCommand() => false;

	private protected virtual bool HasNonLabeledDynamicPrimaryCommand() => false;

	// If the appbar is inline, check to see if opening in the default direction would cause the appbar to appear partially
	// offscreen.
	private bool HasSpaceForAppBarToOpenDown()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);

		GeneralTransform transform = TransformToVisual(null);

		double contentHeight = ContentHeight;

		// Subtract layout bounds to avoid using the System Tray area to open the AppBar.
		Point bottomOfExpandedAppBar = transform.TransformPoint(new (0, contentHeight));

		Rect windowBounds = DXamlCore.Current.GetContentBoundsForElement(this);
		Rect layoutBounds = DXamlCore.Current.GetContentLayoutBoundsForElement(this);

		// Convert the layout bounds X/Y offsets from screen coorindates into window coordinates.
		layoutBounds.X -= windowBounds.X;
		layoutBounds.Y -= windowBounds.Y;

		// Pixel rounding can sometimes cause the bounds and AppBar size to be off by a pixel when we expect them to be equal.
		// To account for that possibility, we'll allow the AppBar to open down if its height is at most one pixel greater
		// than the layout bounds height, after rounding the values to the nearest integer.
		var hasSpace = (XcpRound(bottomOfExpandedAppBar.Y) <= XcpRound(layoutBounds.Y + layoutBounds.Height + 1));
		return hasSpace;
	}

	internal bool TryDismissInlineAppBar()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);

		bool isAppBarDismissed = false;

		var isSticky = IsSticky;
		if (!isSticky)
		{
			var isOpen = IsOpen;
			if (isOpen)
			{
				isAppBarDismissed = true;
				IsOpen = false;
			}
		}

		return isAppBarDismissed;
	}

	private void SetFocusOnAppBar()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);

		var focusedElement = this.GetFocusedElement();

		var isAncestorOf = this.IsAncestorOf(focusedElement);

		// Only steal focus if focus isn't already within the appbar.
		if (!isAncestorOf)
		{
			m_savedFocusedElementWeakRef = WeakReferencePool.RentWeakReference(this, focusedElement);

			if (focusedElement is Control focusedElementAsControl && focusedElementAsControl.FocusState != FocusState.Unfocused)
			{
				m_savedFocusState = focusedElementAsControl.FocusState;
			}
			else
			{
				m_savedFocusState = FocusState.Programmatic;
			}

			// Now focus the first-focusable element in the appbar.
			var firstFocusableElement = FocusManager.FindFirstFocusableElement(this);
			if (firstFocusableElement is { })
			{
				this.SetFocusedElement(firstFocusableElement, m_savedFocusState, animateIfBringIntoView: false);
			}
		}
	}

	private void RestoreSavedFocus()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);

		DependencyObject? savedFocusedElement = null;

		if (m_savedFocusedElementWeakRef is not null && m_savedFocusedElementWeakRef.IsAlive && m_savedFocusedElementWeakRef.Target is DependencyObject savedFocusedElementTarget)
		{
			savedFocusedElement = savedFocusedElementTarget;
		}

		RestoreSavedFocusImpl(savedFocusedElement, m_savedFocusState);

		WeakReferencePool.ReturnWeakReference(this, m_savedFocusedElementWeakRef);
		m_savedFocusedElementWeakRef = null;

		m_savedFocusState = FocusState.Unfocused;
	}

	private void RestoreSavedFocusImpl(DependencyObject? savedFocusedElement, FocusState savedFocusState)
	{
		if (savedFocusedElement is not null)
		{
			this.SetFocusedElement(savedFocusedElement, m_savedFocusState, false /*animateIfBringIntoView*/);
		}
	}

	private bool RefreshContentHeight()
	{
		double oldHeight = m_contentHeight;

		if (m_tpContentRoot is { })
		{
			var newHeight = m_tpContentRoot.ActualHeight;
			ContentHeight = newHeight;
		}

		return oldHeight != ContentHeight;
	}

	// Sets the ExpandButton's automation name to:
	// - "More options" or "Less app bar" depending on whether the AppBar/CommandBar is expanded or not, when the AppBar/CommandBar has no automation name,
	// - "More options for <AppBar_automation_name>" or "Less app bar for <AppBar_automation_name>" depending on whether the AppBar/CommandBar is expanded or not, when the AppBar/CommandBar has an automation name.
	private void SetExpandButtonAutomationName(ButtonBase expandButton, bool isAppBarExpanded)
	{
		MUX_ASSERT(!m_hasExpandButtonCustomAutomationName);

		string appBarAutomationName = AutomationProperties.GetName(this);

		string expandButtonAutomationNameResourceId;

		if (appBarAutomationName == null)
		{
			expandButtonAutomationNameResourceId = isAppBarExpanded ? UIA_LESS_BUTTON : UIA_MORE_BUTTON;
		}
		else
		{
			expandButtonAutomationNameResourceId = isAppBarExpanded ? UIA_LESS_BUTTON_FOR_OWNER : UIA_MORE_BUTTON_FOR_OWNER;
		}

		string expandButtonAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(expandButtonAutomationNameResourceId);

		if (appBarAutomationName != null)
		{
			// Replace %s in expandButtonAutomationName with appBarAutomationName
			char buffer[1024];
			int len = swprintf_s(
			   buffer, ARRAYSIZE(buffer),
			   expandButtonAutomationName.GetRawBuffer(null),
			   appBarAutomationName.GetRawBuffer(null));

			IFCEXPECT_RETURN(len >= 0);

			expandButtonAutomationName.Set(buffer);
		}

		AutomationProperties.SetName(expandButton, expandButtonAutomationName);
	}

	// Sets the ExpandButton's tooltip text to "See less" or "See more" depending on whether the AppBar/CommandBar is expanded or not.
	private void SetExpandButtonToolTip(ButtonBase expandButton, bool isAppBarExpanded)
	{
		string toolTipText = DXamlCore.Current.GetLocalizedResourceString(isAppBarExpanded ? TEXT_HUB_SEE_LESS : TEXT_HUB_SEE_MORE);

		expandButton.SetValue(ToolTipService.ToolTipProperty, toolTipText);
	}

	private void SetupOverlayState()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);
		MUX_ASSERT(!m_isInOverlayState);

		// The approach used to achieve light-dismiss is to create a 1x1 element that is added
		// as the first child of our layout root panel.  Adding it as the first child ensures that
		// it is below our actual content and will therefore not affect the content area's hit-testing.
		// We then use a scale transform to scale up an LTE targeted to the element to match the
		// dimensions of our window.  Finally, we translate that same LTE to make sure it's upper-left
		// corner aligns with the window's upper left corner, causing it to cover the entire window.
		// A pointer pressed handler is attached to the element to intercept any pointer
		// input that is not directed at the actual content.  The value of AppBar.IsSticky controls
		// whether the light-dismiss element is hit-testable (IsSticky=True . hit-testable=False).
		// The pointer pressed handler simply closes the appbar and marks the routed event args
		// message as handled.
		if (m_tpLayoutRoot is null)
		{
			// Create our overlay element if necessary.
			if (m_overlayElement is null)
			{
				Rectangle rectangle = new();
				rectangle.Width = 1;
				rectangle.Height = 1;
				rectangle.UseLayoutRounding = false;

				bool isSticky = IsSticky;
				rectangle.IsHitTestVisible = !isSticky;

				rectangle.PointerPressed += OnOverlayElementPointerPressed;
				m_overlayElementPointerPressedEventHandler.Disposable = Disposable.Create(() => rectangle.PointerPressed -= OnOverlayElementPointerPressed);

				m_overlayElement = rectangle;

				UpdateOverlayElementBrush();
			}

			// Add our overlay element to our layout root panel.
			var layoutRootChildren = m_tpLayoutRoot.Children;
			layoutRootChildren.InsertAt(0, m_overlayElement);
		}

		CreateLTEs();

		// Update the animations to target the newly created overlay element LTE.
		if (m_isOverlayVisible)
		{
			UpdateTargetForOverlayAnimations();
		}

		m_isInOverlayState = true;
	}

	private void TeardownOverlayState()
	{
		MUX_ASSERT(m_Mode == AppBarMode.Inline);
		MUX_ASSERT(m_isInOverlayState);

		DestroyLTEs();

		// Remove our light-dismiss element from our layout root panel.
		if (m_tpLayoutRoot is not null)
		{
			var layoutRootChildren = m_tpLayoutRoot.Children;

			var indexOfOverlayElement = layoutRootChildren.IndexOf(m_overlayElement);
			bool wasFound = indexOfOverlayElement >= 0;

			MUX_ASSERT(wasFound);
			layoutRootChildren.RemoveAt(indexOfOverlayElement);
		}

		m_isInOverlayState = false;
	}

	private void CreateLTEs()
	{
		MUX_ASSERT(m_layoutTransitionElement is null);
		MUX_ASSERT(m_overlayLayoutTransitionElement is null);
		MUX_ASSERT(m_parentElementForLTEs is null);

		// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
		// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
		// which is lower in z-order than these other roots.
		if (ShouldUseParentedLTE())
		{
			IDependencyObject parent;
			VisualTreeHelper.GetParentStatic(this, &parent);
			IFCEXPECT_RETURN(parent);

			SetPtrValueWithQI(m_parentElementForLTEs, parent);
		}

		if (m_overlayElement)
		{
			// Create an LTE for our overlay element.
			(CoreImports.LayoutTransitionElement_Create(
				DXamlCore.GetCurrent().GetHandle(),
				m_overlayElement.Cast<FrameworkElement>().GetHandle(),
				m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>().GetHandle() : null,
				false /*isAbsolutelyPositioned*/,
				m_overlayLayoutTransitionElement.ReleaseAndGetAddressOf()
			));

			// Configure the overlay LTE.
			{
				DependencyObject overlayLTEPeer;
				DXamlCore.GetCurrent().GetPeer(m_overlayLayoutTransitionElement, &overlayLTEPeer);

				wf.Rect windowBounds = default;
				DXamlCore.GetCurrent().GetContentBoundsForElement(GetHandle(), &windowBounds);

				CompositeTransform compositeTransform;
				compositeTransform = new();

				compositeTransform.ScaleX = windowBounds.Width;
				compositeTransform.ScaleY = windowBounds.Height;

				overlayLTEPeer.Cast<UIElement>().RenderTransform = compositeTransform;

				xaml_media.IGeneralTransform transformToVisual;
				m_overlayElement.Cast<FrameworkElement>().TransformToVisual(null, &transformToVisual);

				wf.Point offsetFromRoot = default;
				transformToVisual.TransformPoint({ 0, 0 }, &offsetFromRoot);

				var flowDirection = FlowDirection_LeftToRight;
				get_FlowDirection(&flowDirection);

				// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
				// or the top-right corner of the window (for RTL cases).
				// TransformToVisual(null) will return an offset relative to the top-left corner of the window regardless of
				// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
				// to the right edge of the window.
				compositeTransform.TranslateX = flowDirection == FlowDirection_LeftToRight ? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width;
				compositeTransform.TranslateY = -offsetFromRoot.Y;
			}
		}

	(CoreImports.LayoutTransitionElement_Create(
		DXamlCore.GetCurrent().GetHandle(),
		GetHandle(),
		m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>().GetHandle() : null,
		false /*isAbsolutelyPositioned*/,
		m_layoutTransitionElement.ReleaseAndGetAddressOf()
	));

		// Forward our control's opacity to the LTE since it doesn't happen automatically.
		{
			double opacity = 0.0;
			get_Opacity(&opacity);
			m_layoutTransitionElement.SetValueByKnownIndex(KnownPropertyIndex.UIElement_Opacity, (float)(opacity));
		}

		PositionLTEs();

		return S_OK;
	}

	private void PositionLTEs()
	{
		MUX_ASSERT(m_layoutTransitionElement);

		IDependencyObject parentDO;
		UIElement parent;

		VisualTreeHelper.GetParent(this, &parentDO);

		// If we don't have a parent, then there's nothing for us to do.
		if (parentDO)
		{
			parentDO.As(&parent);

			xaml_media.IGeneralTransform transform;
			TransformToVisual(parent.Cast<UIElement>(), &transform);

			wf.Point offset = default;
			transform.TransformPoint({ 0, 0 }, &offset);

			CoreImports.LayoutTransitionElement_SetDestinationOffset(m_layoutTransitionElement, offset.X, offset.Y);
		}
	}

	private void DestroyLTEs()
	{
		if (m_layoutTransitionElement)
		{
			(CoreImports.LayoutTransitionElement_Destroy(
				DXamlCore.GetCurrent().GetHandle(),
				GetHandle(),
				m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>().GetHandle() : null,
				m_layoutTransitionElement
			));

			m_layoutTransitionElement.reset();
		}

		if (m_overlayLayoutTransitionElement)
		{
			// Destroy our light-dismiss element's LTE.
			(CoreImports.LayoutTransitionElement_Destroy(
				DXamlCore.GetCurrent().GetHandle(),
				m_overlayElement.Cast<FrameworkElement>().GetHandle(),
				m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>().GetHandle() : null,
				m_overlayLayoutTransitionElement
				));

			m_overlayLayoutTransitionElement.reset();
		}

		m_parentElementForLTEs.Clear();
	}

	private void OnOverlayElementPointerPressed(object /*pSender*/, xaml_input.IPointerRoutedEventArgs* pArgs)
	{
		MUX_ASSERT(m_Mode == AppBarMode_Inline);

		TryDismissInlineAppBar();
		pArgs.Handled = true;

		return S_OK;
	}

	private void TryQueryDisplayModesStatesGroup()
	{
		if (!m_tpDisplayModesStateGroup)
		{
			IVisualStateGroup displayModesStateGroup;
			GetTemplatePart<IVisualStateGroup>("DisplayModeStates", displayModesStateGroup.ReleaseAndGetAddressOf());
			m_tpDisplayModesStateGroup = displayModesStateGroup;

			if (m_tpDisplayModesStateGroup)
			{
				m_displayModeStateChangedEventHandler.AttachEventHandler(m_tpDisplayModesStateGroup, std.bind(&AppBar.OnDisplayModesStateChanged, this, _1, _2));
			}
		}
	}

	private bool ShouldUseParentedLTE()
	{
		IDependencyObject rootDO;
		if (SUCCEEDED(VisualTreeHelper.GetRootStatic(this, &rootDO)) && rootDO)
		{
			PopupRoot popupRoot;
			FullWindowMediaRoot mediaRoot;

			if (SUCCEEDED(rootDO.As(&popupRoot)) && popupRoot)
			{
				return true;
			}
			else if (SUCCEEDED(rootDO.As(&mediaRoot)) && mediaRoot)
			{
				return true;
			}
		}

		return false;
	}

	private void OnBackButtonPressedImpl(out bool* pHandled)
	{
		bool isOpen = false;
		bool isSticky = false;

		IFCPTR_RETURN(pHandled);

		get_IsOpen(&isOpen);
		get_IsSticky(&isSticky);
		if (isOpen && !isSticky)
		{
			put_IsOpen(false);
			*pHandled = true;

			if (m_Mode != AppBarMode_Inline)
			{
				IApplicationBarService applicationBarService;
				if (var xamlRoot = XamlRoot.GetImplementationForElementStatic(this))
            {
					xamlRoot.GetApplicationBarService(applicationBarService);
				}
				MUX_ASSERT(applicationBarService);
				applicationBarService.CloseAllNonStickyAppBars();
			}
		}

		return S_OK;
	}

	private void ReevaluateIsOverlayVisible()
	{
		bool isOverlayVisible = false;
		LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(this, &isOverlayVisible);

		// Only inline app bars can enable their overlays.  Top/Bottom/Floating will use
		// the overlay from the ApplicationBarService.
		isOverlayVisible &= (m_Mode == AppBarMode.Inline);

		if (isOverlayVisible != m_isOverlayVisible)
		{
			m_isOverlayVisible = isOverlayVisible;

			if (m_isOverlayVisible)
			{
				if (m_isInOverlayState)
				{
					UpdateTargetForOverlayAnimations();
				}
			}
			else
			{
				// Make sure we've stopped our animations.
				if (m_overlayOpeningStoryboard)
				{
					m_overlayOpeningStoryboard.Stop();
				}

				if (m_overlayClosingStoryboard)
				{
					m_overlayClosingStoryboard.Stop();
				}
			}

			if (m_overlayElement)
			{
				UpdateOverlayElementBrush();
			}
		}

		return S_OK;
	}

	private void
	AppBar.UpdateOverlayElementBrush()
	{
		MUX_ASSERT(m_overlayElement);

		if (m_isOverlayVisible)
		{
			// Create a theme resource for the overlay brush.
			var core = DXamlCore.GetCurrent().GetHandle();
			var dictionary = core.GetThemeResources();

			xstring_ptr themeBrush;
			xstring_ptr.CloneBuffer("AppBarLightDismissOverlayBackground", &themeBrush);

			DependencyObject* initialValueNoRef = null;
			dictionary.GetKeyNoRef(themeBrush, &initialValueNoRef);

			CREATEPARAMETERS cp(core);
			xref_ptr<CThemeResourceExtension> themeResourceExtension;
			(CThemeResourceExtension.Create(
				reinterpret_cast<DependencyObject**>(themeResourceExtension.ReleaseAndGetAddressOf()),
				&cp));

			themeResourceExtension.m_strResourceKey = themeBrush;

			themeResourceExtension.SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary);

			(themeResourceExtension.SetThemeResourceBinding(
				m_overlayElement.Cast<FrameworkElement>().GetHandle(),
				DirectUI.MetadataAPI.GetPropertyByIndex(KnownPropertyIndex.Shape_Fill))
				);
		}
		else
		{
			SolidColorBrush transparentBrush;
			transparentBrush = new();
			transparentBrush.Color = { 0, 0, 0, 0 }
			;

			Rectangle overlayAsRect;
			m_overlayElement.As(&overlayAsRect);
			overlayAsRect.Cast<Rectangle>().Fill = transparentBrush;
		}

		return S_OK;
	}

	private void UpdateTargetForOverlayAnimations()
	{
		MUX_ASSERT(m_layoutTransitionElement is not null);
		MUX_ASSERT(m_isOverlayVisible is not null);

		if (m_overlayOpeningStoryboard is not null)
		{
			m_overlayOpeningStoryboard.Stop();

			Storyboard.SetTarget(
				(CTimeline*)(m_overlayOpeningStoryboard.Cast<Storyboard>().GetHandle()),
				m_overlayLayoutTransitionElement)
				);
		}

		if (m_overlayClosingStoryboard is not null)
		{
			m_overlayClosingStoryboard.Stop();

			Storyboard.SetTarget(
				(CTimeline*)(m_overlayClosingStoryboard.Cast<Storyboard>().GetHandle()),
				m_overlayLayoutTransitionElement)
				);
		}
	}

	private void PlayOverlayOpeningAnimation()
	{
		MUX_ASSERT(m_isInOverlayState);
		MUX_ASSERT(m_isOverlayVisible);

		if (m_overlayClosingStoryboard is not null)
		{
			m_overlayClosingStoryboard.Stop();
		}

		if (m_overlayOpeningStoryboard is not null)
		{
			m_overlayOpeningStoryboard.Begin();
		}
	}

	private void PlayOverlayClosingAnimation()
	{
		MUX_ASSERT(m_isInOverlayState);
		MUX_ASSERT(m_isOverlayVisible);

		if (m_overlayOpeningStoryboard is not null)
		{
			m_overlayOpeningStoryboard.Stop();
		}

		if (m_overlayClosingStoryboard is not null)
		{
			m_overlayClosingStoryboard.Begin();
		}
	}
}
