using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Windows.UI.Xaml;
using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Dispatching;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
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
	[DebuggerDisplay("Name={Name}, Type={Type.FullName}, Owner={OwnerType.FullName}")]
	public sealed partial class DependencyProperty
	{
		private readonly static DependencyPropertyRegistry _registry = new DependencyPropertyRegistry();

		private readonly static TypeToPropertiesDictionary _getPropertiesForType = new TypeToPropertiesDictionary();
		private readonly static NameToPropertyDictionary _getPropertyCache = new NameToPropertyDictionary();

		/// <summary>
		/// A static <see cref="PropertyCacheEntry"/> used for lookups and avoid creating new instances. This assumes that uses are non-reentrant.
		/// </summary>
		private readonly static PropertyCacheEntry _searchPropertyCacheEntry = new();


		private readonly static FrameworkPropertiesForTypeDictionary _getFrameworkPropertiesForType = new FrameworkPropertiesForTypeDictionary();

		private readonly PropertyMetadata _ownerTypeMetadata; // For perf consideration, we keep direct ref the metadata for the owner type
		private readonly PropertyMetadataDictionary _metadata = new PropertyMetadataDictionary();

		private readonly Flags _flags;
		private string _name;
		private Type _propertyType;
		private Type _ownerType;
		private readonly int _uniqueId;
		private object _fallbackDefaultValue;

		private static int _globalId = -1;

		private DependencyProperty(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata, bool attached)
		{
			_name = name;
			_propertyType = propertyType;
			_ownerType = ownerType;

			_flags |= attached ? Flags.IsAttached : Flags.None;
			_flags |= typeof(DependencyObjectCollection).IsAssignableFrom(propertyType) ? Flags.IsDependencyObjectCollection : Flags.None;
			_flags |= GetIsTypeNullable(propertyType) ? Flags.IsTypeNullable : Flags.None;
			_flags |= (defaultMetadata as FrameworkPropertyMetadata)?.Options.HasWeakStorage() is true ? Flags.HasWeakStorage : Flags.None;
			_flags |= ownerType.Assembly.Equals(typeof(DependencyProperty).Assembly) ? Flags.IsUnoType : Flags.None;

			if (ownerType == typeof(FrameworkElement))
			{
				if (name is
					nameof(FrameworkElement.MaxHeight) or
					nameof(FrameworkElement.MinHeight) or
					nameof(FrameworkElement.MinWidth) or
					nameof(FrameworkElement.MaxWidth))
				{
					_flags |= Flags.ValidateNotNegativeAndNotNaN;
				}
			}

			_uniqueId = Interlocked.Increment(ref _globalId);

			_ownerTypeMetadata = defaultMetadata ?? new FrameworkPropertyMetadata(null);
			_metadata.Add(_ownerType, _ownerTypeMetadata);
		}

		// This is our equivalent of WinUI's ValidateXXX methods in PropertySystem.cpp, e.g, CDependencyObject::ValidateFloatValue
		internal void ValidateValue(object value)
		{
			if ((_flags & Flags.ValidateNotNegativeAndNotNaN) != 0)
			{
				if (value is double doubleValue)
				{
					//negative values and NaN are not allowed for these properties
					if (double.IsNaN(doubleValue) || doubleValue < 0)
					{
						throw new ArgumentException($"Property '{_name}' cannot be set to {doubleValue}. It must not be NaN or negative.");
					}
				}
			}
		}

		/// <summary>
		/// Provides a unique identifier for the dependency property lookup
		/// </summary>
		internal int UniqueId => _uniqueId;

		/// <summary>
		/// Determines if the property storage should be backed by a <see cref="Uno.UI.DataBinding.ManagedWeakReference"/>
		/// </summary>
		internal bool HasWeakStorage
			=> (_flags & Flags.HasWeakStorage) != 0;

		/// <summary>
		/// Determines if the property type inherits from <see cref="DependencyObjectCollection"/>
		/// </summary>
		internal bool IsDependencyObjectCollection
			=> (_flags & Flags.IsDependencyObjectCollection) != 0;

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
		/// Registers a dependency property on the specified <paramref name="ownerType"/>.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="propertyType">The type of the property</param>
		/// <param name="ownerType">The owner type of the property</param>
		/// <param name="typeMetadata">The metadata to use when creating the property</param>
		/// <returns>A dependency property instance</returns>
		/// <exception cref="InvalidOperationException">A property with the same name has already been declared for the ownerType</exception>
		/// <remarks>
		/// This method is to ensure that all uno controls defined dependency properties are using <see cref="FrameworkPropertyMetadata"/>.
		/// This is achieved by banning the other public overload in Uno.UI directory.
		/// </remarks>
		internal static DependencyProperty Register(string name, Type propertyType, Type ownerType, FrameworkPropertyMetadata typeMetadata)
#pragma warning disable RS0030 // Do not used banned APIs
			=> Register(name, propertyType, ownerType, (PropertyMetadata)typeMetadata);
#pragma warning restore RS0030 // Do not used banned APIs

		/// <summary>
		/// Registers a attachable dependency property on the specified <paramref name="ownerType"/>.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="propertyType">The type of the property</param>
		/// <param name="ownerType">The owner type of the property</param>
		/// <param name="defaultMetadata">The metadata to use when creating the property</param>
		/// <returns>A dependency property instance</returns>
		/// <exception cref="InvalidOperationException">A property with the same name has already been declared for the ownerType</exception>
		public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata)
		{
			var newProperty = new DependencyProperty(name, propertyType, ownerType, defaultMetadata, attached: true);

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
		/// <remarks>
		/// This method is to ensure that all uno controls defined dependency properties are using <see cref="FrameworkPropertyMetadata"/>.
		/// This is achieved by banning the other public overload in Uno.UI directory.
		/// </remarks>
		internal static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, FrameworkPropertyMetadata typeMetadata)
