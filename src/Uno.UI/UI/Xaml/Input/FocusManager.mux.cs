// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// focusmgr.h, focusmgr.cpp

#nullable enable

using System;

using Uno.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Xaml.Rendering;
using Uno.UI.Xaml.Core.Rendering;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI;
using DirectUI;
using Microsoft.UI.Input;

//TODO:MZ: Handle parameters in/out

namespace Microsoft.UI.Xaml.Input
{
	public partial class FocusManager : IFocusManager
	{
		/// <summary>
		/// Represents the content root with which this focus manager is associated.
		/// </summary>
		private readonly ContentRoot _contentRoot;

		/// <summary>
		/// Responsible for drawing the focus rectangles.
		/// </summary>
		private readonly FocusRectManager _focusRectManager = new FocusRectManager();

		/// <summary>
		/// Represents the currently focused element.
		/// </summary>
		private DependencyObject? _focusedElement;

		/// <summary>
		/// Represents the element being focused.
		/// </summary>
		private DependencyObject? _focusingElement;

		/// <summary>
		/// Focused element's AutomationPeer.
		/// </summary>
		private AutomationPeer? _focusedAutomationPeer;

		/// <summary>
		/// Represents the focus target.
		/// </summary>
		private DependencyObject? _focusTarget;

		/// <summary>
		/// Represents the focus visual.
		/// </summary>
		private WeakReference<UIElement>? _focusRectangleUIElement;

		/// <summary>
		/// Indicates whether plugin is focused.
		/// </summary>
		private bool _pluginFocused = true; //TODO Uno: In case of WASM we could check whether the tab is currently visible/focused.

		/// <summary>
		/// Represents the real focus state for the currently focused element.
		/// </summary>
		private FocusState _realFocusStateForFocusedElement;

		/// <summary>
		/// Foccus observer.
		/// </summary>
		private FocusObserver? _focusObserver;

		/// <summary>
		/// Represents the XY focus manager.
		/// </summary>
		private XYFocus _xyFocus = new XYFocus();

		/// <summary>
		/// This is a leftover from Silverlight times :-) .
		/// When the platform cannot support tabbing out of plugin, we will act as if
		/// there is an implicit top-level TabNavigation="Cycle"
		/// So instead of tabbing out, we handle the "last" tab by cycling to the
		/// "first" tab.  This is deemed better than getting stuck at the end.
		/// (Reverse "first" and "last" for Shift-Tab.)
		/// </summary>
		private static readonly bool _canTabOutOfPlugin =
				OperatingSystem.IsBrowser(); // For WASM it is more appropriate to let the user escape from the app to tab into the browser toolbars.

		private bool _isPrevFocusTextControl;

		// TODO Uno: Control engagement is not yet properly supported.
		/// <summary>
		/// Represents the control which is focus-engaged.
		/// </summary>
		private Control? _engagedControl;

		/// <summary>
		/// Represents a value indicating whether focus is currently locked.
		/// </summary>
		private bool _focusLocked;

		/// <summary>
		/// During Window Activation/Deactivation, we lock focus. However, focus can move internally via the focused element becoming
		/// unfocusable (ie. leaving the tree or changing visibility). This member will force focus manager to bypass the _focusLocked logic.
		/// This should be used with care... if used irresponsibly, we can enable unsupported reentrancy scenarios.
		/// </summary>
		private bool _ignoreFocusLock;

		/// <summary>
		/// It is possible to continue with a focus operation, even though focus is locked. In this case, we need to ensure
		/// that we persist the fact that focus should not be canceled.
		/// </summary>
		private bool _currentFocusOperationCancellable = true;

		/// <summary>
		/// Represents a valud indicating whether we are still to provide the initial focus.
		/// </summary>
		private bool _initialFocus;

		/// <summary>
		/// This represents the async operation that can initiated through a public async FocusManager method, such as TryFocusAsync.
		/// We store it as a memeber because the operation can continue to run even after the api has finished executing.
		/// </summary>
		private FocusAsyncOperation? _asyncOperation;

		internal FocusManager(ContentRoot contentRoot)
		{
			_contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
		}

		internal event EventHandler<FocusedElementRemovedEventArgs>? FocusedElementRemoved;

		/// <summary>
		/// Returns the current focused element.
		/// </summary>
		internal DependencyObject? FocusedElement => _focusedElement;

		/// <summary>
		/// Returns the element about to be focused when focus is changing
		/// </summary>
		internal DependencyObject? FocusingElement => _focusingElement;

		/// <summary>
		/// Returns the content root associated with this focus manager instance.
		/// </summary>
		internal ContentRoot ContentRoot => _contentRoot;

		/// <summary>
		/// Focus rect manager.
		/// </summary>
		internal FocusRectManager FocusRectManager => _focusRectManager;

		/// <summary>
		/// Gets a value indicating whether the user can tab out of plugin.
		/// </summary>
		internal bool CanTabOutOfPlugin => _canTabOutOfPlugin;

		/// <summary>
		/// Represents the control which is focus-engaged.
		/// </summary>
		internal Control? EngagedControl
		{
			get => _engagedControl;
			set => _engagedControl = value;
		}

		//TODO Uno: Currently we set this only from Page, but should be set from other places as well.
		/// <summary>
		/// Represents a value indicating whether we are still to provide the initial focus.
		/// </summary>
		internal bool InitialFocus
		{
			get => _initialFocus;
			set => _initialFocus = value;
		}

		internal void SetIgnoreFocusLock(bool ignore) => _ignoreFocusLock = ignore;

		/// <summary>
		/// Focus observer getter, checking for null.
		/// </summary>
		internal FocusObserver FocusObserver => _focusObserver ?? throw new InvalidOperationException("Focus observer was not set.");

		/// <summary>
		/// Sets the focus observer.
		/// </summary>
		/// <param name="focusObserver"></param>
		internal void SetFocusObserver(FocusObserver focusObserver) => _focusObserver = focusObserver;

		/// <summary>
		/// Sets focused element based on FocusMovement, if target is not focusable, tries to find first
		/// focusable in it. Validates whether the navigation direction matches the situation (tabbing).
		/// In the end updates focus with UpdateFocus method.
		/// </summary>
		/// <param name="movement">Focus movement.</param>
		/// <returns>Focus movement result.</returns>
		internal FocusMovementResult SetFocusedElement(FocusMovement movement)
		{
			var focusTarget = movement.Target;

			if (focusTarget == null)
			{
				return new FocusMovementResult();
			}

			if (!IsFocusable(focusTarget))
			{
				focusTarget = GetFirstFocusableElement(focusTarget);

				if (focusTarget == null || !IsFocusable(focusTarget))
				{
					return new FocusMovementResult();
				}
			}

			var navigationDirection = movement.Direction;
			MUX_ASSERT(!movement.IsProcessingTab || (navigationDirection == FocusNavigationDirection.Next || navigationDirection == FocusNavigationDirection.Previous));

			return UpdateFocus(new FocusMovement(focusTarget, movement));
		}

		/// <summary>
		/// Clears the focus.
		/// </summary>
		/// <remarks>
		/// Original code checks for "shutting down", which is not supported in Uno,
		/// so we always go through UpdateFocus.
		/// </remarks>
		internal void ClearFocus() => UpdateFocus(new FocusMovement(null, FocusNavigationDirection.None, FocusState.Unfocused));

		/// <summary>
		/// Releases resources held by FocusManager's FocusRectManager. These
		/// elements are automatically created on CFrameworkManager.UpdateFocus()
		/// and must be released before core releases its main render target on
		/// shutdown.
		/// </summary>
		internal void ReleaseFocusRectManagerResources()
		{
			// Releases references on UIElements held by CFocusRectManager
			_focusRectManager.ReleaseResources(isDeviceLost: false, cleanupDComp: false, clearPCData: true);
		}

		/// <summary>
		/// Cleans up device related resources.
		/// </summary>
		/// <param name="cleanupDComp">Cleanup.</param>
		internal void CleanupDeviceRelatedResources(bool cleanupDComp) =>
			_focusRectManager.ReleaseResources(isDeviceLost: true, cleanupDComp, clearPCData: false);

		/// <summary>
		/// Checks whether the focused element is hidden behind full
		/// window media.
		/// </summary>
		/// <returns>True if hidden, false otherwise.</returns>
		private bool FocusedElementIsBehindFullWindowMediaRoot()
		{
			var visualTree = _contentRoot.VisualTree;
			return visualTree != null && visualTree.IsBehindFullWindowMediaRoot(_focusedElement);
		}

		/// <summary>
		/// Returns first focusable element from root visual based on given direction (first/last).
		/// </summary>
		/// <param name="useReverseDirection">Should be reverse direction?</param>
		/// <returns>Focusable element or null.</returns>
		private DependencyObject? GetFirstFocusableElementFromRoot(bool useReverseDirection)
		{
			DependencyObject? pFocusableElement = null;

			var activeRootVisual = _contentRoot.VisualTree.ActiveRootVisual;
			if (activeRootVisual != null)
			{
				if (!useReverseDirection)
				{
					pFocusableElement = GetFirstFocusableElement(activeRootVisual, pFocusableElement);
				}
				else
				{
					pFocusableElement = GetLastFocusableElement(activeRootVisual, pFocusableElement);
				}
			}

			return pFocusableElement;
		}

		/// <summary>
		/// Returns the first focusable element from the specified visual tree.
		/// </summary>
		/// <param name="searchStartLocation">Location where to start searching.</param>
		/// <param name="focusCandidate">First focused element candidate.</param>
		/// <returns>First focusable element.</returns>
		internal DependencyObject? GetFirstFocusableElement(DependencyObject searchStartLocation, DependencyObject? focusCandidate = null)
		{
			focusCandidate = GetFirstFocusableElementInternal(searchStartLocation, focusCandidate);

			// GetFirstFocusableElementInternal can return the TabStop element that might not a focusable
			// container like as UserControl(IsTabStop=false) even though it have a focusable child.
			// If the pFirstFocus is not focusable and have a focusable child, the below code will find
			// a first focusable child
			// Keep finding the first focusable child

			if (focusCandidate != null && !IsFocusable(focusCandidate) && CanHaveFocusableChildren(focusCandidate))
			{
				focusCandidate = GetFirstFocusableElement(focusCandidate, null);
			}

			return focusCandidate;
		}

		/// <summary>
		/// Returns the first focusable element from the specified visual tree.
		/// </summary>
		/// <param name="searchStartLocation">Location where to start searching.</param>
		/// <param name="firstFocusedElement">First focused element.</param>
		/// <returns>First focusable element or null.</returns>
		private DependencyObject? GetFirstFocusableElementInternal(DependencyObject searchStartLocation, DependencyObject? firstFocusedElement)
		{
			bool useFirstFocusableFromCallback = false;

			// Ask UIElement for a first focusable suggestion(this is to solve ListViewBase focus issues).
			var firstFocusableFromCallback = (searchStartLocation as UIElement)?.GetFirstFocusableElementOverride();
			if (firstFocusableFromCallback != null)
			{
				useFirstFocusableFromCallback = IsFocusable(firstFocusableFromCallback) || CanHaveFocusableChildren(firstFocusableFromCallback);
			}

			if (useFirstFocusableFromCallback)
			{
				if (firstFocusedElement == null ||
					(GetTabIndex(firstFocusableFromCallback) < GetTabIndex(firstFocusedElement)))
				{
					firstFocusedElement = firstFocusableFromCallback;
				}
			}
			else
			{
				var children = FocusProperties.GetFocusChildrenInTabOrder(searchStartLocation);
				foreach (var childNoRef in children)
				{
					if (childNoRef != null && IsVisible(childNoRef))
					{
						bool bHaveFocusableChild = CanHaveFocusableChildren(childNoRef);

						if (IsPotentialTabStop(childNoRef))
						{
							if (firstFocusedElement == null && (IsFocusable(childNoRef) || bHaveFocusableChild))
							{
								firstFocusedElement = childNoRef;
							}

							if (IsFocusable(childNoRef) || bHaveFocusableChild)
							{
								if (GetTabIndex(childNoRef) < GetTabIndex(firstFocusedElement))
								{
									firstFocusedElement = childNoRef;
								}
							}
						}
						else if (bHaveFocusableChild)
						{
							firstFocusedElement = GetFirstFocusableElementInternal(childNoRef, firstFocusedElement);
						}
					}
				}
			}

			return firstFocusedElement;
		}

		/// <summary>
		/// Returns the last focusable element from the specified visual tree.
		/// </summary>
		/// <param name="searchStartLocation">Search start location.</param>
		/// <param name="focusCandidate">Last foucsed element candidate.</param>
		/// <returns></returns>
		internal DependencyObject? GetLastFocusableElement(DependencyObject searchStartLocation, DependencyObject? focusCandidate = null)
		{
			focusCandidate = GetLastFocusableElementInternal(searchStartLocation, focusCandidate);

			// GetLastFocusableElementInternal can return the TabStop element that might not a focusable
			// container like as UserControl(IsTabStop=false) even though it have a focusable child.
			// If the pLastFocus is not focusable and have a focusable child, the below code will find
			// a last focusable child
			if (focusCandidate != null && CanHaveFocusableChildren(focusCandidate))
			{
				focusCandidate = GetLastFocusableElement(focusCandidate, null);
			}

			return focusCandidate;
		}

