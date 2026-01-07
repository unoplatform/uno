// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.cpp, PopupRoot_Partial.cpp, PopupRoot.cpp

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot
{
	internal enum PopupFilter
	{
		LightDismissOnly,
		LightDismissOrFlyout,
		All,
	}

	[NotImplemented]
	public static Popup? GetOpenPopupForElement(UIElement uiElement)
	{
		// TODO Uno: Implement for proper focus support.
		return null;

		// Early exit when there are no open popups
		//var popupRoot = VisualTree.GetPopupRootForElement(uiElement);
		//if (popupRoot == null || !popupRoot.HasOpenOrUnloadingPopups())
		//{
		//	return null;
		//}

		//// If the element is in a popup, GetRootOfPopupSubTree returns the popup's subtree root.
		//var pPopupSubTreeNoRef = uiElement.GetRootOfPopupSubTree() as UIElement;
		//if (pPopupSubTreeNoRef != null)
		//{
		//	// The logical parent of the subtree root is the popup
		//	var popup = pPopupSubTreeNoRef.GetLogicalParent() as Popup;
		//	if (popup != null && popup.IsOpen && !popup.IsUnloading)
		//	{
		//		return popup;
		//	}
		//}
		//return null;
	}

	internal Popup? GetTopmostPopup(PopupFilter filter)
	{
		if (_openPopups is { })
		{
			foreach (var popupRef in _openPopups)
			{
				if (popupRef.Target is not Popup popup)
				{
					continue;
				}

				// there be should be a check for !popup.IsUnloading
				{
					if (filter == PopupFilter.All || (filter == PopupFilter.LightDismissOrFlyout && popup.AssociatedFlyout is { }) || popup.IsLightDismissEnabled)
					{
						return popup;
					}
				}
			}
		}

		return null;
	}

	[NotImplemented]
	internal void ClearWasOpenedDuringEngagementOnAllOpenPopups()
	{
		// TODO Uno: Implement
		return;
		//if (!_pOpenPopups) { return; }

		//CXcpList<CPopup>::XCPListNode* node = nullptr;
		//node = m_pOpenPopups->GetHead();

		//// Includes unloading popups too
		//while (node)
		//{
		//	node->m_pData->SetOpenedDuringEngagement(false);
		//	node = node->m_pNext;
		//}
	}

	[NotImplemented]
	internal static IList<DependencyObject> GetPopupChildrenOpenedDuringEngagement(DependencyObject element)
	{
		var popupRoot = VisualTree.GetPopupRootForElement(element);

		if (popupRoot == null ||
			popupRoot.GetOpenPopups() is var openedPopups)
		{
			return new List<DependencyObject>();
		}

		// TODO Uno:Implement
		return Array.Empty<DependencyObject>();
		//var popupChildrenDuringEngagement = new List<DependencyObject>(openedPopups.Length);

		//for (int index = 0; index < openPopupsCount; index++)
		//{
		//	Popup popup = openedPopups[index];
		//	if (popup != null && popup.WasOpenedDuringEngagement())
		//	{
		//		popupChildrenDuringEngagement.Add(popup.Child);
		//	}
		//}

		//delete[] openedPopups;

		//return popupChildrenDuringEngagement;
	}

	/// <summary>
	/// Closes the topmost popup matching the specified filter.
	/// </summary>
	/// <param name="focusStateAfterClosing">Focus state to apply after closing.</param>
	/// <param name="filter">Filter to determine which popups can be closed.</param>
	/// <returns>True if a popup was closed.</returns>
	internal bool CloseTopmostPopup(FocusState focusStateAfterClosing, PopupFilter filter)
	{
		CloseTopmostPopup(focusStateAfterClosing, filter, out var didCloseAPopup);
		return didCloseAPopup;
	}

	private void CloseTopmostPopup(FocusState focusStateAfterClosing, PopupFilter filter, out bool didCloseAPopup)
	{
		didCloseAPopup = false;

		var popupNoRef = GetTopmostPopup(filter);
		if (popupNoRef is { })
		{
			// Give the popup an opportunity to cancel closing.
			var cancel = false;
			popupNoRef.OnClosing(ref cancel);
			if (cancel)
			{
				return;
			}

			// Take a ref for the duration of the SetValue call below, which could otherwise release
			// the last reference to the popup in the middle of the call, where the "this" pointer is
			// still expected to be valid.
			// xref_ptr<CPopup> popup(popupNoRef);
			var popup = popupNoRef;

			// Uno Todo
			// popup.m_focusStateAfterClosing = focusStateAfterClosing;
			popup.IsOpen = false;
			didCloseAPopup = true;
		}

		// if (pDidCloseAPopup != nullptr)
		// {
		// 	*pDidCloseAPopup = didCloseAPopup;
		// }
	}

	// TODO Uno: Implementation is currently simplified compared to MUX.
	internal IReadOnlyList<Popup> GetOpenPopups() =>
		_openPopups
			.Where(p => !p.IsDisposed)
			.Select(p => p.Target)
			.OfType<Popup>()
			.Distinct()
			.ToList()
			.AsReadOnly();
}
