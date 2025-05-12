using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Windows.Media.Import;
using CClassInfo = System.Type; // fixme@xy: is that the equivalent or suitable here

namespace DirectUI;

//  public abstract:
//      Defines the interfaces for accessing a single property
//      public abstracts away the differences between the types of properties
//      supported.

internal abstract class PropertyAccess // : public ctl::WeakReferenceSource
{
	//public:
	public abstract object GetValue();
	public abstract void SetValue(object pValue);
	public abstract CClassInfo GetType2();
	public abstract object GetSource();
	public abstract void SetSource(object pSource, bool fListenToChanges);
	public abstract CClassInfo GetSourceType();
	public abstract void DisconnectEventHandlers();
	public abstract bool TryReconnect(object pSource, bool fListenToChanges, out CClassInfo pResolvedType);
	public abstract bool IsConnected();
}

internal interface IPropertyAccessHost // fixme@xy: we should implement explicitly
{
	void SourceChanged();
	string GetPropertyName();
}
internal interface IIndexedPropertyAccessHost : IPropertyAccessHost // fixme@xy: we should implement explicitly
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
	//        _In_ const CClassInfo *pSourceType,
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
	//    _Check_return_ HRESULT GetType(_Outptr_ const CClassInfo **ppType) override;
	//    bool IsConnected() override;
	//    _Check_return_ HRESULT SetSource(_In_opt_ IInspectable *pSource, _In_ BOOLEAN fListenToChanges) override;
	//    _Check_return_ HRESULT GetSource(_Outptr_ IInspectable** ppSource) override;
	public override CClassInfo GetSourceType() => m_pSourceType;
	//    _Check_return_ HRESULT TryReconnect(_In_ IInspectable* pSource, _In_ BOOLEAN fListenToChanges, _Inout_ BOOLEAN& bConnected, _Inout_ const CClassInfo*& pResolvedType) override;
	//    _Check_return_ HRESULT DisconnectEventHandlers() override;

	//public:
	//    static _Check_return_ HRESULT CreateInstance(
	//        _In_ IPropertyAccessHost *pOwner,
	//        _In_ IInspectable *pSource, 
	//        _In_ const CClassInfo *pSourceType,
	//        _In_ bool fListenToChanges,
	//        _Outptr_ PropertyAccess **ppPropertyAccess);

	//private:
	private IPropertyAccessHost m_pOwner;
	private CClassInfo m_pSourceType;
	private object m_tpSource;
	private CCustomProperty m_pProperty;
}
partial class PropertyInfoPropertyAccess // src\dxaml\xcp\dxaml\lib\PropertyInfoPropertyAccess.cpp
{
	public void Initialize(
		IPropertyAccessHost pOwner,
		object pSource,
		CClassInfo pSourceType,
		CCustomProperty pProperty)
	{
		m_pOwner = pOwner;

		m_tpSource = pSource;
		m_pProperty = pProperty;
		m_pSourceType = pSourceType;
	}

	~PropertyInfoPropertyAccess()
	{
		var spSource = m_tpSource;
		if (spSource is { })
		{
			DisconnectPropertyChangedHandler(spSource);
		}
	}

	public override object GetValue()
	{
		object ppValue;

		if (IsConnected())
		{
			ppValue = (m_pProperty as CCustomProperty).GetXamlPropertyNoRef().GetValue(m_tpSource);
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

		(m_pProperty as CCustomProperty).GetXamlPropertyNoRef().SetValue(m_tpSource, pValue);
	}

	public override CClassInfo GetType2()
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

	public override bool TryReconnect(object pSource, bool fListenToChanges, out CClassInfo pResolvedType)
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

	public /*override*/ void /*INPCListenerBase::*/OnPropertyChanged()
	{
		m_pOwner.SourceChanged();
	}

	public /*override*/ string /*INPCListenerBase::*/GetPropertyName()
	{
		return m_pOwner.GetPropertyName();
	}

	public static PropertyAccess CreateInstance(
		IPropertyAccessHost pOwner,
		object pSource,
		CClassInfo pSourceType,
		bool fListenToChanges)
	{
		PropertyAccess ppPropertyAccess;
		//PropertyInfoPropertyAccess spResult;
		//DependencyProperty pProperty = null;
		string pszPropertyName = pOwner.GetPropertyName();
		if (pSource is null) throw new InvalidOperationException();

		ppPropertyAccess = null; // By default no property is generated

		// BindableTypeProvidersGenerationTask? is not available

		// We only care about types that are in the metadata because they are explicitly bindable
		// this will remove things like DPs as we need those to be resolved in a specialized form.
		//if (!pSourceType.IsBindable())
		//{
		//	goto Cleanup;
		//}

		//// Now try to resolve the property by name.
		//pProperty = MetadataAPI.TryGetPropertyByName(pSourceType, pszPropertyName);
		//// ___^ are we actually reflecting on the generated bindable code, or actually fetching the dp from the sourceType?
		//// ____ it cant be the latter, since pSourceType cant guarantee this
		//if (pProperty == null)
		//{
		//	// We failed to find the property, bail out.
		//	return ppPropertyAccess;
		//}

		//// Create a property access object.
		//spResult = new();
		//spResult.Initialize(pOwner, pSource, pSourceType, pProperty as CCustomProperty);

		//if (fListenToChanges)
		//{
		//	// If we're listenting for changes need to keep the name of the property
		//	// to compare it against the name of the properties that are changing
		//	spResult.AddPropertyChangedHandler(pSource);
		//}

		//ppPropertyAccess = spResult;
		return ppPropertyAccess;
	}
}
partial class PropertyInfoPropertyAccess // src\dxaml\xcp\dxaml\lib\INPCListenerBase.cpp
{
	//protected:
	protected partial void AddPropertyChangedHandler(object pSource);
	protected partial void UpdatePropertyChangedHandler(object/*?*/ pOldSource, object/*?*/ pNewSource);
	protected partial void DisconnectPropertyChangedHandler(object pSource);

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

internal partial class MapPropertyAccess
{
}

#if HAS_UNO
internal partial class ReflectionPropertyAccess
{
}
#endif
