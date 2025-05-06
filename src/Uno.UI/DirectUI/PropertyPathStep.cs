using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using CClassInfo = System.Type; // fixme@xy: is that the equivalent or suitable here

namespace DirectUI;

internal abstract partial class PropertyPathStep // src\dxaml\xcp\dxaml\lib\PropertyPathStep.h
{
	//public:
	//public void Initialize(PropertyPathListener pListener);
	//using ctl::WeakReferenceSource::Initialize;

	//~PropertyPathStep() override;

	public void SetNext(PropertyPathStep pNextStep)
	{
		m_tpNext = pNextStep;
	}

	public PropertyPathStep GetNextStep()
	{
		return m_tpNext;
	}

	public abstract void ReConnect(object pSource);

	public abstract object GetValue();
	public abstract void SetValue(object pValue);
	public abstract CClassInfo GetSourceType();
	public abstract bool IsConnected();

	public abstract CClassInfo GetType2();

	public virtual string DebugGetPropertyName() => null;

	//protected:
	//protected abstract void Disconnect();
	protected virtual void DisconnectCurrentItem() { }
	//protected virtual void CollectionViewCurrentChanged();


	//protected void AddCurrentChangedEventHandler();

	// This method is safe to be called from the destructor
	//protected void SafeRemoveCurrentChangedEventHandler();

	//protected void RaiseSourceChanged();

	//protected:
	protected PropertyPathStep m_tpNext;
	protected WeakReference m_spListener;

	protected event CurrentChangedEventCallback m_epCurrentChangedHandler;
	protected ICollectionView m_tpSourceAsCV;
}
partial class PropertyPathStep // src\dxaml\xcp\dxaml\lib\PropertyPathStep.cpp
{
	public void Initialize(PropertyPathListener pListener)
	{
		m_spListener = new WeakReference(pListener);
	}

	protected void Disconnect()
	{
		if (m_epCurrentChangedHandler)
		{
			m_epCurrentChangedHandler.DetachEventHandler(m_tpSourceAsCV.Get());
		}

		m_tpSourceAsCV.Clear();

		DisconnectCurrentItem();
	}

	~PropertyPathStep()
	{
		SafeRemoveCurrentChangedEventHandler();
	}

	protected virtual void CollectionViewCurrentChanged()
	{
		//ASSERT(!m_epCurrentChangedHandler);
		//ASSERT(m_tpSourceAsCV.Get());

		// todo@xy: IDisposable subscription?
		IFC(m_epCurrentChangedHandler.AttachEventHandler(m_tpSourceAsCV.Get(),
			[this](IInspectable * sender, IInspectable * args){
			return CollectionViewCurrentChanged();
		}));
	}

	protected void SafeRemoveCurrentChangedEventHandler()
	{
		// todo@xy: IDisposable subscription?

		// if we do not have an event handler allocated
		// this means that we have nothing to do here
		if (m_epCurrentChangedHandler)
		{
			auto spSource = m_tpSourceAsCV.GetSafeReference();
			if (spSource)
			{
				IFC(m_epCurrentChangedHandler.DetachEventHandler(spSource.Get()));
			}
		}
	}

	protected void RaiseSourceChanged()
	{
		// If the listener is gone, no-op.
		if (m_spListener.Target is IPropertyPathListener spListener)
		{
			((PropertyPathListener)spListener).PropertyPathStepChanged(this);
		}
	}
}

internal partial class SourceAccessPathStep : PropertyPathStep
{
	//public:
	public override void ReConnect(object pSource)
	{
		m_Source = pSource;
	}

	//public override object GetValue() => throw new NotImplementedException();
	public override void SetValue(object pValue)
	{
		//return E_NOTIMPL;
		throw new NotImplementedException();
	}

	//public override CClassInfo GetType2() => throw new NotImplementedException();

	//public override CClassInfo GetSourceType() => throw new NotImplementedException();
	public override bool IsConnected() => m_Source != null;