		/// <summary>
		/// Returns the last focusable element from the specified visual tree.
		/// </summary>
		/// <param name="searchStartLocation">Search start location.</param>
		/// <param name="lastFocusedElement">Last focused elment.</param>
		/// <returns>Last focusable element.</returns>
		private DependencyObject? GetLastFocusableElementInternal(DependencyObject searchStartLocation, DependencyObject? lastFocusedElement)
		{
			bool useLastFocusableFromCallback = false;

			// Ask UIElement for a first focusable suggestion(this is to solve ListViewBase focus issues).
			var lastFocusableFromCallback = (searchStartLocation as UIElement)?.GetLastFocusableElementOverride();
			if (lastFocusableFromCallback != null)
			{
				useLastFocusableFromCallback = IsFocusable(lastFocusableFromCallback) || CanHaveFocusableChildren(lastFocusableFromCallback);
			}

			if (useLastFocusableFromCallback)
			{
				if (lastFocusedElement == null ||
					(GetTabIndex(lastFocusableFromCallback) >= GetTabIndex(lastFocusedElement)))
				{
					lastFocusedElement = lastFocusableFromCallback;
				}
			}
			else
			{

				var children = FocusProperties.GetFocusChildrenInTabOrder(searchStartLocation);
				foreach (var childNoRef in children)
				{
					if (childNoRef != null && IsVisible(childNoRef))
					{
						bool bHaveFocusableChild = CanHaveFocusableChildren(childNoRef);

						if (IsPotentialTabStop(childNoRef))
						{
							if (lastFocusedElement == null && (IsFocusable(childNoRef) || bHaveFocusableChild))
							{
								lastFocusedElement = childNoRef;
							}

							if (IsFocusable(childNoRef) || bHaveFocusableChild)
							{
								if (GetTabIndex(childNoRef) >= GetTabIndex(lastFocusedElement))
								{
									lastFocusedElement = childNoRef;
								}
							}
						}
						else if (bHaveFocusableChild)
						{
							lastFocusedElement = GetLastFocusableElementInternal(childNoRef, lastFocusedElement);
						}
					}
				}
			}

			return lastFocusedElement;
		}

		/// <summary>
		/// Returns true if TabStop can be processed by us.
		/// </summary>
		/// <param name="isReverse">Use reverse direction?</param>
		/// <returns></returns>
		/// <remarks>
		/// Checks whether FocusManager can process tab stop in a given direction.
		/// First checks for popup, then if it is already on a boundary,
		/// then includes special handling for once and cycle.
		/// </remarks>
		private bool CanProcessTabStop(bool isReverse)
		{
			bool canProcessTab = true;
			bool isFocusOnFirst = false;
			bool isFocusOnLast = false;
			DependencyObject? pFocused = _focusedElement;

			if (IsFocusedElementInPopup())
			{
				// focused element is inside a poup. Since we treat tabnavigation
				// in popup as Cycle, we dont need to check if this element is
				// the first or the last tabstop.
				return true;
			}

			if (isReverse)
			{
				// Backward tab processing
				isFocusOnFirst = IsFocusOnFirstTabStop();
			}
			else
			{
				// Forward tab processing
				isFocusOnLast = IsFocusOnLastTabStop();
			}

			if (isFocusOnFirst || isFocusOnLast)
			{
				// Can't process tab from the focus on first or last
				canProcessTab = false;
			}

			if (canProcessTab)
			{
				// Get the first/last focusable control. This is the opposite direction to check up
				// the scope boundary. (e.g. Forward direction need to get the last focusable control)
				DependencyObject? oppositeEdge = GetFirstFocusableElementFromRoot(!isReverse);

				// Need to check the Once navigation mode to be out the plugin control
				// if the current focus is on the edge boundary with Once mode.
				if (oppositeEdge != null)
				{
					var oppositeEdgeParent = GetParentElement(oppositeEdge);
					if (oppositeEdgeParent != null &&
						GetTabNavigation(oppositeEdgeParent) == KeyboardNavigationMode.Once &&
						oppositeEdgeParent == GetParentElement(pFocused))
					{
						// Can't process, so tab will be out of plugin
						canProcessTab = false;
					}
				}
				else
				{
					canProcessTab = false;
				}
			}
			else
			{
				// We can process tab in case of Cycle navigation mode
				// even though the focus is on the edge of plug-in control.
				if (isFocusOnLast || isFocusOnFirst)
				{
					if (GetTabNavigation(pFocused) == KeyboardNavigationMode.Cycle)
					{
						canProcessTab = true;
					}
					else
					{
						UIElement? pFocusedParent = GetParentElement(pFocused);
						while (pFocusedParent != null)
						{
							if (GetTabNavigation(pFocusedParent) == KeyboardNavigationMode.Cycle)
							{
								canProcessTab = true;
								break;
							}
							pFocusedParent = GetParentElement(pFocusedParent);
						}
					}
				}
			}

			return canProcessTab;
		}

		/// <summary>
		/// Returns the candidate next or previous tab stop element.		
		/// </summary>
		/// <param name="isReverse">Is shift pressed?</param>
		/// <param name="queryOnly">Query only?</param>
		/// <returns>Flag whether the search cycled at root visual scope and the candidate.</returns>
		/// <remarks>
		/// Retrieves the candidate for next tab stop depending on the direction
		/// always taking two routes - next/prev.
		/// </remarks>
		private TabStopCandidateSearchResult GetTabStopCandidateElement(bool isReverse, bool queryOnly)
		{
			DependencyObject? pNewTabStop = null;

			var didCycleFocusAtRootVisualScope = false;
			var activeRoot = _contentRoot.VisualTree.ActiveRootVisual;

			if (activeRoot == null)
			{
				return new TabStopCandidateSearchResult(false, null);
			}

			//------------------------------------------------------------------------
			// Bug #29388
			//
			// The internalCycleWorkaround flag is a workaround for an issue in
			// GetNextTabStop()/GetPreviousTabStop() where it fails to properly
			// detect a TabNavigation="Cycle" in a root-level UserControl.
			//
			bool internalCycleWorkaround = false;

			if (_focusedElement != null && _canTabOutOfPlugin)
			{
				// CanProcessTabStop() used to be an early-out test, but the heuristic
				// is flawed and caused bugs like #25058.
				internalCycleWorkaround = CanProcessTabStop(isReverse);
			}
			//------------------------------------------------------------------------

			if (_focusedElement == null || FocusedElementIsBehindFullWindowMediaRoot())
			{
				if (!isReverse)
				{
					pNewTabStop = GetFirstFocusableElement(activeRoot, null);
				}
				else
				{
					pNewTabStop = GetLastFocusableElement(activeRoot, null);
				}

				didCycleFocusAtRootVisualScope = true;
			}
			else if (!isReverse)
			{
				pNewTabStop = GetNextTabStop();

				// If we could not find a tab stop, see if we need to tab cycle.
				if (pNewTabStop == null && (!_canTabOutOfPlugin || internalCycleWorkaround || queryOnly))
				{
					pNewTabStop = GetFirstFocusableElement(activeRoot, null);
					didCycleFocusAtRootVisualScope = true;
				}
			}
			else
			{
				pNewTabStop = GetPreviousTabStop();

				// If we could not find a tab stop, see if we need to tab cycle.
				if (pNewTabStop == null && (!_canTabOutOfPlugin || internalCycleWorkaround || queryOnly))
				{
					pNewTabStop = GetLastFocusableElement(activeRoot, null);
					didCycleFocusAtRootVisualScope = true;
				}
			}

			return new TabStopCandidateSearchResult(didCycleFocusAtRootVisualScope, pNewTabStop);
		}

		/// <summary>
		/// Process TabStop to retrieve the next or previous tab stop element.
		/// </summary>
		/// <param name="isReverse">Is reverse direction?</param>
		/// <param name="queryOnly">Query only.</param>
		/// <returns>Next or previous tab stop element or null.</returns>
		private DependencyObject? ProcessTabStopInternal(bool isReverse, bool queryOnly)
		{
			DependencyObject? ppNewTabStopElement = null;
			bool isTabStopOverridden = false;
			bool didCycleFocusAtRootVisualScope = false;
			DependencyObject? pNewTabStop = null;
			DependencyObject? pDefaultCandidateTabStop = null;
			DependencyObject? pNewTabStopFromCallback = null;

			// Get the default tab stoppable element.
			var result = GetTabStopCandidateElement(isReverse, queryOnly);
			didCycleFocusAtRootVisualScope = result.DidCycleFocusAtRootVisualScope;
			pDefaultCandidateTabStop = result.Candidate;

			// Ask UIElement for a new tab stop suggestion (this is to solve appbar focus issues)
			var processResult = (_focusedElement as UIElement)?.ProcessTabStop(
				_focusedElement,
				pDefaultCandidateTabStop,
				!!isReverse,
				didCycleFocusAtRootVisualScope);
			pNewTabStopFromCallback = processResult?.NewTabStop;
			isTabStopOverridden = processResult?.IsOverriden ?? false;

			if (isTabStopOverridden)
			{
				pNewTabStop = pNewTabStopFromCallback;
			}

			// If no suggestions from framework apply the regular logic
			if (!isTabStopOverridden && pNewTabStop == null && pDefaultCandidateTabStop != null)
			{
				pNewTabStop = pDefaultCandidateTabStop;
			}

			if (pNewTabStop != null)
			{
				ppNewTabStopElement = pNewTabStop;

				pNewTabStop = null;
			}

			return ppNewTabStopElement;
		}

		/// <summary>
		/// Processes the TabStop navigation.
		/// </summary>
		/// <param name="isReverseDirection">Is shift pressed?</param>
		/// <returns>Value indicating whether the request was handled.</returns>
		internal bool ProcessTabStop(bool isReverseDirection)
		{
			var bHandled = false;

			DependencyObject? spNewTabStop;

			// Get the new tab stoppable element.
			bool queryOnly = false;
			spNewTabStop = ProcessTabStopInternal(isReverseDirection, queryOnly);

			var navigationDirection = (isReverseDirection == true) ?
				FocusNavigationDirection.Previous : FocusNavigationDirection.Next;

			if (spNewTabStop != null)
			{
				// Set the focus to the new TabStop control
				var result = SetFocusedElement(new FocusMovement(spNewTabStop, navigationDirection, FocusState.Keyboard));
				bHandled = result.WasMoved;
			}
			else
			{
				Guid correlationId = Guid.NewGuid();
				bool handled = false;
				FocusObserver.DepartFocus(navigationDirection, correlationId, ref handled);
			}

			return bHandled;
		}

		/// <summary>
		/// Returns the first focusable uielement from the root.
		/// </summary>
		/// <returns>First focusable element.</returns>
		/// <remarks>
		/// Special handling for popup, otherwise uses other method to do the work.
		/// </remarks>
		internal UIElement? GetFirstFocusableElement()
		{
			DependencyObject? pFirstFocus = null;
			UIElement? pFirstFocusElement = null;

			if (_contentRoot.VisualTree != null)
			{
				// First give focus to the topmost light-dismiss-enabled popup or a Flyout if any is open.
				var pPopupRoot = _contentRoot.VisualTree.PopupRoot;
				if (pPopupRoot != null)
				{
					Popup? pTopmostLightDismissPopup = pPopupRoot.GetTopmostPopup(PopupRoot.PopupFilter.LightDismissOrFlyout);
					if (pTopmostLightDismissPopup != null)
					{
						pFirstFocusElement = pTopmostLightDismissPopup as UIElement;
					}
				}

				if (pFirstFocusElement == null)
				{
					pFirstFocus = GetFirstFocusableElementFromRoot(false /* bReverse */);

					if (pFirstFocus != null)
					{
						if (pFirstFocus is UIElement pFirstFocusAsUIE)
						{
							pFirstFocusElement = pFirstFocusAsUIE;
						}

						else
						{
							// When the first focusable element is not a Control, look for an
							// appropriate reference to return to the caller who wants a Control.
							// Example: Hyperlink -. Text control hosting the hyperlink
							pFirstFocusElement = GetParentElement(pFirstFocus);
						}
					}
				}
			}

			return pFirstFocusElement;
		}

		/// <summary>
		/// Returns the next focusable element if it is available.
		/// </summary>
		/// <returns>Focusable element or null.</returns>
		/// <remarks>
		/// Includes special handling for Hyperlink.
		/// </remarks>
		private UIElement? GetNextFocusableElement()
		{
			DependencyObject? pNextFocus = GetNextTabStop();
			UIElement? pNextFocusElement = null;

			if (pNextFocus != null)
			{
				if (pNextFocus is UIElement uiElement)
				{
					pNextFocusElement = uiElement;
				}
				else
				{
					// When the first focusable element is not a Control, look for an
					// appropriate reference to return to the caller who wants a Control.
					// Example: Hyperlink -. Text control hosting the hyperlink
					pNextFocusElement = GetParentElement(pNextFocus);
				}
			}

			return pNextFocusElement;
		}

