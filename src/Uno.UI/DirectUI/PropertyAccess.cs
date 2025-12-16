using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;

namespace DirectUI;


/// <summary>
/// Defines the interfaces for accessing a single property
/// public abstracts away the differences between the types of properties
/// supported.
/// </summary>
internal abstract class PropertyAccess // : public ctl::WeakReferenceSource
{
	//public:
	public abstract object GetValue();
	public abstract void SetValue(object pValue);
	public abstract Type GetType2();
	public abstract object GetSource();
	public abstract void SetSource(object pSource, bool fListenToChanges);
	public abstract Type GetSourceType();
	public abstract void DisconnectEventHandlers();
	public abstract bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType);
	public abstract bool IsConnected();
}

internal interface IPropertyAccessHost
{
	void SourceChanged();
	string GetPropertyName();
}
internal interface IIndexedPropertyAccessHost : IPropertyAccessHost
{
	string GetIndexedPropertyName();
};

internal partial class PropertyInfoPropertyAccess : // src\dxaml\xcp\dxaml\lib\PropertyInfoPropertyAccess.h
	PropertyAccess,
	INPCListenerBase
{
	//public:
	//    _Check_return_ HRESULT Initialize(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ IInspectable *pSource,
	//        _In_ const Type *pSourceType,
	//        _In_ const CCustomProperty* pProperty);
	//    using PropertyAccess::Initialize;

	public PropertyInfoPropertyAccess()
	{
		m_pOwner = null;
		m_pSourceType = null;
		m_pProperty = null;
	}

	//    _Check_return_ HRESULT OnPropertyChanged() override;
	//    _Ret_notnull_ const wchar_t* GetPropertyName() override;

	//public:
	//    ~PropertyInfoPropertyAccess() override;

	//public:
	//    // IPropertyAccess
	//    _Check_return_ HRESULT GetValue(_COM_Outptr_result_maybenull_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_ IInspectable *pValue) override;
	//    _Check_return_ HRESULT GetType(_Outptr_ const Type **ppType) override;
	//    bool IsConnected() override;
	//    _Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//    _Check_return_ HRESULT GetSource(_Outptr_ IInspectable** ppSource) override;
	public override Type GetSourceType() => m_pSourceType;
	//    _Check_return_ HRESULT TryReconnect(_In_ IInspectable* pSource, _In_ BOOLEAN fListenToChanges, _Inout_ BOOLEAN& bConnected, _Inout_ const Type*& pResolvedType) override;
	//    _Check_return_ HRESULT DisconnectEventHandlers() override;

	//public:
	//    static _Check_return_ HRESULT CreateInstance(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ IInspectable *pSource, 
	//        _In_ const Type *pSourceType,
	//        _In_ bool fListenToChanges,
	//        _Outptr_ PropertyAccess **ppPropertyAccess);

	//private:
	private IPropertyAccessHost m_pOwner;
	private Type m_pSourceType;
	private object m_tpSource;
	private CustomProperty m_pProperty;
}
partial class PropertyInfoPropertyAccess // src\dxaml\xcp\dxaml\lib\PropertyInfoPropertyAccess.cpp
{
	public void Initialize(
		IPropertyAccessHost pOwner,
		object pSource,
		Type pSourceType,
		CustomProperty pProperty)
	{
		m_pOwner = pOwner;

		m_tpSource = pSource;
		m_pProperty = pProperty;
		m_pSourceType = pSourceType;
	}

	//~PropertyInfoPropertyAccess()
	//{
	//	var spSource = m_tpSource;
	//	if (spSource is { })
	//	{
	//		DisconnectPropertyChangedHandler(spSource);
	//	}
	//}

	public override object GetValue()
	{
		object ppValue;

		if (IsConnected())
		{
			ppValue = (m_pProperty as CustomProperty).GetXamlPropertyNoRef().GetValue(m_tpSource);
		}
		else
		{
			ppValue = null;
		}

		return ppValue;
	}

	public override void SetValue(object pValue)
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		(m_pProperty as CustomProperty).GetXamlPropertyNoRef().SetValue(m_tpSource, pValue);
	}

	public override Type GetType2()
	{
		if (!IsConnected()) throw new InvalidOperationException();
		return m_pProperty.GetPropertyType();
	}

	public override bool IsConnected()
	{
		return m_pProperty is { } && m_tpSource is { } && m_pProperty.IsValid();
	}

	public override void SetSource(object pSource, bool fListenToChanges)
	{
		if (fListenToChanges)
		{
			// Disconnect the handler on the previous source, and attach to the new source. Both
			// old and new source are allowed to be null.
			UpdatePropertyChangedHandler(m_tpSource, pSource);
		}
		else
		{
			DisconnectEventHandlers();
		}
		m_tpSource = pSource;
	}

	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		var bConnected = false;

		pResolvedType = pSource.GetType();

		if (m_pSourceType == pResolvedType)
		{
			SetSource(pSource, fListenToChanges);
			bConnected = true;
		}

		return bConnected;
	}

	public override object GetSource()
	{
		var ppSource = m_tpSource;

		return ppSource;
	}

	public override void DisconnectEventHandlers()
	{
		if (m_pProperty is { } && m_tpSource is { })
		{
			DisconnectPropertyChangedHandler(m_tpSource);
		}
	}

	protected partial void OnPropertyChanged()
	{
		m_pOwner.SourceChanged();
	}

	protected partial string GetPropertyName()
	{
		return m_pOwner.GetPropertyName();
	}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		object pSource,
		Type pSourceType,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		PropertyInfoPropertyAccess spResult;
		//DependencyProperty pProperty = null;
		string pszPropertyName = pOwner.GetPropertyName();
		if (pSource is null) throw new InvalidOperationException();

		ppPropertyAccess = null; // By default no property is generated

