// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

namespace DirectUI
{

	#region Iterator
	// FUTURE: Could we just use the WRL implementations here?
	internal abstract class IteratorBase<T> : IEnumerator<T>
	{
		//typedef typename wf.Internal.GetAbiType<typename IEnumerator<T>.T_complex>.type T;

		//BEGIN_INTERFACE_MAP(IteratorBase, ctl.WeakReferenceSource)
		//	INTERFACE_ENTRY(IteratorBase, IEnumerator<T>)
		//END_INTERFACE_MAP(IteratorBase, ctl.WeakReferenceSource)

		// UNO only
		protected void CheckThread() => DispatcherQueue.CheckThreadAccess();

		public void SetView(IVectorView<T> pView)
		{
			m_tpView = pView;
			bool bHasCurrent;
			bHasCurrent = MoveNext();
			return;
		}

		#region UNO ONLY - Untyped IEnumerator
		/// <inheritdoc />
		public void Reset() { }

		/// <inheritdoc />
		object IEnumerator.Current => Current;

		/// <inheritdoc />
		public void Dispose() { }
		#endregion

		public T Current
		{
			get
			{
				CheckThread();

				var current = GetCurrent();
				return current;
			}
		}

		public bool HasCurrent
		{
			get
			{
				CheckThread();

				uint nSize;
				nSize = m_tpView.Size;
				var hasCurrent = (m_nCurrentIndex > 0 && m_nCurrentIndex <= nSize);
				return hasCurrent;
			}
		}

		public bool MoveNext()
		{
			uint nSize;

			CheckThread();

			nSize = m_tpView.Size;

			var hasCurrent = false;
			ClearCurrent();
			if (m_nCurrentIndex >= 0 && m_nCurrentIndex < nSize)
			{
				// This is really dodgy- this can be either an DependencyObject or
				// a standard value type. We rely on SetCurrent assuming that we've
				// passed ownership for ref-counting to work correctly.
				T current;
				current = m_tpView.GetAt(m_nCurrentIndex);
				SetCurrent(current);
				m_nCurrentIndex++;
				hasCurrent = true;
			}
			else
			{
				// Use this as a marker that we're done, this will make it so
				// HasCurrent will also return false
				m_nCurrentIndex = nSize + 1;
			}
			return hasCurrent;
		}

		//private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
		//{
		//	if (InlineIsEqualGUID(iid, __uuidof(IEnumerator<T>)))
		//	{
		//		ppObject = (IEnumerator<T) *>(this);
		//	}
		//	else
		//	{
		//		return ctl.WeakReferenceSource.QueryInterfaceImpl(iid, ppObject);
		//	}

		//	AddRefOuter();
		//	return;
		//}

		// This class is marked novtable, so must not be instantiated directly.
		protected IteratorBase()
		{
			m_nCurrentIndex = 0;
		}

		//~IteratorBase()
		//{
		//IsDebuggerPresent(); // TODO UNO
		//}

		protected abstract T GetCurrent();
		protected abstract void SetCurrent(T current);
		protected abstract void ClearCurrent();

		//virtual private void Initialize() override
		//{
		//	WeakReferenceSource.Initialize();
		//	return;
		//}

		private IVectorView<T> m_tpView;
		private uint m_nCurrentIndex;
	}

	internal class Iterator<T>
		: IteratorBase<T>
	{
		//typedef typename wf.Internal.GetAbiType<typename IEnumerator<T>.T_complex>.type T;
		//typedef typename std.remove_pointer<T>.type T;

		protected override T GetCurrent()
		{
			var current = m_current;
			//AddRefInterface(m_current);
			return current;
		}

		protected override void ClearCurrent()
		{
			m_current = default;
		}

		protected override void SetCurrent(T current)
		{
			m_current = current;

			// Danger Will Robinson, Danger! Dodgy ownership semantics head...
			//ReleaseInterface(current);
		}

		T m_current;
	}

	//	template<>
	//	class Iterator<FLOAT>
	//		: public IteratorBase<FLOAT>
	//	{
	//	protected:
	//		private void GetCurrent(out FLOAT current) override
	//		{
	//			IFCPTR_RETURN(current);
	//			current = m_current;
	//			return;
	//		}

	//		void SetCurrent( FLOAT current) override
	//		{
	//			m_current = current;
	//		}

	//	private:
	//		FLOAT m_current;
	//	};

	//	template<>
	//	class Iterator<DOUBLE>
	//		: public IteratorBase<DOUBLE>
	//	{
	//	protected:
	//		private void GetCurrent(out DOUBLE current) override
	//		{
	//			IFCPTR_RETURN(current);
	//			current = m_current;
	//			return;
	//		}

	//		void SetCurrent( DOUBLE current) override
	//		{
	//			m_current = current;
	//		}

	//	private:
	//		DOUBLE m_current;
	//	};

	//	template<>
	//	class Iterator<wf.Point>
	//		: public IteratorBase<wf.Point>
	//	{
	//	protected:
	//		private void GetCurrent(out wf.Point current) override
	//		{
	//			IFCPTR_RETURN(current);
	//			current = m_current;
	//			return;
	//		}

	//		void SetCurrent( wf.Point current) override
	//		{
	//			m_current = current;
	//		}

	//	private:
	//		wf.Point m_current;
	//	};


	//	template<>
	//	class Iterator<xaml_docs.TextRange>
	//		: public IteratorBase<xaml_docs.TextRange>
	//	{
	//	protected:
	//		private void GetCurrent(out xaml_docs.TextRange current) override
	//		{
	//			IFCPTR_RETURN(current);
	//			current = m_current;
	//			return;
	//		}

	//		void SetCurrent( xaml_docs.TextRange current) override
	//		{
	//			m_current = current;
	//		}

