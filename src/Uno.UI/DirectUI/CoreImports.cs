// TODO:MZ: Verify method by method

#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace DirectUI;

internal class Pinvokes
{
	internal static void PopupRoot_CloseTopmostPopup(
		DependencyObject popupRootDo,
		bool bLightDismissOnly)
	{
		if (popupRootDo is not PopupRoot popupRoot)
		{
			throw new ArgumentException("PopupRoot not passed in.", nameof(popupRootDo));
		}


		popupRoot.CloseTopmostPopup(
			FocusState.Programmatic,
			bLightDismissOnly ?
				PopupRoot.PopupFilter.LightDismissOnly : PopupRoot.PopupFilter.All);
	}

	internal static void Popup_Close(DependencyObject popupDo)
	{
		if (popupDo is not Popup popup)
		{
			throw new ArgumentException("Popup not passed in.", nameof(popupDo));
		}

		popup.SetValue(Popup.IsOpenProperty, false);
	}

	internal static bool Popup_GetIsLightDismiss(DependencyObject popupDo)
	{
		if (popupDo is Popup popup)
		{
			return popup.m_fIsLightDismiss;
		}

		return false;
	}

	internal static FocusState Popup_GetSavedFocusState(DependencyObject popupDo)
	{

		if (popupDo is Popup popup)
		{
			return popup.CGetSavedFocusState();
		}

		return FocusState.Unfocused;
	}

	XCPEXPORT
	Popup_SetFocusStateAfterClosing(
		_In_ CDependencyObject* pPopupCDO,
		_In_ DirectUI::FocusState focusState)
	{
		static_cast<CPopup*>(pPopupCDO)->SetFocusStateAfterClosing(focusState);
		RRETURN(S_OK);
	}

	XCPEXPORT
	Popup_SetShouldTakeFocus(
		_In_ CDependencyObject* pPopupCDO,
		_In_ XBOOL shouldTakeFocus)
	{
		static_cast<CPopup*>(pPopupCDO)->SetShouldTakeFocus(shouldTakeFocus);
		RRETURN(S_OK);
	}

	internal static DependencyObject? FocusManager_GetFirstFocusableElement(DependencyObject searchStart)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(searchStart);
		if (focusManager is null)
		{
			throw new InvalidOperationException("No focus manager found for element.");
		}
		return focusManager.GetFirstFocusableElement(searchStart);
	}

	internal static DependencyObject? FocusManager_GetLastFocusableElement(DependencyObject searchStart)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(searchStart);
		if (focusManager is null)
		{
			throw new InvalidOperationException("No focus manager found for element.");
		}
		var lastFocusableElement = focusManager.GetLastFocusableElement(searchStart);
		if (lastFocusableElement == null && focusManager.IsFocusable(searchStart))
		{
			// Focus Manager will return null for GetLastFocusableElement if the element itself is focusable but there
			// are no focusable elements under it. So in this case we return the element itself.
			lastFocusableElement = searchStart;
		}

		return lastFocusableElement;
	}

	internal static bool FocusManager_CanHaveFocusableChildren(DependencyObject element) =>
		FocusProperties.CanHaveFocusableChildren(element);

	internal static bool UIElement_IsFocusable(UIElement element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		return element.IsFocusable;
	}
}