	//private:
	private object m_Source;
}
partial class SourceAccessPathStep : PropertyPathStep
{
	public override object GetValue()
	{
		return m_Source;
	}

	public override CClassInfo GetType2()
	{
		// This code is inaccessible right now: GetType on PropertyPathSteps and PropertyAccess
		// instances is only used for IValueConverter::ConvertBack's TypeInfo. SourceAccessPathStep
		// is ONLY used when you have an empty binding expression (e.g. binding to the instance itself),
		// a scenario that doesn't cannot TwoWay binding. For correctness it is left here
		// in the event we use the GetType methods for other purposes.

		// TODO UNO
		throw new NotImplementedException();
		//IInspectable* source = m_Source.Get();
		//if (source)
		//{
		//	IFC_RETURN(MetadataAPI::GetClassInfoFromObject_SkipWinRTPropertyOtherType(source, ppType));
		//}
		//else
		//{
		//	//*ppType = MetadataAPI::GetClassInfoByIndex(KnownTypeIndex::Object);
		//	return typeof(object);
		//}
	}

	public override CClassInfo GetSourceType()
	{
		// TODO UNO
		throw new NotImplementedException();
		//IFCEXPECT(m_Source);
		//IFC(GetType(ppType));
	}
}

internal partial class PropertyAccessPathStep : // src\dxaml\xcp\dxaml\lib\PropertyAccessPathStep.h
	PropertyPathStep,
	IPropertyAccessHost
{
	// public:
	public PropertyAccessPathStep()
	{
		m_szProperty = null;
		m_fListenToChanges = false;
		m_pDP = null;
	}

	public void Initialize(
		PropertyPathListener pListener,
		string szProperty,
		bool fListenToChanges)
	{
		Initialize(pListener);
		m_szProperty = szProperty;
		m_fListenToChanges = fListenToChanges;
	}

	public void Initialize(
		PropertyPathListener pListener,
		DependencyProperty pDP,
		bool fListenToChanges)
	{
		Initialize(pListener);
		m_pDP = pDP;
		m_fListenToChanges = fListenToChanges;
	}

	//~PropertyAccessPathStep() override;

	//public:

	// PropertyPathStep overrides
	//_Check_return_ HRESULT ReConnect(_In_ IInspectable *pSource) override;

	//_Check_return_ HRESULT GetValue(_Outptr_ IInspectable **ppValue) override; 
	//_Check_return_ HRESULT SetValue(_In_  IInspectable *pValue) override; 

	//_Check_return_ HRESULT GetType(_Outptr_ const CClassInfo **ppType) override;

	//_Check_return_ HRESULT GetSourceType(_Outptr_ const CClassInfo **ppType) override;


	public override bool IsConnected() => m_tpPropertyAccess is { } && m_tpPropertyAccess.IsConnected();
	public override string DebugGetPropertyName() => m_szProperty;

	//public:

	// IPropertyAccessHost
	//_Check_return_ HRESULT SourceChanged() override;
	public string GetPropertyName() => m_szProperty;

	//protected:

	// PropertyPathStep overrides
	//void DisconnectCurrentItem() override;
	//_Check_return_ HRESULT CollectionViewCurrentChanged() override;

	//private:

	//_Check_return_ HRESULT ConnectToPropertyOnSource(
	//	_In_ IInspectable *pSource,
	//	_In_ bool fListenToChanges);

	//_Check_return_ HRESULT ConnectPropertyAccessForObject(
	//	_In_ IInspectable *pSource,
	//	_In_ bool fListenToChanges,
	//	_Out_ BOOLEAN* pbConnected);

	//void TraceGetterError();
	//void TraceConnectionError(_In_ IInspectable *pSource);

	// protected:

	protected bool m_fListenToChanges;
	protected string m_szProperty;
	protected DependencyProperty m_pDP;
	protected PropertyAccess m_tpPropertyAccess;
}
partial class PropertyAccessPathStep // src\dxaml\xcp\dxaml\lib\PropertyAccessPathStep.cpp
{
	//PropertyAccessPathStep::~PropertyAccessPathStep()
	//{
	//	delete[] m_szProperty;
	//}