	//	private:
	//		xaml_docs.TextRange m_current;
	//	};
	//#pragma endregion

	//#pragma region Views And Non-DO-Backed Collections
	//	template <class T>
	//	class __declspec(novtable) ViewBase:
	//		public IVectorView<T>,
	//		public IIterable<T>,
	//		public ctl.WeakReferenceSource
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IVector<T>.T_complex>.type T;

	//		BEGIN_INTERFACE_MAP(ViewBase, ctl.WeakReferenceSource)
	//			INTERFACE_ENTRY(ViewBase, IVectorView<T>)
	//			INTERFACE_ENTRY(ViewBase, IIterable<T>)
	//		END_INTERFACE_MAP(ViewBase, ctl.WeakReferenceSource)

	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		ViewBase() = default;

	//		private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
	//		{
	//			if (InlineIsEqualGUID(iid, __uuidof(IVectorView<T>)))
	//			{
	//				ppObject = (IVectorView<T) *>(this);
	//			}
	//			else if (InlineIsEqualGUID(iid, __uuidof(IIterable<T>)))
	//			{
	//				ppObject = (IIterable<T) *>(this);
	//			}
	//			else
	//			{
	//				return ctl.WeakReferenceSource.QueryInterfaceImpl(iid, ppObject);
	//			}

	//			AddRefOuter();
	//			return;
	//		}

	//	public:
	//		~ViewBase()
	//		{
	//			ClearView();
	//		}

	//		virtual void ClearView()
	//		{
	//			foreach (var it in m_list)
	//			{
	//				if (it)
	//				{
	//					(it).Release();
	//				}
	//			}

	//			m_list.clear();
	//		}

	//		private void SetView( IEnumerator<T>* view)
	//		{
	//			T item = NULL;

	//			ClearView();
	//			bool hasCurrent = false;
	//			hasCurrent = view.HasCurrent;
	//			while (hasCurrent)
	//			{
	//				item = view.Current;
	//				m_list.push_back(item);
	//				item = NULL;
	//				hasCurrent = view.MoveNext);
	//			}

	//		Cleanup:

	//			ReleaseInterface(item);

	//			return hr;
	//		}

	//		get_Size(out uint size) override
	//		{
	//			CheckThread();
	//			size = m_list.size();

	//		}

	//		GetAt( uint index, out T item) override
	//		{
	//			uint nPosition = 0;

	//			CheckThread();

	//			for (var it = m_list.begin(); it != m_list.end(); ++nPosition, ++it)
	//			{
	//				if (nPosition == index)
	//				{
	//					item = it;
	//					AddRefInterface(it);
	//					goto Cleanup;
	//				}
	//			}

	//			E_FAIL;

	//		}

	//		// IIterable<T> implementation
	//		First(out result_maybenull_ IEnumerator<T> *first) override
	//		{
	//			Iterator<T>* pIterator = NULL;

	//			CheckThread();

	//			pIterator = ctl.ComObject<Iterator<T>>.CreateInstance);
	//			pIterator.SetView(this);
	//			first = pIterator;
	//			pIterator = NULL;

	//		Cleanup:
	//			ctl.release_interface(pIterator);
	//			return hr;
	//		}

	//	protected:
	//		std.list<T> m_list;
	//	};

	//	template <class T>
	//	class __declspec(novtable) View:
	//		public ViewBase<T>
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IVector<T>.T_complex>.type T;
	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		View() = default;

	//	public:
	//		IndexOf( T value, out uint index, out bool found) override
	//		{
	//			uint nPosition = 0;

	//			this.CheckThread();

	//			for (var it = this.m_list.begin(); it != this.m_list.end(); ++nPosition, ++it)
	//			{
	//				if (it == value)
	//				{
	//					index = nPosition;
	//					found = true;
	//					goto Cleanup;
	//				}
	//			}

	//			index = 0;
	//			found = false;

	//		}
	//	};

	//	template<>
	//	class View<DependencyObject>:
	//		public ViewBase<DependencyObject>
	//	{
	//	public:
	//		IFACEMETHOD(IndexOf)(
	//			 DependencyObject value,
	//			out uint index,
	//			out bool found) override;
	//	};

	//	IUntypedVector: public DependencyObject
	//	{
	//		IFACEMETHOD(UntypedAppend)( DependencyObject pItem);
	//		IFACEMETHOD(UntypedGetSize)(out uint * pSize);
	//		IFACEMETHOD(UntypedGetAt)( uint  index, out  DependencyObject ppItem);
	//		IFACEMETHOD(UntypedInsertAt)( uint  index,  DependencyObject pItem);
	//		IFACEMETHOD(UntypedRemoveAt)( uint  index);
	//		IFACEMETHOD(UntypedClear)();
	//	};

	//	bool UntypedTryGetIndexOf( IUntypedVector vector,  DependencyObject item, out uint * index);

	//	template <class T>
	//	private void InitializeReadOnlyCollectionFromIterable(T pCollection, DependencyObject pSource)
	//	{
	//		xaml_interop.IBindableIterable pBindableItems = NULL;
	//		IIterable<DependencyObject *> pItems = NULL;
	//		IEnumerator<DependencyObject *> pIterator = NULL;
	//		DependencyObject pItem = NULL;
	//		bool hasCurrent = false;

	//		// Note: The source must be iterable at the very least for us to be able to do anything with it

	//		// If the source is not IIterable<DependencyObject *> then it must be
	//		// IBindableIterable, otherwise is a bad source
	//		pItems = ctl.query_interface<IIterable<DependencyObject *>>(pSource);
	//		if (pItems == NULL)
	//		{
	//			pBindableItems = ctl.query_interface<xaml_interop.IBindableIterable>(pSource);
	//			if (pBindableItems == NULL)
	//			{
	//				// The source is not bindable, so nothing to do
	//				goto Cleanup;
	//			}

