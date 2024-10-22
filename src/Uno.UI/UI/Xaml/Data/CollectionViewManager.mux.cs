using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Interop;

namespace Uno.UI.Xaml.Data;

internal class CollectionViewManager
{
	internal static ICollectionView GetViewRecord(
		object source,
		CollectionViewSource collectionViewSource)
	{
		ICollectionView result;

		// First thing to try is to see if the source is a collection view factory
		// in which case we need to call it and defer all of the smarts about grouping
		// to it. If not then we will try to create a suitable collection view.
		if (source is ICollectionViewFactory collectionViewFactory)
		{
			result = collectionViewFactory.CreateView();
			goto Cleanup;
		}

		var isSourceGrouped = collectionViewSource.IsSourceGrouped;

		if (isSourceGrouped)
		{
			var itemsPath = collectionViewSource.ItemsPath;
			result = CreateGroupedView(source, itemsPath);
		}
		else
		{
			result = CreateNewView(source);
		}

	Cleanup:
		return result;
	}

	private static ICollectionView CreateNewView(object source)
	{
		wfc::IVector<IInspectable*>* pVector = NULL;
		wfc::IVectorView<IInspectable*>* pVectorView = NULL;
		wfc::IIterable<IInspectable*>* pIterable = NULL;
		IBindableVectorView* pBindableVectorView = NULL;
		IBindableVector* pBindableVector = NULL;
		IBindableIterable* pBindableIterable = NULL;
		INotifyCollectionChanged* pINCC = NULL;

		*ppResult = NULL;

		// First try Vector and VectorView
		// Vector interfaces IVector<IInspectable *> and IBindableVector
		if (ctl::is < wfc::IVector < IInspectable *>> (pSource))
		{
			IFC(GetVectorFromSource(pSource, &pVector));

			IFC(VectorCollectionView::CreateInstance(pVector, ppResult));
		}
		else if ((pBindableVector = ctl::query_interface<IBindableVector>(pSource)) != NULL)
		{
			IFC(GetVectorFromSource(pSource, &pVector));

			IFC(VectorCollectionView::CreateInstance(pVector, ppResult));
		}
		// Vector view interfaces IVectorView<IInspectable *> and IBindableVectorView
		else if ((pVectorView = ctl::query_interface<wfc::IVectorView<IInspectable*>>(pSource)) != NULL)
		{
			IFC(VectorViewCollectionView::CreateInstance(pVectorView, ppResult));
		}
		else if ((pBindableVectorView = ctl::query_interface<IBindableVectorView>(pSource)) != NULL)
		{
			// IBindableVectorView is bydesign v-table compatible with IVectorView<IInspectable *>
			// And everything that you get from it is v-table compatible as well
			pVectorView = reinterpret_cast<wfc::IVectorView<IInspectable*>*>(pBindableVectorView);
			pBindableVectorView = NULL;

			IFC(VectorViewCollectionView::CreateInstance(pVectorView, ppResult));
		}

		// Second try IIterable interfaces, first the generic one then the non-generic one
		else
		{
			if ((pIterable = ctl::query_interface<wfc::IIterable<IInspectable*>>(pSource)) != NULL)
			{
				hr = IterableCollectionView::CreateInstance(pIterable, ppResult);

				// CLR provides an Iterable<Inspectable> that return E_NOINTERFACE on the call
				// to pIterator->get_HasCurrent.  If we get this, ignore it and continue searching; when we QI
				// for IBindableIterable, we'll get an Iterable<Inspectable> that iterates successfully.
				if (hr == E_NOINTERFACE)
				{
					ReleaseInterface(pIterable);
					hr = S_OK;
				}
				IFC(hr);
			}


			if (pIterable == NULL
				&& (pBindableIterable = ctl::query_interface<IBindableIterable>(pSource)) != NULL)
			{
				// IBindableIterable is v-table compatible with IIterable<IInspectable *> by design
				pIterable = reinterpret_cast<wfc::IIterable<IInspectable*>*>(pBindableIterable);
				pBindableIterable = NULL;

				IFC(IterableCollectionView::CreateInstance(pIterable, ppResult));
			}
		}

		if (*ppResult == NULL)
		{
			// We do not know how to create a view for this object
			IFC(E_INVALIDARG);
		}
	}


	private ICollectionView CreateGroupedView(
		object pSource,
		PropertyPath pItemsPath)
	{
		var sourceAsVector = GetVectorFromSource(pSource);

		GroupedDataCollectionView::CreateInstance(pSourceAsVector, pItemsPath, ppResult));
	}

	internal static IList<object> GetVectorFromSource(object pSource)
	{
		*ppVector = nullptr;
		ctl::ComPtr<IInspectable> source(pSource);
		// Try to convert the source to a vector we can understand in the following order:
		//  IObservableVector<IInspectable *>
		//  IBindableVector
		//      try INCC and create BindableObservableVectorWrapper
		//      try IBindableObservableVector and create BindableObservableVectorWrapper
		//      reintepret_cast to wfc::IVector<IInspectable*>* since it is binary compatible with IBindableVector
		//  IVector<IInspectable*>
		//  IIterable<IInspectable*>
		//  IBindableIterable

		if (auto observableResult = source.AsOrNull<wfc::IObservableVector<IInspectable*>>())
	   {
			IFC_RETURN(observableResult.CopyTo(ppVector));
		}

	else if (auto bindableVector = source.AsOrNull<IBindableVector>())
	   {
			if (auto incc = source.AsOrNull<INotifyCollectionChanged>())
	       {
				IFC_RETURN(BindableObservableVectorWrapper::CreateInstance(bindableVector.Get(), incc.Get(), ppVector));
			}

		else if (auto bindableObservable = source.AsOrNull<IBindableObservableVector>())
	       {
				IFC_RETURN(BindableObservableVectorWrapper::CreateInstance(bindableObservable.Get(), ppVector));
			}

		else
			{
				// *this reintepret_cast is OK because these interfaces are guaranteed to be binary compatible
				*ppVector = reinterpret_cast<wfc::IVector<IInspectable*>*>(bindableVector.Detach());
			}
		}

	else if (auto regularVector = source.AsOrNull<wfc::IVector<IInspectable*>>())
	   {
			*ppVector = regularVector.Detach();
		}

	else
		{
			ctl::ComPtr<ReadOnlyTrackerCollection<IInspectable*>> readOnlyCollection;
			IFC_RETURN(ctl::make(&readOnlyCollection));
			IFC_RETURN(InitializeReadOnlyCollectionFromIterable(readOnlyCollection.Get(), source.Get()));
			*ppVector = readOnlyCollection.Detach();
		}
	}
}