	public override void ReConnect(object pSource)
	{
		// Disconnect from the current source
		Disconnect();

		// Null source does nothing for us
		if (pSource == null)
		{
			return;
		}

		ConnectToPropertyOnSource(pSource, m_fListenToChanges);
	}

	protected override void DisconnectCurrentItem()
	{
		if (m_tpPropertyAccess is { })
		{
			m_tpPropertyAccess.DisconnectEventHandlers();
			m_tpPropertyAccess.SetSource(null, fListenToChanges: false);
		}
	}

	public override object GetValue()
	{
		if (!IsConnected())
		{
			return null;
		}

		return m_tpPropertyAccess.GetValue();

		// cleanup:
		//if (FAILED(hr))
		//{
		//	TraceGetterError();
		//}
	}

	private void TraceGetterError()
	{
		// TODO UNO
		throw new NotImplementedException();
	}

	public override void SetValue(object pValue)
	{
		if (!IsConnected())
		{
			throw new InvalidOperationException();
		}

		m_tpPropertyAccess.SetValue(pValue);
	}

	private void ConnectToPropertyOnSource(object pSource, bool fListenToChanges)
	{
		object spInsp;
		bool bConnected = false;
		ICollectionView spSourceAsCV;

		// If the incoming source is null or empty then
		// we're done
		//if (PropertyValue::IsNullOrEmpty(pSource))
		if (pSource is null)
		{
			if (m_tpPropertyAccess is { })
			{
				m_tpPropertyAccess.DisconnectEventHandlers();
				m_tpPropertyAccess.SetSource(null, fListenToChanges: false);
			}
			return;
		}

		// First try to access the property on the source itself
		ConnectPropertyAccessForObject(pSource, fListenToChanges, &bConnected);

		// If accessing the property on the object failed
		// we will check if the source is a collection view and if so
		// will get the property from the current item
		if (!bConnected)
		{
			spSourceAsCV = ctl::query_interface_cast<xaml_data::ICollectionView>(pSource);
			SetPtrValue(m_tpSourceAsCV, spSourceAsCV);
			if (!m_tpSourceAsCV)
			{
				goto Cleanup;
			}

			IFC(AddCurrentChangedEventHandler());

			// Before Phase 2 the current item is suposed to be
			// a PropertyValue
			IFC(m_tpSourceAsCV.get_CurrentItem(&spInsp));

			IFC(ConnectPropertyAccessForObject(spInsp.Get(), fListenToChanges, &bConnected));
		}
	}