#if !HAS_UNO
		// We only care about types that are in the metadata because they are explicitly bindable
		// this will remove things like DPs as we need those to be resolved in a specialized form.
		//if (!pSourceType.IsBindable())
		if (BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(pSourceType) is { })
		{
			return ppPropertyAccess;
		}

		// Now try to resolve the property by name.
		pProperty = MetadataAPI.TryGetPropertyByName(pSourceType, pszPropertyName);
		// ___^ are we actually reflecting on the generated bindable code, or actually fetching the dp from the sourceType?
		// ____ it cant be the latter, since pSourceType cant guarantee this
		if (pProperty == null)
		{
			// We failed to find the property, bail out.
			return ppPropertyAccess;
		}
#else
		var bindableType = BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(pSourceType);
		var bindableProperty = bindableType?.GetProperty(pszPropertyName);
		if (bindableProperty == null)
		{
			return ppPropertyAccess;
		}

		var pProperty = CustomProperty.FromBindableProperty(bindableProperty, pszPropertyName);
#endif

		// Create a property access object.
		spResult = new();
		spResult.Initialize(pOwner, pSource, pSourceType, pProperty as CustomProperty);

		if (fListenToChanges)
		{
			// If we're listenting for changes need to keep the name of the property
			// to compare it against the name of the properties that are changing
			spResult.AddPropertyChangedHandler(pSource);
		}

		ppPropertyAccess = spResult;
		return ppPropertyAccess;
	}
}
partial class PropertyInfoPropertyAccess // src\dxaml\xcp\dxaml\lib\INPCListenerBase.h
{
	//protected:
	protected partial void AddPropertyChangedHandler(object pSource);
	protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource);
	protected partial void DisconnectPropertyChangedHandler(object pSource);

	protected partial void OnPropertyChanged();
	protected partial string GetPropertyName();

	//private:
	private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs);

	//private:
	private IDisposable m_epPropertyChangedHandler;
	private int m_propertyNameLength;
}
partial class PropertyInfoPropertyAccess // src\dxaml\xcp\dxaml\lib\INPCListenerBase.cpp
{
	protected partial void AddPropertyChangedHandler(object pSource)
	{
		if (m_epPropertyChangedHandler is not null) throw new InvalidOperationException();

		UpdatePropertyChangedHandler(null, pSource);
	}

	protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource)
	{
		INotifyPropertyChanged spINPC;

		string buffer = this.GetPropertyName();
		if (string.IsNullOrEmpty(buffer)) throw new InvalidOperationException();
		m_propertyNameLength = buffer.Length;

		if (pOldSource is { })
		{
			DisconnectPropertyChangedHandler(pOldSource);
		}

		if (pNewSource is { })
		{
			if ((spINPC = pNewSource as INotifyPropertyChanged) is { })
			{
				spINPC.PropertyChanged += OnPropertyChanged;
				m_epPropertyChangedHandler = Disposable.Create(() =>
					spINPC.PropertyChanged -= OnPropertyChanged
				);
				void OnPropertyChanged(object sender, PropertyChangedEventArgs args) => OnPropertyChangedCallback(args);
			}
		}
	}
	protected partial void DisconnectPropertyChangedHandler(object pSource)
	{
		if (m_epPropertyChangedHandler is { })
		{
			m_epPropertyChangedHandler.Dispose();
		}
	}

	private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs)
	{
		bool propertyChanged = false;
		string strProperty;

		strProperty = pArgs.PropertyName;

		// If it is not then we need to compare the string vs. the string in the listener to see if the
		// change affects the listener
		if (strProperty != null)
		{
			int length = 0;
			string buffer = this.GetPropertyName();
			(string current, length) = (strProperty, strProperty.Length);
			propertyChanged = (length == m_propertyNameLength) && (current == buffer);
		}
		else
		{
			// If the property name is null, which means empty, then we want the change
			propertyChanged = true;
		}

		// Notify the class if the property we're listening to changed
		if (propertyChanged)
		{
			OnPropertyChanged();
		}
	}
}

/// <summary>
/// Defines the class to access to properties stored in an IMap
/// implementation
/// </summary>
internal partial class MapPropertyAccess : // src\dxaml\xcp\dxaml\lib\MapPropertyAccess.h
	PropertyAccess
{
	//public:
	public MapPropertyAccess()
	{
		m_pOwner = null;
	}

	//    ~MapPropertyAccess() override;

	//    _Check_return_ HRESULT GetValue(_COM_Outptr_result_maybenull_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_ IInspectable *pValue) override;
	//    _Check_return_ HRESULT GetType(_Outptr_ const Type **ppType) override;
	//    bool IsConnected() override;
	//    _Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//    _Check_return_ HRESULT GetSource(_Outptr_ IInspectable **ppSource) override;
	//    _Check_return_ HRESULT GetSourceType(_Outptr_ const Type **ppType) override;
	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		pResolvedType = default;
		return false;
	}
	//    _Check_return_ HRESULT DisconnectEventHandlers() override;

	//public:

	//    static _Check_return_ HRESULT CreateInstance(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ wfc::IMap<HSTRING, IInspectable *> *pSource,
	//        _In_ bool fListenToChanges,
	//        _Outptr_ PropertyAccess **ppPropertyAccess);

	//private:

	//    void Initialize(
	//        _In_ IPropertyAccessHost* const pOwner,
	//        _In_ wfc::IMap<HSTRING, IInspectable *>* const pSource);

	//protected:

	//    using PropertyAccess::Initialize;

	//private:

	//    _Check_return_ HRESULT MapKeyChanged();

	//    _Check_return_ HRESULT AddKeyChangedEventHandler();

	//    // This method is safe to be called from the destructor
	//    _Check_return_ HRESULT SafeRemoveKeyChangedEventHandler();

	//private:
	private void OnMapChanged(IMapChangedEventArgs<string> pArgs)
	{
		string strKey;

		strKey = pArgs.Key;

		if (strKey == m_strProperty)
		{
			MapKeyChanged();
		}
	}

	//private:
	private IPropertyAccessHost m_pOwner;
	private IDictionary<string, object> m_tpSource;
	private string m_strProperty;
	//ctl::EventPtr<MapChangedEventCallback> m_epMapChangedEventHandler;
	private IDisposable m_epMapChangedEventHandler;
}
partial class MapPropertyAccess // src\dxaml\xcp\dxaml\lib\MapPropertyAccess.cpp
{
	private void Initialize(
		IPropertyAccessHost pOwner,
		IDictionary<string, object> pSource)
	{
		m_pOwner = pOwner;
		m_tpSource = pSource;
	}