		/// <summary>
		/// Returns the next TabStop control if it is available. Otherwise, return null.
		/// </summary>
		/// <param name="pCurrentTabStop">Current tab stop.</param>
		/// <param name="bIgnoreCurrentTabStopScope">Should the current tab scope be ignored?</param>
		/// <returns>Next tab stop or null.</returns>
		/// <remarks>
		/// Gets next tab stop, includes the full logic with special case handling
		/// Calls internal version of the method which contains further logic
		/// </remarks>
		internal DependencyObject? GetNextTabStop(DependencyObject? pCurrentTabStop = null, bool bIgnoreCurrentTabStopScope = false)
		{
			DependencyObject? pNewTabStop = null;
			DependencyObject? pFocused = pCurrentTabStop ?? _focusedElement;
			DependencyObject? pCurrentCompare = null;
			DependencyObject? pNewTabStopFromCallback = null;

			if (pFocused == null || _contentRoot.VisualTree == null)
			{
				return null; // No next tab stop
			}

			// Assign the compare(TabIndex value) control with the focused control
			pCurrentCompare = pFocused;

			// #0. Ask UIElement for the next tab stop suggestion.
			// For example, LightDismiss enabled Popup will process GetNextTabStop callbacks.
			pNewTabStopFromCallback = (pFocused as UIElement)?.GetNextTabStopOverride();
			pNewTabStop = pNewTabStopFromCallback;

			// #1. Search TabStop from the children
			if (pNewTabStop == null &&
				!bIgnoreCurrentTabStopScope &&
				(IsVisible(pFocused) && (CanHaveChildren(pFocused) || CanHaveFocusableChildren(pFocused))))
			{
				pNewTabStop = GetFirstFocusableElement(pFocused, pNewTabStop);
			}

			// #2. Search TabStop from the sibling of parent
			if (pNewTabStop == null)
			{
				bool bCurrentPassed = false;
				var pCurrent = pFocused;
				var pParent = GetFocusParent(pFocused);
				bool parentIsRootVisual = pParent == _contentRoot.VisualTree.RootVisual;

				while (pParent != null && !parentIsRootVisual && pNewTabStop == null)
				{
					if (IsValidTabStopSearchCandidate(pCurrent) && GetTabNavigation(pCurrent) == KeyboardNavigationMode.Cycle)
					{
						if (pCurrent == GetParentElement(pFocused))
						{
							// The focus will be cycled under the focusable children if the current focused
							// control is the cycle navigation mode
							pNewTabStop = GetFirstFocusableElement(pCurrent!, null);
						}
						else
						{
							// The current can be focusable
							pNewTabStop = GetFirstFocusableElement(pCurrent!, pCurrent);
						}
						break;
					}

					if (IsValidTabStopSearchCandidate(pParent) && GetTabNavigation(pParent) == KeyboardNavigationMode.Once)
					{
						pCurrent = pParent;
						pParent = GetFocusParent(pParent);
						if (pParent == null)
						{
							break;
						}
					}
					else if (!IsValidTabStopSearchCandidate(pParent))
					{
						// Get the parent control whether it is a focusable or not
						var pParentElement = GetParentElement(pParent);
						if (pParentElement == null)
						{
							// if the focused element is under a popup and there is no control in its ancestry up until
							// the popup, then consider tabnavigation as cycle within the popup subtree.
							pParent = GetRootOfPopupSubTree(pCurrent);
							if (pParent != null)
							{
								// try to find the next tabstop.
								pNewTabStop = GetNextTabStopInternal(pParent, pCurrent, pNewTabStop, ref bCurrentPassed, ref pCurrentCompare);
								// Retrieve the first tab stop from the current tab stop when the current tab stop is not focusable.
								if (pNewTabStop != null && !IsFocusable(pNewTabStop))
								{
									pNewTabStop = GetFirstFocusableElement(pNewTabStop, null);
								}
								if (pNewTabStop == null)
								{
									// the focused element is the last tabstop. move the focus to the first
									// focusable element within the popup.
									pNewTabStop = GetFirstFocusableElement(pParent, null);
								}
								break;
							}
							pParent = _contentRoot.VisualTree.ActiveRootVisual;
						}
						else if (pParentElement != null && GetTabNavigation(pParentElement) == KeyboardNavigationMode.Once)
						{
							// We need to get out of the current scope in case of Once navigation mode, so
							// reset the current and parent to search the next available focus control
							pCurrent = pParentElement;
							pParent = GetFocusParent(pParentElement);
							if (pParent == null)
							{
								break;
							}
						}
						else
						{
							// Assign the parent that can have a focusable children.
							// If there is no parent that can be a TapStop, assign the root
							// to figure out the next focusable element from the root
							if (pParentElement != null)
							{
								pParent = pParentElement;
							}
							else
							{
								pParent = _contentRoot.VisualTree.ActiveRootVisual;
							}
						}
					}

					pNewTabStop = GetNextTabStopInternal(
						pParent,
						pCurrent,
						pNewTabStop,
						ref bCurrentPassed,
						ref pCurrentCompare);

					// GetNextTabStoopInternal can return the not focusable element which has a focusable child
					if (pNewTabStop != null && !IsFocusable(pNewTabStop) && CanHaveFocusableChildren(pNewTabStop))
					{
						pNewTabStop = GetFirstFocusableElement(pNewTabStop, null);
					}

					if (pNewTabStop != null)
					{
						break;
					}

					// Only assign the current when the parent is a element that can TabStop
					if (IsValidTabStopSearchCandidate(pParent))
					{
						pCurrent = pParent;
					}

					pParent = GetFocusParent(pParent);

					bCurrentPassed = false;

					parentIsRootVisual = pParent == _contentRoot.VisualTree.RootVisual;
				}
			}

			return pNewTabStop;
		}

		/// <summary>
		/// Return the next TabStop control if it is available.
		/// Otherwise, return null.
		/// </summary>
		/// <param name="pParent">Parent element.</param>
		/// <param name="pCurrent">Current element.</param>
		/// <param name="pCandidate">Candidate.</param>
		/// <param name="bCurrentPassed">Was current passed?</param>
		/// <returns>Next tab stop.</returns>
		private DependencyObject? GetNextTabStopInternal(
			DependencyObject? pParent,
			DependencyObject? pCurrent,
			DependencyObject? pCandidate,
			ref bool bCurrentPassed,
			ref DependencyObject? pCurrentCompare)
		{
			DependencyObject? pNewTabStop = pCandidate;
			DependencyObject? pChildStop = null;

			// Update the compare(TabIndex value) control by searching the siblings of ancestor
			if (IsValidTabStopSearchCandidate(pCurrent))
			{
				pCurrentCompare = pCurrent;
			}

			if (pParent != null)
			{
				int compareIndexResult;
				bool bFoundCurrent = false;

				// Find next TabStop from the children
				var children = FocusProperties.GetFocusChildrenInTabOrder(pParent);
				foreach (var childNoRef in children)
				{
					pChildStop = null;

					if (childNoRef != null && childNoRef == pCurrent)
					{
						bFoundCurrent = true;
						bCurrentPassed = true;
						continue;
					}

					if (childNoRef != null && IsVisible(childNoRef))
					{
						// This will only hit in Pre-RS5 scenarios
						if (childNoRef == pCurrent)
						{
							bFoundCurrent = true;
							bCurrentPassed = true;
							continue;
						}

						if (IsValidTabStopSearchCandidate(childNoRef))
						{
							// If we have a UIElement, such as a StackPanel, we want to check it's children for the next tab stop
							if (!IsPotentialTabStop(childNoRef))
							{
								pChildStop = GetNextTabStopInternal(childNoRef, pCurrent, pNewTabStop, ref bCurrentPassed, ref pCurrentCompare);
							}
							else
							{
								pChildStop = childNoRef;
							}
						}
						else if (CanHaveFocusableChildren(childNoRef))
						{
							pChildStop = GetNextTabStopInternal(childNoRef, pCurrent, pNewTabStop, ref bCurrentPassed, ref pCurrentCompare);
						}
					}

					if (pChildStop != null && (IsFocusable(pChildStop) || CanHaveFocusableChildren(pChildStop)))
					{
						compareIndexResult = CompareTabIndex(pChildStop, pCurrentCompare);

						if (compareIndexResult > 0 || ((bFoundCurrent || bCurrentPassed) && compareIndexResult == 0))
						{
							if (pNewTabStop != null)
							{
								if (CompareTabIndex(pChildStop, pNewTabStop) < 0)
								{
									pNewTabStop = pChildStop;
								}
							}
							else
							{
								pNewTabStop = pChildStop;
							}
						}
					}
				}
			}

			return pNewTabStop;
		}

		/// <summary>
		/// Return the previous TabStop control if it is available. Otherwise, return null.
		/// </summary>
		/// <param name="pCurrentTabStop">Current tab stop</param>
		/// <returns>Previous tab stop.</returns>
		internal DependencyObject? GetPreviousTabStop(DependencyObject? pCurrentTabStop = null)
		{
			DependencyObject? pFocused = pCurrentTabStop ?? _focusedElement;
			DependencyObject? pNewTabStop = null;
			DependencyObject? pCurrentCompare = null;

			if (pFocused == null && _contentRoot.VisualTree == null)
			{
				return null; // No previous tab stop
			}

			// Assign the compare(TabIndex value) control with the focused control
			pCurrentCompare = pFocused;

			// #0. Ask UIElement for the previous tab stop suggestion.
			// For example, LightDismiss enabled Popup will process GetPreviousTabStop callbacks.
			var newTabStopFromCallback = (pFocused as UIElement)?.GetPreviousTabStopOverride();
			pNewTabStop = newTabStopFromCallback;

			// Search the previous TabStop from the sibling of parent
			if (pNewTabStop == null)
			{
				bool bCurrentPassed = false;

				var pCurrent = pFocused;
				var parent = GetFocusParent(pFocused);

				while (parent != null && !(parent is RootVisual) && pNewTabStop == null)
				{
					if (IsValidTabStopSearchCandidate(pCurrent) && GetTabNavigation(pCurrent) == KeyboardNavigationMode.Cycle)
					{
						pNewTabStop = GetLastFocusableElement(pCurrent!, pCurrent);
						break;
					}

					if (IsValidTabStopSearchCandidate(parent) && GetTabNavigation(parent) == KeyboardNavigationMode.Once)
					{
						// Set focus on the parent if it is a focusable control. Otherwise, keep search it up.
						if (IsFocusable(parent))
						{
							pNewTabStop = parent;
							break;
						}
						else
						{
							pCurrent = parent;
							parent = GetFocusParent(parent);
							if (parent == null)
							{
								break;
							}
						}
					}
					else if (!IsValidTabStopSearchCandidate(parent))
					{
						// Get the parent control whether it is a focusable or not
						var parentElement = GetParentElement(parent);
						if (parentElement == null)
						{
							// if the focused element is under a popup and there is no control in its ancestry up until
							// the popup, then consider tabnavigation as cycle within the popup subtree.
							parent = GetRootOfPopupSubTree(pCurrent);
							if (parent != null)
							{
								// find the previous tabstop
								pNewTabStop = GetPreviousTabStopInternal(parent, pCurrent, pNewTabStop, ref bCurrentPassed, ref pCurrentCompare);
								// Retrieve the last tab stop from the current tab stop when the current tab stop is not focusable.
								if (pNewTabStop != null && !IsFocusable(pNewTabStop))
								{
									pNewTabStop = GetLastFocusableElement(pNewTabStop, null);
								}
								if (pNewTabStop == null)
								{
									// focused element is the first tabstop within the popup. move focus
									// to the last tabstop within the popup
									pNewTabStop = GetLastFocusableElement(parent, null);
								}
								break;
							}
							parent = _contentRoot.VisualTree.ActiveRootVisual;
						}
						else if (parentElement != null && GetTabNavigation(parentElement) == KeyboardNavigationMode.Once)
						{
							// Set focus on the parent control if it is a focusable control. Otherwise, keep search it up.
							if (IsFocusable(parentElement))
							{
								pNewTabStop = parentElement;
								break;
							}
							else
							{
								pCurrent = parent;
								parent = parentElement;
							}
						}
						else
						{
							// Assign the parent that can have a focusable children.
							// If there is no parent that can be a TapStop, assign the root
							// to figure out the next focusable element from the root
							if (parentElement != null)
							{
								parent = parentElement;
							}
							else
							{
								parent = _contentRoot.VisualTree.ActiveRootVisual;
							}
						}
					}

					pNewTabStop = GetPreviousTabStopInternal(
						parent,
						pCurrent,
						pNewTabStop,
						ref bCurrentPassed,
						ref pCurrentCompare);

					if (pNewTabStop == null && IsPotentialTabStop(parent) && IsFocusable(parent))
					{
						if (parent != null && IsPotentialTabStop(parent) && GetTabNavigation(parent) == KeyboardNavigationMode.Cycle)
						{
							pNewTabStop = GetLastFocusableElement(parent, null /*LastFocusable*/);
						}
						else
						{
							pNewTabStop = parent;
						}
					}
					else
					{
						// Find the last focusable element from the current focusable container
						if (pNewTabStop != null && CanHaveFocusableChildren(pNewTabStop))
						{
							pNewTabStop = GetLastFocusableElement(pNewTabStop, null);
						}
					}

					if (pNewTabStop != null)
					{
						break;
					}

					// Only assign the current when the parent is a element that can TapStop
					if (IsValidTabStopSearchCandidate(parent))
					{
						pCurrent = parent;
					}

					parent = GetFocusParent(parent);
					bCurrentPassed = false;
				}
			}

			return pNewTabStop;
		}

