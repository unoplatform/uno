using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	partial class LoopingSelectorAutomationPeer
	{
		internal LoopingSelectorAutomationPeer(LoopingSelector pOwner) : base(pOwner)
		{
			InitializeImpl(pOwner);
		}

		void InitializeImpl(LoopingSelector pOwner)
		{
			//FrameworkElementAutomationPeerFactory spInnerFactory;
			//FrameworkElementAutomationPeer spInnerInstance;
			//FrameworkElement spLoopingSelectorAsFE;
			//var spOwner = pOwner;
			//DependencyObject spInnerInspectable;

			//ARG_NOTnull(pOwner, "pOwner");

			//LoopingSelectorAutomationPeerGenerated.InitializeImpl(pOwner);
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
			//	&spInnerFactory));

			//(((DependencyObject)(pOwner)).QueryInterface<xaml.FrameworkElement>(
			//	&spLoopingSelectorAsFE));
			//spLoopingSelectorAsFE = pOwner;

			//(spInnerFactory.CreateInstanceWithOwner(
			//		spLoopingSelectorAsFE,
			//		(ILoopingSelectorAutomationPeer)(this),
			//		&spInnerInspectable,
			//		&spInnerInstance));

			//(SetComposableBasePointers(
			//	spInnerInspectable,
			//	spInnerFactory));
		}

		void GetOwnerAsInternalPtrNoRef(out LoopingSelector ppOwnerNoRef)
		{
			UIElement spOwnerAsUIElement;
			LoopingSelector spOwner;
			FrameworkElementAutomationPeer spThisAsFEAP;

			ppOwnerNoRef = null;

			//(QueryInterface(
			//	__uuidof(FrameworkElementAutomationPeer),
			//	&spThisAsFEAP));
			spThisAsFEAP = this;
			spOwnerAsUIElement = spThisAsFEAP.Owner;
			if (spOwnerAsUIElement is { })
			{
				//spOwnerAsUIElement.As(spOwner);
				spOwner = spOwnerAsUIElement as LoopingSelector;
				// No is passed back to the caller.
				ppOwnerNoRef = spOwner;
			}
		}

		internal void ClearPeerMap()
		{
			//for (PeerMap.iterator iter = _peerMap.begin(); iter != _peerMap.end(); iter++)
			//{
			//	iter.second.Release();
			//	iter.second = null;
			//}
			foreach (var iter in _peerMap)
			{
				//iter.Value.Release();
			}

			_peerMap.Clear();
		}

		internal void RealizeItemAtIndex(int index)
		{
			LoopingSelector pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);

			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationRealizeItemForAP((uint)index);
			}
		}

#if false
		#region IItemsContainerProvider

		void
			FindItemByPropertyImpl(
				IRawElementProviderSimple startAfter,
				AutomationProperty automationProperty,
				DependencyObject value,
				out IRawElementProviderSimple returnValue)
		{
			LoopingSelector pOwnerNoRef = null;
			IList<object> spItemsCollection = default;

			returnValue = null;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				spItemsCollection = pOwnerNoRef.Items;
			}

			if (spItemsCollection is { })
			{
				var startIdx = 0;
				var totalItems = 0;
				//string nameProperty;
				//AutomationPeerProtected spThisAsProtected;

				// For the Name and IsSelected property search cases we cache these
				// values outside of the loop. Otherwise these variables remain unused.
				var isSelected = false;
				string strNameToFind = default;
				DependencyObject spSelectedItem = default;

				AutomationHelper.AutomationPropertyEnum propertyAsEnum =
					AutomationHelper.AutomationPropertyEnum.EmptyProperty;

				totalItems = spItemsCollection.Count;
				FindStartIndex(
					startAfter,
					spItemsCollection,
					out startIdx);
				//(AutomationHelper.ConvertPropertyToEnum(
				//	automationProperty,
				//	&propertyAsEnum));
				propertyAsEnum = AutomationHelper.ConvertPropertyToEnum(automationProperty);

				//(QueryInterface(
				//	__uuidof(IAutomationPeerProtected),
				//	&spThisAsProtected));

				if (propertyAsEnum == AutomationHelper.AutomationPropertyEnum.NameProperty && value is { })
				{
					//Private.ValueBoxer.UnboxString(value, strNameToFind);
					strNameToFind = value.ToString();
				}
				else if (propertyAsEnum == AutomationHelper.AutomationPropertyEnum.IsSelectedProperty && value is { })
				{
					//PropertyValue spValueAsPropertyValue;
					//value.QueryInterface<PropertyValue>(spValueAsPropertyValue);

					//spValueAsPropertyValue.GetBoolean(isSelected);
					spSelectedItem = pOwnerNoRef.SelectedItem as DependencyObject;
				}

				for (int itemIdx = startIdx + 1; itemIdx < totalItems; itemIdx++)
				{
					var breakOnPeer = false;

					LoopingSelectorItemDataAutomationPeer spItemDataAP;
					{
						DependencyObject spItem;
						//spItemsCollection.GetAt(itemIdx, &spItem);
						spItem = spItemsCollection[itemIdx] as DependencyObject;
						GetDataAutomationPeerForItem(spItem, out spItemDataAP);
					}

					switch (propertyAsEnum)
					{
						case AutomationHelper.AutomationPropertyEnum.EmptyProperty:
							breakOnPeer = true;
							break;
						case AutomationHelper.AutomationPropertyEnum.NameProperty:
							{
								AutomationPeer spAutomationPeer;
								//spItemDataAP.As(spAutomationPeer);
								spAutomationPeer = spItemDataAP;
								string strNameToCompare;
								strNameToCompare = spAutomationPeer.GetName();
								if (strNameToCompare == strNameToFind)
								{
									breakOnPeer = true;
								}
							}
							break;
						case AutomationHelper.AutomationPropertyEnum.IsSelectedProperty:
							{
								DependencyObject spItem;
								((LoopingSelectorItemDataAutomationPeer)spItemDataAP).GetItem(out spItem);
								if (isSelected && spSelectedItem == spItem ||
									!isSelected && spSelectedItem != spItem)
								{
									breakOnPeer = true;
								}
							}
							break;
					}

					if (breakOnPeer)
					{
						AutomationPeer spItemPeerAsAP;

						//spItemDataAP.As(spItemPeerAsAP);
						spItemPeerAsAP = spItemDataAP;
						returnValue = ProviderFromPeer(spItemPeerAsAP);
						break;
					}
				}
			}
		}

		#endregion
