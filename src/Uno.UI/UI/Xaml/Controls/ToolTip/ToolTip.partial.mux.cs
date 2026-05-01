// MUX Reference dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp
//
// NOTE: Constants and field declarations live in ToolTip.partial.h.mux.cs
// (port of ToolTip_Partial.h). This file ports method bodies in the order
// they appear in ToolTip_Partial.cpp.

#if __SKIA__

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTip : ContentControl
{
	// Suppressions for the in-progress port: many private helpers are wired only by
	// later phases (DP-callback dispatch in Phase 2 closeout, OpenPopup / OnIsOpenChanged
	// in Phase 3, etc.). The corresponding warnings are restored once the wiring lands.
#pragma warning disable IDE0051 // Remove unused private members

	// === WinUI Generated stubs (these come from ToolTip.idl / ToolTip.g.cpp in WinUI;
	// ===  they are not ported from ToolTip_Partial.cpp but Skia needs them because
	// ===  the gated cross-platform ToolTip.cs declared them and the source generator
	// ===  consequently skipped emitting them in Generated/Microsoft.UI.Xaml.Controls/ToolTip.cs.)

	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen), typeof(bool),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(bool), OnIsOpenChangedStatic));

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public static DependencyProperty PlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(Placement), typeof(PlacementMode),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(DefaultPlacementMode));

	public PlacementMode Placement
	{
		get => (PlacementMode)GetValue(PlacementProperty);
		set => SetValue(PlacementProperty, value);
	}

#pragma warning disable CS0067 // Event is never used until OnIsOpenChanged dispatches in Phase 2 closeout.
	public event RoutedEventHandler? Opened;

	public event RoutedEventHandler? Closed;
#pragma warning restore CS0067

	// === Port from ToolTip_Partial.cpp in source order ===

	// MUX Reference: ToolTip_Partial.cpp ToolTip() (line 24).
	public ToolTip()
	{
		DefaultStyleKey = typeof(ToolTip);

		m_pToolTipServicePlacementModeOverride = null;
		m_bIsPopupPositioned = false;
		m_bClosing = false;
		m_bInputEventsHookedUp = false;
		m_bIsOpenAsAutomaticToolTip = false;
		m_bCallPerformPlacementAtNextPopupOpen = false;
		m_inputMode = AutomaticToolTipInputMode.None;
		m_isSliderThumbToolTip = false;

		// MUX Reference: ToolTip_Partial.cpp Initialize() (line 92).
		// C# folds the framework Initialize() lifecycle into the constructor since the
		// generated base does not expose a virtual Initialize hook.
		SizeChanged += OnToolTipSizeChanged;
	}

	// MUX Reference: ToolTip_Partial.cpp ~ToolTip() (line 36).
	// The C++ destructor unhooks owner layout-changed and Xaml-island root handlers,
	// and frees the heap-allocated m_pToolTipServicePlacementModeOverride.
	// In C#, GC handles the field; the SerialDisposable revokers (m_ownerLayoutUpdatedToken,
	// m_xamlIslandRoot* handlers) are disposed by their owners. Phase 6 will wire the
	// explicit Unhook* calls below; for now they are no-ops because the hookups have not
	// been ported yet.
#if false
	~ToolTip()
	{
		VERIFYHR(UnhookOwnerLayoutChangedEvent());
		VERIFYHR(UnhookFromXamlIslandRoot());

		delete m_pToolTipServicePlacementModeOverride;
		m_pToolTipServicePlacementModeOverride = NULL;
	}
