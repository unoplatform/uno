using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Xaml.Markup;

namespace DirectUI;

/// <summary>
/// Metadata API. This is a process-wide API. Custom types may be resolved through IXamlMetadataProvider.
/// </summary>
internal partial class MetadataAPI
{
	//public:
	//    static constexpr KnownPropertyIndex EventPropertyIndex = (KnownPropertyIndex) - 1;

	#region Namespace related methods

	//    // Gets a namespace by its index.
	//    static const CNamespaceInfo* GetNamespaceByIndex(
	//        _In_ KnownNamespaceIndex eNamespaceIndex);

	//    // Gets a built-in namespace by its name.
	//    static const CNamespaceInfo* GetBuiltinNamespaceByName(
	//        _In_ const xstring_ptr_view& strNamespaceName);

	//    // Resolves a namespace by the specified name.
	//    static _Check_return_ HRESULT GetNamespaceByName(
	//        _In_ const xstring_ptr_view& strNamespaceName,
	//        _Outptr_ const CNamespaceInfo** ppNamespace);

	#endregion

	#region Type related methods

	//    // Gets a type by its index.
	//    static const CClassInfo* GetClassInfoByIndex(
	//        _In_ KnownTypeIndex eTypeIndex);

	//    // Gets a built-in type by its short name.
	//    static const CClassInfo* GetBuiltinClassInfoByName(
	//        _In_ const xstring_ptr_view& strTypeName);

	//    // Resolves a type by its full name.
	//    static _Check_return_ HRESULT GetClassInfoByFullName(
	//        _In_ const xstring_ptr_view& strTypeFullName,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Try to find a type by the specified namespace index and the type's short name.
	//    static _Check_return_ HRESULT TryGetClassInfoByName(
	//        _In_ KnownNamespaceIndex eNamespaceIndex,
	//        _In_ const xstring_ptr_view& strTypeName,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Resolves a type by its type name. This function may call out to user code (IXamlMetadataProvider) to resolve custom types.
	//    static _Check_return_ HRESULT GetClassInfoByTypeName(
	//        _In_ wxaml_interop::TypeName typeName,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Resolves the type of an object; will also resolve WinRT PropertyValues with Property_OtherType
	//    static _Check_return_ HRESULT GetClassInfoFromObject_ResolveWinRTPropertyOtherType(
	//        _In_opt_ IInspectable* pInstance,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Resolves the type of an object; will skip resolution WinRT PropertyValues with Property_OtherType, instead classifying them as generic WinRT Object
	//    // Historically, this is what happened when other components called "GetClassInfoFromObject" to resolve the type of an object
	//    static _Check_return_ HRESULT GetClassInfoFromObject_SkipWinRTPropertyOtherType(
	//        _In_opt_ IInspectable* pInstance,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Resolves a WinRT property type (e.g. PropertyType_Int32) to a type (e.g. KnownTypeIndex::Int32).
	//    static _Check_return_ HRESULT GetClassInfoFromWinRTPropertyType(
	//        _In_ wf::IPropertyValue* pValue,
	//        _In_ wf::PropertyType ePropertyType,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Returns the primitive WinRT type for a type. Complex types will be returned as Object.
	//    static _Check_return_ HRESULT GetPrimitiveClassInfo(
	//        _In_opt_ const CClassInfo* pType,
	//        _Outptr_ const CClassInfo** ppType);

	//    // Gets the friendly runtime class name of a type. This may not necessarily be the same as calling GetRuntimeClassName on an IInspectable,
	//    // because it takes into account the available type information. Should generally only be used for debug/tracing output.
	//    static _Check_return_ HRESULT GetFriendlyRuntimeClassName(
	//        _In_ IInspectable* instance,
	//        _Out_ xstring_ptr* runtimeClassName);

	//    // Gets the runtime class name of a type.
	//    static _Check_return_ HRESULT GetRuntimeClassName(
	//        _In_ IInspectable* instance,
	//        _Out_ xstring_ptr* runtimeClassName);

	//    // Resolves a TypeName by the specified type.
	//    static _Check_return_ HRESULT GetTypeNameByClassInfo(
	//        _In_ const CClassInfo* pType,
	//        _Out_ wxaml_interop::TypeName* pTypeName);