#endif

		~LoopingSelectorAutomationPeer()
		{
			ClearPeerMap();
		}

		#region IAutomationPeerOverrides


		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Scroll ||
				patternInterface == PatternInterface.Selection ||
				patternInterface == PatternInterface.ItemContainer)
			{
				//returnValue = this;
				//AddRef();
				return this;
			}
			else
			{
				//LoopingSelectorAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, returnValue);
				return null;
			}
		}

		protected override AutomationControlType GetAutomationControlTypeCore()

		{
			//if (returnValue == null) throw new ArgumentNullException();
			return AutomationControlType.List;
		}

		protected override IList<AutomationPeer> GetChildrenCore()

		{
			LoopingSelector pOwnerNoRef = null;
			IList<AutomationPeer> spReturnValue;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			//IList<AutomationPeer>.Make(spReturnValue);
			spReturnValue = new List<AutomationPeer>();

			if (pOwnerNoRef is { })
			{
				IList<object> spItems;
				int count = 0;

				spItems = pOwnerNoRef.Items;
				count = spItems.Count;

				for (var itemIdx = 0; itemIdx < count; itemIdx++)
				{
					DependencyObject spItem;
					LoopingSelectorItemDataAutomationPeer spLSIDAP;
					AutomationPeer spLSIDAPAsAP;

					//spItems.GetAt(itemIdx, &spItem);
					spItem = spItems[itemIdx] as DependencyObject;
					GetDataAutomationPeerForItem(spItem, out spLSIDAP);
					if (spLSIDAP is { })
					{
						spLSIDAP.SetItemIndex(itemIdx);

						// Update\set the EventsSource here for corresponding container peer for the Item, this ensures
						// its always the data peer that the lower layer is working with. This is especially required
						// anytime bottom up approach comes into play like hit-testing (after finding the right UI target)
						// that code moves bottom up to find relevant AutomationPeer, another case is UIA events.
						LoopingSelectorItemAutomationPeer spContainerPeer;
						GetContainerAutomationPeerForItem(spItem, out spContainerPeer);
						if (spContainerPeer is { } && spLSIDAP is { })
						{
							spContainerPeer.SetEventSource(spLSIDAP);
						}
					}

					//spLSIDAP.As(spLSIDAPAsAP);
					spLSIDAPAsAP = spLSIDAP;
					spReturnValue.Add(spLSIDAPAsAP);
				}
			}

			//spReturnValue.CopyTo(returnValue);
			return spReturnValue;
		}

#if false
		void GetClassNameCoreImpl(out string returnValue)
		{
			//if (returnValue == null) throw new ArgumentNullException();
			//wrl_wrappers.Hstring("LoopingSelector").CopyTo(returnValue);
			returnValue = "LoopingSelector";
		}
