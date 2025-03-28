// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Provider;

namespace Windows.UI.Xaml.Automation.Peers
{
	//typedef std.map<DependencyObject, ILoopingSelectorItemDataAutomationPeer> PeerMap;

	partial class LoopingSelectorAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider, IScrollProvider
	{
		private class PeerMap : Dictionary<DependencyObject, LoopingSelectorItemDataAutomationPeer>
		{
		}

		// public
		// Internal automation methods
		//void GetDataAutomationPeerForItem( DependencyObject pItem, out LoopingSelectorItemDataAutomationPeer ppPeer);
		//void GetContainerAutomationPeerForItem( DependencyObject pItem, out LoopingSelectorItemAutomationPeer ppPeer);
		//void TryScrollItemIntoView( DependencyObject pItem);
		//void ClearPeerMap();
		//void RealizeItemAtIndex(int index);

		// protected

		// IAutomationPeerOverrides
		//void GetPatternCoreImpl(PatternInterface patternInterface,
		//   out DependencyObject returnValue);
		//void GetAutomationControlTypeCoreImpl(
		//   out AutomationControlType returnValue);
		//void GetChildrenCoreImpl(
		//   out  AutomationPeer> returnValue);
		//void GetClassNameCoreImpl(
		//   out string returnValue);

		// private
		// ~LoopingSelectorAutomationPeer();

		// void InitializeImpl(
		//     xaml_primitives.ILoopingSelector pOwner);

		// void GetOwnerAsInternalPtrNoRef(out  xaml_primitives.LoopingSelector ppOwnerNoRef);

		// void FindStartIndex(
		//     xaml.Automation.Provider.IIRawElementProviderSimple pStartAfter,
		//     wfc.IList<DependencyObject> pItems,
		//    out INT pIndex);

		//// IScrollProvider Impl
		// void get_HorizontallyScrollableImpl(out bool pValue);
		// void get_HorizontalScrollPercentImpl(out DOUBLE pValue);
		// void get_HorizontalViewSizeImpl(out DOUBLE pValue);
		// void get_VerticallyScrollableImpl(out bool pValue);
		// void get_VerticalScrollPercentImpl(out DOUBLE pValue);
		// void get_VerticalViewSizeImpl(out DOUBLE pValue);

		// public
		//void ScrollImpl(
		//	ScrollAmount horizontalAmount,
		//	ScrollAmount verticalAmount);

		//void SetScrollPercentImpl(
		//	double horizontalPercent,
		//	double verticalPercent);

		// IItemsContainerProvider Impl
		//void FindItemByPropertyImpl(
		//	IRawElementProviderSimple startAfter,
		//	AutomationProperty automationProperty,
		//	DependencyObject value,
		//	out IRawElementProviderSimple returnValue);

		// ISelectionProvider
		//void get_CanSelectMultipleImpl(out bool value);
		//void get_IsSelectionRequiredImpl(out bool value);

		//void GetSelectionImpl(
		//	out uint pReturnValueSize,
		//	out IRawElementProviderSimple pppReturnValue);

		// private
		// PeerMap is used to keep a 1:1 mapping between items in the list and
		// their respective data automation peers. This is done to ensure that
		// no duplicate data peers are created for items. This class does not
		// keep a on the data peers, and the peers are responsible for removing
		// themselves from this map when deructed.
		private readonly PeerMap _peerMap = new PeerMap();
	}
}
