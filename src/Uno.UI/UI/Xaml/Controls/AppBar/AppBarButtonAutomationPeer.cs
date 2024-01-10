using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AppBarButtonAutomationPeer : global::Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer, global::Microsoft.UI.Xaml.Automation.Provider.IExpandCollapseProvider
	{
		public AppBarButtonAutomationPeer(AppBarButton owner) : base(owner)
		{

		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			var owner = GetOwningAppBarButton();
			object ppReturnValue = null;

			if (patternInterface == PatternInterface.ExpandCollapse)
			{
				// We specifically want to *not* report that we support the expand/collapse pattern when we don't have an attached flyout,
				// because then we have nothing to expand or collapse.  So we unconditionally enter this block even if owner->m_menuHelper
				// is null so we'll get the default null return value in that case.
				if (owner.MenuHelper is { } menuHelper)
				{
					ppReturnValue = this;
				}
			}
			else
			{
				ppReturnValue = base.GetPatternCore(patternInterface);
			}

			return ppReturnValue;
		}

		protected override string GetClassNameCore()
		{
			return "AppBarButton";
		}

		protected override string GetNameCore()
		{
			// Note: We are calling FrameworkElementAutomationPeer::GetNameCore here, rather than going through
			// any of our own immediate superclasses, to avoid the logic in ButtonBaseAutomationPeer that will
			// substitute Content for the automation name if the latter is unset -- we want to either get back
			// the actual value of AutomationProperties.Name if it has been set, or null if it hasn't.

			//UNO ONLY: ButtonBaseAutomationPeer doesn't substitue Content for automation name if it is unset
			var returnValue = base.GetNameCore();

			if (string.IsNullOrWhiteSpace(returnValue))
			{
				var owner = GetOwningAppBarButton();
				returnValue = owner.Label;
			}

			return returnValue;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_APPBAR_BUTTON");
		}

		protected override string GetAcceleratorKeyCore()
		{
			var returnValue = base.GetAcceleratorKeyCore();

			if (!string.IsNullOrWhiteSpace(returnValue))
			{
				// If AutomationProperties.AcceleratorKey hasn't been set, then return the value of our KeyboardAcceleratorTextOverride property.
				var owner = GetOwningAppBarButton();
				returnValue = owner.KeyboardAcceleratorTextOverride?.Trim();
			}

			return returnValue;
		}

		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				bool isOpen = false;
				var owner = GetOwningAppBarButton();
				if (owner.MenuHelper is { } menuHelper)
				{
					isOpen = ((ISubMenuOwner)owner).IsSubMenuOpen;
				}

				return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
			}
		}

		public void Collapse()
		{
			var owner = GetOwningAppBarButton();
			if (owner.MenuHelper is { } menuHelper)
			{
				menuHelper.CloseSubMenu();
			}
		}

		public void Expand()
		{
			var owner = GetOwningAppBarButton();
			if (owner.MenuHelper is { } menuHelper)
			{
				menuHelper.OpenSubMenu();
			}
		}

		public void RaiseExpandCollapseAutomationEvent(bool isOpen)
		{
			//xaml_automation::ExpandCollapseState oldValue;
			//xaml_automation::ExpandCollapseState newValue;
			//CValue valueOld;
			//CValue valueNew;

			//// Converting isOpen to appropriate enumerations
			//if (isOpen)
			//{
			//	oldValue = xaml_automation::ExpandCollapseState::ExpandCollapseState_Collapsed;
			//	newValue = xaml_automation::ExpandCollapseState::ExpandCollapseState_Expanded;
			//}
			//else
			//{
			//	oldValue = xaml_automation::ExpandCollapseState::ExpandCollapseState_Expanded;
			//	newValue = xaml_automation::ExpandCollapseState::ExpandCollapseState_Collapsed;
			//}

			//IFC_RETURN(CValueBoxer::BoxEnumValue(&valueOld, oldValue));
			//IFC_RETURN(CValueBoxer::BoxEnumValue(&valueNew, newValue));
			//IFC_RETURN(::AutomationRaiseAutomationPropertyChanged(static_cast<CAutomationPeer*>(GetHandle()), UIAXcp::APAutomationProperties::APExpandCollapseStateProperty, valueOld, valueNew));
			//return S_OK;
		}

		private AppBarButton GetOwningAppBarButton() => Owner as AppBarButton;
	}
}