	//~MapPropertyAccess()
	//{
	//	SafeRemoveKeyChangedEventHandler();
	//}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		IDictionary<string, object> pSource,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		MapPropertyAccess spResult;

		// By default there's no property access
		ppPropertyAccess = null;

		spResult = new MapPropertyAccess();
		spResult.Initialize(pOwner, pSource);

		spResult.m_strProperty = spResult.m_pOwner.GetPropertyName();

		if (fListenToChanges)
		{
			spResult.AddKeyChangedEventHandler();
		}

		ppPropertyAccess = spResult;

		return ppPropertyAccess;
	}

	public override object GetValue()
	{
		if (IsConnected())
		{
			return m_tpSource.TryGetValue(m_strProperty, out var result) ? result : null;
		}
		else
		{
			return null;
			// This method will only be called if we're connected
			//ASSERT(false);
		}
	}

	public override void SetValue(object pValue)
	{
		//bool bReplaced = false;

		// This method will only be called if we're connected
		//ASSERT(IsConnected());

		m_tpSource[m_strProperty] = pValue;
	}

	private void MapKeyChanged()
	{
		m_pOwner.SourceChanged();
	}

	public override Type GetType2()
	{
		return typeof(object);
	}

	public override bool IsConnected()
	{
		bool bHasKey = false;

		if (m_tpSource is { })
		{
			// Check if the dictionary contains the key that we're looking for
			bHasKey = m_tpSource.ContainsKey(m_strProperty);
		}

		return bHasKey ? true : false;
	}

	public override void SetSource(object pSource, bool fListenToChanges)
	{
		DisconnectEventHandlers();
		m_tpSource = pSource as IDictionary<string, object>;

		if (fListenToChanges)
		{
			AddKeyChangedEventHandler();
		}
	}

	public override object GetSource()
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		return m_tpSource;
	}

	public override void DisconnectEventHandlers()
	{
		if (m_epMapChangedEventHandler is { } && m_tpSource is { })
		{
			m_epMapChangedEventHandler.Dispose();
		}
	}

	private void AddKeyChangedEventHandler()
	{
		IObservableMap<string, object> spObservable;

		spObservable = m_tpSource as IObservableMap<string, object>;
		if (spObservable is { })
		{
			spObservable.MapChanged += OnMapChanged;
			m_epMapChangedEventHandler = Disposable.Create(() =>
				spObservable.MapChanged -= OnMapChanged
			);

			void OnMapChanged(IObservableMap<string, object> pSender, IMapChangedEventArgs<string> pArgs)
			{
				this.OnMapChanged(pArgs);
			}
		}
	}

	[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "...")]
	private void SafeRemoveKeyChangedEventHandler()
	{
		if (m_epMapChangedEventHandler is { })
		{
			//var spSource = m_tpSource;
			//if (spSource is { })
			{
				m_epMapChangedEventHandler.Dispose();
			}
		}
	}

	public override Type GetSourceType()
	{
		return m_tpSource?.GetType();
	}
}

internal partial class DependencyObjectPropertyAccess : // src\dxaml\xcp\dxaml\lib\DependencyObjectPropertyAccess.h
	PropertyAccess
{
	//public:

	//	DependencyObjectPropertyAccess();
	//	~DependencyObjectPropertyAccess() override;

	//	_Check_return_ HRESULT GetValue(_COM_Outptr_result_maybenull_ IInspectable **ppValue) override;
	//	_Check_return_ HRESULT SetValue(_In_ IInspectable *pValue) override;
	//	_Check_return_ HRESULT GetType(_Outptr_ const Type **ppType) override;
	public override bool IsConnected() => m_pProperty is { } && m_tpSource is { };
	//	_Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//	_Check_return_ HRESULT GetSource(_Outptr_ IInspectable **ppSource) override;
	public override Type GetSourceType() => m_pSourceType;
	//	_Check_return_ HRESULT TryReconnect(_In_ IInspectable* pSource, _In_ BOOLEAN fListenToChanges, _Inout_ BOOLEAN& bConnected, _Inout_ const Type*& pResolvedType) override;
	//	_Check_return_ HRESULT DisconnectEventHandlers() override;

	//public:

	//	static _Check_return_ HRESULT CreateInstance(
	//		_In_ IPropertyAccessHost *pOwner, 
	//		_In_ IInspectable *pSource, 
	//		_In_ const Type* pSourceType,
	//		_In_ bool fListenToChanges,
	//		_Outptr_ PropertyAccess **ppPropertyAccess);

	//	static _Check_return_ HRESULT CreateInstance(
	//		_In_ IPropertyAccessHost *pOwner, 
	//		_In_ IInspectable *pSource, 
	//		_In_ const Type *pSourceType,
	//		_In_ const CDependencyProperty* pDP,
	//		_In_ bool fListenToChanges,
	//		_Outptr_ PropertyAccess **ppPropertyAccess);

	//protected:

	//	void Initialize(
	//		_In_ IPropertyAccessHost* const pOwner,
	//		_In_ IInspectable* const pSource,
	//		_In_ const Type* const pSourceType,
	//		_In_ const CDependencyProperty* const pProperty);
	//	using PropertyAccess::Initialize; // Bring in the base function as well

	//private:

	//	_Check_return_ HRESULT ConnectToSourceProperty();

	//	_Check_return_ HRESULT AddPropertyChangedHandler();

	//	// This method can be called from the destructor to 
	//	// remove the event handlers safely
	//	_Check_return_ HRESULT SafeRemovePropertyChangedHandler();

	//	_Check_return_ HRESULT SourceDPChanged();

	//	static _Check_return_ HRESULT GetSource(
	//		_In_ IInspectable *pSource, 
	//		_Outptr_ DependencyObject **ppSource);

	//	_Check_return_ HRESULT GetSource(_Outptr_ DependencyObject **ppSource);

	//	// This method is safe to be called from the destructor path
	//	_Check_return_ HRESULT SafeGetSource(_Outptr_ DependencyObject **ppSource);

	//	static _Check_return_ HRESULT ResolvePropertyName(
	//		_In_ DependencyObject *pSource, 
	//		_In_ const Type *pSourceType,
	//		_In_z_ WCHAR *szPropertyName, 
	//		_Outptr_ const CDependencyProperty** ppDP);

	//	_Check_return_ HRESULT PropertyAccessPathStepDPChanged(_In_ const CDependencyProperty* pDP);


	//private:
	private IPropertyAccessHost m_pOwner;
	private Type m_pSourceType;

	private DependencyProperty m_pProperty;
	private object m_tpSource;

	private IDisposable m_epSyncHandler;
	//	ctl::EventPtr<PropertyAccessPathStepDPChangedCallback> m_epSyncHandler;
}
partial class DependencyObjectPropertyAccess // src\dxaml\xcp\dxaml\lib\DependencyObjectPropertyAccess.cpp
{
	public DependencyObjectPropertyAccess()
	{
		m_pOwner = null;
		m_pSourceType = null;
	}

