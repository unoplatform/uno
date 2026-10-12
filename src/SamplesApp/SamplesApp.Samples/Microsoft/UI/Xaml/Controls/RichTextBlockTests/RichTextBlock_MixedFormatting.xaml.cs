using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_MixedFormatting", Description = "Kitchen-sink static document combining Bold/Italic/Underline, nested Span-in-Span, colored runs, a Hyperlink, LineBreak, per-paragraph TextIndent/Margin, CharacterSpacing and a custom FontFamily. Verify render fidelity against the checklist at the top.", IsManualTest = true)]
public sealed partial class RichTextBlock_MixedFormatting : Page
{
	private int _clickCount;

	public RichTextBlock_MixedFormatting()
	{
		this.InitializeComponent();
	}

	private void OnHyperlinkClick(Hyperlink sender, HyperlinkClickEventArgs args) =>
		ClickCountText.Text = $"Hyperlink clicks: {++_clickCount}";
}
