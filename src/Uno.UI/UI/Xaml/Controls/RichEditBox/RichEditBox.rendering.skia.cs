#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
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
			var terminalParagraph = document.TerminalParagraphFormat;
			var renderParagraphAlignments = HasMixedParagraphAlignments(paragraphRuns, terminalParagraph, text);
			var renderParagraphLayouts = HasVisualParagraphFormatting(paragraphRuns, terminalParagraph);
			var block = _textBoxView.DisplayBlock;
			block.FontFamily = document.IsMathMode
				? new FontFamily(global::Microsoft.UI.Text.RichEditTextDocument.MathFontFamilyName)
				: FontFamily;
			block.DefaultTabStop = document.DefaultTabStop * 4f / 3f;
			var terminalListState = BuildListMarkerState(text, paragraphRuns);
			block.EndingParagraphLayout = renderParagraphLayouts
				? CreateParagraphLayout(terminalParagraph, terminalListState)
				: null;
			block.EndingParagraphAlignment = renderParagraphAlignments
				&& TryMapParagraphAlignment(terminalParagraph.Alignment, out var terminalAlignment)
					? terminalAlignment
					: null;

			if (AllRunsDefault(runs) && !renderParagraphAlignments && !renderParagraphLayouts)
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
				RenderRuns(block, text, runs, paragraphRuns, renderParagraphAlignments, renderParagraphLayouts);
				_textBoxView.Extension?.SetText(text);
				_lastRenderWasRich = true;
			}

			ApplyParagraphAlignment();
		}

		// Projects a uniform paragraph alignment onto the DisplayBlock's block-level fast path. Mixed
		// alignments are carried by individual runs and resolved per visual line by UnicodeText. Setting
		// _paragraphAlignmentOverride makes ITextBoxViewHost.IsTextAlignmentSetToDefault report false.
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
			bool renderParagraphAlignments,
			bool renderParagraphLayouts)
		{
			var inlines = block.Inlines;
			inlines.Clear();

			var position = 0;
			var characterRunIndex = 0;
			var characterRunOffset = 0;
			var paragraphRunIndex = 0;
			var paragraphRunOffset = 0;
			var paragraphEnd = 0;
			ParagraphLayoutInfo? paragraphLayout = null;
			var listState = new ListMarkerCounterState();
			while (position < text.Length && characterRunIndex < runs.Count && paragraphRunIndex < paragraphRuns.Count)
			{
				var characterRun = runs[characterRunIndex];
				var paragraphRun = paragraphRuns[paragraphRunIndex];
				if (position >= paragraphEnd)
				{
					paragraphEnd = GetParagraphEnd(text, position);
					paragraphLayout = CreateParagraphLayout(paragraphRun.Format, listState);
				}
				var requiresPerParagraphMarker = paragraphRun.Format.ListType is not global::Microsoft.UI.Text.MarkerType.None
					and not global::Microsoft.UI.Text.MarkerType.Undefined
					&& paragraphRun.Format.ListLevelIndex > 0;
				var length = Math.Min(
					requiresPerParagraphMarker ? paragraphEnd - position : text.Length - position,
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
				if (renderParagraphLayouts)
				{
					inline.ParagraphLayout = paragraphLayout;
					inline.FlowDirection = paragraphRun.Format.RightToLeft
						? FlowDirection.RightToLeft
						: FlowDirection.LeftToRight;
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

		private static int GetParagraphEnd(string text, int start)
		{
			for (var i = start; i < text.Length; i++)
			{
				if (text[i] == '\r')
				{
					return i + (i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1);
				}
				if (text[i] is '\n' or '\u2029')
				{
					return i + 1;
				}
			}

			return text.Length;
		}

		private static ListMarkerCounterState BuildListMarkerState(string text, IReadOnlyList<ParagraphRun> paragraphRuns)
		{
			var state = new ListMarkerCounterState();
			var paragraphRunIndex = 0;
			var paragraphRunOffset = 0;
			for (var position = 0; position < text.Length;)
			{
				while (paragraphRunIndex < paragraphRuns.Count && paragraphRunOffset >= paragraphRuns[paragraphRunIndex].Length)
				{
					paragraphRunOffset -= paragraphRuns[paragraphRunIndex].Length;
					paragraphRunIndex++;
				}
				if (paragraphRunIndex >= paragraphRuns.Count)
				{
					break;
				}

				_ = CreateParagraphLayout(paragraphRuns[paragraphRunIndex].Format, state);
				var paragraphEnd = GetParagraphEnd(text, position);
				paragraphRunOffset += paragraphEnd - position;
				position = paragraphEnd;
			}

			return state;
		}

		private sealed class ListMarkerCounterState
		{
			internal readonly Dictionary<int, ListMarkerCounter> Counters = new();
			internal int PreviousLevel;
			internal bool PreviousWasList;
		}

		private readonly record struct ListMarkerCounter(
			global::Microsoft.UI.Text.MarkerType Type,
			global::Microsoft.UI.Text.MarkerStyle Style,
			int ConfiguredStart,
			int Value);

		private static ParagraphLayoutInfo CreateParagraphLayout(ParagraphFormatState format, ListMarkerCounterState listState)
		{
			var listType = format.ListType;
			var hasList = listType is not global::Microsoft.UI.Text.MarkerType.None
				and not global::Microsoft.UI.Text.MarkerType.Undefined
				&& format.ListLevelIndex > 0;
			string? marker = null;
			if (hasList)
			{
				var level = format.ListLevelIndex;
				var style = format.ListStyle == global::Microsoft.UI.Text.MarkerStyle.Undefined
					? global::Microsoft.UI.Text.MarkerStyle.Period
					: format.ListStyle;
				if (!listState.PreviousWasList)
				{
					listState.Counters.Clear();
				}
				else if (level <= listState.PreviousLevel)
				{
					var deeperLevels = new List<int>();
					foreach (var existingLevel in listState.Counters.Keys)
					{
						if (existingLevel > level)
						{
							deeperLevels.Add(existingLevel);
						}
					}
					foreach (var deeperLevel in deeperLevels)
					{
						listState.Counters.Remove(deeperLevel);
					}
				}

				var configuredStart = format.ListStart;
				var firstValue = listType == global::Microsoft.UI.Text.MarkerType.UnicodeSequence
					? (IsValidUnicodeScalar(configuredStart) ? configuredStart : 0x2022)
					: configuredStart > 0 ? configuredStart : 1;
				var value = firstValue;
				if (listState.Counters.TryGetValue(level, out var counter)
					&& counter.Type == listType
					&& counter.Style == style
					&& counter.ConfiguredStart == configuredStart)
				{
					value = counter.Value + 1;
				}

				listState.Counters[level] = new ListMarkerCounter(listType, style, configuredStart, value);
				listState.PreviousLevel = level;
				listState.PreviousWasList = true;
				marker = FormatListMarker(listType, style, value);
			}
			else
			{
				listState.Counters.Clear();
				listState.PreviousLevel = 0;
				listState.PreviousWasList = false;
			}

			var lineSpacing = format.LineSpacingRule is global::Microsoft.UI.Text.LineSpacingRule.AtLeast or global::Microsoft.UI.Text.LineSpacingRule.Exactly
				? format.LineSpacing * (float)DipsPerPoint
				: format.LineSpacing;
			return new ParagraphLayoutInfo
			{
				LeftIndent = format.LeftIndent * (float)DipsPerPoint,
				RightIndent = format.RightIndent * (float)DipsPerPoint,
				FirstLineIndent = format.FirstLineIndent * (float)DipsPerPoint,
				SpaceBefore = Math.Max(0, format.SpaceBefore * (float)DipsPerPoint),
				SpaceAfter = Math.Max(0, format.SpaceAfter * (float)DipsPerPoint),
				LineSpacingRule = format.LineSpacingRule,
				LineSpacing = lineSpacing,
				RightToLeft = format.RightToLeft,
				IsList = hasList,
				MarkerText = marker,
				ListTab = Math.Max(0, format.ListTab * (float)DipsPerPoint),
				MarkerAlignment = format.ListAlignment == global::Microsoft.UI.Text.MarkerAlignment.Undefined
					? global::Microsoft.UI.Text.MarkerAlignment.Right
					: format.ListAlignment,
			};
		}

		private static string? FormatListMarker(
			global::Microsoft.UI.Text.MarkerType type,
			global::Microsoft.UI.Text.MarkerStyle style,
			int number)
		{
			if (style == global::Microsoft.UI.Text.MarkerStyle.NoNumber)
			{
				return null;
			}

			var value = type switch
			{
				global::Microsoft.UI.Text.MarkerType.Bullet => "•",
				global::Microsoft.UI.Text.MarkerType.Arabic => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
				global::Microsoft.UI.Text.MarkerType.LowercaseEnglishLetter => ToLetters(number, upper: false),
				global::Microsoft.UI.Text.MarkerType.UppercaseEnglishLetter => ToLetters(number, upper: true),
				global::Microsoft.UI.Text.MarkerType.LowercaseRoman => ToRoman(number).ToLowerInvariant(),
				global::Microsoft.UI.Text.MarkerType.UppercaseRoman => ToRoman(number),
				global::Microsoft.UI.Text.MarkerType.UnicodeSequence when IsValidUnicodeScalar(number) => char.ConvertFromUtf32(number),
				global::Microsoft.UI.Text.MarkerType.CircledNumber => ToCircledNumber(number, black: false),
				global::Microsoft.UI.Text.MarkerType.BlackCircleWingding => ToCircledNumber(number, black: false),
				global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding => ToCircledNumber(number, black: true),
				global::Microsoft.UI.Text.MarkerType.ArabicWide => ToLocalizedDigits(number, '０'),
				global::Microsoft.UI.Text.MarkerType.SimplifiedChinese => ToChineseNumber(number, useTens: true),
				global::Microsoft.UI.Text.MarkerType.TraditionalChinese => number is >= 10 and <= 19 ? ToChineseNumber(number, useTens: true) : ToChineseNumber(number, useTens: false),
				global::Microsoft.UI.Text.MarkerType.JapanSimplifiedChinese => ToChineseNumber(number, useTens: false),
				global::Microsoft.UI.Text.MarkerType.JapanKorea => ToChineseNumber(number, useTens: false),
				global::Microsoft.UI.Text.MarkerType.ArabicDictionary => ToAlphabetic(number, "أبتثجحخدذرزسشصضطظعغفقكلمنهوي"),
				global::Microsoft.UI.Text.MarkerType.ArabicAbjad => ToAlphabetic(number, "أبجدهوزحطيكلمنسعفصقرشتثخذضظغ"),
				global::Microsoft.UI.Text.MarkerType.Hebrew => ToAlphabetic(number, "אבגדהוזחטיכלמנסעפצקרשת"),
				global::Microsoft.UI.Text.MarkerType.ThaiAlphabetic => ToAlphabetic(number, "กขคงจฉชซญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรลวศษสหฬอฮ"),
				global::Microsoft.UI.Text.MarkerType.ThaiNumeric => ToLocalizedDigits(number, '๐'),
				global::Microsoft.UI.Text.MarkerType.DevanagariVowel => ToAlphabetic(number, "अआइईउऊऋॠऌॡएऐओऔ"),
				global::Microsoft.UI.Text.MarkerType.DevanagariConsonant => ToAlphabetic(number, "कखगघङचछजझञटठडढणतथदधनपफबभमयरलवशषसह"),
				global::Microsoft.UI.Text.MarkerType.DevanagariNumeric => ToLocalizedDigits(number, '०'),
				_ => string.Empty,
			};
			if (type == global::Microsoft.UI.Text.MarkerType.Bullet || value.Length == 0)
			{
				return value;
			}

			return style switch
			{
				global::Microsoft.UI.Text.MarkerStyle.Parenthesis => value + ")",
				global::Microsoft.UI.Text.MarkerStyle.Parentheses => "(" + value + ")",
				global::Microsoft.UI.Text.MarkerStyle.Plain => value,
				global::Microsoft.UI.Text.MarkerStyle.Minus => value + "-",
				_ => value + (type == global::Microsoft.UI.Text.MarkerType.JapanSimplifiedChinese ? "．" : "."),
			};
		}

		private static bool IsValidUnicodeScalar(int value)
			=> value is >= 0 and <= 0x10ffff && value is not >= 0xd800 and <= 0xdfff;

		private static string ToLocalizedDigits(int number, char zero)
		{
			var invariant = Math.Max(0, number).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
			var builder = new StringBuilder(invariant.Length);
			foreach (var digit in invariant)
			{
				builder.Append((char)(zero + digit - '0'));
			}
			return builder.ToString();
		}

		private static string ToAlphabetic(int number, string alphabet)
		{
			number = Math.Max(1, number);
			var builder = new StringBuilder();
			while (number > 0)
			{
				number--;
				builder.Insert(0, alphabet[number % alphabet.Length]);
				number /= alphabet.Length;
			}
			return builder.ToString();
		}

		private static string ToCircledNumber(int number, bool black)
		{
			if (number == 0)
			{
				return black ? "⓿" : "⓪";
			}
			if (black && number is >= 1 and <= 10)
			{
				return char.ConvertFromUtf32(0x2776 + number - 1);
			}
			if (number is >= 1 and <= 20)
			{
				return char.ConvertFromUtf32(0x2460 + number - 1);
			}
			return number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
		}

		private static string ToChineseNumber(int number, bool useTens)
		{
			if (number <= 0 || number >= 100 || !useTens)
			{
				var digits = Math.Max(0, number).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
				var builder = new StringBuilder(digits.Length);
				foreach (var digit in digits)
				{
					builder.Append("〇一二三四五六七八九"[digit - '0']);
				}
				return builder.ToString();
			}

			if (number < 10)
			{
				return "一二三四五六七八九"[number - 1].ToString();
			}

			var tens = number / 10;
			var ones = number % 10;
			return (tens == 1 ? string.Empty : "一二三四五六七八九"[tens - 1].ToString())
				+ "十"
				+ (ones == 0 ? string.Empty : "一二三四五六七八九"[ones - 1].ToString());
		}

		private static string ToLetters(int number, bool upper)
		{
			var builder = new StringBuilder();
			number = Math.Max(1, number);
			while (number > 0)
			{
				number--;
				builder.Insert(0, (char)((upper ? 'A' : 'a') + number % 26));
				number /= 26;
			}
			return builder.ToString();
		}

		private static string ToRoman(int number)
		{
			if (number is < 1 or > 3999)
			{
				return number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
			}
			var values = new (int value, string text)[]
			{
				(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
				(50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
			};
			var builder = new StringBuilder();
			foreach (var (value, text) in values)
			{
				while (number >= value)
				{
					builder.Append(text);
					number -= value;
				}
			}
			return builder.ToString();
		}

		private static bool HasMixedParagraphAlignments(
			IReadOnlyList<ParagraphRun> runs,
			ParagraphFormatState terminalParagraph,
			string text)
		{
			if (runs.Count == 0)
			{
				return false;
			}

			var alignment = runs[0].Format.Alignment;
			if (global::Microsoft.UI.Text.TextUnitNavigation.EndsInParagraphBreak(text)
				&& terminalParagraph.Alignment != alignment)
			{
				return true;
			}

			for (var i = 1; i < runs.Count; i++)
			{
				if (runs[i].Format.Alignment != alignment)
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasVisualParagraphFormatting(
			IReadOnlyList<ParagraphRun> runs,
			ParagraphFormatState terminalParagraph)
		{
			foreach (var run in runs)
			{
				var format = run.Format;
				if (format.FirstLineIndent != 0
					|| format.LeftIndent != 0
					|| format.RightIndent != 0
					|| format.SpaceBefore != 0
					|| format.SpaceAfter != 0
					|| format.RightToLeft
					|| format.LineSpacingRule is not global::Microsoft.UI.Text.LineSpacingRule.Single
						and not global::Microsoft.UI.Text.LineSpacingRule.Undefined
					|| format.ListType is not global::Microsoft.UI.Text.MarkerType.None
						and not global::Microsoft.UI.Text.MarkerType.Undefined)
				{
					return true;
				}
			}

			return HasVisualParagraphFormatting(terminalParagraph);
		}

		private static bool HasVisualParagraphFormatting(ParagraphFormatState format)
			=> format.FirstLineIndent != 0
				|| format.LeftIndent != 0
				|| format.RightIndent != 0
				|| format.SpaceBefore != 0
				|| format.SpaceAfter != 0
				|| format.RightToLeft
				|| format.LineSpacingRule is not global::Microsoft.UI.Text.LineSpacingRule.Single
					and not global::Microsoft.UI.Text.LineSpacingRule.Undefined
				|| format.ListType is not global::Microsoft.UI.Text.MarkerType.None
					and not global::Microsoft.UI.Text.MarkerType.Undefined;

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