	//~DependencyObjectPropertyAccess()
	//{
	//	try
	//	{
	//		SafeRemovePropertyChangedHandler();
	//	}
	//	catch (Exception)
	//	{
	//	}
	//}

	protected void Initialize(
		IPropertyAccessHost pOwner,
		object pSource,
		Type pSourceType,
		DependencyProperty pProperty)
	{
		m_pProperty = pProperty;
		m_tpSource = pSource;
		m_pSourceType = pSourceType;
		m_pOwner = pOwner;
	}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		object pInspSource,
		[DynamicallyAccessedMembers(MetadataAPI.TryGetDependencyPropertyByName_Type_Requirements)]
		Type pSourceType,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		DependencyObject spSource;
		DependencyProperty pProperty = null;

		// By default there's no property access
		ppPropertyAccess = null;

		// First resolve the property
		GetSource(pInspSource, out spSource);
		if (spSource != null)
		{
			pProperty = ResolvePropertyName(spSource, pSourceType, pOwner.GetPropertyName());
		}

		// If we didn't find the property then we're done
		if (pProperty is null)
		{
			return ppPropertyAccess;
		}

		ppPropertyAccess = CreateInstance(pOwner, pInspSource, pSourceType, pProperty, fListenToChanges);

		return ppPropertyAccess;
	}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		object pInspSource,
		Type pSourceType,
		DependencyProperty pDP,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		//UNREFERENCED_PARAMETER(pSourceType);

		DependencyObjectPropertyAccess spResult;

		// By default there's no property access
		ppPropertyAccess = null;

		spResult = new DependencyObjectPropertyAccess();
		spResult.Initialize(pOwner, pInspSource, pSourceType, pDP);

		if (fListenToChanges)
		{
			spResult.AddPropertyChangedHandler();
		}

		ppPropertyAccess = spResult;

		return ppPropertyAccess;
	}

	private void AddPropertyChangedHandler()
	{
		DependencyObject spSource;

		GetSource(out spSource);

		if (spSource is null)
		{
			return;
		}

		// uno: subscribe only to m_pProperty, instead of any dp changed
		m_epSyncHandler = spSource.RegisterDisposablePropertyChangedCallback(m_pProperty, (sender, args) =>
		{
			PropertyAccessPathStepDPChanged(args.Property);
		});
		//m_epSyncHandler.AttachEventHandler(spSource.Get(),
		//[this](_In_ xaml::IDependencyObject *sender, _In_ const CDependencyProperty* pDP)
		//{
		//	return this->PropertyAccessPathStepDPChanged(pDP);
		//}));

		// Only FrameworkElement(s) have the ability of raising
		// core property events
	}

	private void PropertyAccessPathStepDPChanged(DependencyProperty pDP)
	{
		if (pDP.UniqueId == m_pProperty.UniqueId)
		{
			SourceDPChanged();
		}
	}

	[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "...")]
	private void SafeRemovePropertyChangedHandler()
	{
		if (m_epSyncHandler is { })
		{
			DependencyObject spSource;
			spSource = SafeGetSource();

			if (spSource is { })
			{
				m_epSyncHandler.Dispose();
			}
		}
	}

	public override object GetValue()
	{
		object ppValue;
		DependencyObject spSource;

		ppValue = null;
		GetSource(out spSource);

		if (spSource is { } && IsConnected())
		{
			//if (!m_pProperty.ShouldBindingGetValueUseCheckOnDemandProperty() ||
			//	!spSource.GetHandle().CheckOnDemandProperty(m_pProperty).IsNull())
			{
				ppValue = spSource.GetValue(m_pProperty);
			}
		}

		return ppValue;
	}

	public override void SetValue(object pValue)
	{
		DependencyObject spSource;

		GetSource(out spSource);

		if (spSource is null)
		{
			return;
		}

		if (IsConnected() is false) throw new InvalidOperationException();

		spSource.SetValue(m_pProperty, pValue);
	}

	public override Type GetType2()
	{
		if (IsConnected() is false) throw new InvalidOperationException();
		return m_pProperty.Type;
	}

	public override void SetSource(object pSource, bool fListenToChanges)
	{
		DisconnectEventHandlers();
		m_tpSource = pSource;

		if (fListenToChanges)
		{
			AddPropertyChangedHandler();
		}
	}

	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		bool bConnected;
		bConnected = false;

		//pResolvedType = MetadataAPI.GetClassInfoFromObject_SkipWinRTPropertyOtherType(pSource);
		pResolvedType = pSource?.GetType();

		if (m_pSourceType == pResolvedType)
		{
			SetSource(pSource, fListenToChanges);
			bConnected = true;
		}

		return bConnected;
	}

	public override object GetSource()
	{
		object ppSource;

		if (IsConnected() is false) throw new InvalidOperationException();

		ppSource = m_tpSource;
		//AddRefInterface(ppSource);

		return ppSource;
	}

	public override void DisconnectEventHandlers()
	{
		DependencyObject spSource;

		if (m_tpSource is { })
		{
			GetSource(out spSource);

			if (m_epSyncHandler is { })
			{
				m_epSyncHandler.Dispose();
			}
		}
	}

	private void SourceDPChanged()
	{
		m_pOwner.SourceChanged();
	}

	private static void GetSource(object pSource, out DependencyObject ppSource)
	{
		ppSource = pSource as DependencyObject;
	}

	// uno: using `out` syntax to avoid conflict with: object PropertyAccess::GetSource()
	private void GetSource(out DependencyObject ppSource)
	{
		GetSource(m_tpSource, out ppSource);
	}

	private DependencyObject SafeGetSource()
	{
		DependencyObject ppSource;
		var spSource = m_tpSource;

		ppSource = null;

		if (spSource is { })
		{
			GetSource(spSource, out ppSource);
		}

		return ppSource;
	}

	private static DependencyProperty ResolvePropertyName(
	DependencyObject pSource,
	[DynamicallyAccessedMembers(MetadataAPI.TryGetDependencyPropertyByName_Type_Requirements)]
	Type pSourceType,
	string szPropertyName)
	{
		DependencyProperty ppDP;

		ppDP = MetadataAPI.TryGetDependencyPropertyByName(pSourceType, szPropertyName);

		return ppDP;
	}
}

