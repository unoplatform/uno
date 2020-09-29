#nullable enable

#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Uno.Extensions;
using Uno.Logging;
using System.Globalization;
using System.Reflection;
using Uno.UI.DataBinding;
using Uno;
using Windows.UI.Xaml;
using System.Collections;
using Uno.Conversion;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Data;
using System.Dynamic;

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

    internal static partial class BindingPropertyHelper
	{
		private static readonly ILogger _log = typeof(BindingPropertyHelper).Log();

		//
		// Warning: These dictionaries are here in place of memoized Funcs for performance
		//          on Mono 4.0, because of the cost of creating generic delegates and invoking 
		//          those. If this situation changes, we could remove the associated code and 
		//          revert to memoized Funcs.
		//
		private static Dictionary<CachedTuple<Type, String, DependencyPropertyValuePrecedences?, bool>, ValueGetterHandler> _getValueGetter = new Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences?, bool>, ValueGetterHandler>(CachedTuple<Type, String, DependencyPropertyValuePrecedences?, bool>.Comparer);
		private static Dictionary<CachedTuple<Type, string, bool, DependencyPropertyValuePrecedences>, ValueSetterHandler> _getValueSetter = new Dictionary<CachedTuple<Type, string, bool, DependencyPropertyValuePrecedences>, ValueSetterHandler>(CachedTuple<Type, string, bool, DependencyPropertyValuePrecedences>.Comparer);
		private static Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueGetterHandler> _getPrecedenceSpecificValueGetter = new Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueGetterHandler>(CachedTuple<Type, string, DependencyPropertyValuePrecedences>.Comparer);
		private static Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueGetterHandler> _getSubstituteValueGetter = new Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueGetterHandler>(CachedTuple<Type, string, DependencyPropertyValuePrecedences>.Comparer);
		private static Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueUnsetterHandler> _getValueUnsetter = new Dictionary<CachedTuple<Type, string, DependencyPropertyValuePrecedences>, ValueUnsetterHandler>(CachedTuple<Type, string, DependencyPropertyValuePrecedences>.Comparer);
		private static Dictionary<CachedTuple<Type, string>, bool> _isEvent = new Dictionary<CachedTuple<Type, string>, bool>(CachedTuple<Type, string>.Comparer);
		private static Dictionary<CachedTuple<Type, string>, Type?> _getPropertyType = new Dictionary<CachedTuple<Type, string>, Type?>(CachedTuple<Type, string>.Comparer);

		static BindingPropertyHelper()
		{
			MethodInvokerBuilder = DefaultInvokerBuilder;
			Conversion = new DefaultConversionExtensions();
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

		public static DefaultConversionExtensions Conversion { get; private set; }

		public static bool IsEvent(Type type, string property)
		{
			var key = CachedTuple.Create(type, property);

			bool result;

			lock (_isEvent)
			{
				if (!_isEvent.TryGetValue(key, out result))
				{
					_isEvent.Add(key, result = InternalIsEvent(type, property));
				}
			}

			return result;
		}

		public static Type? GetPropertyType(Type type, string property)
		{
			var key = CachedTuple.Create(type, property);

			Type? result;

			lock (_getPropertyType)
			{
				if (!_getPropertyType.TryGetValue(key, out result))
				{
					_getPropertyType.Add(key, result = InternalGetPropertyType(type, property));
				}
			}

			return result;
		}

		internal static ValueGetterHandler GetValueGetter(Type type, string property)
			=> GetValueGetter(type: type, property: property, precedence: null, allowPrivateMembers: false);

		/// <summary>
		/// Gets a <see cref="ValueGetterHandler"/> for a named property
		/// </summary>
		/// <param name="type">The type to search</param>
		/// <param name="property">The name of the property to get</param>
		/// <param name="precedence">The precedence for which the getter will get the value</param>
		/// <param name="allowPrivateMembers">Allows for private members to be included in the search</param>
		internal static ValueGetterHandler GetValueGetter(Type type, string property, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
		{
			var key = CachedTuple.Create(type, property, precedence, allowPrivateMembers);

			ValueGetterHandler result;

			lock (_getValueGetter)
			{
				if (!_getValueGetter.TryGetValue(key, out result))
				{
					_getValueGetter.Add(key, result = InternalGetValueGetter(type, property, precedence, allowPrivateMembers));
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
			var key = CachedTuple.Create(type, property, convert, precedence);

			ValueSetterHandler result;

			lock (_getValueSetter)
			{
				if (!_getValueSetter.TryGetValue(key, out result))
				{
					_getValueSetter.Add(key, result = InternalGetValueSetter(type, property, convert, precedence));
				}
			}

			return result;
		}

		internal static ValueGetterHandler GetPrecedenceSpecificValueGetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			var key = CachedTuple.Create(type, property, precedence);

			ValueGetterHandler result;

			lock (_getPrecedenceSpecificValueGetter)
			{
				if (!_getPrecedenceSpecificValueGetter.TryGetValue(key, out result))
				{
					_getPrecedenceSpecificValueGetter.Add(key, result = InternalGetPrecedenceSpecificValueGetter(type, property, precedence));
				}
			}

			return result;
		}

		internal static ValueGetterHandler GetSubstituteValueGetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			var key = CachedTuple.Create(type, property, precedence);

			ValueGetterHandler result;

			lock (_getSubstituteValueGetter)
			{
				if (!_getSubstituteValueGetter.TryGetValue(key, out result))
				{
					_getSubstituteValueGetter.Add(key, result = InternalGetSubstituteValueGetter(type, property, precedence));
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
			var key = CachedTuple.Create(type, property, precedence);

			ValueUnsetterHandler result;

			lock (_getValueUnsetter)
			{
				if (!_getValueUnsetter.TryGetValue(key, out result))
				{
					_getValueUnsetter.Add(key, result = InternalGetValueUnsetter(type, property, precedence));
				}
			}

			return result;
		}

		private static bool InternalIsEvent(Type type, string property)
		{
#if METRO
			return type.GetTypeInfo().GetDeclaredEvent(property) != null;
#else
			return type.GetEvent(property) != null;
#endif
		}

		private static Type? InternalGetPropertyType(Type type, string property)
		{
			if(type == typeof(UnsetValue))
			{
				return null;
			}

			property = SanitizePropertyName(type, property);

#if PROFILE
			using (Performance.Measure("InternalGetPropertyType"))
#endif
			{
				if (BindableMetadataProvider != null)
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
					if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						_log.Debug($"GetPropertyType({type}, {property}) [Reflection]");
					}

					// Fallback on reflection-based lookup
					if (IsIndexerFormat(property))
					{
						// Fallback on reflection-based lookup
						var indexerInfo = GetPropertyInfo(type, "Item", allowPrivateMembers: false);

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

					var propertyInfo = GetPropertyInfo(type, property, allowPrivateMembers: false);

					if (propertyInfo != null)
					{
						return propertyInfo.PropertyType;
					}

					// Look for an attached property
					var attachedPropertyGetter = GetAttachedPropertyGetter(type, property);

					if (attachedPropertyGetter != null)
					{
						return attachedPropertyGetter.ReturnType;
					}

					if(type.IsPrimitive && property == "Value")
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
		private static PropertyInfo? GetPropertyInfo(Type type, string name, bool allowPrivateMembers)
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

				type = type.BaseType;
			}
			while (type != null);

			return null;
		}

		private static FieldInfo? GetFieldInfo(Type type, string name, bool allowPrivateMembers)
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

				type = type.BaseType;
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
			// - "Windows.UI.Xaml.Controls:UIElement.Opacity" (fully qualified)

			if (property.Contains("."))
			{
				var parts = property
					.Replace(":", ".") // ':' is sometimes used to separate namespace from type
					.Split(new[] { '.' })
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

		private static ValueGetterHandler InternalGetValueGetter(Type type, string property, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueGetter;
			}

			property = SanitizePropertyName(type, property);

			if (IsIndexerFormat(property))
			{
				var indexerString = property.Substring(1, property.Length - 2);

				if (type.IsArray)
				{
					int index;

					if (Int32.TryParse(indexerString, out index))
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

					if (Int32.TryParse(indexerString, out index))
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
				if (BindableMetadataProvider != null)
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
								return instance => indexerMethod(instance, indexerString);
							}
						}
					}
				}

#if PROFILE
				using (Performance.Measure("InternalGetValueGetter.Reflection"))
#endif
				{
					if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						_log.Debug($"GetValueGetter({type}, {property}) [Reflection]");
					}

					var indexerInfo = GetPropertyInfo(type, "Item", allowPrivateMembers);

					if (indexerInfo != null)
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

						return instance => handler(
                            instance, 
                            new object?[] {
                                Convert(() => indexerInfo.GetIndexParameters()[0].ParameterType, indexerString)
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
			if (BindableMetadataProvider != null)
			{
#if PROFILE
				using (Performance.Measure("GetValueGetter.BindableMetadataProvider"))
#endif
				{
					var bindableProperty = BindablePropertyDescriptor.GetPropertByBindableMetadataProvider(type, property);

					if (bindableProperty.OwnerType != null)
					{
						if (bindableProperty != null && bindableProperty.Property.Getter != null)
						{
							if (bindableProperty.Property.DependencyProperty is { } dependencyProperty)
							{
								return instance => instance.GetValue(dependencyProperty, precedence);
							}
							else
							{
								var getter = bindableProperty.Property.Getter;
								return instance => getter(instance, precedence);
							}
						}
					}
				}
			}

#if PROFILE
			using (Performance.Measure("InternalGetValueGetter.Reflection2"))
#endif
			{
				if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					_log.Debug($"GetValueGetter({type}, {property}) [Reflection]");
				}

				var dp = FindDependencyProperty(type, property);

				if (dp != null)
				{
					if (precedence == null)
					{
						return instance => ((DependencyObject)instance).GetValue(dp);
					}
					else
					{
						return instance => ((DependencyObject)instance).GetValue(dp, precedence.Value);
					}
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

					return instance => handler(instance, new object[0]);
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

				if(type == typeof(System.Dynamic.ExpandoObject))
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

				if(type.Is(typeof(System.Dynamic.DynamicObject)))
				{
					return instance =>
					{
						if (
							instance is System.Dynamic.DynamicObject dynamicObject
							&& dynamicObject.TryGetMember(new UnoGetMemberBinder(property, true), out var binderValue)
						)
						{
							return binderValue;
						}

						return null;
					};
				}

				if (
					type.IsPrimitive
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "To be refactored"),
		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "To be refactored")]
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
				var indexerName = property.Substring(1, property.Length - 2);

				// The fastest path uses the generated bindable metadata, which does not require
				// the property info, unless there is an actual conversion to perform.
				// So we just close over the property.
				var indexerInfo = Uno.Funcs.CreateMemoized(() => GetPropertyInfo(type, "Item", allowPrivateMembers: false));
				var indexerType = Uno.Funcs.CreateMemoized(() => indexerInfo()?.PropertyType);


				// Start by using the provider, to avoid reflection
				if (BindableMetadataProvider != null)
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
								return (instance, value) => indexerMethod(instance, indexerName, convertSelector(indexerType, value));
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
					if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

							return (instance, value) => handler(instance, new object?[] { indexerName, convertSelector(indexerType, value) });
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
			if (BindableMetadataProvider != null)
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
							if(bindableProperty.Property.DependencyProperty is { } dependencyProperty)
							{
								return (instance, value) => instance.SetValue(dependencyProperty, convertSelector(() => bindableProperty.Property.PropertyType, value), precedence);
							}

							if (bindableProperty.Property.Setter != null)
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
				if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

				if (type == typeof(System.Dynamic.ExpandoObject))
				{
					return (instance, value) =>
					{
						if (instance is IDictionary<string, object?> source)
						{
							source[property] = value;
						}
					};
				}


				if (type.Is(typeof(System.Dynamic.DynamicObject)))
				{
					return (instance, value) =>
					{
						if (instance is System.Dynamic.DynamicObject dynamicObject)
						{
							dynamicObject.TrySetMember(new UnoSetMemberBinder(property, true), value);
						}
					};
				}

				var once = Actions.CreateOnce(
					() => _log.ErrorFormat("The property setter for [{0}] does not exist on [{1}]", property, type)
				);

				return delegate { once(); };
			}
		}

		private static ValueGetterHandler InternalGetPrecedenceSpecificValueGetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueGetter;
			}

			property = SanitizePropertyName(type, property);

			var dp = FindDependencyProperty(type, property);

			if (dp != null)
			{
				return (instance) => DependencyObjectExtensions.GetPrecedenceSpecificValue((DependencyObject)instance, dp, precedence);
			}

			{
				// No getter has been found
				var empty = Funcs.CreateMemoized<object>(() =>
				{
					_log.ErrorFormat("The [{0}] precedence specific property getter does not exist on type [{1}]", property, type);
					return DependencyProperty.UnsetValue;
				});

				return instance => empty();
			}
		}

		private static ValueGetterHandler InternalGetSubstituteValueGetter(Type type, string property, DependencyPropertyValuePrecedences precedence)
		{
			if (type == typeof(UnsetValue))
			{
				return UnsetValueGetter;
			}

			property = SanitizePropertyName(type, property);

			var dp = FindDependencyProperty(type, property);

			if (dp != null)
			{
				return (instance) => DependencyObjectExtensions.GetValueUnderPrecedence((DependencyObject)instance, dp, precedence);
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
				return _ => { };
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

		private static DependencyProperty FindDependencyProperty(Type ownerType, string property) 
			=> DependencyProperty.GetProperty(ownerType, property);

		private static DependencyProperty? FindAttachedProperty(Type type, string property)
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

		private static MethodInfo GetAttachedPropertyGetter(Type type, string property)
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

		internal static object? Convert(Func<Type?>? propertyType, object? value)
		{
			if (value != null && propertyType != null)
			{
				var t = propertyType();

				if (!value.GetType().Is(t))
				{
					if (FastConvert(t, value, out var fastConvertResult))
					{
						return fastConvertResult;
					}
					else if (t != typeof(object))
					{
						try
						{
							value = Conversion.To(value, t, CultureInfo.CurrentCulture);
						}
						catch (Exception)
						{
							// This is a temporary fallback solution.
							// The problem is that we don't actually know which culture we must use in advance.
							// Values can come from the xaml (invariant culture) or from a two way binding (current culture).
							// The real solution would be to pass a culture or source when setting a value in a Dependency Property.
							value = Conversion.To(value, t, CultureInfo.InvariantCulture);
						}
					}
				}
			}
			return value;
		}

		private static object UnsetValueGetter(object unused)
			=> DependencyProperty.UnsetValue;

		private static void UnsetValueSetter(object unused, object? unused2) { }
	}
}
#endif
