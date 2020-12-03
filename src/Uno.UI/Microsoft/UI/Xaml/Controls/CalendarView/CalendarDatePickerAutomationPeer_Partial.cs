// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		//CalendarDatePickerAutomationPeerFactory.CreateInstanceWithOwnerImpl
		IMPLEMENT_CONTROL_AUTOMATIONPEERFACTORY_CREATEINSTANCE(CalendarDatePicker)

		// Initializes a new instance of the CalendarDatePickerAutomationPeer class.
		CalendarDatePickerAutomationPeer.CalendarDatePickerAutomationPeer()
		{
		}

		// Deconstructor
		CalendarDatePickerAutomationPeer.~CalendarDatePickerAutomationPeer()
		{
		}

		IFACEMETHODIMP CalendarDatePickerAutomationPeer.GetPatternCore(xaml_automation_peers.PatternInterface patternInterface, _Outptr_ IInspectable** ppReturnValue)
		{
			HRESULT hr = S_OK;
			IFCPTR(ppReturnValue);
			*ppReturnValue = NULL;

			if (patternInterface == xaml_automation_peers.PatternInterface_Invoke
				|| patternInterface == xaml_automation_peers.PatternInterface_Value)
			{
				*ppReturnValue = ctl.as_iinspectable(this);
				ctl.addref_interface(this);
			}
			else
			{
				IFC(CalendarDatePickerAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue));
			}

			Cleanup:
			RRETURN(hr);
		}

		IFACEMETHODIMP CalendarDatePickerAutomationPeer.GetClassNameCore(out HSTRING* returnValue)
		{
			RRETURN(wrl_wrappers.Hstring(STR_LEN_PAIR("CalendarDatePicker")).CopyTo(returnValue));
		}

		IFACEMETHODIMP CalendarDatePickerAutomationPeer.GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType* pReturnValue)
		{
			*pReturnValue = xaml_automation_peers.AutomationControlType_Button;
			RRETURN(S_OK);
		}

		IFACEMETHODIMP CalendarDatePickerAutomationPeer.GetLocalizedControlTypeCore(out HSTRING* returnValue)
		{
			IFC_RETURN(DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AP_CALENDARDATEPICKER, returnValue));

			return S_OK;
		}

		_Check_return_ HRESULT CalendarDatePickerAutomationPeer.InvokeImpl()
		{
			HRESULT hr = S_OK;
			xaml.IUIElement* pOwner = NULL;
			BOOLEAN bIsEnabled;

			IFC(IsEnabled(&bIsEnabled));
			if (!bIsEnabled)
			{
				IFC(UIA_E_ELEMENTNOTENABLED);
			}

			IFC(get_Owner(&pOwner));
			IFCPTR(pOwner);
			IFC(((CalendarDatePicker*)(pOwner)).put_IsCalendarOpen(TRUE));

			Cleanup:
			ReleaseInterface(pOwner);
			RRETURN(hr);
		}

		_Check_return_ HRESULT CalendarDatePickerAutomationPeer.get_IsReadOnlyImpl(out BOOLEAN* value)
		{
			*value = TRUE;
			return S_OK;
		}

		_Check_return_ HRESULT CalendarDatePickerAutomationPeer.get_ValueImpl(out HSTRING* value)
		{
			ctl.ComPtr<xaml.IUIElement> spOwner;
			IFC_RETURN(get_Owner(&spOwner));

			var ownerItem = spOwner.AsOrNull<CalendarDatePicker>();
			IFCPTR_RETURN(ownerItem);

			IFC_RETURN(ownerItem.GetCurrentFormattedDate(value));

			return S_OK;
		}

		_Check_return_ HRESULT CalendarDatePickerAutomationPeer.SetValueImpl(HSTRING value)
		{
			return E_NOTIMPL;
		}
	}
}