internal partial class PropertyProviderPropertyAccess : // src\dxaml\xcp\dxaml\lib\PropertyProviderPropertyAccess.h
	PropertyAccess,
	INPCListenerBase
{
	//public:
	//    ~PropertyProviderPropertyAccess() override;

	//    // IPropertyAccess
	//    _Check_return_ HRESULT GetValue(_COM_Outptr_result_maybenull_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_ IInspectable *pValue) override;
	public override Type GetType2() => m_pPropertyType;
	public override bool IsConnected() => m_tpProperty is { } && m_tpSource is { };

	//    _Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//    _Check_return_ HRESULT GetSource(_COM_Outptr_result_maybenull_ IInspectable **ppSource) override;
	//    _Check_return_ HRESULT GetSourceType(_Outptr_ const CClassInfo **ppType) override;
	//    _Check_return_ HRESULT TryReconnect(_In_ IInspectable* pSource, _In_ BOOLEAN fListenToChanges, _Inout_ BOOLEAN& bConnected, _Inout_ const CClassInfo*& pResolvedType) override;
	//    _Check_return_ HRESULT DisconnectEventHandlers() override;

	//protected:

	//    using PropertyAccess::Initialize;

	//public:

	//    static _Check_return_ HRESULT CreateInstance(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ xaml_data::ICustomPropertyProvider *pSource,
	//        _In_ bool fListenToChanges,
	//        _Outptr_ PropertyAccess **ppPropertyAccess);

	//private:

	//    void Initialize(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ xaml_data::ICustomPropertyProvider *pSource,
	//        _In_ xaml_data::ICustomProperty *pProperty,
	//        _In_ const CClassInfo *pPropertyType);

	//private:

	//    _Check_return_ HRESULT OnPropertyChanged() override;
	//    _Ret_notnull_ const wchar_t* GetPropertyName() override;

	//private:
	private IPropertyAccessHost m_pOwnerNoRef;
	private Type m_sourceType;
	private ICustomProperty m_tpProperty;
	private ICustomPropertyProvider m_tpSource;
	private Type m_pPropertyType;
}
partial class PropertyProviderPropertyAccess // src\dxaml\xcp\dxaml\lib\PropertyProviderPropertyAccess.cpp
{
	private void Initialize(
		IPropertyAccessHost pOwner,
		ICustomPropertyProvider pSource,
		ICustomProperty pProperty,
		Type pPropertyType)
	{
		m_pOwnerNoRef = pOwner;

		m_tpSource = pSource;
		m_tpProperty = pProperty;

		m_pPropertyType = pPropertyType;
	}

