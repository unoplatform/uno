#nullable enable

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A delegate that gets the value of a dependency property, given the provided precedence.
	/// </summary>
	public delegate object PropertyGetterHandler(object instance, DependencyPropertyValuePrecedences? precedence);

	/// <summary>
	/// A delegate that sets the value of a dependency property, given the provided precedence.
	/// </summary>
	public delegate void PropertySetterHandler(object instance, object? value, DependencyPropertyValuePrecedences? precedence);

	/// <summary>
	/// Defines a bindable property
	/// </summary>
	public interface IBindableProperty
	{
		/// <summary>
		/// Gets the type of the property
		/// </summary>
		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		Type PropertyType { get; }

		/// <summary>
		/// Gets the type of the property
		/// </summary>
		DependencyProperty? DependencyProperty { get; }

		/// <summary>
		/// Provides the getter method for this property
		/// </summary>
		PropertyGetterHandler? Getter { get; }

		/// <summary>
		/// Provides the setter method for this property
		/// </summary>
		PropertySetterHandler? Setter { get; }
	}
}
