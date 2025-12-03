#nullable enable

#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Globalization;
using System.Reflection;

using Microsoft.UI.Xaml;
using System.Collections;

using Microsoft.UI.Xaml.Data;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

namespace Uno.UI.DataBinding
{
	internal delegate void ValueSetterHandler(object instance, object? value);
	internal delegate void ValueUnsetterHandler(object instance);
	internal delegate object? ValueGetterHandler(object instance);

	public static class BindableMetadata
	{
		/// <summary>
		/// Sets the metadata provider for the whole app domain.
		/// </summary>
		public static IBindableMetadataProvider? Provider
		{
			get => BindingPropertyHelper.BindableMetadataProvider;
			set => BindingPropertyHelper.BindableMetadataProvider = value;
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "normal flow of operation")]
	internal static partial class BindingPropertyHelper
	{
		private static readonly Logger _log = typeof(BindingPropertyHelper).Log();

		//
		// Warning: These dictionaries are here in place of memoized Funcs for performance
		//          on Mono 4.0, because of the cost of creating generic delegates and invoking 
		//          those. If this situation changes, we could remove the associated code and 
		//          revert to memoized Funcs.
		//
		private static Dictionary<GetValueGetterCacheKey, ValueGetterHandler> _getValueGetter = new(GetValueGetterCacheKey.Comparer);
		private static Dictionary<GetValueSetterCacheKey, ValueSetterHandler> _getValueSetter = new(GetValueSetterCacheKey.Comparer);
		private static Dictionary<GenericPropertyCacheKey, ValueGetterHandler> _getSubstituteValueGetter = new(GenericPropertyCacheKey.Comparer);
		private static Dictionary<GenericPropertyCacheKey, ValueUnsetterHandler> _getValueUnsetter = new(GenericPropertyCacheKey.Comparer);

		private static HashtableEx _getPropertyType = new HashtableEx(GetPropertyTypeKey.Comparer, usePooling: false);
		private static GetPropertyTypeKey _getPropertyTypeKey = new();

		private static Type? _unoGetMemberBindingType;
		private static Type? _unoSetMemberBindingType;

		static BindingPropertyHelper()
		{
			MethodInvokerBuilder = DefaultInvokerBuilder;
		}

		internal static void ClearCaches()
		{
			_getValueGetter.Clear();
			_getValueSetter.Clear();
			_getSubstituteValueGetter.Clear();
			_getValueUnsetter.Clear();
			_getPropertyType.Clear();
		}

		private static Func<object, object?[], object?> DefaultInvokerBuilder(MethodInfo method)
		{
			return (instance, args) => method.Invoke(instance, args);
		}

		public static Func<MethodInfo, Func<object, object?[], object?>> MethodInvokerBuilder
		{
			get;
			set;
		}

		/// <summary>
		/// Sets an external metadata provider.
		/// </summary>
		public static IBindableMetadataProvider? BindableMetadataProvider { get; set; }

		public static Type? GetPropertyType(Type type, string property, bool allowPrivateMembers)
		{
			_getPropertyTypeKey.Update(type, property, allowPrivateMembers);

			object? result;

			if (!_getPropertyType.TryGetValue(_getPropertyTypeKey, out result))
			{
				_getPropertyType.Add(_getPropertyTypeKey.Clone(), result = InternalGetPropertyType(type, property, allowPrivateMembers));
			}

			return Unsafe.As<Type>(result);
		}

		internal static ValueGetterHandler GetValueGetter(Type type, string property)
			=> GetValueGetter(type: type, property: property, allowPrivateMembers: false);

		/// <summary>
		/// Gets a <see cref="ValueGetterHandler"/> for a named property
		/// </summary>
		/// <param name="type">The type to search</param>
		/// <param name="property">The name of the property to get</param>
		/// <param name="allowPrivateMembers">Allows for private members to be included in the search</param>
		internal static ValueGetterHandler GetValueGetter(Type type, string property, bool allowPrivateMembers)
		{
			var key = new GetValueGetterCacheKey(type, property, allowPrivateMembers);

			ValueGetterHandler? result;

			lock (_getValueGetter)
			{
				if (!_getValueGetter.TryGetValue(key, out result))
				{
					_getValueGetter.Add(key, result = InternalGetValueGetter(type, property, allowPrivateMembers));
				}
			}

			return result;
		}

		internal static ValueSetterHandler GetValueSetter(Type type, string property, bool convert)
		{
			return GetValueSetter(type, property, convert, DependencyPropertyValuePrecedences.Local);
		}

		internal static ValueSetterHandler GetValueSetter(Type type, string property, bool convert, DependencyPropertyValuePrecedences precedence)
		{
			var key = new GetValueSetterCacheKey(type, property, precedence, convert);

			ValueSetterHandler? result;

			lock (_getValueSetter)
			{
				if (!_getValueSetter.TryGetValue(key, out result))
				{
					_getValueSetter.Add(key, result = InternalGetValueSetter(type, property, convert, precedence));
				}
			}

			return result;
		}

