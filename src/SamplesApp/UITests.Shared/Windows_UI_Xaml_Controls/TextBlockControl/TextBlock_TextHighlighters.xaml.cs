using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl;

[Sample("TextBlock")]
public sealed partial class TextBlock_TextHighlighters : UserControl
{
	private ObservableCollection<HighlighterItem> _highlighters = new();

	public TextBlock_TextHighlighters()
	{
		this.InitializeComponent();

		// Bind the list view source
		HighlightersList.ItemsSource = _highlighters;
	}

	private void AddHighlighter_Click(object sender, RoutedEventArgs e)
	{
		int start = (int)StartIndexSlider.Value;
		int length = (int)LengthSlider.Value;

		if (start + length > TargetTextBlock.Text.Length)
		{
			length = TargetTextBlock.Text.Length - start;
		}

		if (length <= 0) return;

		var range = new TextRange { StartIndex = start, Length = length };

		var bgColorName = ((ComboBoxItem)ColorSelect.SelectedItem).Tag.ToString();
		var fgColorName = ((ComboBoxItem)ForegroundSelect.SelectedItem).Tag.ToString();

		var bgBrush = bgColorName == "Null"
			? null
			: new SolidColorBrush(GetColorFromString(bgColorName));

		var fgBrush = fgColorName == "Null"
			? null
			: new SolidColorBrush(GetColorFromString(fgColorName));

		var highlighter = new TextHighlighter
		{
			Background = bgBrush,
			Foreground = fgBrush
		};

		highlighter.Ranges.Add(range);

		TargetTextBlock.TextHighlighters.Add(highlighter);
		_highlighters.Add(new HighlighterItem
		{
			Start = start,
			Length = length,
			Background = bgBrush,
			Foreground = fgBrush
		});
	}

	private void ClearHighlighters_Click(object sender, RoutedEventArgs e)
	{
		TargetTextBlock.TextHighlighters.Clear();
		_highlighters.Clear();
	}

	private Windows.UI.Color GetColorFromString(string colorName) =>
		colorName switch
		{
			"Yellow" => Colors.Yellow,
			"Lime" => Colors.Lime,
			"Cyan" => Colors.Cyan,
			"Pink" => Colors.Pink,
			"Red" => Colors.Red,
			"Blue" => Colors.Blue,
			"White" => Colors.White,
			_ => Colors.Black
		};
}

public class HighlighterItem
{
	public int Start { get; set; }
	public int Length { get; set; }
	public SolidColorBrush Background { get; set; }
	public SolidColorBrush Foreground { get; set; }
}
