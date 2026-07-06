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

		// Uno-specific: a *uniform* paragraph alignment resolved from the TOM paragraph model and
		// projected onto this RichEditBox's own DisplayBlock. Null when no uniform, non-default alignment
		// applies, in which case the control-level TextAlignment DP drives the block. Read by
		// ITextBoxViewHost.IsTextAlignmentSetToDefault so the shared TextBlock honors this override.
		private global::Microsoft.UI.Xaml.TextAlignment? _paragraphAlignmentOverride;

		internal global::Microsoft.UI.Xaml.TextAlignment? ParagraphAlignmentOverride => _paragraphAlignmentOverride;

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
			}
			else
			{
				RenderRuns(block, text, runs);
				_lastRenderWasRich = true;
			}

			ApplyParagraphAlignment();
		}

		// Projects a *uniform* paragraph alignment (Center/Right/Justify) from the Text Object Model onto
		// the shared single-TextBlock DisplayBlock. A single TextBlock cannot express per-paragraph
		// alignment, so only a uniform, non-default value is renderable; Left/Undefined/mixed alignments
		// leave the control-level TextAlignment DP in charge. Setting _paragraphAlignmentOverride makes
		// ITextBoxViewHost.IsTextAlignmentSetToDefault report false so the block's alignment takes effect.
		//
		// TODO Uno: per-paragraph alignment divergence, indents, spacing, and lists still require a
		// multi-paragraph layout (RichTextBlock-style) and remain unrendered — the model round-trips them
		// faithfully but only uniform alignment is shown.
		private void ApplyParagraphAlignment()
		{
			if (_textBoxView is null)
			{
				return;
			}

			var uniform = Document.GetUniformParagraphAlignment();
			if (uniform is { } alignment
				&& alignment != global::Microsoft.UI.Text.ParagraphAlignment.Undefined
				&& alignment != global::Microsoft.UI.Text.ParagraphAlignment.Left
				&& TryMapParagraphAlignment(alignment, out var mapped))
			{
				_paragraphAlignmentOverride = mapped;
				_textBoxView.DisplayBlock.TextAlignment = mapped;
			}
			else if (_paragraphAlignmentOverride is not null)
			{
				// Transition back to the control-level TextAlignment DP.
				_paragraphAlignmentOverride = null;
				_textBoxView.SetTextAlignment();
			}
		}

		private static bool TryMapParagraphAlignment(global::Microsoft.UI.Text.ParagraphAlignment alignment, out global::Microsoft.UI.Xaml.TextAlignment mapped)
		{
			switch (alignment)
			{
				case global::Microsoft.UI.Text.ParagraphAlignment.Left:
					mapped = global::Microsoft.UI.Xaml.TextAlignment.Left;
					return true;
				case global::Microsoft.UI.Text.ParagraphAlignment.Center:
					mapped = global::Microsoft.UI.Xaml.TextAlignment.Center;
					return true;
				case global::Microsoft.UI.Text.ParagraphAlignment.Right:
					mapped = global::Microsoft.UI.Xaml.TextAlignment.Right;
					return true;
				case global::Microsoft.UI.Text.ParagraphAlignment.Justify:
					mapped = global::Microsoft.UI.Xaml.TextAlignment.Justify;
					return true;
				default:
					mapped = global::Microsoft.UI.Xaml.TextAlignment.Left;
					return false;
			}
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
