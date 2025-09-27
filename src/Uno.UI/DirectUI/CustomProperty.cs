using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;

namespace DirectUI;

/// <summary>
/// A default implementation of ICustomProperty, which can be used to describe internal
/// properties on non-DOs.
/// </summary>
internal abstract partial class CustomProperty : // src\dxaml\xcp\dxaml\lib\CustomProperty.h
	ICustomProperty
{
	//    typedef std::function<HRESULT (IInspectable*, IInspectable**)> GetValueFunction;
	public delegate object GetValueFunction(object x);

	//    BEGIN_INTERFACE_MAP(CustomProperty, ctl::WeakReferenceSource)
	//        INTERFACE_ENTRY(CustomProperty, xaml_data::ICustomProperty)
	//    END_INTERFACE_MAP(CustomProperty, ctl::WeakReferenceSource)

	//public:

	//    static _Check_return_ HRESULT CreateObjectProperty(
	//        _In_ HSTRING hName,
	//        _In_ GetValueFunction getValueFunc,
	//        _Outptr_ xaml_data::ICustomProperty** ppProperty);

	//    static _Check_return_ HRESULT CreateInt32Property(
	//        _In_ HSTRING hName,
	//        _In_ GetValueFunction getValueFunc,
	//        _Outptr_ xaml_data::ICustomProperty** ppProperty);

	//    static _Check_return_ HRESULT CreateBooleanProperty(
	//        _In_ HSTRING hName,
	//        _In_ GetValueFunction getValueFunc,
	//        _Outptr_ xaml_data::ICustomProperty** ppProperty);

	public string Name => m_hName;

	public bool CanRead => true;

	public bool CanWrite => false;

	public abstract Type Type { get; }

	public object GetValue(object pTarget) => m_funcGetValue(pTarget);

	public void SetValue(object target, object value) =>
		// RRETURN(E_NOTIMPL);
		throw new NotImplementedException();

	public object GetIndexedValue(object target, object index) =>
		// RRETURN(E_NOTIMPL);
		throw new NotImplementedException();
	public void SetIndexedValue(object target, object value, object index) =>
		// RRETURN(E_NOTIMPL);
		throw new NotImplementedException();

	//protected:
	//    HRESULT QueryInterfaceImpl(_In_ REFIID iid, _Outptr_ void** ppObject) override
	//    {
	//        if (InlineIsEqualGUID(iid, __uuidof(xaml_data::ICustomProperty)))
	//        {
	//            *ppObject = static_cast<xaml_data::ICustomProperty*>(this);
	//        }
	//        else
	//        {
	//            RRETURN(ctl::WeakReferenceSource::QueryInterfaceImpl(iid, ppObject));
	//        }

	//        AddRefOuter();
	//        RRETURN(S_OK);
	//    }

	//    // This class is marked novtable, so must not be instantiated directly.
	//    CustomProperty() = default;

	//private:
	private string m_hName;
	private GetValueFunction m_funcGetValue;

	partial class CustomProperty_Object : CustomProperty
	{
		public override Type Type =>
			//new TypeName("Object", TypeKind.Primitive);
			typeof(object);
	}
	partial class CustomProperty_Int32 : CustomProperty
	{
		public override Type Type =>
			//new TypeName("Int32", TypeKind.Primitive);
			typeof(Int32);
	}
	partial class CustomProperty_Boolean : CustomProperty
	{
		public override Type Type =>
			//new TypeName("Boolean", TypeKind.Primitive);
			typeof(Boolean);
	}
}

partial class CustomProperty // src\dxaml\xcp\dxaml\lib\CustomProperty.cpp
{
	public static ICustomProperty CreateObjectProperty(string hName, GetValueFunction getValueFunc)
	{
		ICustomProperty ppProperty;
		CustomProperty_Object spProperty;

		spProperty = new();
		spProperty.m_hName = hName;
		spProperty.m_funcGetValue = getValueFunc;

		ppProperty = spProperty;

		return ppProperty;
	}
	public static ICustomProperty CreateInt32Property(string hName, GetValueFunction getValueFunc)
	{
		ICustomProperty ppProperty;
		CustomProperty_Int32 spProperty;

		spProperty = new();
		spProperty.m_hName = hName;
		spProperty.m_funcGetValue = getValueFunc;

		ppProperty = spProperty;

		return ppProperty;
	}
	public static ICustomProperty CreateBooleanProperty(string hName, GetValueFunction getValueFunc)
	{
		ICustomProperty ppProperty;
		CustomProperty_Boolean spProperty;

		spProperty = new();
		spProperty.m_hName = hName;
		spProperty.m_funcGetValue = getValueFunc;

		ppProperty = spProperty;

		return ppProperty;
	}
}
#if HAS_UNO
partial class CustomProperty
{
	private Type PropertyType { get; set; }

	internal ICustomProperty GetXamlPropertyNoRef() => this;

	internal bool IsValid() => true;

	internal Type GetPropertyType() => PropertyType;

	public static CustomProperty CreateInstance(string name, Type type, GetValueFunction getter)
	{
		var result = (CustomProperty)(Type.GetTypeCode(type) switch
		{
			TypeCode.Int32 => CreateInt32Property(name, getter),
			TypeCode.Boolean => CreateBooleanProperty(name, getter),
			_ => CreateObjectProperty(name, getter),
		});
		result.PropertyType = type;

		return result;
	}
	public static CustomProperty FromBindableProperty(IBindableProperty property, string name)
	{
		if (property == null)
		{
			return null;
		}

		return CreateInstance(name, property.PropertyType, x => property.Getter(x, default));
	}
	public static CustomProperty FromPropertyInfo(PropertyInfo property)
	{
		if (property == null)
		{
			return null;
		}

		return CreateInstance(property.Name, property.PropertyType, property.GetValue);
	}
}
#endif