		internal static ValueGetterHandler GetSubstituteValueGetter(Type type, string property)
		{
			var key = new GenericPropertyCacheKey(type, property, DependencyPropertyValuePrecedences.Animations);

			ValueGetterHandler? result;

			lock (_getSubstituteValueGetter)
			{
				if (!_getSubstituteValueGetter.TryGetValue(key, out result))
				{
					_getSubstituteValueGetter.Add(key, result = InternalGetSubstituteValueGetter(type, property));
				}
			}

			return result;
		}

		internal static ValueUnsetterHandler GetValueUnsetter(Type type, string property)
		{
			return GetValueUnsetter(type, property, DependencyPropertyValuePrecedences.Local);
		}

		internal static ValueUnsetterHandler GetValueUnsetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			var key = new GenericPropertyCacheKey(type, property, precedence);

			ValueUnsetterHandler? result;

			lock (_getValueUnsetter)
			{
				if (!_getValueUnsetter.TryGetValue(key, out result))
				{
					_getValueUnsetter.Add(key, result = InternalGetValueUnsetter(type, property, precedence));
				}
			}

			return result;
		}

		private static Type? InternalGetPropertyType(Type type, string property, bool allowPrivateMembers)
		{
			if (type == typeof(UnsetValue))
			{
				return null;
			}

			if (property.Equals("''", StringComparison.Ordinal))
			{
				return type;
			}

			property = SanitizePropertyName(type, property);

#if PROFILE
			using (Performance.Measure("InternalGetPropertyType"))
#endif
			{
				if (IsValidMetadataProviderType(type) && BindableMetadataProvider != null)
				{
					var bindablePropertyDescriptor = BindablePropertyDescriptor.GetPropertByBindableMetadataProvider(type, property);

					if (bindablePropertyDescriptor.OwnerType != null)
					{
						if (IsIndexerFormat(property))
						{
							if (bindablePropertyDescriptor.OwnerType.GetIndexerGetter() != null)
							{
								// In the case of bindable properties, the return
								// type of an indexer is always an object.
								return typeof(object);
							}
						}

						if (bindablePropertyDescriptor.Property != null)
						{
							// In the case of bindable properties, the return
							// type of an indexer is always an object.
							return bindablePropertyDescriptor.Property.PropertyType;
						}
						else
						{
							_log.ErrorFormat("The [{0}] property does not exist on type [{1}]", property, type);
							return null;
						}
					}
				}

#if PROFILE
				using (Performance.Measure("InternalGetPropertyType.Reflection"))
#endif
				{
					if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						_log.Debug($"GetPropertyType({type}, {property}) [Reflection]");
					}

					// Fallback on reflection-based lookup
					if (IsIndexerFormat(property))
					{
						// Fallback on reflection-based lookup

						// In some cases, there are multiple indexers, in which case GetIndexerInfo fails due to multiple matches when not given an explicit parameter type.
						// If we know this parses as an integer, then use typeof(int) to reduce the cases of failure.
						Type? indexerParameterType = null;
						if (int.TryParse(property.AsSpan().Slice(1, property.Length - 2), NumberStyles.Number, NumberFormatInfo.InvariantInfo, out _))
						{
							indexerParameterType = typeof(int);
						}
						else
						{
							indexerParameterType = typeof(string);
						}

						var indexerInfo = GetIndexerInfo(type, indexerParameterType, allowPrivateMembers: allowPrivateMembers);
						indexerInfo ??= GetIndexerInfo(type, null, allowPrivateMembers: allowPrivateMembers);

						if (indexerInfo != null)
						{
							return indexerInfo.PropertyType;
						}
						else
						{
							_log.ErrorFormat("The Indexer property getter does not exist on type [{0}]", type);
							return null;
						}
					}

					var propertyInfo = GetPropertyInfo(type, property, allowPrivateMembers: allowPrivateMembers);

					if (propertyInfo != null)
					{
						return propertyInfo.PropertyType;
					}

					// Look for a field (permitted for x:Bind only)
					if (allowPrivateMembers)
					{
						var fieldInfo = GetFieldInfo(type, property, true);
						if (fieldInfo != null)
						{
							return fieldInfo.FieldType;
						}
					}

					// Look for an attached property
					var attachedPropertyGetter = GetAttachedPropertyGetter(type, property);

					if (attachedPropertyGetter != null)
					{
						return attachedPropertyGetter.ReturnType;
					}

					if (type.IsValueType && property == "Value")
					{
						// This case is trying assuming that Value for a primitive is used for the case
						// of a Nullable primitive.

						return type;
					}

					_log.ErrorFormat("The [{0}] property getter does not exist on type [{1}]", property, type);
					return null;
				}
			}
		}

		/// <summary>
		/// Finds the specified property info by name.
		/// </summary>
		/// <param name="type">The type to search</param>
		/// <param name="name">The name of the property</param>
		/// <returns>A <see cref="PropertyInfo"/> instance.</returns>
		/// <remarks>
		/// This method is required when searching in types that
		/// include "new" non-virtual overridden members. In Mono 4.0 and
		/// earlier, the highest match would be returned, but the .NET core 
		/// implementation found in Mono 4.2+ throws an ambiguous match exception.
		/// This requires a recursive search using the <see cref="BindingFlags.DeclaredOnly"/> flag.
		///
		/// The private members lookup is present to enable the binding to
		/// x:Name elements in x:Bind operations.
		/// </remarks>
		private static PropertyInfo? GetPropertyInfo(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
			string name,
			bool allowPrivateMembers)
		{
			do
			{
				var info = type.GetProperty(
					name,
					BindingFlags.Instance
					| BindingFlags.Static
					| BindingFlags.Public
					| (allowPrivateMembers ? BindingFlags.NonPublic : BindingFlags.Default)
					| BindingFlags.DeclaredOnly
				);

				if (info != null)
				{
					return info;
				}

				type = type.BaseType!;
			}
			while (type != null);

			return null;
		}

		/// <summary>
		/// Finds the specified property info by name.
		/// </summary>
		/// <param name="type">The type to search</param>
		/// <param name="name">The name of the property</param>
		/// <returns>A <see cref="PropertyInfo"/> instance.</returns>
		/// <remarks>
		/// This method is required when searching in types that
		/// include "new" non-virtual overridden members. In Mono 4.0 and
		/// earlier, the highest match would be returned, but the .NET core 
		/// implementation found in Mono 4.2+ throws an ambiguous match exception.
		/// This requires a recursive search using the <see cref="BindingFlags.DeclaredOnly"/> flag.
		///
		/// The private members lookup is present to enable the binding to
		/// x:Name elements in x:Bind operations.
		/// </remarks>
		private static PropertyInfo? GetIndexerInfo(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
			Type? parameterType,
			bool allowPrivateMembers)
		{
			var parameterTypes = parameterType is not null
				? new[] { parameterType }
				: null;

			var bindingFlags = BindingFlags.Instance
					| BindingFlags.Static
					| BindingFlags.Public
					| (allowPrivateMembers ? BindingFlags.NonPublic : BindingFlags.Default)
					| BindingFlags.DeclaredOnly;

			do
			{
				var info = parameterTypes is not null
					? type.GetProperty(
						name: "Item"
						, bindingAttr: bindingFlags
						, binder: null
						, returnType: null
						, types: parameterTypes
						, modifiers: null
					)
					: type.GetProperty(name: "Item", bindingAttr: bindingFlags);

				if (info != null)
				{
					return info;
				}

				type = type.BaseType!;
			}
			while (type != null);

			return null;
		}

		private static FieldInfo? GetFieldInfo(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type,
			string name,
			bool allowPrivateMembers)
		{
			do
			{
				var info = type.GetField(
					name,
					BindingFlags.Instance
					| BindingFlags.Static
					| BindingFlags.Public
					| (allowPrivateMembers ? BindingFlags.NonPublic : BindingFlags.Default)
					| BindingFlags.DeclaredOnly
				);

				if (info != null)
				{
					return info;
				}

				type = type.BaseType!;
			}
			while (type != null);

			return null;
		}

		/// <summary>
		/// Determines if the property is referencing a C# indexer.
		/// </summary>
		/// <param name="property">The property name</param>
		private static bool IsIndexerFormat(string property)
		{
			return property.Length > 0 && property[0] == '[' && property[property.Length - 1] == ']';
		}

		private static string SanitizePropertyName(Type type, string property)
		{
			// Possible values for 'property' parameter:
			// - "PropertyName"
			// - "TypeName.PropertyName"
			// - "Microsoft.UI.Xaml.Controls:UIElement.Opacity" (fully qualified)

			if (property.Contains('.'))
			{
				var parts = property
					.Replace(":", ".") // ':' is sometimes used to separate namespace from type
					.Split('.')
					.Reverse()
					.Take(2) // type name + property name
					.Reverse()
					.ToArray();

				if (parts.Length == 2)
				{
					if (parts[0] == type.Name

						// This is the list of types that are currently
						// not fully respecting the WPF class hierarchy.
						// Once this will be fixed, remove those special cases.
						|| parts[0] == "UIElement"
						|| parts[0] == "FrameworkElement"
					)
					{
						property = parts[1];
					}
				}
			}

			return property;
		}

		[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "GetField/GetType may return null, normal flow of operation")]
		[UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "GetField/GetType may return null, normal flow of operation")]
		private static ValueGetterHandler InternalGetValueGetter(Type type, string property, bool allowPrivateMembers)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueGetter;
			}

			if (property.Equals("''", StringComparison.Ordinal))
			{
				return value => value;
			}

			property = SanitizePropertyName(type, property);

			if (IsIndexerFormat(property))
			{
				var indexerString = property.Substring(1, property.Length - 2);

				if (type.IsArray)
				{
					int index;

					if (int.TryParse(indexerString, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
					{
						return obj =>
						{
							if (obj is Array array && array.Length > index)
							{
								return array.GetValue(index);
							}
							else
							{
								_log.ErrorFormat($"The index [{0}] was outside of the bounds of the [{1}]", index, type);
								return DependencyProperty.UnsetValue;
							}
						};
					}
					else
					{
						_log.ErrorFormat("The property [{0}] is not valid for [{1}]", property, type);
					}
				}
				else if (type.Is<IList>())
				{
					int index;

					if (int.TryParse(indexerString, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
					{
						return obj =>
						{
							if (obj is IList list && list.Count > index)
							{
								return list[index];
							}
							else
							{
								_log.ErrorFormat($"The index [{0}] was outside of the bounds of the [{1}]", index, type);
								return DependencyProperty.UnsetValue;
							}
						};
					}
					else
					{
						_log.ErrorFormat("The property [{0}] is not valid for [{1}]", property, type);
					}
				}

				// Start by using the provider, to avoid reflection
				if (IsValidMetadataProviderType(type) && BindableMetadataProvider != null)
				{
#if PROFILE
					using (Performance.Measure("GetValueGetter.BindableMetadataProvider"))
#endif
					{
						var bindableType = BindableMetadataProvider.GetBindableTypeByType(type);

						if (bindableType != null)
						{
							var indexerMethod = bindableType.GetIndexerGetter();

							if (indexerMethod != null)
							{
								indexerString = CoerceIndexerParameter(indexerString, optionalParameterType: null);
								return instance => indexerMethod(instance, indexerString);
							}
						}
					}
				}

#if PROFILE
				using (Performance.Measure("InternalGetValueGetter.Reflection"))
#endif
				{
					if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						_log.Debug($"GetValueGetter({type}, {property}) [Reflection]");
					}

					var indexerPropertyType = int.TryParse(indexerString, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
						? typeof(int)
						: typeof(string);

					// Try explicitly with known types
					var indexerInfo = GetIndexerInfo(type, indexerPropertyType, allowPrivateMembers);

					// Then if search fails, search by name only
					indexerInfo ??= GetIndexerInfo(type, null, allowPrivateMembers);

					if (indexerInfo is not null)
					{
						// This is an indexer
						var method = indexerInfo.GetGetMethod();

						if (method == null)
						{
							var empty = Funcs.CreateMemoized<object>(() =>
							{
								_log.ErrorFormat("The Indexer Getter does not exist on [{0}]", type);
								return DependencyProperty.UnsetValue;
							});

							return instance => empty();
						}

						var handler = MethodInvokerBuilder(method);
						var indexerParameterType = indexerInfo.GetIndexParameters()[0].ParameterType;
						indexerString = CoerceIndexerParameter(indexerString, indexerParameterType);

						return instance => handler(
							instance,
							new object?[] {
								Convert(indexerParameterType, indexerString)
							}
						);
					}
					else
					{
						var empty = Funcs.CreateMemoized<object>(() =>
						{
							_log.ErrorFormat("The Indexer property getter does not exist on type [{0}]", type);
							return DependencyProperty.UnsetValue;
						});

						return instance => empty();
					}
				}
			}

			// Start by using the provider, to avoid reflection
			if (IsValidMetadataProviderType(type) && BindableMetadataProvider != null)
			{
#if PROFILE
				using (Performance.Measure("GetValueGetter.BindableMetadataProvider"))
#endif
				{
					var bindableProperty = BindablePropertyDescriptor.GetPropertByBindableMetadataProvider(type, property);

					if (bindableProperty.OwnerType != null)
					{
						if (bindableProperty?.Property?.Getter != null)
						{
							if (bindableProperty.Property.DependencyProperty is { } dependencyProperty)
							{
								return instance => instance.GetValue(dependencyProperty);
							}
							else
							{
								var getter = bindableProperty.Property.Getter;
								return instance => getter(instance, precedence: null);
							}
						}
					}
				}
			}

#if PROFILE
			using (Performance.Measure("InternalGetValueGetter.Reflection2"))
#endif
			{
				if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					_log.Debug($"GetValueGetter({type}, {property}) [Reflection]");
				}

				var dp = FindDependencyProperty(type, property);

				if (dp != null)
				{
					return instance => ((DependencyObject)instance).GetValue(dp);
				}

				// Look for a property
				var propertyInfo = GetPropertyInfo(type, property, allowPrivateMembers: allowPrivateMembers);
				if (propertyInfo != null)
				{
					var getMethod = propertyInfo.GetGetMethod(nonPublic: allowPrivateMembers);

					if (getMethod == null)
					{
						var emptyProperty = Funcs.CreateMemoized<object>(() =>
						{
							_log.ErrorFormat("The Property [{0}] Getter does not exist on [{1}]", property, type);
							return DependencyProperty.UnsetValue;
						});

						return instance => emptyProperty();
					}

					var handler = MethodInvokerBuilder(getMethod);

					return instance => handler(instance, Array.Empty<object>());
				}

				// Look for a field (permitted for x:Bind only)
				if (allowPrivateMembers)
				{
					var fieldInfo = GetFieldInfo(type, property, true);
					if (fieldInfo != null)
					{
						return instance => fieldInfo.GetValue(instance);
					}
				}

				// Look for an attached property
				var attachedPropertyGetter = GetAttachedPropertyGetter(type, property);

				if (attachedPropertyGetter != null)
				{
					if (!attachedPropertyGetter.IsStatic)
					{
						var emptyAttached = Funcs.CreateMemoized<object>(() =>
						{
							_log.ErrorFormat("The attached property Getter for [{0}] must be static", property);
							return DependencyProperty.UnsetValue;
						});

						return instance => emptyAttached();
					}

					return instance => attachedPropertyGetter.Invoke(null, new[] { instance });
				}

				if (__LinkerHints.Is_System_Dynamic_ExpandoObject_Available
					&& type == typeof(System.Dynamic.ExpandoObject))
				{
					return instance =>
					{
						if (
							instance is IDictionary<string, object> source
							&& source.TryGetValue(property, out var value)
						)
						{
							return value;
						}

						return null;
					};
				}

				if (__LinkerHints.Is_System_Dynamic_DynamicObject_Available
					&& type.Is(typeof(System.Dynamic.DynamicObject)))
				{
					return [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnoGetMemberBinder))] (instance) =>
					{
						if (__LinkerHints.Is_System_Dynamic_DynamicObject_Available
							&& instance is System.Dynamic.DynamicObject dynamicObject)
						{
							// Referencing UnoGetMemberBinder using a typeof or a generic type is enough to pull
							// the System.Dynamic containing assembly. We're using reflection to create the binder
							// given that the full type has been kept through `DynamicDependency` provided above.
							_unoGetMemberBindingType ??=
								Type.GetType("Uno.UI.DataBinding.BindingPropertyHelper+UnoGetMemberBinder, " + typeof(BindingPropertyHelper).Assembly.FullName)
								?? throw new InvalidOperationException();

							var binder = (GetMemberBinder?)Activator.CreateInstance(_unoGetMemberBindingType, property, true);

							if (binder is not null)
							{
								if (dynamicObject.TryGetMember(binder, out var binderValue))
								{
									return binderValue;
								}
							}
							else
							{
								_log.ErrorFormat("The type UnoGetMemberBinder is not available, likely caused by an incorrect Linker configuration.");
							}
						}

						return null;
					};
				}

				if (
					type.IsValueType
					&& property == "Value"
				)
				{
					// This case is trying assuming that Value for a primitive is used for the case
					// of a Nullable primitive.

					return instance => instance;
				}

				// No getter has been found
				var empty = Funcs.CreateMemoized<object>(() =>
				{
					_log.ErrorFormat("The [{0}] property getter does not exist on type [{1}]", property, type);
					return DependencyProperty.UnsetValue;
				});

				return instance => empty();
			}
		}

		private static string CoerceIndexerParameter(string indexerParameter, Type? optionalParameterType)
		{
			if (optionalParameterType is null || optionalParameterType == typeof(string))
			{
				if (indexerParameter.Length >= 2 && indexerParameter[0] == '"' && indexerParameter[indexerParameter.Length - 1] == '"')
				{
					return indexerParameter.Substring(1, indexerParameter.Length - 2);
				}
			}

			return indexerParameter;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "To be refactored")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "To be refactored")]
		[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "GetField/GetType may return null, normal flow of operation")]
		[UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "GetField/GetType may return null, normal flow of operation")]

		private static ValueSetterHandler InternalGetValueSetter(Type type, string property, bool convert, DependencyPropertyValuePrecedences precedence)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueSetter;
			}

			property = SanitizePropertyName(type, property);

			Func<Func<Type?>?, object?, object?> convertSelector =
				convert ? Convert : (Func<Func<Type?>?, object?, object?>)((p, v) => v);

			if (IsIndexerFormat(property))
			{
				// This is an indexer
				var indexerRawParameter = property.Substring(1, property.Length - 2);
				object indexerParameter =
					int.TryParse(indexerRawParameter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var indexerIndex)
						? indexerIndex
						: indexerRawParameter;

				// The fastest path uses the generated bindable metadata, which does not require
				// the property info, unless there is an actual conversion to perform.
				// So we just close over the property.
				var indexerInfo = Uno.Funcs.CreateMemoized(() =>
				{
					var paramType = indexerParameter is int ? typeof(int) : typeof(string);

					// Search for the explicit parameter type
					if (GetIndexerInfo(type, paramType, allowPrivateMembers: false) is { } withParamResult)
					{
						return withParamResult;
					}
					// Search for a string parameter
					else if (GetIndexerInfo(type, typeof(string), allowPrivateMembers: false) is { } withStringParamResult)
					{
						return withStringParamResult;
					}
					// If not found, search for any matching type
					else if (paramType is not null)
					{
						return GetIndexerInfo(type, null, allowPrivateMembers: false);
					}

					return null;
				});
				var indexerType = Uno.Funcs.CreateMemoized(() => indexerInfo()?.PropertyType);


				// Start by using the provider, to avoid reflection
				if (IsValidMetadataProviderType(type)
					&& BindableMetadataProvider != null
					&& indexerParameter is string)
				{
#if PROFILE
					using (Performance.Measure("GetValueSetter.BindableMetadataProvider"))
#endif
					{
						var bindableType = BindableMetadataProvider.GetBindableTypeByType(type);

						if (bindableType != null)
						{
							var indexerMethod = bindableType.GetIndexerSetter();

							if (indexerMethod != null)
							{
								return (instance, value) => indexerMethod(instance, indexerRawParameter, convertSelector(indexerType, value));
							}
							else
							{
								var once = Actions.CreateOnce(
									() => _log.ErrorFormat("The Indexer property setter does not exist on type [{0}]", type)
								);

								return delegate { once(); };
							}
						}
					}
				}

#if PROFILE
				using (Performance.Measure("InternalGetValueSetter.Reflection"))
#endif
				{
					if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						_log.Debug($"GetValueSetter({type}, {property}) [Reflection]");
					}

					var indexerInfoValue = indexerInfo();
					if (indexerInfoValue != null)
					{
						var method = indexerInfoValue.GetSetMethod();

						if (method != null)
						{
							var handler = MethodInvokerBuilder(method);

							if (method.GetParameters() is { Length: 2 } indexerMethodParams
								&& indexerMethodParams[0].ParameterType == typeof(string))
							{
								indexerParameter = indexerRawParameter;
							}

							return (instance, value) => handler(instance, new object?[] { indexerParameter, convertSelector(indexerType, value) });
						}
						else
						{
							var once = Actions.CreateOnce(
								() => _log.ErrorFormat("The Indexer Setter does not exist on [{0}]", type)
							);

							return delegate { once(); };
						}
					}
					else
					{
						var once = Actions.CreateOnce(
							() => _log.ErrorFormat("The Indexer property setter does not exist on type [{0}]", type)
						);

						return delegate { once(); };
					}
				}
			}

			// Start by using the provider, to avoid reflection
			if (IsValidMetadataProviderType(type) && BindableMetadataProvider != null)
			{
#if PROFILE
				using (Performance.Measure("GetValueSetter.BindableMetadataProvider"))
#endif
				{
					var bindableProperty = BindablePropertyDescriptor.GetPropertByBindableMetadataProvider(type, property);

					if (bindableProperty.OwnerType != null)
					{
						if (bindableProperty != null)
						{
							if (bindableProperty.Property?.DependencyProperty is { } dependencyProperty)
							{
								return (instance, value) => instance.SetValue(dependencyProperty, convertSelector(() => bindableProperty.Property.PropertyType, value), precedence);
							}

							if (bindableProperty.Property?.Setter != null)
							{
								var setter = bindableProperty.Property.Setter;

								return (instance, value) => setter(instance, convertSelector(() => bindableProperty.Property.PropertyType, value), precedence);
							}
						}

						var once = Actions.CreateOnce(
							() => _log.ErrorFormat("The property setter for [{0}] does not exist on [{1}]", property, type)
						);

						return delegate { once(); };
					}
				}
			}

