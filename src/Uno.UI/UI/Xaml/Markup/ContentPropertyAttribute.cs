using System;

namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Defines the property that will be used in Xaml when using implicit content.
/// Indicates which property of a type is the XAML content property.
/// A XAML processor uses this information when processing XAML child elements
/// of XAML representations of the attributed type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed partial class ContentPropertyAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the ContentPropertyAttribute class.
	/// </summary>
	public ContentPropertyAttribute()
	{

	}

	/// <summary>
	/// Gets or sets the Content property name.
	/// </summary>
	public string Name;
}
