//UIElement.cpp, UIElement_Partial.cpp

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DirectUI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Windows.System;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml
{
	public partial class UIElement
	{
		private const KeyboardNavigationMode UnsetKeyboardNavigationMode = (KeyboardNavigationMode)3;

		private KeyboardNavigationMode _keyboardNavigationMode = UnsetKeyboardNavigationMode;

		/// <summary>
		/// Set to True when the imminent Focus(FocusState) call needs to use an animation if bringing the focused
		/// element into view.
		/// </summary>
		private bool _animateIfBringIntoView;

		/// <summary>
		/// If true focusmgr does not set the focus on children or the element. Notice that this flag only and only
		/// regulates the focusmanager tab behavior.
		/// </summary>
		internal bool SkipFocusSubtree { get; set; }

		internal bool IsGamepadFocusCandidate { get; set; } = true;

		internal static void NWSetContentDirty(UIElement focusedElement, object render)
		{
			//TODO Uno: Might be useful to implement for Skia
		}

		internal KeyboardNavigationMode GetTabNavigation() => TabFocusNavigation;

		internal void SetTabNavigation(KeyboardNavigationMode mode)
		{
			if (!Enum.IsDefined(typeof(KeyboardNavigationMode), mode))
			{
				throw new ArgumentOutOfRangeException(nameof(mode));
			}

			_keyboardNavigationMode = mode;
		}

		/// <summary>
		/// Checks if the tab navigation value was ever set to a value.
		/// </summary>
		internal bool IsTabNavigationSet
		{
			get
			{
				DependencyPropertyValuePrecedences precedence = DependencyPropertyValuePrecedences.DefaultValue;
				if (this is Control control)
				{
					precedence = control.GetCurrentHighestValuePrecedence(Control.TabNavigationProperty);
				}

				var uiElementPrecedence = this.GetCurrentHighestValuePrecedence(UIElement.TabFocusNavigationProperty);

				if (uiElementPrecedence < precedence)
				{
					precedence = uiElementPrecedence;
				}

				return precedence < DependencyPropertyValuePrecedences.DefaultValue;
			}
		}

		internal float GetOffsetX()
		{
			var value = GetValue(Canvas.LeftProperty);
			return (value as float?) ?? 0f;
		}

		internal float GetOffsetY()
		{
			var value = GetValue(Canvas.TopProperty);
			return (value as float?) ?? 0f;
		}

		internal bool AreAllAncestorsVisible()
		{
			var pElement = this;

			while (pElement != null)
			{
				var pNext = pElement.GetUIElementAdjustedParentInternal(true /*public parents only*/);

				if (pNext?.Visibility == Visibility.Collapsed)
				{
					return false;
				}

				// TODO Uno specific: IsLeaving is not yet implemented on visual tree level,
				// so we check if the Page is being navigated away from here instead.
				if (pNext?.IsLeavingFrame == true)
				{
					return false;
				}

				pElement = pNext;
			}

			return true;
		}

		internal protected AutomationPeer? GetOrCreateAutomationPeer()
		{
			bool isPopupOpen = true;

			if (this is Popup popup)
			{
				isPopupOpen = popup.IsOpen;
			}

			// this condition checks that if Control is visible and if it's popup then it must be open
			if (Visibility != Visibility.Collapsed && isPopupOpen)
			{
				// TODO Uno: Our simplified version just returns new automation peer
				return OnCreateAutomationPeerInternal();
				//if (!m_tpAP)
				//{
				//	ctl::ComPtr<xaml_automation_peers::IAutomationPeer> spAP;
				//	if (FAILED(UIElementGenerated::OnCreateAutomationPeerProtected(&spAP)))
				//	{
				//		RRETURN(E_FAIL);
				//	}
				//	else if (!spAP)
				//	{
				//		RRETURN(S_false);
				//	}

				//	// This FX peer gains state when the AutomationPeer is stored in m_tpAP, so mark as
				//	// having state. Otherwise, a stateless FX peer will be released, which will
				//	// release the automation peer.
				//	IFC(MarkHasState());

				//	SetPtrValue(m_tpAP, spAP.Get());
				//}
			}
			else
			{
				return null;
				//if (m_tpAP)
				//{
				//	m_tpAP.Clear();
				//}
				//RRETURN(S_false);
			}
		}

		//UNO TODO: Implement GetClickablePointRasterizedClient on UIElement
		internal Point GetClickablePointRasterizedClient()
		{
			return new Point();
		}

		//TODO:MZ: Implement all these in appropriate places :-)
		internal TabStopProcessingResult ProcessTabStop(
			DependencyObject? pFocusedElement,
			DependencyObject? pCandidateTabStopElement,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			DependencyObject? spFocusedTarget = null;
			DependencyObject? spCandidateTarget = null;
			DependencyObject? spFocusedTargetParent = null;
			DependencyObject? spCandidateTargetParent = null;
			UIElement? spFocusedTargetAsUIE = null;
			UIElement? spCandidateTargetAsUIE = null;
			UIElement? spNewCandidateTargetAsUIE = null;

			var tabStopProcessingResult = new TabStopProcessingResult();
			var candidateTabStopProcessingResult = new TabStopProcessingResult();

			if (pFocusedElement != null)
			{
				spFocusedTarget = pFocusedElement;
				spFocusedTargetAsUIE = spFocusedTarget as UIElement;
				// Get the parent if it is not a UIElement. (E.g. Hyperlink)
				if (spFocusedTargetAsUIE == null)
				{
					spFocusedTargetParent = VisualTreeHelper.GetParent(spFocusedTarget);
					spFocusedTargetAsUIE = spFocusedTargetParent as UIElement;
				}
			}

			if (pCandidateTabStopElement != null)
			{
				spCandidateTarget = pCandidateTabStopElement;
				spCandidateTargetAsUIE = spCandidateTarget as UIElement;
				// Get the parent if it is not a UIElement. (E.g. Hyperlink)
				if (spCandidateTargetAsUIE == null)
				{
					spCandidateTargetParent = VisualTreeHelper.GetParent(spCandidateTarget);
					spCandidateTargetAsUIE = spCandidateTargetParent as UIElement;
				}
			}

			MUX_ASSERT(tabStopProcessingResult.NewTabStop == null);

			if (spFocusedTargetAsUIE != null)
			{
				tabStopProcessingResult = spFocusedTargetAsUIE.ProcessTabStopInternal(spCandidateTarget, isBackward, didCycleFocusAtRootVisualScope);
			}

			if (!tabStopProcessingResult.IsOverriden && spCandidateTargetAsUIE != null)
			{
				MUX_ASSERT(tabStopProcessingResult.NewTabStop == null);
				var candidateResult = spCandidateTargetAsUIE.ProcessCandidateTabStopInternal(
					spFocusedTarget,
					null,
					isBackward);
				tabStopProcessingResult.NewTabStop = candidateResult.NewTabStop;
				tabStopProcessingResult.IsOverriden = candidateResult.IsOverriden;
			}
			else if (tabStopProcessingResult.IsOverriden && tabStopProcessingResult.NewTabStop != null)
			{
				spNewCandidateTargetAsUIE = tabStopProcessingResult.NewTabStop as UIElement;
				if (spNewCandidateTargetAsUIE != null)
				{
					candidateTabStopProcessingResult = spNewCandidateTargetAsUIE.ProcessCandidateTabStopInternal(
						spFocusedTarget,
						tabStopProcessingResult.NewTabStop,
						isBackward);
				}
			}

			if (!tabStopProcessingResult.IsOverriden && !candidateTabStopProcessingResult.IsOverriden)
			{
				// Process TabStop if the application bar service is available.
				var spApplicationBarService = DXamlCore.Current.TryGetApplicationBarService();
				if (spApplicationBarService != null)
				{
					var appBarResult = spApplicationBarService.ProcessTabStopOverride(
						spFocusedTarget,
						spCandidateTarget,
						isBackward);
					tabStopProcessingResult.NewTabStop = appBarResult.NewTabStop;
					tabStopProcessingResult.IsOverriden = appBarResult.IsOverriden;

					if (tabStopProcessingResult.IsOverriden && tabStopProcessingResult.NewTabStop != null)
					{
						spNewCandidateTargetAsUIE = tabStopProcessingResult.NewTabStop as UIElement;
						if (spNewCandidateTargetAsUIE != null)
						{
							candidateTabStopProcessingResult = spNewCandidateTargetAsUIE.ProcessCandidateTabStopInternal(
								spFocusedTarget,
								tabStopProcessingResult.NewTabStop,
								isBackward);
						}
					}
				}
			}

			var result = new TabStopProcessingResult();

			if (candidateTabStopProcessingResult.IsOverriden)
			{
				if (candidateTabStopProcessingResult.NewTabStop != null)
				{
					result.NewTabStop = candidateTabStopProcessingResult.NewTabStop;
				}
				result.IsOverriden = true;
			}
			else if (tabStopProcessingResult.IsOverriden)
			{
				if (tabStopProcessingResult.NewTabStop != null)
				{
					result.NewTabStop = tabStopProcessingResult.NewTabStop;
				}
				result.IsOverriden = true;
			}

			return result;
		}

		/// <summary>
		/// Called when ProcessTabStopInternal  interact with the tab stop element.
		/// </summary>
		/// <param name="pCandidateTabStop">Candidate tab stop.</param>
		/// <param name="isBackward">True if we are navigating backward.</param>
		/// <param name="didCycleFocusAtRootVisualScope">True if the focus cycled at root visual scope.</param>
		/// <returns>Tab processing result.</returns>
		private TabStopProcessingResult ProcessTabStopInternal(
			DependencyObject? pCandidateTabStop,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			var spCurrent = this;

			var result = new TabStopProcessingResult();

			while (spCurrent != null && !(result.IsOverriden))
			{
				result = spCurrent.ProcessTabStopOverride(
					this,
					pCandidateTabStop,
					isBackward,
					didCycleFocusAtRootVisualScope);

				var spParent = VisualTreeHelper.GetParent(spCurrent);
				spCurrent = spParent as UIElement;
			}

			return result;
		}

		/// <summary>
		/// Called when ProcessCandidateTabStop  interact with the candidate tab stop element.
		/// </summary>
		/// <param name="pCurrentTabStop">Current tab stop.</param>
		/// <param name="pOverriddenCandidateTabStop">Overriden candidate tab stop.</param>
		/// <param name="isBackward">True if backward navigation.</param>
		/// <returns>Candidate tab stop processing result.</returns>
		private TabStopProcessingResult ProcessCandidateTabStopInternal(
			DependencyObject? pCurrentTabStop,
			DependencyObject? pOverriddenCandidateTabStop,
			bool isBackward)
		{
			var spCurrent = this;

			var result = new TabStopProcessingResult();

			while (spCurrent != null && !(result.IsOverriden))
			{
				result = spCurrent.ProcessCandidateTabStopOverride(
					pCurrentTabStop,
					this,
					pOverriddenCandidateTabStop,
					isBackward);

				var spParent = VisualTreeHelper.GetParent(spCurrent);
				spCurrent = spParent as UIElement;
			}

			return result;
		}

		internal virtual TabStopProcessingResult ProcessTabStopOverride(
			DependencyObject? focusedElement,
			DependencyObject? candidateTabStopElement,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			return new TabStopProcessingResult();
		}

		internal virtual TabStopProcessingResult ProcessCandidateTabStopOverride(
			DependencyObject? focusedElement,
			DependencyObject candidateTabStopElement,
			DependencyObject? overriddenCandidateTabStopElement,
			bool isBackward)
		{
			return new TabStopProcessingResult();
		}

		//TODO Uno: The following focus-related methods have custom overrides in the built-in controls. This can be ported for further compatibility.

		/// <summary>
		/// Called when FocusManager get the next TabStop to interact with the focused control.
		/// </summary>
		/// <param name="focusedElement">Currently focused element.</param>
		/// <returns>Next tab stop.</returns>
		internal virtual DependencyObject? GetNextTabStopOverride() => null;

		/// <summary>
		/// Called when FocusManager get the previous TabStop to interact with the focused control.
		/// </summary>
		/// <param name="focusedElement">Currently focused element.</param>
		/// <returns>Previous tab stop.</returns>
		internal virtual DependencyObject? GetPreviousTabStopOverride() => null;

		/// <summary>
		/// Called when FocusManager is looking for the first focusable element from the specified search scope.
		/// </summary>
		/// <returns>First focusable element or <see langword="null"/>.</returns>
		internal virtual DependencyObject? GetFirstFocusableElementOverride() => null;

		/// <summary>
		/// Called when FocusManager is looking for the last focusable element from the specified search scope.
		/// </summary>
		/// <returns>Last focusable element or <see langword="null"/>.</returns>
		internal virtual DependencyObject? GetLastFocusableElementOverride() => null;

		/// <summary>
		/// Gets the parent of element and adjust it to popup if parent is PopupRoot.
		/// </summary>
		/// <param name="publicParentsOnly">Search public parents only.</param>
		/// <param name="useRealParentForClosedParentedPopups">Use real parent for closed parented popups.</param>
		/// <returns>UIElement or null.</returns>
		internal UIElement? GetUIElementAdjustedParentInternal(bool publicParentsOnly = true, bool useRealParentForClosedParentedPopups = false)
		{
			//TODO Uno: Currently we use a very simplified version, original checks for public parents only
			//and adjusts for popup hierarchy.
			var parentDO = VisualTreeHelper.GetParent(this);
			while (parentDO != null)
			{
				if (parentDO is UIElement uiElement)
				{
					return uiElement;
				}
				else
				{
					parentDO = VisualTreeHelper.GetParent(parentDO);
				}
			}

			return null;
			////if (GetTypeIndex() == KnownTypeIndex::XamlIsland)
			////{
			////	// When an element is in a XamlIsland, we treat the XamlIsland as the root of the tree.
			////	// Content in a XamlIsland shouldn't interact with elements outside of that XamlIsland.
			////	// Example: TransformToRoot on an element within a XamlIsland shouldn't include the transform
			////	// of the RootVisual, which is the DPI scale.
			////	return null;
			////}

			//UIElement pParent = GetUIElementParentInternal(publicParentsOnly);

			//// If the immediate parent is the popup root it means this element is a
			//// Popup's child, so we want to jump to the logical parent (the Popup).
			//if (pParent != null)
			//{
			//	bool parentIsPopupRoot = false;

			//	if (SUCCEEDED(GetContext().IsObjectAnActivePopupRoot(pParent, &parentIsPopupRoot)))
			//	{
			//		// The only elements visually parented to the PopupRoot are Popup.Child and TransitionRoots.
			//		// In the former case, the adjusted parent we'll return is the Popup itself.
			//		// In the latter case, the PopupRoot will be returned as the regular visual parent for the TransitionRoot.
			//		if (parentIsPopupRoot && !OfTypeByIndex<KnownTypeIndex::TransitionRoot>())
			//		{
			//			pParent = static_cast<CUIElement*>(GetLogicalParentNoRef());
			//			ASSERT(pParent.OfTypeByIndex<KnownTypeIndex::Popup>());
			//		}
			//	}
			//}

			////
			//// If this element is a parent-less Popup or an open nested Popup inside a closed ancestor,
			//// fallback to treating the PopupRoot as its visual parent since the Popup isn't in a live tree.
			////
			//// The exception is if we're walking up the tree to gather the rendering context for a redirected
			//// walk and that this is the first step of the walk (see GetRedirectionTransformsAndParentCompNode).
			//// In that case, if the first step comes to a closed parented popup, we allow the walk to continue
			//// from the closed popup.
			////
			////  - Open parented popups are active and have a parent. The walk continues from the popup's parent.
			////
			////  - Closed parented popups aren't active but still have a parent. The walk continues from the
			////    popup's parent if closed popups are allowed. Otherwise the walk continues from the popup root.
			////
			////  - Parentless popups aren't active and have no parent, regardless of whether or not they are open.
			////    The walk continues from the popup root, because the popup by definition does not have a parent.
			////
			//if (//!IsActive()
			//	this is Popup
			//	&& (pParent == null || !useRealParentForClosedParentedPopups))
			//{
			//	PopupRoot? pPopupRoot = null;
			//	IGNOREHR(GetContext().GetAdjustedPopupRootForElement(this, pPopupRoot));
			//	pParent = pPopupRoot;
			//}

			//return pParent;
		}

		//UNO TODO: Implement GetUIElementParentInternal on UIElement
		internal DependencyObject GetAccessKeyScopeOwner()
		{
			throw new NotImplementedException("GetUIElementParentInternal is not implemented on UIElement");
		}

		/// <summary>
		/// Override this method and return TRUE in order to navigate among automation children in reverse order.
		/// </summary>
		internal virtual bool AreAutomationPeerChildrenReversed() => false;

		/// <summary>
		/// Default to false and expose as needed.  Elements that don't support having children will never
		/// allocate children collections.  Elements that do support children may do so as an implementation
		/// detail (e.g. selection grippers for TextBlock), or to support public API exposure (e.g. Panel.Children).
		/// </summary>
		internal virtual bool CanHaveChildren() => false;

		internal DependencyObject? GetRootOfPopupSubTree()
		{
			DependencyObject? pParent = this.GetParentInternal(false);
			DependencyObject pChild = this;

			while (pParent != null)
			{
				if (pParent is PopupRoot)
				{
					return pChild;
				}
				pChild = pParent;
				pParent = pParent.GetParentInternal(false);
			}
			return null;
		}

		internal bool Focus(FocusState focusState, bool animateIfBringIntoView, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
		{
			// Get FocusManager
			var pFocusManager = VisualTree.GetFocusManagerForElement(this, VisualTree.LookupOptions.NoFallback);

			if (pFocusManager == null)
			{
				return false;
			}

			// FocusMovement is OK with NULL parameter, sets focusChanged to false
			FocusMovement movement = new FocusMovement(this, focusNavigationDirection, focusState);
			movement.AnimateIfBringIntoView = animateIfBringIntoView;
			FocusMovementResult result = pFocusManager.SetFocusedElement(movement);
			var focusChanged = result.WasMoved;

			return focusChanged;
		}

		internal Rect GetGlobalBounds(bool ignoreClipping)
		{
			//TODO Uno specific: This implementation is significantly simplified from the actual WinUI implementation
			return GetGlobalBoundsLogical(ignoreClipping);
		}

		internal Rect GetGlobalBoundsLogical(bool ignoreClipping = false, bool useTargetInformation = false)
		{
			//TODO Uno specific: This implementation is significantly simplified from the actual WinUI implementation.
			var rootVisual = VisualTree.GetRootForElement(this) ?? VisualTree.GetRootOrIslandForElement(this);
			if (rootVisual == null)
			{
				return Rect.Empty;
			}
			else
			{
				var transformToRoot = this.TransformToVisual(rootVisual);
				var topLeft = transformToRoot.TransformPoint(Point.Zero);
				return new Rect(topLeft.X, topLeft.Y, this.GetActualWidth(), this.GetActualHeight());
			}
		}

		internal void SetAnimateIfBringIntoView()
		{
			MUX_ASSERT(!_animateIfBringIntoView);
			_animateIfBringIntoView = true;
		}

		internal bool FocusImpl(FocusState focusState) =>
			FocusWithDirection(focusState, FocusNavigationDirection.None);

		internal bool FocusWithDirection(FocusState focusState, FocusNavigationDirection focusNavigationDirection)
		{
			// Throw if customer tries to call Focus(FocusState.Unfocused).
			if (FocusState.Unfocused == focusState)
			{
				throw new ArgumentOutOfRangeException(nameof(focusState), "Focus state Unfocused cannot be used when calling Focus.");
			}

			bool animateIfBringIntoView = _animateIfBringIntoView;
			_animateIfBringIntoView = false;

			var focusUpdated = Focus(focusState, animateIfBringIntoView, focusNavigationDirection);

			return focusUpdated;
		}

		protected virtual IEnumerable<DependencyObject>? GetChildrenInTabFocusOrder()
		{
			var children = FocusProperties.GetFocusChildren(this);
			if (children != null && /*!children.IsLeaving() && */children.Length > 0)
			{
				return children;
			}
			return Array.Empty<DependencyObject>();
		}

		internal IEnumerable<DependencyObject>? GetChildrenInTabFocusOrderInternal() => GetChildrenInTabFocusOrder();

		internal bool IsOccluded(UIElement? childElement, Rect elementBounds)
		{
			//TODO Uno: properly check for occlusivity.
			return false;
			//var tester = new OcclusivityTester(this);
			//return tester.Test(childElement, elementBounds);
		}

		/// <summary>
		/// Based on UIElement.IsScroller in MUX. In MUX a private field is used,
		/// this is a simplification.
		/// </summary>
		/// <param name="uiElement">UIElement.</param>
		/// <returns>True if the element is a ScrollViewer.</returns>
		internal bool IsScroller() => this is ScrollViewer;

		private void OnKeyDown(KeyRoutedEventArgs pEventArgs)
		{
#if HAS_UNO // Uno specific: In case of WinUI this logic is called only
			// if not already handled and only on non-controls
			if (pEventArgs.Handled)
			{
				return;
			}

			if (this is Control control)
			{
				return;
			}
#endif

			/*
			1. We take different paths for raising events depending on whether the source is a UIElement or a Control
			2. The DXAML layer OnKeyDown virtual is defined on Control

			As a result, we execute similar logic to process KeyboardAccelerators in both CUIElement::OnKeyDown and Control::OnKeyDown
			One deals with controls, this deals with all other UIElements.
			*/
			KeyRoutedEventArgs pKeyRoutedEventArgs = (KeyRoutedEventArgs)pEventArgs;
			bool handled = false;
			bool handledShouldNotImpedeTextInput = false;
			VirtualKey dxamlOriginalKey;

			dxamlOriginalKey = pKeyRoutedEventArgs.OriginalKey;

			VirtualKey originalKey = dxamlOriginalKey;

			var keyModifiers = CoreImports.Input_GetKeyboardModifiers();

			if (KeyboardAcceleratorUtility.IsKeyValidForAccelerators(originalKey, KeyboardAcceleratorUtility.MapVirtualKeyModifiersToIntegersModifiers(keyModifiers)))
			{
				KeyboardAcceleratorUtility.ProcessKeyboardAccelerators(
					originalKey,
					keyModifiers,
					VisualTree.GetContentRootForElement(this)!.GetAllLiveKeyboardAccelerators(),
					this,
					out handled,
					out handledShouldNotImpedeTextInput,
					null,
					false);

				if (handled)
				{
					pKeyRoutedEventArgs.Handled = true;
				}
				if (handledShouldNotImpedeTextInput)
				{
					pKeyRoutedEventArgs.HandledShouldNotImpedeTextInput = true;
				}
			}
		}

		// Implements a depth-first search of the element's sub-tree,
		// looking for an accelerator that can be invoked
		private static void TryInvokeKeyboardAccelerator(
			DependencyObject? pFocusedElement,
			UIElement pElement,
			VirtualKey key,
			VirtualKeyModifiers keyModifiers,
			ref bool handled,
			ref bool handledShouldNotImpedeTextInput)
		{
			//Try to process accelerators on current CUIElement.
			KeyboardAcceleratorUtility.ProcessKeyboardAccelerators(
				key,
				keyModifiers,
				VisualTree.GetContentRootForElement(pElement)!.GetAllLiveKeyboardAccelerators(),
				pElement,
				out handled,
				out handledShouldNotImpedeTextInput,
				pFocusedElement,
				true /*isCallFromTryInvoke*/ );
			if (handled)
			{
				return;
			}

			IEnumerable<DependencyObject>? pCollection = null;
			if (pElement.CanHaveChildren())
			{
				pCollection = Uno.UI.Extensions.DependencyObjectExtensions.GetChildren(pElement);
			}

			if (pCollection is null)
			{
				return;
			}

			//For each child make recursive call
			foreach (var pDOChild in pCollection)
			{
				var pChild = pDOChild as UIElement;
				if (pChild is not null && pChild.IsEnabled())
				{
					TryInvokeKeyboardAccelerator(pFocusedElement, pChild, key, keyModifiers, ref handled, ref handledShouldNotImpedeTextInput);
					if (handled)
					{
						return;
					}
				}
			}
		}

		/* static */
		internal static bool RaiseKeyboardAcceleratorInvokedStatic(
			DependencyObject pElement,
			KeyboardAcceleratorInvokedEventArgs pKAIEventArgs)
		{
			var peer = pElement;
			if (peer is null)
			{
				return false;
			}

			var element = peer as UIElement;
			if (element is null)
			{
				return false;
			}

			element.OnKeyboardAcceleratorInvoked(pKAIEventArgs);
			return pKAIEventArgs.Handled;
		}

		internal static void RaiseProcessKeyboardAcceleratorsStatic(
			UIElement pUIElement,
			VirtualKey key,
			VirtualKeyModifiers keyModifiers,
			ref bool pHandled,
			ref bool pHandledShouldNotImpedeTextInput)
		{
			DependencyObject peer = pUIElement;
			if (peer is null)
			{
				return;
			}
			UIElement element = pUIElement;

			ProcessKeyboardAcceleratorEventArgs spProcessKeyboardAcceleratorEventArgs = new(key, keyModifiers);

			element.OnProcessKeyboardAccelerators(spProcessKeyboardAcceleratorEventArgs);

			pHandled = spProcessKeyboardAcceleratorEventArgs.Handled;
			pHandledShouldNotImpedeTextInput = spProcessKeyboardAcceleratorEventArgs.HandledShouldNotImpedeTextInput;

			if (!pHandled)
			{
				element.ProcessKeyboardAccelerators?.Invoke(element, spProcessKeyboardAcceleratorEventArgs);

				pHandled = spProcessKeyboardAcceleratorEventArgs.Handled;
				pHandledShouldNotImpedeTextInput = spProcessKeyboardAcceleratorEventArgs.HandledShouldNotImpedeTextInput;
			}
		}

		protected virtual void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
		{
			FlyoutBase spFlyout = ContextFlyout;
			if (spFlyout is not null)
			{
				spFlyout.TryInvokeKeyboardAccelerator(args);

				bool bHandled = args.Handled;
				if (bHandled)
				{
					return;
				}
			}

			// If event is not yet handled and current element is Button then TryInvoke on Flyout if it exists.
			if (this is Button button)
			{
				button.OnProcessKeyboardAcceleratorsImplLocal(args);
			}
		}

		/// <summary>
		/// Attempts to invoke a keyboard shortcut (or accelerator) by searching the entire visual tree of the UIElement for the shortcut.
		/// </summary>
		/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
		public void TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args)
		{
			// Search for an accelerator that can be invoked
			bool handled = false;
			bool handledShouldNotImpedeTextInput = false;
			VirtualKey key = args.Key;
			VirtualKeyModifiers keyModifiers = args.Modifiers;
			if (KeyboardAcceleratorUtility.IsKeyValidForAccelerators(key, KeyboardAcceleratorUtility.MapVirtualKeyModifiersToIntegersModifiers(keyModifiers)))
			{
				// Get the focused element
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				var pFocusedElement = focusManager?.FocusedElement;

				UIElement.TryInvokeKeyboardAccelerator(pFocusedElement, this, key, keyModifiers, ref handled, ref handledShouldNotImpedeTextInput);
				args.Handled = handled;
				args.HandledShouldNotImpedeTextInput = handledShouldNotImpedeTextInput;
			}
		}

		//UNO TODO: Implement GetGlobalBoundsWithOptions on UIElement
		internal Rect GetGlobalBoundsWithOptions(bool ignoreClipping, bool ignoreClippingOnScrollContentPresenters, bool useTargetInformation)
		{
			return new Rect();
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		// Doesn't exactly match WinUI code.
		internal virtual void Enter(EnterParams @params, int depth)
		{
			Depth = depth;

			if (@params.IsLive)
			{
				IsActiveInVisualTree = true;
			}

			foreach (var child in _children)
			{
				if (child == this)
				{
					// In some cases, we end up with ContentPresenter having itself as a child.
					// Initial investigation: ScrollViewer sets its Content to ContentPresenter, and
					// ContentPresenter sets its Content as the ScrollViewer Content.
					// Skip this case for now.
					// TODO: Investigate this more deeply.
					continue;
				}

				child.Enter(@params, depth + 1);
			}

			if (@params.IsLive)
			{
				var core = this.GetContext();
				core.EventManager.AddRequestsInOrder(this);
			}

			// Make sure that we propagate OnDirtyPath bits to the new parent.
			SetLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.ArrangeDirty);
		}

		internal virtual void Leave(LeaveParams @params)
		{
			foreach (var child in _children)
			{
				if (child == this)
				{
					// In some cases, we end up with ContentPresenter having itself as a child.
					// Initial investigation: ScrollViewer sets its Content to ContentPresenter, and
					// ContentPresenter sets its Content as the ScrollViewer Content.
					// Skip this case for now.
					// TODO: Investigate this more deeply.
					continue;
				}

				child.Leave(@params);
			}

			if (IsActiveInVisualTree)
			{
				if (IsLoaded)
				{
					// This doesn't match WinUI.
					// On WinUI, Unloaded can be fired even if Loaded is not fired.
					// However, currently this is problematic due to bugs in ContentControl.
					// Mainly, when we set ContentControl.Content, the content is added as a direct child to ContentControl
					// Then, at some point later, we'll need to attach the Content to ContentPresenter instead of ContentControl directly.
					// This "re-attaching" of Content will cause it to leave the visual tree and fire unloaded, which is not correct.
					// In WinUI, ContentControl works differently.
					//
					// Another reason why we may not want to match WinUI behavior is that we rely on Loaded/Unloaded for event subscriptions/unsubscriptions in many controls
					// Matching WinUI behavior can make those control go into bad state and/or memory leaks in niche cases.
					// This can be revisited in the future if it caused issues.
					OnElementUnloaded();
				}

				var eventManager = this.GetContext().EventManager;
				eventManager.RemoveRequest(this);
			}
		}
#endif
	}
}
