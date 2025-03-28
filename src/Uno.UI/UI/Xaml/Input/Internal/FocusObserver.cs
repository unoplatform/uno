// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusObserver.h, FocusObserver.cpp

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Core;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using static Uno.UI.Xaml.Input.FocusConversionFunctions;

namespace Uno.UI.Xaml.Input;

internal class FocusObserver
{
	private readonly ContentRoot _contentRoot;

	private FocusController? _focusController;

	private XamlSourceFocusNavigationRequest? _currentInteraction;

	internal FocusObserver(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));

		// TODO Uno: Move this initialization somewhere else based on WinUI sources.
		Init(new FocusController(new InputFocusController()));
	}

	internal FocusController FocusController => _focusController!;

	internal void Init(FocusController focusController) => _focusController = focusController;

	private Rect GetOriginToComponent(DependencyObject? pOldFocusedElement)
	{
		Rect focusedElementBounds = new Rect();

		//Transform the bounding rect of currently focused element to Component's co-ordinate space
		if (pOldFocusedElement != null)
		{
			if (FocusableHelper.GetIFocusableForDO(pOldFocusedElement) is { } focusable)
			{
				//TODO Uno: Implement support for HyperLink focus
				//DependencyObject depObj = focusable.GetDOForIFocusable();
				//IFC_RETURN(do_pointer_cast<CTextElement>(depObj).GetContainingFrameworkElement().GetGlobalBounds(&focusedElementBounds, true));
			}
			else
			{
				UIElement? pUIElement = pOldFocusedElement as UIElement;
				if (pUIElement != null)
				{
					focusedElementBounds = pUIElement.GetGlobalBounds(true);
				}
			}
		}

		var origin = new Rect();
		origin.X = focusedElementBounds.Left;
		origin.Y = focusedElementBounds.Top;
		origin.Width = focusedElementBounds.Right - focusedElementBounds.Left;
		origin.Height = focusedElementBounds.Bottom - focusedElementBounds.Top;

		return origin;
	}

	private Rect GetOriginFromInteraction()
	{
		var rectRB = new Rect();

		if (_currentInteraction != null)
		{
			var origin = _currentInteraction.HintRect;

			rectRB = new Rect(
				new Point(origin.X, origin.Y),
				new Point(origin.X + origin.Width, origin.Y + origin.Height));
		}

		return rectRB;
	}

	private FocusMovementResult NavigateFocusXY(
		DependencyObject pComponent,
		FocusNavigationDirection direction,
		Rect origin)
	{
		Rect rect = origin;
		XYFocusOptions xyFocusOptions = new XYFocusOptions();
		xyFocusOptions.FocusHintRectangle = rect;

		// Any value of the manifold is meaning less when Navigating or Departing into or from a component.
		// The current manifold needs to be updated from the Origin given.
		xyFocusOptions.UpdateManifoldsFromFocusHintRectangle = true;

		FocusMovement movement = new FocusMovement(xyFocusOptions, direction, pComponent);

		// We dont handle cancellation of a focus request from a host:
		//   We could support this by calling DepartFocus from the component
		//   if the component returns result.WasCanceled()
		//   We choose to not support it.
		movement.CanCancel = false;

		// Do not allow DepartFocus to be called, CoreWindowsFocusAdapter will handle it.
		movement.CanDepartFocus = false;

		movement.ShouldCompleteAsyncOperation = true;

		return _contentRoot.FocusManager.FindAndSetNextFocus(movement);
	}

	private Rect CalculateNewOrigin(FocusNavigationDirection direction, Rect currentOrigin)
	{
		var pFocusedElement = _contentRoot.VisualTree.ActiveRootVisual;
		var windowBounds = GetOriginToComponent(pFocusedElement);

		var newOrigin = currentOrigin;
		switch (direction)
		{
			case FocusNavigationDirection.Left:
			case FocusNavigationDirection.Right:
				newOrigin.X = windowBounds.X;
				newOrigin.Width = windowBounds.Width;
				break;
			case FocusNavigationDirection.Up:
			case FocusNavigationDirection.Down:
				newOrigin.Y = windowBounds.Y;
				newOrigin.Height = windowBounds.Height;
				break;
		}

		return newOrigin;
	}

	internal bool ProcessNavigateFocusRequest(XamlSourceFocusNavigationRequest focusNavigationRequest)
	{
		var pHandled = false;

		UpdateCurrentInteraction(focusNavigationRequest);

		var reason = focusNavigationRequest.Reason;

		DependencyObject? pRoot = null;

		pRoot = _contentRoot.Type switch
		{
			ContentRootType.XamlIslandRoot => _contentRoot.VisualTree.RootScrollViewer ?? _contentRoot.VisualTree.ActiveRootVisual,
			_ => _contentRoot.VisualTree.ActiveRootVisual,
		};

		FocusNavigationDirection direction = GetFocusNavigationDirectionFromReason(reason);

		if (reason == XamlSourceFocusNavigationReason.First ||
			reason == XamlSourceFocusNavigationReason.Last)
		{
			_contentRoot.InputManager.LastInputDeviceType = GetInputDeviceTypeFromDirection(direction);
			bool bReverse = (reason == XamlSourceFocusNavigationReason.Last);

			if (pRoot == null)
			{
				// No content has been loaded, bail out
				return false;
			}

			DependencyObject? pCandidateElement = null;
			if (bReverse)
			{
				pCandidateElement = _contentRoot.FocusManager.GetLastFocusableElement(pRoot);
			}
			else
			{
				pCandidateElement = _contentRoot.FocusManager.GetFirstFocusableElement(pRoot);
			}

			bool retryWithPopupRoot = (pCandidateElement == null);
			if (retryWithPopupRoot)
			{
				var popupRoot = _contentRoot.VisualTree.PopupRoot;
				if (popupRoot != null)
				{
					if (bReverse)
					{
						pCandidateElement = _contentRoot.FocusManager.GetLastFocusableElement(popupRoot);
					}
					else
					{
						pCandidateElement = _contentRoot.FocusManager.GetFirstFocusableElement(popupRoot);
					}
				}
			}

			if (pCandidateElement != null)
			{
				// When we move focus into XAML with tab, we first call ClearFocus to mimic desktop behavior.
				// On desktop, we call ClearFocus during a tab cycle in CJupiterWindow.AcceleratorKeyActivated, but when
				// running as a component (e.g. in c-shell) the way XAML loses focus when the user tabs away is different
				// (we call DepartFocus) and if we call ClearFocus at the same time as we do on desktop, we'll end up
				// firing the LostFocus for the previously-focused before calling GettingFocus for the newly focused element.
				// So instead, we call ClearFocus as we tab *in* to XAML content.  This preserves the focus event ordering on
				// tab cycles.
				_contentRoot.FocusManager.ClearFocus();

				FocusMovement movement = new FocusMovement(
					pCandidateElement,
					bReverse ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next,
					FocusState.Keyboard);

				// We dont handle cancellation of a focus request from a host:
				//   We could support this by calling DepartFocus from the component
				//   if the component returns result.WasCanceled()
				//   We choose to not support it.
				movement.CanCancel = false;

				FocusMovementResult result = _contentRoot.FocusManager.SetFocusedElement(movement);
				if (result.WasMoved)
				{
					pHandled = StopInteraction();
				}
			}
			else
			{
				Guid correlationId = focusNavigationRequest.CorrelationId;

				DepartFocus(direction, correlationId, ref pHandled);
			}
		}
		else if (reason == XamlSourceFocusNavigationReason.Restore ||
				 reason == XamlSourceFocusNavigationReason.Programmatic)
		{
			var pFocusedElement = _contentRoot.FocusManager.FocusedElement;
			if (pFocusedElement != null)
			{
				pHandled = StopInteraction();
			}
			else if (pFocusedElement == null && reason == XamlSourceFocusNavigationReason.Programmatic)
			{
				if (pRoot == null)
				{
					// No content has been loaded, bail out
					return false;
				}

				var pCandidateElement = _contentRoot.FocusManager.GetFirstFocusableElement(pRoot);
				if (pCandidateElement == null)
				{
					pCandidateElement = pRoot;
				}
				if (pCandidateElement != null)
				{
					FocusMovement movement = new FocusMovement(pCandidateElement, FocusNavigationDirection.None, FocusState.Programmatic);

					// We dont handle cancellation of a focus request from a host:
					//   We could support this by calling DepartFocus from the component
					//   if the component returns result.WasCanceled()
					//   We choose to not support it.
					movement.CanCancel = false;

					FocusMovementResult result = _contentRoot.FocusManager.SetFocusedElement(movement);
					if (result.WasMoved)
					{
						pHandled = StopInteraction();
					}
				}
			}
		}
		else if (reason == XamlSourceFocusNavigationReason.Left ||
				 reason == XamlSourceFocusNavigationReason.Right ||
				 reason == XamlSourceFocusNavigationReason.Up ||
				 reason == XamlSourceFocusNavigationReason.Down)
		{
			if (pRoot == null)
			{
				// No content has been loaded, bail out
				return false;
			}

			_contentRoot.InputManager.LastInputDeviceType = GetInputDeviceTypeFromDirection(direction);

			Rect rect = GetOriginFromInteraction();

			FocusMovementResult result = NavigateFocusXY(pRoot, direction, rect);
			//IFC_RETURN(result.GetHResult());
			if (result.WasMoved)
			{
				pHandled = StopInteraction();
			}
			else
			{
				//
				// If we could not find a target via XY then we need to depart focus again
				// But this time from an orgin inside of the component
				//
				//                             ┌────────────────────────────────┐
				//                             │        CoreWindow              │
				//                             │                                │
				//                             │                                │
				//   ┌──────────┐ Direction    ├────────────────────────────────┤
				//   │  origin  │ ─────────>   │      New Origin:               │ Depart Focus from new origin
				//   │          │              │ Calculated as the intersertion │  ─────────>
				//   │          │              │ from the direction             │
				//   └──────────┘              ├────────────────────────────────┤
				//                             │                                │
				//                             │                                │
				//                             │                                │
				//                             └────────────────────────────────┘

				Rect origin = focusNavigationRequest.HintRect;
				Rect newOrigin = CalculateNewOrigin(direction, origin);
				Guid correlationId = focusNavigationRequest.CorrelationId;

				DepartFocus(direction, newOrigin, correlationId, ref pHandled);
			}
		}

		return pHandled;
	}

	internal void DepartFocus(
		FocusNavigationDirection direction,
		Guid correlationId,
		ref bool handled)
	{
		var pFocusedElement = _contentRoot.FocusManager.FocusedElement;
		var origin = GetOriginToComponent(pFocusedElement);

		DepartFocus(direction, origin, correlationId, ref handled);
	}

	private void DepartFocus(
		FocusNavigationDirection direction,
		Rect origin,
		Guid correlationId,
		ref bool handled)
	{
		if (handled || _focusController is null)
		{
			return;
		}

		var reason = GetFocusNavigationReasonFromDirection(direction);
		if (reason == null)
		{
			// Do nothing if we dont support this navigation
			return;
		}

		StartInteraction(reason.Value, origin, correlationId);

		_focusController.DepartFocus(_currentInteraction!);
		handled = true;
	}

	private void StartInteraction(
		XamlSourceFocusNavigationReason reason,
		Rect origin,
		Guid correlationId)
	{
		var request = new XamlSourceFocusNavigationRequest(reason, origin, correlationId);
		_currentInteraction = request;
	}

	private bool StopInteraction()
	{
		MUX_ASSERT(_currentInteraction != null);
		_currentInteraction = null;
		return true;
	}

	protected virtual CoreWindowActivationMode GetActivationMode() => CoreWindowActivationMode.None;

	private void UpdateCurrentInteraction(XamlSourceFocusNavigationRequest? pRequest)
	{
		_currentInteraction = pRequest;
	}
}