	//~PropertyProviderPropertyAccess()
	//{
	//	var spSource = m_tpSource;
	//	if (spSource is { })
	//	{
	//		DisconnectPropertyChangedHandler(spSource);
	//	}
	//}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		ICustomPropertyProvider pSource,
		bool fListenToChanges)
	{
		// By default there's no property access
		PropertyAccess ppPropertyAccess = null;

		// First try to resolve the property
		ICustomProperty spProperty;
		spProperty = pSource.GetCustomProperty(pOwner.GetPropertyName());

		// If no property with that name exists then we're done
		if (spProperty is null)
		{
			return null;
		}

#if !HAS_UNO
		TypeName typeName;
		typeName = spProperty.Type;

		Type pPropertyType = null;
		{
			var hr = MetadataAPI.GetClassInfoByTypeName(typeName, &pPropertyType);

			// We really want to fail, but unfortunately because we didn't in Windows 8.1, we can't do so now either.
			// TODO: [Add quirk.] TFS#677191: Quirk: GetClassInfoFromObject and PropertyProviderPropertyAccess should not swallow errors from GetClassInfoByTypeName anymore in Threshold and beyond.
			//ASSERTSUCCEEDED(hr);
			//if (FAILED(hr))
			if (pPropertyType is null)
			{
				// Fall back on Object for compatibility with Windows 8.1.
				//pPropertyType = MetadataAPI.GetClassInfoByIndex(KnownTypeIndex.Object);
				pPropertyType = typeof(object);
			}
		}
#else
		Type pPropertyType = spProperty.Type ?? typeof(object);
#endif

		// Translate to a primitive WinRT type (int, string, ..., object).
		pPropertyType = MetadataAPI.GetPrimitiveClassInfo(pPropertyType);

		PropertyProviderPropertyAccess spResult;
		spResult = new PropertyProviderPropertyAccess();
		spResult.Initialize(
			pOwner,
			pSource,
			spProperty,
			pPropertyType);

		if (fListenToChanges)
		{
			spResult.AddPropertyChangedHandler(pSource);
		}

		ppPropertyAccess = spResult;

		return ppPropertyAccess;
	}

	public override object GetValue()
	{
		if (IsConnected())
		{
			return m_tpProperty.GetValue(m_tpSource);
		}
		else
		{
			return null;
		}
	}

	public override void SetValue(object pValue)
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		m_tpProperty.SetValue(m_tpSource, pValue);
	}

	public override void SetSource(object pSource, bool fListenToChanges)
	{
		// If we're clearing out the source, it's possible we by-pass the code in TryReconnect. Make sure we
		// get the source's type while we still can, so we have an opportunity in the future to re-use this path.
		if (m_sourceType is null && m_tpSource is { })
		{
			Type sourceType;
			if ((sourceType = m_tpSource.Type) is { })
			{
				m_sourceType = sourceType;
			}
		}

		if (fListenToChanges)
		{
			// Disconnect the handler on the previous source, and attach to the new source. Both
			// old and new source are allowed to be null.
			UpdatePropertyChangedHandler(m_tpSource, pSource);
		}
		else
		{
			DisconnectEventHandlers();
		}
		m_tpSource = (ICustomPropertyProvider)pSource;
	}

	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		bool bConnected = false;
		pResolvedType = default;

		var spCPP = pSource as ICustomPropertyProvider;
		if (spCPP is { })
		{
			// Make sure we've resolved the type of the previous source.
			if (m_sourceType is null && m_tpSource is { })
			{
				Type sourceType;
				if ((sourceType = m_tpSource.Type) is { })
				{
					m_sourceType = sourceType;
				}
			}

			Type newSourceType;
			if (/*m_sourceType.Name &&*/ (newSourceType = spCPP.Type) is { } /*&& m_sourceType.Kind == newSourceType.Kind*/)
			{
				//int nResult = 0;
				//WindowsCompareStringOrdinal(m_sourceType.Name, newSourceType.Name, &nResult);
				//if (nResult == 0)
				if (m_sourceType == newSourceType)
				{
					SetSource(pSource, fListenToChanges);
					bConnected = true;
				}
			}
		}

		return bConnected;
	}

	public override object GetSource()
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		return m_tpSource;
	}

	public override void DisconnectEventHandlers()
	{
		if (IsConnected())
		{
			DisconnectPropertyChangedHandler(m_tpSource);
		}
	}

	protected partial void OnPropertyChanged()
	{
		m_pOwnerNoRef.SourceChanged();
	}

	protected partial string GetPropertyName()
	{
		return m_pOwnerNoRef.GetPropertyName();
	}

	public override Type GetSourceType()
	{
		//return MetadataAPI.GetClassInfoFromObject_SkipWinRTPropertyOtherType(m_tpSource);
		return m_tpSource.GetType();
	}
}
partial class PropertyProviderPropertyAccess // src\dxaml\xcp\dxaml\lib\INPCListenerBase.h
{
	//protected:
	protected partial void AddPropertyChangedHandler(object pSource);
	protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource);
	protected partial void DisconnectPropertyChangedHandler(object pSource);

	protected partial void OnPropertyChanged();
	protected partial string GetPropertyName();

	//private:
	private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs);

	//private:
	private IDisposable m_epPropertyChangedHandler;
	private int m_propertyNameLength;
}
partial class PropertyProviderPropertyAccess // src\dxaml\xcp\dxaml\lib\INPCListenerBase.cpp
{
	protected partial void AddPropertyChangedHandler(object pSource)
	{
		if (m_epPropertyChangedHandler is not null) throw new InvalidOperationException();

		UpdatePropertyChangedHandler(null, pSource);
	}

	protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource)
	{
		INotifyPropertyChanged spINPC;

		string buffer = this.GetPropertyName();
		if (string.IsNullOrEmpty(buffer)) throw new InvalidOperationException();
		m_propertyNameLength = buffer.Length;

		if (pOldSource is { })
		{
			DisconnectPropertyChangedHandler(pOldSource);
		}

		if (pNewSource is { })
		{
			if ((spINPC = pNewSource as INotifyPropertyChanged) is { })
			{
				spINPC.PropertyChanged += OnPropertyChanged;
				m_epPropertyChangedHandler = Disposable.Create(() =>
					spINPC.PropertyChanged -= OnPropertyChanged
				);
				void OnPropertyChanged(object sender, PropertyChangedEventArgs args) => OnPropertyChangedCallback(args);
			}
		}
	}
	protected partial void DisconnectPropertyChangedHandler(object pSource)
	{
		if (m_epPropertyChangedHandler is { })
		{
			m_epPropertyChangedHandler.Dispose();
		}
	}

	private partial void OnPropertyChangedCallback(PropertyChangedEventArgs pArgs)
	{
		bool propertyChanged = false;
		string strProperty;

		strProperty = pArgs.PropertyName;

		// If it is not then we need to compare the string vs. the string in the listener to see if the
		// change affects the listener
		if (strProperty != null)
		{
			int length = 0;
			string buffer = this.GetPropertyName();
			(string current, length) = (strProperty, strProperty.Length);
			propertyChanged = (length == m_propertyNameLength) && (current == buffer);
		}
		else
		{
			// If the property name is null, which means empty, then we want the change
			propertyChanged = true;
		}

		// Notify the class if the property we're listening to changed
		if (propertyChanged)
		{
			OnPropertyChanged();
		}
	}
}