#if PROFILE
			using (Performance.Measure("InternalGetValueSetter.Reflection"))
#endif
			{
				if (_log.IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					_log.Debug($"GetValueSetter({type}, {property}) [Reflection]");
				}

				var dp = FindDependencyProperty(type, property) ?? FindAttachedProperty(type, property);

				if (dp != null)
				{
					return (instance, value) => DependencyObjectExtensions.SetValue((DependencyObject)instance, dp, convertSelector(() => dp.Type, value), precedence);
				}

				var propertyInfo = GetPropertyInfo(type, property, allowPrivateMembers: false);
				if (propertyInfo != null)
				{
					var setMethod = propertyInfo.GetSetMethod();

					if (setMethod != null)
					{
						var propertyType = Uno.Funcs.CreateMemoized(() => GetPropertyOrDependencyPropertyType(type, property));
						var handler = MethodInvokerBuilder(setMethod);

						return (instance, value) => handler(instance, new object?[] { convertSelector(propertyType, value) });
					}
				}

				if (__LinkerHints.Is_System_Dynamic_ExpandoObject_Available
					&& type == typeof(System.Dynamic.ExpandoObject))
				{
					return (instance, value) =>
					{
						if (instance is IDictionary<string, object?> source)
						{
							source[property] = value;
						}
					};
				}


				if (__LinkerHints.Is_System_Dynamic_DynamicObject_Available
					&& type.Is(typeof(System.Dynamic.DynamicObject)))
				{
					return [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnoSetMemberBinder))] (instance, value) =>
					{
						if (__LinkerHints.Is_System_Dynamic_DynamicObject_Available
							&& instance is System.Dynamic.DynamicObject dynamicObject)
						{
							// Referencing UnoSetMemberBinder using a typeof or a generic type is enough to pull
							// the System.Dynamic containing assembly. We're using reflection to create the binder
							// given that the full type has been kept through `DynamicDependency` provided above.
							_unoSetMemberBindingType ??=
								Type.GetType("Uno.UI.DataBinding.BindingPropertyHelper+UnoSetMemberBinder, " + typeof(BindingPropertyHelper).Assembly.FullName)
								?? throw new InvalidOperationException();

							var binder = (SetMemberBinder?)Activator.CreateInstance(_unoSetMemberBindingType, property, true);

							if (binder is not null)
							{
								dynamicObject.TrySetMember(binder, value);
							}
							else
							{
								_log.ErrorFormat("The type UnoSetMemberBinder is not available, likely caused by an incorrect Linker configuration.");
							}

						}
					};
				}

				var once = Actions.CreateOnce(
					() => _log.ErrorFormat("The property setter for [{0}] does not exist on [{1}]", property, type)
				);

				return delegate { once(); };
			}
		}

		private static ValueGetterHandler InternalGetSubstituteValueGetter(Type type, string property)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueGetter;
			}

			property = SanitizePropertyName(type, property);

			var dp = FindDependencyProperty(type, property);

			if (dp != null)
			{
				return (instance) => DependencyObjectExtensions.GetBaseValue(instance, dp);
			}

			{
				// No getter has been found
				var empty = Funcs.CreateMemoized<object>(() =>
				{
					_log.ErrorFormat("The [{0}] substitute property getter does not exist on type [{1}]", property, type);
					return DependencyProperty.UnsetValue;
				});

				return instance => empty();
			}
		}

		private static ValueUnsetterHandler InternalGetValueUnsetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueUnsetter;
			}

			property = SanitizePropertyName(type, property);

			var dp = FindDependencyProperty(type, property) ?? FindAttachedProperty(type, property);

			if (dp != null)
			{
				return (instance) => DependencyObjectExtensions.ClearValue((DependencyObject)instance, dp, precedence);
			}

			var once = Actions.CreateOnce(
				() => _log.ErrorFormat("The property unsetter for [{0}] does not exist on [{1}]", property, type)
			);

			return delegate { once(); };
		}

		private static DependencyProperty FindDependencyProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type ownerType, string property)
			=> DependencyProperty.GetProperty(ownerType, property);

		private static DependencyProperty? FindAttachedProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type, string property)
		{
			var propertyInfo = DependencyPropertyDescriptor.Parse(property);
			if (propertyInfo != null)
			{
				type = propertyInfo.OwnerType;
				property = propertyInfo.Name;
			}

			return type?.GetField(property + "Property")?.GetValue(null) as DependencyProperty;
		}

		private static Type? GetPropertyOrDependencyPropertyType(Type type, string property)
		{
			var propertyType = GetPropertyInfo(type, property, allowPrivateMembers: false);
			if (propertyType != null)
			{
				return propertyType.PropertyType;
			}

			var attachedPropertyGetter = GetAttachedPropertyGetter(type, property);

			if (attachedPropertyGetter != null)
			{
				return attachedPropertyGetter.ReturnType;
			}

			return null;
		}

		private static MethodInfo? GetAttachedPropertyGetter(Type type, string property)
		{
			var propertyInfo = DependencyPropertyDescriptor.Parse(property);

			if (propertyInfo != null)
			{
				type = propertyInfo.OwnerType;
				property = propertyInfo.Name;
			}

			return type
				.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(m => m.Name == "Get" + property && m.GetParameters().Length == 1)
				.FirstOrDefault();
		}

		private static object? ConvertToEnum([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType, object value)
		{
			var valueString = Enum.GetName(enumType, value);

			FieldInfo? defaultValue = null;
			foreach (var field in enumType.GetFields())
			{
				if (field.Name.Equals(valueString, StringComparison.OrdinalIgnoreCase))
				{
					return Enum.Parse(enumType, field.Name);
				}

				var descriptionAttribute = field.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
				if (descriptionAttribute is not null)
				{
					if (descriptionAttribute.Description.Equals(valueString, StringComparison.CurrentCultureIgnoreCase) ||
						descriptionAttribute.Description.Equals(valueString, StringComparison.InvariantCultureIgnoreCase))
					{
						if (Enum.TryParse(enumType, field.Name, ignoreCase: true, out var enumValue))
						{
							return enumValue;
						}
					}

					if (descriptionAttribute.Description == "?")
					{
						defaultValue = field;
					}
				}
			}

			if (defaultValue is not null)
			{
				if (Enum.TryParse(enumType, defaultValue.Name, ignoreCase: true, out var enumValue))
				{
					return enumValue;
				}
			}

			return DependencyProperty.UnsetValue;
		}

		private static object? ConvertPrimitiveToPrimitive(Type type, object value)
		{
			try
			{
				return System.Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
			}
			catch (Exception)
			{
				// This is a temporary fallback solution.
				// The problem is that we don't actually know which culture we must use in advance.
				// Values can come from the xaml (invariant culture) or from a two way binding (current culture).
				// The real solution would be to pass a culture or source when setting a value in a Dependency Property.
				return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types may be removed or not present as part of the normal operations of that method")]
		[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Types may be removed or not present as part of the normal operations of that method")]
		private static object? ConvertUsingTypeDescriptor(Type type, object value)
		{
			var valueTypeConverter = TypeDescriptor.GetConverter(value.GetType());
			if (valueTypeConverter.CanConvertTo(type))
			{
				try
				{
					return valueTypeConverter.ConvertTo(null, CultureInfo.CurrentCulture, value, type);
				}
				catch (Exception)
				{
					return valueTypeConverter.ConvertTo(null, CultureInfo.InvariantCulture, value, type);
				}
			}

			if (type == typeof(float))
			{
				var valueToString = value.ToString();
				if (float.TryParse(valueToString, NumberStyles.Float, CultureInfo.CurrentCulture, out var fValue))
				{
					return fValue;
				}

				if (float.TryParse(valueToString, NumberStyles.Float, CultureInfo.InvariantCulture, out fValue))
				{
					return fValue;
				}
			}
			else if (type == typeof(decimal))
			{
				var valueToString = value.ToString();
				if (decimal.TryParse(valueToString, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var dValue))
				{
					return dValue;
				}
				if (decimal.TryParse(valueToString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out dValue))
				{
					return dValue;
				}
			}

			try
			{
				return TypeDescriptor.GetConverter(type).ConvertFrom(null, CultureInfo.CurrentCulture, value);
			}
			catch (Exception)
			{
				return TypeDescriptor.GetConverter(type).ConvertFrom(null, CultureInfo.InvariantCulture, value);
			}
		}

		internal static object? Convert(Func<Type?>? propertyType, object? value)
		{
			if (value != null && propertyType != null)
			{
				var t = propertyType();

				return Convert(t, value);
			}

			return value;
		}

		internal static object? Convert(
			Type? propertyType,
			object? value)
		{
			if (value != null)
			{
				if (!value.GetType().Is(propertyType))
				{
					if (FastConvert(propertyType, value, out var fastConvertResult))
					{
						return fastConvertResult;
					}
					else if (propertyType is null || propertyType == typeof(object))
					{
						return value;
					}
					else if (propertyType == typeof(string))
					{
						return value?.ToString();
					}
					else if (propertyType.IsEnum)
					{
						return ConvertToEnum(propertyType, value);
					}
					else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType) is { IsPrimitive: true } toTypeUnwrapped && IsPrimitive(value))
					{
						return ConvertPrimitiveToPrimitive(toTypeUnwrapped, value);
					}
					else
					{
						return ConvertUsingTypeDescriptor(propertyType, value);
					}
				}
			}

			return value;
		}

		private static bool IsPrimitive(object value)
		{
			return value.GetType().IsPrimitive
				|| value is string
#if __APPLE_UIKIT__
				// Those are platform primitives provided for 64 bits compatibility
				// with iOS 8.0 and later
				|| value is nfloat
				|| value is nint
				|| value is nuint
#endif
									;
		}

		private static object UnsetValueGetter(object unused)
			=> DependencyProperty.UnsetValue;

		private static void UnsetValueSetter(object unused, object? unused2) { }

		private static void UnsetValueUnsetter(object unused) { }

		/// <summary>
		/// Determines if the type can be provided by the MetadataProvider
		/// </summary>
		/// <remarks>This method needs to be aligned with the symbols query in BindableTypeProvidersSourceGenerator.</remarks>
		private static bool IsValidMetadataProviderType(Type type)
			=> type.IsPublic && (type.IsClass || type.IsValueType);
	}
}
#endif