	//    // Resolves a TypeName by the specified full name.
	//    static _Check_return_ HRESULT GetTypeNameByFullName(
	//        _In_ const xstring_ptr_view& strTypeFullName,
	//        _Out_ wxaml_interop::TypeName* pTypeName);

	//    // If the type represents a boxed type, e.g. IReference<Boolean>, returns true.
	//    static _Check_return_ bool RepresentsBoxedType(
	//        _In_ const CClassInfo* pType);

	//    // If the type represents a boxed type, e.g. IReference<Boolean>, gets the inner type like Boolean.
	//    // Otherwise, gets null.
	//    static _Check_return_ const CClassInfo* TryGetBoxedType(
	//        _In_ const CClassInfo* pType);

	#endregion

	#region Property related methods

	//    // Gets a property base by its index.
	//    static const CPropertyBase* GetPropertyBaseByIndex(
	//        _In_ KnownPropertyIndex ePropertyIndex);

	//    // Finds built-in property by name or returns nullptr if not found.
	//    static const CPropertyBase* TryGetBuiltInPropertyBaseByName(
	//        _In_ const CClassInfo* pType,
	//        _In_ const xstring_ptr_view& strName,
	//        _In_ bool allowDirectives = false);

	//    // Gets a dependency property by its index.
	//    static const CDependencyProperty* GetDependencyPropertyByIndex(
	//        _In_ KnownPropertyIndex ePropertyIndex);

	//    // Try to find a built-in or custom dependency property on the specified type with the specified name.
	//    static _Check_return_ HRESULT TryGetDependencyPropertyByName(
	//        _In_ const CClassInfo* pType,
	//        _In_ const xstring_ptr_view& strName,
	//        _Outptr_result_maybenull_ const CDependencyProperty** ppDP,
	//        _In_ bool allowDirectives = false);

	//    // Given a fully qualified property name (including namespace, e.g. "Microsoft.UI.Xaml.Shapes.Shape.Fill" instead of "Fill"),
	//    // return the property.  Will search for a dependency property, simple property, or custom user property (in that order).
	//    static _Check_return_ HRESULT TryGetPropertyByFullName(
	//        _In_ const xstring_ptr_view& strName,
	//        _Outptr_result_maybenull_ const CPropertyBase** ppProp);

	//    // Look for a DP given its full name, it might use the ObjectWriter context if allowed to.
	//    static _Check_return_ HRESULT TryGetDependencyPropertyByFullyQualifiedName(
	//        _In_ const xstring_ptr_view& strName,
	//        _In_opt_ XamlServiceProviderContext* context,
	//        _Outptr_result_maybenull_ const CDependencyProperty** ppDP);

	//    // Gets a property by its index.
	//    static const CDependencyProperty* GetPropertyByIndex(
	//        _In_ KnownPropertyIndex ePropertyIndex);

	//    // Try to find a built-in dependency property or custom property on the specified type with the specified name.
	//    static _Check_return_ HRESULT TryGetPropertyByName(
	//        _In_ const CClassInfo* pType,
	//        _In_ const xstring_ptr_view& strName,
	//        _Outptr_result_maybenull_ const CDependencyProperty** ppProperty,
	//        _In_ bool allowDirectives = false);

	//    // Tries to resolve an attached property by its name. strName should use the format ClassName.PropertyName. This should only
	//    // be called for built-in attached properties.
	//    static _Check_return_ HRESULT TryGetAttachedPropertyByName(
	//        _In_ const xstring_ptr_view& strName,
	//        _Outptr_result_maybenull_ const CDependencyProperty** ppDP);

	//    // Gets the underlying dependency property from a property. Use this if pProperty may
	//    // refer to a regular property, and you want the underlying DP for it. If it refers to a DP already,
	//    // this function will return a reference to that DP.
	//    static _Check_return_ HRESULT GetUnderlyingDependencyProperty(
	//        _In_ const CDependencyProperty* pProperty,
	//        _Outptr_ const CDependencyProperty** ppDP);

