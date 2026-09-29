#nullable enable

using System;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Declares which dependency property of a control is its validation property, i.e. the property whose
/// binding source is inspected for <see cref="System.ComponentModel.INotifyDataErrorInfo"/> errors.
/// </summary>
/// <remarks>
/// This is not WinUI's <see cref="Microsoft.UI.Xaml.Controls.InputPropertyAttribute"/>, which is XAML
/// child-element processing metadata and carries a different meaning.
/// <para>
/// Apply it to the type that owns the input, never to a shared base that has non-validating subclasses:
/// it is inherited, so every subclass of the annotated type validates the same property.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class InputValidationPropertyAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InputValidationPropertyAttribute"/> class.
	/// </summary>
	/// <param name="name">The name of the dependency property to validate.</param>
	public InputValidationPropertyAttribute(string name) => Name = name;

	/// <summary>
	/// The name of the dependency property to validate.
	/// </summary>
	public string Name { get; }
}