internal partial class IndexerPropertyAccess : // src\dxaml\xcp\dxaml\lib\IndexerPropertyAccess.h
	PropertyAccess
{
	//private:

	//    void Initialize(
	//        _In_ IIndexedPropertyAccessHost *pOwner,
	//        _In_ xaml_data::ICustomPropertyProvider *pSource,
	//        _In_ IInspectable *pIndex, 
	//        _In_ xaml_data::ICustomProperty *pProperty,
	//        _In_ const CClassInfo *pPropertyType);

	//protected:

	//    using PropertyAccess::Initialize;   // Bring in the base class Initialize

	//public:

	public IndexerPropertyAccess()
	{
		m_pOwner = null;
		m_pPropertyType = null;
	}
	//    ~IndexerPropertyAccess() override;

	//    // IPropertyAccess
	//    _Check_return_ HRESULT GetValue(_COM_Outptr_result_maybenull_ IInspectable **ppValue) override;
	//    _Check_return_ HRESULT SetValue(_In_ IInspectable *pValue) override;
	public override Type GetType2()
	{
		return m_pPropertyType;
	}
	public override bool IsConnected()
	{
		return m_tpIndexer is { } && m_tpSource is { };
	}
	//    _Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//    _Check_return_ HRESULT GetSource(_Outptr_ IInspectable **ppSource) override;
	//    _Check_return_ HRESULT GetSourceType(_Outptr_ const CClassInfo **ppType) override;
	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		pResolvedType = null;
		return false;
	}
	//    _Check_return_ HRESULT DisconnectEventHandlers() override;

	//public:

	//    static _Check_return_ HRESULT CreateInstance(
	//        _In_ IIndexedPropertyAccessHost *pOwner,
	//        _In_ xaml_data::ICustomPropertyProvider *pSource,
	//        _In_ wxaml_interop::TypeName sTypeName,
	//        _In_ IInspectable *pIndex,
	//        _In_ bool fListenToChanges,
	//        _Outptr_ PropertyAccess **ppPropertyAccess);

	//private:

	//    _Check_return_ HRESULT AddPropertyChangedHandler();

	//    // This method is safe to be called from the destructor
	//    _Check_return_ HRESULT SafeRemovePropertyChangedHandler();

	//    _Check_return_ HRESULT PropertyChanged();

	//    // This function is used to workarround a CLR bug
	//    // that requires the index to be a different value evevery time
	//    // This will be removed later, once the bug is fixed
	//    static _Check_return_ HRESULT DuplicatePropertyValue(_In_ IInspectable *pValue, _Outptr_ IInspectable **ppDupe);

	//private:
	private void OnPropertyChanged(PropertyChangedEventArgs pArgs)
	{
		string strProperty;
		string szProperty = null;
		string szIndexerName = null;
		bool fChanged = false;

		strProperty = pArgs.PropertyName;

		szProperty = strProperty;

		// Determine if the indexer has changed
		if (string.Compare(szProperty, "Item[]", StringComparison.InvariantCulture) != 0)
		{
			szIndexerName = m_pOwner.GetIndexedPropertyName();
			fChanged = string.Compare(szProperty, szIndexerName, StringComparison.InvariantCulture) != 0;
		}
		else
		{
			fChanged = true;
		}

		// Notify of the change
		if (fChanged)
		{
			PropertyChanged();
		}
	}

	//private:
	private IIndexedPropertyAccessHost m_pOwner;
	private ICustomProperty m_tpIndexer;
	private ICustomPropertyProvider m_tpSource;
	private object m_tpIndex;
	//ctl::EventPtr<PropertyChangedEventCallback> m_epPropertyChangedHandler;
	private IDisposable m_epPropertyChangedHandler;
	private Type m_pPropertyType;
}
partial class IndexerPropertyAccess // src\dxaml\xcp\dxaml\lib\IndexerPropertyAccess.cpp
{
	public static PropertyAccess CreateInstance(
		IIndexedPropertyAccessHost pOwner,
		ICustomPropertyProvider pSource,
		Type sTypeName,
		object pIndex,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		ICustomProperty spIndexer;
		Type pIndexerType = null;
		Type sIndexerType = default;
		IndexerPropertyAccess spResult;

		// By default no property access is created.
		ppPropertyAccess = null;

		// Resolve the indexer by the type of the parameter.
		spIndexer = pSource.GetIndexedProperty("Item", sTypeName);
		if (spIndexer is null)
		{
			// No indexer found we're done
			return ppPropertyAccess;
		}

		// Get the type of the property.
		sIndexerType = spIndexer.Type;
		pIndexerType = MetadataAPI.GetClassInfoByTypeName(sIndexerType);

		// Finally create the property access
		spResult = new IndexerPropertyAccess();
		spResult.Initialize(
			pOwner,
			pSource,
			pIndex,
			spIndexer,
			pIndexerType);

		if (fListenToChanges)
		{
			spResult.AddPropertyChangedHandler();
		}

		// And return the newly constructed property access
		ppPropertyAccess = spResult;

		return ppPropertyAccess;
	}

	private void Initialize(
		IIndexedPropertyAccessHost pOwner,
		ICustomPropertyProvider pSource,
		object pIndex,
		ICustomProperty pProperty,
		Type pPropertyType)
	{
		m_pOwner = pOwner;

		m_tpIndexer = pProperty;
		m_tpSource = pSource;
		m_tpIndex = pIndex;

		m_pPropertyType = pPropertyType;
	}