	//    // Tries to get the underlying dependency property from a property. Use this if pProperty may
	//    // refer to a regular property, and you want the underlying DP for it. If it refers to a DP already,
	//    // this function will return a reference to that DP.
	//    static _Check_return_ HRESULT TryGetUnderlyingDependencyProperty(
	//        _In_ const CDependencyProperty* pProperty,
	//        _Outptr_result_maybenull_ const CDependencyProperty** ppDP);

	//    // Gets the property slot for a DP.
	//    static UINT8 GetPropertySlot(
	//        _In_ KnownPropertyIndex ePropertyIndex);

	//    // Gets the property slot count for a type.
	//    static UINT8 GetPropertySlotCount(
	//        _In_ KnownTypeIndex eTypeIndex);

	//    // Gets an IDependencyProperty object.
	//    static _Check_return_ HRESULT GetIDependencyProperty(
	//        _In_ KnownPropertyIndex ePropertyIndex,
	//        _Outptr_ xaml::IDependencyProperty** ppProperty);

	//    // Registers a custom dependency property.
	//    static _Check_return_ HRESULT RegisterDependencyProperty(
	//        _In_ bool bIsAttached,
	//        _In_ HSTRING hName,
	//        _In_ wxaml_interop::TypeName propertyType,
	//        _In_ wxaml_interop::TypeName ownerType,
	//        _In_opt_ xaml::IPropertyMetadata* pDefaultMetadata,
	//        _In_ bool bIsReadOnly,
	//        _Outptr_ xaml::IDependencyProperty** ppProperty);

	//    // Gets a null enter property, which can be used to identify the last enter property of a type.
	//    static const CEnterDependencyProperty* GetNullEnterProperty();

	//    // Gets a null object property, which can be used to identify the last object property of a type.
	//    static const CObjectDependencyProperty* GetNullObjectProperty();

	//    // Gets a null render property, which can be used to identify the last render property of a type.
	//    static const CRenderDependencyProperty* GetNullRenderProperty();

	//    static _Check_return_ HRESULT EnsureDependencyPropertyInitialized(_In_ const CDependencyProperty* prop);

	#endregion

	#region Assignability tests

	//    // Determines whether the specified target type is assignable from the specified source type.
	//    static bool IsAssignableFrom(
	//        _In_ const CClassInfo* pTargetType,
	//        _In_ const CClassInfo* pSourceType);

	//    static bool IsAssignableFrom(
	//        _In_ KnownTypeIndex eTargetTypeIndex,
	//        _In_ KnownTypeIndex eSourceTypeIndex);

	//    template <KnownTypeIndex targetTypeIndex>
	//    static constexpr bool IsAssignableFrom(
	//        KnownTypeIndex sourceTypeIndex)
	//    {
	//        static_assert(targetTypeIndex != KnownTypeIndex::UnknownType, "Target type needs to be known.");
	//        static_assert(IsKnownIndex(targetTypeIndex), "Target type cannot be custom.");
	//        static_assert(c_aTypeCheckData[static_cast<UINT>(targetTypeIndex)].IsHandleValid(), "Fast check on this target type is not supported.");

	//        ASSERT(sourceTypeIndex != KnownTypeIndex::UnknownType);

	//        if (targetTypeIndex == sourceTypeIndex || targetTypeIndex == KnownTypeIndex::Object)
	//        {
	//            return true;
	//        }
	//        else if (IsKnownIndex(sourceTypeIndex))
	//        {
	//            if constexpr (c_aTypeCheckData[static_cast<UINT>(targetTypeIndex)].IsNotLeaf())
	//            {
	//                const UINT64 sourceHandle = c_aTypeCheckData[static_cast<UINT>(sourceTypeIndex)].GetHandle();
	//                return (sourceHandle & c_aTypeCheckData[static_cast<UINT>(targetTypeIndex)].GetHandleMask()) == c_aTypeCheckData[static_cast<UINT>(targetTypeIndex)].GetHandle();
	//            }
	//            else
	//            {
	//                return false;
	//            }
	//        }
	//        else
	//        {
	//            // No fast type check available. Loop through the base types.
	//            return CompareCustomTypesHelper(GetClassInfoByIndex(sourceTypeIndex), targetTypeIndex);
	//        }
	//    }

