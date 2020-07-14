using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Uno.Extensions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using System.Threading;

#if XAMARIN_ANDROID
using _View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using _View = UIKit.UIView;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines a dependency property for a <see cref="DependencyObject"/>.
	/// </summary>
	/// <remarks>The properties are attached to the <see cref="IDependencyObject"/> marker interface.</remarks>
	[DebuggerDisplay("Name={Name}, Type={Type.FullName}, Owner={OwnerType.FullName}, DefaultValue={Metadata.DefaultValue}")]
	public sealed partial class DependencyProperty
	{
		private static Dictionary<Type, Dictionary<string, DependencyProperty>> _registry
			= new Dictionary<Type, Dictionary<string, DependencyProperty>>(Uno.Core.Comparison.FastTypeComparer.Default);

		private readonly static Dictionary<Type, DependencyProperty[]> _getPropertiesForType = new Dictionary<Type, DependencyProperty[]>(Uno.Core.Comparison.FastTypeComparer.Default);
		private readonly static Dictionary<PropertyCacheEntry, DependencyProperty> _getPropertyCache = new Dictionary<PropertyCacheEntry, DependencyProperty>(PropertyCacheEntry.DefaultComparer);

		private readonly static Dictionary<CachedTuple<Type, FrameworkPropertyMetadataOptions>, DependencyProperty[]> _getFrameworkPropertiesForType = new Dictionary<CachedTuple<Type, FrameworkPropertyMetadataOptions>, DependencyProperty[]>(CachedTuple<Type, FrameworkPropertyMetadataOptions>.Comparer);
		private readonly static Dictionary<Type, DependencyProperty[]> _getDependencyObjectPropertiesForType = new Dictionary<Type, DependencyProperty[]>(Uno.Core.Comparison.FastTypeComparer.Default);

		private readonly PropertyMetadata _ownerTypeMetadata; // For perf consideration, we keep direct ref the metadata for the owner type
		private readonly Dictionary<Type, PropertyMetadata> _metadata = new Dictionary<Type, PropertyMetadata>(Uno.Core.Comparison.FastTypeComparer.Default);

		private string _name;
		private Type _propertyType;
		private Type _ownerType;
		private readonly bool _isAttached;
		private readonly bool _isTypeNullable;
		private readonly int _uniqueId;
		private readonly bool _hasAutoDataContextInherit;
		private readonly bool _isDependencyObjectCollection;
		private readonly bool _hasWeakStorage;
		private object _fallbackDefaultValue;

		private static int _globalId;

		private DependencyProperty(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, bool attached)
		{
			_name = name;
			_propertyType = propertyType;
			_ownerType = attached || IsTypeDependencyObject(ownerType) ? ownerType : typeof(_View);
			_isAttached = attached;
			_hasAutoDataContextInherit = CanAutoInheritDataContext(propertyType);
			_isDependencyObjectCollection = typeof(DependencyObjectCollection).IsAssignableFrom(propertyType);
			_isTypeNullable = propertyType.IsNullableCached();
			_uniqueId = Interlocked.Increment(ref _globalId);
			_hasWeakStorage = (defaultMetadata as FrameworkPropertyMetadata)?.Options.HasWeakStorage() ?? false;

			_ownerTypeMetadata = defaultMetadata ?? new FrameworkPropertyMetadata(null);
			_metadata.Add(_ownerType, _ownerTypeMetadata);

			// Improve the performance of the hash code by
			CachedHashCode = _name.GetHashCode() ^ ownerType.GetHashCode();
		}

		/// <summary>
		/// Determines of the provided property type can inherit a DataContext.
		/// </summary>
		/// <remarks>
		/// The types checked by this method are generally types that do not contain any
		/// dependency property, or that cannot be used in a data binding context. Those types
		/// are forcibly ignored, even if they inherit from <see cref="DependencyObject"/>.
		/// </remarks>
		private static bool CanAutoInheritDataContext(Type propertyType)
			=> typeof(DependencyObject).IsAssignableFrom(propertyType)
			&& propertyType != typeof(Style)
			&& !typeof(FrameworkTemplate).IsAssignableFrom(propertyType);

		/// <summary>
		/// Provides a unique identifier for the dependency property lookup
		/// </summary>
		internal int UniqueId => _uniqueId;

		/// <summary>
		/// Determines if the property storage should be backed by a <see cref="Uno.UI.DataBinding.ManagedWeakReference"/>
		/// </summary>
		internal bool HasWeakStorage => _hasWeakStorage;

		/// <summary>
		/// Determines if the property type inherits from <see cref="DependencyObjectCollection"/>
		/// </summary>
		internal bool IsDependencyObjectCollection => _isDependencyObjectCollection;

		/// <summary>
		/// Registers a dependency property on the specified <paramref name="ownerType"/>.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="propertyType">The type of the property</param>
		/// <param name="ownerType">The owner type of the property</param>
		/// <param name="typeMetadata">The metadata to use when creating the property</param>
		/// <returns>A dependency property instance</returns>
		/// <exception cref="InvalidOperationException">A property with the same name has already been declared for the ownerType</exception>
		public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata)
		{
			var newProperty = new DependencyProperty(name, propertyType, ownerType, typeMetadata, attached: false);

			try
			{
				RegisterProperty(ownerType, name, newProperty);
			}
			catch (ArgumentException e)
			{
				throw new InvalidOperationException(
					"The dependency property {0} already exists in type {1}".InvariantCultureFormat(name, ownerType),
					e
				);
			}

			return newProperty;
		}

		/// <summary>
		/// Registers a attachable dependency property on the specified <paramref name="ownerType"/>.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="propertyType">The type of the property</param>
		/// <param name="ownerType">The owner type of the property</param>
		/// <param name="typeMetadata">The metadata to use when creating the property</param>
		/// <returns>A dependency property instance</returns>
		/// <exception cref="InvalidOperationException">A property with the same name has already been declared for the ownerType</exception>
		public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata)
		{
			var newProperty = new DependencyProperty(name, propertyType, ownerType, typeMetadata, attached: true);

			try
			{
				RegisterProperty(ownerType, name, newProperty);
			}
			catch (ArgumentException e)
			{
				throw new InvalidOperationException(
					"The dependency property {0} already exists in type {1}".InvariantCultureFormat(name, ownerType),
					e
				);
			}

			return newProperty;
		}

		/// <summary>
		/// A cached value of the hash code, which can only be defined once
		/// in the entire lifetime of the application.
		/// </summary>
		internal int CachedHashCode
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Specifies a static value that is used by the dependency property system rather than null to indicate that
		/// the property exists, but does not have its value set by the dependency property system.
		/// </summary>
		public static readonly object UnsetValue = Windows.UI.Xaml.UnsetValue.Instance;

		/// <summary>
		/// Retrieves the property metadata value for the dependency property as registered to a type. You specify the type you want info from as a type reference.
		/// </summary>
		/// <param name="forType">The name of the specific type from which to retrieve the dependency property metadata, as a type reference</param>
		/// <returns>A property metadata object.</returns>
		public PropertyMetadata GetMetadata(Type forType)
		{
			if (forType == _ownerType)
			{
				return _ownerTypeMetadata;
			}

			PropertyMetadata metadata = null;
			if (!_metadata.TryGetValue(forType, out metadata))
			{
				if (
					!IsTypeDependencyObject(forType)
#if !__WASM__ // Perf: On WASM the Panel.Children are UIElement, so avoid costly check
					// This check must be removed when Panel.Children will support only
					// UIElement as its elements. See #103492
					&& !forType.Is<_View>()
#endif
				)
				{
					throw new ArgumentException($"'{forType}' type must derive from DependencyObject.", nameof(forType));
				}

				var baseType = forType.IsSubclassOf(_ownerType)
					? forType.BaseType
					: _ownerType;

				ForceInitializeTypeConstructor(forType);

				metadata = _metadata.FindOrCreate(forType, () => GetMetadata(baseType));
			}

			return metadata;
		}

		private static bool IsTypeDependencyObject(Type forType) => typeof(DependencyObject).IsAssignableFrom(forType);

		internal void OverrideMetadata(Type forType, PropertyMetadata typeMetadata)
		{
			ForceInitializeTypeConstructor(forType);

			if (forType == null)
			{
				throw new ArgumentNullException(nameof(forType), "Value cannot be null.");
			}

			if (typeMetadata == null)
			{
				throw new ArgumentNullException(nameof(typeMetadata));
			}

			if (!(typeof(DependencyObject).IsAssignableFrom(forType)))
			{
				throw new ArgumentException($"'{forType}' type must derive from DependencyObject.", nameof(forType));
			}

			if (_metadata.ContainsKey(forType))
			{
				throw new ArgumentException($"PropertyMetadata is already registered for type '{forType}'.", nameof(forType));
			}

			if (_metadata.ContainsValue(typeMetadata))
			{
				throw new ArgumentException($"Metadata is already associated with a type and property. A new one must be created.", nameof(typeMetadata));
			}

			var baseMetadata = GetMetadata(forType.BaseType);
			if (!baseMetadata.GetType().IsAssignableFrom(typeMetadata.GetType()))
			{
				throw new ArgumentException($"Metadata override and base metadata must be of the same type or derived type.", nameof(typeMetadata));
			}

			typeMetadata.Merge(baseMetadata, this);
			_metadata.Add(forType, typeMetadata);
		}

		internal Type OwnerType
		{
			get { return _ownerType; }
		}

		internal Type Type
		{
			get { return _propertyType; }
		}

		/// <summary>
		/// Determines if the Type of the property is a ValueType
		/// </summary>
		internal bool IsTypeNullable
		{
			get { return _isTypeNullable; }
		}

		internal object GetFallbackDefaultValue()
			=> _fallbackDefaultValue != null ? _fallbackDefaultValue : _fallbackDefaultValue = Activator.CreateInstance(Type);

		/// <summary>
		/// Determines if the Type of the property is a <see cref="DependencyObject"/>
		/// </summary>
		internal bool HasAutoDataContextInherit => _hasAutoDataContextInherit;

		internal string Name
		{
			get { return _name; }
		}

		internal bool IsAttached { get { return _isAttached; } }

		/// <summary>
		/// Get the specified dependency property on the specified owner type.
		/// </summary>
		/// <param name="type">The type that owns the dependency property</param>
		/// <param name="name">The name of the dependency property</param>
		/// <returns>A <see cref="DependencyProperty"/> instance, otherwise null it not found.</returns>
		internal static DependencyProperty GetProperty(Type type, string name)
		{
			DependencyProperty result = null;
			var key = new PropertyCacheEntry(type, name);

			if (!_getPropertyCache.TryGetValue(key, out result))
			{
				_getPropertyCache.Add(key, result = InternalGetProperty(type, name));
			}

			return result;
		}

		private static void ResetGetPropertyCache(Type ownerType, string name)
		{
			if (_getPropertyCache.Count != 0)
			{
				_getPropertyCache.Remove(new PropertyCacheEntry(ownerType, name));
			}
		}

		private static DependencyProperty InternalGetProperty(Type type, string name)
		{
			ForceInitializeTypeConstructor(type);

			DependencyProperty result = null;

			var propertyInfo = DependencyPropertyDescriptor.Parse(name);

			if(propertyInfo != null)
			{
				type = propertyInfo.OwnerType;
				name = propertyInfo.Name;
			}

			do
			{
				var properties = _registry.UnoGetValueOrDefault(type);

				if (properties != null)
				{
					result = properties.UnoGetValueOrDefault(name);

					if (result != null)
					{
						return result;
					}
				}

				// Dependency properties are inherited
				type = type.BaseType;
			}
			while (type != typeof(object) && type != null);

			return result;
		}

		/// <summary>
		/// Gets the dependencies properties for the specified type
		/// </summary>
		/// <param name="type">A dependency object</param>
		/// <returns>An array of Dependency Properties.</returns>
		internal static DependencyProperty[] GetPropertiesForType(Type type)
		{
			DependencyProperty[] result = null;

			if (!_getPropertiesForType.TryGetValue(type, out result))
			{
				_getPropertiesForType.Add(type, result = InternalGetPropertiesForType(type));
			}

			return result;
		}

		/// <summary>
		/// Gets the dependencies properties for the specified type with specific Framework metadata options
		/// </summary>
		/// <param name="type">A dependency object</param>
		/// <param name="options">A set of flags that must be set</param>
		/// <returns>An array of Dependency Properties.</returns>
		internal static DependencyProperty[] GetFrameworkPropertiesForType(Type type, FrameworkPropertyMetadataOptions options)
		{
			DependencyProperty[] result = null;
			var key = CachedTuple.Create(type, options);

			if (!_getFrameworkPropertiesForType.TryGetValue(key, out result))
			{
				_getFrameworkPropertiesForType.Add(key, result = InternalGetFrameworkPropertiesForType(type, options));
			}

			return result;
		}

		/// <summary>
		/// Gets the dependencies properties for the specified type for which the type is a <see cref="DependencyObject"/>.
		/// </summary>
		/// <param name="type">A dependency object</param>
		/// <returns>An array of Dependency Properties.</returns>
		internal static DependencyProperty[] GetDependencyObjectPropertiesForType(Type type)
		{
			DependencyProperty[] result = null;

			if (!_getDependencyObjectPropertiesForType.TryGetValue(type, out result))
			{
				_getDependencyObjectPropertiesForType.Add(type, result = InternalGetDependencyObjectPropertiesForType(type));
			}

			return result;
		}

		/// <summary>
		/// Clears all the property registrations, when used in unit tests.
		/// </summary>
		internal static void ClearRegistry()
		{
			_registry.Clear();
			_getPropertiesForType.Clear();
			_getPropertyCache.Clear();
			_getFrameworkPropertiesForType.Clear();
			_getDependencyObjectPropertiesForType.Clear();
		}

		private static void RegisterProperty(Type ownerType, string name, DependencyProperty newProperty)
		{
			var properties = _registry.FindOrCreate(ownerType, () => new Dictionary<string, DependencyProperty>());

			ResetGetPropertyCache(ownerType, name);

			properties.Add(name, newProperty);
		}

		private static DependencyProperty[] InternalGetPropertiesForType(Type type)
		{
			ForceInitializeTypeConstructor(type);

			var results = new List<DependencyProperty>();

			do
			{
				var properties = _registry.UnoGetValueOrDefault(type);

				if (properties != null)
				{
					results.AddRange(properties.Values);
				}

				// Dependency properties are inherited
				type = type.BaseType;
			}
			while (type != typeof(object) && type != null);

			var array = results.ToArray();

			// Produce a pre-sorted list, aligned with the initial behavior of DependencyPropertyDetailsCollection
			Array.Sort(array, (l, r) => l.UniqueId - r.UniqueId);

			return array;
		}

		/// <summary>
		/// Forces the invocation of the cctor of a type and its base types that may contain DependencyProperty registrations.
		/// </summary>
		/// <remarks>
		/// This is required because of the lazy initialization nature of the classes that do not contain
		/// an explicit type ctor, but contain statically initialized fields. DependencyProperty.Register may not be called as a
		/// result, if none of the DependencyProperty fields are accessed prior to the enumeration of the fields via reflection.
		///
		/// This method avoids requiring controls to include an explicit type constructor to function properly.
		///
		/// See: http://stackoverflow.com/questions/6729841/why-did-the-beforefieldinit-behavior-change-in-net-4
		/// </remarks>
		private static void ForceInitializeTypeConstructor(Type type)
		{
			do
			{
				global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

				type = type.BaseType;
			}
			while (type != null);
		}

		private static DependencyProperty[] InternalGetFrameworkPropertiesForType(Type type, FrameworkPropertyMetadataOptions options)
		{
			var output = new List<DependencyProperty>();

			foreach (var prop in GetPropertiesForType(type))
			{
				var propertyOptions = (prop.GetMetadata(type) as FrameworkPropertyMetadata)?.Options;

				if (propertyOptions != null && (propertyOptions & options) != 0)
				{
					output.Add(prop);
				}
			}

			return output.ToArray();
		}

		private static DependencyProperty[] InternalGetDependencyObjectPropertiesForType(Type type)
		{
			var output = new List<DependencyProperty>();

			var props = GetPropertiesForType(type);

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				var propertyOptions = (prop.GetMetadata(type) as FrameworkPropertyMetadata)?.Options ?? FrameworkPropertyMetadataOptions.None;

				if (
					(
						prop.HasAutoDataContextInherit

						// We must include explicitly marked properties for now, until the
						// metadata generator can provide this information.
						|| propertyOptions.HasValueInheritsDataContext()
					)
					&& !propertyOptions.HasValueDoesNotInheritDataContext()
				)
				{
					output.Add(prop);
				}
			}

			return output.ToArray();
		}

		internal static DependencyProperty Register(string v, Type type1, Type type2, PropertyMetadata propertyMetadata, object updateSourceOnChanged)
		{
			throw new NotImplementedException();
		}
	}
}
