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
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Collections;
using Windows.System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml
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
			if (!Enum.IsDefined(mode))
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

		internal AutomationPeer? GetOrCreateAutomationPeer()
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
			if (children != null && /*!children.IsLeaving() && */children.Count > 0)
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
			var contentRoot = VisualTree.GetContentRootForElement(pElement);

			if (contentRoot is null)
			{
				return;
			}

			//Try to process accelerators on current CUIElement.
			KeyboardAcceleratorUtility.ProcessKeyboardAccelerators(
				key,
				keyModifiers,
				contentRoot.GetAllLiveKeyboardAccelerators(),
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
		private bool _isProcessingEnterLeave;

		// NOTE: This should actually be on DependencyObject, not UIElement.
		// We'll be able to do it once DependencyObject is a class instead of an interface.
		internal void Enter(EnterParams @params, int depth)
		{
			// If IsProcessingEnterLeave is true, then this element is already part of the
			// Enter/Leave walk. This can happen, for instance, if a custom DP's value has
			// been set to some ancestor of this node.
			if (_isProcessingEnterLeave)
			{
				return;
			}
			else
			{
				_isProcessingEnterLeave = true;
			}

			try
			{
				// UNO TODO: We naively call EnterImpl right away.
				EnterImpl(@params, depth);

				//if (this is XamlIslandRoot xamlIsland)
				//{
				//	// The CXamlIslandRoot can enter the tree in a few different ways, and we need to make sure
				//	// that however it enters, we override the params.visualTree with the one from the CXamlIslandRoot.
				//	// CXamlIslandRoot always defines its own visual tree, so we must set it here.  Note that after
				//	// the tree refactoring, this won't be necessary because the XamlIslandRoot won't be in the tree
				//	// anymore.
				//	@params.VisualTree = xamlIsland.GetVisualTreeNoRef();
				//}

				//if (@params.IsLive && @params.VisualTree is not null)
				//{
				//	// As the DO enters the live tree, we call SetVisualTree to remember which one it's associated with
				//	SetVisualTree(@params.VisualTree);
				//}

				//DependencyObject pAdjustedNamescopeOwner = pNamescopeOwner;

				//When we copy the EnterParams, reset the pointer to the resource dictionary
				//parent so that descendants don't think they are the direct child of one.
				//EnterParams enterParams = @params;
				//enterParams.ParentResourceDictionary = null;

				//if (m_pInheritedProperties is not null)
				//{
				//	if (m_pInheritedProperties.m_pWriter == this)
				//	{
				//		// This DO owns this m_pInheritedProperties. Mark it as out of date
				//		// so that subsequent property accesses get updated property values.
				//		m_pInheritedProperties.m_cGenerationCounter = 0;
				//	}
				//	else
				//	{
				//		// This DO's inherited properties are a read only reference to some
				//		// parent, that reference is no longer valid, and we need to release
				//		// it.
				//		DisconnectInheritedProperties();
				//	}
				//}

				//if (this.IsStandardNameScopeOwner())
				//{
				//	pAdjustedNamescopeOwner = this;

				//	// If we are entering some other scope
				//	if (pAdjustedNamescopeOwner != pNamescopeOwner)
				//	{
				//		// If this is a permanent namescopeOwner, but its names are not registered
				//		// in its own namescope, then the optimization further down about skipping name registration
				//		// wouldn't apply.
				//		if (!@params.SkipNameRegistration &&
				//			this.ShouldRegisterInParentNamescope() &&
				//			pNamescopeOwner != this &&
				//			!IsTemplateNamescopeMember())
				//		{
				//			// not using the "adjusted" namescope owner
				//			RegisterName(pNamescopeOwner);
				//		}

				//		// regarding condition below: The only element that is a Permanent
				//		// Namescope owner, but not a Namescope member is the Root Visual;
				//		// and it isn't necessary to try to defer name registration for the root visual.
				//		if (this.IsStandardNameScopeMember() && this.GetContext().HasRegisteredNames(this))
				//		{
				//			//ASSERT(this.IsStandardNameScopeOwner());

				//			if (@params.IsLive)
				//			{
				//				if (!IsActiveInVisualTree)
				//				{
				//					TryReCreateUIAWrapper();
				//				}

				//				// pass TRUE for bSkipRegistration:  The names have already been
				//				// registered, and being a Permanent Namescope Owner, we aren't
				//				// expected to have to merge Namescopes with a parent Namescope.
				//				enterParams.SkipNameRegistration = true;
				//				this.EnterImpl(pAdjustedNamescopeOwner, enterParams);
				//				return;
				//			}
				//			else
				//			{
				//				// Skipping the non-live Enter walk here, as it should only be propagating
				//				// name information, which we already have. This is kind of an odd part of
				//				// the non-live enter. You can never rely on anything except your direct parent
				//				// remaining unchanged because this optimization will terminate non-live Enters
				//				// at NameScope boundaries when manipulating XAML fragments.
				//				return;
				//			}
				//		}
				//	}
				//}

				//if (@params.SkipNameRegistration && GetParentInternal() is PopupRoot)
				//{
				//	// Popup's child receives Enter from two different namescopes - namescope of its logical
				//	// parent and that of its visual parent. This Enter is from the visual parent of popup's child.
				//	// It should have a valid namescope owner by now since name registration is done
				//	// before the popup is opened. Use the namescope in which name registration is done
				//	// to ensure the correct IsNamescopeMember flag.
				//	pAdjustedNamescopeOwner = GetStandardNameScopeOwner();
				//	ASSERT(pAdjustedNamescopeOwner);

				//	if (this.GetLogicalParentNoRef() is Popup popup)
				//	{
				//		// See comment in CDependencyObject::Leave
				//		popup.SetCachedStandardNamescopeOwner(pAdjustedNamescopeOwner);
				//	}
				//}

				//// [Blue Compat]: When parsing App.xaml, skip entering any tree children of the Application object.
				//bool isNamescopeOwnerApplicationDuringParse = pNamescopeOwner is not null && pNamescopeOwner.ParserOwnsParent() && pNamescopeOwner is Application;

				//// MultiParentShareableDependencyObjects may not have a namescope owner (e.g. when it has multiple parents). But we
				//// still need to make sure we do a live enter, so that types such as BitmapImage can still do work upon entering/
				//// leaving the tree.
				//bool liveEnterOnMultiParentShareableDO = @params.IsLive && DoesAllowMultipleParents();

				//if ((pAdjustedNamescopeOwner is not null || liveEnterOnMultiParentShareableDO)
				//	&& !isNamescopeOwnerApplicationDuringParse)
				//{
				//	if (@params.IsLive && !IsActiveInVisualTree)
				//	{
				//		TryReCreateUIAWrapper();
				//	}

				//	if (pAdjustedNamescopeOwner is not null)
				//	{
				//		SetIsStandardNameScopeMember(pAdjustedNamescopeOwner.IsStandardNameScopeMember());
				//	}
				//	EnterImpl(pAdjustedNamescopeOwner, @params);
				//}
				//else if (@params.IsForKeyboardAccelerator)
				//{
				//	// This is dead enter to register any keyboard accelerators collection to the list of live accelerators
				//	@params.IsLive = false;
				//	@params.SkipNameRegistration = true;
				//	@params.UseLayoutRounding = false;
				//	@params.CoercedIsEnabled = false;
				//	EnterImpl(pAdjustedNamescopeOwner, @params);
				//}
			}
			finally
			{
				_isProcessingEnterLeave = false;
			}
		}

		// This method should be on DependencyObject instead of UIElement.
		// We can only do that once DependencyObject becomes a class instead of interface.
		private protected virtual void EnterImpl(
			bool live
			//bool skipNameRegistration,
			//bool coercedIsEnabled,
			//bool useLayoutRounding
			)
		{

		}

		private void DependencyObject_EnterImpl(EnterParams @params)
		{
			if (@params.IsLive)
			{
				if (!IsActiveInVisualTree)
				{
					IsActiveInVisualTree = true;
				}

				//m_checkForResourceOverrides = @params.fCheckForResourceOverrides;
			}

			//if (!@params.SkipNameRegistration)
			//{
			//	if (!IsTemplateNamescopeMember())
			//	{
			//		RegisterName(pNamescopeOwner);
			//		RegisterDeferredStandardNameScopeEntries(pNamescopeOwner);
			//	}
			//}

			//if (HasDeferred())
			//{
			//	CDeferredMapping.NotifyEnter(
			//		pNamescopeOwner,
			//		this,
			//		@params.fSkipNameRegistration);
			//}

			// Nothing else to do for value types and control/data templates.
			//var pClassInfo = this.GetType();

			//if (pClassInfo.IsValueType
			//	|| pClassInfo == typeof(ControlTemplate)
			//	|| pClassInfo == typeof(DataTemplate))
			//{
			//	return ;
			//}

			// Enumerate all the field-backed properties and enter/invoke as needed.
			//EnterDependencyProperty pNullEnterProperty = MetadataAPI.GetNullEnterProperty();
			//for (EnterDependencyProperty pEnterProperty = pClassInfo.GetFirstEnterProperty(); pEnterProperty != pNullEnterProperty; pEnterProperty = pEnterProperty.GetNextProperty())
			//{
			//	if (!pEnterProperty.DoNotEnterLeave())
			//	{
			//		if (pEnterProperty->IsObjectProperty())
			//		{
			//			DependencyObject pDO = MapPropertyAndGroupOffsetToDO(pEnterProperty->m_nOffset, pEnterProperty->m_nGroupOffset);
			//			if (pDO != null)
			//			{
			//				EnterObjectProperty(pDO, pNamescopeOwner, @params);
			//			}
			//		}
			//	}
			//	if (pEnterProperty.NeedsInvoke())
			//	{
			//		Invoke(MetadataAPI.GetDependencyPropertyByIndex(pEnterProperty->m_nPropertyIndex), pNamescopeOwner, @params.IsLive);
			//	}
			//}

			//EnterSparseProperties(pNamescopeOwner, @params);

			// ----------------------- UNO-specific END -----------------------
			// The way this works on WinUI is that when an element enters the visual tree, all values
			// of properties that are marked with MetaDataPropertyInfoFlags::IsSparse and MetaDataPropertyInfoFlags::IsVisualTreeProperty
			// are entered as well.
			// The property we currently know it has an effect is Resources
			// In WinUI, it happens in CDependencyObject::EnterImpl (the call to EnterSparseProperties)
			if (this is FrameworkElement fe && fe.TryGetResources() is { } resources)
			{
				// Using ValuesInternal to avoid Enumerator boxing
				foreach (var resource in resources.ValuesInternal)
				{
					if (resource is FrameworkElement resourceAsUIElement)
					{
						resourceAsUIElement.XamlRoot = XamlRoot;
						resourceAsUIElement.EnterImpl(@params, int.MinValue);
					}
				}
			}
			// ----------------------- UNO-specific END -----------------------


			//if (@params.IsLive && m_bitFields.fWantsInheritanceContextChanged)
			//{
			//	// We only raise this InheritanceContextChanged if we're entering the live tree because the
			//	// event also acts like a DO.Loaded event for BindingExpression.  This keeps us from adding
			//	// a new internal event
			//	NotifyInheritanceContextChanged();
			//}

			//if (IsActiveInVisualTree)
			//{
			//	// If our theme is different from the parent, make sure we walk the subtree.
			//	DependencyObject? pParent = null;

			//	if (this is FrameworkElement thisAsFe)
			//	{
			//		// Get logical parent so popups and flyouts inherit theme changes
			//		pParent = GetInheritanceParentInternal(true /* fLogicalParent */);
			//	}
			//	else
			//	{
			//		pParent = GetParentInternal(false /* public */);
			//	}

			//	if (pParent is not null && pParent.GetTheme() != Theme.None && pParent.GetTheme() != m_theme)
			//	{
			//		NotifyThemeChanged(pParent.GetTheme());
			//	}
			//	else
			//	{
			//		// Update theme references to account for new ancestor theme dictionaries.
			//		UpdateAllThemeReferences();
			//	}
			//}
		}

		internal virtual void EnterImpl(EnterParams @params, int depth)
		{
			Depth = depth;

			var core = this.GetContext();
			//bool isParentEnabled = @params.CoercedIsEnabled;

			//// Explicitly check the shadow for ancestor elements set as Receivers. We can't rely on CThemeShadow::EnterImpl
			//// here, because ThemeShadows can be shared, and the shadow could already be in the tree elsewhere.
			//if (@params.IsLive && !ThemeShadow.IsDropShadowMode)
			//{
			//	if (this.Shadow is ThemeShadow themeShadow)
			//	{
			//		themeShadow.CheckForAncestorReceivers(this /* newParent */);
			//		//Task: 15141734
			//		//If ThemeShadow is set, we force the canvas root to have compNode
			//		EnsureRootCanvasCompNode();
			//	}
			//}

			// Uno docs: NOTE IMPORTANT -> GetIsEnabled() in WinUI is different from IsEnabled()
			//// If parent is enabled, but local value of IsEnabled is FALSE, need to disable children.
			//if (isParentEnabled && !GetIsEnabled())
			//{
			//	@params.CoercedIsEnabled = false;
			//}

			//if (@params.IsLive)
			//{
			//	// If parent is disabled, but local value is enabled, then coerce to FALSE.
			//	if (!isParentEnabled && GetIsEnabled())
			//	{
			//		// Coerce value and raise changed event.
			//		CoerceIsEnabled(false, /*bCoerceChildren*/false);
			//	}

			//	// Inherit the UseLayoutRounding property
			//	if (!IsPropertyDefaultByIndex(UIElement.UseLayoutRounding))
			//	{
			//		// Pass on the non-default value
			//		@params.UseLayoutRounding = GetUseLayoutRounding();
			//	}
			//	else
			//	{
			//		// Inherit the new value
			//		SetUseLayoutRounding(@params.UseLayoutRounding);
			//	}

			//	// layout storage has been created by the parent collection (OnAddToCollection) or by the setting
			//	// of one of the transitions.
			//	// when transitioning into a live tree, the absolute offset can be transformed into a relative value
			//	// that is used by layout transitions.
			//	if (!IsActiveInVisualTree)
			//	{
			//		if (HasLayoutTransitionStorage())
			//		{
			//			LayoutTransitionStorage pStorage = GetLayoutTransitionStorage();

			//			Rect offset = new Rect(0, 0, 0, 0);
			//			Rect topLeft = new Rect(0, 0, 0, 0);
			//			if (pStorage.m_nextGenerationCounter > 0)  // used to verify storage should have meaningful values
			//			{
			//				CLayoutManager pLayoutManager = VisualTree.GetLayoutManagerForElement(this);
			//				// after the base implementation of enter, the element will be active (and thus no longer relative)

			//				ASSERT(GetParentInternal(false));

			//				// since we have just been reparented, the offset is currently to the plugin
			//				xref_ptr<ITransformer> pTransformer;
			//				((UIElement)GetParentInternal(false)).TransformToRoot(out pTransformer);
			//				Transformer.TransformBounds(pTransformer, ref topLeft, ref offset);

			//				// current offset will be either the last actual layout pass or the bcb information
			//				pStorage.m_currentOffset.x -= offset.X;
			//				pStorage.m_currentOffset.y -= offset.Y;

			//				// copy these offsets to next generation as well
			//				pStorage.m_nextGenerationOffset = pStorage.m_currentOffset;
			//				if (pLayoutManager is not null)
			//				{
			//					pStorage.m_nextGenerationCounter = pLayoutManager.GetNextLayoutCounter();
			//				}
			//			}
			//		}

			//		m_enteredTreeCounter = EnteredInThisTick;   // the enter counter will be set on the next layout cycle
			//	}
			//}

			// Pass updated params to children.
			DependencyObject_EnterImpl(@params);

			//// Extends EnterImpl to the ContextFlyout
			//FlyoutBase pFlyoutBase = this.ContextFlyout;
			//if (pFlyoutBase is not null)
			//{
			//	// This FlyoutBase can be shared between ContentRoots -- remove the VisualTree
			//	// pointer here for this enter.  TODO: figure out why this happens
			//	// Bug 19548424: Investigate places where an element entering the tree doesn't have a unique VisualTree ptr
			//	EnterParams newParams = @params;
			//	newParams.VisualTree = null;
			//	pFlyoutBase.Enter(pNamescopeOwner, newParams/*EnterParams*/);
			//}

			//// Work on the children
			//if (m_pChildren is not null)
			//{
			//	m_pChildren.Enter(pNamescopeOwner, @params));
			//}

			// UNO specific: We don't have UIElementCollection field, so we do it this way
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

				this.ChildEnter(child, @params);
			}

			//{
			//	DependencyObject annotations = GetAutomationAnnotationsStorage();

			//	if (annotations is not null)
			//	{
			//		annotations.Enter(pNamescopeOwner, @params);
			//	}
			//}

			//If this object has a managed peer, it needs to process Enter as well.
			//if (HasManagedPeer())
			{
				this.EnterImpl(@params.IsLive
					//@params.SkipNameRegistration,
					//@params.CoercedIsEnabled,
					//@params.UseLayoutRounding
					);
			}

			//var contentRoot = VisualTree.GetContentRootForElement(this);

			if (@params.IsLive)
			{
				// If there are events registered on this element, ask the
				// EventManager to extract them and a request for every event.
				//if (m_pEventList)
				{
					// Get the event manager.
					EventManager pEventManager = core.EventManager;
					pEventManager.AddRequestsInOrder(this);
				}

				// Make sure that we propagate OnDirtyPath bits to the new parent.
				//if ((GetIsLayoutElement() || GetIsParentLayoutElement()))
				{
					SetLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.ArrangeDirty);
				}
				//else
				//{
				//	if (GetIsMeasureDirty() == FALSE)
				//	{
				//		SetLayoutFlags(LF_MEASURE_DIRTY_PENDING, TRUE);
				//	}
				//	if (GetIsArrangeDirty() == FALSE)
				//	{
				//		SetLayoutFlags(LF_ARRANGE_DIRTY_PENDING, TRUE);
				//	}
				//}

				//bool propMeasure = GetRequiresMeasure();
				//bool propArrange = GetRequiresArrange();

				//bool propViewport = GetIsViewportDirtyOrOnViewportDirtyPath();
				//bool propContributesToViewport = GetWantsViewportOrContributesToViewport();

				//if (CanBeScrollAnchor)
				//{
				//	UpdateAnchorCandidateOnParentScrollProvider(true /* add */);
				//}

				//if (EventEnabledElementAddedInfo())
				//{
				//	var pParent = this.GetUIElementParentInternal();
				//	TraceElementAddedInfo(reinterpret_cast<XUINT64>(this), reinterpret_cast<XUINT64>(pParent));
				//}

				//if (propMeasure)
				//{
				//	PropagateOnMeasureDirtyPath();

				//	// We need to invalidate the parent's measure so that it will re-measure its children
				//	CUIElement* pParent = GetUIElementParentInternal();
				//	if (pParent != nullptr)
				//	{
				//		pParent->InvalidateMeasure();
				//	}
				//}

				//if (propArrange)
				//{
				//	PropagateOnArrangeDirtyPath();
				//}

				//if (propViewport)
				//{
				//	PropagateOnViewportDirtyPath();
				//}

				//if (propContributesToViewport)
				//{
				//	PropagateOnContributesToViewport();
				//}

				//InvalidateAutomationPeerDataInternal();

				//var akExport = contentRoot.GetAKExport();

				//if (akExport.IsActive())
				//{
				//	akExport.AddElementToAKMode(this);
				//}

				ResetLayoutInformation();

				// Let this DirectManipulation container know that it is now live in the tree.
				//if (m_fIsDirectManipulationContainer)
				//{
				//	ctl::ComPtr<CUIDMContainer> dmContainer;
				//	IFC_RETURN(GetDirectManipulationContainer(&dmContainer));
				//	if (dmContainer != nullptr)
				//	{
				//		CInputServices* inputServicesNoRef = GetContext()->GetInputServices();
				//		if (inputServicesNoRef)
				//		{
				//			inputServicesNoRef->EnsureHwndForDManipService(this, GetElementInputWindow());
				//		}

				//		IFC_RETURN(dmContainer->NotifyManipulatabilityAffectingPropertyChanged(TRUE /*fIsInLiveTree*/));
				//	}
				//}
			}
		}

		// NOTE: This should actually be on DependencyObject, not UIElement.
		// We'll be able to do it once DependencyObject is a class instead of an interface.
		//
		// Causes the object and its properties to leave scope. If bLive,
		// then the object is leaving the "Live" tree, and the object can no
		// longer respond to OM requests related to being Live.   Actions
		// like downloads and animation will be halted.
		private void Leave(LeaveParams @params)
		{
			// If IsProcessingEnterLeave is true, then this element is already part of the
			// Enter/Leave walk.  This can happen, for instance, if a custom DP's value has
			// been set to some ancestor of this node.
			if (_isProcessingEnterLeave)
			{
				return;
			}
			else
			{
				_isProcessingEnterLeave = true;
			}

			try
			{
				// UNO TODO: We naively call LeaveImpl right away.
				LeaveImpl(@params);

				//DependencyObject pAdjustedNamescopeOwner = pNamescopeOwner;

				//// When we copy the LeaveParams, reset the pointer to the resource dictionary
				//// parent so that descendants don't think they are the direct child of one.
				//LeaveParams leaveParams = params;
				//leaveParams.pParentResourceDictionary = null;

				//// It only makes sense to leave a live tree if you are currently live.
				//// If this happens, we are likely recovering from a situation where this
				//// tree has only partially entered the live tree.
				//bool bAdjustedLive = @params.IsLive && IsActiveInVisualTree;

				//if (m_pInheritedProperties != null)
				//{
				//	if (m_pInheritedProperties.m_pWriter == this)
				//	{
				//		// This DO owns this m_pInheritedProperties. Mark it as out of date
				//		// so that subsequent property accesses get updated property values.
				//		m_pInheritedProperties.m_cGenerationCounter = 0;
				//	}
				//	else
				//	{
				//		// This DO's inherited properties are a read only reference to some
				//		// parent, that reference is no longer valid, and we need to release
				//		// it.
				//		DisconnectInheritedProperties();
				//	}
				//}

				//if (this.IsStandardNameScopeOwner())
				//{
				//	pAdjustedNamescopeOwner = this;

				//	// If this is a permanent namescopeOwner, but its names are not registered
				//	// in its own namescope, then the optimization further down about skipping name registration
				//	// wouldn't apply.
				//	if (!@params.SkipNameRegistration && this.ShouldRegisterInParentNamescope() && pNamescopeOwner != this &&
				//		!IsTemplateNamescopeMember())
				//	{
				//		// not using the "adjusted" namescope owner
				//		UnregisterName(pNamescopeOwner);
				//	}

				//	if (HasDeferred())
				//	{
				//		// Either of code-paths below will leave the method, so NotifyLeave should not be called twice
				//		DeferredMapping.NotifyLeave(
				//			pNamescopeOwner,
				//			this,
				//			@params.fSkipNameRegistration);
				//	}

				//	if (!bAdjustedLive && !this.GetContext().HasRegisteredNames(this))
				//	{
				//		// If we are removing a Namescope Owner from a non-live tree,
				//		// and it hasn't had gone through registration of the names, then
				//		// there is nothing to clean up.  Score.
				//		return;
				//	}
				//	else
				//	{
				//		// ensure that the fNamescopeMember is set, (as this may not be
				//		// the case if this is the rootVisual leaving the tree)
				//		SetIsStandardNameScopeMember(TRUE);

				//		// If this is a permanent NamescopeOwner, then he will be taking his
				//		// names with him en masse, so pass TRUE for bSkipRegistration
				//		leaveParams.IsLive = bAdjustedLive;
				//		leaveParams.SkipNameRegistration = TRUE;

				//		LeaveImpl(pAdjustedNamescopeOwner, leaveParams);
				//		return;
				//	}
				//}

				//if (@params.SkipNameRegistration
				//	&& null != GetParentInternal() as PopupRoot)
				//{
				//	// Popup's child receives Leave from two different namescopes - namescope of its logical
				//	// parent and that of its visual parent. This Leave is from the visual parent of popup's child.
				//	// Use the namescope in which name registration is done to ensure the correct
				//	// IsNamescopeMember flag.
				//	pAdjustedNamescopeOwner = GetStandardNameScopeOwner();

				//	if (pAdjustedNamescopeOwner == null && IsActiveInVisualTree)
				//	{
				//		if (this.GetLogicalParentNoRef() is Popup popup)
				//		{
				//			// This is a temporary fix for the case where a Popup's child is leaving the tree, but we can't find
				//			// the namescope owner (because the owner isn't live anymore, so GetStandardNameScopeOwnerInternal
				//			// skips it).  We hit this case in the CommandBarFlyoutCommandBar, which contains a parented popup
				//			// called "OverflowPopup".  When the flyout closes, the children begin the process of leaving the
				//			// tree.  As part of this process, the OverflowPopup is closed, and as its children process Leave,
				//			// pAdjustedNamescopeOwner comes back as nullptr here because the namescope owner has already left
				//			// the "live" state.  We've made the change this way to minimize risk for a WinAppSDK 1.0 update.
				//			// An easy way to repro this case is to right-click a TextBox in two different XAML Windows on
				//			// the same thread.
				//			// Find a better, more comprehensive fix with http://osgvsowi/19548424
				//			pAdjustedNamescopeOwner = popup.GetCachedStandardNamescopeOwnerNoRef();
				//			popup.SetCachedStandardNamescopeOwner(null);
				//		}
				//	}
				//}

				//// MultiParentShareableDependencyObjects may not have a namescope owner (e.g. when it has multiple parents). But we
				//// still need to make sure we do a live enter, so that types such as BitmapImage can still do work upon entering/
				//// leaving the tree.
				//bool liveLeaveOnMultiParentShareableDO = bAdjustedLive && DoesAllowMultipleParents();
				//if (pAdjustedNamescopeOwner is not null || liveLeaveOnMultiParentShareableDO)
				//{
				//	if (pAdjustedNamescopeOwner is not null)
				//	{
				//		SetIsStandardNameScopeMember(pAdjustedNamescopeOwner.IsStandardNameScopeMember());
				//	}
				//	leaveParams.IsLive = bAdjustedLive;
				//	leaveParams.SkipNameRegistration = @params.fSkipNameRegistration;
				//	LeaveImpl(pAdjustedNamescopeOwner, leaveParams);
				//}
				//else if (leaveParams.IsForKeyboardAccelerator)
				//{
				//	// This is dead leave to register any keyboard accelerators collection to the list of live accelerators
				//	leaveParams.IsLive = false;
				//	leaveParams.SkipNameRegistration = true;
				//	leaveParams.UseLayoutRounding = false;
				//	leaveParams.CoercedIsEnabled = false;
				//	LeaveImpl(pAdjustedNamescopeOwner, leaveParams);
				//}

				//if (HasDeferred())
				//{
				//	DeferredMapping.NotifyLeave(
				//		pNamescopeOwner,
				//		this,
				//		@params.SkipNameRegistration));
				//}
			}
			finally
			{
				_isProcessingEnterLeave = false;
			}
		}


		// This method should be on DependencyObject instead of UIElement.
		// We can only do that once DependencyObject becomes a class instead of interface.
		private protected virtual void LeaveImpl(
			bool live
			//bool skipNameRegistration,
			//bool coercedIsEnabled,
			//bool visualTreeBeingReset
			)
		{

		}

		// Causes the object and its properties to leave scope. If bLive,
		// then the object is leaving the "Live" tree, and the object can no
		// longer respond to OM requests related to being Live.   Actions
		// like downloads and animation will be halted.
		//
		// Derived classes are expected to first call <base>::LeaveImpl, and
		// then call Leave on any "children".
		//
		// Objects are expected to cleanup all the unshared device resources on their leave from live tree
		// i.e., when params.fIsLive = TRUE. This include primitives,
		// composition nodes, visuals, shape realizations and cache realizations for UIElements.
		// And it includes textures etc, for other objects like brushes and image sources.
		// The recursive leave call for children elements and children properties
		// would do similar cleanup on their final leave. This enables appropriate sharing.
		// Hence an element should not cleanup resources for its
		// child/property in its leave.
		private void DependencyObject_LeaveImpl(LeaveParams @params)
		{
			// Raise InheritanceContextChanged for the live leave.  We need to do this before m_bitFields.fLive is updated.
			// params.fIsLive cannot be used because it is updated before we get here.
			//if (IsActiveInVisualTree && m_bitFields.fWantsInheritanceContextChanged)
			//{
			//	NotifyInheritanceContextChanged();
			//}

			// Mark the object as out of tree if the intention of this walk is to notify
			// the element that it is leaving the live tree (as indicated by the bLive parameter.)
			if (@params.IsLive)
			{
				//	m_checkForResourceOverrides = @params.CheckForResourceOverrides;
				Depth = int.MinValue;
				IsActiveInVisualTree = false;
			}

			//// Enumerate all the properties in its class

			//if (!@params.SkipNameRegistration &&
			//	!IsTemplateNamescopeMember())
			//{
			//	UnregisterName(pNamescopeOwner);
			//	UnregisterDeferredStandardNameScopeEntries(pNamescopeOwner);
			//}

			//var pClassInfo = this.GetType();

			//// Nothing else to do for value types and control/data templates.
			//if (pClassInfo.IsValueType
			//	|| pClassInfo == typeof(ControlTemplate)
			//	|| pClassInfo == typeof(DataTemplate))
			//{
			//	return;
			//}

			//// Enumerate all the field-backed properties and leave as needed.
			//EnterDependencyProperty pNullEnterProperty = MetadataAPI.GetNullEnterProperty();
			//for (EnterDependencyProperty pEnterProperty = pClassInfo.GetFirstEnterProperty(); pEnterProperty != pNullEnterProperty; pEnterProperty = pEnterProperty.GetNextProperty())
			//{
			//	if (pEnterProperty.DoNotEnterLeave())
			//	{
			//		continue;
			//	}

			//	if (pEnterProperty.IsObjectProperty())
			//	{
			//		DependencyObject pDO = MapPropertyAndGroupOffsetToDO(pEnterProperty.m_nOffset, pEnterProperty.m_nGroupOffset);
			//		if (pDO != null)
			//		{
			//			LeaveObjectProperty(pDO, pNamescopeOwner, @params);
			//		}
			//	}
			//}

			//LeaveSparseProperties(pNamescopeOwner, @params);
			//if (@params.IsLive)
			//{
			//	// If we're currently the focused element, remove ourselves from being focused
			//	var contentRoot = VisualTree.GetContentRootForElement(this, VisualTree.LookupOptions.NoFallback);
			//	if (contentRoot != null)
			//	{
			//		FocusManager pFocusManager = contentRoot.GetFocusManagerNoRef();

			//		if (pFocusManager is not null && pFocusManager.GetFocusedElementNoRef() == this)
			//		{
			//			pFocusManager.ClearFocus();
			//		}

			//		var akExport = contentRoot.GetAKExport();

			//		if (akExport.IsActive())
			//		{
			//			akExport.RemoveElementFromAKMode(this);
			//		}
			//	}
			//}

			//InputServices inputServices = this.GetContext().GetInputServices();

			//if (inputServices != null)
			//{
			//	inputServices->ObjectLeavingTree(this);
			//}
		}

		internal virtual void LeaveImpl(LeaveParams @params)
		{
			// --------- UNO Specific BEGIN ---------
			// This should be done in FrameworkElement's override of LeaveImpl.
			// But:
			// 1. Currently we manage Loaded/Unloaded in UIElement
			// 2. OnElementUnloaded is important to be called on UIElement because Wasm hit testing implementation relies on that (for TextElements specifically)
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
			// --------- UNO Specific END ---------

			//var core = this.GetContext();
			//var isParentEnabled = @params.CoercedIsEnabled;
			//bool alreadyCanceledTransitions = false;

			// Uno docs: NOTE IMPORTANT -> GetIsEnabled() in WinUI is different from IsEnabled()
			//// If parent is enabled, but local value of IsEnabled is FALSE, need to disable children.
			//if (isParentEnabled && !GetIsEnabled())
			//{
			//	@params.CoercedIsEnabled = false;
			//}

			//ASSERT(IsTabNavigationWithVirtualizedItemsSupported() || !m_skipFocusSubtree_OffScreenPosition);

			//// Clear the skip focus subtree flags.
			//m_skipFocusSubtree_OffScreenPosition = false;
			//m_skipFocusSubtree_Other = false;

			//// When visual tree is being reset, (CCoreServices::ResetVisualTree) no need to coerce values/raise events.
			//if (@params.IsLive && !@params.VisualTreeBeingReset)
			//{
			//	// If parent is enabled and local value is enabled and coerced value is disabled, then coerce to TRUE.
			//	if (isParentEnabled && GetIsEnabled() && !IsEnabled())
			//	{
			//		// Coerce value and raise changed event.
			//		CoerceIsEnabled(true, /*bCoerceChildren*/false);
			//	}

			//	// Revert the UseLayoutRounding property
			//	if (!IsPropertyDefaultByIndex(UIElement.UseLayoutRounding))
			//	{
			//		// Pass on the non-default value
			//		@params.UseLayoutRounding = GetUseLayoutRounding();
			//	}
			//	else
			//	{
			//		// Inherit the new value
			//		SetUseLayoutRounding(@params.fUseLayoutRounding);
			//	}
			//}

			//// If we have absolutely positioned renderers, we always need
			//// to cancel transitions regardless of fIsLive. This covers scenarios
			//// like removing the dragged item from a list while dragging, which
			//// only results in a non-live leave walk (because of how DOCollection
			//// handles removes).
			//if (HasAbsolutelyPositionedLayoutTransitionRenderers())
			//{
			//	if (HasLayoutTransitionStorage())
			//	{
			//		GetLayoutTransitionStorage().UnregisterElementForTransitions(this);     // kills unrealized transitions in the layout manager, very unlikely
			//	}

			//	CTransition.CancelTransitions(this);  // kills currently running animations, quite likely.
			//	alreadyCanceledTransitions = true;
			//}

			//// calculate offsets when transitioning from a live element in the visual tree to a non live element
			//if (@params.fIsLive)
			//{
			//	CLayoutManager pLayoutManager = VisualTree.GetLayoutManagerForElement(this);

			//	// transform the offset to be absolute against the plugin
			//	if (HasLayoutTransitionStorage())
			//	{
			//		Rect offset = new Rect(0, 0, 0, 0);
			//		Rect topLeft = new Rect(0, 0, 0, 0);
			//		LayoutTransitionStorage pStorage = GetLayoutTransitionStorage();
			//		UIElement pParent = GetParentInternal(false);

			//		ASSERT(pParent);    // difficult assert, based on the logic inside Collection::Remove
			//							// but it can be trusted to never change.

			//		// no matter what, if we are leaving the live tree and we have a transition active, we should stop it
			//		// * this is an important step in how unloading nesting works. When a sub-graph is leaving the visual tree,
			//		//   individual nodes that are unloading are not stopped by the cancellation of unloads happening
			//		//   higher in the tree (there are no dependencies).
			//		// * another important reason to call cancel here is that the cancel will update the current offset with data
			//		//   from the LTE.

			//		if (!alreadyCanceledTransitions)
			//		{
			//			pStorage.UnregisterElementForTransitions(this);     // kills unrealized transitions in the layout manager, very unlikely
			//			Transition.CancelTransitions(this);  // kills currently running animations, quite likely.
			//		}

			//		xref_ptr<ITransformer> pTransformer;
			//		pParent.TransformToRoot(out pTransformer);
			//		Transformer.TransformBounds(pTransformer, ref topLeft, ref offset);

			//		// preparation:
			//		// if we are in a new layout cycle, next generation information is actually correct
			//		if (pStorage is not null && pLayoutManager is not null && pLayoutManager.GetLayoutCounter() >= pStorage.m_nextGenerationCounter)
			//		{
			//			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset;
			//			pStorage.m_currentSize = pStorage.m_nextGenerationSize;
			//		}

			//		// current offset was relative to parent
			//		pStorage.m_currentOffset.x += offset.X;
			//		pStorage.m_currentOffset.y += offset.Y;

			//		// update opacity
			//		{
			//			// if there was an active transition, it will always have better information
			//			// by way of the opacity on the LayoutTransitionElement.
			//			// Unfortunately, the cancelling of a transition and the call to leave occurs
			//			// at different points in time, so we use a value to indicate whether the
			//			// leaveImpl should bother with caching the opacity.
			//			// It will do so, if there was not active transition during unload.

			//			// The flag will normally be true, except if an element is being removed that had
			//			// an active transition.
			//			if (pStorage.m_opacityCache == LeaveShouldDetermineOpacity)
			//			{
			//				pStorage.m_opacityCache = GetOpacityToRoot();
			//			}
			//		}

			//	}

			//	if (pLayoutManager is not null) // can be null when plugin is tearing down.
			//	{
			//		// indicates when this leave occurred (is used as a signal to know if a reparent is occurring)

			//		// we cannot use the LeftInThisTick constant, since that would be sure to trigger a reparent
			//		// when the element is being entered again. Instead, we need to set the counter such that,
			//		// --- if an enter occurs in the same tick it will be counted as a reparent ---

			//		m_leftTreeCounter = pLayoutManager.GetLayoutCounter();
			//	}
			//}

			//if (@params.fIsLive)
			//{
			//	// Ensure the element's render data is removed from the scene by clearing it.
			//	// We don't need to do this recursively since the Leave() walk is already recursive.
			//	// TODO: INCWALK: Consider just calling the recursive version here anyway? It would be simpler and less error-prone, but perhaps worse for perf.

			//	// Cleanup all the unshared per element device resources. This include primitives,
			//	// composition nodes, visuals, shape realizations and cache realizations.
			//	// The recursive leave call for children would do similar cleanup on the elements of subtree.
			//	if (m_propertyRenderData.IsRenderWalkTypeForComposition())
			//	{
			//		EnsurePropertyRenderData(RWT_None);
			//	}
			//	else if (m_propertyRenderData.type == RWT_NonePreserveDComp)
			//	{
			//		// An item was removed from the tree after a Device Lost, but before the
			//		// the Property Render Data was completely restored.  Clean up the
			//		// composition data.
			//		RemoveCompositionPeer();
			//	}

			//	// If there is a UIA client listening, register this element to have the StructureChanged
			//	// automation event fired for it. We do this here because CUIElement::LeavePCSceneRecursive
			//	// (i.e. the place where we register removed elements) will not be called on this subtree,
			//	// given that it is trying to be efficient and ride on the Leave walk instead of doing
			//	// another one.
			//	if (core->UIAClientsAreListening(UIAXcp::AEStructureChanged) == S_OK)
			//	{
			//		RegisterForStructureChangedEvent(
			//			AutomationEventsHelper::StructureChangedType::Removed);
			//	}

			//	// Special case for LTE embedded in the tree in the HWWalk. The TransitionRoot does not
			//	// enter/leave the tree like regular UIElements do, so we need to ensure we keep it up-to-date here.
			//	CTransitionRoot* localTransitionRootNoRef = GetLocalTransitionRoot(false);
			//	if (localTransitionRootNoRef)
			//	{
			//		localTransitionRootNoRef->LeavePCSceneRecursive();
			//	}
			//}

			DependencyObject_LeaveImpl(@params);

			//// Extends LeaveImpl to the ContextFlyout.
			//FlyoutBase pFlyoutBase = ContextFlyout;
			//if (pFlyoutBase is not null)
			//{
			//	pFlyoutBase.Leave(pNamescopeOwner, @params /*LeaveParams*/);
			//}

			//if (EventEnabledElementRemovedInfo() && @params.fIsLive)
			//{
			//	var pParent = this.GetUIElementParentInternal();
			//	TraceElementRemovedInfo(reinterpret_cast<XUINT64>(this), reinterpret_cast<XUINT64>(pParent));
			//}

			//// Work on the children
			//if (m_pChildren is not null)
			//{
			//	m_pChildren.Leave(pNamescopeOwner, @params));
			//}

			// UNO specific: We don't have UIElementCollection field, so we do it this way
			foreach (var child in _children)
			{
				child.Leave(@params);
			}

			// If this object has a managed peer, it needs to process Leave as well.
			//if (HasManagedPeer())
			{
				this.LeaveImpl(@params.IsLive
					//@params.fSkipNameRegistration,
					//@params.fCoercedIsEnabled,
					//@params.fVisualTreeBeingReset
					);
			}

			if (@params.IsLive)
			{
				//// If we are leaving the Live tree and there are events.
				//// Popup can live outside the live tree. Do not remove event handlers for Popup when it leaves the tree.
				//// Popup's event handlers will be removed when it gets deleted.
				//if (this is not Popup)
				//{
				//	RemoveAllEventListeners(true /* leaveUIEShownHiddenEventListenersAttached */);
				//}

				//// Let this DirectManipulation container know that it no longer lives in the tree
				//if (m_fIsDirectManipulationContainer)
				//{
				//	ctl::ComPtr<CUIDMContainer> dmContainer;
				//	GetDirectManipulationContainer(&dmContainer);
				//	if (dmContainer != null)
				//	{
				//		// This call marks the associated viewport's m_fNeedsUnregistration flag to true so that the viewport
				//		// will be removed from the InputManager's internal viewports xvector, and the viewport will no longer be
				//		// submitted to the compositor.
				//		dmContainer.NotifyManipulatabilityAffectingPropertyChanged(false /*fIsInLiveTree*/);
				//	}
				//}

				//// Discard the potential rejection viewports within this leaving element's subtree
				//DiscardRejectionViewportsInSubTree();

				//if (CanBeScrollAnchor)
				//{
				//	UpdateAnchorCandidateOnParentScrollProvider(false /* add */);
				//}
			}

		}
#endif

		internal virtual bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation horizontal)
			=> true;

		internal bool IsNonClippingSubtree { get; set; }
	}
}