	//    // Determines whether the specified object is an instance of the specified type.
	//    static _Check_return_ HRESULT IsInstanceOfType(
	//        _In_ IInspectable* pInstance,
	//        _In_ const CClassInfo* pType,
	//        _Out_ bool* pbIsInstanceOfType);

	#endregion

	#region Provider related methods

	//    // Sets the metadata provider to use for resolving custom types.
	//    static _Check_return_ HRESULT SetMetadataProvider(
	//        _In_ xaml_markup::IXamlMetadataProvider* pMetadataProvider);

	//    // Tries to get a IXamlMetadataProvider.
	//    static _Check_return_ HRESULT TryGetMetadataProvider(
	//        _Outptr_result_maybenull_ xaml_markup::IXamlMetadataProvider** ppMetadataProvider);

	//    // Overrides the default metadata provider.
	//    static void OverrideMetadataProvider(
	//        _In_opt_ xaml_markup::IXamlMetadataProvider* pMetadataProvider);

	#endregion

	#region Index checks

	//    // Determines whether the specified index refers to a built-in namespace.
	//    static constexpr bool IsKnownIndex(_In_ KnownNamespaceIndex eNamespaceIndex)
	//    {
	//        return (static_cast<UINT16>(eNamespaceIndex) < KnownNamespaceCount);
	//    }

	//    // Determines whether the specified index refers to a built-in property.
	//    static constexpr bool IsKnownIndex(_In_ KnownPropertyIndex ePropertyIndex)
	//    {
	//        return (static_cast<UINT16>(ePropertyIndex) < KnownPropertyCount);
	//    }

	//    static constexpr bool IsKnownDependencyPropertyIndex(_In_ KnownPropertyIndex ePropertyIndex)
	//    {
	//        return (static_cast<UINT16>(ePropertyIndex) < KnownDependencyPropertyCount);
	//    }

	//    static constexpr bool IsKnownSimplePropertyIndex(_In_ KnownPropertyIndex ePropertyIndex)
	//    {
	//        return (static_cast<UINT16>(ePropertyIndex) >= KnownDependencyPropertyCount) &&
	//                (static_cast<UINT16>(ePropertyIndex) < KnownPropertyCount);
	//    }

	//    static bool IsCustomIndex(_In_ KnownPropertyIndex ePropertyIndex);

	//    // Determines whether the specified index refers to a built-in type.
	//    static constexpr bool IsKnownIndex(_In_ KnownTypeIndex eTypeIndex)
	//    {
	//        return (static_cast<UINT16>(eTypeIndex) < KnownTypeCount);
	//    }

	//    static UINT16 GetRelativeSimplePropertyIndex(KnownPropertyIndex index)
	//    {
	//        ASSERT(IsKnownSimplePropertyIndex(index));
	//        return static_cast<UINT>(index) - KnownDependencyPropertyCount;
	//    }

	//    static UINT16 GetRelativeCustomIndex(KnownPropertyIndex index)
	//    {
	//        ASSERT(!IsKnownIndex(index));
	//        return static_cast<UINT>(index) - KnownPropertyCount;
	//    }

	#endregion

	#region Reset related methods

	//    // Resets the runtime type caches. BEWARE: This API should really only be called when we are in design mode. However, at the
	//    // time there are other obstacles preventing us from enforcing this. If not in design mode, this will not get our metadata to a
	//    // known good state because the metadata providers won't re-register themselves. We need to fix the re-entrancy issue and have
	//    // specific Shutdown and Initialize methods (which can be encapsulated inside of Reset).
	//    static void Reset();

	//    // Destroys all metadata state.
	//    static void Destroy();

	#endregion

	//    // Checks whether the specified name used to be (<=Blue) an event name that we silently ignored at runtime.
	//    static bool IsLegacyEventName(
	//        _In_ const CClassInfo* pType,
	//        _In_ const xstring_ptr_view& strName);

	//private:
	//    // Associates a dependency property with a type in a runtime cache.
	//    static _Check_return_ HRESULT AssociateDependencyProperty(_In_ const CClassInfo* pType, _In_ const CDependencyProperty* pProperty);

