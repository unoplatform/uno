// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference SplitView.cpp, tag winui3/release/1.4.2

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
namespace Microsoft.UI.Xaml.Controls;

public partial class SplitView
{
	private RectangleGeometry m_spPaneClipRectangle;

	private UIElement m_spPaneRoot;
	private UIElement m_spContentRoot;
	private UIElement m_spLightDismissLayer;

	private WeakReference<DependencyObject> m_spPrevFocusedElementWeakRef;
	private FocusState m_prevFocusState = FocusState.Unfocused;

	private bool m_isPaneClosingByLightDismiss;
	private double m_paneMeasuredLength;

	// CSplitView::~CSplitView()
	// {
	// 	NT_VERIFY(SUCCEEDED(UnregisterEventHandlers()));
	//
	// 	ReleaseInterface(m_pTemplateSettings);
	// }

	// _Check_return_ HRESULT
	// CSplitView::InitInstance()
	// {
	// 	CREATEPARAMETERS cp(GetContext());
	// 	IFC_RETURN(CSplitViewTemplateSettings::Create((CDependencyObject **)&m_pTemplateSettings, &cp));
	//
	// 	SetIsCustomType();
	//
	// 	return S_OK;
	// }

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// IFC_RETURN(__super::OnPropertyChanged(args));

