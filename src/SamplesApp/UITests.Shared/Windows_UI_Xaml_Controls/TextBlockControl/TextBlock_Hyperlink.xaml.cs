using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[SampleControlInfo("TextBlock", "TextBlock_Hyperlink")]
	public sealed partial class TextBlock_Hyperlink : UserControl
	{
		public TextBlock_Hyperlink()
		{
			this.InitializeComponent();

			var run1 = new Run { Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " };
			var run2 = new Run { Text = "sed do eiusmod tempor " };
			var run3 = new Run { Text = "incididunt ut labore et dolore magna aliqua." };
			var hyperlink = new Hyperlink();
			hyperlink.Inlines.Add(run2);
			hyperlink.Click += Hyperlink_Click;
			MyTextBlock.Inlines.Clear();
			MyTextBlock.Inlines.Add(run1);
			MyTextBlock.Inlines.Add(hyperlink);
			MyTextBlock.Inlines.Add(run3);
		}

		private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
		{
			var t = new Windows.UI.Popups.MessageDialog("Hyperlink clicked!").ShowAsync();
		}
	}
}
