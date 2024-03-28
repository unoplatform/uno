using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class LoopingSelectorItemDataAutomationPeer
	{
		internal LoopingSelectorItemDataAutomationPeer(
			DependencyObject pItem,
			LoopingSelectorAutomationPeer pOwner)
		{
			_itemIndex = -1;

			InitializeImpl(pItem, pOwner);
		}

		void InitializeImpl(
			DependencyObject pItem,
			LoopingSelectorAutomationPeer pOwner)
		{
			//AutomationPeerFactory spInnerFactory;
			//AutomationPeer spInnerInstance;
			//DependencyObject spInnerInspectable;

			//LoopingSelectorItemDataAutomationPeerGenerated.InitializeImpl(pItem, pOwner);
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_AutomationPeer),
			//	&spInnerFactory));

			//(spInnerFactory.CreateInstance(
			//	(LoopingSelectorItemDataAutomationPeer)(this),
			//	&spInnerInspectable,
			//	&spInnerInstance));

			//(SetComposableBasePointers(
			//	spInnerInspectable,
			//	spInnerFactory));

			SetParent(pOwner);
			SetItem(pItem);
		}

		void SetParent(LoopingSelectorAutomationPeer pOwner)
		{
			AutomationPeer spThisAsAP;

			//(QueryInterface(
			//	__uuidof(AutomationPeer),
			//	&spThisAsAP));
			spThisAsAP = this;

			//LoopingSelectorAutomationPeer(pOwner).AsWeak(_wrParent);
			_wrParent = new WeakReference<LoopingSelectorAutomationPeer>(pOwner);
			if (pOwner is { })
			{
				AutomationPeer spLSAsAP;
				//pOwner.QueryInterface<AutomationPeer>(spLSAsAP);
				spLSAsAP = pOwner;

				// NOTE: This causes an addmost likely, I think that's a good idea.
				spThisAsAP.SetParent(spLSAsAP);
			}
			else
			{
				spThisAsAP.SetParent(null);
			}
		}

		public void SetItem(DependencyObject pItem)
		{
			_tpItem = pItem;
		}

		internal void
			GetItem(out DependencyObject ppItem)
		{
			ppItem = null;
			//_tpItem.CopyTo(ppItem);
			ppItem = _tpItem;
		}

		public void SetItemIndex(int index)
		{
			_itemIndex = index;
		}

		void ThrowElementNotAvailableException()
		{
			//return UIA_E_INVALIDOPERATION;
			throw new InvalidOperationException();
		}

		void GetContainerAutomationPeer(
			out AutomationPeer ppContainer)
		{
			ppContainer = null;
			LoopingSelectorAutomationPeer spParent = default;
			//_wrParent.As(spParent);
			//spParent = _wrParent;
			//if (spParent is {})
			if (_wrParent?.TryGetTarget(out spParent) ?? false)
			{
				LoopingSelectorItemAutomationPeer spLSIAP;
				spParent.GetContainerAutomationPeerForItem(_tpItem, out spLSIAP);

				if (spLSIAP == null)
				{
					// If the item has not been realized, spLSIAP will be null.
					// Realize the item on demand now and try again
					Realize();
					spParent.GetContainerAutomationPeerForItem(_tpItem, out spLSIAP);
				}

				if (spLSIAP is { })
				{
					//spLSIAP.CopyTo(ppContainer);
					ppContainer = spLSIAP;
				}
			}
		}

#if false
		void
			RealizeImpl()
		{
			LoopingSelectorAutomationPeer spParent = default;
			//_wrParent.As(spParent);
			//if (spParent && _tpItem)
			if (_wrParent?.TryGetTarget(out spParent) ?? false)
			{
				spParent.RealizeItemAtIndex(_itemIndex);
			}
		}
