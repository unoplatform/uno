namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	internal static void KeyboardAcceleratorFlyoutItemEnter(
		DependencyObject* element,
		DependencyObject* pNamescopeOwner,
		KnownPropertyIndex collectionPropertyIndex,
		EnterParams parameters)
	{
		CValue value;
		IFC_RETURN(element->GetValueByIndex(collectionPropertyIndex, &value));

		if (CMenuFlyoutItemBaseCollection * const items = do_pointer_cast<CMenuFlyoutItemBaseCollection>(value.AsObject()))

	{
        // This is a dead enter to register any keyboard accelerators that may be present in the MenuFlyout items
        // to the list of live accelerators
        params.fIsForKeyboardAccelerator = true;
        params.fIsLive = false;
        params.fSkipNameRegistration = true;
        params.fUseLayoutRounding = false;
        params.fCoercedIsEnabled = false;

			for (CDependencyObject* item : *items)
			{
				IFC_RETURN(item->Enter(pNamescopeOwner, params));
			}
		}

		return S_OK;
	}

	internal static void KeyboardAcceleratorFlyoutItemLeave(
		DependencyObject element,
		DependencyObject pNamescopeOwner,
		KnownPropertyIndex collectionPropertyIndex,
		LeaveParams parameters)
	{
		CValue value;
		IFC_RETURN(element->GetValueByIndex(collectionPropertyIndex, &value));

		if (CMenuFlyoutItemBaseCollection * const items = do_pointer_cast<CMenuFlyoutItemBaseCollection>(value.AsObject()))

	{
        // This is a dead leave to remove any keyboard accelerators that may be present in the MenuFlyout items
        // from the list of live accelerators
        params.fIsForKeyboardAccelerator = true;
        params.fIsLive = false;
        params.fSkipNameRegistration = true;
        params.fUseLayoutRounding = false;
        params.fCoercedIsEnabled = false;

			for (CDependencyObject* item : *items)
			{
				IFC_RETURN(item->Leave(pNamescopeOwner, params));
			}
		}
	}

	private void EnterImpl(DependencyObject namescopeOwner, EnterParams parameters)
	{
		//base.EnterImpl(pNamescopeOwner, params));
		IFC_RETURN(KeyboardAcceleratorFlyoutItemEnter(this, pNamescopeOwner, KnownPropertyIndex::MenuFlyout_Items, params));
	}

	private void LeaveImpl(DependencyObject namescopeOwner, LeaveParams parameters)
	{
		//base.LeaveImpl(pNamescopeOwner, params);
		IFC_RETURN(KeyboardAcceleratorFlyoutItemLeave(this, pNamescopeOwner, KnownPropertyIndex::MenuFlyout_Items, params));
	}
}