	private bool ConnectPropertyAccessForObject(
		object pSource,
		bool fListenToChanges)
	{
		ctl::ComPtr<wfc::IMap<HSTRING, IInspectable*>> spMap;
		ctl::ComPtr<PropertyAccess> spResult;
		ctl::ComPtr<xaml_data::ICustomPropertyProvider> spPropertyProvider;
		ctl::ComPtr<IInspectable> spInsp;
		ctl::ComPtr<IInspectable> spSourceForDP;
		const CClassInfo* pSourceType = NULL;

		*pbConnected = FALSE;

		// If the incoming source is a weak IInspectable then
		// we need to extract the wrapped value from it to
		// see what kind of property access to create
		if (ctl::is < IWeakInspectable > (pSource))
		{
			// We will never wrap an IInspectable in a ValueWeakReference
			spInsp.Attach(ValueWeakReference::get_value_as<xaml::IDependencyObject>(pSource));
			spSourceForDP = pSource;     // If the source is a weak reference wrapper we want to keep that

			ASSERT(!ctl::is < wf::IPropertyValue > (spInsp));
		}
		else
		{
			// The incoming source is just an IInspectable, if it happens to be an
			// IPropertyValue we will get the IInspectable out of it
			spInsp = pSource;
			spSourceForDP = spInsp;       // The source is the object itself then
		}

		// Nothing we can create if the value is NULL
		if (!spInsp)
		{
			if (m_tpPropertyAccess)
			{
				IFC(m_tpPropertyAccess.DisconnectEventHandlers());
				IFC(m_tpPropertyAccess.SetSource(nullptr, /* fListenToChanges */ FALSE));
			}
			goto Cleanup;
		}

		if (m_tpPropertyAccess != nullptr)
		{
			IFC(m_tpPropertyAccess.TryReconnect(spInsp.Get(), !!fListenToChanges, *pbConnected, pSourceType));
			if (*pbConnected)
			{
				goto Cleanup;
			}
		}

		// If this is the first time we connect, or re-connect failed and we didn't resolve a source type yet,
		// resolve it now.
		if (!pSourceType)
		{
			IFC(MetadataAPI::GetClassInfoFromObject_SkipWinRTPropertyOtherType(spInsp.Get(), &pSourceType));
		}

		// See if we can create one of the known types of property accessors
		if (ctl::is < xaml::IDependencyObject > (spInsp))
		{
			// The dependency object property access gets the original source because it
			// can be a weak reference wrapper
			if (m_pDP)
			{
				IFC(DependencyObjectPropertyAccess::CreateInstance(this, spSourceForDP.Get(), pSourceType, m_pDP, fListenToChanges, &spResult));
			}
			else
			{
				IFC(DependencyObjectPropertyAccess::CreateInstance(this, spSourceForDP.Get(), pSourceType, fListenToChanges, &spResult));
			}
		}

		// If we havent resolved the property yet and we were not asked to bind to a DP
		// keep looking
		if (!spResult && !m_pDP)
		{
			// First try to acquire a property access through application metadata
			IFC(PropertyInfoPropertyAccess::CreateInstance(this, spInsp.Get(), pSourceType, fListenToChanges, &spResult));

			// If the metadata doesn't contain information about this property then try
			// the object itself for metadata
			if (!spResult)
			{
				// The object might also be a POCO, try to resolve the property there
				if ((spMap = spInsp.AsOrNull<wfc::IMap<HSTRING, IInspectable*>>()))
				{
					IFC(MapPropertyAccess::CreateInstance(this, spMap.Get(), fListenToChanges, &spResult));
				}
				else
				{
					// Check for ICustomPropertyProvider
					spPropertyProvider = spInsp.AsOrNull<xaml_data::ICustomPropertyProvider>();
					if (!spPropertyProvider)
					{
						// If we couldn't get an ICPP directly, and this is a CLR application, get an
						// ICPP wrapper from there.
						ctl::ComPtr<IInspectable> spWrapper;
						spWrapper.Attach(ReferenceTrackerManager::GetTrackerTarget(spInsp.Get()));
						if (spWrapper)
						{
							spPropertyProvider = spWrapper.AsOrNull<xaml_data::ICustomPropertyProvider>();
						}
					}

					if (spPropertyProvider)
					{
						IFC(PropertyProviderPropertyAccess::CreateInstance(this, spPropertyProvider.Get(), fListenToChanges, &spResult));
					}
				}
			}
		}

		// If we haven't found the property log about it
		if (!spResult)
		{
			TraceConnectionError(spInsp.Get());
		}
		else
		{
			// Remember the property connection
			SetPtrValue(m_tpPropertyAccess, spResult);
			*pbConnected = TRUE;
		}
	}

	private void TraceConnectionError(object pSource)
	{
		// TODO UNO
		throw new NotImplementedException();
	}

	protected override void CollectionViewCurrentChanged()
	{
		object spInsp;
		bool bConnected = false;

		// We do not care about the property access anymore
		// we need to create a new one based on the
		// new current item
		DisconnectCurrentItem();

		spInsp = m_tpSourceAsCV.CurrentItem;

		bConnected = ConnectPropertyAccessForObject(spInsp, m_fListenToChanges);

		// Notify that the value of the source has changed
		RaiseSourceChanged();
	}

