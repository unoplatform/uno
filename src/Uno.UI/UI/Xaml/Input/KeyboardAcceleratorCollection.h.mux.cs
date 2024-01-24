// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\CKeyboardAcceleratorCollection.h, 

namespace Microsoft.UI.Xaml.Input;

internal class KeyboardAcceleratorCollection : DependencyObjectCollection<KeyboardAccelerator>
{
	_Check_return_ HRESULT EnterImpl(_In_ CDependencyObject *pNamescopeOwner, EnterParams params) override
    {

        IFC_RETURN(__super::EnterImpl(pNamescopeOwner, params));

        if (params.fIsLive || params.fIsForKeyboardAccelerator)
        {
            CContentRoot* pContentRoot = VisualTree::GetContentRootForElement(this);
            if (nullptr != pContentRoot)
            {
                pContentRoot->AddToLiveKeyboardAccelerators(this);
}
        }
        return S_OK;
    }

    _Check_return_ HRESULT LeaveImpl(_In_ CDependencyObject *pNamescopeOwner, LeaveParams params) override
    {
		IFC_RETURN(__super::LeaveImpl(pNamescopeOwner, params));
		if (params.fIsLive || params.fIsForKeyboardAccelerator)
				{
					CContentRoot *pContentRoot = VisualTree::GetContentRootForElement(this);
		if (nullptr != pContentRoot)
		{
			pContentRoot->RemoveFromLiveKeyboardAccelerators(this);
		}
				}
				return S_OK;
    }
}
