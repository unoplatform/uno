// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes GroupItem types to Microsoft UI Automation.
/// </summary>
public partial class GroupItemAutomationPeer : FrameworkElementAutomationPeer
{
	public GroupItemAutomationPeer(GroupItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GroupItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;

	//protected override IList<AutomationPeer> GetChildrenCore()
	//{
	//	HRESULT hr = S_OK;
	//	xaml::IUIElement* pOwner = NULL;
	//	wfc::IVector<xaml::UIElement*>* pItemsFromItemsHostPanel = NULL;
	//	wfc::IVector<xaml_automation_peers::AutomationPeer*>* pAPChildren = NULL;
	//	xaml_automation_peers::IItemAutomationPeer* pItemPeer = NULL;
	//	xaml_automation_peers::IItemsControlAutomationPeer* pItemsControlAutomationPeer = NULL;
	//	xaml_controls::IItemsControl* pTemplatedItemsControl = NULL;
	//	xaml_automation_peers::IAutomationPeer* pItemPeerAsAP = NULL;
	//	xaml_automation_peers::IAutomationPeer* pContainerItemPeer = NULL;
	//	xaml_controls::IPanel* pItemsHostPanel = NULL;
	//	xaml_controls::IControl* pHeaderContent = NULL;
	//	xaml::IUIElement* pHeaderContentAsUIE = NULL;
	//	IUIElement* pItemContainer = NULL;
	//	IInspectable* pItem = NULL;
	//	IDependencyObject* pItemContainerAsDO = NULL;
	//	BOOLEAN bFoundInChildrenCache = FALSE;

	//	IFCPTR(ppReturnValue);

	//	IFC(ctl::ComObject<TrackerCollection<xaml_automation_peers::AutomationPeer*>>::CreateInstance(&pAPChildren));

	//	IFC(get_Owner(&pOwner));
	//	IFCPTR(pOwner);

	//	// Append Header content of the Groups before the Items in UIA tree.
	//	IFC((static_cast<GroupItem*>(pOwner))->GetHeaderContent(&pHeaderContent));
	//	if(pHeaderContent)
	//	{
	//		IFC(ctl::do_query_interface(pHeaderContentAsUIE, pHeaderContent));
	//	IFC(FrameworkElementAutomationPeer::GetAutomationPeerChildren(pHeaderContentAsUIE, pAPChildren));
	//}

	//IFC(get_ParentItemsControlAutomationPeer(&pItemsControlAutomationPeer));
	//	IFC((static_cast<GroupItem*>(pOwner))->GetTemplatedItemsControl(&pTemplatedItemsControl));
	//	if(pTemplatedItemsControl && pItemsControlAutomationPeer)
	//	{
	//		IFC((static_cast<ItemsControl*>(pTemplatedItemsControl))->get_ItemsHost(&pItemsHostPanel));
	//		if(pItemsHostPanel)
	//		{
	//			IFC(pItemsHostPanel->get_Children(&pItemsFromItemsHostPanel));
	//			UINT nCount = 0;
	//IFC(pItemsFromItemsHostPanel->get_Size(&nCount));
	//			if(nCount > 0)
	//			{
	//				for(UINT i = 0; i<nCount; i++)
	//				{
	//					IFC(pItemsFromItemsHostPanel->GetAt(i, &pItemContainer));
	//					IFC(ctl::do_query_interface(pItemContainerAsDO, pItemContainer));
	//IFCEXPECT(pItemContainerAsDO);
	//IFC((static_cast<ItemsControl*>(pTemplatedItemsControl))->GetItemOrContainerFromContainer(pItemContainerAsDO, &pItem));

	//					if(pItem != NULL)
	//					{
	//						IFC(static_cast<ItemsControlAutomationPeer*>(pItemsControlAutomationPeer)->GetItemPeerFromChildrenCache(pItem, &pItemPeer));
	//						if(!pItemPeer)
	//						{
	//							BOOLEAN bFoundInCache = FALSE;
	//IFC(static_cast<ItemsControlAutomationPeer*>(pItemsControlAutomationPeer)->GetItemPeerFromItemContainerCache(pItem, &pItemPeer, bFoundInCache));
	//						}

	//						else
	//{
	//	bFoundInChildrenCache = TRUE;
	//}
	//if (!pItemPeer)
	//{
	//	IFC(static_cast<ItemsControlAutomationPeer*>(pItemsControlAutomationPeer)->OnCreateItemAutomationPeerProtected(pItem, &pItemPeer));
	//}

	//if (pItemPeer != NULL)
	//{
	//	IFC(static_cast<ItemAutomationPeer*>(pItemPeer)->GetContainerPeer(&pContainerItemPeer));
	//	if (pContainerItemPeer)
	//	{
	//		IFC(ctl::do_query_interface(pItemPeerAsAP, pItemPeer));
	//		IFC(static_cast<AutomationPeer*>(pContainerItemPeer)->put_EventsSource(pItemPeerAsAP));
	//		IFC(pAPChildren->Append(pItemPeerAsAP));
	//		if (!bFoundInChildrenCache)
	//		{
	//			IFC(static_cast<ItemsControlAutomationPeer*>(pItemsControlAutomationPeer)->AddItemAutomationPeerToItemPeerStorage(static_cast<ItemAutomationPeer*>(pItemPeer)));
	//		}
	//	}
	//}
	//					}
	//					ReleaseInterface(pContainerItemPeer);
	//ReleaseInterface(pItemPeerAsAP);
	//ReleaseInterface(pItem);
	//ReleaseInterface(pItemContainer);
	//ReleaseInterface(pItemContainerAsDO);
	//ReleaseInterface(pItemPeer);
	//				}
	//			}
	//		}
	//	}
	//	*ppReturnValue = pAPChildren;
	//pAPChildren = NULL;

	//Cleanup:
	//ReleaseInterface(pOwner);
	//ReleaseInterface(pItemsFromItemsHostPanel);
	//ReleaseInterface(pItemsHostPanel);
	//ReleaseInterface(pItem);
	//ReleaseInterface(pItemPeer);
	//ReleaseInterface(pItemPeerAsAP);
	//ReleaseInterface(pContainerItemPeer);
	//ReleaseInterface(pItemContainer);
	//ReleaseInterface(pItemContainerAsDO);
	//ReleaseInterface(pItemsControlAutomationPeer);
	//ReleaseInterface(pTemplatedItemsControl);
	//ReleaseInterface(pHeaderContent);
	//ReleaseInterface(pHeaderContentAsUIE);
	//ReleaseInterface(pAPChildren);
	//RRETURN(hr);
	//}

	//// Gets the AP for Parent ItemsControl for this GroupItem if there is one.
	//_Check_return_ HRESULT GroupItemAutomationPeer::get_ParentItemsControlAutomationPeer(_Out_ xaml_automation_peers::IItemsControlAutomationPeer** ppParentItemsControl)
	//{

	//	HRESULT hr = S_OK;
	//	xaml::IUIElement* pOwner = NULL;
	//	xaml_controls::IItemsControl* pItemsControl = NULL;
	//	xaml_automation_peers::IItemsControlAutomationPeer* pItemsControlAutomationPeer = NULL;
	//	xaml_automation_peers::IAutomationPeer* pItemsControlAutomationPeerAsAP = NULL;
	//	IFCPTR(ppParentItemsControl);
	//	IFC(get_Owner(&pOwner));
	//	IFCPTR(pOwner);

	//	IFC(ItemsControl::ItemsControlFromItemContainer(static_cast<UIElement*>(pOwner), &pItemsControl));
	//	if(pItemsControl)
	//	{
	//		IFC(static_cast<ItemsControl*>(pItemsControl)->GetOrCreateAutomationPeer(&pItemsControlAutomationPeerAsAP));
	//		IFC(ctl::do_query_interface(pItemsControlAutomationPeer, pItemsControlAutomationPeerAsAP));
	//		*ppParentItemsControl = pItemsControlAutomationPeer;
	//		pItemsControlAutomationPeer = NULL;
	//	}

	//Cleanup:
	//	ReleaseInterface(pOwner);
	//ReleaseInterface(pItemsControl);
	//ReleaseInterface(pItemsControlAutomationPeerAsAP);
	//ReleaseInterface(pItemsControlAutomationPeer);
	//RRETURN(hr);
	//}
}
