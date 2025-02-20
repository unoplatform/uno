// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InspectingDataSource.cpp, commit 37ade09; ItemsSourceView.cpp, commit dc8d573

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Interop;
using _IBindableIterable = System.Collections.IEnumerable;
// WinRT types that have a different name in .net
using _IBindableVector = System.Collections.IList;
using CollectionChange = global::Windows.Foundation.Collections.CollectionChange;
using INotifyCollectionChanged = global::System.Collections.Specialized.INotifyCollectionChanged;
using NotifyCollectionChangedAction = global::System.Collections.Specialized.NotifyCollectionChangedAction;
// Conflicting types
using NotifyCollectionChangedEventArgs = global::System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using Uno.Extensions;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class ItemsSourceView
{
	private readonly IList<object> m_vector;
	private readonly IReadOnlyList<object> m_vectorView;
	private readonly IKeyIndexMapping m_uniqueIdMaping;

	private IDisposable _collectionChangedListener;

	public ItemsSourceView(object source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("Argument 'source' is null.");
		}

		if (source is IList<object> vector)
		{
			m_vector = vector;
			ListenToCollectionChanges(vector);
		}
		else
		{
			// The bindable interop interface are abi compatible with the corresponding
			// WinRT interfaces.
			if (source is _IBindableVector bindableVector) // IList
			{
				m_vector = ListAdapter.ToGeneric(bindableVector);
				ListenToCollectionChanges(bindableVector);
			}
			else if (source is IReadOnlyList<object> readOnlyList)
			{
				m_vectorView = readOnlyList;
				ListenToCollectionChanges(readOnlyList);
			}
			else
			{
				if (source is IEnumerable<object> iterable)
				{
					m_vector = WrapIterable(iterable);
				}
				else
				{
					if (source is _IBindableIterable bindableIterable)
					{
						m_vector = WrapIterable(bindableIterable);
					}
					else
					{
						throw new ArgumentException("Argument 'source' is not a supported vector.");
					}
				}
			}
		}

		m_uniqueIdMaping = source as IKeyIndexMapping;
	}

	~ItemsSourceView()
	{
		UnListenToCollectionChanges();
	}

	#region IDataSourceOverrides
	private protected int GetSizeCore()
	{
		return m_vectorView is not null ? m_vectorView.Count : m_vector.Count;
	}

	private protected object GetAtCore(int index)
	{
		return m_vectorView is not null ? m_vectorView[index] : m_vector[index];
	}

	private protected bool HasKeyIndexMappingCore()
	{
		return m_uniqueIdMaping != null;
	}

	private protected string KeyFromIndexCore(int index)
	{
		if (m_uniqueIdMaping != null)
		{
			return m_uniqueIdMaping.KeyFromIndex(index);
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	private protected int IndexFromKeyCore(string id)
	{
		if (m_uniqueIdMaping != null)
		{
			return m_uniqueIdMaping.IndexFromKey(id);
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	private protected int IndexOfCore(object value)
	{
		if (m_vectorView is not null)
		{
			return m_vectorView.IndexOf(value);
		}
		else
		{
			return m_vector?.IndexOf(value) ?? -1;
		}
	}

	#endregion

	IList<object> WrapIterable(IEnumerable iterable)
		=> iterable.Cast<object>().ToList();

	IList<object> WrapIterable(IEnumerable<object> iterable)
		=> new List<object>(iterable);

	void UnListenToCollectionChanges()
	{
		_collectionChangedListener?.Dispose();
	}

	void ListenToCollectionChanges(object vector)
	{
		if (m_vector is null)
		{
			MUX_ASSERT(m_vectorView is not null);
		}
		else
		{
			MUX_ASSERT(m_vector is not null);
		}

		switch (vector)
		{
			case INotifyCollectionChanged incc:
				_collectionChangedListener = Disposable.Create(() => incc.CollectionChanged -= OnCollectionChanged);
				incc.CollectionChanged += OnCollectionChanged;
				break;

			case IBindableObservableVector bindableObservableVector:
				_collectionChangedListener = Disposable.Create(() => bindableObservableVector.VectorChanged -= OnBindableVectorChanged);
				bindableObservableVector.VectorChanged += OnBindableVectorChanged;
				break;

			case IObservableVector<object> observableVector:
				_collectionChangedListener = Disposable.Create(() => observableVector.VectorChanged -= OnVectorChanged);
				observableVector.VectorChanged += OnVectorChanged;
				break;

			case IObservableVector observableVectorUntyped:
				_collectionChangedListener = Disposable.Create(() => observableVectorUntyped.UntypedVectorChanged -= OnUntypedVectorChanged);
				observableVectorUntyped.UntypedVectorChanged += OnUntypedVectorChanged;
				break;
		}
	}

	void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		OnItemsSourceChanged(e);
	}

	void OnBindableVectorChanged(IBindableObservableVector sender, object e)
	{
		OnVectorChanged(default, (IVectorChangedEventArgs)e);
	}

	void OnUntypedVectorChanged(object sender, IVectorChangedEventArgs args)
	{
		OnVectorChanged(default, args);
	}

	void OnVectorChanged(IObservableVector<object> _, IVectorChangedEventArgs e)
	{
		// We need to build up NotifyCollectionChangedEventArgs here to raise the event.
		// There is opportunity to make this faster by caching the args if it does 
		// show up as a perf issue.
		// Also note that we do not access the data - we just add null. We just 
		// need the count.

		// UNO: We use the right NotifyCollectionChangedEventArgs as the provided action is
		//		restricted for each ctor overload.

		switch (e.CollectionChange)
		{
			case CollectionChange.ItemInserted:
				OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new object[] { null }, (int)e.Index));
				break;
			case CollectionChange.ItemRemoved:
				OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new object[] { null }, (int)e.Index));
				break;
			case CollectionChange.ItemChanged:
				OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { null }, new object[] { null }, (int)e.Index));
				break;
			case CollectionChange.Reset:
				OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				break;
			default:
				global::System.Diagnostics.Debug.Assert(false);
				OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				break;
		}
	}
}
