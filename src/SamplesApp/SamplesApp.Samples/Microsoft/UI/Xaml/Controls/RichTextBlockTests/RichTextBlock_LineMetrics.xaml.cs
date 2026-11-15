using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_LineMetrics", Description =
	"Line-metrics playground: adjust LineHeight/LineStackingStrategy/TextIndent/TextAlignment and verify " +
	"the paragraph re-flows per strategy and the baseline marker tracks BaselineOffset.",
	IsManualTest = true)]
public sealed partial class RichTextBlock_LineMetrics : Page
{
	public RichTextBlock_LineMetrics()
	{
		this.InitializeComponent();
		Rtb.LayoutUpdated += (s, e) => UpdateReadout();
		UpdateReadout();
	}

	private void OnLineHeightChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		Rtb.LineHeight = e.NewValue;
		UpdateReadout();
	}

	private void OnTextIndentChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		FirstParagraph.TextIndent = e.NewValue;
		SecondParagraph.TextIndent = e.NewValue;
		UpdateReadout();
	}

	private void OnStrategyChanged(object sender, SelectionChangedEventArgs e)
	{
		if (StrategyCombo.SelectedItem is string value)
		{
			Rtb.LineStackingStrategy = (LineStackingStrategy)System.Enum.Parse(typeof(LineStackingStrategy), value);
			UpdateReadout();
		}
	}

	private void OnAlignmentChanged(object sender, SelectionChangedEventArgs e)
	{
		if (AlignmentCombo.SelectedItem is string value)
		{
			Rtb.TextAlignment = (TextAlignment)System.Enum.Parse(typeof(TextAlignment), value);
			UpdateReadout();
		}
	}

	// BaselineOffset is a plain computed property (not a DP), so it's re-read after every layout pass.
	private void UpdateReadout()
	{
		var baseline = Rtb.BaselineOffset;
		BaselineMarker.Margin = new Thickness(0, baseline, 0, 0);
		ReadoutText.Text =
			$"LineHeight={Rtb.LineHeight:0} | Strategy={Rtb.LineStackingStrategy} | TextIndent={FirstParagraph.TextIndent:0} | " +
			$"TextAlignment={Rtb.TextAlignment} | BaselineOffset={baseline:0.##}";
	}
}
