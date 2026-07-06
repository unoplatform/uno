using System;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_CharacterFormatting", Description = "Functional RichEditBox on Skia: apply Bold/Italic/Underline/Strikethrough/Foreground/Size to a Document range via the Text Object Model, rendered as inlines, with undo/redo.")]
	public sealed partial class RichEditBox_CharacterFormatting : Page
	{
		private const string InitialText = "The quick brown fox jumps over the lazy dog.";

		public RichEditBox_CharacterFormatting()
		{
			this.InitializeComponent();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Editor.Document.SetText(TextSetOptions.None, InitialText);
			Output.Text = "Pick a Start/End range, then apply formatting.";
		}

		// Resolves the current [start, end) range from the two text boxes, clamped to the document.
		private (ITextRange range, int start, int end) GetRange()
		{
			Editor.Document.GetText(TextGetOptions.None, out var text);
			var length = text.Length;
			var start = Math.Clamp(ParseOr(StartBox.Text, 0), 0, length);
			var end = Math.Clamp(ParseOr(EndBox.Text, length), start, length);
			return (Editor.Document.GetRange(start, end), start, end);
		}

		private static int ParseOr(string value, int fallback)
			=> int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

		private void Run(Func<string> action)
		{
			try
			{
				Output.Text = action();
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}

		private void OnBoldClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var isBold = range.CharacterFormat.Bold == FormatEffect.On;
			range.CharacterFormat.Bold = isBold ? FormatEffect.Off : FormatEffect.On;
			return $"Bold {(isBold ? "off" : "on")} over [{start}, {end}).";
		});

		private void OnItalicClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var isItalic = range.CharacterFormat.Italic == FormatEffect.On;
			range.CharacterFormat.Italic = isItalic ? FormatEffect.Off : FormatEffect.On;
			return $"Italic {(isItalic ? "off" : "on")} over [{start}, {end}).";
		});

		private void OnUnderlineClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var isUnderlined = range.CharacterFormat.Underline == UnderlineType.Single;
			range.CharacterFormat.Underline = isUnderlined ? UnderlineType.None : UnderlineType.Single;
			return $"Underline {(isUnderlined ? "off" : "on")} over [{start}, {end}).";
		});

		private void OnStrikethroughClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var isStruck = range.CharacterFormat.Strikethrough == FormatEffect.On;
			range.CharacterFormat.Strikethrough = isStruck ? FormatEffect.Off : FormatEffect.On;
			return $"Strikethrough {(isStruck ? "off" : "on")} over [{start}, {end}).";
		});

		private void OnRedClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			range.CharacterFormat.ForegroundColor = global::Microsoft.UI.Colors.Red;
			return $"Foreground red over [{start}, {end}).";
		});

		private void OnGreenClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			range.CharacterFormat.ForegroundColor = global::Microsoft.UI.Colors.Green;
			return $"Foreground green over [{start}, {end}).";
		});

		private void OnBiggerClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var current = range.CharacterFormat.Size;
			var size = current > 0 ? current + 4 : 20;
			range.CharacterFormat.Size = size;
			return $"Size {size} over [{start}, {end}).";
		});

		private void OnSmallerClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			var (range, start, end) = GetRange();
			var current = range.CharacterFormat.Size;
			var size = current > 4 ? current - 4 : 12;
			range.CharacterFormat.Size = size;
			return $"Size {size} over [{start}, {end}).";
		});

		private void OnUndoClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			if (!Editor.Document.CanUndo())
			{
				return "Nothing to undo.";
			}

			Editor.Document.Undo();
			return "Undone.";
		});

		private void OnRedoClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			if (!Editor.Document.CanRedo())
			{
				return "Nothing to redo.";
			}

			Editor.Document.Redo();
			return "Redone.";
		});

		private void OnResetClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			Editor.Document.SetText(TextSetOptions.None, InitialText);
			return "Document reset to plain text.";
		});
	}
}
