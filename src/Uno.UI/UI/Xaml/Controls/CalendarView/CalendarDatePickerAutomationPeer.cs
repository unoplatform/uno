// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using DirectUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//CalendarDatePickerAutomationPeerFactory.CreateInstanceWithOwnerImpl
//IMPLEMENT_CONTROL_AUTOMATIONPEERFACTORY_CREATEINSTANCE(CalendarDatePicker)

// Initializes a new instance of the CalendarDatePickerAutomationPeer class.
namespace Windows.UI.Xaml.Automation.Peers
{

	partial class CalendarDatePickerAutomationPeer : FrameworkElementAutomationPeer
	{
		private const string UIA_AP_CALENDARDATEPICKER = nameof(UIA_AP_CALENDARDATEPICKER);

		public CalendarDatePickerAutomationPeer(Controls.CalendarDatePicker owner) : base(owner)
		{
		}

		private void GetPatternCore(PatternInterface patternInterface,
			out DependencyObject ppReturnValue)
		{
			ppReturnValue = null;

			if (patternInterface == PatternInterface.Invoke
				|| patternInterface == PatternInterface.Value)
			{
				//ppReturnValue = ctl.as_iinspectable(this);
				ppReturnValue = this;
				//ctl.addref_interface(this);
			}
			else
			{
				//CalendarDatePickerAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue);
				ppReturnValue = base.GetPatternCore(patternInterface) as DependencyObject;
			}

		}

		protected override string GetClassNameCore()
		{
			return nameof(CalendarDatePicker);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Button;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AP_CALENDARDATEPICKER);
		}

		public void Invoke()
		{
			UIElement pOwner = null;
			bool bIsEnabled;

			bIsEnabled = IsEnabled();
			if (!bIsEnabled)
			{
				//UIA_E_ELEMENTNOTENABLED;
				throw new ElementNotEnabledException();
			}

			pOwner = Owner;
			((CalendarDatePicker)(pOwner)).IsCalendarOpen = true;
		}

		private void get_IsReadOnlyImpl(out bool value)
		{
			value = true;
			return;
		}

		private void get_ValueImpl(out string value)
		{
			UIElement spOwner;
			spOwner = Owner;

			var ownerItem = spOwner as CalendarDatePicker;
			//IFCPTR_RETURN(ownerItem);
			if (ownerItem == null)
			{
				throw new ArgumentNullException();
			}

			ownerItem.GetCurrentFormattedDate(out value);

			return;
		}

		public void SetValueImpl(string value)
		{
			throw new NotImplementedException();
		}
	}
}
