using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents;

[ContentProperty(Name = nameof(Inlines))]
public partial class Paragraph : Block
{
	public double TextIndent
	{
		get => (double)GetValue(TextIndentProperty);
		set => SetValue(TextIndentProperty, value);
	}

	public InlineCollection Inlines { get; }

	public static global::Microsoft.UI.Xaml.DependencyProperty TextIndentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(TextIndent),
			propertyType: typeof(double),
			ownerType: typeof(global::Microsoft.UI.Xaml.Documents.Paragraph),
			typeMetadata: new FrameworkPropertyMetadata(0.0)
		);

	public Paragraph() : base()
	{
		Inlines = new InlineCollection(this);
	}
}
