using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents an attribute that indicates which property of a type is the XAML input property.
/// A XAML processor uses this information when processing XAML child elements of XAML representations of the attributed type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public partial class InputPropertyAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the InputPropertyAttribute class.
	/// </summary>
	public InputPropertyAttribute() : base()
	{
	}

	/// <summary>
	/// The property name.
	/// </summary>
	public string Name;
}
