using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_TextPointers", Description = "Click the paragraph to inspect the TextPointer (offset + GetCharacterRect marker) at that pixel; use the sliders and Select Range to build TextPointers via GetPositionAtOffset and select programmatically.", IsManualTest = true)]
public sealed partial class RichTextBlock_TextPointers : Page
{
	public RichTextBlock_TextPointers()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e) => RefreshContentReadouts();

	private void OnContentPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		var point = e.GetCurrentPoint(ContentBlock).Position;
		var pointer = ContentBlock.GetPositionFromPoint(point);

		if (pointer is null)
		{
			PointerOffsetText.Text = "Last click offset: (no hit)";
			ClickMarker.Visibility = Visibility.Collapsed;
			return;
		}

		PointerOffsetText.Text = $"Last click offset: {pointer.Offset}";

		var rect = pointer.GetCharacterRect(LogicalDirection.Forward);
		// Fully qualified: the sibling UITests...Canvas sample namespace shadows the control type here.
		Microsoft.UI.Xaml.Controls.Canvas.SetLeft(ClickMarker, rect.X);
		Microsoft.UI.Xaml.Controls.Canvas.SetTop(ClickMarker, rect.Y);
		ClickMarker.Width = rect.Width > 0 ? rect.Width : 2;
		ClickMarker.Height = rect.Height > 0 ? rect.Height : 16;
		ClickMarker.Visibility = Visibility.Visible;

		RefreshContentReadouts();
	}

	private void OnSelectRange(object sender, RoutedEventArgs e)
	{
		// GetPositionAtOffset moves relative to the pointer it's called on, so anchoring at
		// ContentStart (offset 0) turns the slider values into absolute document offsets.
		var start = ContentBlock.ContentStart?.GetPositionAtOffset((int)StartOffsetSlider.Value, LogicalDirection.Forward);
		var end = ContentBlock.ContentStart?.GetPositionAtOffset((int)EndOffsetSlider.Value, LogicalDirection.Forward);

		if (start is null || end is null)
		{
			SelectionResultText.Text = "Selection: could not resolve offsets";
			return;
		}

		ContentBlock.Select(start, end);
		SelectionResultText.Text = $"Selection: [{ContentBlock.SelectionStart?.Offset}, {ContentBlock.SelectionEnd?.Offset})";
	}

	private void RefreshContentReadouts()
	{
		var contentStart = ContentBlock.ContentStart;
		var contentEnd = ContentBlock.ContentEnd;

		ContentStartText.Text = $"ContentStart.Offset: {contentStart?.Offset.ToString() ?? "n/a"}";
		ContentEndText.Text = $"ContentEnd.Offset: {contentEnd?.Offset.ToString() ?? "n/a"}";
		BaselineOffsetText.Text = $"BaselineOffset: {ContentBlock.BaselineOffset:0.##}";

		if (contentEnd is not null)
		{
			StartOffsetSlider.Maximum = contentEnd.Offset;
			EndOffsetSlider.Maximum = contentEnd.Offset;
		}
	}
}