	public override CClassInfo GetType2()
	{
		if (m_tpPropertyAccess is null)
		{
			return null;
		}

		return m_tpPropertyAccess.GetType();
	}

	public void SourceChanged()
	{
		RaiseSourceChanged();
	}

	public override CClassInfo GetSourceType()
	{
		if (m_tpPropertyAccess is null)
		{
			return null;
		}

		return m_tpPropertyAccess.GetSourceType();
	}
}

internal partial class IntIndexerPathStep : // src\dxaml\xcp\dxaml\lib\IntIndexerPathStep.h
	PropertyPathStep,
	IIndexedPropertyAccessHost
{
	//public:
	public IntIndexerPathStep()
	{
		m_nIndex = 0;
		m_fListenToChanges = false;
		m_szIndexerName = null;
	}
	public void Initialize(PropertyPathListener pOwner, int nIndex, bool fListenToChanges)
	{
		Initialize(pOwner);
		m_nIndex = nIndex;
		m_fListenToChanges = fListenToChanges;
	}

	public void SourceChanged() => throw new NotImplementedException();

	//    ~IntIndexerPathStep() override;

	//public:
	//    // IPropertyAccessHost
	//    _Check_return_ HRESULT SourceChanged() override;
	public string GetPropertyName()
	{
		return null; // No name for the property
	}

	//    // IIndexedPropertyAccessHost
	//    _Check_return_ HRESULT GetIndexedPropertyName(_Outptr_result_z_ WCHAR **pszPropertyName) override;

	//    // PropertyPathStep overrides
	//    _Check_return_ HRESULT ReConnect(_In_ IInspectable *pSource) override;

	//    _Check_return_ HRESULT GetValue(_Out_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_  IInspectable *pValue) override;

	//    bool IsConnected() override;

	//    _Check_return_ HRESULT GetType(_Outptr_ const CClassInfo **ppType) override;

	//    _Check_return_ HRESULT GetSourceType(_Outptr_ const CClassInfo **ppType) override;

	//protected:
	//    // PropertyPathStep overrides
	//    void DisconnectCurrentItem() override;
	//    _Check_return_ HRESULT CollectionViewCurrentChanged() override;

	//private:
	//    _Check_return_ HRESULT VectorChanged(_In_ wfc::IVectorChangedEventArgs *pArgs);

	//    _Check_return_ HRESULT AddVectorChangedHandler();
	//    _Check_return_ HRESULT SafeRemoveVectorChangedHandler();

	//    _Check_return_ HRESULT GetVectorSize(_Out_ XUINT32 *pnSize);

	//    _Check_return_ HRESULT GetValueAtIndex(_Out_ IInspectable **ppValue);
	//    _Check_return_ HRESULT SetValueAtIndex(_Out_ IInspectable *pValue);

	//    _Check_return_ HRESULT InitializeFromSource(_In_ IInspectable *pRawSource);
	//    _Check_return_ HRESULT InitializeFromVector(_In_ IInspectable *pSource, _Out_ bool *pfResult);

	//    void TraceGetterFailure();
	//    void TraceConnectionFailure(_In_ IInspectable *pSource);

	//private:
	private ICollection<object> m_tpVector;
	private ICollectionView<object> m_tpVectorView;
	PropertyAccess m_tpIndexer;
	int m_nIndex;
	string m_szIndexerName;
	bool m_fListenToChanges;

	//    ctl::EventPtr<VectorChangedEventCallback> m_epVectorChangedEventHandler;
}
partial class IntIndexerPathStep // src\dxaml\xcp\dxaml\lib\IntIndexerPathStep.cpp
{
	~IntIndexerPathStep()
	{
		SafeRemoveVectorChangedHandler();
	}

