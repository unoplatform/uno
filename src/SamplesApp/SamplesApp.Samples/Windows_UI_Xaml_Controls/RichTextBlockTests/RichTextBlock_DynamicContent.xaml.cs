using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_DynamicContent", Description =
	"Runtime mutation of paragraphs/runs and control-level FontSize. Verify layout re-flows after every " +
	"mutation, selection clears on content changes, and selection survives font-size-only changes.",
	IsManualTest = true)]
public sealed partial class RichTextBlock_DynamicContent : Page
{
	private int _paragraphCounter = 2;
	private bool _isToggleRunRed;
	private bool _isToggleRunLarge;

	public RichTextBlock_DynamicContent()
	{
		this.InitializeComponent();
		UpdateStatus();
	}

	private void OnAddParagraph(object sender, RoutedEventArgs e)
	{
		_paragraphCounter++;
		var paragraph = new Paragraph();
		paragraph.Inlines.Add(new Run { Text = $"Paragraph {_paragraphCounter}: added at runtime." });
		ContentBlock.Blocks.Add(paragraph);
		UpdateStatus();
	}

	private void OnRemoveLastParagraph(object sender, RoutedEventArgs e)
	{
		if (ContentBlock.Blocks.Count > 0)
		{
			ContentBlock.Blocks.RemoveAt(ContentBlock.Blocks.Count - 1);
		}

		UpdateStatus();
	}

	private void OnClearAll(object sender, RoutedEventArgs e)
	{
		ContentBlock.Blocks.Clear();
		UpdateStatus();
	}

	private void OnAppendToFirstRun(object sender, RoutedEventArgs e)
	{
		FirstRun.Text += " More text appended.";
		UpdateStatus();
	}

	private void OnShrinkFirstRun(object sender, RoutedEventArgs e)
	{
		var text = FirstRun.Text ?? string.Empty;
		FirstRun.Text = text.Substring(0, Math.Max(0, text.Length - 5));
		UpdateStatus();
	}

	private void OnToggleRunColor(object sender, RoutedEventArgs e)
	{
		_isToggleRunRed = !_isToggleRunRed;
		if (_isToggleRunRed)
		{
			ToggleRun.Foreground = new SolidColorBrush(Colors.Red);
		}
		else
		{
			ToggleRun.ClearValue(TextElement.ForegroundProperty);
		}

		UpdateStatus();
	}

	private void OnToggleRunFontSize(object sender, RoutedEventArgs e)
	{
		_isToggleRunLarge = !_isToggleRunLarge;
		ToggleRun.FontSize = _isToggleRunLarge ? 24 : 14;
		UpdateStatus();
	}

	private void OnControlFontSizeChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		// Control-level FontSize only - should never clear an in-progress selection.
		ContentBlock.FontSize = e.NewValue;
		UpdateStatus();
	}

	private void OnSelectionChanged(object sender, RoutedEventArgs e) => UpdateStatus();

	private void UpdateStatus()
	{
		var totalLength = GetTextLength(ContentBlock.Blocks);
		var selectedLength = ContentBlock.SelectedText?.Length ?? 0;
		StatusText.Text = $"Blocks.Count: {ContentBlock.Blocks.Count} | Total text length: {totalLength} | Selected text length: {selectedLength}";
	}

	private static int GetTextLength(BlockCollection blocks)
	{
		var total = 0;
		foreach (var block in blocks)
		{
			if (block is Paragraph paragraph)
			{
				total += GetTextLength(paragraph.Inlines);
			}
		}

		return total;
	}

	private static int GetTextLength(InlineCollection inlines)
	{
		var total = 0;
		foreach (var inline in inlines)
		{
			total += inline switch
			{
				Run run => run.Text?.Length ?? 0,
				Span span => GetTextLength(span.Inlines),
				_ => 0
			};
		}

		return total;
	}
}
