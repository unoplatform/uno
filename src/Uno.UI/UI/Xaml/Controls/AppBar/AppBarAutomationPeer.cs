using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AppBarAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, IExpandCollapseProvider, IWindowProvider
	{
		public AppBarAutomationPeer(AppBar owner) : base(owner)
		{
		}

		public ToggleState ToggleState
		{
			get
			{
				bool isOpen = false;
				var pOwner = Owner as AppBar;
				isOpen = pOwner.IsOpen;

				return isOpen ? ToggleState.On : ToggleState.Off;
			}
		}

		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				bool isOpen = false;
				var pOwner = Owner as AppBar;
				isOpen = pOwner.IsOpen;

				return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
			}
		}

		public bool IsModal
		{
			get
			{
				return true;
			}
		}

		public bool IsTopmost
		{
			get
			{
				return true;
			}
		}

		public bool Maximizable
		{
			get
			{
				return false;
			}
		}

		public bool Minimizable
		{
			get
			{
				return false;
			}
		}

		public WindowInteractionState InteractionState
		{
			get
			{
				return WindowInteractionState.Running;
			}
		}

		public WindowVisualState VisualState
		{
			get
			{
				return WindowVisualState.Normal;
			}
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			bool shouldReturnWindowPattern = false;
			object ppReturnValue = null;

			if (patternInterface == PatternInterface.Window)
			{
				var owner = Owner as AppBar;
				var isOpen = owner.IsOpen;

				shouldReturnWindowPattern = isOpen;
			}

			if ((patternInterface == PatternInterface.ExpandCollapse) ||
				(patternInterface == PatternInterface.Toggle) ||
				(shouldReturnWindowPattern))
			{
				ppReturnValue = this;
			}
			else
			{
				ppReturnValue = base.GetPatternCore(patternInterface);
			}

			return ppReturnValue;
		}

		protected override string GetClassNameCore()
		{
			return "ApplicationBar";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.AppBar;
		}

		public void Toggle()
		{
			bool bIsEnabled;
			bool isOpen = false;

			bIsEnabled = IsEnabled();
			if (!bIsEnabled)
			{
				//UIA_E_ELEMENTNOTENABLED;
				throw new ElementNotEnabledException();
			}

			var pOwner = Owner as AppBar;
			isOpen = pOwner.IsOpen;
			pOwner.IsOpen = !isOpen;
		}

		public void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
		{
			//HRESULT hr = S_OK;
			//xaml_automation::ToggleState oldValue;
			//xaml_automation::ToggleState newValue;
			//CValue valueOld;
			//CValue valueNew;

			//IFC(ToggleButtonAutomationPeer::ConvertToToggleState(pOldValue, &oldValue))

			//IFC(ToggleButtonAutomationPeer::ConvertToToggleState(pNewValue, &newValue))


			//if (oldValue != newValue)
			//{
			//	IFC(CValueBoxer::BoxEnumValue(&valueOld, oldValue));
			//	IFC(CValueBoxer::BoxEnumValue(&valueNew, newValue));

			//	IFC(::AutomationRaiseAutomationPropertyChanged(static_cast<CAutomationPeer*>(GetHandle()), UIAXcp::APAutomationProperties::APToggleStateProperty, valueOld, valueNew));
			//}
		}

		public void RaiseExpandCollapseAutomationEvent(bool isOpen)
		{
			//HRESULT hr = S_OK;

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

			//IFC(CValueBoxer::BoxEnumValue(&valueOld, oldValue));
			//IFC(CValueBoxer::BoxEnumValue(&valueNew, newValue));
			//IFC(::AutomationRaiseAutomationPropertyChanged(static_cast<CAutomationPeer*>(GetHandle()), UIAXcp::APAutomationProperties::APExpandCollapseStateProperty, valueOld, valueNew));
		}

		public void Expand()
		{
			bool bIsEnabled;

			bIsEnabled = IsEnabled();
			if (!bIsEnabled)
			{
				//UIA_E_ELEMENTNOTENABLED;
				throw new ElementNotEnabledException();
			}

			var pOwner = Owner as AppBar;
			pOwner.IsOpen = true;
		}

		public void Collapse()
		{
			bool bIsEnabled;

			bIsEnabled = IsEnabled();
			if (!bIsEnabled)
			{
				//UIA_E_ELEMENTNOTENABLED;
				throw new ElementNotEnabledException();
			}

			var pOwner = Owner as AppBar;
			pOwner.IsOpen = false;
		}

		public void Close()
		{

		}

		public void SetVisualState(global::Microsoft.UI.Xaml.Automation.WindowVisualState state)
		{

		}


		public bool WaitForInputIdle(int milliseconds)
		{
			return false;
		}
	}
}