	//    // Associates a property with a type in a runtime cache.
	//    static _Check_return_ HRESULT AssociateProperty(_In_ const CClassInfo* pType, _In_ const CDependencyProperty* pProperty);

	//    // Resolves the type of an object; optionally resolves WinRT PropertyValues with Property_OtherType
	//    static _Check_return_ HRESULT GetClassInfoFromObject_Helper(_In_opt_ IInspectable* pInstance, _Outptr_ const CClassInfo** ppType, _In_ bool resolveWinRTPropertyOtherType);

	//    // Extracts the namespace name and short name from the specified full name.
	//    static _Check_return_ HRESULT ExtractNamespaceNameAndShortName(_In_ const xstring_ptr_view& strTypeFullName, _Out_ xephemeral_string_ptr* pstrNamespaceName, _Out_ xephemeral_string_ptr* pstrTypeName);

	//    // Extracts the short name from the specified full name,
	//    static void ExtractShortName(_In_ const xstring_ptr_view& strTypeFullName, _Out_ xephemeral_string_ptr* pstrTypeName);

	//    // Resolves a type to represent the specified IXamlType.
	//    static _Check_return_ HRESULT GetClassInfoByXamlType(_In_ xaml_markup::IXamlType* pXamlType, _Outptr_ const CClassInfo** ppType);

	//    // Resolves a type from an IPropertyValue of type=OtherType.
	//    static _Check_return_ HRESULT GetClassInfoFromOtherTypeBox(_In_ wf::IPropertyValue* pValue, _Outptr_ const CClassInfo** ppType);

	//    // Imports an IXamlType.
	//    static _Check_return_ HRESULT ImportClassInfo(_In_ xaml_markup::IXamlType* pXamlType, _Outptr_ const CClassInfo** ppType);

	//    // Resolves a type via the IXamlMetadataProvider and adds it to the runtime type cache.
	//    static _Check_return_ HRESULT ImportClassInfoFromMetadataProvider(_In_ const xstring_ptr_view& strTypeFullName, _Outptr_ const CClassInfo** ppType);
	//    static _Check_return_ HRESULT ImportClassInfoFromMetadataProvider(_In_ wxaml_interop::TypeName typeName, _Outptr_ const CClassInfo** ppType);

	//    // Imports a namespace into the runtime namespace cache.
	//    static _Check_return_ HRESULT ImportNamespaceInfo(_In_ const xstring_ptr_view& strNamespaceName, _Outptr_ const CNamespaceInfo** ppNamespace);

	//    // Imports an IXamlMember.
	//    static _Check_return_ HRESULT ImportPropertyInfo(_In_ const CClassInfo* pType, _In_ xaml_markup::IXamlMember* pXamlPropery, _Outptr_ const CDependencyProperty** ppProperty);

	//    // Imports an unknown class. Unknown classes are used for type references from DependencyProperty::Register calls to types
	//    // that are not described by anything.
	//    static _Check_return_ HRESULT ImportUnknownClassInfo(_In_ wxaml_interop::TypeName typeName, _Outptr_ const CClassInfo** ppType);

	//    // Processes the queued DP registrations.
	//    static _Check_return_ HRESULT ProcessRegistrations();

	//    // Determines whether the specified type name appears to exist in the namespace table. This allows us
	//    // to skip the type table lookup for custom types.
	//    static bool ShouldLookupInBuiltinNamespaceTable(_In_ const xstring_ptr_view& strNamespaceName)
	//    {
	//        return !!strNamespaceName.StartsWith(XSTRING_PTR_EPHEMERAL(L"Windows.")) || !!strNamespaceName.StartsWith(XSTRING_PTR_EPHEMERAL(L"Microsoft."));
	//    }

	//    // Determines whether the specified type name appears to exist in the type table. This allows us
	//    // to skip the type table lookup for custom types.
	//    static bool ShouldLookupInBuiltinTypeTable(_In_ const xstring_ptr_view& strTypeFullName, _Inout_ bool& fIsPrimitiveType)
	//    {
	//        return strTypeFullName.StartsWith(XSTRING_PTR_EPHEMERAL(L"Windows.")) ||
	//            strTypeFullName.StartsWith(XSTRING_PTR_EPHEMERAL(L"Microsoft.")) ||
	//            (fIsPrimitiveType = (strTypeFullName.FindChar(L'.') == xstring_ptr_view::npos));
	//    }

