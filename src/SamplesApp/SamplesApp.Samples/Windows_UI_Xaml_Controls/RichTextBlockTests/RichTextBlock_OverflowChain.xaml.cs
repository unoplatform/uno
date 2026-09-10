using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_OverflowChain", Description = "Resize the master and change MaxLines; verify content re-flows across the Master -> Overflow1 -> Overflow2 chain and the master never paints past its own slice.", IsManualTest = true)]
public sealed partial class RichTextBlock_OverflowChain : Page
{
	public RichTextBlock_OverflowChain()
	{
		this.InitializeComponent();

		Master.OverflowContentTarget = Overflow1;
		Overflow1.OverflowContentTarget = Overflow2;

		Master.LayoutUpdated += (s, e) => RefreshIndicators();
		Overflow1.LayoutUpdated += (s, e) => RefreshIndicators();
		Overflow2.LayoutUpdated += (s, e) => RefreshIndicators();
	}

	private void OnWidthChanged(object sender, RangeBaseValueChangedEventArgs e) => Master.Width = e.NewValue;

	private void OnHeightChanged(object sender, RangeBaseValueChangedEventArgs e) => Master.Height = e.NewValue;

	private void OnMaxLinesChanged(object sender, RangeBaseValueChangedEventArgs e) => Master.MaxLines = (int)e.NewValue;

	private void RefreshIndicators()
	{
		MasterOverflowIndicator.Text = $"Master.HasOverflowContent: {Master.HasOverflowContent}";
		MasterTrimmedIndicator.Text = $"Master.IsTextTrimmed: {Master.IsTextTrimmed}";
		Overflow1Indicator.Text = $"Overflow1.HasOverflowContent: {Overflow1.HasOverflowContent}";
		Overflow2Indicator.Text = $"Overflow2.HasOverflowContent: {Overflow2.HasOverflowContent}";
	}
}