	protected void DisconnectCurrentItem()
	{
		// fixme@xy: disposable pattern
		//if (m_epVectorChangedEventHandler)
		//{
		//	VERIFYHR(m_epVectorChangedEventHandler.DetachEventHandler(m_tpVector.Get()));
		//}

		m_tpVector.Clear();
		m_tpVectorView.Clear();

		if (m_tpIndexer is { })
		{
			m_tpIndexer.DisconnectEventHandlers();
			m_tpIndexer.Clear();
		}
	}

	protected void CollectionViewCurrentChanged()
	{
		object spItem;

		// Forget the previous vector
		DisconnectCurrentItem();

		spItem = m_tpSourceAsCV.CurrentItem;

		// Connect to the new vector
		InitializeFromSource(spItem);

		// Notify of the change
		RaiseSourceChanged();
	}
	private void VectorChanged(IVectorChangedEventArgs pArgs)
	{
		bool fChanged = false;

		switch (pArgs.CollectionChange)
		{
			case CollectionChange.ItemInserted:
			case CollectionChange.ItemRemoved:
				if (pArgs.Index <= m_nIndex)
				{
					fChanged = true;
				}
				break;

			case CollectionChange.ItemChanged:
				if (pArgs.Index == m_nIndex)
				{
					fChanged = true;
				}
				break;

			case CollectionChange.Reset:
				fChanged = true;
				break;
		}

		if (fChanged)
		{
			RaiseSourceChanged();
		}
	}

	public void SourceChanged()
	{
		RaiseSourceChanged();
	}

	public string GetIndexedPropertyName()
	{
		if (m_szIndexerName == null)
		{
			m_szIndexerName = $"Item[{m_nIndex}]";
		}

		return m_szIndexerName;
	}

	public override void ReConnect(object pSource)
	{
		object spInsp;
		ICollectionView spSourceAsCV;

		// First disconnect from the current source
		Disconnect();

		// A null source is a NOOP
		//if (PropertyValue::IsNullOrEmpty(pSource))
		if (pSource is null)
		{
			Cleanup();
			return;
		}

		InitializeFromSource(pSource);

		// If we were not able to get either a vector or a vector view from the source then
		// see if it is a collection view and if so connect to it
		if (m_tpVector is null && m_tpVectorView is null && m_tpIndexer is null)
		{
			spSourceAsCV = pSource as ICollectionView;
			m_tpSourceAsCV = spSourceAsCV;
			if (m_tpSourceAsCV is null)
			{
				Cleanup();
				return;
			}

			AddCurrentChangedEventHandler();

			// Before Phase 2 the current item is suposed to be
			// a PropertyValue
			spInsp = m_tpSourceAsCV.CurrentItem;

			InitializeFromSource(spInsp);
		}

		void Cleanup()
		{
			if (pSource != null && !IsConnected())
			{
				TraceConnectionFailure(pSource);
			}
		}
	}

	void TraceGetterFailure()
	{
		// TODO UNO
		throw new NotImplementedException();
		//HRESULT hr = S_OK; // WARNING_IGNORES_FAILURES
		//const CClassInfo* pTypeInfo = NULL;
		//const WCHAR* szTraceString = NULL;
		//xstring_ptr strSourceClassName;
		//ctl::ComPtr<IInspectable> spSource;
		//wrl_wrappers::HString strErrorString;
		//ctl::ComPtr<IPropertyPathListener> spListener;

		//if (!DebugOutput::IsLoggingForBindingEnabled())
		//{
		//	goto Cleanup;
		//}

		//IFC(m_tpIndexer->GetSource(spSource.ReleaseAndGetAddressOf()));
		//IFC(MetadataAPI::GetFriendlyRuntimeClassName(spSource.Get(), &strSourceClassName));

		//IFC(m_tpIndexer->GetType(&pTypeInfo));

		//IFC(m_spListener.As(&spListener));
		//IFCEXPECT_ASSERT(spListener.Get());
		//IFC(spListener.Cast<PropertyPathListener>()->GetTraceString(&szTraceString));

		//IFC(DXamlCore::GetCurrentNoCreate()->GetNonLocalizedErrorString(TEXT_BINDINGTRACE_INT_INDEXER_FAILED, strErrorString.GetAddressOf()));

		//DebugOutput::LogBindingErrorMessage(StringCchPrintfWWrapper(
		//	const_cast<WCHAR*>(strErrorString.GetRawBuffer(nullptr)),
		//	m_nIndex,
		//	pTypeInfo->GetName().GetBuffer(),
		//	const_cast<WCHAR*>(strSourceClassName.GetBuffer()),
		//	szTraceString));
	}

