using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Events",
		Description = "Demonstrates TextChanged, TextChanging, and SelectionChanged events with live logging.")]
	public sealed partial class RichEditBox_Events : UserControl
	{
		private int _textChangedCount;
		private int _textChangingCount;
		private int _selectionChangedCount;
		private int _allEventsSequence;
		private int _programmaticEventCount;

		public RichEditBox_Events()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			SelectionChangedRichEditBox.Document.SetText(TextSetOptions.None,
				"Select portions of this text to see the SelectionChanged event fire. Try clicking at different positions or dragging to select.");
		}

		// ===== TextChanged =====
		private void TextChangedRichEditBox_TextChanged(object sender, RoutedEventArgs e)
		{
			_textChangedCount++;
			TextChangedCountTextBlock.Text = $"TextChanged count: {_textChangedCount}";

			var reb = sender as RichEditBox;
			reb.Document.GetText(TextGetOptions.None, out var text);
			var truncated = text.Length > 50 ? text.Substring(0, 50) + "..." : text;

			var log = TextChangedLogTextBlock.Text;
			if (log == "(events will be logged here)")
			{
				log = "";
			}

			TextChangedLogTextBlock.Text =
				$"[{_textChangedCount}] TextChanged - Length: {text.Length}, Text: \"{truncated}\"\n{log}";
		}

		// ===== TextChanging =====
		private void TextChangingRichEditBox_TextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs args)
		{
			_textChangingCount++;
			TextChangingCountTextBlock.Text = $"TextChanging count: {_textChangingCount}";

			var log = TextChangingLogTextBlock.Text;
			if (log == "(events will be logged here)")
			{
				log = "";
			}

			TextChangingLogTextBlock.Text =
				$"[{_textChangingCount}] TextChanging fired\n{log}";
		}

		// ===== SelectionChanged =====
		private void SelectionChangedRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			_selectionChangedCount++;
			SelectionChangedCountTextBlock.Text = $"SelectionChanged count: {_selectionChangedCount}";

			var reb = sender as RichEditBox;
			var sel = reb.Document.Selection;

			var log = SelectionChangedLogTextBlock.Text;
			if (log == "(events will be logged here)")
			{
				log = "";
			}

			sel.GetText(TextGetOptions.None, out var selText);
			var truncated = selText.Length > 30 ? selText.Substring(0, 30) + "..." : selText;

			SelectionChangedLogTextBlock.Text =
				$"[{_selectionChangedCount}] SelectionChanged - Start: {sel.StartPosition}, End: {sel.EndPosition}, Text: \"{truncated}\"\n{log}";
		}

		// ===== All Events Combined =====
		private void AllEventsRichEditBox_TextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs args)
		{
			_allEventsSequence++;
			AppendAllEventsLog($"[{_allEventsSequence}] TextChanging");
		}

		private void AllEventsRichEditBox_TextChanged(object sender, RoutedEventArgs e)
		{
			_allEventsSequence++;
			var reb = sender as RichEditBox;
			reb.Document.GetText(TextGetOptions.None, out var text);
			AppendAllEventsLog($"[{_allEventsSequence}] TextChanged (length: {text.Length})");
		}

		private void AllEventsRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			_allEventsSequence++;
			var reb = sender as RichEditBox;
			var sel = reb.Document.Selection;
			AppendAllEventsLog($"[{_allEventsSequence}] SelectionChanged (pos: {sel.StartPosition}-{sel.EndPosition})");
		}

		private void AppendAllEventsLog(string entry)
		{
			AllEventsSequenceTextBlock.Text = $"Event sequence #: {_allEventsSequence}";

			var log = AllEventsLogTextBlock.Text;
			if (log == "(events will be logged here)")
			{
				log = "";
			}

			AllEventsLogTextBlock.Text = entry + "\n" + log;
		}

		private void ClearAllEventsLogButton_Click(object sender, RoutedEventArgs e)
		{
			_allEventsSequence = 0;
			AllEventsSequenceTextBlock.Text = "Event sequence #: 0";
			AllEventsLogTextBlock.Text = "(events will be logged here)";
		}

		// ===== Programmatic Changes =====
		private void ProgrammaticRichEditBox_TextChanged(object sender, RoutedEventArgs e)
		{
			_programmaticEventCount++;
			AppendProgrammaticLog($"[{_programmaticEventCount}] TextChanged fired");
		}

		private void ProgrammaticRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			_programmaticEventCount++;
			var sel = ProgrammaticRichEditBox.Document.Selection;
			AppendProgrammaticLog($"[{_programmaticEventCount}] SelectionChanged (pos: {sel.StartPosition}-{sel.EndPosition})");
		}

		private void ProgrammaticSetTextButton_Click(object sender, RoutedEventArgs e)
		{
			AppendProgrammaticLog("--- SetText called ---");
			ProgrammaticRichEditBox.Document.SetText(TextSetOptions.None, "Programmatically set text.");
		}

		private void ProgrammaticTypeTextButton_Click(object sender, RoutedEventArgs e)
		{
			AppendProgrammaticLog("--- TypeText called ---");
			ProgrammaticRichEditBox.Document.Selection.TypeText("Typed ");
		}

		private void ClearProgrammaticLogButton_Click(object sender, RoutedEventArgs e)
		{
			_programmaticEventCount = 0;
			ProgrammaticLogTextBlock.Text = "(events will be logged here)";
		}

		private void AppendProgrammaticLog(string entry)
		{
			var log = ProgrammaticLogTextBlock.Text;
			if (log == "(events will be logged here)")
			{
				log = "";
			}

			ProgrammaticLogTextBlock.Text = entry + "\n" + log;
		}
	}
}
