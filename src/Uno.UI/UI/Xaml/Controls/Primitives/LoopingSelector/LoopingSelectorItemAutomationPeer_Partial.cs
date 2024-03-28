// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{

	partial class LoopingSelectorItemAutomationPeer
	{

		internal LoopingSelectorItemAutomationPeer(LoopingSelectorItem pOwner)
		{
			InitializeImpl(pOwner);
		}

		void InitializeImpl(LoopingSelectorItem pOwner)
		{

			//FrameworkElementAutomationPeerFactory spInnerFactory;
			//FrameworkElementAutomationPeer spInnerInstance;
			//FrameworkElement spLoopingSelectorItemAsFE;
			//LoopingSelectorItem spOwner = pOwner;
			//DependencyObject spInnerInspectable;

			//LoopingSelectorItemAutomationPeerGenerated.InitializeImpl(pOwner);
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
			//	&spInnerFactory));

			//(((DependencyObject)(pOwner)).QueryInterface<FrameworkElement>(
			//	&spLoopingSelectorItemAsFE));
			//spLoopingSelectorItemAsFE = pOwner;

			//(spInnerFactory.CreateInstanceWithOwner(
			//	spLoopingSelectorItemAsFE,
			//	(ILoopingSelectorItemAutomationPeer)(this),
			//	&spInnerInspectable,
			//	&spInnerInstance));

			//(SetComposableBasePointers(
			//	spInnerInspectable,
			//	spInnerFactory));

			UpdateEventSource();
		}

		internal void UpdateEventSource()
		{
			LoopingSelectorItemDataAutomationPeer spLSIDAP;

			GetDataAutomationPeer(out spLSIDAP);

			if (spLSIDAP is { })
			{
				SetEventSource(spLSIDAP);
			}

			return;
		}

		internal void UpdateItemIndex(int itemIndex)
		{
			LoopingSelectorItemDataAutomationPeer spLSIDAP;

			GetDataAutomationPeer(out spLSIDAP);

			if (spLSIDAP is { })
			{
				((LoopingSelectorItemDataAutomationPeer)spLSIDAP).SetItemIndex(itemIndex);
			}

			return;
		}

		internal void SetEventSource(LoopingSelectorItemDataAutomationPeer pLSIDAP)
		{
			AutomationPeer spLSIDAPAsAP;
			AutomationPeer spThisAsAP;

			//pLSIDAP.QueryInterface<AutomationPeer>(spLSIDAPAsAP);
			spLSIDAPAsAP = pLSIDAP;
			//(QueryInterface(
			//	__uuidof(AutomationPeer),
			//	&spThisAsAP));
			spThisAsAP = this;
			spThisAsAP.EventsSource = spLSIDAPAsAP;
		}

		void GetDataAutomationPeer(
			out LoopingSelectorItemDataAutomationPeer ppLSIDAP)
		{
			LoopingSelectorItem pOwnerNoRef = null;
			LoopingSelector pLoopingSelectorNoRef = null;
			//FrameworkElementAutomationPeerStatics spFEAPStatics;
			UIElement spLSAsUIE;
			AutomationPeer spLSAPAsAP;
			LoopingSelectorAutomationPeer spLSAP;
			LoopingSelectorAutomationPeer pLoopingSelectorAPNoRef = null;
			DependencyObject spItem;
			ContentControl spLSIAsCC;

			ppLSIDAP = null;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);

			if (pOwnerNoRef is { })
			{
				//(pOwnerNoRef.QueryInterface(
				//	__uuidof(IContentControl),
				//	&spLSIAsCC));
				spLSIAsCC = pOwnerNoRef;
				spItem = spLSIAsCC.Content as DependencyObject;

				// If we don't have an item yet, then we don't want to generate a data automation peer yet.
				// Otherwise, we'll insert an entry into our LoopingSelector's automation peer map
				// corresponding to a null item, which gets us into a bad state.
				// See LoopingSelectorAutomationPeer.GetDataAutomationPeerForItem().
				if (spItem is { })
				{
					pOwnerNoRef.GetParentNoRef(out pLoopingSelectorNoRef);

					//NT_global.System.Diagnostics.Debug.Assert(pLoopingSelectorNoRef);

					//(wf.GetActivationFactory(
					//	wrl_wrappers.Hstring(
					//		RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
					//	&spFEAPStatics));
					//(pLoopingSelectorNoRef.QueryInterface(
					//	__uuidof(UIElement),
					//	&spLSAsUIE));
					spLSAsUIE = pLoopingSelectorNoRef;
					//spFEAPStatics.CreatePeerForElement(spLSAsUIE, &spLSAPAsAP);
					spLSAPAsAP = FrameworkElementAutomationPeer.CreatePeerForElement(spLSAsUIE);

					//spLSAPAsAP.As(spLSAP);
					spLSAP = spLSAPAsAP as LoopingSelectorAutomationPeer;
					pLoopingSelectorAPNoRef = (LoopingSelectorAutomationPeer)(spLSAP);

					pLoopingSelectorAPNoRef.GetDataAutomationPeerForItem(spItem, out ppLSIDAP);
				}
			}

			return;
		}

		void GetOwnerAsInternalPtrNoRef(
			out LoopingSelectorItem ppOwnerNoRef)
		{

			UIElement spOwnerAsUIElement;
			LoopingSelectorItem spOwner;
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
				spOwner = spOwnerAsUIElement as LoopingSelectorItem;
				// No is passed back to the caller.
				ppOwnerNoRef = (LoopingSelectorItem)(spOwner);
			}

		}

		#region AutomationPeerOverrides

		protected override object GetPatternCore(PatternInterface patternInterface)
		{

			DependencyObject returnValue = default;
			if (patternInterface == PatternInterface.ScrollItem ||
				patternInterface == PatternInterface.SelectionItem)
			{
				returnValue = (LoopingSelectorItemAutomationPeer)(this);
				//AddRef();
			}
			else
			{
				//LoopingSelectorItemAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, returnValue);
			}

			return returnValue;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			AutomationControlType returnValue = default;

			//if (returnValue == null) throw new ArgumentNullException();
			returnValue = AutomationControlType.ListItem;

			return returnValue;

		}

		protected override string GetClassNameCore()
		{

			//if (returnValue == null) throw new ArgumentNullException();
			//wrl_wrappers.Hstring("LoopingSelectorItem").CopyTo(returnValue);
			return "LoopingSelectorItem";
		}

		protected override bool IsKeyboardFocusableCore()
		{
			bool returnValue = default;
			AutomationPeer loopingSelectorAP;

			returnValue = false;

			// LoopingSelectorItems aren't actually keyboard focusable,
			// but we need to give them automation focus in order to have
			// UIA clients like Narrator read their contents when they're
			// selected, so we'll act as though they're keyboard focusable
			// to enable that to be possible.  In order to do this,
			// for the keyboard focus status of a LoopingSelectorItem,
			// we'll just report the keyboard focus status of its parent LoopingSelector.
			GetParentAutomationPeer(out loopingSelectorAP);

			if (loopingSelectorAP is { })
			{
				returnValue = loopingSelectorAP.IsKeyboardFocusable();
			}

			return returnValue;
		}

		protected override bool HasKeyboardFocusCore()
		{
			AutomationPeer loopingSelectorAP;

			bool returnValue = false;

			// In order to support giving automation focus to selected LoopingSelectorItem
			// automation peers, we'll report that a LoopingSelectorItem has keyboard focus
			// if its parent LoopingSelector has keyboard focus, and if this LoopingSelectorItem
			// is selected.
			GetParentAutomationPeer(out loopingSelectorAP);

			if (loopingSelectorAP is { })
			{
				bool hasKeyboardFocus = false;

				hasKeyboardFocus = loopingSelectorAP.HasKeyboardFocus();

				if (hasKeyboardFocus)
				{
					returnValue = IsSelected;
				}
			}

			return returnValue;
		}


		#endregion

		#region IScrollItemProvider

		public void ScrollIntoView()
		{
			LoopingSelectorItem pOwnerNoRef = null;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);

			if (pOwnerNoRef is { })
			{
				LoopingSelector pOwnerParentNoRef = null;

				pOwnerNoRef.GetParentNoRef(out pOwnerParentNoRef);

				if (pOwnerParentNoRef is { })
				{
					ContentControl spOwnerAsContentControl;
					DependencyObject spContent;

					//pOwnerNoRef.QueryInterface(__uuidof(IContentControl), &spOwnerAsContentControl);
					spOwnerAsContentControl = pOwnerNoRef;
					spContent = spOwnerAsContentControl.Content as DependencyObject;
					pOwnerParentNoRef.AutomationTryScrollItemIntoView(spContent);
				}
			}

			return;
		}

		#endregion

		#region ISelectionItemProvider

		public bool IsSelected
		{
			get
			{
				bool value = default;
				LoopingSelectorItem pOwnerNoRef = null;
				GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
				if (pOwnerNoRef is { })
				{
					pOwnerNoRef.AutomationGetIsSelected(out value);
				}

				return value;
			}
		}

		public IRawElementProviderSimple SelectionContainer
		{
			get
			{
				IRawElementProviderSimple ppValue;

				LoopingSelectorItem pOwnerNoRef = null;
				AutomationPeer spAutomationPeer;

				ppValue = null;

				GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
				if (pOwnerNoRef is { })
				{
					pOwnerNoRef.AutomationGetSelectionContainerUIAPeer(out spAutomationPeer);
					if (spAutomationPeer is { })
					{
						//AutomationPeer spAutomationPeerAsProtected;
						IRawElementProviderSimple spProvider = default;

						//spAutomationPeer.As(spAutomationPeerAsProtected);
						//spAutomationPeerAsProtected = spAutomationPeer;
						//spProvider = spAutomationPeerAsProtected.ProviderFromPeer(spAutomationPeer);
						ppValue = spProvider; //.Detach();
					}
				}

				return ppValue;
			}
		}

		public void AddToSelection()
		{
			//return UIA_E_INVALIDOPERATION;
			throw new InvalidOperationException();
		}

		public void RemoveFromSelection()
		{
			//return UIA_E_INVALIDOPERATION;
			throw new InvalidOperationException();
		}

		public void Select()
		{

			LoopingSelectorItem pOwnerNoRef = null;
			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);
			if (pOwnerNoRef is { })
			{
				pOwnerNoRef.AutomationSelect();
			}



		}

		#endregion

		void
			GetParentAutomationPeer(
				out AutomationPeer parentAutomationPeer)
		{
			LoopingSelectorItem pOwnerNoRef = null;

			parentAutomationPeer = null;

			GetOwnerAsInternalPtrNoRef(out pOwnerNoRef);

			if (pOwnerNoRef is { })
			{
				LoopingSelector pLoopingSelectorNoRef = null;

				pOwnerNoRef.GetParentNoRef(out pLoopingSelectorNoRef);

				if (pLoopingSelectorNoRef is { })
				{
					LoopingSelector loopingSelector = pLoopingSelectorNoRef;
					UIElement loopingSelectorAsUIE;

					//IGNOREHR(loopingSelector.As(loopingSelectorAsUIE));
					loopingSelectorAsUIE = loopingSelector;

					if (loopingSelectorAsUIE is { })
					{
						AutomationPeer loopingSelectorAP;

						//(Private.AutomationHelper.CreatePeerForElement(
						//	loopingSelectorAsUIE,
						//	&loopingSelectorAP));
						loopingSelectorAP = AutomationHelper.CreatePeerForElement(loopingSelectorAsUIE);

						parentAutomationPeer = loopingSelectorAP; //.Detach();
					}
				}
			}

			return;
		}

	}
}
