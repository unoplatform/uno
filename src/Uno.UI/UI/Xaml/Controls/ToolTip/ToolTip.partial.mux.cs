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
using Uno.Extensions;
using Windows.Foundation;
using Windows.System;

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

	public static DependencyProperty HorizontalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalOffset), typeof(double),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(double), OnPlacementCriteriaChangedDP));

	public double HorizontalOffset
	{
		get => (double)GetValue(HorizontalOffsetProperty);
		set => SetValue(HorizontalOffsetProperty, value);
	}

	public static DependencyProperty VerticalOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalOffset), typeof(double),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(double), OnPlacementCriteriaChangedDP));

	public double VerticalOffset
	{
		get => (double)GetValue(VerticalOffsetProperty);
		set => SetValue(VerticalOffsetProperty, value);
	}

	public static DependencyProperty PlacementRectProperty { get; } =
		DependencyProperty.Register(
			nameof(PlacementRect), typeof(Rect?),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(Rect?), OnPlacementCriteriaChangedDP));

	public Rect? PlacementRect
	{
		get => (Rect?)GetValue(PlacementRectProperty);
		set => SetValue(PlacementRectProperty, value);
	}

	public static DependencyProperty PlacementTargetProperty { get; } =
		DependencyProperty.Register(
			nameof(PlacementTarget), typeof(UIElement),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(UIElement)));

	public UIElement PlacementTarget
	{
		get => (UIElement)GetValue(PlacementTargetProperty);
		set => SetValue(PlacementTargetProperty, value);
	}

	private static void OnPlacementCriteriaChangedDP(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		(sender as ToolTip)?.OnPlacementCriteriaChanged();
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
		m_wrContainer = null;

		if (pNewContainer is not null)
		{
			m_wrContainer = new WeakReference(pNewContainer);

			// MUX semantics: capture the container's DataContext so the ToolTip inherits
			// it for binding purposes. The WinUI native code does this via the
			// inheritance context that the ToolTipObject attached property establishes
			// between owner and ToolTip; that mechanism has no Uno equivalent, so we
			// install a Binding so subsequent owner DataContext changes flow through.
			this.SetBinding(DataContextProperty, new Data.Binding
			{
				Source = pNewContainer,
				Path = new PropertyPath(nameof(DataContext)),
			});
		}
		else
		{
			// If pNewContainer is NULL, we'll clear the DataContext here.
			this.ClearValue(DataContextProperty);
		}
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
	// In C# we use individual DP PropertyChangedCallbacks instead of the WinUI single-method
	// dispatch. The IsOpen DP routes through OnIsOpenChangedStatic below; HorizontalOffset /
	// VerticalOffset / PlacementRect callbacks (Phase 5) will route through OnPlacementCriteriaChanged.

	private static void OnIsOpenChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		(sender as ToolTip)?.OnIsOpenChanged((bool)args.NewValue);
	}

	// MUX Reference: ToolTip_Partial.cpp OnIsOpenChanged (line 264).
	private void OnIsOpenChanged(bool bIsOpen)
	{
		var pToolTipServiceMetadata = ToolTipService.GetToolTipServiceMetadata();

		if (bIsOpen)
		{
			Popup? spPopup = null;

			m_bIsOpenAsAutomaticToolTip = ToolTipService.s_bOpeningAutomaticToolTip;
			if (m_bIsOpenAsAutomaticToolTip)
			{
				// If there is another ToolTip currently playing its closing animation, destroy it.
				if (pToolTipServiceMetadata.m_tpCurrentPopup is { } currentPopup)
				{
					var spPopupChild = currentPopup.Child;
					if (spPopupChild is ToolTip spPreviousToolTip)
					{
						spPreviousToolTip.ForceFinishClosing(null, null);
					}

					pToolTipServiceMetadata.m_tpCurrentPopup = null;
				}

				// Sometimes the current ToolTip is not fully closed by this point
				// we need to ensure that it is as so it is not still in the live tree
				// when we try to open it below
				// We should only try to do this if the popup is still alive
				// The only known issue so far with this is Slider Thumb tooltips, those
				// are opened automatically by the Slider control during the PointerPressed
				// event to show the current value.
				if (m_bClosing)
				{
					var spResolvedPopup = m_wrPopup?.Target as Popup;
					if (spResolvedPopup is not null)
					{
						ForceFinishClosing(null, null);
					}
				}

				global::System.Diagnostics.Debug.Assert(m_wrTargetOverride is null);
				global::System.Diagnostics.Debug.Assert(m_pToolTipServicePlacementModeOverride is null);
				global::System.Diagnostics.Debug.Assert(pToolTipServiceMetadata.m_tpOwner is not null);

				if (pToolTipServiceMetadata.m_tpCloseTimer is not null)
				{
					pToolTipServiceMetadata.m_tpCloseTimer.Stop();
				}

				if (pToolTipServiceMetadata.m_tpSafeZoneCheckTimer is not null)
				{
					pToolTipServiceMetadata.m_tpSafeZoneCheckTimer.Stop();
				}

				pToolTipServiceMetadata.SetCurrentToolTip(this);

				SetPlacementOverrides(pToolTipServiceMetadata.m_tpContainer);

				spPopup = HookupParentPopup();
				global::System.Diagnostics.Debug.Assert(pToolTipServiceMetadata.m_tpCurrentPopup is null);
				pToolTipServiceMetadata.SetCurrentPopup(spPopup);
				m_wrPopup = new WeakReference(spPopup);

				// TODO Uno (Phase 6): FrameworkElement.SetHasOpenToolTip is not exposed in Uno.
#if false
				if (pToolTipServiceMetadata->m_tpContainer)
				{
					IFC(static_cast<FrameworkElement*>(pToolTipServiceMetadata->m_tpContainer.Get())->SetHasOpenToolTip(TRUE));
				}
#endif
			}
			else
			{
				var spContainer = m_wrContainer?.Target as FrameworkElement;
				SetPlacementOverrides(spContainer);

				spPopup = m_wrPopup?.Target as Popup;
				if (spPopup is not null)
				{
					// Make sure to finish closing if we are currently closing the Popup.
					ForceFinishClosing(null, null);

					// Since the Popup closes/opens asynchronously, we may not get a SizeChanged notification
					// in the correct order, so we have to explicitly call PerformPlacement() the next time the
					// Popup is opened.
					m_bCallPerformPlacementAtNextPopupOpen = true;
				}
				else
				{
					spPopup = HookupParentPopup();
					m_wrPopup = new WeakReference(spPopup);
				}
			}

			ForwardOwnerThemePropertyToToolTip();

			OpenPopup();

			// Since ToolTip is kept open, need to hook up with CoreWindow or XamlIslandRoot to handle PointerMove and Key event
			HookupOwnerLayoutChangedEvent();
			HookupXamlIslandRoot();
			UpdateOwnersBoundary(); // it's OK if boundary can't be got
		}
		else
		{
			UnhookOwnerLayoutChangedEvent();
			UnhookFromXamlIslandRoot();

			// When IsOpen is set to True, we wait for the Popup to be created and opened before updating state.
			// When IsOpen is set to False, we go to the Closed state immediately and close the Popup after
			// we have finished transitioning to the Closed state.
			UpdateVisualState();

			// Uno-specific safety net: the C++ port relies on the Closed VSM state's
			// Storyboard.Completed event to trigger ForceFinishClosing -> Close. The
			// default ToolTip XAML template (ToolTip.xaml) has its VSM commented out, so
			// no animation actually fires. ChangeVisualState handles the !bWentToState
			// case, but if Uno's GoToState reports success even for missing states, the
			// close path doesn't run. Force-close here defensively.
			if (m_bClosing)
			{
				m_bClosing = false;
				Close();
			}

			m_pToolTipServicePlacementModeOverride = null;
			m_wrTargetOverride = null;

			if (m_bIsOpenAsAutomaticToolTip)
			{
				pToolTipServiceMetadata.m_tpCurrentToolTip = null;
				pToolTipServiceMetadata.m_tpCurrentPopup = null;

				// TODO Uno (Phase 6): FrameworkElement.SetHasOpenToolTip is not exposed in Uno.
#if false
				if (pToolTipServiceMetadata->m_tpContainer)
				{
					IFC(static_cast<FrameworkElement*>(pToolTipServiceMetadata->m_tpContainer.Get())->SetHasOpenToolTip(FALSE));
				}
#endif

				m_bIsOpenAsAutomaticToolTip = false;
			}

			m_bCallPerformPlacementAtNextPopupOpen = false;
			m_inputMode = AutomaticToolTipInputMode.None;
		}
	}

	// MUX Reference: ToolTip_Partial.cpp OnCreateAutomationPeer (line 251).
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ToolTipAutomationPeer(this);
	}

	// MUX Reference: ToolTip_Partial.cpp OnApplyTemplate (line 412).
	// Apply a template to the ToolTip.
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// This hunk of code traverses the ToolTip's VSM states and finds its Closed state.
		// If this state is found, we add a Completed event handler to the Closed state's Storyboard.
		// When we finish going to the Closed state, then we need to close the ToolTip's Popup.
		// If no Closed state is found, we close the ToolTip's Popup immediately.
		// This logic allows us to achieve a fade-out effect for ToolTip.
		bool bClosedStateHandled = false;
		int childCount = VisualTreeHelper.GetChildrenCount(this);
		if (childCount > 0)
		{
			var spChild = VisualTreeHelper.GetChild(this, 0);
			var spChildAsFE = spChild as FrameworkElement;

			if (spChildAsFE is not null)
			{
				var spChildVisualStateGroups = VisualStateManager.GetVisualStateGroups(spChildAsFE);

				if (spChildVisualStateGroups is not null)
				{
					// TODO: Clean this up to use the following call instead of manually iterating the groups:
					// IFC_RETURN(VisualStateManager::TryGetState(spChildVisualStateGroups.Get(), hsClosedStateName, &spGroup, &spState, &bResult));

					foreach (var spGroup in spChildVisualStateGroups)
					{
						var spVisualStates = spGroup.States;
						if (spVisualStates is not null)
						{
							foreach (var spState in spVisualStates)
							{
								if (spState.Name == "Closed")
								{
									// Add handler close the Tooltip's Popup after its Closed state has completed.
									var spStoryboard = spState.Storyboard;
									if (spStoryboard is not null)
									{
										spStoryboard.Completed += ForceFinishClosing;
									}

									bClosedStateHandled = true;
									break;
								}
							}

							if (bClosedStateHandled)
							{
								break;
							}
						}
					}
				}
			}
		}

		// TODO: 105819 - Dynamic Timeline needs to clear and recreate children collection when its inheritance context changes
		// When the bug is fixed, uncomment UpdateVisualState() and delete the explicit GoToState call.
		//IFC_RETURN(UpdateVisualState(FALSE));
		GoToState(false, "Opened");
	}

	// MUX Reference: ToolTip_Partial.cpp SetPlacementOverrides (line 516).
	private void SetPlacementOverrides(FrameworkElement? pInputTargetOverride)
	{
		if (pInputTargetOverride is null)
		{
			return;
		}

		var spOwnerAsIDO = pInputTargetOverride;

		// The override values below will be cleared in ToolTip when the ToolTip closes.

		// ToolTipService overrides any Placement and PlacementTarget that have been set on the ToolTip.
		var spOwnerPlacementTarget = ToolTipService.GetPlacementTarget(spOwnerAsIDO);
		FrameworkElement? spTargetOverride = spOwnerPlacementTarget as FrameworkElement;
		if (spTargetOverride is null)
		{
			spTargetOverride = pInputTargetOverride;
		}

		// Since we don't have coercion, we can't override the PlacementTarget like WPF does.
		// Instead, we use m_wrTargetOverride for this purpose.
		global::System.Diagnostics.Debug.Assert(spTargetOverride is not null);
		if (spTargetOverride is not null)
		{
			m_wrTargetOverride = new WeakReference(spTargetOverride);
		}

		// We need to tell if the Placement property has been set already.
		var ownerPlacement = ToolTipService.GetPlacement(spOwnerAsIDO);
		if (ownerPlacement != DefaultPlacementMode)
		{
			m_pToolTipServicePlacementModeOverride = ownerPlacement;
		}
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
	//
	// Sets the location of the ToolTip's Popup.
	//
	// Slider "Disambiguation UI" ToolTips need special handling since they need to remain centered
	// over the sliding Thumb, which has not yet rendered in its new position.  Therefore, we pass
	// the new target rect to handle this case.
	internal void PerformPlacement(Rect? pTargetRect = null)
	{
		// It is possible for this function to be called even though the ToolTip is closed.
		// This can happen if the ToolTip gets closed before it has had a chance to layout and complete its opening sequence.
		// If this happens, we don't want to continue opening, as that could result in a "closed" ToolTip that is still visible on the screen.
		var spPopup = m_wrPopup?.Target as Popup;

		if (spPopup is not null && IsOpen)
		{
			// TODO Uno (Phase 6): windowed-popup branch is gated on CPopup::IsWindowed which Skia does not yet expose.
#if false
			if (static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed())
			{
				// Sets the location of the ToolTip's Popup out of the Xaml window.
				PerformPlacementWithWindowedPopup(pTargetRect);
			}
			else
#endif
			{
				// Sets the location of the ToolTip's Popup within the Xaml window.
				PerformPlacementWithPopup(pTargetRect);
			}

			// If PerformPlacementWithPopup/PerformPlacementWithWindowedPopup fail to position the Popup, they will set ToolTip.IsOpen=False.
			// If this happens, we don't want to continue opening the ToolTip.
			if (!m_bIsPopupPositioned && IsOpen)
			{
				m_bIsPopupPositioned = true;
				UpdateVisualState();
			}
		}
	}

	// MUX Reference: ToolTip_Partial.cpp PerformPlacementWithPopup (line 772).
	// Sets the location of the ToolTip's Popup within the Xaml window.
	private void PerformPlacementWithPopup(Rect? pTargetRect)
	{
		// Make sure we can actually place the ToolTip.  The size should be > 0, and
		// IsOpen and IsEnabled should be true.
		if (IsOpen == false ||
			IsEnabled == false ||
			!(ActualWidth > 0) ||
			!(ActualHeight > 0))
		{
			return;
		}

		// If the ToolTipService is opening the ToolTip, then its Placement is used,
		// regardless of what the ToolTip.Placement has been set to.
		var placement = m_pToolTipServicePlacementModeOverride ?? Placement;

		var dimentions = new Rect(HorizontalOffset, VerticalOffset, ActualWidth, ActualHeight);

		// PlacementMode.Mouse only makes sense for automatic ToolTips opened by touch or mouse.
		if (placement == PlacementMode.Mouse &&
			(m_inputMode == AutomaticToolTipInputMode.Touch || m_inputMode == AutomaticToolTipInputMode.Mouse))
		{
			PerformMousePlacementWithPopup(dimentions, placement);
		}
		else
		{
			PerformNonMousePlacementWithPopup(pTargetRect, dimentions, placement);
		}
	}

	// MUX Reference: ToolTip_Partial.cpp PerformMousePlacementWithPopup (line 892).
	// Sets the location of the ToolTip's Popup within the Xaml window.
	private void PerformMousePlacementWithPopup(Rect pDimentions, PlacementMode placement)
	{
		double tooltipActualWidth = pDimentions.Right;
		double tooltipActualHeight = pDimentions.Bottom;
		double horizontalOffset = pDimentions.Left;
		double verticalOffset = pDimentions.Top;

		var bIsRTL = false;

		double maxX;
		double maxY;
		double left;
		double top;
		var toolTipRect = default(Rect);
		var intersectionRect = default(Rect);

		var spTarget = GetTarget();
		var bounds = XamlRoot?.VisualTree.VisibleBounds
			?? spTarget?.XamlRoot?.VisualTree.VisibleBounds
			?? Window.CurrentSafe?.Bounds
			?? default;
		double screenWidth = bounds.Width;
		double screenHeight = bounds.Height;

		if (spTarget is not null)
		{
			// TODO Uno: FlowDirection-based RTL detection is the same as the C++ port; once Uno's FlowDirection
			// is wired through Popup, set bIsRTL accordingly. For now we leave it false (LTR fallback).
			bIsRTL = false;

			// We should not do placement if the target is no longer in the live tree.
			if (!spTarget.IsLoaded)
			{
				return;
			}
		}

		var spPopup = m_wrPopup?.Target as Popup;
		if (spPopup is null)
		{
			return;
		}

		var lastPointerEnteredPoint = Microsoft.UI.Xaml.Input.PointerRoutedEventArgs.LastPointerEvent?.GetCurrentPoint(null).Position
			?? new Point();

		left = lastPointerEnteredPoint.X;
		top = lastPointerEnteredPoint.Y;

		// If we are in RTL mode, then flip the X coordinate around so that it appears to be in LTR mode. That
		// means all of the LTR logic will still work.
		if (bIsRTL)
		{
			left = screenWidth - left;
		}

		MovePointToPointerToolTipShowPosition(ref left, ref top, placement);

		// align ToolTip with the bottom left corner of mouse bounding rectangle
		top += m_mousePlacementVerticalOffset + verticalOffset;
		left += horizontalOffset;

		// pessimistic check of top value - can be 0 only if TextBlock().FontSize == 0
		top = Math.Max(TOOLTIP_TOLERANCE, top);

		// left can be less then TOOLTIP_tolerance if user put mouse pointer on the border of object
		left = Math.Max(TOOLTIP_TOLERANCE, left);

		maxX = screenWidth;
		maxY = screenHeight;

		toolTipRect.X = left;
		toolTipRect.Y = top;
		toolTipRect.Width = tooltipActualWidth;
		toolTipRect.Height = tooltipActualHeight;

		intersectionRect.Width = maxX;
		intersectionRect.Height = maxY;

		intersectionRect.Intersect(toolTipRect);
		if ((Math.Abs(intersectionRect.Width - toolTipRect.Width) < TOOLTIP_TOLERANCE) &&
			(Math.Abs(intersectionRect.Height - toolTipRect.Height) < TOOLTIP_TOLERANCE))
		{
			// The placement algorithm operates in LTR mode (with transformed data if it
			// is really in RTL mode), so we also need to transform the X value it returns.
			if (bIsRTL)
			{
				left = screenWidth - left;
			}

			// ToolTip is completely inside the plug-in
			spPopup.VerticalOffset = top;
			spPopup.HorizontalOffset = left;
		}
		else
		{
			if (top + toolTipRect.Height > maxY)
			{
				// If the lower edge of the plug-in obscures the ToolTip,
				// it repositions itself to align with the upper edge of the bounding box of the mouse.
				top = maxY - toolTipRect.Height - TOOLTIP_TOLERANCE;
			}

			if (top < 0)
			{
				// If the upper edge of Plug-in obscures the ToolTip,
				// the control repositions itself to align with the upper edge.
				// align with the top of the plug-in
				top = 0;
			}

			if (left + toolTipRect.Width > maxX)
			{
				// If the right edge obscures the ToolTip,
				// it opens in the opposite direction from the obscuring edge.
				left = maxX - toolTipRect.Width - TOOLTIP_TOLERANCE;
			}

			if (left < 0)
			{
				// If the left edge obscures the ToolTip,
				// it then aligns with the obscuring screen edge
				left = 0;
			}

			// if right/bottom doesn't fit into the plug-in bounds, clip the ToolTip
			{
				var clipCalculationsRect = new Rect(left, top, toolTipRect.Width, toolTipRect.Height);
				CalculateTooltipClip(clipCalculationsRect, maxX, maxY);
			}

			// The placement algorithm operates in LTR mode (with transformed data if it
			// is really in RTL mode), so we also need to transform the X value it returns.

			if (bIsRTL)
			{
				left = screenWidth - left;
			}

			// position the parent Popup
			spPopup.VerticalOffset = top + verticalOffset;
			spPopup.HorizontalOffset = left + horizontalOffset;
		}
	}

	// MUX Reference: ToolTip_Partial.cpp PerformNonMousePlacementWithPopup (line 1066).
	// Sets the location of the ToolTip's Popup within the Xaml window.
	private void PerformNonMousePlacementWithPopup(Rect? pTargetRect, Rect pDimentions, PlacementMode placement)
	{
		var horizontalOffset = pDimentions.Left;
		var verticalOffset = pDimentions.Top;

		var bIsRTL = false;

		var origin = default(Point);
		var rcDockTo = default(Rect);
		var szFlyout = default(Size);

		var spTarget = m_wrOwner?.Target as FrameworkElement;
		global::System.Diagnostics.Debug.Assert(spTarget is not null, "pTarget expected to be non-null in ToolTip_Partial::PerformPlacement()");
		if (spTarget is not null)
		{
			// TODO Uno: FlowDirection-based RTL detection (see PerformMousePlacementWithPopup TODO).
			bIsRTL = false;

			// We should not do placement if the target is no longer in the live tree.
			if (!spTarget.IsLoaded)
			{
				return;
			}
		}

		var spPopup = m_wrPopup?.Target as Popup;
		if (spPopup is null)
		{
			return;
		}

		// For ToolTips opened by keyboard focus, PlacementMode_Mouse doesn't make any sense.
		// Fall back to the default - PlacementMode_Top.
		if (placement == PlacementMode.Mouse)
		{
			placement = PlacementMode.Top;
		}

		if (spTarget is null)
		{
			return;
		}

		var visibleRect = XamlRoot?.VisualTree.VisibleBounds
			?? spTarget?.XamlRoot?.VisualTree.VisibleBounds
			?? Window.CurrentSafe?.Bounds
			?? default;
		var constraint = visibleRect;

		var windowRect = XamlRoot?.VisualTree.VisibleBounds
			?? spTarget?.XamlRoot?.VisualTree.VisibleBounds
			?? Window.CurrentSafe?.Bounds
			?? default;
		origin.X = windowRect.X;
		origin.Y = windowRect.Y;

		szFlyout = new Size(ActualWidth, ActualHeight);
		if (szFlyout.Width == 0 || szFlyout.Height == 0)
		{
			// Ensure we have a correct size before layouting ToolTip, otherwise it may appear under mouse, steal focus and dismiss itself
			ApplyTemplate();
			Measure(visibleRect.Size);
			szFlyout = DesiredSize;
		}

		var placementRect = GetPlacementRectInWindowCoordinates();

		var getDockToRectFromTargetElement = false;

		if (pTargetRect.HasValue)
		{
			// Slider case - position ToolTip over Thumb rect
			rcDockTo = pTargetRect.Value;
		}
		else if (!placementRect.IsEmpty)
		{
			rcDockTo = placementRect;
		}
		else if (!m_isSliderThumbToolTip &&
			(AutomaticToolTipInputMode.Touch == m_inputMode || AutomaticToolTipInputMode.Mouse == m_inputMode))
		{
			var lastPointerEnteredPoint = Microsoft.UI.Xaml.Input.PointerRoutedEventArgs.LastPointerEvent?.GetCurrentPoint(null).Position
				?? new Point();

			rcDockTo.X = lastPointerEnteredPoint.X;
			rcDockTo.Y = lastPointerEnteredPoint.Y;

			// TODO Uno (Phase 6): for touch, account for the context-menu hint vertical offset
			// (CONTEXT_MENU_HINT_VERTICAL_OFFSET).
		}
		else
		{
			getDockToRectFromTargetElement = true;
		}

		var target = GetTarget();
		if (getDockToRectFromTargetElement && target is not null)
		{
			var targetTopLeft = default(Point);
			targetTopLeft = target.TransformToVisual(null).TransformPoint(targetTopLeft);

			// TODO Uno: RTL adjustment (subtract targetActualWidth from targetTopLeft.X for RTL).

			rcDockTo.X = targetTopLeft.X;
			rcDockTo.Y = targetTopLeft.Y;
			rcDockTo.Width = target.ActualWidth;
			rcDockTo.Height = target.ActualHeight;
		}

		rcDockTo = rcDockTo.OffsetRect(origin.X, origin.Y);

		// If horizontal & vertical offset are not specified, use the system defaults.
		var isPropertyLocal = this.IsDependencyPropertySet(HorizontalOffsetProperty);
		if (!isPropertyLocal)
		{
			horizontalOffset = DEFAULT_MOUSE_OFFSET;
			switch (m_inputMode)
			{
				case AutomaticToolTipInputMode.Keyboard:
					horizontalOffset = DEFAULT_KEYBOARD_OFFSET;
					break;
				case AutomaticToolTipInputMode.Mouse:
					horizontalOffset = DEFAULT_MOUSE_OFFSET;
					break;
				case AutomaticToolTipInputMode.Touch:
					horizontalOffset = DEFAULT_TOUCH_OFFSET;
					break;
			}
		}

		isPropertyLocal = this.IsDependencyPropertySet(VerticalOffsetProperty);
		if (!isPropertyLocal)
		{
			verticalOffset = DEFAULT_MOUSE_OFFSET;
			switch (m_inputMode)
			{
				case AutomaticToolTipInputMode.Keyboard:
					verticalOffset = DEFAULT_KEYBOARD_OFFSET;
					break;
				case AutomaticToolTipInputMode.Mouse:
					verticalOffset = DEFAULT_MOUSE_OFFSET;
					break;
				case AutomaticToolTipInputMode.Touch:
					verticalOffset = DEFAULT_TOUCH_OFFSET;
					break;
			}
		}

		// TODO Uno: Reverse Left/Right placement for RTL, because the target is flipped.

		// To honor horizontal/vertical offset, inflate the placement target by these offsets before calling into
		// the Windows popup positioning logic.
		rcDockTo.Inflate(horizontalOffset, verticalOffset);
		(var rcResult, var placementChosen) = ToolTipPositioning.QueryRelativePosition(
			constraint,
			szFlyout,
			rcDockTo,
			placement);

		// if right/bottom doesn't fit into the plug-in bounds, clip the ToolTip
		{
			var clipCalculationsRect = new Rect(0, 0, ActualWidth, ActualHeight);
			CalculateTooltipClip(clipCalculationsRect, visibleRect.Width - visibleRect.X, visibleRect.Height - visibleRect.Y);
		}

		// Position tooltip by setting popup's offsets
		spPopup.VerticalOffset = rcResult.Top - origin.Y;
		// ToolTipPositioning::QueryRelativePosition is used to position in LTR and RTL. In LTR, the horizontal
		// offset is  in rcResult.Left. In RTL, the horizontal offset is in rcResult.Right because the
		// popup is flipped.
		spPopup.HorizontalOffset = bIsRTL ? rcResult.Right - origin.X : rcResult.Left - origin.X;

		// There used to be a setting of FromVerticalOffset and FromHorizontalOffset on the ToolTipTemplateSettings
		// gotten from get_TemplateSettings here.  However, this is no longer done because the ToolTip animation
		// is now a FadeIn/FadeOut which doesn't use any FromHorizontalOffset/FromVerticalOffset.  This leaves
		// ToolTipTemplateSettings and all associated code in a basically non-used and deprecated state - but as of
		// now, we can't make any breaking changes.  So, to preserve max compatibility, the ToolTipTemplateSettings
		// is still around as a class/interface/property of the tooltip for now.  It should be removed (or at least
		// the ToolTip-specific-ness of the Tooltip's template settings should be removed) as soon as it's ok to
		// make a breaking change.
	}

	// MUX Reference: ToolTip_Partial.cpp CalculateTooltipClip (line 1310).
	private void CalculateTooltipClip(Rect toolTipRect, double maxX, double maxY)
	{
		var clipSize = default(Size);
		double dX;
		double dY;

		dX = toolTipRect.Left + toolTipRect.Right - maxX;
		dY = toolTipRect.Top + toolTipRect.Bottom - maxY;

		if ((dX >= 0) || (dY >= 0))
		{
			dX = Math.Max(0, dX);
			dY = Math.Max(0, dY);
			clipSize.Width = Math.Max(0, toolTipRect.Right - dX);
			clipSize.Height = Math.Max(0, toolTipRect.Bottom - dY);
			PerformClipping(clipSize);
		}
	}

	// MUX Reference: ToolTip_Partial.cpp MovePointToPointerToolTipShowPosition (line 1791).
	private void MovePointToPointerToolTipShowPosition(ref Point point, PlacementMode placement)
	{
		// If the point is inside the placement rect, move it out of the placement rect.
		var placementRect = GetPlacementRectInWindowCoordinates();

		if (!placementRect.IsEmpty && placementRect.Contains(point))
		{
			switch (placement)
			{
				case PlacementMode.Left:
					point.X = placementRect.X;
					break;
				case PlacementMode.Right:
					point.X = placementRect.X + placementRect.Width;
					break;
				case PlacementMode.Top:
				case PlacementMode.Mouse:
					point.Y = placementRect.Y;
					break;
				case PlacementMode.Bottom:
					point.Y = placementRect.Y + placementRect.Height;
					break;
			}
		}
	}

	// MUX Reference: ToolTip_Partial.cpp MovePointToPointerToolTipShowPosition (line 1822, second overload).
	private void MovePointToPointerToolTipShowPosition(ref double left, ref double top, PlacementMode placement)
	{
		var point = new Point(left, top);

		MovePointToPointerToolTipShowPosition(ref point, placement);

		left = point.X;
		top = point.Y;
	}

	// MUX Reference: ToolTip_Partial.cpp GetPlacementRectInWindowCoordinates (line 1837).
	private Rect GetPlacementRectInWindowCoordinates()
	{
		var placementRectLocal = Rect.Empty;

		if (PlacementRect is Rect placementRect)
		{
			placementRectLocal = placementRect;

			var target = GetTarget();
			if (target is not null)
			{
				var tr = target.TransformToVisual(null);
				placementRectLocal = tr.TransformBounds(placementRectLocal);
			}
		}

		return placementRectLocal;
	}

	// MUX Reference: ToolTip_Partial.cpp PerformPlacementInternal (line 1866).
	// If the owner is a TextElement, we'll get its bounding rect and use that as our placement target rect.
	// Otherwise, we'll use the default rect derived from the target.
	private void PerformPlacementInternal()
	{
		var owner = m_wrOwner?.Target as DependencyObject;
		if (owner is Documents.TextElement textElement)
		{
			// TODO Uno: GetTextElementBoundingRect equivalent on Skia. The cross-platform Uno
			// codepath was a TODO too; for now fall through to the standard PerformPlacement.
			PerformPlacement();
		}
		else
		{
			PerformPlacement();
		}
	}

	// MUX Reference: ToolTip_Partial.cpp OnToolTipSizeChanged (line 1899).
	// Handle the SizeChanged event.
	private void OnToolTipSizeChanged(object pSender, SizeChangedEventArgs pArgs)
	{
		PerformPlacementInternal();
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

	// MUX Reference: ToolTip_Partial.cpp PerformClipping (line 2007).
	private void PerformClipping(Size size)
	{
		// By default a tooltip has only 1 child (border).
		var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(this);
		if (childCount > 0)
		{
			var spChildAsFE = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
			if (spChildAsFE is not null)
			{
				if (size.Width < spChildAsFE.ActualWidth)
				{
					spChildAsFE.Width = size.Width;
				}

				if (size.Height < spChildAsFE.ActualHeight)
				{
					spChildAsFE.Height = size.Height;
				}
			}
		}
	}

	// MUX Reference: ToolTip_Partial.cpp OnRootVisualSizeChanged (line 2045).
	internal void OnRootVisualSizeChanged()
	{
		PerformPlacementInternal();
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

	// === Phase 6 helpers (safe-zone + Xaml-island roots) ===

	// MUX Reference: ToolTip_Partial.cpp IsControlKeyOnly (line 2253).
	// Returns true when the supplied key is the Control key with no other modifier
	// (Alt/Shift/Windows). Used by HookupXamlIslandRoot to implement the Ctrl-only
	// dismiss behavior (only Ctrl Down then Ctrl Up dismiss the ToolTip).
	private static bool IsControlKeyOnly(VirtualKey key)
	{
		if (key == VirtualKey.Control)
		{
			var modifiers = global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
			bool altDown = (modifiers & global::Windows.UI.Core.CoreVirtualKeyStates.Down) == global::Windows.UI.Core.CoreVirtualKeyStates.Down;

			modifiers = global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
			bool shiftDown = (modifiers & global::Windows.UI.Core.CoreVirtualKeyStates.Down) == global::Windows.UI.Core.CoreVirtualKeyStates.Down;

			modifiers = global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);
			bool winDown = (modifiers & global::Windows.UI.Core.CoreVirtualKeyStates.Down) == global::Windows.UI.Core.CoreVirtualKeyStates.Down;

			return !altDown && !shiftDown && !winDown;
		}

		return false;
	}

	// MUX Reference: ToolTip_Partial.cpp HandlePointInSafeZone (line 2221, Point overload).
	internal void HandlePointInSafeZone(Point point)
	{
		var owner = m_wrOwner?.Target as DependencyObject;
		if (owner is not null)
		{
			ToolTipService.HandleToolTipSafeZone(point, this, owner);
		}
	}

	// MUX Reference: ToolTip_Partial.cpp IsOwnerPositionChanged (line 2233).
	private bool IsOwnerPositionChanged()
	{
		var owner = m_wrOwner?.Target as DependencyObject;
		if (owner is not null)
		{
			var ownerBounds = ToolTipService.GetToolTipOwnersBoundary(owner);
			if ((Math.Abs(ownerBounds.Bottom - m_ownerBounds.Bottom) > 0.5) ||
				(Math.Abs(ownerBounds.Top - m_ownerBounds.Top) > 0.5) ||
				(Math.Abs(ownerBounds.Left - m_ownerBounds.Left) > 0.5) ||
				(Math.Abs(ownerBounds.Right - m_ownerBounds.Right) > 0.5))
			{
				return true;
			}
		}
		return false;
	}

	// MUX Reference: ToolTip_Partial.cpp UpdateOwnersBoundary (line 2268).
	private void UpdateOwnersBoundary()
	{
		var owner = m_wrOwner?.Target as DependencyObject;
		if (owner is not null)
		{
			m_ownerBounds = ToolTipService.GetToolTipOwnersBoundary(owner);
		}
	}

	// MUX Reference: ToolTip_Partial.cpp HookupXamlIslandRoot (line 2278).
	// Hooks up the CoreWindow's or XamlIslandRoot's PointerMoved event so the ToolTip can be
	// automatically closed if it's out of safe zone. On Skia the equivalent is the XamlRoot.
	private void HookupXamlIslandRoot()
	{
		// TODO Uno (Phase 6 closeout): port HookupXamlIslandRoot. The Uno Skia equivalent
		// hooks PointerMoved + Key{Down,Up} on the XamlRoot.Content. Currently no-op so
		// the open path doesn't crash; the safe-zone close fallback still runs via the
		// iter #6 immediate-close path in OnOwnerPointerExitedOrLostOrCanceled.
	}

	// MUX Reference: ToolTip_Partial.cpp HookupOwnerLayoutChangedEvent (line 2296).
	private void HookupOwnerLayoutChangedEvent()
	{
		var owner = m_wrOwner?.Target as DependencyObject;
		if (owner is FrameworkElement ownerAsFE)
		{
			global::System.EventHandler<object> handler = (sender, args) =>
			{
				if (this.Parent is not null && IsOwnerPositionChanged())
				{
					var pToolTipServiceMetadataNoRef = ToolTipService.GetToolTipServiceMetadata();

					var current = pToolTipServiceMetadataNoRef.m_tpCurrentToolTip;
					if (current is not null && ReferenceEquals(current, this))
					{
						ToolTipService.CancelAutomaticToolTip();
					}
				}
			};
			ownerAsFE.LayoutUpdated += handler;
			m_ownerLayoutUpdatedToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => ownerAsFE.LayoutUpdated -= handler);
		}
	}

	// MUX Reference: ToolTip_Partial.cpp UnhookOwnerLayoutChangedEvent (line 2337).
	private void UnhookOwnerLayoutChangedEvent()
	{
		m_ownerLayoutUpdatedToken.Disposable = null;
	}

	// MUX Reference: ToolTip_Partial.cpp UnhookFromXamlIslandRoot (line 2447).
	private void UnhookFromXamlIslandRoot()
	{
		m_xamlIslandRootPointerMovedHandler.Disposable = null;
		m_xamlIslandRootKeyDownHandler.Disposable = null;
		m_xamlIslandRootKeyUpHandler.Disposable = null;
	}

	// MUX Reference: ToolTip_Partial.cpp ForwardOwnerThemePropertyToToolTip (line 2510).
	private void ForwardOwnerThemePropertyToToolTip()
	{
		// We'll only override the requested theme on the ToolTip if its value hasn't been explicitly set.
		// Otherwise, we'll abide by its existing value.
		if (!this.IsDependencyPropertySet(RequestedThemeProperty) ||
			m_isToolTipRequestedThemeOverridden)
		{
			ElementTheme currentToolTipTheme = RequestedTheme;
			ElementTheme requestedTheme = ElementTheme.Default;
			DependencyObject? spCurrent = m_wrOwner?.Target as DependencyObject;
			FrameworkElement? spCurrentAsFE = null;

			// Walk up the tree from the placement target until we find an element with a RequestedTheme.
			while (spCurrent is not null)
			{
				if (spCurrent is FrameworkElement spCurrentAsFE2)
				{
					spCurrentAsFE = spCurrentAsFE2;

					requestedTheme = spCurrentAsFE2.RequestedTheme;
					if (requestedTheme != ElementTheme.Default)
					{
						break;
					}
				}
				else if (spCurrent is Documents.TextElement textElement)
				{
					var parent = textElement.GetContainingFrameworkElement();
					if (parent is not null)
					{
						spCurrent = parent;
						continue;
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}

				DependencyObject? spParent = Media.VisualTreeHelper.GetParent(spCurrent);
				if (spParent is Primitives.PopupRoot)
				{
					// If the target is in a Popup and the Popup is in the Visual Tree, we want to inherit the theme
					// from that Popup's parent. Otherwise we will get the App's theme, which might not be what
					// is expected.
					if (spCurrentAsFE is not null)
					{
						spParent = spCurrentAsFE.Parent;
					}
				}

				spCurrent = spParent;
			}

			// Uno-specific fallback: if the walk-up found no explicit RequestedTheme, use the
			// owner's ActualTheme. The popup-hosted ToolTip is in a parentless Popup whose
			// templated child does not always inherit the ApplicationTheme automatically on
			// Skia, so without this fallback the Background ThemeResource resolves to the
			// fallback (often Default which is not what the test asserts). Setting RequestedTheme
			// explicitly to the owner's resolved ActualTheme makes the templated child pick
			// up the right brush variant.
			if (requestedTheme == ElementTheme.Default)
			{
				var ownerFE = m_wrOwner?.Target as FrameworkElement;
				if (ownerFE is not null)
				{
					requestedTheme = ownerFE.ActualTheme;
				}
			}

			if (requestedTheme != currentToolTipTheme)
			{
				RequestedTheme = requestedTheme;
				m_isToolTipRequestedThemeOverridden = true;
			}
		}
	}

#pragma warning restore IDE0051
}

#endif // __SKIA__