	//    // Try to find a built-in type by the specified full name.
	//    static const CClassInfo* TryGetBuiltinClassInfoByFullName(_In_ const xstring_ptr_view& strTypeFullName);

	//    // Try to find a type by the specified full name.
	//    static _Check_return_ HRESULT TryGetClassInfoByFullName(_In_ const xstring_ptr_view& strTypeFullName, _In_ bool bSearchCustomTypesOnly, _Outptr_ const CClassInfo** ppType);

	//    // Tries to resolve a DP given its full name using the current parser context.
	//    // NOTE: This method does not need to use the lock because it is not actually looking for the property on the tables directly,
	//    // it will first use the supplied context then default to the tables using one of the existing methods.
	//    static _Check_return_ HRESULT TryGetDependencyPropertyUsingOWContext(_In_ const xstring_ptr_view& strName, _In_ XamlServiceProviderContext* context, _Outptr_result_maybenull_ const CDependencyProperty** ppDP);

	//    // Tries to resolve a property via the IXamlMetadataProvider and adds it to the runtime type cache.
	//    static _Check_return_ HRESULT TryImportPropertyInfoFromMetadataProvider(_In_ const CClassInfo* pType, _In_ const xstring_ptr_view& strName, _Outptr_ const CDependencyProperty** ppProperty);

	//    static bool CompareCustomTypesHelper(const CClassInfo* typeClass, KnownTypeIndex targetType);
}
partial class MetadataAPI // src\dxaml\xcp\components\metadata\MetadataAPI.cpp
{
	/// <summary>
	/// Tries to resolve an attached property by its name. strName should use the format ClassName.PropertyName.
	/// This should only be called for built-in attached properties.
	/// </summary>
	public static DependencyProperty TryGetAttachedPropertyByName(string strName)
	{
		DependencyProperty ppDP = null;

#if !HAS_UNO
		// The property was not found or we can't use the parser context. Assume that the property is in the default namespace.
		// The property will be of the format: ClassName.PropertyName.
		// We will extract the class name first then the property name and do the lookup using the type tables.
		var nDotIndex = strName.IndexOf('.');
		if (nDotIndex != -1)
		{
			// Try to resolve the type.
			string strClassName;
			strClassName = strName[..nDotIndex];
			Type pType = GetBuiltinClassInfoByName(strClassName);

			if (pType != null)
			{
				// Try to resolve the property.
				string strPropertyName;
				strPropertyName = strName[(nDotIndex + 1)..];
				ppDP = TryGetDependencyPropertyByName(pType, strPropertyName);
			}
		}
#else
		if (strName.Split('.', 2) is [string type, string property])
		{
			ppDP = DependencyProperty.GetProperty(type, property);
		}
#endif

		return ppDP;
	}

