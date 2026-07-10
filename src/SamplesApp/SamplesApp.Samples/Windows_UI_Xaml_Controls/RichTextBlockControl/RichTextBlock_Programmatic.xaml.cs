using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichTextBlockControl
{
	[Sample("RichTextBlock", Name = "RichTextBlock_Programmatic")]
	public sealed partial class RichTextBlock_Programmatic : UserControl
	{
		private int _paragraphCounter = 1;

		public RichTextBlock_Programmatic()
		{
			this.InitializeComponent();
			BuildDynamicContent();
		}

		private void BuildDynamicContent()
		{
			// Build rich text content programmatically
			var paragraph1 = new Paragraph();
			paragraph1.Inlines.Add(new Run { Text = "This paragraph was " });
			paragraph1.Inlines.Add(new Run { Text = "built programmatically", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
			paragraph1.Inlines.Add(new Run { Text = " using C# code." });

			var paragraph2 = new Paragraph();
			paragraph2.Inlines.Add(new Run { Text = "Mixed " });
			paragraph2.Inlines.Add(new Run { Text = "colors", Foreground = new SolidColorBrush(Colors.Red) });
			paragraph2.Inlines.Add(new Run { Text = " and " });
			paragraph2.Inlines.Add(new Run { Text = "sizes", FontSize = 24 });
			paragraph2.Inlines.Add(new Run { Text = " in code." });

			var paragraph3 = new Paragraph();
			var hyperlink = new Hyperlink { NavigateUri = new Uri("https://platform.uno") };
			hyperlink.Inlines.Add(new Run { Text = "Uno Platform link" });
			paragraph3.Inlines.Add(new Run { Text = "A programmatic hyperlink: " });
			paragraph3.Inlines.Add(hyperlink);

			DynamicBlock.Blocks.Add(paragraph1);
			DynamicBlock.Blocks.Add(paragraph2);
			DynamicBlock.Blocks.Add(paragraph3);
		}

		private void OnAddParagraph(object sender, RoutedEventArgs e)
		{
			_paragraphCounter++;
			var para = new Paragraph();
			para.Inlines.Add(new Run { Text = $"Added paragraph #{_paragraphCounter}" });
			MutableBlock.Blocks.Add(para);
			ParagraphCount.Text = $"Paragraphs: {MutableBlock.Blocks.Count}";
		}

		private void OnClearAll(object sender, RoutedEventArgs e)
		{
			MutableBlock.Blocks.Clear();
			ParagraphCount.Text = $"Paragraphs: {MutableBlock.Blocks.Count}";
		}
	}
}
