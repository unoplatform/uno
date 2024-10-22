// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\CollectionViewSource_Partial.cpp, tag winui3/release/1.6.1, commit d74a0332

using System;
using System.Collections;
using System.Collections.Generic;
using Uno.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Provides a data source that adds grouping and current-item support to collection classes.
/// </summary>
public partial class CollectionViewSource : IDependencyObjectInternal
{
	//_Check_return_ HRESULT DirectUI::CollectionViewSource::GetValue(_In_ const CDependencyProperty* pProperty, _Out_ IInspectable** ppValue)
	//{

	//	HRESULT hr = S_OK;

	//	// If we're looking for the ItemsPath value, go directly through get_ItemsPath so the
	//	// string automatically gets wrapped in a PropertyPath object.
	//	if (pProperty->GetIndex() == KnownPropertyIndex::CollectionViewSource_ItemsPath)
	//	{
	//		ctl::ComPtr<xaml::IPropertyPath> spItemsPath;
	//	IFC(get_ItemsPath(&spItemsPath));
	//		*ppValue = spItemsPath.Detach();
	//	}
	//	else
	//	{
	//		IFC(CollectionViewSourceGenerated::GetValue(pProperty, ppValue));
	//	}
	//}

	void IDependencyObjectInternal.OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		// base.OnPropertyChanged2(args);

		if (args.Property == SourceProperty)
		{
			OnSourceChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == IsSourceGroupedProperty)
		{
			OnIsSourceGroupedChanged();
		}
		else if (args.Property == ItemsPathProperty)
		{
			OnItemsPathChanged();
		}
	}

	private void OnSourceChanged(object pOldSource, object pNewSource)
	{
		// Validate the source
		if (!IsSourceValid(pNewSource))
		{
			// If it's not valid as is, and this is a CLR application, try to make it valid by injecting a CLR wrapper

			//TODO: does this leak?
			//object pWrapper = ReferenceTrackerManager::GetTrackerTarget(pNewSource);
			//if (pWrapper != null)
			//{
			//	pNewSource = pWrapper;
			//	if (!IsSourceValid(pNewSource))
			//	{
			//		IFC(E_INVALIDARG);
			//	}
			//}
			//else
			//{
			throw new InvalidOperationException("The source is null");
			//}
		}

		EnsureView(pNewSource);
	}

	private void OnIsSourceGroupedChanged()
	{
		var source = Source;

		// The source doesn't need to be checked again because it was checked
		// whenever it was set

		EnsureView(source);
	}

	private void OnItemsPathChanged()
	{
		var source = Source;

		EnsureView(source);
	}

	private void EnsureView(object newSource)
	{
		ICollectionView pView = null;
		bool valid = false;

		// Only try to create a collection view if we have a source
		// otherwise we will just set the value to NULL
		if (newSource is not null)
		{
			pView = CollectionViewManager.GetViewRecord(newSource, this);

			// A view should have been created by now, or we would have failed
			// if the source is not supported
			if (pView is null)
			{
				throw new InvalidOperationException("The source is not supported");
			}

			// Move the item to the first position on the newly created collection
			// if we have one
			valid = pView.MoveCurrentToFirst();
		}

		// Set the read only property
		// Now built-in collection views are DOs which are walkable so
		// storing it as the DP value is all that is needed to ensure
		// that they are walked during GC
		SetValueInternal(
			CollectionViewSource.ViewProperty,
			pView,
			true /*fAllowReadOnly*/);

		// Notify of the changes
		CVSViewChanged?.Invoke(this, null);
	}

	private bool IsSourceValid(object source)
	{
		if ((source is null ||
			source is IEnumerable ||
			source is IEnumerable<object>) &&
			source is not ICollectionView) // TODO:MZ: Validate this condition
		{
			return true;
		}
		return false;
	}

	internal EventHandler<object> CVSViewChanged;
}