		/// <summary>
		/// Return the previous tab stop control if it is available.
		/// Otherwise, return null.
		/// </summary>
		private DependencyObject? GetPreviousTabStopInternal(
			DependencyObject? parent,
			DependencyObject? pCurrent,
			DependencyObject? pCandidate,
			ref bool bCurrentPassed,
			ref DependencyObject? pCurrentCompare)
		{
			DependencyObject? pNewTabStop = pCandidate;
			DependencyObject? pChildStop = null;

			// Update the compare(TabIndex value) control by searching the siblings of ancestor
			if (IsValidTabStopSearchCandidate(pCurrent))
			{
				pCurrentCompare = pCurrent;
			}

			if (parent != null)
			{
				int compareIndexResult;
				bool bFoundCurrent = false;
				bool bCurrentCompare;

				// Find previous TabStop from the children
				var children = FocusProperties.GetFocusChildrenInTabOrder(parent);
				foreach (var child in children)
				{
					bCurrentCompare = false;
					pChildStop = null;

					if (child != null && child == pCurrent)
					{
						bFoundCurrent = true;
						bCurrentPassed = true;
						continue;
					}

					if (child != null && IsVisible(child))
					{
						// This will only hit in Pre-RS5 scenarios
						if (child == pCurrent)
						{
							bFoundCurrent = true;
							bCurrentPassed = true;
							continue;
						}

						if (IsValidTabStopSearchCandidate(child))
						{
							// If we have a UIElement, such as a StackPanel, we want to check it's children for the next tab stop
							if (!IsPotentialTabStop(child))
							{
								pChildStop = GetPreviousTabStopInternal(
									child,
									pCurrent,
									pNewTabStop,
									ref bCurrentPassed,
									ref pCurrentCompare);
								bCurrentCompare = true;
							}
							else
							{
								pChildStop = child;
							}
						}
						else if (CanHaveFocusableChildren(child))
						{
							pChildStop = GetPreviousTabStopInternal(
								child,
								pCurrent,
								pNewTabStop,
								ref bCurrentPassed,
								ref pCurrentCompare);
							bCurrentCompare = true;
						}
					}

					if (pChildStop != null && (IsFocusable(pChildStop) || CanHaveFocusableChildren(pChildStop)))
					{
						compareIndexResult = CompareTabIndex(pChildStop, pCurrentCompare);

						if (compareIndexResult < 0 ||
							(((!bFoundCurrent && !bCurrentPassed) || bCurrentCompare) && compareIndexResult == 0))
						{
							if (pNewTabStop != null)
							{
								if (CompareTabIndex(pChildStop, pNewTabStop) >= 0)
								{
									pNewTabStop = pChildStop;
								}
							}
							else
							{
								pNewTabStop = pChildStop;
							}
						}
					}
				}
			}

			return pNewTabStop;
		}

		/// <summary>
		/// Compare TabIndex value between focusable(TabStop) controls.
		/// </summary>
		/// <param name="firstObject"></param>
		/// <param name="secondObject"></param>
		/// <returns></returns>
		/// <remarks>
		/// Return +1 if control1 &gt; control2
		/// Return = if control1 = control2
		/// Return -1 if control1 &lt; control2
		/// </remarks>
		private int CompareTabIndex(DependencyObject? firstObject, DependencyObject? secondObject)
		{
			if (GetTabIndex(firstObject) > GetTabIndex(secondObject))
			{
				return 1;
			}
			else if (GetTabIndex(firstObject) < GetTabIndex(secondObject))
			{
				return -1;
			}

			return 0;
		}

