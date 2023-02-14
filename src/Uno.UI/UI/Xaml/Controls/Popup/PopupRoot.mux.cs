// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.cpp, PopupRoot_Partial.cpp, PopupRoot.cpp

#nullable enable

using System;
using System.Collections.Generic;
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

	[NotImplemented]
	internal Popup? GetTopmostPopup(PopupFilter filter)
	{
		// TODO Uno: Implement
		return null;
		//if (m_pOpenPopups)
		//{
		//	// Find the most recently opened Popup that has light dismiss enabled.
		//	// Unloading popups don't count.
		//	for (CXcpList<Popup>::XCPListNode* pNode = m_pOpenPopups.GetHead(); pNode != null; pNode = pNode.m_pNext)
		//	{
		//		Popup pPopup = pNode.m_pData;
		//		if (!pPopup.IsUnloading())
		//		{
		//			if (filter == PopupFilter.All || (filter == PopupFilter.LightDismissOrFlyout && pPopup.IsFlyout()) || pPopup.m_fIsLightDismiss)
		//			{
		//				return pPopup;
		//			}
		//		}
		//	}
		//}

		//return null;
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

	[NotImplemented]
	internal Popup[] GetOpenPopups()
	{
		// TODO Uno: Implement
		return Array.Empty<Popup>();
		//	CXcpList<Popup>::XCPListNode* pNode = null;
		//	XINT32 count = 0;
		//	Popup** ppResults = null;

		//	if (!m_pOpenPopups)
		//	{
		//		// no open popups, nothing to do here.
		//		pnCount = 0;
		//		*pppPopups = null;
		//		goto Cleanup;
		//	}

		//	pNode = m_pOpenPopups.GetHead();

		//	// get count
		//	while (pNode && count < XINT32_MAX)
		//	{
		//		if (!pNode.m_pData.IsUnloading())
		//		{
		//			count++;
		//		}

		//		pNode = pNode.m_pNext;
		//	}

		//	if (count > 0)
		//	{
		//		pNode = m_pOpenPopups.GetHead();
		//		ppResults = new Popup[count];

		//		for (XINT32 i = 0; i < count && pNode; pNode = pNode.m_pNext)
		//		{
		//			if (!pNode.m_pData.IsUnloading())
		//			{
		//				ppResults[i] = pNode.m_pData;
		//				AddRefInterface(pNode.m_pData);
		//				i++;
		//			}
		//		}
		//	}

		//	*pnCount = count;
		//	*pppPopups = ppResults;
		//	ppResults = null;
		//Cleanup:
		//	delete[] ppResults;
		//	RRETURN(hr);
	}
}
