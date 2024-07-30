using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	/// <summary>
	/// Gets or sets the style of the TextBox in the ComboBox when the ComboBox is editable.
	/// </summary>
	public Style TextBoxStyle
	{
		get => (Style)GetValue(TextBoxStyleProperty);
		set => SetValue(TextBoxStyleProperty, value);
	}

	/// <summary>
	/// Identifies the TextBoxStyle dependency property.
	/// </summary>
	public static DependencyProperty TextBoxStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(TextBoxStyle),
			typeof(Style),
			typeof(ComboBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the text in the ComboBox.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(ComboBox),
			new FrameworkPropertyMetadata(""));

	/// <summary>
	/// Gets or sets a brush that describes the color of placeholder text.
	/// </summary>
	public Brush PlaceholderForeground
	{
		get => (Brush)GetValue(PlaceholderForegroundProperty);
		set => SetValue(PlaceholderForegroundProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderForeground dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderForeground),
			typeof(Brush),
			typeof(ComboBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that indicates whether the user can
	/// edit text in the text box portion of the ComboBox.
	/// </summary>
	public bool IsEditable
	{
		get => (bool)GetValue(IsEditableProperty);
		set => SetValue(IsEditableProperty, value);
	}

	/// <summary>
	/// Identifies the IsEditable dependency property.
	/// </summary>
	public static DependencyProperty IsEditableProperty { get; } =
		DependencyProperty.Register(
			nameof(IsEditable),
			typeof(bool),
			typeof(ComboBox),
			new FrameworkPropertyMetadata(false));
}