	//~IndexerPropertyAccess()
	//{
	//	SafeRemovePropertyChangedHandler();
	//}

	public override object GetValue()
	{
		object spIndex;

		// Workarround for CLR bug, we need to send a new object
		// everytime or the CLR will fail to do the unboxing on the other side
		spIndex = DuplicatePropertyValue(m_tpIndex);

		return m_tpIndexer.GetIndexedValue(m_tpSource, spIndex);
	}

	public override void SetValue(object pValue)
	{
		object spIndex;

		// Workarround for CLR bug, we need to send a new object
		// everytime or the CLR will fail to do the unboxing on the other side
		spIndex = DuplicatePropertyValue(m_tpIndex);

		m_tpIndexer.SetIndexedValue(m_tpSource, pValue, spIndex);
	}

	public override void SetSource(object pSource, bool fListenToChanges)
	{
		DisconnectEventHandlers();
		m_tpSource = (ICustomPropertyProvider)pSource;

		if (fListenToChanges)
		{
			AddPropertyChangedHandler();
		}
	}

	public override object GetSource()
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		return m_tpSource;
		//AddRefInterface(ppSource);
	}

	public override void DisconnectEventHandlers()
	{
		if (m_epPropertyChangedHandler is { } && m_tpSource is { })
		{
			m_epPropertyChangedHandler.Dispose();
		}
	}

	private void AddPropertyChangedHandler()
	{
		INotifyPropertyChanged spINPC;

		spINPC = m_tpSource as INotifyPropertyChanged;
		if (spINPC is { })
		{
			spINPC.PropertyChanged += OnPropertyChanged;
			m_epPropertyChangedHandler = Disposable.Create(() =>
				spINPC.PropertyChanged -= OnPropertyChanged
			);

			void OnPropertyChanged(object pSource, PropertyChangedEventArgs pArgs)
			{
				this.OnPropertyChanged(pArgs);
			}
		}
	}

	[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "...")]
	private void SafeRemovePropertyChangedHandler()
	{
		if (m_epPropertyChangedHandler is { })
		{
			var spSource = m_tpSource;
			if (spSource is { })
			{
				m_epPropertyChangedHandler.Dispose();
			}
		}
	}

	private void PropertyChanged()
	{
		m_pOwner.SourceChanged();
	}

	/// <summary>
	/// This function is used to workaround a CLR bug
	/// that requires the index to be a different value every time
	/// This will be removed later, once the bug is fixed
	/// </summary>
	/// <remarks>UNO: probably not necessary</remarks>
	private static object DuplicatePropertyValue(object pValue)
	{
		return pValue;

		//ctl::ComPtr<wf::IPropertyValue> spPV;
		//wf::PropertyType propertyType;

		//spPV = ctl::query_interface_cast<wf::IPropertyValue>(pValue);
		//if (!spPV)
		//{
		//	*ppDupe = pValue;
		//	ctl::addref_interface(pValue);
		//	goto Cleanup;
		//}

		//IFC(spPV->get_Type(&propertyType));

		//switch (propertyType)
		//{
		//	case wf::PropertyType_String:
		//		{
		//			wrl_wrappers::HString strValue;
		//			IFC(spPV->GetString(strValue.GetAddressOf()));
		//			IFC(PropertyValue::CreateFromString(strValue.Get(), ppDupe));
		//		}
		//		break;

		//	case wf::PropertyType_Int32:
		//		{
		//			INT32 iValue = 0;
		//			IFC(spPV->GetInt32(&iValue));
		//			IFC(PropertyValue::CreateFromInt32(iValue, ppDupe));
		//		}
		//		break;

		//	default:

		//		// We should not get here since the only supported
		//		// indexes are int and string
		//		IFC(E_UNEXPECTED);
		//		break;
		//}
	}

	public override Type GetSourceType()
	{
		if (IsConnected() is false) throw new InvalidOperationException();

		//return MetadataAPI.GetClassInfoFromObject_SkipWinRTPropertyOtherType(m_tpSource);
		return m_tpSource.Type ?? m_tpSource.GetType();
	}
}

#if HAS_UNO
internal class ReflectionPropertyAccess : PropertyAccess
{
	private IPropertyAccessHost _owner;
	private object _source;
	private Type _sourceType;
	private CustomProperty _property;

	public ReflectionPropertyAccess(IPropertyAccessHost owner, object source, Type sourceType, CustomProperty property)
	{
		_owner = owner;
		_source = source;
		_sourceType = sourceType;
		_property = property;
	}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		object pSource,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		Type pSourceType,
		bool fListenToChanges)
	{
		if (pSource is null) throw new InvalidOperationException();

		var propertyName = pOwner.GetPropertyName();
		var property = pSourceType.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
		if (property?.CanRead != true)
		{
			return null;
		}

		var cp = CustomProperty.FromPropertyInfo(property);
		var result = new ReflectionPropertyAccess(pOwner, pSource, pSourceType, cp);
		if (fListenToChanges)
		{
			throw new NotSupportedException();
		}

		return result;
	}

	public override bool IsConnected() => _source is { } && _property is { } && _property.IsValid();
	public override object GetSource() => _source;
	public override Type GetSourceType() => _sourceType;
	public override Type GetType2() => _property.Type;
	public override object GetValue() => _property.GetValue(_source);
	public override void SetSource(object pSource, bool fListenToChanges)
	{
		if (fListenToChanges)
		{
			throw new NotSupportedException();
		}

		_source = pSource;
	}
	public override void SetValue(object pValue) => _property.SetValue(_source, pValue);
	public override bool TryReconnect(object pSource, bool fListenToChanges, out Type pResolvedType)
	{
		pResolvedType = pSource.GetType();

		if (_sourceType == pResolvedType)
		{
			SetSource(pSource, fListenToChanges);
			return true;
		}
		else
		{
			return false;
		}
	}
	public override void DisconnectEventHandlers() { }
}
#endif
