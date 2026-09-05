using System;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace Uno.UI.Samples.Content.UITests.RichTextBlockControl
{
	[Sample("RichTextBlock", Name = "RichTextBlock_Showcase", Description = "Comprehensive side-by-side parity bench: ~55 captioned RichTextBlock instances across formatting, hyperlinks, inline UI, layout, typography, i18n, selection, highlighters, overflow, pointers and edge cases. Compare against native WinUI to spot gaps.")]
	public sealed partial class RichTextBlock_Showcase : UserControl
	{
		private int _hyperlinkClicks;

		public RichTextBlock_Showcase()
		{
			this.InitializeComponent();
			Loaded += OnShowcaseLoaded;
		}

		private void OnShowcaseLoaded(object sender, RoutedEventArgs e)
		{
			SetupHighlighters();
			BuildProgrammaticContent();
			RefreshOverflowReadout();
			ReadPointerInfo();
		}

		// 2.1 — Hyperlink Click
		private void OnShowcaseHyperlinkClick(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			_hyperlinkClicks++;
			HyperlinkReadout.Text = $"Click fired (count: {_hyperlinkClicks})";
		}

		// 8.1 — Selection
		private void OnShowcaseSelectionChanged(object sender, RoutedEventArgs e)
		{
			var text = SelectionSample.SelectedText ?? string.Empty;
			SelectionReadout.Text = text.Length == 0
				? "SelectedText: (none)"
				: $"SelectedText ({text.Length} chars): \"{text}\"";
		}

		private void OnShowcaseSelectAll(object sender, RoutedEventArgs e)
		{
			try
			{
				SelectionSample.SelectAll();
				OnShowcaseSelectionChanged(sender, e);
			}
			catch (Exception ex)
			{
				SelectionReadout.Text = $"⚠ not supported: SelectAll — {ex.GetType().Name}";
			}
		}

		private void OnShowcaseCopySelection(object sender, RoutedEventArgs e)
		{
			try
			{
				SelectionSample.CopySelectionToClipboard();
				var len = (SelectionSample.SelectedText ?? string.Empty).Length;
				SelectionReadout.Text = $"Copied {len} chars to clipboard.";
			}
			catch (Exception ex)
			{
				SelectionReadout.Text = $"⚠ not supported: CopySelectionToClipboard — {ex.GetType().Name}";
			}
		}

		// 9.1 — Highlighters (TextRange has no XAML syntax, so applied here)
		private void SetupHighlighters()
		{
			try
			{
				var backgroundOnly = new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow) };
				backgroundOnly.Ranges.Add(new TextRange { StartIndex = 0, Length = 20 });
				HighlighterSample.TextHighlighters.Add(backgroundOnly);

				var withForeground = new TextHighlighter
				{
					Background = new SolidColorBrush(Colors.MediumPurple),
					Foreground = new SolidColorBrush(Colors.White),
				};
				withForeground.Ranges.Add(new TextRange { StartIndex = 46, Length = 22 });
				HighlighterSample.TextHighlighters.Add(withForeground);

				// Spans across the paragraph boundary into the second paragraph.
				var crossParagraph = new TextHighlighter { Background = new SolidColorBrush(Colors.LightGreen) };
				crossParagraph.Ranges.Add(new TextRange { StartIndex = 92, Length = 60 });
				HighlighterSample.TextHighlighters.Add(crossParagraph);

				// Overlaps the cross-paragraph highlight to exercise the merge algorithm.
				var overlapping = new TextHighlighter { Background = new SolidColorBrush(Colors.LightSalmon) };
				overlapping.Ranges.Add(new TextRange { StartIndex = 130, Length = 40 });
				HighlighterSample.TextHighlighters.Add(overlapping);
			}
			catch (Exception ex)
			{
				HighlighterSample.TextHighlighters.Clear();
				System.Diagnostics.Debug.WriteLine($"[RichTextBlock_Showcase] highlighters failed: {ex}");
			}
		}

		// 10.1 — Overflow chain readout
		private void OnShowcaseRefreshOverflow(object sender, RoutedEventArgs e) => RefreshOverflowReadout();

		private void RefreshOverflowReadout()
		{
			try
			{
				OverflowReadout.Text =
					$"master.HasOverflowContent={OverflowMaster.HasOverflowContent}  ·  " +
					$"overflow1.HasOverflowContent={Overflow1.HasOverflowContent}  ·  " +
					$"overflow2.HasOverflowContent={Overflow2.HasOverflowContent} / IsTextTrimmed={Overflow2.IsTextTrimmed}";
			}
			catch (Exception ex)
			{
				OverflowReadout.Text = $"⚠ not supported: overflow readout — {ex.GetType().Name}";
			}
		}

		// 11.1 — Programmatic content
		private void BuildProgrammaticContent()
		{
			try
			{
				var rtb = new RichTextBlock { TextWrapping = TextWrapping.Wrap };

				var p1 = new Paragraph();
				p1.Inlines.Add(new Run { Text = "This entire block was built at runtime — " });
				p1.Inlines.Add(new Run { Text = "Blocks", FontWeight = FontWeights.Bold });
				p1.Inlines.Add(new Run { Text = ", " });
				p1.Inlines.Add(new Run { Text = "Paragraphs", FontStyle = Windows.UI.Text.FontStyle.Italic });
				p1.Inlines.Add(new Run { Text = " and " });
				p1.Inlines.Add(new Run { Text = "Runs", Foreground = new SolidColorBrush(Colors.SteelBlue) });
				p1.Inlines.Add(new Run { Text = " added via the object model." });
				rtb.Blocks.Add(p1);

				var p2 = new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
				var link = new Hyperlink { NavigateUri = new Uri("https://platform.uno/") };
				link.Inlines.Add(new Run { Text = "a hyperlink added in code" });
				p2.Inlines.Add(new Run { Text = "It even contains " });
				p2.Inlines.Add(link);
				p2.Inlines.Add(new Run { Text = "." });
				rtb.Blocks.Add(p2);

				ProgrammaticHost.Child = rtb;
			}
			catch (Exception ex)
			{
				ProgrammaticHost.Child = new TextBlock
				{
					Text = $"⚠ not supported: programmatic build — {ex.GetType().Name}",
					FontFamily = new FontFamily("Consolas"),
					Foreground = new SolidColorBrush(Colors.OrangeRed),
				};
			}
		}

		// 11.2 — TextPointer hit-testing
		private void OnShowcasePointerTapped(object sender, TappedRoutedEventArgs e)
		{
			try
			{
				Point p = e.GetPosition(PointerSample);
				var pointer = PointerSample.GetPositionFromPoint(p);
				PointerReadout.Text = pointer is null
					? $"GetPositionFromPoint({p.X:0},{p.Y:0}) → null"
					: $"GetPositionFromPoint({p.X:0},{p.Y:0}) → offset {pointer.Offset}, dir {pointer.LogicalDirection}";
			}
			catch (Exception ex)
			{
				PointerReadout.Text = $"⚠ not supported: GetPositionFromPoint — {ex.GetType().Name}";
			}
		}

		private void ReadPointerInfo()
		{
			try
			{
				var start = PointerSample.ContentStart;
				var end = PointerSample.ContentEnd;
				string baseline;
				try
				{
					baseline = PointerSample.BaselineOffset.ToString("0.##");
				}
				catch (Exception ex)
				{
					baseline = $"⚠ {ex.GetType().Name}";
				}

				PointerReadout.Text =
					$"ContentStart.Offset={start?.Offset}  ·  ContentEnd.Offset={end?.Offset}  ·  BaselineOffset={baseline}  ·  (tap to hit-test)";
			}
			catch (Exception ex)
			{
				PointerReadout.Text = $"⚠ not supported: content pointers — {ex.GetType().Name}";
			}
		}
	}
}
