// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\Flyout.cpp, tag winui3/release/1.4.3, commit 685d2bf

namespace Windows.UI.Xaml.Controls;

// TODO:MZ: Ensure ContentAttribute is not removed
partial class Flyout
{
	private void EnterImpl(DependencyObject pNamescopeOwner, EnterParams parameters)
	{

		base.EnterImpl(pNamescopeOwner, parameters);

		CValue value;
		IFC_RETURN(GetValueByIndex(KnownPropertyIndex::Flyout_Content, &value));
		CUIElement * const pContent = do_pointer_cast<CUIElement>(value.AsObject());

		if (pContent != null)
		{
			//This is a dead enter to register any keyboard accelerators that may be present in the Flyout Content 
			//to the list of live accelerators
			params.fIsForKeyboardAccelerator = true;
			params.fIsLive = false;
			params.fSkipNameRegistration = true;
			params.fUseLayoutRounding = false;
			params.fCoercedIsEnabled = false;
			pContent.Enter(pNamescopeOwner, parameters);
		}
	}

	private void LeaveImpl(DependencyObject pNamescopeOwner, LeaveParams parameters)
	{
		base.LeaveImpl(pNamescopeOwner, parameters);

		CValue value;
		IFC_RETURN(GetValueByIndex(KnownPropertyIndex::Flyout_Content, &value));
		CUIElement * const pContent = do_pointer_cast<CUIElement>(value.AsObject());
		if (pContent != null)
		{
			//This is a dead leave to remove any keyboard accelerators that may be present in the Flyout Content
			//from the list of live accelerators
			parameters.fIsForKeyboardAccelerator = true;
			parameters.fIsLive = false;
			parameters.fSkipNameRegistration = true;
			parameters.fUseLayoutRounding = false;
			parameters.fCoercedIsEnabled = false;
			pContent.Leave(pNamescopeOwner, parameters);
		}
	}
}