	//			// IBindableIterable is v-table compatible with IIterable<DependencyObject *>
	//			pItems = reinterpret_cast<IIterable<DependencyObject *> *>(pBindableItems);
	//			pBindableItems = NULL;
	//		}

	//		pIterator = pItems.First);
	//		hasCurrent = pIterator.HasCurrent;
	//		while (hasCurrent)
	//		{
	//			pItem = pIterator.Current;
	//			pCollection.InternalAppend(pItem);
	//			ReleaseInterface(pItem);
	//			hasCurrent = pIterator.MoveNext);
	//		}

	//	Cleanup:
	//		ReleaseInterface(pBindableItems);
	//		ReleaseInterface(pItems);
	//		ReleaseInterface(pIterator);
	//		ReleaseInterface(pItem);
	//		return hr;
	//	}

	//#pragma endregion

	//#pragma region DO-backed Collection
	//	class __declspec(novtable) PresentationFrameworkCollectionBase
	//		: public DependencyObject
	//	{
	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		PresentationFrameworkCollectionBase() = default;
	//	};

	//	template <class T>
	//	class __declspec(novtable) PresentationFrameworkCollectionTemplateBase:
	//		public IVectorView<T>,
	//		public IVector<T>,
	//		public IIterable<T>,
	//		public PresentationFrameworkCollectionBase
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IVector<T>.T_complex>.type T;

	//		BEGIN_INTERFACE_MAP(PresentationFrameworkCollectionTemplateBase, PresentationFrameworkCollectionBase)
	//			INTERFACE_ENTRY(PresentationFrameworkCollectionTemplateBase, IVectorView<T>)
	//			INTERFACE_ENTRY(PresentationFrameworkCollectionTemplateBase, IVector<T>)
	//			INTERFACE_ENTRY(PresentationFrameworkCollectionTemplateBase, IIterable<T>)
	//		END_INTERFACE_MAP(PresentationFrameworkCollectionTemplateBase, PresentationFrameworkCollectionBase)

	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		PresentationFrameworkCollectionTemplateBase() = default;

	//		private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
	//		{
	//			if (InlineIsEqualGUID(iid, __uuidof(IVectorView<T>)))
	//			{
	//				ppObject = (IVectorView<T) *>(this);
	//			}
	//			else if (InlineIsEqualGUID(iid, __uuidof(IVector<T>)))
	//			{
	//				ppObject = (IVector<T) *>(this);
	//			}
	//			else if (InlineIsEqualGUID(iid, __uuidof(IIterable<T>)))
	//			{
	//				ppObject = (IIterable<T) *>(this);
	//			}
	//			else
	//			{
	//				return PresentationFrameworkCollectionBase.QueryInterfaceImpl(iid, ppObject);
	//			}
	//			AddRefOuter();
	//			return;
	//		}

	//	public:
	//		get_Size(out uint size) override
	//		{
	//			CheckThread();
	//			size = (CCollection)(GetHandle()).GetCount();
	//			return;
	//		}

	//		GetView(out result_maybenull_ IVectorView<T> *view) override
	//		{
	//			if (view)
	//			{
	//				return (ctl.do_query_interface(view, this));
	//			}
	//			throw new NotImplementedException();
	//		}

	//		SetAt( uint index,  T item) override
	//		{
	//			CheckThread();
	//			uint size = 0;
	//			size = Size;
	//			IFCEXPECTRC_RETURN(index < size, E_BOUNDS);

	//			// Two step process to simulate setting a particular
	//			// position on the collection
	//			InsertAt(index, item);
	//			RemoveAt(index + 1);
	//			return;
	//		}

	//		RemoveAt( uint index) override
	//		{
	//			CheckThread();

	//			uint size = 0;
	//			size = Size;
	//			IFCEXPECTRC_RETURN(index < size, E_BOUNDS);

	//			Collection_RemoveAt((CCollection)(GetHandle()), index);
	//			return;
	//		}

	//		RemoveAtEnd() override
	//		{
	//			uint size = 0;

	//			CheckThread();
	//			size = Size;
	//			IFCEXPECT_RETURN(size > 0);

	//			RemoveAt(size - 1);
	//			return;
	//		}

	//		Clear() override
	//		{
	//			CheckThread();
	//			.Collection_Clear((CCollection)(GetHandle()));
	//			return;
	//		}

	//		//  The method moves the element from "index" position to the position immediately before the element at position "position".
	//		//  Move (pos, pos) is NoOp
	//		//  Move (pos, pos+1) is NoOp
	//		//  To move to the end, position should be equal to elements count.
	//		/// <param name="index">Index of the element which should be moved</param>
	//		/// <param name="position">Position immediately after the element's new position</param>
	//		private void MoveInternal( uint index,  uint position)
	//		{
	//			uint nCount = 0;
	//			nCount = Size;
	//			IFCEXPECT_RETURN(index >= 0 && index < nCount && position >= 0 && position <= nCount);
	//			if (index != position && position - index != 1)
	//			{
	//				.Collection_Move((CCollection)(GetHandle()), index, position);
	//			}
	//			return;
	//		}

	//		First(out result_maybenull_ IEnumerator<T>** first) override
	//		{
	//			Iterator<T>* pIterator = null;
	//			CheckThread();
	//			pIterator = ctl.ComObject<Iterator<T>>.CreateInstance);
	//			pIterator.SetView(this);
	//			first = pIterator;
	//			return;
	//		}
	//	};

	//	template <class T>
	//	class __declspec(novtable) PresentationFrameworkCollection:
	//		public IUntypedVector,
	//		public PresentationFrameworkCollectionTemplateBase<T>
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IVector<T>.T_complex>.type T;

	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		PresentationFrameworkCollection() = default;

