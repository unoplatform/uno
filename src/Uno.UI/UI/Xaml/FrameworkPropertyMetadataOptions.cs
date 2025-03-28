using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	[Flags]
	public enum FrameworkPropertyMetadataOptions
	{
		None = 0,

		/// <summary>
		/// Specifies that the property will be inherited for children controls
		/// </summary>
		Inherits = 1 << 0, // 1

		/// <summary>
		/// Specifies that this property's value or values will inherit the DataContext of its or their parent.
		/// </summary>
		ValueInheritsDataContext = 1 << 1, // 2

		/// <summary>
		/// Forces the conversion of a set value to the type of a <see cref="DependencyProperty"/>
		/// </summary>
		AutoConvert = 1 << 2, // 4

		/// <summary>
		/// Prevents this property's value or values from inheriting the DataContext of its or their parent.
		/// </summary>
		/// <remarks>
		/// This is useful for framework properties of type <see cref="DependencyObject"/> for which ValueInheritsDataContext is enabled by default
		/// (cf. <see cref="Default"/>).
		/// </remarks>
		ValueDoesNotInheritDataContext = 1 << 3, // 8

		/// <summary>
		/// Determines if the storage of this property's value should use a <see cref="Uno.UI.DataBinding.ManagedWeakReference"/> backing
		/// </summary>
		WeakStorage = 1 << 4, // 16

		/// <summary>
		/// Automatic opt-in for <see cref="DependencyObjectExtensions.SetParent(object, object)"/> call.
		/// </summary>
		LogicalChild = 1 << 5, // 32

		/// <summary>
		/// Updates to this property affect arrange on <see cref="FrameworkElement"/> <see cref="DependencyObject"/> instances.
		/// </summary>
		AffectsArrange = 1 << 6, // 64

		/// <summary>
		/// Updates to this property affect measure on <see cref="FrameworkElement"/> <see cref="DependencyObject"/> instances.
		/// </summary>
		AffectsMeasure = 1 << 7, // 128

		/// <summary>
		/// Updates to this property affect render on <see cref="FrameworkElement"/> <see cref="DependencyObject"/> instances.
		/// </summary>
		AffectsRender = 1 << 8, // 256

		/// <summary>
		/// Should <see cref="CoerceValueCallback"/> be raised only when value has changed?
		/// </summary>
		CoerceOnlyWhenChanged = 1 << 9, // 512

		/// <summary>
		/// Normally, when a property is updated and the coerced value is equal to
		/// the value being updated to, the coerced value is thrown away. <seealso cref="DependencyObjectStore.ApplyCoercion"/>
		/// This property is added specifically for Control.IsEnabledProperty in order
		/// to override this behaviour.
		/// </summary>
		KeepCoercedWhenEquals = 1 << 10, // 1024

		/// <summary>
		/// The default options set when creating a <see cref="FrameworkPropertyMetadata"/>.
		/// </summary>
		/// <remarks>
		/// By default all DP declared by the framework will inherit the DataContext while DP declared by application won't.
		/// </remarks>
		Default = ValueInheritsDataContext,
	}
}
