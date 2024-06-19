using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Xaml;

namespace XamlGenerationTests;

public sealed partial class ResourceMarkup_Tests : Page
{
	public ResourceMarkup_Tests()
	{
		this.InitializeComponent();
	}
}


public partial class TestControl : Control
{
	#region DependencyProperty: Text

	public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(TestControl),
		new PropertyMetadata(default(string)));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion
}

public partial class TestDepObj : DependencyObject
{
	#region DependencyProperty: Text

	public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(TestDepObj),
		new PropertyMetadata(default(string)));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion
}

[ContentProperty(Name = nameof(Content))]
public partial class TestMatryoshka : DependencyObject // self-nestable element
{
	#region DependencyProperty: Content

	public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(
		nameof(Content),
		typeof(object),
		typeof(TestMatryoshka),
		new PropertyMetadata(default(object)));

	public object Content
	{
		get => (object)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	#endregion

}
