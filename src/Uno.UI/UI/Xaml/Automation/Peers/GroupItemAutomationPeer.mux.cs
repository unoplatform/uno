// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItemAutomationPeer_Partial.cpp, commit 5f9e85113

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class GroupItemAutomationPeer
{
	// CreateInstanceWithOwnerImpl is represented in C# by the public constructor accepting a GroupItem owner.
	// Original C++:
	// _Check_return_ HRESULT GroupItemAutomationPeerFactory::CreateInstanceWithOwnerImpl(
	//     _In_ xaml_controls::IGroupItem* owner,
	//     _In_opt_ IInspectable* pOuter,
	//     _Outptr_ IInspectable** ppInner,
	//     _Outptr_ xaml_automation_peers::IGroupItemAutomationPeer** ppInstance)
	// {
	//     HRESULT hr = S_OK;
	//     xaml_automation_peers::IGroupItemAutomationPeer* pInstance = NULL;
	//     IInspectable* pInner = NULL;
	//     xaml::IUIElement* ownerAsUIE = NULL;
	//
	//     IFCPTR(ppInstance);
	//     IFCEXPECT(pOuter == NULL || ppInner != NULL);
	//     IFCPTR(owner);
	//     IFC(ctl::do_query_interface(ownerAsUIE, owner));
	//
	//     IFC(ActivateInstance(pOuter,
	//             static_cast<GroupItem*>(owner)->GetHandle(),
	//             &pInner));
	//     IFC(ctl::do_query_interface(pInstance, pInner));
	//     IFC(static_cast<GroupItemAutomationPeer*>(pInstance)->put_Owner(ownerAsUIE));
	//
	//     if (ppInner)
	//     {
	//         *ppInner = pInner;
	//         pInner = NULL;
	//     }
	//
	//     *ppInstance = pInstance;
	//     pInstance = NULL;
	//
	// Cleanup:
	//     ReleaseInterface(ownerAsUIE);
	//     ReleaseInterface(pInstance);
	//     ReleaseInterface(pInner);
	//     RRETURN(hr);
	// }

	// Initializes a new instance of the GroupAutomationPeer class.
	// GroupItemAutomationPeer::GroupItemAutomationPeer() — represented by the default C# constructor in the main class declaration.

#if HAS_UNO
	// TODO Uno: Original C++ destructor. Uno does not support cleanup via finalizers.
	// Deconstructor
	// GroupItemAutomationPeer::~GroupItemAutomationPeer()
	// {
	// }
#endif

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		// Uno: The WinUI custom implementation is disabled (see #if false block below) because it depends on
		// ItemsControl / ItemsControlAutomationPeer / ItemAutomationPeer / FrameworkElementAutomationPeer
		// internals that Uno does not yet expose. Fall back to the base FrameworkElementAutomationPeer
		// implementation so that at least the standard visual-tree traversal runs.
		IList<AutomationPeer> pAPChildren = base.GetChildrenCore();
#if false // TODO Uno: Depends on ItemsControl.get_ItemsHost, ItemsControl.GetItemOrContainerFromContainer, ItemsControlAutomationPeer.GetItemPeerFromChildrenCache / GetItemPeerFromItemContainerCache / OnCreateItemAutomationPeerProtected / AddItemAutomationPeerToItemPeerStorage, ItemAutomationPeer.GetContainerPeer, and FrameworkElementAutomationPeer.GetAutomationPeerChildren — none of which are currently exposed in Uno with the matching signatures.
		UIElement? pOwner = null;
		global::System.Collections.Generic.IList<UIElement>? pItemsFromItemsHostPanel = null;
		IList<AutomationPeer>? pAPChildren2 = null;
		ItemAutomationPeer? pItemPeer = null;
		ItemsControlAutomationPeer? pItemsControlAutomationPeer = null;
		ItemsControl? pTemplatedItemsControl = null;
		AutomationPeer? pItemPeerAsAP = null;
		AutomationPeer? pContainerItemPeer = null;
		Panel? pItemsHostPanel = null;
		Control? pHeaderContent = null;
		UIElement? pHeaderContentAsUIE = null;
		UIElement? pItemContainer = null;
		object? pItem = null;
		DependencyObject? pItemContainerAsDO = null;
		bool bFoundInChildrenCache = false;

		pAPChildren2 = new List<AutomationPeer>();

		pOwner = Owner;

		// Append Header content of the Groups before the Items in UIA tree.
		pHeaderContent = ((GroupItem)pOwner).GetHeaderContent();
		if (pHeaderContent is not null)
		{
			pHeaderContentAsUIE = pHeaderContent as UIElement;
			FrameworkElementAutomationPeer.GetAutomationPeerChildren(pHeaderContentAsUIE, pAPChildren2);
		}

		pItemsControlAutomationPeer = get_ParentItemsControlAutomationPeer();
		pTemplatedItemsControl = ((GroupItem)pOwner).GetTemplatedItemsControl();
		if (pTemplatedItemsControl is not null && pItemsControlAutomationPeer is not null)
		{
			pItemsHostPanel = ((ItemsControl)pTemplatedItemsControl).get_ItemsHost();
			if (pItemsHostPanel is not null)
			{
				pItemsFromItemsHostPanel = pItemsHostPanel.Children;
				uint nCount = 0;
				nCount = (uint)pItemsFromItemsHostPanel.Count;
				if (nCount > 0)
				{
					for (uint i = 0; i < nCount; i++)
					{
						pItemContainer = pItemsFromItemsHostPanel[(int)i];
						pItemContainerAsDO = pItemContainer as DependencyObject;
						// IFCEXPECT(pItemContainerAsDO);
						pItem = ((ItemsControl)pTemplatedItemsControl).GetItemOrContainerFromContainer(pItemContainerAsDO);

						if (pItem is not null)
						{
							pItemPeer = ((ItemsControlAutomationPeer)pItemsControlAutomationPeer).GetItemPeerFromChildrenCache(pItem);
							if (pItemPeer is null)
							{
								bool bFoundInCache = false;
								pItemPeer = ((ItemsControlAutomationPeer)pItemsControlAutomationPeer).GetItemPeerFromItemContainerCache(pItem, out bFoundInCache);
							}
							else
							{
								bFoundInChildrenCache = true;
							}
							if (pItemPeer is null)
							{
								pItemPeer = ((ItemsControlAutomationPeer)pItemsControlAutomationPeer).OnCreateItemAutomationPeerProtected(pItem);
							}

							if (pItemPeer is not null)
							{
								pContainerItemPeer = ((ItemAutomationPeer)pItemPeer).GetContainerPeer();
								if (pContainerItemPeer is not null)
								{
									pItemPeerAsAP = pItemPeer as AutomationPeer;
									((AutomationPeer)pContainerItemPeer).EventsSource = pItemPeerAsAP;
									pAPChildren2.Add(pItemPeerAsAP!);
									if (!bFoundInChildrenCache)
									{
										((ItemsControlAutomationPeer)pItemsControlAutomationPeer).AddItemAutomationPeerToItemPeerStorage((ItemAutomationPeer)pItemPeer);
									}
								}
							}
						}
						// ReleaseInterface(pContainerItemPeer);
						// ReleaseInterface(pItemPeerAsAP);
						// ReleaseInterface(pItem);
						// ReleaseInterface(pItemContainer);
						// ReleaseInterface(pItemContainerAsDO);
						// ReleaseInterface(pItemPeer);
					}
				}
			}
		}
		pAPChildren = pAPChildren2;
#endif
		return pAPChildren;
	}

	protected override string GetClassNameCore()
	{
		return "GroupItem";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Group;
	}

	// Gets the AP for Parent ItemsControl for this GroupItem if there is one.
	private ItemsControlAutomationPeer? get_ParentItemsControlAutomationPeer()
	{
		ItemsControlAutomationPeer? ppParentItemsControl = null;
		UIElement? pOwner;
		ItemsControl? pItemsControl;
		ItemsControlAutomationPeer? pItemsControlAutomationPeer = null;
		AutomationPeer? pItemsControlAutomationPeerAsAP;

		pOwner = Owner;
		if (pOwner is null)
		{
			return null;
		}

		pItemsControl = ItemsControl.ItemsControlFromItemContainer(pOwner);
		if (pItemsControl is not null)
		{
#if false // TODO Uno: ItemsControl.GetOrCreateAutomationPeer is not exposed in Uno with the matching signature.
			pItemsControlAutomationPeerAsAP = ((ItemsControl)pItemsControl).GetOrCreateAutomationPeer();
			pItemsControlAutomationPeer = pItemsControlAutomationPeerAsAP as ItemsControlAutomationPeer;
			ppParentItemsControl = pItemsControlAutomationPeer;
			pItemsControlAutomationPeer = null;
#endif
		}

		return ppParentItemsControl;
	}
}