	//	public:
	//		Append( T item) override
	//		{
	//			CValue boxedValue;
	//			BoxerBuffer buffer;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();
	//			pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), item, &buffer);

	//			IFC(.Collection_Add(
	//				(CCollection)(this.GetHandle()),
	//				&boxedValue));

	//		Cleanup:
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}

	//		GetAt( uint index, out T item) override
	//		{
	//			CValue value;
	//			Xint nIndex = (XINT32)(index);

	//			this.CheckThread();
	//			value = .Collection_GetItem((CCollection)(this.GetHandle()), nIndex);
	//			CValueBoxer.UnboxObjectValue(&value, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), __uuidof(T), reinterpret_cast<void*>(item));

	//		}

	//		IndexOf( T value, out uint index, out bool found) override
	//		{
	//			CValue boxedValue;
	//			Xint coreIndex = -1;
	//			BoxerBuffer buffer;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();

	//			if (value != NULL)
	//			{
	//				pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), value, &buffer);

	//				if (SUCCEEDED(.Collection_IndexOf(
	//					(CCollection)(this.GetHandle()),
	//					&boxedValue,
	//					&coreIndex)))
	//				{
	//					index = (uint)(coreIndex);
	//				}
	//			}

	//			found = coreIndex != -1;

	//		Cleanup:
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}

	//		InsertAt( uint index,  T item) override
	//		{
	//			CValue boxedValue;
	//			BoxerBuffer buffer;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();

	//			uint size = 0;
	//			size = this.Size;
	//			IFCEXPECTRC(index <= size, E_BOUNDS);

	//			pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), item, &buffer);

	//			IFC(.Collection_Insert(
	//				(CCollection)(this.GetHandle()),
	//				index,
	//				&boxedValue));

	//		Cleanup:
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}

	//		UntypedAppend( DependencyObject pItem)
	//		{
	//			T pTypedItem = NULL;

	//			this.CheckThread();
	//			ctl.do_query_interface(pTypedItem, pItem);
	//			this.Append(pTypedItem);

	//		Cleanup:
	//			ReleaseInterface(pTypedItem);
	//			return hr;
	//		}

	//		UntypedGetSize(
	//			out uint * pSize)
	//		{
	//			this.CheckThread();
	//			return this.get_Size(pSize);
	//		}

	//		UntypedGetAt(
	//			 uint  index,
	//			out  DependencyObject ppItem)
	//		{
	//			wrl.ComPtr<std.remove_pointer<T>.type> spTypedItem;

	//			this.CheckThread();
	//			spTypedItem = this.GetAt(index);
	//			spTypedItem.CopyTo(ppItem);

	//			return;
	//		}

	//		UntypedInsertAt( uint  index,  DependencyObject pItem)
	//		{
	//			wrl.ComPtr<DependencyObject> spItem (pItem);
	//			wrl.ComPtr<std.remove_pointer<T>.type> spTypedItem;

	//			this.CheckThread();
	//			spTypedItem = spItem.As);
	//			this.InsertAt(index, spTypedItem);

	//			return;
	//		}

	//		UntypedRemoveAt( uint  index)
	//		{
	//			this.CheckThread();
	//			this.RemoveAt(index);

	//			return;
	//		}

	//		UntypedClear()
	//		{
	//			this.CheckThread();
	//			this.Clear();

	//			return;
	//		}

	//	protected:
	//		private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
	//		{
	//			if (InlineIsEqualGUID(iid, __uuidof(IUntypedVector)))
	//			{
	//				ppObject = (IUntypedVector)(this);
	//			}
	//			else
	//			{
	//				return PresentationFrameworkCollectionTemplateBase<T>.QueryInterfaceImpl(iid, ppObject);
	//			}

	//			this.AddRefOuter();
	//			return;
	//		}
	//	};

	//	template <class T>
	//	class __declspec(novtable) ObservablePresentationFrameworkCollectionTemplateBase:
	//		public IObservableVector<T>,
	//		public PresentationFrameworkCollectionTemplateBase<T>
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IObservableVector<T>.T_complex>.type T;

	//		BEGIN_INTERFACE_MAP(ObservablePresentationFrameworkCollectionTemplateBase, PresentationFrameworkCollectionTemplateBase<T>)
	//			INTERFACE_ENTRY(ObservablePresentationFrameworkCollectionTemplateBase, IObservableVector<T>)
	//		END_INTERFACE_MAP(ObservablePresentationFrameworkCollectionTemplateBase, PresentationFrameworkCollectionTemplateBase<T>)

	//	protected:
	//		private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
	//		{
	//			if (InlineIsEqualGUID(iid, __uuidof(IObservableVector<T>)))
	//			{
	//				ppObject = (IObservableVector<T) *>(this);
	//			}
	//			else
	//			{
	//				return PresentationFrameworkCollectionTemplateBase<T>.QueryInterfaceImpl(iid, ppObject);
	//			}

	//			this.AddRefOuter();
	//			return;
	//		}

	//		// This class is marked novtable, so must not be instantiated directly.
	//		ObservablePresentationFrameworkCollectionTemplateBase()
	//		{
	//			m_pEventSource = NULL;
	//		}

	//	public:
	//		typedef CEventSource<VectorChangedEventHandler<T>, IObservableVector<T>, IVectorChangedEventArgs> VectorChangedEventSourceType;

	//		~ObservablePresentationFrameworkCollectionTemplateBase()
	//		{
	//			ctl.release_interface(m_pEventSource);
	//		}

	//		void OnReferenceTrackerWalk(int walkType) override
	//		{
	//			if( m_pEventSource != NULL )
	//				m_pEventSource.ReferenceTrackerWalk((EReferenceTrackerWalkType)(walkType));

	//			PresentationFrameworkCollectionTemplateBase<T>.OnReferenceTrackerWalk( walkType );
	//		}

	//		add_VectorChanged( VectorChangedEventHandler<T>* pHandler, out EventRegistrationToken ptToken) override
	//		{
	//			VectorChangedEventSourceType pEventSource = NULL;

	//			pEventSource = GetVectorChangedEventSource);
	//			pEventSource.AddHandler(pHandler);

	//			ptToken.value = (INT64)pHandler;

	//		Cleanup:
	//			ctl.release_interface(pEventSource);
	//			return hr;
	//		}

	//		remove_VectorChanged( EventRegistrationToken tToken) override
	//		{
	//			VectorChangedEventSourceType pEventSource = NULL;
	//			VectorChangedEventHandler<T>* pHandler = (VectorChangedEventHandler<T>*)tToken.value;

	//			pEventSource = GetVectorChangedEventSource);
	//			pEventSource.RemoveHandler(pHandler);

	//			tToken.value = 0;

	//		Cleanup:
	//			ctl.release_interface(pEventSource);
	//			return hr;
	//		}

	//	protected:
	//		private void GetVectorChangedEventSource(out VectorChangedEventSourceType* ppEventSource)
	//		{
	//			this.CheckThread();
	//			if (!m_pEventSource)
	//			{
	//				m_pEventSource = ctl.ComObject<VectorChangedEventSourceType>.CreateInstance);
	//				m_pEventSource.Initialize(KnownEventIndex.UnknownType_UnknownEvent, this, false);
	//			}

	//			ppEventSource = m_pEventSource;
	//			ctl.addref_interface(m_pEventSource);

	//		}

	//	private:
	//		VectorChangedEventSourceType m_pEventSource;
	//	};

	//	template <class T>
	//	class __declspec(novtable) ObservablePresentationFrameworkCollection:
	//		public ObservablePresentationFrameworkCollectionTemplateBase<T>
	//	{
	//		typedef typename wf.Internal.GetAbiType<typename IVector<T>.T_complex>.type T;

	//	protected:
	//		// This class is marked novtable, so must not be instantiated directly.
	//		ObservablePresentationFrameworkCollection() = default;

	//	public:
	//		Append( T item) override
	//		{
	//			CValue boxedValue;
	//			BoxerBuffer buffer;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();
	//			pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), item, &buffer);

	//			IFC(.Collection_Add(
	//				(CCollection)(this.GetHandle()),
	//				&boxedValue));

	//		Cleanup:
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}

	//		GetAt( uint index, out T item) override
	//		{
	//			CValue value;
	//			Xint nIndex = (XINT32)(index);

	//			this.CheckThread();
	//			value = .Collection_GetItem((CCollection)(this.GetHandle()), nIndex);
	//			CValueBoxer.UnboxObjectValue(&value, NULL, __uuidof(T), reinterpret_cast<void*>(item));

	//		}

	//		IndexOf( T value, out uint index, out bool found) override
	//		{
	//			CValue boxedValue;
	//			BoxerBuffer buffer;
	//			Xint coreIndex = -1;
	//			T pCurrentValue = NULL;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();

	//			found = false;

	//			if (value != NULL)
	//			{
	//				bool wrappingNeeded = false;
	//				ExternalObjectReference.ShouldBeWrapped(value, wrappingNeeded);
	//				if (!wrappingNeeded)
	//				{
	//					pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), value, &buffer);

	//					if (SUCCEEDED(.Collection_IndexOf(
	//						(CCollection)(this.GetHandle()),
	//						&boxedValue,
	//						&coreIndex)))
	//					{
	//						index = (uint)(coreIndex);
	//					}

	//					found = coreIndex != -1;
	//				}
	//				else
	//				{
	//					uint nCount = 0;
	//					nCount = this.Size;
	//					for (uint i = 0; i < nCount; ++i)
	//					{
	//						bool areEqual = false;
	//						pCurrentValue = this.GetAt(i);
	//						areEqual = PropertyValue.AreEqual(value, pCurrentValue);
	//						ReleaseInterface(pCurrentValue);
	//						if (areEqual)
	//						{
	//							index = i;
	//							found = true;
	//							break;
	//						}
	//					}
	//				}
	//			}

	//		Cleanup:
	//			ReleaseInterface(pCurrentValue);
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}

	//		InsertAt( uint index,  T item) override
	//		{
	//			CValue boxedValue;
	//			BoxerBuffer buffer;
	//			DependencyObject pMOR = NULL;

	//			this.CheckThread();

	//			uint size = 0;
	//			size = this.Size;
	//			IFCEXPECTRC(index <= size, E_BOUNDS);

	//			pMOR = CValueBoxer.BoxObjectValue(&boxedValue, MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.DependencyObject), item, &buffer);

	//			IFC(.Collection_Insert(
	//				(CCollection)(this.GetHandle()),
	//				index,
	//				&boxedValue));

	//		Cleanup:
	//			ctl.release_interface(pMOR);
	//			return hr;
	//		}
	//	};

	//	// This exists because in codegen TextElementCollection used to be a separate
	//	// entity from the standard PresentationFrameworkCollection. It serves no purpose
	//	// otherwise.
	//	template <class T>
	//	class TextElementCollection : public PresentationFrameworkCollection<T> { };
	//#pragma endregion

	//#pragma region DO-backed Value Collection Specializations
	//	// These collections exist to support projection of CDoubleCollection and friends
	//	// into WinRT interfaces.

	//	template<>
	//	class PresentationFrameworkCollection<FLOAT> :
	//		public PresentationFrameworkCollectionTemplateBase<FLOAT>
	//	{
	//	public:
	//		// IVector<FLOAT> implementation
	//		IFACEMETHOD(Append)( FLOAT item) override;
	//		IFACEMETHOD(GetAt)( uint index, out FLOAT item) override;
	//		IFACEMETHOD(InsertAt)( uint index,  FLOAT item) override;
	//		IFACEMETHOD(IndexOf)( FLOAT value, out uint index, out bool found) override;
	//	};

	//	template<>
	//	class PresentationFrameworkCollection<DOUBLE> :
	//		public PresentationFrameworkCollectionTemplateBase<DOUBLE>
	//	{
	//	public:
	//		// IVector<DOUBLE> implementation
	//		IFACEMETHOD(Append)( DOUBLE item) override;
	//		IFACEMETHOD(GetAt)( uint index, out DOUBLE item) override;
	//		IFACEMETHOD(InsertAt)( uint index,  DOUBLE item) override;
	//		IFACEMETHOD(IndexOf)( DOUBLE value, out uint index, out bool found) override;
	//	};

	//	template<>
	//	class PresentationFrameworkCollection<wf.Point> :
	//		public PresentationFrameworkCollectionTemplateBase<wf.Point>
	//	{
	//	public:
	//		// IVector<wf.Point> implementation
	//		IFACEMETHOD(Append)( wf.Point item) override;
	//		IFACEMETHOD(GetAt)( uint index, out wf.Point item) override;
	//		IFACEMETHOD(InsertAt)( uint index,  wf.Point item) override;
	//		IFACEMETHOD(IndexOf)( wf.Point value, out uint index, out bool found) override;
	//	};

	//	template<>
	//	class PresentationFrameworkCollection<xaml_docs.TextRange> :
	//		public PresentationFrameworkCollectionTemplateBase<xaml_docs.TextRange>
	//	{
	//	public:
	//		// IVector<wf.TextRange> implementation
	//		IFACEMETHOD(Append)( xaml_docs.TextRange item) override;
	//		IFACEMETHOD(GetAt)( uint index, out xaml_docs.TextRange item) override;
	//		IFACEMETHOD(InsertAt)( uint index,  xaml_docs.TextRange item) override;
	//		IFACEMETHOD(IndexOf)( xaml_docs.TextRange value, out uint index, out bool found) override;
	//	};
	//#pragma endregion

	//#pragma region ValueType Collections
	//	// DEPRECATED: All of these have WRL-equivalent implementations. It's unclear why they were
	//	// historically used, maybe WRL collections weren't available yet, but they probably have
	//	// subtle differences in behavior from the official collections which means we must keep
	//	// them around for now until we can remove their usage sites under a quirk.
	//	//
	//	// Do not use these in new code.
	//	template<typename T>
	//	class __declspec(novtable) ValueTypeIterator :
	//		public IteratorBase<T>
	//	{
	//	protected:

	//		// This class is marked novtable, so must not be instantiated directly.
	//		ValueTypeIterator() = default;

	//		private void GetCurrent(out T current)
	//		{
	//			current = m_current;

	//		}

	//		void ClearCurrent()
	//		{
	//		}

	//		void SetCurrent( T current)
	//		{
	//			m_current = current;
	//		}

	//		T m_current;
	//	};

	internal class UnoVectorViewToEnumerableAdapter<T> : IEnumerator<T>
	{
		private readonly IVectorView<T> _view;
		private int _index = -1;

		public UnoVectorViewToEnumerableAdapter(IVectorView<T> view) => _view = view;

		object IEnumerator.Current => Current;
		public T Current => _view.GetAt((uint)_index);

		public bool MoveNext() => ++_index < _view.Size;
		public void Reset() { }
		public void Dispose() { }
	}

	internal class UnoEnumeratorToIteratorAdapter<T> : IIterator<T>
	{
		private readonly IEnumerator<T> _inner;

		public UnoEnumeratorToIteratorAdapter(IEnumerator<T> inner)
		{
			_inner = inner;
		}

		/// <inheritdoc />
		public bool MoveNext()
			=> HasCurrent = _inner.MoveNext();

		/// <inheritdoc />
		public void Reset()
			=> _inner.Reset();

		/// <inheritdoc />
		public T Current => _inner.Current;

		/// <inheritdoc />
		object IEnumerator.Current => ((IEnumerator)_inner).Current;

		/// <inheritdoc />
		public void Dispose()
			=> _inner.Dispose();

		/// <inheritdoc />
		public bool HasCurrent { get; private set; }
	}

	internal interface IVectorView<T> : IReadOnlyList<T>
	{
		T GetAt(uint index);
		bool IndexOf(T value, out uint index); // This is the public UWP contract of IBindableView
		uint Size { get; }

		/* UNO Read only list adapter, should be a partial interface implementation.

		// BEGIN UNO Specific

		// IReadOnlyList
		public int Count => (int)Size;
		public T this[int index] => GetAt((uint)index);
		public IEnumerator<T> GetEnumerator() => new UnoVectorViewToEnumerableAdapter<T>(this);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		// UWP API Adapter

		// END UNO Specific

		 */
	}

	internal interface IIterator<T> : IEnumerator<T>
	{
		//bool MoveNext();
		//T Current { get; }
		bool HasCurrent { get; }
	}

	internal interface IIterable<T> : IEnumerable<T>
	{
		IIterator<T> GetIterator();
	}

	internal interface IVector<T> : IList<T>
	{
	}

	internal class ValueTypeViewBase<T> :
		IVectorView<T>,
		IIterable<T>
	{
		//BEGIN_INTERFACE_MAP(ValueTypeViewBase, ctl.WeakReferenceSource)
		//	INTERFACE_ENTRY(ValueTypeViewBase, IVectorView<T>)
		//	INTERFACE_ENTRY(ValueTypeViewBase, IIterable<T>)
		//END_INTERFACE_MAP(ValueTypeViewBase, ctl.WeakReferenceSource)


		// This class is marked novtable, so must not be instantiated directly.
		protected ValueTypeViewBase()
		{
		}

		//private void QueryInterfaceImpl(REFIID iid, out  void* ppObject) override
		//{
		//	if (InlineIsEqualGUID(iid, __uuidof(IVectorView<T>)))
		//	{
		//		ppObject = (IVectorView<T) *>(this);
		//	}
		//	else if (InlineIsEqualGUID(iid, __uuidof(IIterable<T>)))
		//	{
		//		ppObject = (IIterable<T) *>(this);
		//	}
		//	else
		//	{
		//		return ctl.WeakReferenceSource.QueryInterfaceImpl(iid, ppObject);
		//	}

		//	AddRefOuter();
		//	return;
		//}


		~ValueTypeViewBase()
		{
			ClearView();
		}

		// BEGIN UNO Specific
		protected void CheckThread() => CoreDispatcher.CheckThreadAccess();

		public int Count => (int)Size;
		public T this[int index] => GetAt((uint)index);
		public IEnumerator<T> GetEnumerator() => m_vector.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IIterator<T> GetIterator() => new UnoEnumeratorToIteratorAdapter<T>(GetEnumerator());

		public bool IndexOf(T value, out uint index)
		{
			var i = m_vector.IndexOf(value);
			index = (uint)i;
			return i >= 0;
		}
		// END UNO Specific

		protected virtual void ClearView()
		{
			m_vector.Clear();
		}

		internal void SetView(IIterator<T> view)
		{
			ClearView();
			bool hasCurrent = false;
			hasCurrent = view.HasCurrent;
			while (hasCurrent)
			{
				T item;
				item = view.Current;
				m_vector.Add(item);
				hasCurrent = view.MoveNext();
			}
		}

		//private void SetView(reads_opt_(cItems)  T pItems,  uint cItems)
		//{
		//	ClearView();
		//	if (pItems)
		//	{
		//		m_vector.assign(pItems, pItems + cItems);
		//	}

		//	return hr;//RRETURN_REMOVAL
		//}

		// Note use of pass-by-value. std.vector knows how to rvalue move itself, so we let the caller
		// decide if they are copying from an lvalue or moving from an rvalue. Either, way, we will
		// do a fast move from the result.
		internal void SetView(List<T> items)
		{
			m_vector = items;
		}

		public uint Size
		{
			get
			{
				CheckThread();
				var size = m_vector.Count;
				return (uint)size;
			}
		}

		public T GetAt(uint index)
		{
			CheckThread();

			if (index < m_vector.Count)
			{
				var item = m_vector[(int)index];
				return item;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		// IIterable<T> implementation
		//public T First(out result_maybenull_ IEnumerator<T> *first)
		//{
		//	ValueTypeIterator<T> spIterator;

		//	CheckThread();

		//	spIterator = default;
		//	spIterator.SetView(this);
		//	first = spIterator.Detach();

		//}

		protected List<T> m_vector = new List<T>();
	};

	internal class ValueTypeView<T> :
		ValueTypeViewBase<T>
	{
		// This class is marked novtable, so must not be instantiated directly.
		protected ValueTypeView() { }

		public virtual void IndexOf(T value, out uint index, out bool found)
		{
			index = 0;
			found = false;

			this.CheckThread();

			{
				//var beginIterator = std.begin(this.m_vector);
				//var endIterator = std.end(this.m_vector);
				//var foundIterator = std.find(beginIterator, endIterator, value);

				//if (foundIterator != endIterator)
				//{
				//	index = (uint)(foundIterator - beginIterator);
				//	found = true;
				//}
				found = base.IndexOf(value, out index);
			}
		}
	}

	internal class ValueTypeCollection<T> :
		ValueTypeView<T>,
		IVector<T>
	{
		//BEGIN_INTERFACE_MAP(ValueTypeCollection, ValueTypeView<T>)
		//	INTERFACE_ENTRY(ValueTypeCollection, IVector<T>)
		//END_INTERFACE_MAP(ValueTypeCollection, ValueTypeView<T>)

		// This class is marked novtable, so must not be instantiated directly.
		internal ValueTypeCollection() { }

		//private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
		//{
		//	if (InlineIsEqualGUID(iid, __uuidof(IVector<T>)))
		//	{
		//		ppObject = (IVector<T) *>(this);
		//	}
		//	else
		//	{
		//		return ValueTypeView<T>.QueryInterfaceImpl(iid, ppObject);
		//	}

		//	this.AddRefOuter();
		//	return;
		//}

		//get_GetAt(uint index, out T item)
		//{
		//	return ValueTypeView<T>.GetAt(index, item);
		//}

		//get_Size(out uint size) override
		//{
		//	return ValueTypeView<T>.get_Size(size);
		//}

		//GetView(out result_maybenull_ IVectorView<T>** view) override
		//{
		//	if (view)
		//	{
		//		return ctl.do_query_interface(view, this);
		//	}
		//	throw new NotImplementedException();
		//}

		//IndexOf( T value, out uint index, out bool found) override
		//{
		//	return ValueTypeView<T>.IndexOf(value, index, found);
		//}

		public virtual void SetAt(uint index, T item)
		{
			this.CheckThread();

			// Wow! The old implementation used to clip "index" to the actual size, possibly writing
			// to one-beyond the end. This is bad bad bad. Let's fail instead of writing to random memory
			if (index < this.m_vector.Count)
			{
				this.m_vector[(int)index] = item;
			}
			else
			{
				throw new InvalidOperationException();
			}

			RaiseVectorChanged(CollectionChange.ItemChanged, index);
		}

		public virtual void InsertAt(uint index, T item)
		{
			this.CheckThread();

			// The old std.list-based implementation used to clip "index" to the actual size
			// The old implementation also used to not wrap the allocation in an IFCSTL
			this.m_vector.Insert((int)index, item);

			RaiseVectorChanged(CollectionChange.ItemInserted, index);
		}

		public virtual void RemoveAt(uint index)
		{
			this.CheckThread();

			// The old std.list-based implementation used to clip "index" to the actual size
			this.m_vector.RemoveAt((int)index);

			RaiseVectorChanged(CollectionChange.ItemRemoved, index);

		}

		public virtual void Append(T item)
		{
			this.CheckThread();

			this.m_vector.Add(item);
			RaiseVectorChanged(CollectionChange.ItemInserted, (uint)this.m_vector.Count - 1);

		}

		public void RemoveAtEnd()
		{
			uint size = (uint)this.m_vector.Count;

			RemoveAt(size - 1);

			return;
		}

		public virtual void Clear()
		{
			this.CheckThread();
			this.ClearView();
			RaiseVectorChanged(CollectionChange.Reset, 0);
		}

		private protected virtual void RaiseVectorChanged(CollectionChange action, uint index)
		{
			return;
		}

		// BEGIN UNO Specific

		public bool IsReadOnly { get; }
		public new T this[int index]
		{
			get => GetAt((uint)index);
			set => SetAt((uint)index, value);
		}

		public void Add(T item) => Append(item);
		public void Insert(int index, T item) => InsertAt((uint)index, item);
		public bool Contains(T item) => IndexOf(item, out _);
		public int IndexOf(T item) => IndexOf(item, out uint index) ? (int)index : -1;
		public void CopyTo(T[] array, int arrayIndex) => m_vector.CopyTo(array, arrayIndex);
		public void RemoveAt(int index) => RemoveAt((uint)index);
		public bool Remove(T item)
		{
			if (IndexOf(item, out var index))
			{
				RemoveAt(index); // Make sure to use the RemoveAt which will raise the change event
				return true;
			}
			else
			{
				return false;
			}
		}

		// END UNO Specific
	};

	internal class ValueTypeObservableCollection<T> :
		ValueTypeCollection<T>,
		IObservableVector<T>
	{
		//BEGIN_INTERFACE_MAP(ValueTypeObservableCollection, ValueTypeCollection<T>)
		//	INTERFACE_ENTRY(ValueTypeObservableCollection, IObservableVector<T>)
		//END_INTERFACE_MAP(ValueTypeObservableCollection, ValueTypeCollection<T>)

		// This class is marked novtable, so must not be instantiated directly.
		internal ValueTypeObservableCollection() { }

		//private void QueryInterfaceImpl( REFIID iid, out  void* ppObject) override
		//{
		//	if (InlineIsEqualGUID(iid, __uuidof(IObservableVector<T>)))
		//	{
		//		ppObject = (IObservableVector<T) *>(this);
		//	}
		//	else
		//	{
		//		return ValueTypeCollection<T>.QueryInterfaceImpl(iid, ppObject);
		//	}

		//	this.AddRefOuter();
		//	return;
		//}

		~ValueTypeObservableCollection()
		{
			ClearHandlers();
		}

		public event VectorChangedEventHandler<T> VectorChanged
		{
			add => m_handlers.Add(value);
			remove => m_handlers.Remove(value);
		}

		private protected override void RaiseVectorChanged(CollectionChange action, uint index)
		{
			VectorChangedEventArgs pArgs = null;

			pArgs = new VectorChangedEventArgs(action, index);

			foreach (var it in m_handlers)
			{
				(it).Invoke(this, pArgs);
			}
		}

		private void ClearHandlers()
		{
			m_handlers.Clear();
		}

		private List<VectorChangedEventHandler<T>> m_handlers = new List<VectorChangedEventHandler<T>>();
	}

	//// Template Specialization of ValueTypeView<T>.IndexOf for DateTime
	//// because DateTime struct doesn't define operator==, which is used by std.find in ValueTypeView<T>.IndexOf
	//template <>
	//ValueTypeView<DateTime>.IndexOf( DateTime value, out uint index, out bool found) override
	//{
	//	index = 0;
	//	found = false;

	//	CheckThread();

	//	for (uint i = 0; i < m_vector.size(); ++i)
	//	{
	//		if (m_vector[i].UniversalTime == value.UniversalTime)
	//		{
	//			index = i;
	//			found = true;
	//			break;
	//		}
	//	}

	//}
	#endregion

	internal class TrackerCollection<T> : List<T>, IVector<T>
	{
		public void RemoveAtEnd()
		{
			if (Count > 0)
			{
				RemoveAt(Count - 1);
			}
		}
	}

	internal class TrackerView<T> : IVectorView<T>
	{
		private IVector<T> _collection;

		public TrackerView()
		{
		}

		public void SetCollection(IVector<T> collection)
		{
			_collection = collection;
		}

		/// <inheritdoc />
		public T GetAt(uint index)
			=> _collection[(int)index];

		/// <inheritdoc />
		public bool IndexOf(T value, out uint index)
		{
			var i = _collection.IndexOf(value);
			if (i == -1)
			{
				index = 0;
				return false;
			}
			else
			{
				index = (uint)i;
				return true;
			}
		}

		/// <inheritdoc />
		public uint Size => (uint)Count;

		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator()
			=> _collection.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable)_collection).GetEnumerator();

		/// <inheritdoc />
		public int Count => _collection.Count;

		/// <inheritdoc />
		public T this[int index] => _collection[index];
	}
}