	public static Type GetPrimitiveClassInfo(Type pType)
	{
		if (pType != null)
		{
#if HAS_UNO
			if (pType == typeof(Guid))
			{
				return pType;
			}
#endif

			switch (Type.GetTypeCode(pType))
			{
				case TypeCode.Int32:
				//case TypeCode.Char16:
				case TypeCode.UInt32:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.String:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Boolean:
#if !HAS_UNO
				case TypeCode.Guid:
#endif
					return pType;

				default:
					return typeof(object);
			}
		}
		else
		{
			return typeof(object);
		}
	}
}
partial class MetadataAPI // src\dxaml\xcp\core\metadata\ReflectionAPI.cpp
{
	/// <summary>
	/// Gets the friendly runtime class name of a type. This may not necessarily be the same as calling GetRuntimeClassName on an IInspectable,
	/// because it takes into account the available type information. Should generally only be used for debug/tracing output.
	/// </summary>
	public static string GetFriendlyRuntimeClassName(object instance)
	{
		// Try to get the name from the object via its TypeName entry in our type table.
		var customPropertyProvider = instance as ICustomPropertyProvider;
		if (customPropertyProvider != null)
		{
#if !HAS_UNO
			TypeName typeName;
			if (typeName = customPropertyProvider.Type)
			{
				const CClassInfo type = null;
				if (SUCCEEDED(GetClassInfoByTypeName(typeName, &type)) && type && type.IsPublic())
				{
					friendlyName = type.GetFullName();
					return S_OK;
				}
			}

			// If we don't get the string from our TypeTable, then we can get really ugly looking strings like:
			//      MyApp.MyType, MyApp, Version=abc.easyas.123, Culture=neutral, etc. Getting the string representation
			// from the CustomPropertyProvder will strip the stuff after MyApp.MyType from the class name and deliver a
			// nice string for debug/tracing.
			string stringRep;
			if (stringRep = customPropertyProvider.GetStringRepresentation())
			{
				friendlyName = stringRep;
				return S_OK;
			}
#else
			if (customPropertyProvider.Type is { IsPublic: true } type)
			{
				return type.FullName;
			}

			return customPropertyProvider.GetStringRepresentation();
#endif
		}

		// Fall back on the runtime class name.
		return GetRuntimeClassName(instance);
	}

	/// <summary>
	/// Gets the runtime class name of a type.
	/// </summary>
	public static string GetRuntimeClassName(object instance)
	{
		// IInspectable::GetRuntimeClassName: Gets the fully qualified name of the current Windows Runtime object.
		return instance.GetType().AssemblyQualifiedName;
	}

	/// <summary>
	/// Look for a DP given its full name, it might use the ObjectWriter context if allowed to.
	/// </summary>
	public static DependencyProperty TryGetDependencyPropertyByFullyQualifiedName(
		string strName,
		XamlServiceProviderContext context = null)
	{
		// Assume it is not found.
		DependencyProperty ppDP = null;

		// Uno TODO: implemented/inject IXamlTypeResolver
		//if (context is { })
		//{
		//	ppDP = TryGetDependencyPropertyUsingOWContext(strName, context);
		//}
		if (ppDP == null)
		{
			ppDP = TryGetAttachedPropertyByName(strName);
		}

		return ppDP;
	}
}
partial class MetadataAPI // src\dxaml\xcp\dxaml\lib\MetadataAPI.cpp
{

}
partial class MetadataAPI // quick impl
{
	private static Dictionary<(Type Type, string Property), DependencyProperty> _dependencyPropertyReflectionCache = new(512);

	private const BindingFlags DpStorageFlags =
		BindingFlags.Public | BindingFlags.NonPublic |
		BindingFlags.Static |
		BindingFlags.FlattenHierarchy;

	internal const DynamicallyAccessedMemberTypes TryGetDependencyPropertyByName_Type_Requirements =
		  DynamicallyAccessedMemberTypes.PublicProperties
		| DynamicallyAccessedMemberTypes.NonPublicProperties
		| DynamicallyAccessedMemberTypes.PublicFields
		| DynamicallyAccessedMemberTypes.NonPublicFields
#if NET10_0_OR_GREATER
		| DynamicallyAccessedMemberTypes.NonPublicFieldsWithInherited
		| DynamicallyAccessedMemberTypes.NonPublicPropertiesWithInherited
#endif  // NET10_0_OR_GREATER
		;

	public static DependencyProperty TryGetDependencyPropertyByName(
		[DynamicallyAccessedMembers(TryGetDependencyPropertyByName_Type_Requirements)]
		Type type,
		string propertyName)
	{
		var key = (type, propertyName);

		if (!_dependencyPropertyReflectionCache.TryGetValue(key, out var property))
		{
			property = GetValue(
				type.GetProperty($"{propertyName}Property", DpStorageFlags) as MemberInfo ??
				type.GetField($"{propertyName}Property", DpStorageFlags)
			);

			static DependencyProperty GetValue(MemberInfo member) => member switch
			{
				PropertyInfo pi => pi.GetValue(null) as DependencyProperty,
				FieldInfo fi => fi.GetValue(null) as DependencyProperty,
				_ => null,
			};
		}

		return property;
	}

	public static Type GetClassInfoByTypeName(Type typeName) => typeName;
}
