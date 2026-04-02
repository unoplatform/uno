using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_BasicUsage",
		Description = "Demonstrates basic RichEditBox usage with PlaceholderText, Header, Description, and pre-populated content.")]
	public sealed partial class RichEditBox_BasicUsage : UserControl
	{
		public RichEditBox_BasicUsage()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Pre-populate one RichEditBox with text
			PrePopulatedRichEditBox.Document.SetText(TextSetOptions.None,
				"This RichEditBox was pre-populated with text using the Document.SetText API.\r\rIt supports multiple paragraphs separated by carriage returns.\r\rYou can edit this text freely.");
		}

		private void GetContentButton_Click(object sender, RoutedEventArgs e)
		{
			ContentReadRichEditBox.Document.GetText(TextGetOptions.None, out var text);
			if (string.IsNullOrEmpty(text))
			{
				ContentOutputTextBlock.Text = "(empty - type something first)";
			}
			else
			{
				ContentOutputTextBlock.Text = text;
			}
		}
	}
}