		/// <summary>
		/// Checks whether focus is currently on the first tab stop.
		/// </summary>
		/// <returns>True if focus is in the first tab stop.</returns>
		private bool IsFocusOnFirstTabStop()
		{
			DependencyObject? pFocused = _focusedElement;
			DependencyObject? activeRoot = null;
			DependencyObject? pFirstFocus = null;

			if (pFocused == null || _contentRoot.VisualTree == null)
			{
				return false;
			}

			activeRoot = _contentRoot.VisualTree.ActiveRootVisual;
			MUX_ASSERT(activeRoot != null);

			pFirstFocus = GetFirstFocusableElement(activeRoot!, null);

			if (pFocused == pFirstFocus)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks whether focus is currently on the first tab stop.
		/// </summary>
		/// <returns>True if focus is in the first tab stop.</returns>
		private bool IsFocusOnLastTabStop()
		{
			DependencyObject? pFocused = _focusedElement;
			DependencyObject? activeRoot = null;
			DependencyObject? pLastFocus = null;

			if (pFocused == null || _contentRoot.VisualTree == null)
			{
				return false;
			}

			activeRoot = _contentRoot.VisualTree.ActiveRootVisual;
			MUX_ASSERT(activeRoot != null);

			pLastFocus = GetLastFocusableElement(activeRoot!, null);

			if (pFocused == pLastFocus)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if there is a focusable child.
		/// </summary>
		/// <param name="pParent">Parent.</param>
		/// <returns>True if a focusable child exists.</returns>
		private bool CanHaveFocusableChildren(DependencyObject? pParent) =>
			FocusProperties.CanHaveFocusableChildren(pParent);

		/// <summary>
		/// Returns the control of parent (ancestor) from the current control
		/// </summary>
		/// <param name="pCurrent">Current control.</param>
		/// <returns>Parent UIElement or null.</returns>
		private UIElement? GetParentElement(DependencyObject? pCurrent)
		{
			DependencyObject? pParent = null;

			if (pCurrent != null)
			{
				pParent = GetFocusParent(pCurrent);

				while (pParent != null)
				{
					if (IsValidTabStopSearchCandidate(pParent) && pParent is UIElement parentUiElement)
					{
						return parentUiElement;
					}

					pParent = GetFocusParent(pParent);
				}
			}

			return null;
		}

		/// <summary>
		/// Notify the focus changing that ensure the focused element visible with input host manager.
		/// </summary>
		/// <param name="bringIntoView">Bring into view?</param>
		/// <param name="animateIfBringIntoView">Animate?</param>
		private void NotifyFocusChanged(bool bringIntoView, bool animateIfBringIntoView)
		{
			if (_focusedElement != null) //&& static_cast<DependencyObject>(_focusedElement).GetContext())
			{
				_contentRoot.InputManager.NotifyFocusChanged(_focusedElement, bringIntoView, animateIfBringIntoView);
			}
		}

		/// <summary>
		/// Determine if a particular DependencyObject cares to take focus.
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>True if focusable.</returns>
		private bool IsFocusable(DependencyObject? dependencyObject) => FocusProperties.IsFocusable(dependencyObject);

		/// <summary>
		/// Return the object that should be considered the parent for the purposes
		/// of tab focus.  This is not necessarily the same as the standard
		/// DependencyObject.GetParent() object.
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>Parent or null.</returns>
		private DependencyObject? GetFocusParent(DependencyObject? dependencyObject)
		{
			if (dependencyObject != null)
			{
				if (FocusableHelper.IsFocusableDO(dependencyObject))
				{
					return FocusableHelper.GetContainingFrameworkElementIfFocusable(dependencyObject);

				}
				else
				{
					var activeRoot = _contentRoot.VisualTree.ActiveRootVisual;
					MUX_ASSERT(activeRoot != null);

					// Root SV is located between the visual root and hidden root.
					// GetFocusParent shouldn't return the ancestor of the visual root.
					if (dependencyObject != activeRoot)
					{
						return dependencyObject.GetParent() as DependencyObject;
					}
				}
			}

			// null input -. null output
			return null;
		}

		/// <summary>
		/// Determines if the object is visible.
		/// </summary>
		/// <param name="pObject"></param>
		/// <returns>True if visible.</returns>
		private bool IsVisible(DependencyObject? pObject) => FocusProperties.IsVisible(pObject);

		/// <summary>
		/// Gets the value set by application developer to determine the tab
		/// order. Default value is int.MaxValue which means to evaluate them in
		/// their ordering of the focus children collection.
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>Tab index or int max value.</returns>
		private int GetTabIndex(DependencyObject? dependencyObject)
		{
			if (dependencyObject != null)
			{
				if (dependencyObject is UIElement uiElement)
				{
					return uiElement.TabIndex;
				}
				else if (FocusableHelper.GetIFocusableForDO(dependencyObject) is IFocusable focusable)
				{
					return focusable.GetTabIndex();
				}
			}

			return int.MaxValue;
		}

		/// <summary>
		/// Determines if this object type can have children that should be considered
		/// for tab ordering.  Note that this doesn't necessarily mean if the object
		/// instance has focusable children right now.  CanHaveChildren() may be true
		/// even though the focus children collection is empty.
		/// </summary>
		/// <param name="pObject"></param>
		/// <returns></returns>
		private bool CanHaveChildren(DependencyObject? pObject)
		{
			if (pObject is UIElement pUIElement) // Really INDEX_VISUAL but Visual is not in the publicly exposed hierarchy.
			{
				return pUIElement.CanHaveChildren();
			}

			return false;
		}

		/// <summary>
		/// Returns non-null if the given object is within a popup.
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>Root of popup tree or null.</returns>
		private DependencyObject? GetRootOfPopupSubTree(DependencyObject? dependencyObject)
		{
			DependencyObject? root = null;

			if (dependencyObject != null)
			{
				if (dependencyObject is UIElement pUIElement)
				{
					root = pUIElement.GetRootOfPopupSubTree();
				}
				else
				{
					var parentElement = GetParentElement(dependencyObject);

					if (parentElement != null)
					{
						root = parentElement.GetRootOfPopupSubTree();
					}
				}
			}

			return root;
		}

		/// <summary>
		/// Returns the value of the TabNavigation property.
		/// </summary>
		/// <param name="pObject">Dependency object.</param>
		/// <returns>Keyboard navigation mode.</returns>
		private KeyboardNavigationMode GetTabNavigation(DependencyObject? pObject)
		{
			if (!(pObject is UIElement element))
			{
				return KeyboardNavigationMode.Local;
			}

			return element.GetTabNavigation();
		}

		/// <summary>
		/// Tests to see if the parameter object's type supports being a tab stop.
		/// Though the specific instance might not be a valid tab stop at the moment.
		/// (Example: Disabled state)
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>True if input object is potential tab stop.</returns>
		private bool IsPotentialTabStop(DependencyObject? dependencyObject) => FocusProperties.IsPotentialTabStop(dependencyObject);

		/// <summary>
		/// Allow the focus change event with the below condition
		///	1. Plugin has a focus
		///	2. FullScreen window mode
		/// </summary>
		/// <returns></returns>
		private bool CanRaiseFocusEventChange()
		{
			bool bCanRaiseFocusEvent = false;

			//if (_coreService != null)
			{
				if (IsPluginFocused())
				{
					bCanRaiseFocusEvent = true;
				}
			}

			return bCanRaiseFocusEvent;
		}

		private bool ShouldUpdateFocus(DependencyObject? newFocus, FocusState focusState)
		{
			bool shouldUpdateFocus = true;

			//TFS 5777889. There are some scenarios (mainly with SIP), where we want to interact with another element, but
			//have the focus stay with the currently focused element
			if (newFocus != null)
			{
				if (newFocus is FrameworkElement newFocusFrameworkElement)
				{
					shouldUpdateFocus = FocusSelection.ShouldUpdateFocus(
						newFocusFrameworkElement,
						focusState);
				}

				else if (newFocus is FlyoutBase newFocusFlyoutBase)
				{
					shouldUpdateFocus = FocusSelection.ShouldUpdateFocus(
						newFocusFlyoutBase,
						focusState);
				}

				else if (newFocus is TextElement newFocusTextElement)
				{
					shouldUpdateFocus = FocusSelection.ShouldUpdateFocus(
						newFocusTextElement,
						focusState);
				}
			}

			return shouldUpdateFocus;
		}

		/// <summary>
		///	Changes our currently focused element.  FocusMovementResult.WasMoved indicates
		/// whether the update is successful.  We will also be responsible for raising focus related
		/// events (GettingFocus/LosingFocus and GotFocus/LostFocus).
		///
		/// FocusMovementResult.WasMoved indicates that the process has executed successfully.
		/// </summary>
		/// <param name="movement"></param>
		/// <returns></returns>
		/// <remarks>
		/// We have two ways (FocusMovementResult.GetHResult() and FocusMovementResult.WasMoved) to report success and failure.
		/// This is because it is possible that the given pNewFocus could not actually take focus for
		/// innocent reasons. (Currently in disabled state, etc.) This is not an important failure and GetHResult()
		/// return S_OK even though WasMoved() is false.
		/// 
		/// A failing HRESULT is reserved for important failures like OOM conditions or
		/// reentrancy prevention. This distinction is made in the interest of reducing noise 
		/// in tools like XcpMon.
		///
		///  Boundary cases:
		/// - If pNewFocus is the object that already has current focus,
		///   it is counted as success even though nothing was technically "updated".
		///
		/// - If a focus change is cancelled in a Getting/Losing Focus handler, it
		///   is counted as success even though focus was technically not "updated".
		/// </remarks>
		private FocusMovementResult UpdateFocus(FocusMovement movement)
		{
			if (movement is null)
			{
				throw new ArgumentNullException(nameof(movement));
			}

			var newFocusTarget = movement.Target;
			FocusNavigationDirection focusNavigationDirection = movement.Direction;
			bool forceBringIntoView = movement.ForceBringIntoView;
			bool animateIfBringIntoView = movement.AnimateIfBringIntoView;

			FocusState nonCoercedFocusState = movement.FocusState;
			FocusState coercedFocusState = CoerceFocusState(nonCoercedFocusState);

			bool success = false;

			bool focusCancelled = false;
			bool shouldCompleteAsyncOperation = movement.ShouldCompleteAsyncOperation;

			DependencyObject? oldFocusedElement = null;

			bool shouldBringIntoView = false;
			Guid correlationId = _asyncOperation != null ? _asyncOperation.CorrelationId : movement.CorrelationId;

			LogTraceTraceUpdateFocusBegin();

			InputDeviceType lastInputDeviceType = InputDeviceType.None;

			//TODO:MZ:Exceptions can break focus forever
			if (_asyncOperation != null && shouldCompleteAsyncOperation == false)
			{
				//throw new InvalidOperationException("An asynchronous operation is in progress.");

				//return Cleanup();
			}

			if (_focusLocked && _ignoreFocusLock == false)
			{
				throw new InvalidOperationException("Focus change not allowed during focus changing.");
			}

			if (!ShouldUpdateFocus(newFocusTarget, nonCoercedFocusState))
			{
				return Cleanup();
			}

			if (_contentRoot.IsShuttingDown())
			{
				// Don't change focus setting in case of the processing of ResetVisualTree
				return Cleanup();
			}

			// When navigating between pages, we need to clear the (prior focus) state on XYFocus
			if (newFocusTarget == null
				&& coercedFocusState == FocusState.Unfocused)
			{
				_xyFocus.ResetManifolds();
			}

			lastInputDeviceType = _contentRoot.InputManager.LastInputDeviceType;

			if (lastInputDeviceType == InputDeviceType.GamepadOrRemote && __LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBox_Available)
			{
				var pElementAsTextBox = newFocusTarget as TextBox;
				if (pElementAsTextBox != null)
				{
					//TODO Uno: We don't have special handling for Gamepad input.
					//pElementAsTextBox.IncomingFocusFromGamepad();
				}
			}

			if (newFocusTarget == _focusedElement)
			{
				UIElement? newFocusAsElement = newFocusTarget as UIElement;

				if (newFocusAsElement != null && newFocusAsElement.FocusState != coercedFocusState)
				{
					// We do not raise GettingFocus here since the OldFocusedElement and NewFocusedElement
					// would be the same element.
					RaiseGotFocusEvent(_focusedElement!, correlationId);

					// Make sure the FocusState is up-to-date.
					newFocusAsElement.UpdateFocusState(coercedFocusState);
					_realFocusStateForFocusedElement = nonCoercedFocusState;
				}
				else if (FocusableHelper.GetIFocusableForDO(newFocusTarget) is IFocusable newFocusAsIFocusable)
				{
					var value = (FocusState)newFocusTarget!.GetValue(newFocusAsIFocusable.GetFocusStatePropertyIndex());

					if (coercedFocusState != value)
					{
						RaiseGotFocusEvent(_focusedElement!, correlationId);

						value = coercedFocusState;

						newFocusTarget.SetValue(newFocusAsIFocusable.GetFocusStatePropertyIndex(), value);
					}

					_realFocusStateForFocusedElement = nonCoercedFocusState;
				}

				success = true;

				// No change in focus element - can skip the rest of this method.
				return Cleanup();
			}

			if (movement.RaiseGettingLosingEvents)
			{
				if (CanRaiseFocusEventChange() &&
					RaiseAndProcessGettingAndLosingFocusEvents(
						_focusedElement,
						newFocusTarget,
						nonCoercedFocusState,
						focusNavigationDirection,
						movement.CanCancel,
						correlationId).Canceled)
				{
					success = true;
					focusCancelled = true;

					return Cleanup();
				}
			}

			MUX_ASSERT((newFocusTarget == null) || IsFocusable(newFocusTarget));

			// Update the previous focused control
			oldFocusedElement = _focusedElement; // Still has reference that will be freed in Cleanup.
			_focusingElement = newFocusTarget;

			if (oldFocusedElement != null && FocusableHelper.GetIFocusableForDO(oldFocusedElement) is IFocusable oldFocusFocusable)
			{
				oldFocusedElement.SetValue(
					oldFocusFocusable.GetFocusStatePropertyIndex(),
					FocusState.Unfocused);
			}
			else if (oldFocusedElement != null && oldFocusedElement is UIElement oldFocusedUIElement)
			{
				oldFocusedUIElement.UpdateFocusState(FocusState.Unfocused);
			}

			// Update the focused control
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				_log.Value.LogDebug($"{nameof(UpdateFocus)}() - oldFocus={_focusedElement} ({_realFocusStateForFocusedElement}), newFocus={newFocusTarget} ({nonCoercedFocusState})");
			}
			_focusedElement = newFocusTarget;
			_realFocusStateForFocusedElement = nonCoercedFocusState;

			//
			// NOTE:
			//
			// The calls to SetFocus will synchronously call into SetWindowFocus
			// We have to be extra careful that _focusedElement is already set to avoid
			// a reentrancy call to UpdateFocus
			//
			_contentRoot.FocusAdapter.SetFocus();

			// Bring element into view when it is focused using the keyboard, so user
			// can see the element that was tabbed to
			if (_focusedElement != null
				&& (nonCoercedFocusState == FocusState.Keyboard || forceBringIntoView))
			{
				var pFocusedElement = _focusedElement as UIElement;
				if (pFocusedElement != null)
				{
					// Note that this is done before calling UpdateFocusState, so the
					// control can use the state before it gains focus to decide whether it should
					// be brought into view when focus is gained.
					shouldBringIntoView = true;
				}
				else
				{
					// Other TabStop elements should be brought into view.
					shouldBringIntoView = IsPotentialTabStop(_focusedElement);
				}
			}

			// UpdateFocusState.
			// Use pNewFocus for UpdateFocusState instead of _focusedElement because the .SetFocus call above can cause
			// reentrancy and change _focusedElement.
			if (newFocusTarget != null && FocusableHelper.GetIFocusableForDO(newFocusTarget) is IFocusable newFocusFocusable)
			{
				newFocusTarget.SetValue(newFocusFocusable.GetFocusStatePropertyIndex(), coercedFocusState);
			}
			else if (newFocusTarget is UIElement newFocusTargetUIElement)
			{
				// Some controls query this flag immediately - setting in OnGotFocus is too late.
				newFocusTargetUIElement.UpdateFocusState(coercedFocusState);
			}

			// We don't need to do a complete UpdateFocusRect now (redrawing the rect), as NWDrawTree will call UpdateFocusRect
			// when it needs it. But we do want to see if we can clean up FocusRectManager's secret children, particularly in
			// the case when the previously focused item has been removed from the tree
			//UpdateFocusRect(focusNavigationDirection, true /* cleanOnly */);

			// TODO Uno specific: We need to do a full redraw, as render loop does not yet check for focus visuals rendering.
			UpdateFocusRect(focusNavigationDirection, false);
			FocusNative(_focusedElement as UIElement);
			_contentRoot.InputManager.Pointers.NotifyFocusChanged(); // Note: This sounds like a duplicate of the NotifyFocusChanged done a few lines below (1944). We need to evaluate if this is still relevant or if we can just remove that uno-specific call.

			// At this point the focused pointer has been switched.  So success is true
			// even in the case we run into trouble raising the event(s) to notify as such.
			success = true;

			// If the new focused element is not a Text Control then we need to
			// call ClearLastSelectedTextElement since we are sure that
			// there will be no selection rect drawn on the screen.
			// This is to achieve the light text selection dismiss model.
			if (_focusedElement == null || !TextCore.IsTextControl(_focusedElement))
			{
				if (_isPrevFocusTextControl)
				{
					var textCore = TextCore.Instance;
					textCore.ClearLastSelectedTextElement();
					_isPrevFocusTextControl = false;
				}
			}
			else
			{
				_isPrevFocusTextControl = true;
			}

			// Fire focus changed event for UIAutomation
			if (_focusedElement != null)
			{
				FireAutomationFocusChanged();
			}

			// Raise the focus Lost/GotFocus events

			// Raise the focus event while plugin has a focus, on full screen mode or on windowless mode
			if (CanRaiseFocusEventChange())
			{
				// Raise the LostFocus event to the old focused element
				if (oldFocusedElement != null)
				{
					bool isNavigatedToByEngagingControl = newFocusTarget != null && NavigatedToByEngagingControl(newFocusTarget);

					if (_engagedControl != null && !isNavigatedToByEngagingControl)
					{
						_engagedControl.RemoveFocusEngagement();
					}

					RaiseLostFocusEvent(oldFocusedElement, correlationId);
				}
				else
				{
					// Raise the FocusManagerLostFocus event asynchronously
					// TODO Uno: We raise the events using dispatcher queue, which is a bit different from MUX
					// which does everything via EventManager
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						LostFocus?.Invoke(null, new FocusManagerLostFocusEventArgs(null, correlationId));
					});
				}

				if (_focusedElement != null)
				{
					RaiseGotFocusEvent(_focusedElement, correlationId);
				}
				else
				{
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						GotFocus?.Invoke(null, new FocusManagerGotFocusEventArgs(_focusedElement, correlationId));
					});
				}
			}
			else if (oldFocusedElement != null && oldFocusedElement is Control control)
			{
				// Update the visual state of the old focused element even when IsPluginFocused() returns false so it no longer displays focus cues.
				control.UpdateVisualState(true);
			}

			// Notify the focus changing on InputManager to ensure the focused element visible with
			// Input Host Manager
			NotifyFocusChanged(shouldBringIntoView, animateIfBringIntoView);

			_contentRoot.AccessKeyExport.UpdateScope();

			// Request the playing sound for changing focus with the keyboard, gamepad or remote input
			if ((coercedFocusState == FocusState.Keyboard && _contentRoot.InputManager.ShouldRequestFocusSound()) &&
				(lastInputDeviceType == InputDeviceType.Keyboard || lastInputDeviceType == InputDeviceType.GamepadOrRemote))
			{
				ElementSoundPlayerService.Instance.RequestInteractionSoundForElement(ElementSoundKind.Focus, newFocusTarget);
			}

			_focusingElement = null;

			return Cleanup();

			FocusMovementResult Cleanup()
			{
				TraceUpdateFocusEnd(newFocusTarget);

				// Before RS2, UpdateFocus did not propagate errors. As a result, we want to limit the number of failure
				// cases that callers should deal with to only those related to the new RS2 GettingFocus and LosingFocus events				

				FocusMovementResult result = new FocusMovementResult(success, focusCancelled);

				if (_asyncOperation != null)
				{
					CancelCurrentAsyncOperation(result);
				}

				_currentFocusOperationCancellable = true;

				return result;
			}
		}

		private void FireAutomationFocusChanged()
		{
			AutomationPeer? pAP = null;
			// TODO Uno: When automation is developed further, we want the second check to make sure not to create automation peers when not needed
			if (_focusedElement != null) //&& S_OK == _pCoreService.UIAClientsAreListening(UIAXcp.AEAutomationFocusChanged)) 
			{
				pAP = (_focusedElement as UIElement)?.OnCreateAutomationPeerInternal();

				// There's one specific circumstance that we want to handle: attempting to focus a ContentControl inside a popup.
				// The ContentControl is able to be keyboard focused, but because ContentControlAutomationPeer doesn't exist,
				// we won't raise any UIA focus changed event since we have no automation peer to raise it on.
				// Since UIA clients like Narrator may be relying on this to transfer focus into an opened popup,
				// we should raise the focus changed event on the popup containing the ContentControl,
				// so that way we can ensure that UIA clients like Narrator have properly had focus trapped in the popup.
				// TODO 10588657: Undo this change when we implement a ContentControlAutomationPeer.
				if (pAP == null &&
					_focusedElement is ContentControl contentControl)
				{
					var focusedElementAsUIE = _focusedElement as UIElement;
					var popupToFocus = PopupRoot.GetOpenPopupForElement(focusedElementAsUIE!);

					if (popupToFocus != null)
					{
						pAP = popupToFocus.OnCreateAutomationPeerInternal();
					}
				}

				if (pAP != null)
				{
					_focusedAutomationPeer = pAP;
					var apToRaiseEvent = pAP.EventsSource ?? pAP;
					apToRaiseEvent.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
				}
			}

			if (pAP == null)
			{
				_focusedAutomationPeer = null;
			}
		}

		/// <summary>
		/// Raises the lost focus event asynchronously.
		/// </summary>
		/// <param name="pLostFocusElement">Element which lost focus.</param>
		/// <param name="correlationId">Correlation ID.</param>
		private void RaiseLostFocusEvent(
			DependencyObject pLostFocusElement,
			Guid correlationId)
		{
			if (pLostFocusElement is null)
			{
				throw new ArgumentNullException(nameof(pLostFocusElement));
			}

			// Create DO that represents the GotFocus event args
			var spLostFocusEventArgs = new RoutedEventArgs();
			var spFocusManagerLostFocusEventArgs = new FocusManagerLostFocusEventArgs(pLostFocusElement, correlationId);

			// Set the GotFocus Source value on the routed event args
			spLostFocusEventArgs.OriginalSource = pLostFocusElement;

			// Raise the LostFocus event to the old focused element asynchronously
			if (FocusableHelper.GetIFocusableForDO(pLostFocusElement) is IFocusable lostFocusElementFocusable)
			{
				// In the case of an IFocusable raise both <IFocusable>_LostFocus and UIElement_LostFocus.
				// UIElement_LostFocus is used internally for to decide where focus rects should be rendered.
				_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					lostFocusElementFocusable.OnLostFocus(spLostFocusEventArgs);
				});
			}

			// Raise the LostFocus event to the new focused element asynchronously
			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				(pLostFocusElement as UIElement)?.RaiseEvent(UIElement.LostFocusEvent, spLostFocusEventArgs);
			});

			// Raise the FocusManagerLostFocus event to the focus manager asynchronously
			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				LostFocus?.Invoke(null, spFocusManagerLostFocusEventArgs);
			});
		}

		/// <summary>
		/// Returns true if pFocused was navigated to by engaging a control.
		/// </summary>
		/// <param name="pFocused">Focused element.</param>
		/// <returns>True if focused was navigated to by engaging a control.</returns>
		private bool NavigatedToByEngagingControl(DependencyObject focused)
		{
			DependencyObject? pFocused = focused;
			bool isNavigatedTo = false;

			if (_engagedControl != null)
			{
				bool isChild = false;
				bool isElementChildOfPopupOpenedDuringEngagement = false;
				bool isElementPopupOpenedDuringEngagement = false;
				bool isSelf = _engagedControl == (pFocused as Control);

				if (FocusableHelper.IsFocusableDO(pFocused))
				{
					pFocused = GetFocusParent(pFocused) as DependencyObject;
				}

				var pNewFocusedElement = pFocused as UIElement;

				if (pNewFocusedElement != null)
				{
					isChild = _engagedControl.IsAncestorOf(pNewFocusedElement);
				}

				var pNewFocusedElementAsPopup = pFocused as Popup;
				if (GetRootOfPopupSubTree(pNewFocusedElement) != null || pNewFocusedElementAsPopup != null)
				{
					var popupList = PopupRoot.GetPopupChildrenOpenedDuringEngagement(pFocused!);

					foreach (var popup in popupList)
					{
						isElementPopupOpenedDuringEngagement = (popup == pNewFocusedElement);
						if (isElementPopupOpenedDuringEngagement) { break; }

						/*
						  In the case of a (Menu)Flyout, the new Focused Element could
						  be the actual popup, which is an ancestor of the (Menu)FlyoutPresenter
						  popup opened during engagement. This is a result of the fact that Flyouts (eg Button.Flyout)
						  are excluded from the Visual Tree.

						  In this situation, the new Focused element would be a Popup with no parent.
						  It's child would parent the 'popup' opened during engagement.
						*/
						bool isNewFocusedElementChildOfFlyoutOpenedDuringEngagement =
									pNewFocusedElementAsPopup != null &&
									(pNewFocusedElementAsPopup.Child != null) &&
									(popup == pNewFocusedElementAsPopup.Child ||
									 pNewFocusedElementAsPopup.Child.IsAncestorOf(popup));

						isElementChildOfPopupOpenedDuringEngagement =
							isNewFocusedElementChildOfFlyoutOpenedDuringEngagement
							|| popup.IsAncestorOf(pNewFocusedElement);

						if (isElementChildOfPopupOpenedDuringEngagement)
						{
							break;
						}
					}
				}

				/*
		        The new focused element is  navigated to during Engagement if:
		        1. It is the engaged control OR
		        2. It is a child of the engaged control OR
		        3. It is a popup opened during engagement OR
		        4. It is the descendant of a popup opened during engagement
		        */
				isNavigatedTo =
					isChild ||
					isSelf ||
					isElementPopupOpenedDuringEngagement ||
					isElementChildOfPopupOpenedDuringEngagement;
			}

			return isNavigatedTo;
		}

		/// <summary>
		/// Raises the got focus event asynchronously.
		/// </summary>
		/// <param name="pGotFocusElement">Element that got focus.</param>
		/// <param name="correlationId">Correlation Id</param>
		private void RaiseGotFocusEvent(DependencyObject pGotFocusElement, Guid correlationId)
		{
			if (pGotFocusElement is null)
			{
				throw new ArgumentNullException(nameof(pGotFocusElement));
			}

			// Create DO that represents the GotFocus event args
			var spGotFocusEventArgs = new RoutedEventArgs();
			var spFocusManagerGotFocusEventArgs = new FocusManagerGotFocusEventArgs(_focusedElement, correlationId);

			// Set the GotFocus Source value on the routed event args
			spGotFocusEventArgs.OriginalSource = pGotFocusElement;

			if (FocusableHelper.GetIFocusableForDO(pGotFocusElement) is IFocusable gotFocusElementFocusable)
			{
				_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					gotFocusElementFocusable.OnGotFocus(spGotFocusEventArgs);
				});
			}

			// Raise the GotFocus event to the new focused element asynchronously
			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				(pGotFocusElement as UIElement)?.RaiseEvent(UIElement.GotFocusEvent, spGotFocusEventArgs);
			});

			// Raise the FocusManagerGotFocus event to the focus manager asynchronously
			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				GotFocus?.Invoke(null, spFocusManagerGotFocusEventArgs);
			});
		}

		/// <summary>
		/// Raises focus element removed event.
		/// </summary>
		/// <param name="currentNextFocusableElement">Current next focusable element.</param>
		/// <returns>New focused element.</returns>
		private DependencyObject? RaiseFocusElementRemovedEvent(DependencyObject? currentNextFocusableElement)
		{
			var eventArgs = new FocusedElementRemovedEventArgs(_focusedElement, currentNextFocusableElement);

			FocusedElementRemoved?.Invoke(this, eventArgs);

			return eventArgs.NewFocusedElement;
		}

		/// <summary>
		/// Set the focus on the next focusable control. This method will look
		/// up both the children and ancestor to find the next focusable control 
		/// </summary>
		/// <param name="focusState">Focus state.</param>
		/// <param name="shouldFireFocusedRemoved">Should fire focused removed event?</param>
		internal void SetFocusOnNextFocusableElement(FocusState focusState, bool shouldFireFocusedRemoved)
		{
			bool focusSet = true;

			// Find the next focusable element
			DependencyObject? nextFocusableElement = GetNextFocusableElement();
			if (nextFocusableElement == null)
			{
				// Find the first focusable element from the root
				nextFocusableElement = GetFirstFocusableElement();
			}

			//On Xbox, we want to give them the power dictate where the focus should go when the currently focused element is
			//leaving the tree
			if (shouldFireFocusedRemoved)
			{
				nextFocusableElement = RaiseFocusElementRemovedEvent(nextFocusableElement);
			}

			if (nextFocusableElement != null && FocusableHelper.IsFocusableDO(nextFocusableElement))
			{
				var result = SetFocusedElement(new FocusMovement(nextFocusableElement, FocusNavigationDirection.Next, focusState));
				focusSet = result.WasMoved;
			}

			// When the candidate element has AllowFocusOnInteraction set to false, we should still set focus on this element
			if (nextFocusableElement is FrameworkElement focusableFrameworkElement)
			{
				if (!FocusSelection.ShouldUpdateFocus(focusableFrameworkElement, focusState))
				{
					focusState = FocusState.Programmatic;
				}
			}

			UIElement? nextFocusableUIElement = nextFocusableElement as UIElement;
			if (nextFocusableElement == null || !focusSet)
			{
				ClearFocus();
			}
			else if (nextFocusableUIElement != null)
			{
				bool focusUpdated = false;
				focusUpdated = nextFocusableUIElement.Focus(focusState, false /*animateIfBringIntoView*/);

				if (!focusUpdated)
				{
					// Failed to set focus. We need to clean the focus state
					ClearFocus();
				}
			}
		}

		/// <summary>
		/// Move the focus to the specified navigation direction(Next/Previous).
		/// </summary>
		/// <param name="focusNavigationDirection">Focus navigation direction.</param>
		/// <returns>Was the move successful?</returns>
		internal bool TryMoveFocusInstance(FocusNavigationDirection focusNavigationDirection)
		{
			var xyFocusOptions = XYFocusOptions.Default;
			var result = FindAndSetNextFocus(new FocusMovement(xyFocusOptions, focusNavigationDirection, null));
			return result.WasMoved;
		}

		internal bool FindAndSetNextFocus(FocusNavigationDirection direction)
		{
			var xyFocusOptions = XYFocusOptions.Default;
			var result = FindAndSetNextFocus(new FocusMovement(xyFocusOptions, direction, null));
			return result.WasMoved;
		}

		internal FocusMovementResult FindAndSetNextFocus(FocusMovement movement)
		{
			FocusMovementResult result = new FocusMovementResult();

			var direction = movement.Direction;
			bool queryOnly = true;

			MUX_ASSERT(
				direction == FocusNavigationDirection.Down ||
				direction == FocusNavigationDirection.Left ||
				direction == FocusNavigationDirection.Right ||
				direction == FocusNavigationDirection.Up ||
				direction == FocusNavigationDirection.Next ||
				direction == FocusNavigationDirection.Previous);

			MUX_ASSERT(movement.XYFocusOptions != null);
			var xyFocusOptions = movement.XYFocusOptions!.Value;

			if (_focusLocked)
			{
				throw new InvalidOperationException("Cannot change focus while focus is already changing.");
			}

			if (xyFocusOptions.UpdateManifoldsFromFocusHintRectangle && xyFocusOptions.FocusHintRectangle != null)
			{
				XYFocus.Manifolds manifolds = new XYFocus.Manifolds();
				manifolds.Horizontal = (xyFocusOptions.FocusHintRectangle.Value.Top, xyFocusOptions.FocusHintRectangle.Value.Bottom);
				manifolds.Vertical = (xyFocusOptions.FocusHintRectangle.Value.Left, xyFocusOptions.FocusHintRectangle.Value.Right);
				_xyFocus.SetManifolds(manifolds);
			}

			queryOnly = !_contentRoot.FocusAdapter.ShouldDepartFocus(direction);

			if (FindNextFocus(new FindFocusOptions(direction, queryOnly), xyFocusOptions, movement.Target, false) is DependencyObject nextFocusedElement)
			{
				result = SetFocusedElement(new FocusMovement(nextFocusedElement, movement));

				if (result.WasMoved && !result.WasCanceled && xyFocusOptions.UpdateManifold)
				{
					Rect bounds = xyFocusOptions.FocusHintRectangle != null ? xyFocusOptions.FocusHintRectangle.Value : xyFocusOptions.FocusedElementBounds;
					_xyFocus.UpdateManifolds(direction, bounds, nextFocusedElement!, xyFocusOptions.IgnoreClipping);
				}
			}
			else if (movement.CanDepartFocus)
			{
				bool handled = false;
				FocusObserver.DepartFocus(direction, movement.CorrelationId, ref handled);
			}

			return result;
		}

		internal DependencyObject? FindNextFocus(FocusNavigationDirection direction)
		{
			var options = XYFocusOptions.Default;
			return FindNextFocus(new FindFocusOptions(direction), options);
		}

		internal DependencyObject? FindNextFocus(
			FindFocusOptions findFocusOptions,
			XYFocusOptions xyFocusOptions,
			DependencyObject? component = null,
			bool updateManifolds = true)
		{
			FocusNavigationDirection direction = findFocusOptions.Direction;
			MUX_ASSERT(
				direction == FocusNavigationDirection.Down ||
				direction == FocusNavigationDirection.Left ||
				direction == FocusNavigationDirection.Right ||
				direction == FocusNavigationDirection.Up ||
				direction == FocusNavigationDirection.Next ||
				direction == FocusNavigationDirection.Previous);

			switch (direction)
			{
				case FocusNavigationDirection.Next:
					TraceXYFocusEnteredBegin("Next");
					break;
				case FocusNavigationDirection.Previous:
					TraceXYFocusEnteredBegin("Previous");
					break;
				case FocusNavigationDirection.Up:
					TraceXYFocusEnteredBegin("Up");
					break;
				case FocusNavigationDirection.Down:
					TraceXYFocusEnteredBegin("Down");
					break;
				case FocusNavigationDirection.Left:
					TraceXYFocusEnteredBegin("Left");
					break;
				case FocusNavigationDirection.Right:
					TraceXYFocusEnteredBegin("Right");
					break;
				default:
					TraceXYFocusEnteredBegin("Invalid");
					break;
			}

			DependencyObject? nextFocusedElement = null;
			var engagedControl = xyFocusOptions.ConsiderEngagement ? _engagedControl : null;

			//If we're hosting a component (for e.g. WebView) and focus is moving from within one of our hosted component's children,
			//we interpret the component (WebView) as previously focused element
			var currentFocusedElementOrComponent = (component == null) ? _focusedElement : component;

			if (direction == FocusNavigationDirection.Previous ||
				direction == FocusNavigationDirection.Next ||
				currentFocusedElementOrComponent == null)
			{
				bool bPressedShift = direction == FocusNavigationDirection.Previous;

				// Get the move candidate element according to next/previous navigation direction.
				//TODO:MZ: The original code checks FAILED status
				nextFocusedElement = ProcessTabStopInternal(bPressedShift, findFocusOptions.QueryOnly);
				if (nextFocusedElement == null)
				{
					return null;
				}
			}
			else
			{
				{
					var elementAsUI = currentFocusedElementOrComponent as UIElement;

					if (elementAsUI == null && FocusableHelper.IsFocusableDO(currentFocusedElementOrComponent))
					{
						elementAsUI = GetFocusParent(currentFocusedElementOrComponent) as UIElement;
					}

					xyFocusOptions.FocusedElementBounds = elementAsUI?.GetGlobalBoundsLogical(xyFocusOptions.IgnoreClipping) ?? Rect.Empty;
				}

				nextFocusedElement = _xyFocus.GetNextFocusableElement(
					direction,
					currentFocusedElementOrComponent,
					engagedControl,
					_contentRoot.VisualTree,
					updateManifolds,
					xyFocusOptions);
			}

			TraceXYFocusEnteredEnd();

			return nextFocusedElement;
		}

		/// <summary>
		/// Get the focus target for this element if one exists (may return null, or a child element of given element).
		/// </summary>
		/// <param name="element">UI element.</param>
		/// <returns>UI element.</returns>
		private UIElement? GetFocusTargetDescendant(UIElement element)
		{
			if (element is Control control)
			{
				return control.FocusTargetDescendant;
			}
			return null;
		}

		/// <summary>
		/// Get the element we need to draw the focus rect on.  Returning null will cause the focus rectangle
		/// to not be drawn.
		/// </summary>
		/// <returns>Focus target.</returns>
		private DependencyObject? GetFocusTarget()
		{
			var candidate = _focusedElement;

			if (candidate == null)
			{
				if (_focusRectangleUIElement != null &&
					_focusRectangleUIElement.TryGetTarget(out var focusRectangleUIElement)) //&& focusRectangleUIElement.IsActive())
				{
					candidate = focusRectangleUIElement;
				}
			}

			if (candidate == null)
			{
				return null;
			}

			if (!IsPluginFocused())
			{
				return null;
			}

			// don't draw focus rect for disabled FE(Got focus because of AllowFocusWhenDisabled is set)
			if (candidate is FrameworkElement candidateAsFrameworkElement
				&& candidateAsFrameworkElement.AllowFocusWhenDisabled
				&& candidateAsFrameworkElement is Control { IsEnabled: false })
			{
				return null;
			}

			// Test for if the element doesn't have a child to draw to.
			if (candidate is UIElement candidateAsUIElement
				&& candidateAsUIElement.IsFocused
				&& candidateAsUIElement.IsKeyboardFocused)
			{
				var useSystemFocusVisualsValue = candidateAsUIElement.UseSystemFocusVisuals;

				if (useSystemFocusVisualsValue)
				{
					// Remember the focusTarget, it's different from the focused element in this case
					var focusTargetDescendant = GetFocusTargetDescendant(candidateAsUIElement);
					if (focusTargetDescendant != null)
					{
						return focusTargetDescendant;
					}
					else
					{
						return candidateAsUIElement;
					}
				}
			}

			// For Hyperlinks:
			//  * If we're not in high-visibility mode, CRichTextBlock.HWRenderFocusRects will do the drawing
			if (FocusRectManager.AreHighVisibilityFocusRectsEnabled())
			{
				TextElement? textElement = GetTextElementForFocusRectCandidate();
				if (textElement != null)
				{
					return textElement;
				}
			}

			return null;
		}

		private TextElement? GetTextElementForFocusRectCandidate()
		{
			// Draw focus rect for HyperLink with conditions:
			// - Window focused or we are delegating input (GameBar does this)
			// - not in special case when focus is gained by SIP keyboard input
			// - last input device is keyboard or gamepad
			if (IsPluginFocused())
			{
				var focusedElement = _focusedElement;
				if (focusedElement != null && FocusableHelper.IsFocusableDO(focusedElement))
				{
					var focusManager = VisualTree.GetFocusManagerForElement(focusedElement);
					var inputManager = _contentRoot.InputManager;
					if (inputManager.LastInputDeviceType == InputDeviceType.Keyboard &&
						inputManager.LastInputWasNonFocusNavigationKeyFromSIP())
					{
						return null;
					}
					bool lastInputDeviceWasKeyboardWithProgrammaticFocusState = focusManager != null &&
						(focusManager.GetRealFocusStateForFocusedElement() == FocusState.Programmatic) &&
						(inputManager.LastInputDeviceType == InputDeviceType.Keyboard
							|| inputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote);

					bool currentFocusStateIsKeyboard = focusManager != null &&
						(focusManager.GetRealFocusStateForFocusedElement() == FocusState.Keyboard);

					if (currentFocusStateIsKeyboard || lastInputDeviceWasKeyboardWithProgrammaticFocusState)
					{
						// TODO: don't assume this will alays be a TextElement going forward
						return focusedElement as TextElement;
					}
				}
			}
			return null;
		}

		private void UpdateFocusRect(FocusNavigationDirection focusNavigationDirection, bool cleanOnly)
		{
			_focusTarget = GetFocusTarget();
			_focusRectManager.UpdateFocusRect(_focusedElement, _focusTarget, focusNavigationDirection, cleanOnly);
		}

		//TODO Uno: Send key press events here when keyboard handling is properly implemented
		internal void OnFocusedElementKeyPressed()
		{
			_focusRectManager.OnFocusedElementKeyPressed();
		}

		//TODO Uno: Send key press events here when keyboard handling is properly implemented
		internal void OnFocusedElementKeyReleased()
		{
			_focusRectManager.OnFocusedElementKeyReleased();
		}

		internal void RenderFocusRectForElementIfNeeded(UIElement element, IContentRenderer? renderer)
		{
			if (element == _focusTarget)
			{
				_focusRectManager.RenderFocusRectForElement(element, renderer);
			}
		}

		/// <summary>
		/// Call when properties of focus visual change. Specifically called when Application.FocusVisualKind
		/// changes.
		/// </summary>
		internal void SetFocusVisualDirty()
		{
			DependencyObject? focusedObject = _focusedElement;
			UIElement? focusedElement = focusedObject as UIElement;
			if (focusedElement != null)
			{
				UIElement.NWSetContentDirty(focusedElement, DirtyFlags.Render);
			}
			else
			{
				if (focusedObject != null && FocusableHelper.IsFocusableDO(focusedObject))
				{
					var hyperlinkHost = FocusableHelper.GetContainingFrameworkElementIfFocusable(focusedObject);
					if (hyperlinkHost != null)
					{
						UIElement.NWSetContentDirty(hyperlinkHost, DirtyFlags.Render);
					}
				}
			}
		}

		internal void OnAccessKeyDisplayModeChanged()
		{
			// We should update the caret to visible/collapsed depending on if AK mode is active
			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBox_Available && _focusedElement is TextBox pTextBox)
			{
				//TODO Uno: We don't support caret show/hide in TextBoxView yet
				//pTextBox.GetView().ShowOrHideCaret();
			}
		}

		private ChangingFocusEventRaiseResult RaiseChangingFocusEvent<TArgs>(
			DependencyObject? losingFocusElement,
			DependencyObject? gettingFocusElement,
			FocusState newFocusState,
			FocusNavigationDirection navigationDirection,
			RoutedEvent routedEvent,
			Guid correlationId,
			ChangingFocusEventArgsFactory<TArgs> argsFactory)
			where TArgs : RoutedEventArgs, IChangingFocusEventArgs
		{
			//Locking focus to prevent Focus changes in Getting/Losing focus handlers
			_focusLocked = true;
			try
			{
				var deviceKind = _contentRoot.InputManager.LastFocusInputDeviceKind;

				var args = argsFactory(
					losingFocusElement,
					gettingFocusElement,
					newFocusState,
					navigationDirection,
					deviceKind,
					_currentFocusOperationCancellable,
					correlationId);

				UIElement? changingFocusTarget = null;
				if (routedEvent == UIElement.LosingFocusEvent)
				{
					changingFocusTarget = losingFocusElement as UIElement;
				}
				else if (routedEvent == UIElement.GettingFocusEvent)
				{
					changingFocusTarget = gettingFocusElement as UIElement;
				}

				if (changingFocusTarget != null)
				{
					args.OriginalSource = changingFocusTarget;

					changingFocusTarget.RaiseEvent(routedEvent, args);
				}

				// Always raises FocusManagerGettingFocus/LosingFocus synchronous event.
				// sender - passing null for focus manager
				if (routedEvent == UIElement.LosingFocusEvent)
				{
					LosingFocus?.Invoke(null, (args as LosingFocusEventArgs)!);
				}
				else
				{
					GettingFocus?.Invoke(null, (args as GettingFocusEventArgs)!);
				}

				var finalGettingFocusElement = args.NewFocusedElement;

				// Check if :
				// 1. Focus was redirected
				// 2. The element to which we are redirecting focus is focusable.
				// If this element is not focusable, look for the focusable child.
				// If there is no focusable child, cancel the redirection.
				if ((finalGettingFocusElement != gettingFocusElement) &&
					finalGettingFocusElement != null &&
					!IsFocusable(finalGettingFocusElement))
				{
					DependencyObject? childFocus = null;

					if (navigationDirection == FocusNavigationDirection.Previous)
					{
						childFocus = GetLastFocusableElement(finalGettingFocusElement);
					}
					else
					{
						childFocus = GetFirstFocusableElement(finalGettingFocusElement);
					}

					finalGettingFocusElement = childFocus;
				}

				//We cancel the focus change if:
				//1. The Cancel flag on the args is set
				//2. The focus target is the same as the old focused element
				//3. The focus was redirected to null
				//4. The focus target after a redirection is not focusable and has no focusable children
				if (args.Cancel
					|| (finalGettingFocusElement == losingFocusElement)
					|| (gettingFocusElement != null && (finalGettingFocusElement == null)))
				{
					return new ChangingFocusEventRaiseResult(canceled: true);
				}
				return new ChangingFocusEventRaiseResult(canceled: false, finalGettingFocusElement);
			}
			finally
			{
				_focusLocked = false;
			}
		}

		/// <summary>
		/// Returns true if the focus change is cancelled.
		/// </summary>
		private ChangingFocusEventRaiseResult RaiseAndProcessGettingAndLosingFocusEvents(
			DependencyObject? pOldFocus,
			DependencyObject? pFocusTarget,
			FocusState focusState,
			FocusNavigationDirection focusNavigationDirection,
			bool focusChangeCancellable,
			Guid correlationId)
		{
			DependencyObject? pNewFocus = pFocusTarget;

			_currentFocusOperationCancellable = _currentFocusOperationCancellable && focusChangeCancellable;

			bool focusRedirected = false;

			var losingFocusRaiseResult = RaiseChangingFocusEvent(
				pOldFocus,
				pNewFocus,
				focusState,
				focusNavigationDirection,
				UIElement.LosingFocusEvent,
				correlationId,
				CreateLosingFocusEventArgs);
			if (losingFocusRaiseResult.Canceled)
			{
				return new ChangingFocusEventRaiseResult(canceled: true);
			}
			var pFinalNewFocus = losingFocusRaiseResult.FinalGettingFocusElement;

			if (pNewFocus != pFinalNewFocus)
			{
				focusRedirected = true;
			}

			if (!focusRedirected)
			{
				pFinalNewFocus = null;

				var gettingFocusRaiseResult = RaiseChangingFocusEvent(
					pOldFocus,
					pNewFocus,
					focusState,
					focusNavigationDirection,
					UIElement.GettingFocusEvent,
					correlationId,
					CreateGettingFocusEventArgs);
				if (gettingFocusRaiseResult.Canceled)
				{
					return new ChangingFocusEventRaiseResult(canceled: true);
				}
				pFinalNewFocus = gettingFocusRaiseResult.FinalGettingFocusElement;

				if (pNewFocus != pFinalNewFocus)
				{
					focusRedirected = true;
				}
			}

			if (focusRedirected)
			{
				if (!ShouldUpdateFocus(pFinalNewFocus, focusState))
				{
					return new ChangingFocusEventRaiseResult(canceled: true);
				}

				var redirectedRaiseResult = RaiseAndProcessGettingAndLosingFocusEvents(
					pOldFocus,
					pFinalNewFocus,
					focusState,
					focusNavigationDirection,
					focusChangeCancellable,
					correlationId);
				if (redirectedRaiseResult.Canceled)
				{
					return new ChangingFocusEventRaiseResult(canceled: true);
				}
				pFinalNewFocus = redirectedRaiseResult.FinalGettingFocusElement;
				pFocusTarget = pFinalNewFocus;
			}
			return new ChangingFocusEventRaiseResult(canceled: false, pFocusTarget);
		}

		private bool IsValidTabStopSearchCandidate(DependencyObject? element)
		{
			bool isValid = IsPotentialTabStop(element);

			// If IsPotentialTabStop is false, we could have a UIElement that has TabFocusNavigation Set. If it does, then
			// it is still a valid search candidate
			if (isValid == false)
			{
				// We only care if we have a UIElement has TabFocusNavigation set
				isValid = element is UIElement uiElement && uiElement.IsTabNavigationSet;
			}

			return isValid;
		}

		internal void RaiseNoFocusCandidateFoundEvent(FocusNavigationDirection navigationDirection)
		{
			var noFocusCandidateFoundTarget = _focusedElement as UIElement;
			var focusedElementAsTextElement = _focusedElement as TextElement;

			switch (navigationDirection)
			{
				case FocusNavigationDirection.Next:
					TraceXYFocusNotFoundInfo(_focusedElement, "Next");
					break;
				case FocusNavigationDirection.Previous:
					TraceXYFocusNotFoundInfo(_focusedElement, "Previous");
					break;
				case FocusNavigationDirection.Up:
					TraceXYFocusNotFoundInfo(_focusedElement, "Up");
					break;
				case FocusNavigationDirection.Down:
					TraceXYFocusNotFoundInfo(_focusedElement, "Down");
					break;
				case FocusNavigationDirection.Left:
					TraceXYFocusNotFoundInfo(_focusedElement, "Left");
					break;
				case FocusNavigationDirection.Right:
					TraceXYFocusNotFoundInfo(_focusedElement, "Right");
					break;
				default:
					TraceXYFocusNotFoundInfo(_focusedElement, "Invalid");
					break;
			}

			if (focusedElementAsTextElement != null)
			{
				//We get the containing framework element for all text elements till Bug 10065690 is resolved.
				noFocusCandidateFoundTarget = focusedElementAsTextElement.GetContainingFrameworkElement();
			}

			Guid correlationId = Guid.NewGuid();
			bool handled = false;
			FocusObserver.DepartFocus(navigationDirection, correlationId, ref handled);

			//We should never raise on a null source
			if (noFocusCandidateFoundTarget != null)
			{
				FocusInputDeviceKind deviceKind = _contentRoot.InputManager.LastFocusInputDeviceKind;

				var eventArgs = new NoFocusCandidateFoundEventArgs(navigationDirection, deviceKind);

				//The source should be the focused element
				eventArgs.OriginalSource = _focusedElement;

				noFocusCandidateFoundTarget.RaiseEvent(UIElement.NoFocusCandidateFoundEvent, eventArgs);
			}
		}

		private void SetPluginFocusStatus(bool pluginFocused)
		{
			_pluginFocused = pluginFocused;
		}

		// When setting focus to a new element and we don't have a FocusState to use, this function helps us
		// decide a good focus state based on a recently-used input device type.  This function assumes focus is
		// actually being set, so it won't return "Unfocued" as a FocusState.
		/*static*/
		internal FocusState GetFocusStateFromInputDeviceType(InputDeviceType inputDeviceType)
		{
			switch (inputDeviceType)
			{
				case InputDeviceType.None:
					return FocusState.Programmatic;
				case InputDeviceType.Mouse:
				case InputDeviceType.Touch:
				case InputDeviceType.Pen:
					return FocusState.Pointer;
				case InputDeviceType.Keyboard:
				case InputDeviceType.GamepadOrRemote:
					return FocusState.Keyboard;
			}

			// Unexpected inputDeviceType
			MUX_ASSERT(false);
			return FocusState.Programmatic;
		}

		internal bool TrySetAsyncOperation(FocusAsyncOperation asyncOperation)
		{
			if (_asyncOperation != null)
			{
				return false;
			}

			_asyncOperation = asyncOperation;
			return true;
		}

		private void CancelCurrentAsyncOperation(FocusMovementResult result)
		{
			if (_asyncOperation != null)
			{
				_asyncOperation.CoreSetResults(result);
				_asyncOperation.CoreFireCompletion();
				//_asyncOperation.CoreReleaseRef();

				_asyncOperation = null;
			}
		}

		private FocusState CoerceFocusState(FocusState focusState)
		{

			InputDeviceType lastInputDeviceType = _contentRoot.InputManager.LastInputDeviceType;

			// Set the new focus state with the last input device type
			// if the focus state set as programmatic.
			if (focusState == FocusState.Programmatic)
			{
				/*
				* On Programmatic focus, we look at the last input device to decide what could be the focus state.
				* Depending on focus state we decide whether to show a focus rectangle or not.
				* If focus state is keyboard we always show a rectangle.
				* Focus state will be set to Keyboard iff last input device was
				* a. GamepadOrRemote
				* With GamepadOrRemote, we always show focus rectangle on Xbox unless developer explicitly wants us not to do so,
				* in that case it will be set to FocusState.Pointer. Even, if SIP is open and input is any key
				* we will display focus rectangle if user is using GamepadOrRemote to type on SIP
				* b. Hardware Keyboard
				* c. Software keyboard (SIP) and focus movement keys (Tab or Arrow keys)
				* Otherwise we set focus state to default FocusState.Pointer state. Setting focus state to Pointer
				* will make sure that focus rectangle will not be drawn
				*/
				switch (lastInputDeviceType)
				{
					case InputDeviceType.Keyboard:
						// In case of non focus navigation keys from SIP, we do not want to display
						// focus rectangle, hence setting new focus state as a Pointer.
						if (_contentRoot.InputManager.LastInputWasNonFocusNavigationKeyFromSIP())
						{
							return FocusState.Pointer;
						}
						/*
						* Intentional fall through in the case where input is
						* a. any one of focus navigation key(Tab or an arrow key) from SIP or
						* b. any key from actual hardware keyboard
						*/
						return FocusState.Keyboard;
					case InputDeviceType.GamepadOrRemote:
						return FocusState.Keyboard;
					default:
						// For all other rest of the cases, setting focus state to Pointer
						// will make sure that focus rectangle will not be drawn
						return FocusState.Pointer;
				}
			}

			return focusState;
		}

		internal void SetWindowFocus(bool isFocused, bool isShiftDown)
		{
			Guid correlationId = Guid.NewGuid();

			SetPluginFocusStatus(isFocused);

			var focusedElement = _focusedElement;

			// We cache the value of UISettings.AnimationsEnabled to avoid the expensive call
			// to get it all the time. Unfortunately there is no notification mechanism yet when it
			// changes. We work around that by re-evaluating it only when the app window focus changes.
			// This is usually the case because in order to change the setting the user needs to switch
			// to the settings app and back.
			//_coreService.SetShouldReevaluateIsAnimationEnabled(true);

			if (focusedElement == null && isFocused)
			{
				// Find the first focusable element from the root
				// and set it as the new focused element.
				focusedElement = GetFirstFocusableElementFromRoot(isShiftDown /*bReverse*/);
				if (focusedElement != null)
				{
					FocusState initialFocusState = FocusState.Programmatic;
					if (_contentRoot.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote)
					{
						initialFocusState = FocusState.Keyboard;
					}

					InitialFocus = true;
					//If an error is propagated to the Input Manager here, we are in an invalid state.

					var result = SetFocusedElement(new FocusMovement(focusedElement, FocusNavigationDirection.None, initialFocusState));

					// Note that because of focus redirection, _focusedElement can be different than focusedElement
					// For RS5, we are NOT going to fix this, so we will leave the next line commented.
					// focusedElement = _focusedElement;

					return;
				}
			}

			if (focusedElement == null)
			{
				focusedElement = _contentRoot.VisualTree.PublicRootVisual;
			}

			if (focusedElement != null)
			{
				// Create the DO that represents the event args
				var args = new RoutedEventArgs();

				// Call raise event AND reset handled to false if this is not ours
				if (isFocused)
				{
					DependencyObject? focusTarget = focusedElement;
					bool wasFocusChangedDuringSyncEvents = false;

					using var focusLockGuard = new UnsafeFocusLockOverrideGuard(this);
					DependencyObject? currentlyFocusedElement = _focusedElement;

					// Raise changing focus events. We cannot cancel or redirect focus here.
					// RaiseAndProcessGettingAndLosingFocusEvents returns true if a focus change has been cancelled.
					var gettingLosingRaiseResult = RaiseAndProcessGettingAndLosingFocusEvents(
						null /*pOldFocus*/,
						focusTarget,
						GetRealFocusStateForFocusedElement(),
						FocusNavigationDirection.None,
						false /*focusChangeCancellable*/,
						correlationId);
					focusTarget = gettingLosingRaiseResult.FinalGettingFocusElement;

					var focusChangeCancelled = gettingLosingRaiseResult.Canceled;

					MUX_ASSERT(!focusChangeCancelled);
					_currentFocusOperationCancellable = true;

					// The focused element could have changed after raising the getting/losing events
					if (currentlyFocusedElement != _focusedElement)
					{
						wasFocusChangedDuringSyncEvents = true;
						focusedElement = _focusedElement;
					}

					// Set the Source value on the routed event args
					args.OriginalSource = focusedElement;

					// We only want to set the focused element as dirty if it is a uielement
					if (focusedElement is UIElement uiElement)
					{
						UIElement.NWSetContentDirty(uiElement, DirtyFlags.Render);
					}

					// If focus changed, then the GotFocus event has already been raised for the focused element
					if (wasFocusChangedDuringSyncEvents == false)
					{
						// TODO Uno: We raise the events using dispatcher queue, which is a bit different from MUX
						// which does everything via EventManager
						_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
						{
							// In the case of hyperlink raise both Hyperlink_GotFocus and UIElement_GotFocus. UIElement_GotFocus
							// is used internally for to decide where focus rects should be rendered.
							if (focusedElement is Hyperlink hyperlink)
							{
								hyperlink.OnGotFocus(args);
							}

							// Raise the current focused element
							(focusedElement as UIElement)?.RaiseEvent(UIElement.GotFocusEvent, args);

							// Raise the FocusManagerLostFocus event to the focus manager asynchronously
							LostFocus?.Invoke(null, new FocusManagerLostFocusEventArgs(null, correlationId));

							// Raise the FocusManagerGotFocus event to the focus manager asynchronously
							GotFocus?.Invoke(null, new FocusManagerGotFocusEventArgs(focusedElement, correlationId));
						});
					}

					// Fire focus changed event for UIAutomation
					FireAutomationFocusChanged();
				}
				else
				{
					using var focusLockGuard = new UnsafeFocusLockOverrideGuard(this);
					DependencyObject? currentlyFocusedElement = _focusedElement;

					// Raise changing focus events. We cannot cancel or redirect focus here.
					// RaiseAndProcessGettingAndLosingFocusEvents returns true if a focus change has been cancelled.
					var gettingLosingRaiseResult = RaiseAndProcessGettingAndLosingFocusEvents(
						focusedElement,
						null /*focusTarget*/,
						FocusState.Unfocused,
						FocusNavigationDirection.None,
						false /*focusChangeCancellable*/,
						correlationId);

					bool focusChangeCancelled = gettingLosingRaiseResult.Canceled;

					MUX_ASSERT(!focusChangeCancelled);
					_currentFocusOperationCancellable = true;

					// The focused element could have changed after raising the getting/losing events
					if (currentlyFocusedElement != _focusedElement)
					{
						focusedElement = _focusedElement;
					}

					// Set the Source value on the routed event args
					args.OriginalSource = focusedElement;

					//Be wary of the fact that a got focus event also updates the visual. In this scenario, we preceed a lost focus event, so
					//we don't have to worry about visuals being updated.

					//Manually go and update the visuals. Raising a got focus here instead of calling to UpdateVisualState is risky since it is async
					//and we cannot guarantee when it will complete.
					//TODO: How will this affect third party apps?
					if (focusedElement is Control focusedElementAsControl)
					{
						//We want to store the current focus state. We will simulate an unfocused state so that the visuals will change,
						//but we will not actually change the focusedState.
						FocusState focusedState = focusedElementAsControl.FocusState;
						focusedElementAsControl.UpdateFocusState(FocusState.Unfocused);

						(focusedElement as Control)?.UpdateVisualState(useTransitions: true);

						//Restore the original focus state. We lie and say that the focused state is unfocused, although that isn't
						//the case. This is done so that if this element is being inspected before a window activated has been received,
						//we can ensure that the behavior doesn't change.
						focusedElementAsControl.UpdateFocusState(focusedState);
					}

					// TODO Uno: We raise the events using dispatcher queue, which is a bit different from MUX
					// which does everything via EventManager
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						// In the case of hyperlink raise both Hyperlink_LostFocus and UIElement_LostFocus. UIElement_LostFocus
						// is used internally for to decide where focus rects should be rendered.
						if (focusedElement is Hyperlink hyperlink)
						{
							hyperlink.OnLostFocus(args);
						}

						// Raise the current focused element
						(focusedElement as UIElement)?.RaiseEvent(UIElement.LostFocusEvent, args);

						// Raise the FocusManagerLostFocus event to the focus manager asynchronously
						LostFocus?.Invoke(null, new FocusManagerLostFocusEventArgs(focusedElement, correlationId));

						// Raise the FocusManagerGotFocus event to the focus manager asynchronously
						GotFocus?.Invoke(null, new FocusManagerGotFocusEventArgs(null, correlationId));
					});
				}
			}
		}

		/// <summary>
		/// Checks whether the focused element is in a popup.
		/// </summary>
		/// <returns>True if in popup.</returns>
		private bool IsFocusedElementInPopup() => _focusedElement != null && GetRootOfPopupSubTree(_focusedElement) != null;

		internal void SetFocusRectangleUIElement(UIElement? newFocusRectangle)
		{
			if (newFocusRectangle == null)
			{
				_focusRectangleUIElement = null;
			}
			else
			{
				_focusRectangleUIElement = new WeakReference<UIElement>(newFocusRectangle);
			}
		}

		/// <summary>
		/// Checks whether plugin is focused.
		/// </summary>
		/// <returns>True if focused.</returns>
		internal bool IsPluginFocused() => _pluginFocused;

		internal FocusState GetRealFocusStateForFocusedElement() => _realFocusStateForFocusedElement;

		private void TraceXYFocusEnteredBegin(string direction)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XY focus entered begin for direction {direction}");
			}
		}

		private void TraceXYFocusEnteredEnd()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XY focus entered end");
			}
		}

		private void LogTraceTraceUpdateFocusBegin()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("Update focus begin");
			}
		}

		private void TraceXYFocusNotFoundInfo(DependencyObject? focusedElement, string direction)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Did not find XY focus from {focusedElement} in {direction}");
			}
		}

		private void TraceUpdateFocusEnd(DependencyObject? focusedElement)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Update focus ended for {focusedElement}");
			}
		}

		private static LosingFocusEventArgs CreateLosingFocusEventArgs(
			DependencyObject? oldFocusedElement,
			DependencyObject? newFocusedElement,
			FocusState focusState,
			FocusNavigationDirection direction,
			FocusInputDeviceKind inputDevice,
			bool canCancelFocus,
			Guid correlationId) =>
			new LosingFocusEventArgs(
				oldFocusedElement,
				newFocusedElement,
				focusState,
				direction,
				inputDevice,
				canCancelFocus,
				correlationId);

		private static GettingFocusEventArgs CreateGettingFocusEventArgs(
			DependencyObject? oldFocusedElement,
			DependencyObject? newFocusedElement,
			FocusState focusState,
			FocusNavigationDirection direction,
			FocusInputDeviceKind inputDevice,
			bool canCancelFocus,
			Guid correlationId) =>
			new GettingFocusEventArgs(
				oldFocusedElement,
				newFocusedElement,
				focusState,
				direction,
				inputDevice,
				canCancelFocus,
				correlationId);

		DependencyObject? IFocusManager.FindNextFocus(FindFocusOptions findFocusOptions, XYFocusOptions xyFocusOptions, DependencyObject? component, bool updateManifolds) =>
			FindNextFocus(findFocusOptions, xyFocusOptions, component, updateManifolds);

		FocusMovementResult IFocusManager.SetFocusedElement(FocusMovement movement) => SetFocusedElement(movement);
	}
}
