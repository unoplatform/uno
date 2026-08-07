using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents;

[ContentProperty(Name = nameof(Inlines))]
public partial class Paragraph : Block
{
	// MUX Reference Paragraph::AppendAutomationPeerChildren — recurse into the inlines whose content-start
	// falls within [startPos, endPos]. The walk needs the TextPointer/position layer, which only Skia has;
	// the override still has to exist everywhere so the reference assembly matches the runtime API.
	internal protected override void AppendAutomationPeerChildren(IList<AutomationPeer> automationPeerChildren, int startPos, int endPos)
	{
#if __SKIA__
		foreach (var inline in Inlines)
		{
			var inlineStart = inline.GetContentStart();
			var posInlineStart = inlineStart?.Offset ?? -1;
			if (startPos <= posInlineStart && posInlineStart <= endPos)
			{
				inline.AppendAutomationPeerChildren(automationPeerChildren, startPos, endPos);
			}
		}
#else
		base.AppendAutomationPeerChildren(automationPeerChildren, startPos, endPos);
#endif
	}

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
