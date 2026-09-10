using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_SelectionInteraction", Description =
	"Selection playground. Drag to select, double-click a word, Shift+click to extend, Ctrl+A/Ctrl+C, " +
	"right-click for the Copy flyout. Use the controls to toggle selection, change the highlight color, " +
	"and drive selection programmatically. Verify SelectedText and the SelectionChanged log update correctly.",
	IsManualTest = true)]
public sealed partial class RichTextBlock_SelectionInteraction : Page
{
	private const int MaxLogEntries = 20;

	private readonly List<string> _logEntries = new();

	public RichTextBlock_SelectionInteraction()
	{
		this.InitializeComponent();
	}

	private void OnSelectionEnabledToggled(object sender, RoutedEventArgs e) =>
		TestRichTextBlock.IsTextSelectionEnabled = SelectionEnabledSwitch.IsOn;

	private void OnHighlightColorChanged(object sender, SelectionChangedEventArgs e)
	{
		if (HighlightColorCombo.SelectedItem is not ComboBoxItem { Tag: string tag })
		{
			return;
		}

		TestRichTextBlock.SelectionHighlightColor = new SolidColorBrush(tag switch
		{
			"Orange" => Colors.Orange,
			"Green" => Colors.Green,
			"Magenta" => Colors.Magenta,
			_ => Colors.CornflowerBlue,
		});
	}

	private void OnSelectAllClick(object sender, RoutedEventArgs e) => TestRichTextBlock.SelectAll();

	private void OnCollapseClick(object sender, RoutedEventArgs e)
	{
		var start = TestRichTextBlock.ContentStart;
		if (start is not null)
		{
			TestRichTextBlock.Select(start, start);
		}
	}

	private void OnCopyClick(object sender, RoutedEventArgs e) => TestRichTextBlock.CopySelectionToClipboard();

	private void OnRichTextBlockSelectionChanged(object sender, RoutedEventArgs e)
	{
		var text = TestRichTextBlock.SelectedText;
		SelectedTextDisplay.Text = string.IsNullOrEmpty(text) ? "(none)" : text;

		var startOffset = TestRichTextBlock.SelectionStart?.Offset;
		var endOffset = TestRichTextBlock.SelectionEnd?.Offset;
		_logEntries.Insert(0, $"Start={startOffset?.ToString() ?? "null"}, End={endOffset?.ToString() ?? "null"}");
		while (_logEntries.Count > MaxLogEntries)
		{
			_logEntries.RemoveAt(_logEntries.Count - 1);
		}

		SelectionLog.ItemsSource = _logEntries.ToList();
	}
}