	void TraceConnectionFailure(_In_ IInspectable *pSource)
	{
		// TODO UNO
		throw new NotImplementedException();
		//HRESULT hr = S_OK; // WARNING_IGNORES_FAILURES
		//xstring_ptr strClassName;
		//const WCHAR* szTraceString = NULL;
		//wrl_wrappers::HString strErrorString;
		//ctl::ComPtr<IPropertyPathListener> spListener;

		//if (!DebugOutput::IsLoggingForBindingEnabled())
		//{
		//	goto Cleanup;
		//}

		//IFC(MetadataAPI::GetFriendlyRuntimeClassName(pSource, &strClassName));

		//IFC(m_spListener.As(&spListener));
		//IFCEXPECT_ASSERT(spListener.Get());
		//IFC(spListener.Cast<PropertyPathListener>()->GetTraceString(&szTraceString));

		//IFC(DXamlCore::GetCurrentNoCreate()->GetNonLocalizedErrorString(TEXT_BINDINGTRACE_INT_INDEXER_CONNECTION_FAILED, strErrorString.GetAddressOf()));

		//DebugOutput::LogBindingErrorMessage(StringCchPrintfWWrapper(
		//	const_cast<WCHAR*>(strErrorString.GetRawBuffer(nullptr)),
		//	m_nIndex,
		//	const_cast<WCHAR*>(strClassName.GetBuffer()),
		//	szTraceString));
	}

	private void InitializeFromSource(object pRawSource)
	{
		ICustomPropertyProvider spProvider;
		wxaml_interop::TypeName sTypeName = { 0 };
		string strTypeName;
		ctl::ComPtr<IInspectable> spIndex;
		bool fInitialized = false;
		ctl::ComPtr<IInspectable> spSource;
		ctl::ComPtr<IInspectable> spWrapper;
		//spSource.Attach(ValueWeakReference::get_value_as<IInspectable>(pRawSource));
		spSource = pRawSource;

		// Try to look for a vector in the source
		fInitialized = InitializeFromVector(spSource);
		if (!fInitialized)
		{
			// We couldn't find a vector interface in the source, if this is
			// a CLR app try to wrap it in a CLR wrapper to see if we have something
			spWrapper.Attach(ReferenceTrackerManager::GetTrackerTarget(spSource.Get()));
			if (spWrapper)
			{
				fInitialized = InitializeFromVector(spWrapper);
			}
		}

		// We coudn't find a vector, we will look for an integer indexer instead
		if (!fInitialized && (spProvider = spSource as ICustomPropertyProvider) is { })
		{
			PropertyAccess spIndexer;

			// The source at this point is neither a vector or a vector view, default to try
			// to get a custom indexer of type int
			//IFC(strTypeName.Set(STR_LEN_PAIR(L"Int32")));
			//sTypeName.Name = strTypeName.Get();
			//sTypeName.Kind = wxaml_interop::TypeKind_Primitive;
			//IFC(PropertyValue::CreateFromInt32(m_nIndex, &spIndex));
			// ____^ create a WinRT boxed int from c++?
			spIndex = m_nIndex;

			spIndexer = IndexerPropertyAccess::CreateInstance(
				this,
				spProvider, sTypeName,
				spIndex,
				m_fListenToChanges
			);
			m_tpIndexer = spIndexer;
		}

		if (m_fListenToChanges)
		{
			AddVectorChangedHandler();
		}
	}
}
internal partial class StringIndexerPathStep;
