// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference DatePickerAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes DatePicker types to Microsoft UI Automation.
/// </summary>
public partial class DatePickerAutomationPeer : FrameworkElementAutomationPeer
{
	public DatePickerAutomationPeer(DatePicker owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(DatePicker);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;

	//IFACEMETHODIMP DatePickerAutomationPeer::GetNameCore(_Out_ HSTRING* returnValue)
	//{
	//	XUINT32 pLength = 0;

	//	IFC_RETURN(DatePickerAutomationPeerGenerated::GetNameCore(returnValue));

	//	pLength = WindowsGetStringLen(*returnValue);
	//	if (pLength == 0)
	//	{
	//		ctl::ComPtr<xaml::IUIElement> spOwnerAsUIE;
	//		ctl::ComPtr<IInspectable> spHeaderAsInspectable;

	//		DELETE_STRING(*returnValue);
	//		*returnValue = nullptr;

	//		IFC_RETURN(get_Owner(&spOwnerAsUIE));

	//		IFC_RETURN(spOwnerAsUIE.Cast<DatePicker>()->get_Header(&spHeaderAsInspectable));
	//		if (spHeaderAsInspectable)
	//		{
	//			IFC_RETURN(FrameworkElement::GetStringFromObject(spHeaderAsInspectable.Get(), returnValue));
	//			pLength = WindowsGetStringLen(*returnValue);
	//		}

	//		if (pLength == 0)
	//		{
	//			DELETE_STRING(*returnValue);
	//			*returnValue = nullptr;
	//			IFC_RETURN(DXamlCore::GetCurrentNoCreate()->GetLocalizedResourceString(UIA_AP_DATEPICKER, returnValue));
	//		}
	//	}

	//	return S_OK;
	//}
}
