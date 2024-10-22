using System.Linq;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Data;

partial class CollectionViewGroup
{
	//	CollectionViewGroup::CollectionViewGroup()
	//{
	//}

	//CollectionViewGroup::~CollectionViewGroup()
	//{
	//	if (m_epVectorChangedHandler)
	//	{
	//		auto spObservableItems = m_tpObservableItems.GetSafeReference();
	//		if (spObservableItems)
	//		{
	//			VERIFYHR(m_epVectorChangedHandler.DetachEventHandler(spObservableItems.Get()));
	//		}
	//	}
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::get_GroupImpl(_Outptr_ IInspectable** pValue)
	//{
	//    RRETURN(m_tpGroup.CopyTo(pValue));
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::get_GroupItemsImpl(_Outptr_ wfc::IObservableVector<IInspectable*>** pValue)
	//{
	//    RRETURN(m_tpGroupItems.CopyTo(pValue));
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::get_TypeImpl(_Out_ wxaml_interop::TypeName* pValue)
	//{
	//    HRESULT hr = S_OK;
	//wxaml_interop::TypeName typeName = { };

	//typeName.Kind = wxaml_interop::TypeKind_Metadata;
	//IFC(WindowsCreateString(STR_LEN_PAIR(L"Microsoft.UI.Xaml.Data.ICollectionViewGroup"), &typeName.Name));

	//*pValue = typeName;

	//Cleanup:
	//RRETURN(hr);
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::GetStringRepresentationImpl(_Out_ HSTRING* returnValue)
	//{
	//    if (m_tpGroup)
	//    {
	//        IFC_RETURN(FrameworkElement::GetStringFromObject(m_tpGroup.Get(), returnValue));
	//    }

	//	else
	//{

	//	IFC_RETURN(WindowsCreateString(STR_LEN_PAIR(L"Microsoft.UI.Xaml.Data.CollectionViewGroup"), returnValue));
	//}

	//return S_OK;
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::GetCustomPropertyImpl(_In_ HSTRING name, _Outptr_ xaml_data::ICustomProperty** returnValue)
	//{
	//    HRESULT hr = S_OK;

	//if (wrl_wrappers::HStringReference(L"Group") == name)
	//{
	//	hr = CustomProperty::CreateObjectProperty(
	//	name,
	//		&CollectionViewGroup::GetGroupPropertyValue,
	//		returnValue);
	//}
	//else if (wrl_wrappers::HStringReference(L"GroupItems") == name)
	//{
	//	hr = CustomProperty::CreateObjectProperty(
	//		name,
	//		&CollectionViewGroup::GetGroupItemsPropertyValue,
	//		returnValue);
	//}
	//IFC(hr);

	//Cleanup:
	//RRETURN(hr);
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::GetGroupPropertyValue(_In_ IInspectable* pTarget, _Outptr_ IInspectable** ppValue)
	//{
	//    HRESULT hr = S_OK;
	//ctl::ComPtr<xaml_data::ICollectionViewGroup> spCVG;
	//ctl::ComPtr<IInspectable> spTarget = pTarget;

	//IFC(spTarget.As(&spCVG));
	//IFC(spCVG.Cast<CollectionViewGroup>()->m_tpGroup.CopyTo(ppValue));

	//Cleanup:
	//RRETURN(hr);
	//}

	//_Check_return_ HRESULT
	//CollectionViewGroup::GetGroupItemsPropertyValue(_In_ IInspectable* pTarget, _Outptr_ IInspectable** ppValue)
	//{
	//    HRESULT hr = S_OK;
	//ctl::ComPtr<xaml_data::ICollectionViewGroup> spCVG;
	//ctl::ComPtr<IInspectable> spTarget = pTarget;

	//IFC(spTarget.As(&spCVG));
	//IFC(spCVG.Cast<CollectionViewGroup>()->m_tpGroupItems.CopyTo(ppValue));

	//Cleanup:
	//RRETURN(hr);
	//}

