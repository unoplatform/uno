using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Uno.UI.Samples.Content.UITests.RichTextBlockControl
{
	[Sample("RichTextBlock", Name = "RichTextBlock_Hyperlinks")]
	public sealed partial class RichTextBlock_Hyperlinks : UserControl
	{
		public RichTextBlock_Hyperlinks()
		{
			this.InitializeComponent();
		}

		private void OnHyperlinkClick(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			ClickResult.Text = "Hyperlink was clicked!";
		}
	}
}
