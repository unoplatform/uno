// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_UIAProperties.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListViewBase
{
	internal string? GetDropTargetDropEffect()
	{
		if (m_strDropTargetDropEffect is null)
		{
			UpdateDropTargetDropEffect(true, false /*isLeaving*/);
		}
		return m_strDropTargetDropEffect;
	}

	internal string[] GetDropTargetDropEffects()
	{
		string[] phsNewArray = new string[1];

		phsNewArray[0] = GetDropTargetDropEffect() ?? string.Empty;

		return phsNewArray;
	}

	internal IRawElementProviderSimple[] GetDragGrabbedItems()
	{
		if (m_strDropTargetDropEffect is null)
		{
			UpdateDragGrabbedItems(true);
		}
		return CreateCopyOfDragGrabbedItems();
	}

	private static void GetString(string id, ref string? phsString, ref string? ppszString)
	{
		if (phsString is null)
		{
			phsString = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(id);
			ppszString = phsString;
		}
	}

	private void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving)
		=> UpdateDropTargetDropEffect(forceUpdate, isLeaving, null);

	private void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving, UIElement? keyboardReorderedContainer)
	{
		AutomationPeer? spAP;
		UIElement? spDragItem = null;
#if !HAS_UNO // TODO Uno: DragDrop is not available. The dragged item and its count come from the primary dragged container.
		ctl::WeakRefPtr wrDragItemWeak;
		DragDrop* pDragDrop = nullptr;
#endif

		string szBuffer = string.Empty;
		string szItemBuffer;
		int cchBuffer = 0;

		string? strOldValue;

		string? pszItem = null;
		string? pszLeft = null;
		string? pszRight = null;
		string? pszOver = null;

		// These strings are used to construct UIAutomation messages for drag and drop events.
		string? strMsgItem = null;
		string? strMsgDragItemsCount = null;
		string? strMsgCancelDragging = null;
		string? strMsgPlaceBetween = null;
		string? strMsgAfter = null;
		string? strMsgBefore = null;
		string? strMsgPlace3 = null;
		string? strMsgPlaceItem = null;
		string? strMsgDropInto = null;
		string? strItem = null;

		string? pszMsgDragItemsCount = null;
		string? pszMsgCancelDragging = null;
		string? pszMsgPlaceBetween = null;
		string? pszMsgAfter = null;
		string? pszMsgBefore = null;
		string? pszMsgPlace3 = null;
		string? pszMsgPlaceItem = null;
		string? pszMsgDropInto = null;

		spAP = GetOrCreateAutomationPeer();

		if (spAP is null || (!forceUpdate && !AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)))
		{
			m_strDropTargetDropEffect = null;
			return;
		}

		if (keyboardReorderedContainer is not null)
		{
			spDragItem = keyboardReorderedContainer;
		}
		else
		{
#if !HAS_UNO // TODO Uno: DragDrop is not available.
			pDragDrop = DXamlCore::GetCurrent()->GetDragDrop();
			if (pDragDrop && pDragDrop->GetIsDragDropInProgress())
			{
				wrDragItemWeak = pDragDrop->GetCurrentDragItem();
			}
			if (wrDragItemWeak)
			{
				IFC(wrDragItemWeak.As<IUIElement>(&spDragItem));
			}
#else
			if (IsInDragDrop())
			{
				spDragItem = m_tpPrimaryDraggedContainer;
			}
#endif
		}

		if (spDragItem is not null)
		{
			int cItemCount = -1;

#if !HAS_UNO // TODO Uno: DragDrop is not available.
			if (pDragDrop)
			{
				cItemCount = pDragDrop->GetItemCount();
			}
#else
			if (keyboardReorderedContainer is null)
			{
				cItemCount = (int)m_dragItemsCount;
			}
#endif

			if (cItemCount != -1 && 1 < cItemCount)
			{
				GetString("UIA_DRAG_ITEMS_COUNT", ref strMsgDragItemsCount, ref pszMsgDragItemsCount);
				string szNumber = cItemCount.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
				szItemBuffer = StringUtil.FormatMsg(pszMsgDragItemsCount!, szNumber);
				if (!(1 < szItemBuffer.Length))
				{
					throw new InvalidOperationException();
				}
				pszItem = szItemBuffer;
			}
			else
			{
				spAP = spDragItem.GetOrCreateAutomationPeer();
				if (spAP is not null)
				{
					strItem = spAP.GetName();
				}

				if (!string.IsNullOrEmpty(strItem))
				{
					pszItem = strItem;
				}

				if (pszItem is null)
				{
					GetString("UIA_DRAG_ITEM", ref strMsgItem, ref pszItem);
				}
			}

			if (isLeaving)
			{
				GetString("UIA_DRAG_CANCEL_DRAGGING", ref strMsgCancelDragging, ref pszMsgCancelDragging);
				szBuffer = StringUtil.FormatMsg(pszMsgCancelDragging!, pszItem!);
				cchBuffer = szBuffer.Length;
			}
			else
			{
				DependencyObject? spLeftItem = null;
				DependencyObject? spRightItem = null;

				if (m_tpDragOverItem is not null)
				{
					DependencyObject spOverItem;

					spOverItem = m_tpDragOverItem;
					pszOver = GetAutomationString(spOverItem);
				}
				else
				{
					int rightIndex;
					int leftIndex;

					if (keyboardReorderedContainer is not null)
					{
						int index = 0;
						index = IndexFromContainer(keyboardReorderedContainer);
						rightIndex = index + 1;
						leftIndex = index - 1;
					}
					else
					{
						rightIndex = GetInsertionIndexForLiveReorder();
						leftIndex = rightIndex - 1;
					}

					int itemsCount = -1;

					itemsCount = GetItemsCount();

					if (leftIndex >= 0 && leftIndex < itemsCount)
					{
						spLeftItem = ContainerFromIndex(leftIndex);
					}

					if (rightIndex >= 0 && rightIndex < itemsCount)
					{
						spRightItem = ContainerFromIndex(rightIndex);
					}
				}

				if (spLeftItem is not null)
				{
					pszLeft = GetAutomationString(spLeftItem);
				}

				if (spRightItem is not null)
				{
					pszRight = GetAutomationString(spRightItem);
				}

				if (pszLeft is not null && pszRight is not null)
				{
					GetString("UIA_DRAG_PLACE_BETWEEN", ref strMsgPlaceBetween, ref pszMsgPlaceBetween);
					szBuffer = StringUtil.FormatMsg(pszMsgPlaceBetween!, pszItem!, pszLeft, pszRight);
					cchBuffer = szBuffer.Length;
				}
				else if (pszLeft is not null)
				{
					GetString("UIA_DRAG_AFTER", ref strMsgAfter, ref pszMsgAfter);
					GetString("UIA_DRAG_PLACE_3", ref strMsgPlace3, ref pszMsgPlace3);

					szBuffer = StringUtil.FormatMsg(pszMsgPlace3!, pszItem!, pszMsgAfter!, pszLeft);
					cchBuffer = szBuffer.Length;
				}
				else if (pszRight is not null)
				{
					GetString("UIA_DRAG_BEFORE", ref strMsgBefore, ref pszMsgBefore);
					GetString("UIA_DRAG_PLACE_3", ref strMsgPlace3, ref pszMsgPlace3);

					szBuffer = StringUtil.FormatMsg(pszMsgPlace3!, pszItem!, pszMsgBefore!, pszRight);
					cchBuffer = szBuffer.Length;
				}
				else if (pszOver is not null)
				{
					GetString("UIA_DRAG_DROPINTO", ref strMsgDropInto, ref pszMsgDropInto);
					szBuffer = StringUtil.FormatMsg(pszMsgDropInto!, pszItem!, pszOver);
					cchBuffer = szBuffer.Length;
				}
				else
				{
					GetString("UIA_DRAG_PLACE_ITEM", ref strMsgPlaceItem, ref pszMsgPlaceItem);
					szBuffer = StringUtil.FormatMsg(pszMsgPlaceItem!, pszItem!);
					cchBuffer = szBuffer.Length;
				}
			}

			if (cchBuffer < 0)
			{
				throw new InvalidOperationException();
			}
		}
		else
		{
			szBuffer = string.Empty;
			cchBuffer = 0;
		}

		strOldValue = m_strDropTargetDropEffect;
		m_strDropTargetDropEffect = null;

		// An empty HSTRING is null, which is what the "not computed yet" checks look for.
		m_strDropTargetDropEffect = cchBuffer == 0 ? null : szBuffer;

		if (!forceUpdate && spAP is not null && AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			if (!string.Equals(strOldValue ?? string.Empty, m_strDropTargetDropEffect ?? string.Empty, StringComparison.Ordinal))
			{
				spAP.RaisePropertyChangedEvent(DropTargetPatternIdentifiers.DropTargetEffectProperty, strOldValue ?? string.Empty, m_strDropTargetDropEffect ?? string.Empty);
			}
		}
	}

	private void UpdateDragGrabbedItems(bool forceUpdate)
	{
		// The dragged items collection is really a function of the ListView but the UIA property is surfaced on the ListviewBaseItem.
		// Since ListView has all the data we do the implementation here but are careful to raise UIA events on the ListViewBaseItem's automation peer.
		AutomationPeer? spAP = null;

		if (m_tpDragGrabbedItems is null)
		{
			TrackerCollection<IRawElementProviderSimple> spDragGrabbedItems = new();
			m_tpDragGrabbedItems = spDragGrabbedItems;
		}
		else
		{
			m_tpDragGrabbedItems.Clear();
		}

#if !HAS_UNO // TODO Uno: DragDrop is not available.
		pDragDrop = DXamlCore::GetCurrent()->GetDragDrop();
		if (pDragDrop && pDragDrop->GetIsDragDropInProgress() && m_tpPrimaryDraggedContainer)
#else
		if (IsInDragDrop() && m_tpPrimaryDraggedContainer is not null)
#endif
		{
			spAP = ((ListViewBaseItem)m_tpPrimaryDraggedContainer).GetOrCreateAutomationPeer();
		}

		if (spAP is not null && (forceUpdate || AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)))
		{
			IList<object>? spItems;
			List<uint> draggedItemIndices = new();

			GetDraggedItems((ListViewBaseItem)m_tpPrimaryDraggedContainer!, true, out spItems, draggedItemIndices);

#if DEBUG
			{
				uint srcSize = 0;
				srcSize = (uint)spItems!.Count;
				global::System.Diagnostics.Debug.Assert(srcSize == draggedItemIndices.Count);
			}
#endif

			for (int i = 0; i < draggedItemIndices.Count; ++i)
			{
				object spItem;
				UIElement? spUIE;
				AutomationPeer? spRepsAP;

				spItem = spItems![i];
				spUIE = UIA_GetContainerForDataItemOverride(spItem, (int)draggedItemIndices[i]);
				if (spUIE is not null)
				{
					spRepsAP = spUIE.GetOrCreateAutomationPeer();
					if (spRepsAP is not null)
					{
						IRawElementProviderSimple spItemProvider;
						spItemProvider = spRepsAP.ProviderFromPeer(spRepsAP);
						m_tpDragGrabbedItems.Add(spItemProvider);
					}
				}
			}
		}
	}

	private IRawElementProviderSimple[] CreateCopyOfDragGrabbedItems()
	{
		IRawElementProviderSimple[] ppDragGrabbedItems;
		uint cDragGrabbedItems;

		// m_tpDragGrabbedItems might not have been set yet. ListViewBase::UpdateDragGrabbedItems is the only place where m_tpDragGrabbedItems gets
		// set, but this can be skipped when the first drag in the process triggers ListViewBase::OnDragGesture and UpdateDropTargetDropEffect, the
		// old string (“”) Doesn’t match the new UIA drag string, a property event fires, UIA tries to access m_tpDragGrabbedItems
		if (m_tpDragGrabbedItems is null)
		{
			UpdateDragGrabbedItems(false);

			// If for some reason m_tpDragGrabbedItems is sill not set then something catastrophic reallly did happen!
			if (m_tpDragGrabbedItems is null)
			{
				throw new InvalidOperationException();
			}
		}
		cDragGrabbedItems = (uint)m_tpDragGrabbedItems.Count;

		ppDragGrabbedItems = new IRawElementProviderSimple[cDragGrabbedItems];

		for (uint i = 0; i < cDragGrabbedItems; ++i)
		{
			IRawElementProviderSimple pItem;
			pItem = m_tpDragGrabbedItems[(int)i];
			ppDragGrabbedItems[i] = pItem;
		}

		return ppDragGrabbedItems;
	}

	private string? GetAutomationString(
		DependencyObject item)
	{
		DependencyObject? spItem = item;
		string? automationString = null;

		if (spItem is not null)
		{
			string? name = null;
			AutomationPeer? spAP;

			spAP = ((ListViewBaseItem)spItem).GetOrCreateAutomationPeer();
			if (spAP is not null)
			{
				name = spAP.GetName();
			}

			if (!string.IsNullOrEmpty(name))
			{
				automationString = name;
			}
		}

		return automationString;
	}
}