#endif

		#endregion

		#region ISelectionProvider

		public bool CanSelectMultiple => false;

		public bool IsSelectionRequired => true;

		public IRawElementProviderSimple[] GetSelection()
		{
			LoopingSelector pOwnerNoRef = null;
			AutomationPeer spAutomationPeer;

			//int pReturnValueSize = 0;
			IRawElementProviderSimple[] pppReturnValue = null;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationTryGetSelectionUIAPeer(out spAutomationPeer);
				if (spAutomationPeer is { })
				{
					//AutomationPeerProtected spAutomationPeerAsProtected;
					//IRawElementProviderSimple spProvider;

					//spAutomationPeer.As(spAutomationPeerAsProtected);

					// TODO
					//spProvider = spAutomationPeer.ProviderFromPeer(spAutomationPeer);
					//pppReturnValue =
					//	(xaml.Automation.Provider.IIRawElementProviderSimple)CoTaskMemAlloc(
					//		sizeof(xaml.Automation.Provider.IIRawElementProviderSimple));
					//if (pppReturnValue)
					//{
					//	pppReturnValue = spProvider.Detach();
					//	pReturnValueSize = 1;
					//}
				}
			}

			return pppReturnValue;
		}

		#endregion

		#region IScrollProvider

		public bool HorizontallyScrollable => false;

		public bool VerticallyScrollable
		{
			get
			{
				LoopingSelector pOwnerNoRef = null;
				GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
				var pValue = false;

				if (pOwnerNoRef is { })
				{
					pOwnerNoRef.AutomationGetIsScrollable(out pValue);
				}

				return pValue;
			}
		}

		public double HorizontalScrollPercent => 0d;

		public double VerticalScrollPercent
		{
			get
			{
				LoopingSelector pOwnerNoRef = null;
				double pValue = 0.0;

				GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
				if (pOwnerNoRef is { })
				{
					pOwnerNoRef.AutomationGetScrollPercent(out pValue);
				}

				return pValue;
			}
		}

		public double VerticalViewSize
		{
			get
			{
				LoopingSelector pOwnerNoRef = null;
				double pValue = 0.0;

				GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
				if (pOwnerNoRef is { })
				{
					pOwnerNoRef.AutomationGetScrollViewSize(out pValue);
				}

				return pValue;
			}
		}

		public double HorizontalViewSize => 100.0d;


		public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
		{
			//UNREFERENCED_PARAMETER(horizontalAmount);


			LoopingSelector pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationScroll(verticalAmount);
			}
		}

		public void SetScrollPercent(double horizontalPercent, double verticalPercent)
		{
			//UNREFERENCED_PARAMETER(horizontalPercent);

			LoopingSelector pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationSetScrollPercent(verticalPercent);
			}
		}

		#endregion

		#region DataItem Support

		public void GetDataAutomationPeerForItem(
			DependencyObject pItem,
			out LoopingSelectorItemDataAutomationPeer ppPeer)
		{
			//PeerMap.iterator peerIter;

			//peerIter = _peerMap.find(pItem);
			if (!_peerMap.TryGetValue(pItem, out var peerIter))
			{
				ppPeer = default;
				return;
			}

			if (peerIter == _peerMap.LastOrDefault().Value)
			{
				LoopingSelectorItemDataAutomationPeer spDataPeer;
				//(wrl.MakeAndInitialize<LoopingSelectorItemDataAutomationPeer>(
				//	&spDataPeer,
				//	pItem,
				//	(LoopingSelectorAutomationPeer)this));
				spDataPeer = new LoopingSelectorItemDataAutomationPeer(pItem, this);

				// PeerMap keeps a pointer to this automation peer.
				// The peers lifetime is owned by this map and is released
				// when LoopingSelectorAP dies or when the items collection
				// changes.
				_peerMap[pItem] = spDataPeer;
				//spDataPeer.AddRef();
				//spDataPeer.CopyTo(ppPeer);
				ppPeer = spDataPeer;
			}
			else
			{
				ppPeer = peerIter;
				//ppPeer.AddRef();
			}
		}

		internal void GetContainerAutomationPeerForItem(
			DependencyObject pItem,
			out LoopingSelectorItemAutomationPeer ppPeer)
		{
			ppPeer = null;
			LoopingSelector pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationGetContainerUIAPeerForItem(pItem, out ppPeer);
			}
		}

#if false
		void FindStartIndex(
			IRawElementProviderSimple pStartAfter,
			IList<object> pItems,
			out int pIndex)
		{
			LoopingSelectorItemDataAutomationPeer pDataPeerNoRef = null;

			pIndex = -1;

			if (pStartAfter is { })
			{
				AutomationPeer spProviderAsPeer;
				LoopingSelectorItemDataAutomationPeer spDataPeer;
				//AutomationPeerProtected spThisAsAPProtected;

				//(QueryInterface(
				//	__uuidof(IAutomationPeerProtected),
				//	&spThisAsAPProtected));

				//spThisAsAPProtected.PeerFromProvider(pStartAfter, &spProviderAsPeer);
				spProviderAsPeer = PeerFromProvider(pStartAfter);
				//spProviderAsPeer.As(spDataPeer);
				spDataPeer = spProviderAsPeer as LoopingSelectorItemDataAutomationPeer;
				pDataPeerNoRef = (LoopingSelectorItemDataAutomationPeer)spDataPeer;
			}

			if (pDataPeerNoRef is { })
			{
				DependencyObject spItem = null;
				var found = false;
				int index = 0;

				pDataPeerNoRef.GetItem(out spItem);
				//pItems.IndexOf(spItem, &index, &found);
				index = pItems.IndexOf(spItem);
				found = index >= 0;
				if (found)
				{
					pIndex = index;
				}
			}
		}

		void TryScrollItemIntoView(DependencyObject pItem)
		{
			LoopingSelector pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationTryScrollItemIntoView(pItem);
			}
		}
#endif
		#endregion
	}
}
