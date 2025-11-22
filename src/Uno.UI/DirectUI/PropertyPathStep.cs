using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Disposables;
using Windows.Foundation.Collections;

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
	public abstract Type GetSourceType();
	public abstract bool IsConnected();

	public abstract Type GetType2();

	public virtual string DebugGetPropertyName() => null;

	//protected:
	//protected abstract void Disconnect();
	protected virtual void DisconnectCurrentItem() { }
	protected virtual void CollectionViewCurrentChanged() =>
		// return E_NOTIMPL;
		throw new NotImplementedException();

	//protected void AddCurrentChangedEventHandler();

	// This method is safe to be called from the destructor
	//protected void SafeRemoveCurrentChangedEventHandler();

	//protected void RaiseSourceChanged();

	//protected:
	protected PropertyPathStep m_tpNext;
	protected WeakReference m_spListener;

	//ctl::EventPtr<CurrentChangedEventCallback> m_epCurrentChangedHandler;
	protected IDisposable m_epCurrentChangedHandler;
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
		if (m_epCurrentChangedHandler is { })
		{
			m_epCurrentChangedHandler.Dispose();
		}

		m_tpSourceAsCV = null;

		DisconnectCurrentItem();
	}

	//~PropertyPathStep()
	//{
	//	SafeRemoveCurrentChangedEventHandler();
	//}

	protected virtual void AddCurrentChangedEventHandler()
	{
		//ASSERT(!m_epCurrentChangedHandler);
		//ASSERT(m_tpSourceAsCV.Get());

		m_tpSourceAsCV.CurrentChanged += OnCurrentChanged;
		m_epCurrentChangedHandler = Disposable.Create(() =>
			m_tpSourceAsCV.CurrentChanged -= OnCurrentChanged
		);

		void OnCurrentChanged(object sender, object args) => CollectionViewCurrentChanged();
	}

	protected void SafeRemoveCurrentChangedEventHandler()
	{
		// if we do not have an event handler allocated
		// this means that we have nothing to do here
		if (m_epCurrentChangedHandler is { })
		{
			//auto spSource = m_tpSourceAsCV.GetSafeReference();
			//if (spSource)
			//{
			//	IFC(m_epCurrentChangedHandler.DetachEventHandler(spSource.Get()));
			//}
			m_epCurrentChangedHandler.Dispose();
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
partial class PropertyPathStep : IDisposable
{
	public void Dispose()
	{
		Disconnect();
	}
}

internal partial class SourceAccessPathStep : // src\dxaml\xcp\dxaml\lib\SourceAccessPathStep.h
	PropertyPathStep
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
partial class SourceAccessPathStep // src\dxaml\xcp\dxaml\lib\SourceAccessPathStep.cpp
{
	public override object GetValue()
	{
		return m_Source;
	}

	public override Type GetType2()
	{
		// This code is inaccessible right now: GetType on PropertyPathSteps and PropertyAccess
		// instances is only used for IValueConverter::ConvertBack's TypeInfo. SourceAccessPathStep
		// is ONLY used when you have an empty binding expression (e.g. binding to the instance itself),
		// a scenario that doesn't cannot TwoWay binding. For correctness it is left here
		// in the event we use the GetType methods for other purposes.

		object source = m_Source;
		if (source is { })
		{
			return source.GetType();
		}
		else
		{
			return typeof(object);
		}
	}

	public override Type GetSourceType()
	{
		if (m_Source is null)
		{
			//IFCEXPECT(m_Source);
			throw new InvalidOperationException();
		}

		return GetType2();
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
		try
		{
			if (!IsConnected())
			{
				return null;
			}

			return m_tpPropertyAccess.GetValue();
		}
		catch (Exception)
		{
			TraceGetterError();
			throw;
		}
	}

	private void TraceGetterError()
	{
		// TODO UNO
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
		bConnected = ConnectPropertyAccessForObject(pSource, fListenToChanges);

		// If accessing the property on the object failed
		// we will check if the source is a collection view and if so
		// will get the property from the current item
		if (!bConnected)
		{
			spSourceAsCV = pSource as ICollectionView;
			m_tpSourceAsCV = spSourceAsCV;
			if (m_tpSourceAsCV is null)
			{
				return;
			}

			AddCurrentChangedEventHandler();

			// Before Phase 2 the current item is suposed to be
			// a PropertyValue
			spInsp = m_tpSourceAsCV.CurrentItem;

			bConnected = ConnectPropertyAccessForObject(spInsp, fListenToChanges);
		}
	}

	private bool ConnectPropertyAccessForObject(
		object pSource,
		bool fListenToChanges)
	{
		bool pbConnected;

		IDictionary<string, object> spMap;
		PropertyAccess spResult = null;
		ICustomPropertyProvider spPropertyProvider;
		object spInsp;
		object spSourceForDP;
		Type pSourceType = null;

		pbConnected = false;

		// If the incoming source is a weak IInspectable then
		// we need to extract the wrapped value from it to
		// see what kind of property access to create
		//if (ctl::is <IWeakInspectable> (pSource))
		//{
		//	// We will never wrap an IInspectable in a ValueWeakReference
		//	spInsp.Attach(ValueWeakReference::get_value_as<xaml::IDependencyObject>(pSource));
		//	spSourceForDP = pSource;     // If the source is a weak reference wrapper we want to keep that

		//	ASSERT(!ctl::is<wf::IPropertyValue>(spInsp));
		//}
		//else
		{
			// The incoming source is just an IInspectable, if it happens to be an
			// IPropertyValue we will get the IInspectable out of it
			spInsp = pSource;
			spSourceForDP = spInsp;       // The source is the object itself then
		}

		// Nothing we can create if the value is NULL
		if (spInsp is null)
		{
			if (m_tpPropertyAccess is { })
			{
				m_tpPropertyAccess.DisconnectEventHandlers();
				m_tpPropertyAccess.SetSource(null, fListenToChanges: false);
			}
			return pbConnected;
		}

		if (m_tpPropertyAccess != null)
		{
			pbConnected = m_tpPropertyAccess.TryReconnect(spInsp, !!fListenToChanges, out pSourceType);
			if (pbConnected)
			{
				return pbConnected;
			}
		}

		// If this is the first time we connect, or re-connect failed and we didn't resolve a source type yet,
		// resolve it now.
		if (pSourceType == null)
		{
			//IFC(MetadataAPI::GetClassInfoFromObject_SkipWinRTPropertyOtherType(spInsp.Get(), &pSourceType));
			pSourceType = spInsp.GetType();
		}

		// See if we can create one of the known types of property accessors
		if (spInsp is DependencyObject)
		{
			// The dependency object property access gets the original source because it
			// can be a weak reference wrapper
			if (m_pDP is { })
			{
				spResult = DependencyObjectPropertyAccess.CreateInstance(this, spSourceForDP, pSourceType, m_pDP, fListenToChanges);
			}
			else
			{
				spResult = CreateFallbackPropertyAccess(this, spSourceForDP, pSourceType, fListenToChanges);
			}
		}

		// If we havent resolved the property yet and we were not asked to bind to a DP
		// keep looking
		if (spResult is null && m_pDP is null)
		{
			// First try to acquire a property access through application metadata
			spResult = PropertyInfoPropertyAccess.CreateInstance(this, spInsp, pSourceType, fListenToChanges);

			// If the metadata doesn't contain information about this property then try
			// the object itself for metadata
			if (spResult is null)
			{
				// The object might also be a POCO, try to resolve the property there
				if ((spMap = spInsp as IDictionary<string, object>) is { })
				{
					spResult = MapPropertyAccess.CreateInstance(this, spMap, fListenToChanges);
				}
				else
				{
					// Check for ICustomPropertyProvider
					spPropertyProvider = spInsp as ICustomPropertyProvider;
					//if (spPropertyProvider is null)
					//{
					//	// If we couldn't get an ICPP directly, and this is a CLR application, get an
					//	// ICPP wrapper from there.
					//	if (spInsp is { } spWrapper)
					//	{
					//		spPropertyProvider = spWrapper as ICustomPropertyProvider;
					//	}
					//}

					if (spPropertyProvider is { })
					{
						spResult = PropertyProviderPropertyAccess.CreateInstance(this, spPropertyProvider, fListenToChanges);
					}
				}
			}

#if HAS_UNO
			// uno: fallback to reflection
			if (spResult is null)
			{
				spResult = CreateFallbackPropertyAccess(this, spInsp, pSourceType, fListenToChanges);
			}
#endif
		}

		// If we haven't found the property log about it
		if (spResult is null)
		{
			TraceConnectionError(spInsp);
		}
		else
		{
			// Remember the property connection
			m_tpPropertyAccess = spResult;
			pbConnected = true;
		}

		return pbConnected;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "`type` may come from `object.GetType()`, which is dynamic.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "`type` may come from `object.GetType()`, which is dynamic.")]
	private static PropertyAccess CreateFallbackPropertyAccess(IPropertyAccessHost owner, object source, Type type, bool fListenToChanges)
		=> ReflectionPropertyAccess.CreateInstance(owner, source, type, fListenToChanges);

	private void TraceConnectionError(object pSource)
	{
		//string strClassName;
		//string szTraceString = null;
		//string strErrorString;
		//IPropertyPathListener spListener;

		//if (!DebugOutput.IsLoggingForBindingEnabled())
		//{
		//	return;
		//}

		//strClassName = MetadataAPI.GetFriendlyRuntimeClassName(pSource);

		//spListener = m_spListener;
		////IFCEXPECT_ASSERT(spListener);
		//szTraceString = ((PropertyPathListener)spListener).GetTraceString();

		//strErrorString = DXamlCore.GetCurrentNoCreate().GetNonLocalizedErrorString(TEXT_BINDINGTRACE_PROPERTY_CONNECTION_FAILED);

		//DebugOutput.LogBindingErrorMessage(string.Format(
		//	CultureInfo.InvariantCulture,
		//	strErrorString,
		//	GetPropertyName(),
		//	strClassName,
		//	szTraceString
		//));
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

	public override Type GetType2()
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

	public override Type GetSourceType()
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
#if HAS_UNO
	private IList m_tpList;
	private IReadOnlyList<object> m_tpReadOnlyList;
	private IObservableVector m_tpObservableVector;
#endif
	private IVector<object> m_tpVector;
	private IVectorView<object> m_tpVectorView;
	private PropertyAccess m_tpIndexer;
	private int m_nIndex;
	private string m_szIndexerName;
	private bool m_fListenToChanges;

	//ctl::EventPtr<VectorChangedEventCallback> m_epVectorChangedEventHandler;
	private IDisposable m_epVectorChangedEventHandler;
}
partial class IntIndexerPathStep // src\dxaml\xcp\dxaml\lib\IntIndexerPathStep.cpp
{
	//~IntIndexerPathStep()
	//{
	//	SafeRemoveVectorChangedHandler();
	//}

	protected override void DisconnectCurrentItem()
	{
		if (m_epVectorChangedEventHandler is { })
		{
			m_epVectorChangedEventHandler.Dispose();
		}

		m_tpList = null;
		m_tpReadOnlyList = null;
		m_tpObservableVector = null;
		m_tpVector = null;
		m_tpVectorView = null;

		if (m_tpIndexer is { })
		{
			m_tpIndexer.DisconnectEventHandlers();
			m_tpIndexer = null;
		}
	}

	protected override void CollectionViewCurrentChanged()
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

	public virtual void SourceChanged()
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
		//object spInsp;
		//ICollectionView spSourceAsCV;

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
		//if (m_tpVector is null && m_tpVectorView is null && m_tpIndexer is null)
		//{
		//	spSourceAsCV = pSource as ICollectionView;
		//	m_tpSourceAsCV = spSourceAsCV;
		//	if (m_tpSourceAsCV is null)
		//	{
		//		Cleanup();
		//		return;
		//	}

		//	AddCurrentChangedEventHandler();

		//	// Before Phase 2 the current item is suposed to be
		//	// a PropertyValue
		//	spInsp = m_tpSourceAsCV.CurrentItem;

		//	InitializeFromSource(spInsp);
		//}

		void Cleanup()
		{
			if (pSource != null && !IsConnected())
			{
				TraceConnectionFailure(pSource);
			}
		}
	}

	//void TraceGetterFailure()
	//{
	//	const CClassInfo* pTypeInfo = NULL;
	//	const WCHAR* szTraceString = NULL;
	//	xstring_ptr strSourceClassName;
	//	ctl::ComPtr<IInspectable> spSource;
	//	wrl_wrappers::HString strErrorString;
	//	ctl::ComPtr<IPropertyPathListener> spListener;

	//	if (!DebugOutput::IsLoggingForBindingEnabled())
	//	{
	//		goto Cleanup;
	//	}

	//	IFC(m_tpIndexer->GetSource(spSource.ReleaseAndGetAddressOf()));
	//	IFC(MetadataAPI::GetFriendlyRuntimeClassName(spSource.Get(), &strSourceClassName));

	//	IFC(m_tpIndexer->GetType(&pTypeInfo));

	//	IFC(m_spListener.As(&spListener));
	//	IFCEXPECT_ASSERT(spListener.Get());
	//	IFC(spListener.Cast<PropertyPathListener>()->GetTraceString(&szTraceString));

	//	IFC(DXamlCore::GetCurrentNoCreate()->GetNonLocalizedErrorString(TEXT_BINDINGTRACE_INT_INDEXER_FAILED, strErrorString.GetAddressOf()));

	//	DebugOutput::LogBindingErrorMessage(StringCchPrintfWWrapper(
	//		const_cast<WCHAR*>(strErrorString.GetRawBuffer(nullptr)),
	//		m_nIndex,
	//		pTypeInfo->GetName().GetBuffer(),
	//		const_cast<WCHAR*>(strSourceClassName.GetBuffer()),
	//		szTraceString));
	//}

	void TraceConnectionFailure(object pSource)
	{
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
		Type sTypeName = default;
		//string strTypeName;
		object spIndex;
		bool fInitialized = false;
		object spSource;
		//object spWrapper;
		spSource = pRawSource;

		// Try to look for a vector in the source
		fInitialized = InitializeFromVector(spSource);
		//if (!fInitialized)
		//{
		//	// We couldn't find a vector interface in the source, if this is
		//	// a CLR app try to wrap it in a CLR wrapper to see if we have something
		//	//spWrapper.Attach(ReferenceTrackerManager.GetTrackerTarget(spSource));
		//	if (spWrapper)
		//	{
		//		fInitialized = InitializeFromVector(spWrapper);
		//	}
		//}

		// We coudn't find a vector, we will look for an integer indexer instead
		if (!fInitialized && (spProvider = spSource as ICustomPropertyProvider) is { })
		{
			PropertyAccess spIndexer;

			// The source at this point is neither a vector or a vector view, default to try
			// to get a custom indexer of type int
			//strTypeName = "Int32";
			//sTypeName.Name = strTypeName;
			//sTypeName.Kind = TypeKind.Primitive;
			sTypeName = typeof(int);
			spIndex = m_nIndex;

			spIndexer = IndexerPropertyAccess.CreateInstance(
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

	private bool InitializeFromVector(object pSource)
	{
		bool pfResult;
		object source = pSource;
		// We should be clean at this point
		//ASSERT(!m_tpVector && !m_tpVectorView && !m_tpIndexer);

		// Not initialzed yet
		pfResult = false;

		// Get the vector from the property value
		// if the source is not a vector, then nothing to do
		//if (source is IBindableVector spBindableVector)
		//{
		//	IVector<object> bindableVectorWrapper;
		//	if (source is INotifyCollectionChanged spINCC)
		//	{
		//		BindableObservableVectorWrapper.CreateInstance(spBindableVector, spINCC, bindableVectorWrapper);
		//	}
		//	else if (source is IBindableObservableVector spObservableBindable)
		//	{
		//		BindableObservableVectorWrapper.CreateInstance(spObservableBindable, bindableVectorWrapper);
		//	}
		//	else
		//	{
		//		bindableVectorWrapper = spBindableVector as IVector<object>;
		//	}
		//
		//	m_tpVector = bindableVectorWrapper;
		//	pfResult = true;
		//}
#if HAS_UNO
		if (source is IObservableVector observableVector)
		{
			m_tpObservableVector = observableVector;
			pfResult = true;
		}
#endif
		else if (source is IVector<object> spVector)
		{
			m_tpVector = spVector;
			pfResult = true;
		}
		// The source could also be a vector view, account for that
		else if (source is IVectorView<object> spVectorView)
		{
			m_tpVectorView = spVectorView;
			pfResult = true;
		}
#if HAS_UNO
		else if (source is IList list)
		{
			m_tpList = list;
			pfResult = true;
		}
		else if (source is IReadOnlyList<object> readonlyList)
		{
			m_tpReadOnlyList = readonlyList;
			pfResult = true;
		}
#endif
		//else if (source is IBindableVectorView spBindableView)
		//{
		//	m_tpVectorView = spBindableView as IVectorView<object>;
		//	pfResult = true;
		//}
		//else if (source is ValidationErrorsCollection validationErrorsCollection)
		//{
		//	// We want to create our own special wrapper to ensures that Text="{Binding Path=(Validation.Errors)[0].ErrorMessage}"
		//	// works as expected
		//	m_tpVector = ValidationErrorsObservableVectorWrapper.CreateInstanceAsVector(validationErrorsCollection);
		//	pfResult = true;
		//}

		return pfResult;
	}

	private void AddVectorChangedHandler()
	{
		IObservableVector<object> spObservableVector;

		//spObservableVector = m_tpVector as IObservableVector<object>;
		spObservableVector = (m_tpVector as IObservableVector<object> ?? m_tpList as IObservableVector<object>);
		if (spObservableVector is { })
		{
			spObservableVector.VectorChanged += OnVectorChanged;
			m_epVectorChangedEventHandler = Disposable.Create(() =>
				spObservableVector.VectorChanged -= OnVectorChanged
			);

			void OnVectorChanged(object sender, IVectorChangedEventArgs args) => VectorChanged(args);
		}
#if HAS_UNO
		else if (m_tpObservableVector is { })
		{
			m_tpObservableVector.UntypedVectorChanged += OnUntypedVectorChanged;
			m_epVectorChangedEventHandler = Disposable.Create(() =>
				m_tpObservableVector.UntypedVectorChanged -= OnUntypedVectorChanged
			);

			void OnUntypedVectorChanged(object sender, IVectorChangedEventArgs args) => VectorChanged(args);
		}
#endif
	}

	[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "...")]
	private void SafeRemoveVectorChangedHandler()
	{
		if (m_epVectorChangedEventHandler is { })
		{
			var spVector = m_tpVector;
			if (spVector is { })
			{
				m_epVectorChangedEventHandler.Dispose();
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "disabled imported code")]
	private int GetVectorSize()
	{
#if !HAS_UNO
		int pnSize = default;
		if (!(m_tpVector is { } || m_tpVectorView is { }))
		{
			throw new InvalidOperationException();
		}

		if (m_tpVector is { })
		{
			pnSize = m_tpVector.Count;
		}
		else if (m_tpVectorView is { })
		{
			pnSize = m_tpVectorView.Count;
		}
		else
		{
			throw new InvalidOperationException();
		}

		return pnSize;
#else
		return
			m_tpList?.Count ??
			m_tpReadOnlyList?.Count ??
			m_tpObservableVector?.Count ??
			m_tpVector?.Count ??
			m_tpVectorView?.Count ??
			// m_tpIndexer not supported
			throw new InvalidOperationException();
#endif
	}

	private object GetValueAtIndex()
	{
#if !HAS_UNO
		object ppValue = default;

		if (!(m_tpVector is { } || m_tpVectorView is { } || m_tpIndexer is { }))
		{
			throw new InvalidOperationException();
		}

		if (m_tpVector is { })
		{
			ppValue = m_tpVector[m_nIndex];
		}
		else if (m_tpVectorView is { })
		{
			ppValue = m_tpVectorView[m_nIndex];
		}
		else
		{
			ppValue = m_tpIndexer.GetValue();
		}

		return ppValue;
#else
		return (m_tpList ?? m_tpReadOnlyList ?? m_tpObservableVector ?? m_tpVector ?? m_tpVectorView ?? m_tpIndexer as object) switch
		{
			IList indexable => indexable[m_nIndex],
			IVectorView<object> indexable => indexable[m_nIndex],
			IReadOnlyList<object> indexable => indexable[m_nIndex],
			IObservableVector indexable => indexable[m_nIndex],
			IVector<object> indexable => indexable[m_nIndex],
			PropertyAccess indexable => indexable.GetValue(),

			_ => throw new InvalidOperationException(),
		};
#endif
	}

	private void SetValueAtIndex(object pValue)
	{
#if !HAS_UNO
		if (!((m_tpVector is { } || m_tpIndexer is { }) && m_tpVectorView is not { }))
		{
			throw new InvalidOperationException();
		}

		if (m_tpVector is { })
		{
			m_tpVector[m_nIndex] = pValue;
		}
		else
		{
			m_tpIndexer.SetValue(pValue);
		}
#else
		_ = (m_tpList ?? m_tpReadOnlyList ?? m_tpObservableVector ?? m_tpVector ?? m_tpVectorView ?? m_tpIndexer as object) switch
		{
			IList indexable => indexable[m_nIndex] = pValue,
			IVectorView<object> indexable => throw new NotImplementedException(),
			IReadOnlyList<object> indexable => throw new NotImplementedException(),
			IObservableVector indexable => throw new NotImplementedException(),
			IVector<object> indexable => indexable[m_nIndex] = pValue,
			PropertyAccess indexable => indexable.GetValue(),

			_ => throw new NotImplementedException(),
		};
#endif
	}

	public override bool IsConnected()
	{
#if !HAS_UNO
		//HRESULT hr = S_OK; // WARNING_IGNORES_FAILURES
		bool fReturn = false;
		int nSize = 0;

		// No vector or indexer means we're not connected
		if (m_tpVector is null && m_tpVectorView is null && m_tpIndexer is null)
		{
			return fReturn;
		}

		// Check if we're accessing the data through an indexer or through IVector
		if (m_tpIndexer is { })
		{
			// If we have an indexer, nothing else to check, we're connected
			fReturn = true;
		}
		else
		{
			// We're connected as long as 1) there's a vector
			// and 2) the index is within range
			nSize = GetVectorSize();
			fReturn = m_nIndex < nSize ? true : false;
		}

		return fReturn;
#else
		return
			// If we have an indexer, nothing else to check, we're connected
			(m_tpIndexer is { }) ||
			// We're connected as long as 1) there's a vector and 2) the index is within range
			(
				m_tpList?.Count ??
				m_tpReadOnlyList?.Count ??
				m_tpObservableVector?.Count ??
				m_tpVector?.Count ??
				m_tpVectorView?.Count ??
				0
			) > m_nIndex;
#endif
	}

	public override object GetValue()
	{
		object ppValue = default;
		//try
		{
			// If we're not connected nothing to do
			if (!IsConnected())
			{
				ppValue = null;
				return ppValue;
			}

			// Read the value
			ppValue = GetValueAtIndex();
		}
		//catch (Exception)
		//{
		//	TraceGetterFailure();
		//	throw;
		//}

		return ppValue;
	}

	public override void SetValue(object pValue)
	{
		// Set value can only work if we're connected and the caller must know this
		if (!IsConnected())
		{
			throw new InvalidOperationException();
		}

		// Write the value
		SetValueAtIndex(pValue);
	}

	public override Type GetType2()
	{
		// If we have an indexer then get the type of it directly
		if (m_tpIndexer is { })
		{
			return m_tpIndexer.GetType2();
		}

		// Since we operate only with IVector<object>/IVectorView<object> the
		// type will always be of object
		return typeof(object);
	}

	public override Type GetSourceType()
	{
		if (m_tpIndexer is null)
		{
			throw new InvalidOperationException();
		}

		return m_tpIndexer.GetSourceType();
	}
}

internal partial class StringIndexerPathStep : // src\dxaml\xcp\dxaml\lib\StringIndexerPathStep.h
	PropertyPathStep,
	IIndexedPropertyAccessHost
{
	//public:
	public StringIndexerPathStep()
	{
		m_szIndex = null;
		m_fListenToChanges = false;
		m_szIndexerName = null;
	}
	public void Initialize(
		PropertyPathListener pOwner,
		string szIndex,
		bool fListenToChanges)
	{
		Initialize(pOwner);
		m_szIndex = szIndex;
		m_fListenToChanges = fListenToChanges;
	}
	//    using PropertyPathStep::Initialize;
	//    ~StringIndexerPathStep() override;

	//public:
	//    // PropertyPathStep overrides
	//    _Check_return_ HRESULT ReConnect(_In_ IInspectable *pSource) override;
	//    _Check_return_ HRESULT GetValue(_Out_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_  IInspectable *pValue) override;
	public override bool IsConnected() => m_tpPropertyAccess is { } && m_tpPropertyAccess.IsConnected();
	//    bool IsConnected() override
	//    { return m_tpPropertyAccess && m_tpPropertyAccess->IsConnected(); }
	//    _Check_return_ HRESULT GetType(_Outptr_ const CClassInfo **ppType) override;
	//    _Check_return_ HRESULT GetSourceType(_Outptr_ const CClassInfo **ppType) override;

	//protected:
	//    // PropertyPathStep overrides
	//    void DisconnectCurrentItem() override;
	//    _Check_return_ HRESULT CollectionViewCurrentChanged() override;

	//private:
	//    _Check_return_ HRESULT InitializeFromSource(_In_ IInspectable *pRawSource);
	//    void TraceGetterFailure();
	//    void TraceConnectionFailure(_In_ IInspectable *pSource);

	//public:
	//    // IPropertyAccessHost 
	//    _Check_return_ HRESULT SourceChanged() override;
	public string GetPropertyName() => m_szIndex;
	//    // IIndexedPropertyAccessHost
	//    _Check_return_ HRESULT GetIndexedPropertyName(_Outptr_result_z_ WCHAR **pszPropertyName) override;

	//private:
	private PropertyAccess m_tpPropertyAccess;
	private string m_szIndex;
	private bool m_fListenToChanges;
	private string m_szIndexerName;
}
partial class StringIndexerPathStep // src\dxaml\xcp\dxaml\lib\StringIndexerPathStep.cpp
{
	//~StringIndexerPathStep()
	//{
	//	delete[] m_szIndex;
	//	delete[] m_szIndexerName;
	//	m_szIndex = null;
	//}

	protected override void DisconnectCurrentItem()
	{
		if (m_tpPropertyAccess is { })
		{
			m_tpPropertyAccess.DisconnectEventHandlers();
			m_tpPropertyAccess = null;
		}
	}

	protected override void CollectionViewCurrentChanged()
	{
		object spItem;

		// Disconnect from the vector source
		DisconnectCurrentItem();

		// Now get the new current value and
		// try to connect again to it
		spItem = m_tpSourceAsCV.CurrentItem;

		InitializeFromSource(spItem);

		// Notify the source of changes
		RaiseSourceChanged();
	}

	public override void ReConnect(object pSource)
	{
		object spInsp;
		ICollectionView spSourceAsCV;

		// First cleanup ourselves
		Disconnect();

		// If the value is null or empty nothing to do
		if (pSource == null)
		{
			Cleanup();
			return;
		}

		// Get the property map out, if it is not one then nothing to do
		InitializeFromSource(pSource);
		if (m_tpPropertyAccess is null)
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

		Cleanup();

		void Cleanup()
		{
			if (pSource != null && !IsConnected())
			{
				TraceConnectionFailure(pSource);
			}
		}
	}

	private void InitializeFromSource(object pRawSource)
	{
		IDictionary<string, object> spMap;
		ICustomPropertyProvider spPropertyProvider;
		Type sTypeName = default;
		//string strTypeName;
		object spIndex;
		PropertyAccess spPropertyAccess;
		object spSource;
		spSource = pRawSource;

		spMap = spSource as IDictionary<string, object>;
		if (spMap is { })
		{
			spPropertyAccess = MapPropertyAccess.CreateInstance(this, spMap, m_fListenToChanges);
			m_tpPropertyAccess = spPropertyAccess;
		}
		else
		{
			spPropertyProvider = spSource as ICustomPropertyProvider;
			if (spPropertyProvider is { })
			{
				//strTypeName = "String";

				//sTypeName.Name = strTypeName;
				//sTypeName.Kind = TypeKind.Primitive;
				sTypeName = typeof(string);

				//PropertyValue.CreateFromString(wrl_wrappers.HStringReference(m_szIndex, wcslen(m_szIndex)), &spIndex);
				spIndex = m_szIndex;

				spPropertyAccess = IndexerPropertyAccess.CreateInstance(
					this,
					spPropertyProvider,
					sTypeName,
					spIndex,
					m_fListenToChanges);
				m_tpPropertyAccess = spPropertyAccess;
			}
		}
	}

	private void TraceConnectionFailure(object pSource)
	{
		return;

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

		//IFC(DXamlCore::GetCurrentNoCreate()->GetNonLocalizedErrorString(TEXT_BINDINGTRACE_STR_INDEXER_CONNECTION_FAILED, strErrorString.GetAddressOf()));

		//DebugOutput::LogBindingErrorMessage(StringCchPrintfWWrapper(
		//	const_cast<WCHAR*>(strErrorString.GetRawBuffer(nullptr)),
		//	m_szIndex,
		//	const_cast<WCHAR*>(strClassName.GetBuffer()),
		//	szTraceString));

		//Cleanup:
		//	return;
	}

	public override object GetValue()
	{
		//try
		{
			if (!IsConnected())
			{
				return null;
			}

			return m_tpPropertyAccess.GetValue();
		}
		//catch (Exception)
		//{
		//	TraceGetterFailure();
		//	throw;
		//}
	}

	//private void TraceGetterFailure()
	//{
	//	const CClassInfo* pTypeInfo = NULL;
	//	const WCHAR* szTraceString = NULL;
	//	xstring_ptr strSourceClassName;
	//	ctl::ComPtr<IInspectable> spSource;
	//	wrl_wrappers::HString strErrorString;
	//	ctl::ComPtr<IPropertyPathListener> spListener;

	//	if (!DebugOutput::IsLoggingForBindingEnabled())
	//	{
	//		goto Cleanup;
	//	}

	//	IFC(m_tpPropertyAccess->GetSource(&spSource));
	//	IFC(MetadataAPI::GetFriendlyRuntimeClassName(spSource.Get(), &strSourceClassName));

	//	IFC(m_tpPropertyAccess->GetType(&pTypeInfo));

	//	IFC(m_spListener.As(&spListener));
	//	IFCEXPECT_ASSERT(spListener.Get());
	//	IFC(spListener.Cast<PropertyPathListener>()->GetTraceString(&szTraceString));

	//	IFC(DXamlCore::GetCurrentNoCreate()->GetNonLocalizedErrorString(TEXT_BINDINGTRACE_STR_INDEXER_FAILED, strErrorString.GetAddressOf()));

	//	DebugOutput::LogBindingErrorMessage(StringCchPrintfWWrapper(
	//		const_cast<WCHAR*>(strErrorString.GetRawBuffer(nullptr)),
	//		m_szIndex,
	//		pTypeInfo->GetName().GetBuffer(),
	//		const_cast<WCHAR*>(strSourceClassName.GetBuffer()),
	//		szTraceString));
	//}

	public override void SetValue(object pValue)
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		m_tpPropertyAccess.SetValue(pValue);
	}

	public override Type GetType2()
	{
		if (m_tpPropertyAccess is null) throw new InvalidOperationException();

		// Use the type of the property access instead of assuming
		// that everything is of type object. We can have indexers
		// that accept a string but are of different types.
		return m_tpPropertyAccess.GetType();
	}

	public void SourceChanged()
	{
		RaiseSourceChanged();
	}

	public string GetIndexedPropertyName()
	{
		if (m_szIndexerName == null)
		{
			m_szIndexerName = $"Item[{m_szIndex}]";
		}

		return m_szIndexerName;
	}

	public override Type GetSourceType()
	{
		if (m_tpPropertyAccess is null) throw new InvalidOperationException();

		return m_tpPropertyAccess.GetSourceType();
	}
}
