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
/// on <c>Slider</c> rather than <c>RangeBase</c> (which <c>ProgressBar</c> also derives from), and on
/// <c>ComboBox</c> rather than <c>Selector</c> (which <c>FlipView</c> also derives from).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ValidationPropertyAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationPropertyAttribute"/> class.
	/// </summary>
	/// <param name="name">The name of the dependency property to validate.</param>
	public ValidationPropertyAttribute(string name) => Name = name;

	/// <summary>
	/// The name of the dependency property to validate.
	/// </summary>
	public string Name { get; }
}
