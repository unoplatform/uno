using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class TextBox
{
	/// <summary>
	/// Gets or sets the valu indicting how the Enter 
	/// key should appear on the virtual keyboard.
	/// </summary>
	public EnterKeyHint EnterKeyHint
	{
		get => (EnterKeyHint)GetValue(EnterKeyHintProperty);
		set => SetValue(EnterKeyHintProperty, value);
	}

	/// <summary>
	/// Indentifies the EnterKeyHint dependency property.
	/// </summary>
	public static DependencyProperty EnterKeyHintProperty { get; } =
		DependencyProperty.Register(
			nameof(EnterKeyHint),
			typeof(EnterKeyHint),
			typeof(TextBox),
			new FrameworkPropertyMetadata(
				EnterKeyHint.Default,
				(s, e) => ((TextBox)s)?.OnEnterKeyHintChangedPartial((EnterKeyHint)e.NewValue)
			));

	partial void OnEnterKeyHintChangedPartial(EnterKeyHint enterKeyHint);
}
