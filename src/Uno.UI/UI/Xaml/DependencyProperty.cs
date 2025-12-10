using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Media;

#if __ANDROID__
using _View = Android.Views.View;
#elif __APPLE_UIKIT__
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Defines a dependency property for a <see cref="DependencyObject"/>.
	/// </summary>
	/// <remarks>The properties are attached to the <see cref="IDependencyObject"/> marker interface.</remarks>
	[DebuggerDisplay("Name={Name}, Type={Type.FullName}, Owner={OwnerType.FullName}")]
	public sealed partial class DependencyProperty
	{
		private readonly static DependencyPropertyRegistry _registry = DependencyPropertyRegistry.Instance;

		private readonly static NameToPropertyDictionary _getPropertyCache = new NameToPropertyDictionary();
		private static object DefaultThemeAnimationDurationBox = new Duration(FeatureConfiguration.ThemeAnimation.DefaultThemeAnimationDuration);

		/// <summary>
		/// A static <see cref="PropertyCacheEntry"/> used for lookups and avoid creating new instances. This assumes that uses are non-reentrant.
		/// </summary>
		private readonly static PropertyCacheEntry _searchPropertyCacheEntry = new();


		private readonly static FrameworkPropertiesForTypeDictionary _getInheritedPropertiesForType = new FrameworkPropertiesForTypeDictionary();

		private readonly PropertyMetadata _ownerTypeMetadata; // For perf consideration, we keep direct ref the metadata for the owner type

		private readonly Flags _flags;
		private string _name;
		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		private Type _propertyType;
		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		private Type _ownerType;
		private readonly int _uniqueId;

		private static int _globalId = -1;

		private DependencyProperty(
			string name,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
			Type propertyType,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
			Type ownerType,
			PropertyMetadata defaultMetadata,
			bool attached)
		{
			_name = name;
			_propertyType = propertyType;
			_ownerType = ownerType;

			_flags |= attached ? Flags.IsAttached : Flags.None;
			_flags |= typeof(DependencyObjectCollection).IsAssignableFrom(propertyType) ? Flags.IsDependencyObjectCollection : Flags.None;
			_flags |= GetIsTypeNullable(propertyType) ? Flags.IsTypeNullable : Flags.None;
			if (defaultMetadata is FrameworkPropertyMetadata frameworkMetadata)
			{
				_flags |= frameworkMetadata.Options.HasWeakStorage() ? Flags.HasWeakStorage : Flags.None;
				_flags |= frameworkMetadata.Options.HasInherits() ? Flags.IsInherited : Flags.None;
			}

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

		internal PropertyMetadata Metadata => _ownerTypeMetadata;

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
		public static DependencyProperty Register(
			string name,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type propertyType,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type ownerType,
			PropertyMetadata typeMetadata)
		{
			typeMetadata = FixMetadataIfNeeded(propertyType, typeMetadata);

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

		private static PropertyMetadata FixMetadataIfNeeded(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type propertyType,
			PropertyMetadata metadata)
		{
			if (metadata is null)
			{
				var defaultValue = propertyType.IsNullable() ? null : RuntimeHelpers.GetUninitializedObject(propertyType);
				return new PropertyMetadata(defaultValue);
			}
			else if (!propertyType.IsNullable() && metadata.DefaultValue is null)
			{
				return metadata.CloneWithOverwrittenDefaultValue(RuntimeHelpers.GetUninitializedObject(propertyType));
			}

			return metadata;
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
		internal static DependencyProperty Register(
			string name,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type propertyType,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type ownerType,
			FrameworkPropertyMetadata typeMetadata)
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
		public static DependencyProperty RegisterAttached(
			string name,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type propertyType,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)] Type ownerType,
			PropertyMetadata defaultMetadata)
		{
			defaultMetadata = FixMetadataIfNeeded(propertyType, defaultMetadata);

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
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types manipulated here have been marked earlier")]
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
		public static object UnsetValue { get; } = Microsoft.UI.Xaml.UnsetValue.Instance;

		/// <summary>
		/// Retrieves the property metadata value for the dependency property as registered to a type. You specify the type you want info from as a type reference.
		/// </summary>
		/// <param name="forType">The name of the specific type from which to retrieve the dependency property metadata, as a type reference</param>
		/// <returns>A property metadata object.</returns>
		public PropertyMetadata GetMetadata(Type forType)
		{
			// NOTE: WinUI always allocates a and returns fresh PropertyMetadata and only sets DefaultValue on the returned instance, and nothing else.
			// For now, we have internal usages that requires the full info to be returned, so we return the complete info and even the same instance.

			if (forType == _ownerType)
			{
				return _ownerTypeMetadata;
			}

			var defaultValueForType = GetDefaultValue(null, forType);

			if (!DependencyObjectStore.AreDifferent(_ownerTypeMetadata.DefaultValue, defaultValueForType))
			{
				return _ownerTypeMetadata;
			}

			return _ownerTypeMetadata.CloneWithOverwrittenDefaultValue(defaultValueForType);
		}

		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		internal Type OwnerType
		{
			get { return _ownerType; }
		}

		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		internal Type Type
		{
			get { return _propertyType; }
		}

		/// <summary>
		/// Determines if the Type of the property is a ValueType
		/// </summary>
		internal bool IsTypeNullable
			=> (_flags & Flags.IsTypeNullable) != 0;

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
		/// Determines if the property is an inherited property
		/// </summary>
		internal bool IsInherited
			=> (_flags & Flags.IsInherited) != 0;

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
			if (!FeatureConfiguration.DependencyProperty.DisableThreadingCheck && !NativeDispatcher.Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The dependency property system should not be accessed from non UI thread.");
			}

			_searchPropertyCacheEntry.Update(type, name);

			if (!_getPropertyCache.TryGetValue(_searchPropertyCacheEntry, out var result))
			{
				_getPropertyCache.Add(_searchPropertyCacheEntry.Clone(), result = InternalGetProperty(type, name));
			}

			return result;
		}

		internal static DependencyProperty GetProperty(string type, string name)
		{
			if (_registry.TryGetValueByName(type, name, out var result))
			{
				return result;
			}

			return null;
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
		/// Gets the dependencies properties for the specified type with specific Framework metadata options
		/// </summary>
		/// <param name="type">A dependency object</param>
		/// <param name="options">A set of flags that must be set</param>
		/// <returns>An array of Dependency Properties.</returns>
		internal static DependencyProperty[] GetInheritedPropertiesForType(Type type)
		{
			DependencyProperty[] result = null;

			if (!_getInheritedPropertiesForType.TryGetValue(type, out result))
			{
				_getInheritedPropertiesForType.Add(type, result = InternalGetInheritedPropertiesForType(type));
			}

			return result;
		}

		private static void RegisterProperty(Type ownerType, string name, DependencyProperty newProperty)
		{
			ResetGetPropertyCache(ownerType, name);

			_registry.Add(ownerType, name, newProperty);
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
		[UnconditionalSuppressMessage("Trimming", "IL2059", Justification = "Normal flow of operations")]
		internal static void ForceInitializeTypeConstructor(Type type)
		{
			do
			{
				global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

				type = type.BaseType;
			}
			while (type != null);
		}

		private static DependencyProperty[] InternalGetInheritedPropertiesForType(Type type)
		{
			ForceInitializeTypeConstructor(type);

			var results = new List<DependencyProperty>();

			do
			{
				_registry.AppendInheritedPropertiesForType(type, results);

				// Dependency properties are inherited
				type = type.BaseType;
			}
			while (type != typeof(object) && type != null);

			var array = results.ToArray();

			// Produce a pre-sorted list, aligned with the initial behavior of DependencyPropertyDetailsCollection
			Array.Sort(array, (l, r) => l.UniqueId - r.UniqueId);

			return array;
		}

		private bool TryGetDefaultInheritedPropertyValue(out object defaultValue)
		{
			if (this == TextElement.ForegroundProperty ||
				this == TextBlock.ForegroundProperty ||
				this == Control.ForegroundProperty ||
				this == RichTextBlock.ForegroundProperty ||
				this == ContentPresenter.ForegroundProperty ||
				this == IconElement.ForegroundProperty)
			{
				defaultValue = DefaultBrushes.TextForegroundBrush;
				return true;
			}

			defaultValue = null;
			return false;
		}

		internal object GetDefaultValue(DependencyObject referenceObject, Type forType)
		{
			if ((referenceObject as UIElement)?.GetDefaultValue2(this, out var defaultValue) == true)
			{
				return defaultValue;
			}

			if (IsInherited && TryGetDefaultInheritedPropertyValue(out var value))
			{
				return value;
			}

			if (this == FrameworkElement.FocusVisualPrimaryBrushProperty &&
				ResourceResolver.TryStaticRetrieval("SystemControlFocusVisualPrimaryBrush", null, out var primaryBrush))
			{
				return primaryBrush;
			}
			else if (this == FrameworkElement.FocusVisualSecondaryBrushProperty &&
				ResourceResolver.TryStaticRetrieval("SystemControlFocusVisualSecondaryBrush", null, out var secondaryBrush))
			{
				return secondaryBrush;
			}

			if (this == UIElement.IsTabStopProperty)
			{
				if (forType.IsAssignableTo(typeof(UserControl)))
				{
					return Boxes.BooleanBoxes.BoxedFalse;
				}
				else if (forType.IsAssignableTo(typeof(Control)))
				{
					return Boxes.BooleanBoxes.BoxedTrue;
				}

				return Boxes.BooleanBoxes.BoxedFalse;
			}

			if (this == Shape.StretchProperty)
			{
				if (forType == typeof(Rectangle) || forType == typeof(Ellipse))
				{
					return Boxes.StretchBoxes.Fill;
				}
			}

			if (this == Timeline.DurationProperty)
			{
				if (referenceObject is FadeInThemeAnimation or FadeOutThemeAnimation)
				{
					if (((Duration)DefaultThemeAnimationDurationBox).TimeSpan == FeatureConfiguration.ThemeAnimation.DefaultThemeAnimationDuration)
					{
						// Our box is valid, so we can re-use it.
						return DefaultThemeAnimationDurationBox;
					}

					// Rare code path, it will be hit only once per change in the feature configuration.
					// The cached box is not valid. So we create a new box update the cached box.
					DefaultThemeAnimationDurationBox = new Duration(FeatureConfiguration.ThemeAnimation.DefaultThemeAnimationDuration);
					return DefaultThemeAnimationDurationBox;
				}
			}

			if (this == Storyboard.TargetPropertyProperty)
			{
				if (referenceObject is FadeInThemeAnimation or FadeOutThemeAnimation)
				{
					return "Opacity";
				}
			}

			if (this == DoubleAnimation.ToProperty)
			{
				if (referenceObject is FadeInThemeAnimation)
				{
					return Uno.UI.Helpers.Boxes.NullableDoubleBoxes.One;
				}
				else if (referenceObject is FadeOutThemeAnimation)
				{
					return Uno.UI.Helpers.Boxes.NullableDoubleBoxes.Zero;
				}
			}

			if (_ownerTypeMetadata.CreateDefaultValueCallback != null)
			{
				return _ownerTypeMetadata.CreateDefaultValueCallback();
			}

			return _ownerTypeMetadata.DefaultValue;
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

			/// <summary>
			/// Set when the property is an inherited property
			/// </summary>
			IsInherited = (1 << 6),
		}
	}
}