#pragma warning disable RS0030 // Do not used banned APIs
			=> RegisterAttached(name, propertyType, ownerType, (PropertyMetadata)typeMetadata);
#pragma warning restore RS0030 // Do not used banned APIs

		/// <summary>
		/// A cached value of the hash code, which can only be defined once
		/// in the entire lifetime of the application.
		/// </summary>
		internal int CachedHashCode
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _uniqueId;
		}

		/// <summary>
		/// Specifies a static value that is used by the dependency property system rather than null to indicate that
		/// the property exists, but does not have its value set by the dependency property system.
		/// </summary>
		public static object UnsetValue { get; } = Windows.UI.Xaml.UnsetValue.Instance;

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
#if !UNO_REFERENCE_API
					// Perf: On generic API the Panel.Children are UIElement, so avoid costly check
					// This check must be removed when Panel.Children will support only
					// UIElement as its elements. See #103492
					&& !forType.Is<_View>()
#endif
					&& OwnerType != typeof(AttachedDependencyObject)
				)
				{
					throw new ArgumentException($"'{forType}' type must derive from DependencyObject.", nameof(forType));
				}

				var baseType = forType.IsSubclassOf(_ownerType)
					? forType.BaseType
					: _ownerType;

				ForceInitializeTypeConstructor(forType);

				metadata = _metadata.FindOrCreate(forType, baseType, this);
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
			=> (_flags & Flags.IsTypeNullable) != 0;

		internal object GetFallbackDefaultValue()
			=> _fallbackDefaultValue != null ? _fallbackDefaultValue : _fallbackDefaultValue = Activator.CreateInstance(Type);

		internal string Name
		{
			get { return _name; }
		}

		/// <summary>
		/// Determines if the property is an attached property
		/// </summary>
		internal bool IsAttached
			=> (_flags & Flags.IsAttached) != 0;

		/// <summary>
		/// Determines if the owner type is declared by Uno.UI
		/// </summary>
		internal bool IsUnoType
			=> (_flags & Flags.IsUnoType) != 0;

		/// <summary>
		/// Get the specified dependency property on the specified owner type.
		/// </summary>
		/// <param name="type">The type that owns the dependency property</param>
		/// <param name="name">The name of the dependency property</param>
		/// <returns>A <see cref="DependencyProperty"/> instance, otherwise null it not found.</returns>
		internal static DependencyProperty GetProperty(Type type, string name)
		{
#if !__WASM__
			if (!FeatureConfiguration.DependencyProperty.DisableThreadingCheck && !NativeDispatcher.Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The dependency property system should not be accessed from non UI thread.");
			}
#endif

			_searchPropertyCacheEntry.Update(type, name);

			if (!_getPropertyCache.TryGetValue(_searchPropertyCacheEntry, out var result))
			{
				_getPropertyCache.Add(_searchPropertyCacheEntry.Clone(), result = InternalGetProperty(type, name));
			}

			return result;
		}

		private static void ResetGetPropertyCache(Type ownerType, string name)
		{
			if (_getPropertyCache.Count != 0)
			{
				_searchPropertyCacheEntry.Update(ownerType, name);

				_getPropertyCache.Remove(_searchPropertyCacheEntry);
			}
		}

		private static DependencyProperty InternalGetProperty(Type type, string name)
		{
			ForceInitializeTypeConstructor(type);

			var propertyInfo = DependencyPropertyDescriptor.Parse(name);

			if (propertyInfo != null)
			{
				type = propertyInfo.OwnerType;
				name = propertyInfo.Name;
			}

			do
			{
				if (_registry.TryGetValue(type, name, out var result))
				{
					return result;
				}

				// Dependency properties are inherited
				type = type.BaseType;
			}
			while (type != typeof(object) && type != null);

			return null;
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
		/// Clears all the property registrations, when used in unit tests.
		/// </summary>
		internal static void ClearRegistry()
		{
			_registry.Clear();
			_getPropertiesForType.Clear();
			_getPropertyCache.Clear();
			_getFrameworkPropertiesForType.Clear();
		}

		private static void RegisterProperty(Type ownerType, string name, DependencyProperty newProperty)
		{
			ResetGetPropertyCache(ownerType, name);

			_registry.Add(ownerType, name, newProperty);
		}

		private static DependencyProperty[] InternalGetPropertiesForType(Type type)
		{
			ForceInitializeTypeConstructor(type);

			var results = new List<DependencyProperty>();

			do
			{
				_registry.AppendPropertiesForType(type, results);

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

		public override int GetHashCode() => CachedHashCode;

		[Flags]
		private enum Flags
		{
			/// <summary>
			/// No flag
			/// </summary>
			None = 0,

			/// <summary>
			/// Set when the property is an attached property
			/// </summary>
			IsAttached = (1 << 0),

			/// <summary>
			/// Set when the <see cref="_propertyType"/> is nullable
			/// </summary>
			IsTypeNullable = (1 << 1),

			/// <summary>
			/// Set when the <see cref="_propertyType"/> is a <see cref="DependencyObjectCollection"/>
			/// </summary>
			IsDependencyObjectCollection = (1 << 2),

			/// <summary>
			/// Set when the internal storage for the property is using weak references
			/// </summary>
			HasWeakStorage = (1 << 3),

			/// <summary>
			/// Set when the property type is declared in Uno.UI
			/// </summary>
			IsUnoType = (1 << 4),

			ValidateNotNegativeAndNotNaN = (1 << 5),
		}
	}
}