#endif

		#region Method forwarders

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			DependencyObject returnValue = default;
			if (patternInterface == PatternInterface.VirtualizedItem)
			{
				returnValue = (LoopingSelectorItemDataAutomationPeer)this;
				//AddRef();
			}
			else
			{
				AutomationPeer spAutomationPeer;
				GetContainerAutomationPeer(out spAutomationPeer);
				if (spAutomationPeer is { })
				{
					returnValue = spAutomationPeer.GetPattern(patternInterface) as DependencyObject;
				}
				else
				{
					//LoopingSelectorItemDataAutomationPeerGenerated.GetPatternCoreImpl(patternInterface, out returnValue);
				}
			}

			return returnValue;
		}

		protected override string GetAcceleratorKeyCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetAcceleratorKey();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetAccessKeyCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetAccessKey();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()

		{
			AutomationControlType returnValue;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetAutomationControlType();
			}
			else
			{
				returnValue = AutomationControlType.ListItem;
			}

			return returnValue;
		}

		protected override string GetAutomationIdCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetAutomationId();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override Rect GetBoundingRectangleCore()
		{
			Rect returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetBoundingRectangle();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override IList<AutomationPeer> GetChildrenCore()
		{
			IList<AutomationPeer> returnValue;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetChildren();
			}
			else
			{
				returnValue = null;
			}

			return returnValue;
		}

		protected override string GetClassNameCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetClassName();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override Point GetClickablePointCore()
		{
			Point returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetClickablePoint();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetHelpTextCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetHelpText();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetItemStatusCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetItemStatus();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetItemTypeCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetItemType();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override AutomationPeer GetLabeledByCore()
		{
			AutomationPeer returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLabeledBy();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLocalizedControlType();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetNameCore()
		{
			string returnValue = default;

			if (_tpItem is { })
			{
				returnValue = AutomationHelper.GetPlainText(_tpItem);
			}

			return returnValue;
		}

		protected override AutomationOrientation GetOrientationCore()
		{
			AutomationOrientation returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetOrientation();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override AutomationLiveSetting GetLiveSettingCore()
		{
			AutomationLiveSetting returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLiveSetting();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override IReadOnlyList<AutomationPeer> GetControlledPeersCore()
		{
			IReadOnlyList<AutomationPeer> returnValue;
			returnValue = null;

			return returnValue;
		}

		protected override bool HasKeyboardFocusCore()
		{
			bool returnValue;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.HasKeyboardFocus();
			}
			else
			{
				returnValue = false;
			}

			return returnValue;
		}


		protected override bool IsContentElementCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsContentElement();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override bool IsControlElementCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsControlElement();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override bool IsEnabledCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsEnabled();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override bool IsKeyboardFocusableCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsKeyboardFocusable();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override bool IsOffscreenCore()
		{
			bool returnValue;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsOffscreen();
			}
			else
			{
				returnValue = true;
			}

			return returnValue;
		}

		protected override bool IsPasswordCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsPassword();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override bool IsRequiredForFormCore()
		{
			bool returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.IsRequiredForForm();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override void SetFocusCore()
		{
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);
			if (spAutomationPeer is { })
			{
				spAutomationPeer.SetFocus();
			}
		}

		protected override IList<AutomationPeerAnnotation> GetAnnotationsCore()
		{
			IList<AutomationPeerAnnotation> returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetAnnotations();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override int GetPositionInSetCore()
		{
			int returnValue = -1;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetPositionInSet();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override int GetSizeOfSetCore()
		{
			int returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetSizeOfSet();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override int GetLevelCore()
		{
			int returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLevel();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override AutomationLandmarkType GetLandmarkTypeCore()
		{
			AutomationLandmarkType returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLandmarkType();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		protected override string GetLocalizedLandmarkTypeCore()
		{
			string returnValue = default;
			AutomationPeer spAutomationPeer;
			GetContainerAutomationPeer(out spAutomationPeer);

			if (spAutomationPeer is { })
			{
				returnValue = spAutomationPeer.GetLocalizedLandmarkType();
			}
			else
			{
				ThrowElementNotAvailableException();
			}

			return returnValue;
		}

		#endregion
	}
}
