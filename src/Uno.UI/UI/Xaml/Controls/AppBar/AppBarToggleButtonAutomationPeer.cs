using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using AppBarToggleButton = Microsoft.UI.Xaml.Controls.AppBarToggleButton;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class AppBarToggleButtonAutomationPeer : global::Microsoft.UI.Xaml.Automation.Peers.ToggleButtonAutomationPeer
	{
		public AppBarToggleButtonAutomationPeer(AppBarToggleButton owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "AppBarToggleButton";
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
				var owner = GetOwningAppBarToggleButton();
				returnValue = owner.Label;
			}

			return returnValue;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_APPBAR_TOGGLEBUTTON");
		}

		protected override string GetAcceleratorKeyCore()
		{
			var returnValue = base.GetAcceleratorKeyCore();

			if (string.IsNullOrWhiteSpace(returnValue))
			{
				// If AutomationProperties.AcceleratorKey hasn't been set, then return the value of our KeyboardAcceleratorTextOverride property.
				var owner = GetOwningAppBarToggleButton();
				var keyboardAcceleratorTextOverride = owner.KeyboardAcceleratorTextOverride;

				returnValue = keyboardAcceleratorTextOverride?.Trim();
			}

			return returnValue;
		}

		public new void Toggle()
		{
			var isEnabled = IsEnabled();
			if (!isEnabled)
			{
				throw new ElementNotEnabledException();
			}

			var owner = GetOwningAppBarToggleButton();
			owner.AutomationToggleButtonOnToggle();
		}

		public new ToggleState ToggleState
		{
			get
			{
				var owner = GetOwningAppBarToggleButton();
				var isChecked = owner.IsChecked;

				if (isChecked == null)
				{
					return ToggleState.Indeterminate;
				}
				else
				{
					if (isChecked.Value)
					{
						return ToggleState.On;
					}
					else
					{
						return ToggleState.Off;
					}
				}
			}
		}

		private AppBarToggleButton GetOwningAppBarToggleButton()
		{
			var owner = Owner;
			return owner as AppBarToggleButton;
		}
	}
}
