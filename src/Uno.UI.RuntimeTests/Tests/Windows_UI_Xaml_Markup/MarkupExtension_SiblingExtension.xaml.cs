using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class MarkupExtension_SiblingExtension : Page
{
	public MarkupExtension_SiblingExtension()
	{
		this.InitializeComponent();

		// Required for the {Binding BoundValue} usage.
		DataContext = this;
	}

	public string BoundValue => "bound";
}

/// <summary>
/// A regular control (not a markup extension) that has a sibling <see cref="SiblingExtensionControlExtension"/>
/// markup extension declared in the same namespace. Used to validate that the XAML generator/reader does not
/// misidentify the control element as a markup extension (https://github.com/unoplatform/uno/issues/21992).
/// </summary>
public partial class SiblingExtensionControl : Control
{
	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(nameof(Value), typeof(string), typeof(SiblingExtensionControl), new PropertyMetadata(null));
}

public sealed class SiblingExtensionControlExtension : MarkupExtension
{
	protected override object ProvideValue() => "from-extension";
}
