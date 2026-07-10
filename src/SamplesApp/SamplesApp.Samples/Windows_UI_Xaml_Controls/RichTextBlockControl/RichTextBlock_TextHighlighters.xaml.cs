using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichTextBlockControl
{
	[Sample("RichTextBlock", Name = "RichTextBlock_TextHighlighters")]
	public sealed partial class RichTextBlock_TextHighlighters : UserControl
	{
		public RichTextBlock_TextHighlighters()
		{
			this.InitializeComponent();
			SetupHighlights();
		}

		private void SetupHighlights()
		{
			// Single highlight
			var singleHighlighter = new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow) };
			singleHighlighter.Ranges.Add(new TextRange { StartIndex = 10, Length = 15 });
			SingleHighlight.TextHighlighters.Add(singleHighlighter);

			// Multiple highlights
			var h1 = new TextHighlighter { Background = new SolidColorBrush(Colors.LightBlue) };
			h1.Ranges.Add(new TextRange { StartIndex = 0, Length = 10 });
			MultipleHighlights.TextHighlighters.Add(h1);

			var h2 = new TextHighlighter { Background = new SolidColorBrush(Colors.LightGreen) };
			h2.Ranges.Add(new TextRange { StartIndex = 15, Length = 10 });
			MultipleHighlights.TextHighlighters.Add(h2);

			var h3 = new TextHighlighter { Background = new SolidColorBrush(Colors.LightPink) };
			h3.Ranges.Add(new TextRange { StartIndex = 30, Length = 10 });
			MultipleHighlights.TextHighlighters.Add(h3);

			// Cross-paragraph highlight
			var crossHighlighter = new TextHighlighter { Background = new SolidColorBrush(Colors.LightYellow) };
			crossHighlighter.Ranges.Add(new TextRange { StartIndex = 10, Length = 40 });
			CrossParagraphHighlight.TextHighlighters.Add(crossHighlighter);
		}

		private void OnAddHighlight(object sender, RoutedEventArgs e)
		{
			var highlighter = new TextHighlighter
			{
				Background = new SolidColorBrush(Colors.Orange),
			};
			highlighter.Ranges.Add(new TextRange { StartIndex = 5, Length = 20 });
			ProgrammaticHighlight.TextHighlighters.Add(highlighter);
		}

		private void OnClearHighlights(object sender, RoutedEventArgs e)
		{
			ProgrammaticHighlight.TextHighlighters.Clear();
		}
	}
}