		if (args.Property == DisplayModeProperty)
		{
			// Uno Specific: Originally in OnPropertyChanged2
			OnDisplayModeChanged();

			RestoreSavedFocusElement();
			UpdateVisualState(true);
		}
		else if (args.Property == PanePlacementProperty || args.Property == LightDismissOverlayModeProperty)
		{
			UpdateVisualState(true);
		}
		else if (args.Property == OpenPaneLengthProperty || args.Property == CompactPaneLengthProperty)
		{
			UpdateTemplateSettings();

			// Force the bindings in our VisualState animations to refresh by intentionally
			// passing in false for 'useTransitions.'
			UpdateVisualState(false);
		}
		else if (args.Property == IsPaneOpenProperty)
		{
			// Uno Specific: Originally in OnPropertyChanged2
			OnIsPaneOpenChanged((bool)args.NewValue);
		}
	}

	// Uno Doc: CSplitView's OnApplyTemplate
	private void COnApplyTemplate()
	{
		UnregisterEventHandlers();

		// Clear any hold-overs from the previous template.
		m_spPaneClipRectangle = null;
		m_spContentRoot = null;
		m_spPaneRoot = null;
		m_spLightDismissLayer = null;

		base.OnApplyTemplate();

		DependencyObject spTemplateChild = GetTemplateChild("PaneClipRectangle");
		m_spPaneClipRectangle = spTemplateChild as RectangleGeometry;

		spTemplateChild = GetTemplateChild("ContentRoot");
		m_spContentRoot = spTemplateChild as UIElement;

		spTemplateChild = GetTemplateChild("PaneRoot");
		m_spPaneRoot = spTemplateChild as UIElement;

		spTemplateChild = GetTemplateChild("LightDismissLayer");
		m_spLightDismissLayer = spTemplateChild as UIElement;

		RegisterEventHandlers();
		UpdateTemplateSettings();
		UpdateVisualState(true);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		// Measure the pane content so that we can use the desired size in cases
		// where open pane length is set to Auto.
		var value = Pane;
		if (value is { })
		{
			var pPaneElement = value;

			pPaneElement.Measure(availableSize);

			m_paneMeasuredLength = pPaneElement.DesiredSize.Width;
		}

		var @out = base.MeasureOverride(availableSize);

		UpdateTemplateSettings();

		return @out;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var newFinalSize = base.ArrangeOverride(finalSize);

		if (m_spPaneClipRectangle is { })
		{
			Rect rect = new Rect(0, 0, OpenPaneLength, newFinalSize.Height);
			m_spPaneClipRectangle.Rect = rect;
		}

		return newFinalSize;
	}

	private bool IsLightDismissible()
	{
		return DisplayMode is not (SplitViewDisplayMode.Inline or SplitViewDisplayMode.CompactInline);
	}

	private bool CanLightDismiss()
	{
		return IsPaneOpen && !m_isPaneClosingByLightDismiss && IsLightDismissible();
	}

	private double GetOpenPaneLength()
	{
		double openPaneLength = OpenPaneLength;

		// Support Auto/NaN for open pane length to size to the pane content.
		if (openPaneLength.IsNaN())
		{
			openPaneLength = m_paneMeasuredLength;
		}

		return openPaneLength;
	}

	private void TryCloseLightDismissiblePane()
	{
		global::System.Diagnostics.Debug.Assert(CanLightDismiss());

		var core = this.GetContext();
		SplitViewPaneClosingEventArgs spArgs = new();

		// Uno Specific
		// // Raise the closing event to give the app a chance to cancel.
		// CEventManager *pEventManager = core->GetEventManager();
		// if (pEventManager != nullptr)
		// {
		// 	pEventManager->Raise(
		// 		EventHandle(KnownEventIndex::SplitView_PaneClosing),
		// 		TRUE /*bRefire*/,
		// 		this,
		// 		spArgs,
		// 		FALSE /*fRaiseSync*/
		// 	);
		// }
		DispatcherQueue.GetForCurrentThread().TryEnqueue(() => PaneClosing?.Invoke(this, spArgs));

		// Uno Specific
		// // Queue up a deferred UI thread executor that will actually close the pane
		// // based on whether it was canceled or not.
		// SplitViewPaneClosingExecutor spSplitViewExecutor;
		// spSplitViewExecutor.init(new CSplitViewPaneClosingExecutor(this, spArgs.get()));
		// IFC_RETURN(core->ExecuteOnUIThread(spSplitViewExecutor, ReentrancyBehavior::CrashOnReentrancy));
		DispatcherQueue.GetForCurrentThread().TryEnqueue(() => new SplitViewPaneClosingExecutor(this, spArgs).Execute());

		// Flag that we're attempting to close so that we don't queue up multiple of these messages.
		m_isPaneClosingByLightDismiss = true;
	}

	internal void OnCancelClosing()
	{
		m_isPaneClosingByLightDismiss = false;
	}

	// CSplitView::ProcessTabStop(
	//     _In_ bool isForward,
	//     _In_opt_ CDependencyObject* pFocusedElement,
	//     _In_opt_ CDependencyObject* pCandidateTabStopElement
	//     )
	// {
	//     CDependencyObject* newTabStop = nullptr;
	//
	//     // Panes that can be be light dismissed hold onto focus until they're closed.
	//     if (CanLightDismiss() && m_spPaneRoot)
	//     {
	//         if (pFocusedElement != nullptr)
	//         {
	//             // If the element losing focus is in our pane, then we evaluate the candidate element to
	//             // determine whether we need to override it.
	//             const bool isFocusedElementInPane = m_spPaneRoot->IsAncestorOf(pFocusedElement);
	//             if (isFocusedElementInPane)
	//             {
	//                 bool doOverrideCandidate = false;
	//
	//                 if (pCandidateTabStopElement != nullptr)
	//                 {
	//                     // If the candidate element isn't in the pane, we need to override it to keep focus within the pane.
	//                     doOverrideCandidate = m_spPaneRoot->IsAncestorOf(pCandidateTabStopElement) == false;
	//                 }
	//                 else
	//                 {
	//                     // If there's no candidate, then we need to make sure focus stays within the pane.
	//                     doOverrideCandidate = true;
	//                 }
	//
	//                 if (doOverrideCandidate)
	//                 {
	//                     auto pFocusManager = VisualTree::GetFocusManagerForElement(m_spPaneRoot);
	//                     if (pFocusManager != nullptr)
	//                     {
	//                         auto pNewTabStop = static_cast<CDependencyObject*>(
	//                             isForward ? pFocusManager->GetFirstFocusableElement(m_spPaneRoot) : pFocusManager->GetLastFocusableElement(m_spPaneRoot));
	//
	//                         if (pNewTabStop)
	//                         {
	//                             AddRefInterface(pNewTabStop);
	//                             newTabStop = pNewTabStop;
	//                         }
	//                     }
	//                 }
	//             }
	//         }
	//     }
	//
	//     return newTabStop;
	// }

	private DependencyObject GetFirstFocusableElementFromPane()
	{
		if (m_spPaneRoot is { })
		{
			var focusManager = VisualTree.GetFocusManagerForElement(m_spPaneRoot);
			if (focusManager != null)
			{
				var element = focusManager.GetFirstFocusableElement(m_spPaneRoot);
				if (element != null)
				{
					return element;
				}
			}
		}

		return null;
	}

	private DependencyObject GetLastFocusableElementFromPane()
	{
		if (m_spPaneRoot is { })
		{
			var focusManager = VisualTree.GetFocusManagerForElement(m_spPaneRoot);
			if (focusManager != null)
			{
				var element = focusManager.GetLastFocusableElement(m_spPaneRoot);
				if (element != null)
				{
					return element;
				}
			}
		}

		return null;
	}

	private void UpdateTemplateSettings()
	{
		double openPaneLength = GetOpenPaneLength();
		double compactLength = CompactPaneLength;

		// Set the template settings values.
		TemplateSettings.OpenPaneLength = openPaneLength;

		TemplateSettings.NegativeOpenPaneLength = openPaneLength * -1;

		TemplateSettings.OpenPaneLengthMinusCompactLength = openPaneLength - compactLength;

		TemplateSettings.NegativeOpenPaneLengthMinusCompactLength = compactLength - openPaneLength;

		GridLength gridLength = openPaneLength;
		TemplateSettings.OpenPaneGridLength = gridLength;

		gridLength = compactLength;
		TemplateSettings.CompactPaneGridLength = gridLength;
	}

	private void RegisterEventHandlers()
	{
		// Uno Specific: the override takes care of subscribing
		// handler.SetInternalHandler(OnKeyDown);
		// IFC_RETURN(AddEventListener(
		// 	EventHandle(KnownEventIndex::UIElement_KeyDown),
		// 	&handler,
		// 	EVENTLISTENER_INTERNAL,
		// 	nullptr)
		// );

		if (m_spLightDismissLayer is { })
		{
			m_spLightDismissLayer.PointerReleased += OnLightDismissLayerPointerReleased;
		}
	}

	private void UnregisterEventHandlers()
	{
		// Uno Specific: the override takes care of unsubscribing
		// handler.SetInternalHandler(OnKeyDown);
		// IFC_RETURN(RemoveEventListener(EventHandle(KnownEventIndex::UIElement_KeyUp), &handler));

		if (m_spLightDismissLayer is { })
		{
			m_spLightDismissLayer.PointerReleased -= OnLightDismissLayerPointerReleased;
		}
	}

	private void OnPaneOpening()
	{
		// Try to focus the pane if it's light-dismissible.
		if (IsLightDismissible() && m_spPaneRoot is { })
		{
			SetFocusToPane();
		}
	}

	private void OnPaneClosing()
	{
		// If the closing flag isn't set, then we're not closing due to some light-dismissible
		// action but rather are closing because the app explicitly set IsPaneOpen = false.
		// In this case, we haven't fired the PaneClosing event yet, so do it now before we
		// fire the PaneClosed event.  Note, this closing action is not cancelable, so we
		// don't care if the app sets the Cancel property on the closing event args.
		if (!m_isPaneClosingByLightDismiss)
		{
			SplitViewPaneClosingEventArgs spArgs = new();

			// Uno Specific
			// // Raise the closing event to give the app a chance to cancel.
			// CEventManager *pEventManager = GetContext()->GetEventManager();
			// if (pEventManager != nullptr)
			// {
			// 	pEventManager->Raise(
			// 		EventHandle(KnownEventIndex::SplitView_PaneClosing),
			// 		TRUE /*bRefire*/,
			// 		this /*pSender*/,
			// 		spArgs,
			// 		FALSE /*fRaiseSync*/
			// 	);
			// }
			DispatcherQueue.GetForCurrentThread().TryEnqueue(() => PaneClosing?.Invoke(this, spArgs));
		}

		if (IsLightDismissible())
		{
			RestoreSavedFocusElement();
		}
	}

	private void OnPaneClosed()
	{
		m_isPaneClosingByLightDismiss = false;

		// Uno Specific
		// // Fire the closed event.
		// CEventManager *pEventManager = GetContext()->GetEventManager();
		// if (pEventManager != nullptr)
		// {
		//     pEventManager->Raise(
		//         EventHandle(KnownEventIndex::SplitView_PaneClosed),
		//         TRUE /*bRefire*/,
		//         this /*pSender*/,
		//         nullptr /*pArgs*/,
		//         FALSE /*fRaiseSync*/
		//         );
		// }
		DispatcherQueue.GetForCurrentThread().TryEnqueue(() => PaneClosed?.Invoke(this, null));
	}

	private void SetFocusToPane()
	{
		// Store weak reference to the previously focused element.
		var pFocusManager = VisualTree.GetFocusManagerForElement(this);
		if (pFocusManager != null)
		{
			var pPrevFocusedElement = pFocusManager.FocusedElement;
			if (pPrevFocusedElement != null)
			{
				m_spPrevFocusedElementWeakRef = new WeakReference<DependencyObject>(pPrevFocusedElement);
				m_prevFocusState = pFocusManager.GetRealFocusStateForFocusedElement();
			}
		}

		if (m_prevFocusState == FocusState.Unfocused)
		{
			// We will give the pane focus using the same focus state as that of the currently focused element
			// If there is no currently focused element we will fall back to Programmatic focus state.
			m_prevFocusState = FocusState.Programmatic;
		}

		// Put focus on the pane.
		if (m_spPaneRoot is { })
		{
			global::System.Diagnostics.Debug.Assert(m_prevFocusState != FocusState.Unfocused);
			// We'll use the previous focus state when setting focus to the pane.
			bool wasFocused = m_spPaneRoot.Focus(m_prevFocusState, false /*animateIfBringIntoView*/);
		}
	}

	private void RestoreSavedFocusElement()
	{
		if (m_spPrevFocusedElementWeakRef is { })
		{
			bool wasFocusRestored = false;
			var pPrevFocusedElement = m_spPrevFocusedElementWeakRef.TryGetTarget(out var target) ? target : null;

			// Restore focus to our cached element.
			if (pPrevFocusedElement != null)
			{
				var pFocusManager = VisualTree.GetFocusManagerForElement(pPrevFocusedElement);
				// UnoSpecific using FocusProperties.IsFocusable instead of FocusManager.IsFocusable
				if (pFocusManager is { } && FocusProperties.IsFocusable(pPrevFocusedElement))
				{
					FocusMovementResult result = pFocusManager.SetFocusedElement(new FocusMovement(pPrevFocusedElement, FocusNavigationDirection.None, m_prevFocusState));
					wasFocusRestored = result.WasMoved;
				}
			}

			// If we failed to restore focus, then try to focus an item in the content area.
			if (!wasFocusRestored && m_spContentRoot is { })
			{
				bool wasFocused = m_spContentRoot.Focus(m_prevFocusState, false /*animateIfBringIntoView*/);
				// TODO: What do do if it fails?  Bug 9810211
			}

			// Reset our saved focus information.
			m_spPrevFocusedElementWeakRef = null;
			m_prevFocusState = FocusState.Unfocused;
		}
	}

	// Uno Doc: CSplitView's OnApplyTemplate
	// Uno Specific: takes a KeyRoutedEventArgs instead of EventsArgs + cast to KeyEventArgs
	private void COnKeyDown(KeyRoutedEventArgs eventArgs)
	{
		var splitView = this;
		var keyEventArgs = eventArgs;
		var pFocusManager = VisualTree.GetFocusManagerForElement(splitView);

		//Only consume the Back Key/Trap the focus within the pane
		//If Pane is open and the Display mode is either Overlay or CompatOverlay
		//For Inline modes, Auto-focus already handles the key handling because the sub-tree
		//participates in layout.
		if (pFocusManager is { } && splitView.CanLightDismiss())
		{
			DependencyObject currentFocusedElement = pFocusManager.FocusedElement;
			DependencyObject autoFocusCandidate = null;

			XYFocusOptions xyFocusOptions = XYFocusOptions.Default;

			FocusNavigationDirection navigationDirection = FocusNavigationDirection.None;
			// Uno Specific: slightly modified to work with virtual keys instead of key codes
			switch (keyEventArgs.OriginalKey)
			{
				case VirtualKey.Escape:
				case VirtualKey.GamepadB:
					{
						keyEventArgs.Handled = true;
						splitView.TryCloseLightDismissiblePane();
						break;
					}
				case VirtualKey.GamepadDPadLeft:
				case VirtualKey.GamepadLeftThumbstickLeft:
					{
						keyEventArgs.Handled = true;
						navigationDirection = FocusNavigationDirection.Left;
						autoFocusCandidate = pFocusManager.FindNextFocus(new FindFocusOptions(navigationDirection), xyFocusOptions);
						break;
					}
				case VirtualKey.GamepadDPadRight:
				case VirtualKey.GamepadLeftThumbstickRight:
					{
						keyEventArgs.Handled = true;
						navigationDirection = FocusNavigationDirection.Right;
						autoFocusCandidate = pFocusManager.FindNextFocus(new FindFocusOptions(navigationDirection), xyFocusOptions);
						break;
					}
				case VirtualKey.GamepadDPadUp:
				case VirtualKey.GamepadLeftThumbstickUp:
					{
						keyEventArgs.Handled = true;
						navigationDirection = FocusNavigationDirection.Up;
						autoFocusCandidate = pFocusManager.FindNextFocus(new FindFocusOptions(navigationDirection), xyFocusOptions);
						break;
					}
				case VirtualKey.GamepadDPadDown:
				case VirtualKey.GamepadLeftThumbstickDown:
					{
						keyEventArgs.Handled = true;
						navigationDirection = FocusNavigationDirection.Down;
						autoFocusCandidate = pFocusManager.FindNextFocus(new FindFocusOptions(navigationDirection), xyFocusOptions);
						break;
					}
			}

			if (currentFocusedElement is { } && autoFocusCandidate is { })
			{
				bool isFocusedElementInPane = splitView.m_spPaneRoot.IsAncestorOf(currentFocusedElement);

				if (isFocusedElementInPane)
				{
					bool isCandidateElementInPane = splitView.m_spPaneRoot.IsAncestorOf(autoFocusCandidate);

					//Only set focus to an auto-focus candidate if it lives in m_spPaneRoot sub-tree, swallow the input otherwise
					if (isCandidateElementInPane)
					{
						FocusMovementResult result = pFocusManager.SetFocusedElement(new FocusMovement(autoFocusCandidate, navigationDirection, FocusState.Keyboard));
						keyEventArgs.Handled = result.WasMoved;
					}
				}
			}
		}
	}

	// Uno Specific: PointerRoutedEventArgs instead of EventArgs
	private static void OnLightDismissLayerPointerReleased(object sender, PointerRoutedEventArgs eventArgs)
	{
		var splitView = ((UIElement)sender).TemplatedParent as SplitView;

		if (splitView != null && splitView.CanLightDismiss())
		{
			splitView.TryCloseLightDismissiblePane();

			var routedEventArgs = eventArgs;
			routedEventArgs.Handled = true;
		}
	}

	// // Content and Pane are labeled as non-visual-tree properties, meaning they won't get Enter/Leave walks by the property
	// // system when set in sparse storage. This is intentional, since the property system uses live Enter/Leave, which we want
	// // to avoid because the one live Enter/Leave walk should happen through the visual child collection. We do want non-live
	// // Enter/Leave walks though, so override Enter/Leave and kick those off manually.
	// _Check_return_ HRESULT
	// CSplitView::EnterImpl(_In_ CDependencyObject *pNamescopeOwner, EnterParams params)
	// {
	// 	if (!params.fSkipNameRegistration)
	// 	{
	// 		EnterParams newParams(params);
	// 		newParams.fIsLive = FALSE;
	//
	// 		if (m_spPaneRoot)
	// 		{
	// 			IFC_RETURN(m_spPaneRoot->Enter(pNamescopeOwner, newParams));
	// 		}
	//
	// 		if (m_spContentRoot)
	// 		{
	// 			IFC_RETURN(m_spContentRoot->Enter(pNamescopeOwner, newParams));
	// 		}
	// 	}
	//
	// 	IFC_RETURN(__super::EnterImpl(pNamescopeOwner, params));
	//
	// 	return S_OK;
	// }
	//
	// // Content and Pane are labeled as non-visual-tree properties, meaning they won't get Enter/Leave walks by the property
	// // system when set in sparse storage. This is intentional, since the property system uses live Enter/Leave, which we want
	// // to avoid because the one live Enter/Leave walk should happen through the visual child collection. We do want non-live
	// // Enter/Leave walks though, so override Enter/Leave and kick those off manually.
	// _Check_return_ HRESULT
	// CSplitView::LeaveImpl(_In_ CDependencyObject *pNamescopeOwner, LeaveParams params)
	// {
	// 	if (!params.fSkipNameRegistration)
	// 	{
	// 		LeaveParams newParams(params);
	// 		newParams.fIsLive = FALSE;
	//
	// 		if (m_spPaneRoot)
	// 		{
	// 			IFC_RETURN(m_spPaneRoot->Leave(pNamescopeOwner, newParams));
	// 		}
	//
	// 		if (m_spContentRoot)
	// 		{
	// 			IFC_RETURN(m_spContentRoot->Leave(pNamescopeOwner, newParams));
	// 		}
	// 	}
	//
	// 	IFC_RETURN(__super::LeaveImpl(pNamescopeOwner, params));
	//
	// 	return S_OK;
	// }
	//
	// // Event Args
	// _Check_return_ HRESULT CSplitViewPaneClosingEventArgs::CreateFrameworkPeer(_Outptr_ IInspectable** ppPeer)
	// {
	// 	RRETURN(DirectUI::OnFrameworkCreateSplitViewPaneClosingEventArgs(this, ppPeer));
	// }
}