#endif

	// MUX Reference: ToolTip_Partial.cpp SetOwner (line 45).
	internal void SetOwner(DependencyObject? pNewOwner)
	{
		// TODO Uno (Phase 6): IFC_RETURN(UnhookOwnerLayoutChangedEvent());

		m_wrOwner = null;

		if (pNewOwner is not null)
		{
			m_wrOwner = new WeakReference(pNewOwner);
		}
	}

	// Phase 0 scaffolding shim: Slider.mux.cs calls SetAnchor (Uno's historical name).
	// Per feedback_winui_port_gating_pattern.md we keep the public surface intact and
	// delegate to the faithful SetOwner port. Slider.mux.cs migration to SetOwner can
	// happen later once the rest of the port is validated.
	public void SetAnchor(UIElement element) => SetOwner(element);

	// MUX Reference: ToolTip_Partial.cpp GetContainer (line 61).
	internal FrameworkElement? GetContainer()
	{
		return m_wrContainer?.Target as FrameworkElement;
	}

	// MUX Reference: ToolTip_Partial.cpp SetContainer (line 72).
	internal void SetContainer(FrameworkElement? pNewContainer)
	{
		object? spContainerDataContext = null;

		m_wrContainer = null;

		if (pNewContainer is not null)
		{
			m_wrContainer = new WeakReference(pNewContainer);
			spContainerDataContext = pNewContainer.DataContext;
		}

		// If pNewContainer is NULL, we'll clear the DataContext here.
		this.DataContext = spContainerDataContext;
	}

	// MUX Reference: ToolTip_Partial.cpp HookupParentPopup (line 113).
	internal Popup HookupParentPopup()
	{
		IsTabStop = false;
		IsHitTestVisible = false;

		var spTarget = GetTarget();

		var spPopup = new Popup();

		// Set the Popup's ToolTip owner : This is a hint required by XamlIslandRoots
		// ToolTips use parentless popups.  ToolTip targets point to an element in the
		// XamlIslandRoot's tree and can be walked to find the root for that XamlIslandRoot.
		// The popup can then be rooted under the correct PopupRoot so it displays
		// relative to the island position.
#if false
		auto popup = do_pointer_cast<CPopup>(spPopup->GetHandle());
		if(popup)
		{
			popup->m_toolTipOwnerWeakRef = xref::get_weakref(static_cast<FrameworkElement*>(spTarget.Get())->GetHandle());
		}
#endif
		// TODO Uno (Phase 6): port the m_toolTipOwnerWeakRef hint once XamlIslandRoot
		// support lands on Skia. The XamlIslandRoot routing it enables is gated on
		// Phase 6 hookups.

		// ToolTip sets the location to the out of Xaml Window by using the windowed Popup in Threshold Windows
#if false
		if (CPopup::DoesPlatformSupportWindowedPopup(DXamlCore::GetCurrent()->GetHandle()) &&
			!static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed())
		{
			// Set the windowed popup to support the render popup at the out of Xaml window
			IFC_RETURN(static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->SetIsWindowed());
			ASSERT(static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed());
		}
#endif
		// TODO Uno (Phase 5): port the windowed-popup configuration once Skia exposes
		// the equivalent of CPopup::SetIsWindowed.

		// Don't show the Popup for disabled ToolTips.
		bool bIsEnabled = IsEnabled;
		if (!bIsEnabled)
		{
			spPopup.Opacity = 0;
		}

		if (spTarget is not null)
		{
			// Propagate FlowDirection from placement target to popup
			var spFlowDirectionBinding = new Data.Binding
			{
				Source = spTarget,
				Path = new PropertyPath("FlowDirection"),
			};

			spPopup.SetBinding(FrameworkElement.FlowDirectionProperty, spFlowDirectionBinding);

			// Propagate Language from placement target to popup
			spPopup.Language = spTarget.Language;
		}

		// Listening to the Opened and Closed events lets us guarantee that
		// the popup is actually opened when we perform those functions.

		spPopup.Opened += OnPopupOpened;
		spPopup.Closed += OnPopupClosed;

#if false
		IFC_RETURN(spPopup.Cast<Popup>()->SetOwner(this));
#endif
		// TODO Uno (Phase 5): Popup.SetOwner is internal in WinUI for hit-testing
		// routing back to the owning ToolTip. Uno's Popup does not yet expose this hook.

		return spPopup;
	}

	// MUX Reference: ToolTip_Partial.cpp OnPropertyChanged2 (line 211).
	// TODO Uno (Phase 2 closeout): Hook into OnPropertyChanged dispatch so IsOpen,
	// HorizontalOffset, VerticalOffset, PlacementRect changes route through the port.
	// For Phase 2 we approximate via the IsOpen DP-callback (OnIsOpenChangedStatic below).

	private static void OnIsOpenChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 2 closeout will wire OnIsOpenChanged faithfully (line 264 of C++).
		// For now this is a no-op; the Phase 3 ToolTipService.OpenAutomaticToolTip path
		// handles popup hosting.
	}

	// MUX Reference: ToolTip_Partial.cpp OnCreateAutomationPeer (line 251).
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ToolTipAutomationPeer(this);
	}

	// MUX Reference: ToolTip_Partial.cpp OnPlacementCriteriaChanged (line 567).
	private void OnPlacementCriteriaChanged()
	{
		PerformPlacementInternal();
	}

	// MUX Reference: ToolTip_Partial.cpp OpenPopup (line 573).
	private void OpenPopup()
	{
		var spPopup = m_wrPopup?.Target as Popup;
		global::System.Diagnostics.Debug.Assert(spPopup is not null, "popup from weak reference expected to be non-null");
		if (spPopup is null)
		{
			return;
		}

		// MUX Reference: VisualTree::GetForElementNoRef equivalent in Uno is XamlRoot.GetForElement.
		var targetVisualTree = spPopup.XamlRoot;

		// If the visual tree is null, there are two circumstances that might lead to this:
		// either we haven't yet parented the popup to a visual tree, in which case we can fall back
		// to using the visual tree of the tool tip target; or we may have gotten into the state
		// where we're cleaning up and no longer have a visual tree associated with the popup at all,
		// in which case we'll just do nothing.
		if (targetVisualTree is null)
		{
			var owner = m_wrOwner?.Target as DependencyObject;

			if (owner is not null)
			{
				targetVisualTree = XamlRoot.GetForElement(owner);
			}

			if (targetVisualTree is null)
			{
				return;
			}

			spPopup.XamlRoot = targetVisualTree;
		}

		spPopup.Child = this;

		var spNewDataContext = DataContext;
		spPopup.DataContext = spNewDataContext;

		ToolTipService.EnsureHandlersAttachedToRootElement(targetVisualTree);

		bool popupIsOpen = spPopup.IsOpen;
		global::System.Diagnostics.Debug.Assert(!popupIsOpen);
		spPopup.IsOpen = true;

		// Count the number of times that we've opened the popup. Rapidly closing and reopening the popup can put the ToolTip
		// in an inconsistent state when the Popup.Opened event gets delayed in being delivered, so we wait until that final
		// Popup.Opened event in this scenario. See comment for m_pendingPopupOpenEventCount in header for details.
		m_pendingPopupOpenEventCount++;

		var spTemplate = Template;
		if (spTemplate is not null)
		{
			ApplyTemplate();

			// Expected structure:
			//  CPopupRoot
			//    CToolTip
			//      CContentPresenter "LayoutRoot"
			//
			//  We apply shadow on ContentPresenter to capture FadeInThemeAnimation/FadeOutThemeAnimation

			var contentPresenterAsDO = GetTemplateChild("LayoutRoot") as UIElement;

			if (contentPresenterAsDO is not null)
			{
				if (ThemeShadow.IsDropShadowMode)
				{
					// Under drop shadows, ToolTip has a smaller shadow than normal.
					// TODO Uno: Uno's ApplyElevationEffect only accepts depth; the C++ baseElevation
					// (16) parameter has no Uno equivalent yet. Phase 6 polish will add it.
					ElevationHelper.ApplyElevationEffect(contentPresenterAsDO, 0 /* depth */);
				}
				else
				{
					ElevationHelper.ApplyElevationEffect(contentPresenterAsDO);
				}
			}
		}
		else
		{
			// In phone scenarios we do no offer a default template. So instead of having an open popup that doesn't display anything,
			// close it.
			spPopup.IsOpen = false;
		}
	}

	// MUX Reference: ToolTip_Partial.cpp Close (line 666).
	private void Close()
	{
		UnhookOwnerLayoutChangedEvent();
		UnhookFromXamlIslandRoot();

		var spTemplate = Template;
		// If we don't have a template, we never opened the popup in the first place.
		if (spTemplate is not null)
		{
			var spPopup = m_wrPopup?.Target as Popup;

			// Fix for Bug 2146297: The popup may already be null by the time ToolTip::Close() is called.
			if (spPopup is not null)
			{
				// Though cleanup of the Popup will release the ToolTip, it's possible this ToolTip might try to open again
				// with a different Popup before that cleanup happens.  I've seen something to this effect happening, and found
				// that clearing Popup.Child here prevents this problem.
				spPopup.Child = null;
				spPopup.IsOpen = false;
			}
		}
	}

	// MUX Reference: ToolTip_Partial.cpp GetTarget (line 696).
	// help method to get the target from placement override or placement target.
	internal FrameworkElement? GetTarget()
	{
		// If the ToolTipService is opening the ToolTip, then its owner is the placement target,
		// regardless of what the PlacementTarget has been set to.
		var spTarget = m_wrTargetOverride?.Target as FrameworkElement;
		if (spTarget is null)
		{
			var spTargetAsUIElement = PlacementTarget;
			spTarget = spTargetAsUIElement as FrameworkElement;
		}

		return spTarget;
	}

	// MUX Reference: ToolTip_Partial.cpp PerformPlacement (line 723).
	// Phase 5 will port PerformPlacement faithfully. Until then this stub keeps
	// the public Slider call-site working.
	//
	// Sets the location of the ToolTip's Popup.
	//
	// Slider "Disambiguation UI" ToolTips need special handling since they need to remain centered
	// over the sliding Thumb, which has not yet rendered in its new position.  Therefore, we pass
	// the new target rect to handle this case.
	internal void PerformPlacement(Rect? pTargetRect = null)
	{
		// TODO Uno (Phase 5): port PerformPlacement faithfully.
	}

	// MUX Reference: ToolTip_Partial.cpp PerformPlacementInternal (line 1866).
	// Phase 5 will port PerformPlacementInternal faithfully.
	private void PerformPlacementInternal()
	{
		// TODO Uno (Phase 5): port PerformPlacementInternal.
	}

	// MUX Reference: ToolTip_Partial.cpp OnToolTipSizeChanged (line 1899).
	// Phase 5 (placement) will replace this stub with the faithful port.
	private void OnToolTipSizeChanged(object sender, SizeChangedEventArgs args)
	{
		// TODO Uno (Phase 5): port the SizeChanged-driven re-placement.
	}

	// MUX Reference: ToolTip_Partial.cpp OnPopupOpened (line 1911).
	private void OnPopupOpened(object? pUnused1, object pUnused2)
	{
		OnOpened();

		if (AutomationPeer.ListenerExistsHelper(AutomationEvents.ToolTipOpened))
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is not null)
			{
				spAutomationPeer.RaiseAutomationEvent(AutomationEvents.ToolTipOpened);
			}
		}

		// If we've recently closed and reopened this ToolTip, then don't do anything until we receive the final Popup.Opened
		// event. See comment for m_pendingPopupOpenEventCount in header for details.
		m_pendingPopupOpenEventCount--;
		if (m_pendingPopupOpenEventCount == 0 && m_bCallPerformPlacementAtNextPopupOpen)
		{
			m_bCallPerformPlacementAtNextPopupOpen = false;
			PerformPlacementInternal();
		}
	}

	// MUX Reference: ToolTip_Partial.cpp OnPopupClosed (line 1945).
	private void OnPopupClosed(object? pUnused1, object pUnused2)
	{
		OnClosed();

		if (AutomationPeer.ListenerExistsHelper(AutomationEvents.ToolTipClosed))
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is not null)
			{
				spAutomationPeer.RaiseAutomationEvent(AutomationEvents.ToolTipClosed);
			}
		}

		m_bIsPopupPositioned = false;
	}

	// MUX Reference: ToolTip_Partial.cpp OnOpened (line 1971).
	private void OnOpened()
	{
		// Create the args
		var spArgs = new RoutedEventArgs(this);

		// Raise the event
		Opened?.Invoke(this, spArgs);
	}

	// MUX Reference: ToolTip_Partial.cpp OnClosed (line 1989).
	private void OnClosed()
	{
		// Create the args
		var spArgs = new RoutedEventArgs(this);

		// Raise the event
		Closed?.Invoke(this, spArgs);
	}

	// MUX Reference: ToolTip_Partial.cpp ForceFinishClosing (line 2057).
	// If we are in the process of animating to the Closed state, then closes the ToolTip's Popup.
	// Else, does nothing.
	private void ForceFinishClosing(object? pUnused1, object? pUnused2)
	{
		// Avoid closing the current Popup if it's already been done i.e. by another ToolTip trying to open.
		if (m_bClosing)
		{
			Close();
			m_bClosing = false;
		}
	}

	// MUX Reference: ToolTip_Partial.cpp OnIsEnabledChanged (line 2075).
	// Called when the IsEnabled property changes.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
	{
		bool bIsOpen = IsOpen;
		if (bIsOpen)
		{
			var spPopup = m_wrPopup?.Target as Popup;
			global::System.Diagnostics.Debug.Assert(spPopup is not null, "popup from weak reference expected to be non-null");
			if (spPopup is null)
			{
				return;
			}

			bool bIsEnabled = IsEnabled;
			if (bIsEnabled)
			{
				PerformPlacementInternal();
			}

			// Make the ToolTip visible if IsEnabled=True, or hidden otherwise.
			spPopup.Opacity = bIsEnabled ? 1 : 0;
		}
	}

	// MUX Reference: ToolTip_Partial.cpp ChangeVisualState (line 2105).
	// Change to the correct visual state for the ToolTip.
	//
	// bUseTransitions: true to use transitions when updating the visual state, false
	// to snap directly to the new visual state.
	private protected override void ChangeVisualState(bool bUseTransitions)
	{
		bool bIsOpen = IsOpen;

		if (bIsOpen && m_bIsPopupPositioned)
		{
			GoToState(bUseTransitions, "Opened");
		}
		else
		{
			bool alreadyInClosedState = false;

			// MUX Reference: VSM::TryGetState equivalent (Uno does not expose TryGetState publicly).
			// Walk the VSM groups on the templated child to find the "Closed" state and check whether
			// the current state of its group is already "Closed".
			VisualState? spClosedVisualState = null;
			VisualStateGroup? spClosedVisualStateGroup = null;
			if (VisualTreeHelper.GetChildrenCount(this) > 0)
			{
				if (VisualTreeHelper.GetChild(this, 0) is FrameworkElement spChildAsFE)
				{
					var spVisualStateGroups = VisualStateManager.GetVisualStateGroups(spChildAsFE);
					if (spVisualStateGroups is not null)
					{
						foreach (var group in spVisualStateGroups)
						{
							foreach (var state in group.States)
							{
								if (state.Name == "Closed")
								{
									spClosedVisualState = state;
									spClosedVisualStateGroup = group;
									break;
								}
							}

							if (spClosedVisualState is not null)
							{
								break;
							}
						}
					}
				}
			}

			if (spClosedVisualStateGroup is not null && spClosedVisualState is not null)
			{
				var spCurrentVisualState = spClosedVisualStateGroup.CurrentState;
				alreadyInClosedState = ReferenceEquals(spCurrentVisualState, spClosedVisualState);
			}

			m_bClosing = true;

			bool bWentToState = GoToState(bUseTransitions, "Closed");

			if (!bWentToState || alreadyInClosedState)
			{
				// We could not go to the "Closed" state. This could happen if there is no Closed state or if we were already in
				// the Closed state.
				// Ordinarily, the Storyboard Completed event will trigger the call to Close(), but if this is not going to happen,
				// we need to call it directly from here.
				m_bClosing = false;
				Close();
			}
		}
	}

	// MUX Reference: ToolTip_Partial.cpp RemoveAutomaticStatusFromOpenToolTip (line 2160).
	//
	// For Slider, the Thumb ToolTip may be opened as an automatic ToolTip by pointer hover.  However, if
	// we click on the Thumb and start to drag, we don't want the ToolTip to disappear after several seconds.
	// Thus, we remove the automatic flag and keep the ToolTip open for Slider to handle.
	[NotImplemented]
	internal void RemoveAutomaticStatusFromOpenToolTip()
	{
		// TODO Uno: Phase 2 closeout / Phase 3 will port RemoveAutomaticStatusFromOpenToolTip.
	}

	// === Helpers awaiting Phase 6 (Xaml-island roots + safe zone) ===

	// MUX Reference: ToolTip_Partial.cpp UnhookOwnerLayoutChangedEvent (later in file).
	private void UnhookOwnerLayoutChangedEvent()
	{
		// TODO Uno (Phase 6): port UnhookOwnerLayoutChangedEvent.
		m_ownerLayoutUpdatedToken.Disposable = null;
	}

	// MUX Reference: ToolTip_Partial.cpp UnhookFromXamlIslandRoot (later in file).
	private void UnhookFromXamlIslandRoot()
	{
		// TODO Uno (Phase 6): port UnhookFromXamlIslandRoot.
		m_xamlIslandRootPointerMovedHandler.Disposable = null;
		m_xamlIslandRootKeyDownHandler.Disposable = null;
		m_xamlIslandRootKeyUpHandler.Disposable = null;
	}

#pragma warning restore IDE0051
}

#endif // __SKIA__