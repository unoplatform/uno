using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Documents;

[ContentProperty(Name = nameof(Inlines))]
public partial class Paragraph : Block
{
	public double TextIndent
	{
		get => (double)GetValue(TextIndentProperty);
		set => SetValue(TextIndentProperty, value);
	}

	public InlineCollection Inlines { get; }

	public static global::Windows.UI.Xaml.DependencyProperty TextIndentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(TextIndent),
			propertyType: typeof(double),
			ownerType: typeof(global::Windows.UI.Xaml.Documents.Paragraph),
			typeMetadata: new FrameworkPropertyMetadata(0.0)
		);

	public Paragraph() : base()
	{
		Inlines = new InlineCollection(this);
	}
}
