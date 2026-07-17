#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// Projects the RichEditBox Text Object Model's character-format run model onto the shared
	// DisplayBlock (a TextBlock). When no run carries special formatting the plain-text fast path
	// (identical to TextBox) is used; otherwise each run becomes a TextBlock inline carrying the
	// tracked formatting (weight, style, decorations, foreground, size, family).
	partial class RichEditBox
	{
		private const double DipsPerPoint = 96d / 72d;
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
			var paragraphRuns = document.ParagraphRuns;
			var renderParagraphAlignments = HasMixedParagraphAlignments(paragraphRuns);
			var block = _textBoxView.DisplayBlock;
			block.FontFamily = document.IsMathMode
				? new FontFamily(global::Microsoft.UI.Text.RichEditTextDocument.MathFontFamilyName)
				: FontFamily;
			block.DefaultTabStop = document.DefaultTabStop * 4f / 3f;

			if (AllRunsDefault(runs) && !renderParagraphAlignments)
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
				RenderRuns(block, text, runs, paragraphRuns, renderParagraphAlignments);
				_textBoxView.Extension?.SetText(text);
				_lastRenderWasRich = true;
			}

			ApplyParagraphAlignment();
		}

		// Projects a uniform paragraph alignment onto the DisplayBlock's block-level fast path. Mixed
		// alignments are carried by individual runs and resolved per visual line by UnicodeText. Setting
		// _paragraphAlignmentOverride makes ITextBoxViewHost.IsTextAlignmentSetToDefault report false.
		//
		// TODO Uno: Indents, spacing, and lists remain unrendered while their model state round-trips.
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

		private static void RenderRuns(
			TextBlock block,
			string text,
			IReadOnlyList<FormatRun> runs,
			IReadOnlyList<ParagraphRun> paragraphRuns,
			bool renderParagraphAlignments)
		{
			var inlines = block.Inlines;
			inlines.Clear();

			var position = 0;
			var characterRunIndex = 0;
			var characterRunOffset = 0;
			var paragraphRunIndex = 0;
			var paragraphRunOffset = 0;
			while (position < text.Length && characterRunIndex < runs.Count && paragraphRunIndex < paragraphRuns.Count)
			{
				var characterRun = runs[characterRunIndex];
				var paragraphRun = paragraphRuns[paragraphRunIndex];
				var length = Math.Min(
					text.Length - position,
					Math.Min(characterRun.Length - characterRunOffset, paragraphRun.Length - paragraphRunOffset));
				if (characterRun.Format.InlineImage is not null)
				{
					length = Math.Min(length, 1);
				}
				if (length <= 0)
				{
					if (characterRunOffset >= characterRun.Length)
					{
						characterRunIndex++;
						characterRunOffset = 0;
					}
					if (paragraphRunOffset >= paragraphRun.Length)
					{
						paragraphRunIndex++;
						paragraphRunOffset = 0;
					}
					continue;
				}

				var segment = text.Substring(position, length);
				var inline = CreateRun(segment, characterRun.Format, block.FontSize);
				if (renderParagraphAlignments && TryMapParagraphAlignment(paragraphRun.Format.Alignment, out var paragraphAlignment))
				{
					inline.ParagraphAlignment = paragraphAlignment;
				}

				if (characterRun.Format.Link is not null)
				{
					var hyperlink = new Hyperlink();
					hyperlink.Inlines.Add(inline);
					inlines.Add(hyperlink);
				}
				else
				{
					inlines.Add(inline);
				}

				position += length;
				characterRunOffset += length;
				paragraphRunOffset += length;
				if (characterRunOffset == characterRun.Length)
				{
					characterRunIndex++;
					characterRunOffset = 0;
				}
				if (paragraphRunOffset == paragraphRun.Length)
				{
					paragraphRunIndex++;
					paragraphRunOffset = 0;
				}
			}
		}

		private static bool HasMixedParagraphAlignments(IReadOnlyList<ParagraphRun> runs)
		{
			if (runs.Count < 2)
			{
				return false;
			}

			var alignment = runs[0].Format.Alignment;
			for (var i = 1; i < runs.Count; i++)
			{
				if (runs[i].Format.Alignment != alignment)
				{
					return true;
				}
			}

			return false;
		}

		private static Run CreateRun(string text, CharacterFormatState format, double inheritedFontSize)
		{
			var run = new Run { Text = text };
			run.CharacterBackground = format.Background;
			run.IsHidden = format.Hidden;
			if (format.InlineImage is { } inlineImage)
			{
				run.InlineObject = new InlineObjectInfo(
					inlineImage.GetDecodedImage(),
					inlineImage.Width,
					inlineImage.Height,
					inlineImage.Ascent,
					inlineImage.VerticalAlignment);
			}

			if (format.WeightExplicit || format.Weight != 400)
			{
				run.FontWeight = new global::Windows.UI.Text.FontWeight((ushort)Math.Clamp(format.Weight, 0, 999));
			}

			if (format.Italic)
			{
				run.FontStyle = global::Windows.UI.Text.FontStyle.Italic;
			}

			if (format.FontStretch != global::Windows.UI.Text.FontStretch.Normal)
			{
				run.FontStretch = format.FontStretch;
			}

			var decorations = global::Windows.UI.Text.TextDecorations.None;
			if (format.Underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined)
			{
				decorations |= global::Windows.UI.Text.TextDecorations.Underline;
				run.RichEditUnderlineType = format.Underline;
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
				run.FontSize = format.Size * DipsPerPoint;
			}

			if (format.Spacing != 0)
			{
				var fontSizeInPoints = format.Size > 0 ? format.Size : (float)(inheritedFontSize / DipsPerPoint);
				if (fontSizeInPoints > 0)
				{
					run.CharacterSpacing = (int)Math.Round(format.Spacing / fontSizeInPoints * 1000, MidpointRounding.AwayFromZero);
				}
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
					|| format.WeightExplicit
					|| format.Weight != 400
										|| format.Background is not null
										|| format.Hidden
					|| format.Italic
					|| format.FontStretch != global::Windows.UI.Text.FontStretch.Normal
					|| format.Strikethrough
					|| format.Underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined
					|| format.Foreground is not null
					|| format.Spacing != 0
					|| format.Size > 0
					|| !string.IsNullOrEmpty(format.Name)
					|| format.Link is not null
					|| format.InlineImage is not null)
				{
					return false;
				}
			}

			return true;
		}
	}
}
