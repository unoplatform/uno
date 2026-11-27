using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_RightToLeft", Description = "Mixed bidi text, a hyperlink, a hosted element and an overflow in a right-to-left RichTextBlock.", IsManualTest = true)]
public sealed partial class RichTextBlock_RightToLeft : Page
{
	private int _clicks;

	public RichTextBlock_RightToLeft()
	{
		this.InitializeComponent();

		Master.OverflowContentTarget = Overflow;
	}

	private void OnDirectionToggled(object sender, RoutedEventArgs e)
		=> Master.FlowDirection = DirectionSwitch.IsOn ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

	private void OnLinkClicked(Hyperlink sender, HyperlinkClickEventArgs args)
		=> ClickStatus.Text = $"Link clicked {++_clicks} time(s).";
}
