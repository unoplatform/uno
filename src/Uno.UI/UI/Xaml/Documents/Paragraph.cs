using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents;

[ContentProperty(Name = nameof(Inlines))]
public partial class Paragraph : Block
{
#if __SKIA__
	// MUX Reference Paragraph::AppendAutomationPeerChildren — recurse into the inlines whose content-start
	// falls within [startPos, endPos]. Skia-only: requires the TextPointer/position layer.
	internal protected override void AppendAutomationPeerChildren(IList<AutomationPeer> automationPeerChildren, int startPos, int endPos)
	{
		foreach (var inline in Inlines)
		{
			var inlineStart = inline.GetContentStart();
			var posInlineStart = inlineStart?.Offset ?? -1;
			if (startPos <= posInlineStart && posInlineStart <= endPos)
			{
				inline.AppendAutomationPeerChildren(automationPeerChildren, startPos, endPos);
			}
		}
	}
#endif

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
