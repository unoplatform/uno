#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.ViewLibrary;

/// <summary>
/// A control that participates in input validation from outside Uno.UI, so it sees only the public surface.
/// </summary>
/// <remarks>
/// This project is deliberately absent from Uno.UI's InternalsVisibleTo list and does not reference
/// Uno.UI.Extras, which makes the participation contract a compile-time one: declaring the input is all a
/// third-party control needs, and the attached properties do the rest.
/// </remarks>
[InputValidationProperty(nameof(Text))]
public partial class MyValidatingControl : Control
{
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(MyValidatingControl),
			new PropertyMetadata(string.Empty));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
}

/// <summary>
/// Carries no attribute, so its input has to be registered in
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/> — the route left for a
/// control whose source cannot be annotated.
/// </summary>
public partial class MyMappedValidatingControl : Control
{
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(
			nameof(Value),
			typeof(string),
			typeof(MyMappedValidatingControl),
			new PropertyMetadata(string.Empty));

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
}
