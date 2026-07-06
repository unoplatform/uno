#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// Projects the RichEditBox Text Object Model's character-format run model onto the shared
	// DisplayBlock (a TextBlock). When no run carries special formatting the plain-text fast path
	// (identical to TextBox) is used; otherwise each run becomes a TextBlock inline carrying the
	// tracked formatting (weight, style, decorations, foreground, size, family).
	partial class RichEditBox
	{
		private bool _lastRenderWasRich;

		private void RenderDocument()
		{
			if (_textBoxView is null)
			{
				return;
			}

			var document = Document;
			var text = document.PlainText;
			var runs = document.FormatRuns;
			var block = _textBoxView.DisplayBlock;

			if (AllRunsDefault(runs))
			{
				if (_lastRenderWasRich)
				{
					// Deterministically collapse any previously-built rich inlines back to plain text;
					// setting DisplayBlock.Text alone would be a no-op when the text is unchanged.
					block.Inlines.Clear();
					_lastRenderWasRich = false;
				}

				_textBoxView.SetTextNative(text);
				return;
			}

			RenderRuns(block, text, runs);
			_lastRenderWasRich = true;
		}

		private static void RenderRuns(TextBlock block, string text, IReadOnlyList<FormatRun> runs)
		{
			var inlines = block.Inlines;
			inlines.Clear();

			var position = 0;
			foreach (var run in runs)
			{
				if (position >= text.Length)
				{
					break;
				}

				var length = Math.Min(run.Length, text.Length - position);
				if (length <= 0)
				{
					continue;
				}

				var segment = text.Substring(position, length);
				position += length;
				inlines.Add(CreateRun(segment, run.Format));
			}
		}

		private static Run CreateRun(string text, CharacterFormatState format)
		{
			var run = new Run { Text = text };

			if (format.Bold)
			{
				run.FontWeight = global::Microsoft.UI.Text.FontWeights.Bold;
			}

			if (format.Italic)
			{
				run.FontStyle = global::Windows.UI.Text.FontStyle.Italic;
			}

			var decorations = global::Windows.UI.Text.TextDecorations.None;
			if (format.Underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined)
			{
				decorations |= global::Windows.UI.Text.TextDecorations.Underline;
			}

			if (format.Strikethrough)
			{
				decorations |= global::Windows.UI.Text.TextDecorations.Strikethrough;
			}

			if (decorations != global::Windows.UI.Text.TextDecorations.None)
			{
				run.TextDecorations = decorations;
			}

			if (format.Foreground is { } color)
			{
				run.Foreground = new SolidColorBrush(color);
			}

			if (format.Size > 0)
			{
				run.FontSize = format.Size;
			}

			if (!string.IsNullOrEmpty(format.Name))
			{
				run.FontFamily = new FontFamily(format.Name);
			}

			return run;
		}

		private static bool AllRunsDefault(IReadOnlyList<FormatRun> runs)
		{
			foreach (var run in runs)
			{
				var format = run.Format;
				if (format.Bold
					|| format.Italic
					|| format.Strikethrough
					|| format.Underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined
					|| format.Foreground is not null
					|| format.Size > 0
					|| !string.IsNullOrEmpty(format.Name))
				{
					return false;
				}
			}

			return true;
		}
	}
}