	private void SetSource(object pSource, object pItems, GroupedDataCollectionView pOwner)
	{
		HRESULT hr = S_OK;
		ctl::ComPtr<wfc::IObservableVector<IInspectable*>> spItems;
		XBOOL fSourceIsObservable = FALSE;

		// Store the name in the Name DP
		SetPtrValue(m_tpGroup, pSource);

		// Get the observable vector of items, if there are no items then
		// we will end up with an empty collection
		IFC(GetObservableVector(pItems, &spItems, &fSourceIsObservable));

		// Store the observable collection in the property
		SetPtrValue(m_tpGroupItems, spItems.Get());

		// TODO: Listen for changes if the source is observable to notify the owner
		if (fSourceIsObservable)
		{
			SetPtrValue(m_tpObservableItems, spItems);

			IFC(m_epVectorChangedHandler.AttachEventHandler(m_tpObservableItems.Get(),
				[this](wfc::IObservableVector < IInspectable *> *pSender, wfc::IVectorChangedEventArgs * pArgs)


				{
				return OnItemsVectorChanged(pArgs);
			}));

			IFC(ctl::AsWeak(pOwner, &m_spOwnerRef));
		}

	Cleanup:
		RRETURN(hr);
	}

	private void ClearOwner()
	{
		// Forego the weak reference all together
		if (m_spOwnerRef is not null)
		{
			WeakReferencePool.ReturnWeakReference(this, m_spOwnerRef);
			m_spOwnerRef = null;
		}
	}

	private void GetObservableVector(
		object pSource,
		out IObservableVector<object> ppObservableVector,
		out bool pfSourceIsObservable)
	{

		HRESULT hr = S_OK;
		wfc::IVector<IInspectable*>* pVector = NULL;
		wfc::IObservableVector<IInspectable*>* pResult = NULL;
		IBindableObservableVector* pBindableObservable = NULL;
		IBindableVector* pItemsBindableVector = NULL;
		INotifyCollectionChanged* pINCC = NULL;
		ReadOnlyObservableTrackerCollection<IInspectable*>* pReadOnlyObservable = NULL;

		// Now try to get an observable vector from the items, if that can't be done then
		// we will manufacture one by reading the items from the source
		pResult = ctl::query_interface<wfc::IObservableVector<IInspectable*>>(pSource);
		if (pResult == NULL)
		{
			pItemsBindableVector = ctl::query_interface<IBindableVector>(pSource);
			if (pItemsBindableVector != NULL)
			{
				pINCC = ctl::query_interface<INotifyCollectionChanged>(pItemsBindableVector);
				if (pINCC != NULL)
				{
					IFC(BindableObservableVectorWrapper::CreateInstance(pItemsBindableVector, pINCC, &pVector));
					IFC(ctl::do_query_interface(pResult, pVector));
				}
				else if ((pBindableObservable = ctl::query_interface<IBindableObservableVector>(pSource)) != NULL)
				{
					IFC(BindableObservableVectorWrapper::CreateInstance(pBindableObservable, &pVector));
					IFC(ctl::do_query_interface(pResult, pVector));
				}
			}
		}

		if (pResult)
		{
			// If the source is observable then remember that
			*pfSourceIsObservable = TRUE;
		}
		else
		{
			// The source is not observable, we need to create an observable vector anyway but remember so
			// so we don't listen for changes that will never happen
			IFC(ctl::ComObject < ReadOnlyObservableTrackerCollection < IInspectable *>>::CreateInstance(&pReadOnlyObservable));
			IFC(InitializeReadOnlyCollectionFromIterable(pReadOnlyObservable, pSource));

			pResult = pReadOnlyObservable;
			pReadOnlyObservable = NULL;

			*pfSourceIsObservable = FALSE;
		}

		// Return the new vector
		*ppObservableVector = pResult;
		pResult = NULL;

	Cleanup:

		ReleaseInterface(pVector);
		ReleaseInterface(pResult);
		ReleaseInterface(pBindableObservable);
		ReleaseInterface(pItemsBindableVector);
		ReleaseInterface(pINCC);
		ctl::release_interface(pReadOnlyObservable);

		RRETURN(hr);
	}

	private void OnItemsVectorChanged(IVectorChangedEventArgs args)
	{
		ctl::ComPtr<ICollectionView> spOwner;

		// If we can't notify the owner then we're done
		if (!m_spOwnerRef)
		{
			goto Cleanup;
		}

		IFC(m_spOwnerRef.As(&spOwner));
		if (!spOwner)
		{
			goto Cleanup;
		}

		// Notify the owner
		IFC(spOwner.Cast<GroupedDataCollectionView>()->OnGroupItemsChanged(this, pArgs));
	}
}
