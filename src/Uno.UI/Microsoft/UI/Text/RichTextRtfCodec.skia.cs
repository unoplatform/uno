#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.UI.Text
{
	// Standard RTF transport for the managed RichEditBox model. The supported subset covers Unicode
	// text, the persisted character/paragraph properties, and friendly-name hyperlinks. Unsupported
	// destinations retain bounded opaque metadata while the modeled text remains available for rendering.
	internal static partial class RichTextRtfCodec
	{
		internal const int MaxRtfInputLength = 16 * 1024 * 1024;
		internal const int MaxRtfOutputLength = 16 * 1024 * 1024;
		private const int MaxGroupDepth = 256;
		private const int MaxParsedGroups = 65_536;
		private const int MaxFontNameLength = 256;
		private const int MaxParsedFonts = 4096;
		private const int MaxParsedColors = 4096;
		private const int MaxParsedLists = 4096;
		private const int MaxListLevels = 9;
		private const int HardMaxParsedCharacters = MaxRtfInputLength;
		private const int MaxParsedControlTokens = 262_144;
		private const int MaxParsedFormatRuns = 65_536;
		private const int MaxPreservedRtfEntries = 4096;
		private const int MaxPreservedRtfGroupLength = 256 * 1024;
		private const int MaxPreservedRtfTotalLength = 2 * 1024 * 1024;
		private const int MaxParsedImages = 128;
		private const int MaxParsedImageBytes = 16 * 1024 * 1024;
		private const long MaxParsedImagePixels = 32L * 1024 * 1024;
		private const int MaxCharacterMetadataLength = 4 * 1024;
		private const int MaxParagraphMetadataLength = 16 * 1024;
		private const int MaxInlineImageMetadataLength = 72 * 1024;
		private const int MaxObjectMetadataLength = InlineImageState.MaxAlternateTextLength;
		private const int MaxObjectResultTextLength = 64 * 1024;
		private const int MaxFieldInstructionLength = 16 * 1024;
		private const int MaxEncodedLanguageTagLength = 2 * 1024;
		private const int MaxEncodedParagraphTabsLength = 8 * 1024;
		private const float MaxParagraphMetric = 4096;
		[ThreadStatic]
		private static int _controlWordStringAllocationCount;

		internal static int ControlWordStringAllocationCount => _controlWordStringAllocationCount;

		internal static void ResetParserDiagnosticsForTesting() => _controlWordStringAllocationCount = 0;

		internal static string Write(RichTextFragment fragment, int maxOutputLength = MaxRtfOutputLength)
		{
			if (!fragment.AreRunInvariantsValid())
			{
				throw new ArgumentException("The rich-text formatting runs are inconsistent.", nameof(fragment));
			}

			var lists = CollectLists(fragment.ParagraphRuns, fragment.TerminalParagraphState);
			var fonts = CollectFonts(fragment.CharacterRuns, lists.Keys);
			var colors = CollectColors(fragment.CharacterRuns);
			var builder = new BoundedRtfBuilder(maxOutputLength);
			builder.Append(@"{\rtf1\ansi\deff0");
			AppendFontTable(builder, fonts);
			AppendColorTable(builder, colors);
			AppendListTables(builder, lists, fonts);
			builder.Append(@"\viewkind4\uc1 ");

			string? openLink = null;
			string? openLinkAnchor = null;
			CharacterFormatState? previousCharacter = null;
			ParagraphFormatState? previousParagraph = null;
			var listMarkerState = new ParagraphListMarkerState();
			var preservedEntries = fragment.PreservedRtfMetadata.Entries;
			var preservedIndex = 0;
			var characterRunIndex = 0;
			var paragraphRunIndex = 0;
			var characterRunEnd = fragment.CharacterRuns.Count == 0 ? 0 : fragment.CharacterRuns[0].Length;
			var paragraphRunEnd = fragment.ParagraphRuns.Count == 0 ? 0 : fragment.ParagraphRuns[0].Length;
			var position = 0;
			while (position < fragment.Text.Length)
			{
				var character = fragment.CharacterRuns[characterRunIndex].Format;
				var paragraph = fragment.ParagraphRuns[paragraphRunIndex].Format;
				var segmentEnd = Math.Min(characterRunEnd, paragraphRunEnd);
				var paragraphTransition = previousParagraph is null || !previousParagraph.Equals(paragraph);
				if (character.InlineImage is { } image)
				{
					if (openLink is not null)
					{
						builder.Append("}}");
						openLink = null;
						openLinkAnchor = null;
					}

					for (; position < segmentEnd; position++)
					{
						var replacesProjectedCharacter = AppendPreservedRtf(position);
						var isParagraphStart = position == 0 || fragment.Text[position - 1] == '\r';
						if (paragraphTransition || (isParagraphStart && HasList(paragraph)))
						{
							AppendParagraphControls(builder, paragraph, lists, listMarkerState, isParagraphStart);
							previousParagraph = paragraph;
							paragraphTransition = false;
						}

						if (!replacesProjectedCharacter)
						{
							AppendInlineImage(builder, image);
						}
					}

					previousCharacter = null;
				}
				else
				{
					var nextLink = IsSafeHyperlink(character.Link) ? character.Link : null;
					var nextLinkAnchor = nextLink is null ? null : character.LinkAnchor;
					if (!string.Equals(openLink, nextLink, StringComparison.Ordinal)
						|| !string.Equals(openLinkAnchor, nextLinkAnchor, StringComparison.Ordinal))
					{
						if (openLink is not null)
						{
							builder.Append("}}");
						}

						openLink = nextLink;
						openLinkAnchor = nextLinkAnchor;
						if (openLink is not null)
						{
							builder.Append(@"{\field{\*\fldinst HYPERLINK ");
							AppendInstruction(builder, openLink);
							if (!string.IsNullOrEmpty(openLinkAnchor))
							{
								builder.Append(@" \l ");
								AppendQuotedInstruction(builder, openLinkAnchor);
							}
							builder.Append(@"}{\fldrslt ");
						}
					}

					var characterTransition = previousCharacter is null || !previousCharacter.Equals(character);
					for (; position < segmentEnd; position++)
					{
						var replacesProjectedCharacter = AppendPreservedRtf(position);
						var isParagraphStart = position == 0 || fragment.Text[position - 1] == '\r';
						if (paragraphTransition || (isParagraphStart && HasList(paragraph)))
						{
							AppendParagraphControls(builder, paragraph, lists, listMarkerState, isParagraphStart);
							previousParagraph = paragraph;
							paragraphTransition = false;
						}

						if (characterTransition)
						{
							AppendCharacterControls(builder, character, fonts, colors);
							previousCharacter = character;
							characterTransition = false;
						}

						if (!replacesProjectedCharacter)
						{
							AppendTextCharacter(builder, fragment.Text[position]);
						}
					}
				}

				if (position == characterRunEnd)
				{
					characterRunIndex++;
					if (characterRunIndex < fragment.CharacterRuns.Count)
					{
						characterRunEnd = checked(characterRunEnd + fragment.CharacterRuns[characterRunIndex].Length);
					}
				}
				if (position == paragraphRunEnd)
				{
					paragraphRunIndex++;
					if (paragraphRunIndex < fragment.ParagraphRuns.Count)
					{
						paragraphRunEnd = checked(paragraphRunEnd + fragment.ParagraphRuns[paragraphRunIndex].Length);
					}
				}
			}

			if (openLink is not null)
			{
				builder.Append("}}");
			}
			AppendPreservedRtf(fragment.Text.Length);

			if (previousParagraph is null || !previousParagraph.Equals(fragment.TerminalParagraphState))
			{
				AppendParagraphControls(builder, fragment.TerminalParagraphState, lists, listMarkerState, includeListText: false);
			}
			builder.Append(@"{\*\unoterminal}");

			builder.Append('}');
			return builder.ToString();

			bool AppendPreservedRtf(int anchor)
			{
				var replacesProjectedCharacter = false;
				while (preservedIndex < preservedEntries.Count && preservedEntries[preservedIndex].Anchor == anchor)
				{
					var entry = preservedEntries[preservedIndex++];
					builder.Append(entry.Rtf);
					if (entry.Rtf.Length > 0 && char.IsLetterOrDigit(entry.Rtf[^1]))
					{
						builder.Append(' ');
					}
					replacesProjectedCharacter |= entry.ProjectedLength != 0;
				}
				return replacesProjectedCharacter;
			}
		}

		internal static RichTextFragment Read(
			string rtf,
			int maxCharacters = HardMaxParsedCharacters,
			bool truncateAtLimit = false)
		{
			if (string.IsNullOrWhiteSpace(rtf)
				|| rtf.Length > MaxRtfInputLength)
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			return ReadCore(rtf, maxCharacters, truncateAtLimit);
		}

		internal static RichTextFragment Read(
			byte[] rtf,
			int maxCharacters = HardMaxParsedCharacters,
			bool truncateAtLimit = false)
		{
			ArgumentNullException.ThrowIfNull(rtf);
			if (rtf.Length == 0 || rtf.Length > MaxRtfInputLength)
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			return ReadCore(Encoding.Latin1.GetString(rtf), maxCharacters, truncateAtLimit);
		}

		private static RichTextFragment ReadCore(string rtf, int maxCharacters, bool truncateAtLimit)
		{
			var (rootStart, rootEnd) = ValidateFraming(rtf);
			var (fonts, defaultFontIndex, documentCodePage) = ParseFonts(rtf, new ParseWorkBudget());
			var defaultFontName = defaultFontIndex is { } fontIndex && fonts.TryGetValue(fontIndex, out var defaultFont)
				? defaultFont.Name
				: null;
			var defaultCodePage = defaultFontIndex is { } defaultIndex && fonts.TryGetValue(defaultIndex, out defaultFont)
				? defaultFont.CodePage ?? documentCodePage
				: documentCodePage;
			maxCharacters = Math.Clamp(maxCharacters, 0, HardMaxParsedCharacters);
			var budget = new ParseBudget(maxCharacters, truncateAtLimit);
			var colors = ParseColors(rtf, new ParseWorkBudget());
			var lists = ParseLists(rtf, fonts, new ParseWorkBudget());
			var workBudget = new ParseWorkBudget();
			var output = new ParsedFragmentBuilder();
			var initialState = new ParserState
			{
				Character = new CharacterFormatState { Name = defaultFontName },
				DefaultFontName = defaultFontName,
				DefaultFontIndex = defaultFontIndex,
				CurrentFontIndex = defaultFontIndex,
				DocumentCodePage = documentCodePage,
			};
			initialState.SetCodePage(defaultCodePage);
			var stack = new List<ParserFrame>
			{
				new(initialState),
			};
			var terminalParagraph = new ParagraphFormatState();
			var imageBytes = 0;
			long imagePixels = 0;
			var groupCount = 0;

			for (var i = rootStart; i <= rootEnd; i++)
			{
				var value = rtf[i];
				if (value == '{')
				{
					FlushDecoder(stack[stack.Count - 1], output, budget);
					if (++groupCount > MaxParsedGroups)
					{
						throw new ArgumentException("The RTF contains too many groups.", nameof(rtf));
					}

					if (stack.Count >= MaxGroupDepth)
					{
						throw new ArgumentException("The RTF group nesting is too deep.", nameof(rtf));
					}

					var parent = stack[stack.Count - 1];
					var child = parent.CreateChild();
					child.GroupStart = i;
					child.ProjectedStart = output.TextLength;
					child.UnicodeFallbackRemaining = parent.UnicodeFallbackRemaining;
					parent.UnicodeFallbackRemaining = 0;
					stack.Add(child);
					continue;
				}

				if (value == '}')
				{
					FlushDecoder(stack[stack.Count - 1], output, budget);
					if (stack.Count == 1)
					{
						throw new ArgumentException("The RTF contains an unmatched closing group.", nameof(rtf));
					}

					if (stack.Count == 2)
					{
						terminalParagraph = stack[stack.Count - 1].State.Paragraph.Clone();
					}
					stack[stack.Count - 1].GroupEnd = i;

					CloseFrame(
						rtf,
						stack,
						output,
						ref imageBytes,
						ref imagePixels,
						budget);
					continue;
				}

				if (value == '\\')
				{
					ParseControl(rtf, ref i, stack, fonts, colors, lists, output, budget, workBudget);
					continue;
				}

				if (value is '\r' or '\n')
				{
					FlushDecoder(stack[stack.Count - 1], output, budget);
					continue;
				}

				var frame = stack[stack.Count - 1];
				if (value <= byte.MaxValue && (value >= 0x80 || frame.State.DecoderHasPendingBytes))
				{
					AppendEncodedByte((byte)value, frame, output, budget);
				}
				else
				{
					FlushDecoder(frame, output, budget);
					AppendParsedCharacter(value, frame, output, budget: budget);
				}
			}

			if (stack.Count != 1)
			{
				throw new ArgumentException("The RTF contains an unterminated group.", nameof(rtf));
			}

			return output.Build(terminalParagraph, !budget.WasTruncated);
		}

		private static (int RootStart, int RootEnd) ValidateFraming(string rtf)
		{
			var rootStart = 0;
			while (rootStart < rtf.Length && char.IsWhiteSpace(rtf[rootStart]))
			{
				rootStart++;
			}

			if (rootStart >= rtf.Length || rtf[rootStart] != '{')
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			var headerPosition = rootStart + 1;
			if (!TryReadControlWord(rtf, ref headerPosition, out var header, out var hasVersion, out var version)
				|| !header.SequenceEqual("rtf")
				|| !hasVersion
				|| version <= 0)
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			var depth = 0;
			var rootEnd = -1;
			for (var i = rootStart; i < rtf.Length; i++)
			{
				switch (rtf[i])
				{
					case '\\':
						SkipControl(rtf, ref i);
						break;
					case '{':
						depth++;
						break;
					case '}':
						if (--depth < 0)
						{
							throw new ArgumentException("The RTF contains an unmatched closing group.", nameof(rtf));
						}
						if (depth == 0)
						{
							rootEnd = i;
							i = rtf.Length;
						}
						break;
				}
			}

			if (rootEnd < 0)
			{
				throw new ArgumentException("The RTF contains an unterminated group.", nameof(rtf));
			}

			for (var i = rootEnd + 1; i < rtf.Length; i++)
			{
				if (rtf[i] != '\0' && !char.IsWhiteSpace(rtf[i]))
				{
					throw new ArgumentException("The RTF contains data outside its root group.", nameof(rtf));
				}
			}

			return (rootStart, rootEnd);
		}

		private static Dictionary<string, int> CollectFonts(
			IReadOnlyList<FormatRun> runs,
			IEnumerable<RtfListKey> lists)
		{
			var fonts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Segoe UI"] = 0 };
			foreach (var run in runs)
			{
				var state = run.Format;
				if (!string.IsNullOrEmpty(state.Name) && !fonts.ContainsKey(state.Name))
				{
					fonts[state.Name] = fonts.Count;
				}
			}
			foreach (var list in lists)
			{
				if (GetMarkerFontName(list.Type) is { } markerFont && !fonts.ContainsKey(markerFont))
				{
					fonts[markerFont] = fonts.Count;
				}
			}

			return fonts;
		}

		private static Dictionary<global::Windows.UI.Color, int> CollectColors(IReadOnlyList<FormatRun> runs)
		{
			var colors = new Dictionary<global::Windows.UI.Color, int>();
			foreach (var run in runs)
			{
				var state = run.Format;
				if (state.Foreground is { } color && !colors.ContainsKey(color))
				{
					colors[color] = colors.Count + 1;
				}
				if (state.Background is { } background && !colors.ContainsKey(background))
				{
					colors[background] = colors.Count + 1;
				}
			}

			return colors;
		}

		private static Dictionary<RtfListKey, RtfListInfo> CollectLists(
			IReadOnlyList<ParagraphRun> runs,
			ParagraphFormatState terminalParagraph)
		{
			var lists = new Dictionary<RtfListKey, RtfListInfo>();
			foreach (var run in runs)
			{
				AddList(run.Format);
			}
			AddList(terminalParagraph);
			return lists;

			void AddList(ParagraphFormatState state)
			{
				if (!HasList(state))
				{
					return;
				}

				var key = RtfListKey.FromState(state);
				if (!lists.ContainsKey(key))
				{
					if (lists.Count >= MaxParsedLists)
					{
						throw new ArgumentException("The rich text contains too many list definitions.", nameof(runs));
					}

					var id = lists.Count + 1;
					lists.Add(key, new RtfListInfo(id, id));
				}
			}
		}

		private static bool HasList(ParagraphFormatState state)
			=> state.ListType is not global::Microsoft.UI.Text.MarkerType.None
				and not global::Microsoft.UI.Text.MarkerType.Undefined
				&& state.ListLevelIndex >= 0;

		private static void AppendListTables(
			BoundedRtfBuilder builder,
			Dictionary<RtfListKey, RtfListInfo> lists,
			Dictionary<string, int> fonts)
		{
			if (lists.Count == 0)
			{
				return;
			}

			builder.Append(@"{\*\listtable");
			foreach (var pair in lists)
			{
				var key = pair.Key;
				var info = pair.Value;
				builder.Append(@"{\list\listtemplateid").Append(info.ListId).Append(@"\listhybrid");
				var levelCount = Math.Clamp(key.Level + 1, 1, MaxListLevels);
				for (var level = 0; level < levelCount; level++)
				{
					AppendListLevel(builder, key, level, fonts);
				}
				builder.Append(@"{\listname ;}\listid").Append(info.ListId).Append("}");
			}
			builder.Append("}");

			builder.Append(@"{\*\listoverridetable");
			foreach (var info in lists.Values)
			{
				builder.Append(@"{\listoverride\listid").Append(info.ListId)
					.Append(@"\listoverridecount0\ls").Append(info.OverrideId).Append("}");
			}
			builder.Append("}");
		}

		private static void AppendListLevel(
			BoundedRtfBuilder builder,
			RtfListKey key,
			int level,
			Dictionary<string, int> fonts)
		{
			var numberFormat = GetRtfNumberFormat(key.Type);
			var alignment = key.Alignment switch
			{
				global::Microsoft.UI.Text.MarkerAlignment.Center => 1,
				global::Microsoft.UI.Text.MarkerAlignment.Right => 2,
				_ => 0,
			};
			var start = key.Type == global::Microsoft.UI.Text.MarkerType.UnicodeSequence
				? 1
				: Math.Max(1, key.Start);
			var tab = key.Tab > 0
				? Math.Max(1, (int)Math.Round(key.Tab * 20, MidpointRounding.AwayFromZero))
				: 720 * (level + 1);
			builder.Append(@"{\listlevel\levelnfc").Append(numberFormat)
				.Append(@"\levelnfcn").Append(numberFormat)
				.Append(@"\leveljc").Append(alignment)
				.Append(@"\leveljcn").Append(alignment)
				.Append(@"\levelfollow0\levelstartat").Append(start)
				.Append(@"\levelspace0\levelindent0");
			AppendLevelText(builder, key, fonts);
			builder.Append(@"\fi-360\li").Append(tab).Append(@"\lin").Append(tab)
				.Append(@"\tx").Append(tab).Append("}");
		}

		private static void AppendLevelText(
			BoundedRtfBuilder builder,
			RtfListKey key,
			Dictionary<string, int> fonts)
		{
			var markerFontIndex = GetMarkerFontName(key.Type) is { } markerFont
				&& fonts.TryGetValue(markerFont, out var parsedMarkerFontIndex)
					? parsedMarkerFontIndex
					: (int?)null;
			if (key.Type == global::Microsoft.UI.Text.MarkerType.Bullet)
			{
				builder.Append(@"{\leveltext\'");
				AppendLevelTextMarker(builder, "\u2022", markerFontIndex);
				builder.Append(@";}{\levelnumbers;}");
				return;
			}
			if (key.Type is global::Microsoft.UI.Text.MarkerType.BlackCircleWingding
				or global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding)
			{
				var marker = key.Type == global::Microsoft.UI.Text.MarkerType.BlackCircleWingding ? "l" : "n";
				builder.Append(@"{\leveltext\'");
				AppendLevelTextMarker(builder, marker, markerFontIndex);
				builder.Append(@";}{\levelnumbers;}");
				return;
			}
			if (key.Type == global::Microsoft.UI.Text.MarkerType.UnicodeSequence)
			{
				var scalar = global::Microsoft.UI.Xaml.Controls.RichEditBox.IsValidListMarkerUnicodeScalar(key.Start)
					? key.Start
					: 0x2022;
				var marker = char.ConvertFromUtf32(scalar);
				builder.Append(@"{\leveltext\'");
				AppendLevelTextMarker(builder, marker, markerFontIndex);
				builder.Append(@";}{\levelnumbers;}");
				return;
			}

			switch (key.Style)
			{
				case global::Microsoft.UI.Text.MarkerStyle.Parentheses:
					builder.Append(@"{\leveltext\'03(\'00);}{\levelnumbers\'02;}");
					break;
				case global::Microsoft.UI.Text.MarkerStyle.Parenthesis:
					builder.Append(@"{\leveltext\'02\'00);}{\levelnumbers\'01;}");
					break;
				case global::Microsoft.UI.Text.MarkerStyle.Plain:
				case global::Microsoft.UI.Text.MarkerStyle.NoNumber:
					builder.Append(@"{\leveltext\'01\'00;}{\levelnumbers\'01;}");
					break;
				case global::Microsoft.UI.Text.MarkerStyle.Minus:
					builder.Append(@"{\leveltext\'02\'00-;}{\levelnumbers\'01;}");
					break;
				default:
					builder.Append(@"{\leveltext\'02\'00.;}{\levelnumbers\'01;}");
					break;
			}
		}

		private static void AppendLevelTextMarker(
			BoundedRtfBuilder builder,
			string marker,
			int? fontIndex)
		{
			builder.Append(marker.Length.ToString("x2", CultureInfo.InvariantCulture));
			if (fontIndex is { } value)
			{
				builder.Append(@"\f").Append(value).Append(' ');
			}
			foreach (var character in marker)
			{
				AppendTextCharacter(builder, character);
			}
		}

		private static string? GetMarkerFontName(global::Microsoft.UI.Text.MarkerType type)
			=> type switch
			{
				global::Microsoft.UI.Text.MarkerType.BlackCircleWingding
					or global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding => "Wingdings",
				global::Microsoft.UI.Text.MarkerType.Bullet
					or global::Microsoft.UI.Text.MarkerType.UnicodeSequence => "Segoe UI Symbol",
				_ => null,
			};

		private static int GetRtfNumberFormat(global::Microsoft.UI.Text.MarkerType type)
			=> type switch
			{
				global::Microsoft.UI.Text.MarkerType.UppercaseRoman => 1,
				global::Microsoft.UI.Text.MarkerType.LowercaseRoman => 2,
				global::Microsoft.UI.Text.MarkerType.UppercaseEnglishLetter => 3,
				global::Microsoft.UI.Text.MarkerType.LowercaseEnglishLetter => 4,
				global::Microsoft.UI.Text.MarkerType.CircledNumber => 18,
				global::Microsoft.UI.Text.MarkerType.Bullet => 23,
				global::Microsoft.UI.Text.MarkerType.UnicodeSequence => 23,
				global::Microsoft.UI.Text.MarkerType.BlackCircleWingding => 23,
				global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding => 23,
				global::Microsoft.UI.Text.MarkerType.ArabicWide => 14,
				global::Microsoft.UI.Text.MarkerType.SimplifiedChinese => 38,
				global::Microsoft.UI.Text.MarkerType.TraditionalChinese => 34,
				global::Microsoft.UI.Text.MarkerType.JapanSimplifiedChinese => 10,
				global::Microsoft.UI.Text.MarkerType.JapanKorea => 41,
				global::Microsoft.UI.Text.MarkerType.ArabicDictionary => 46,
				global::Microsoft.UI.Text.MarkerType.ArabicAbjad => 48,
				global::Microsoft.UI.Text.MarkerType.Hebrew => 45,
				global::Microsoft.UI.Text.MarkerType.ThaiAlphabetic => 53,
				global::Microsoft.UI.Text.MarkerType.ThaiNumeric => 54,
				global::Microsoft.UI.Text.MarkerType.DevanagariVowel => 49,
				global::Microsoft.UI.Text.MarkerType.DevanagariConsonant => 50,
				global::Microsoft.UI.Text.MarkerType.DevanagariNumeric => 51,
				_ => 0,
			};

		private static void AppendFontTable(BoundedRtfBuilder builder, Dictionary<string, int> fonts)
		{
			builder.Append(@"{\fonttbl");
			foreach (var pair in fonts)
			{
				if (!IsSafeRtfFontName(pair.Key) || pair.Key.Contains(';'))
				{
					throw new ArgumentException("The rich text font name is invalid.", nameof(fonts));
				}

				builder.Append(@"{\f").Append(pair.Value)
					.Append(@"\fnil\fcharset")
					.Append(IsSymbolFont(pair.Key) ? 2 : 0)
					.Append(' ');
				AppendEscapedAscii(builder, pair.Key);
				builder.Append(";}");
			}
			builder.Append('}');
		}

		private static bool IsSymbolFont(string name)
			=> name.StartsWith("Wingdings", StringComparison.OrdinalIgnoreCase)
				|| name.StartsWith("Webdings", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "Symbol", StringComparison.OrdinalIgnoreCase);

		private static void AppendColorTable(BoundedRtfBuilder builder, Dictionary<global::Windows.UI.Color, int> colors)
		{
			builder.Append(@"{\colortbl ;");
			foreach (var pair in colors)
			{
				var color = pair.Key;
				builder.Append(@"\red").Append(color.R)
					.Append(@"\green").Append(color.G)
					.Append(@"\blue").Append(color.B).Append(';');
			}
			builder.Append('}');
		}

		private static void AppendCharacterControls(BoundedRtfBuilder builder, CharacterFormatState state, Dictionary<string, int> fonts, Dictionary<global::Windows.UI.Color, int> colors)
		{
			builder.Append(@"\plain");
			if (state.AllCaps)
			{
				builder.Append(@"\caps");
			}
			if (state.Bold)
			{
				builder.Append(@"\b");
			}
			else if (state.WeightExplicit)
			{
				builder.Append(@"\b0");
			}
			if (state.Hidden)
			{
				builder.Append(@"\v");
			}
			if (state.Italic)
			{
				builder.Append(@"\i");
			}
			if (state.Outline)
			{
				builder.Append(@"\outl");
			}
			if (state.ProtectedText)
			{
				builder.Append(@"\protect");
			}
			if (state.SmallCaps)
			{
				builder.Append(@"\scaps");
			}
			if (GetUnderlineControl(state.Underline) is { } underline)
			{
				builder.Append('\\').Append(underline);
			}
			if (state.Strikethrough)
			{
				builder.Append(@"\strike");
			}
			if (state.Superscript)
			{
				builder.Append(@"\super");
			}
			else if (state.Subscript)
			{
				builder.Append(@"\sub");
			}
			if (!string.IsNullOrEmpty(state.Name) && fonts.TryGetValue(state.Name, out var font))
			{
				builder.Append(@"\f").Append(font);
			}
			AppendLanguageAndScriptControls(builder, state);
			if (state.Size > 0)
			{
				builder.Append(@"\fs").Append(Math.Max(1, (int)Math.Round(state.Size * 2, MidpointRounding.AwayFromZero)));
			}
			if (state.Foreground is { } color && colors.TryGetValue(color, out var colorIndex))
			{
				builder.Append(@"\cf").Append(colorIndex);
			}
			if (state.Background is { } background && colors.TryGetValue(background, out var backgroundIndex))
			{
				builder.Append(@"\highlight").Append(backgroundIndex);
			}
			if (state.Spacing != 0)
			{
				builder.Append(@"\expndtw").Append((int)Math.Round(state.Spacing * 20, MidpointRounding.AwayFromZero));
			}
			if (state.Kerning != 0)
			{
				builder.Append(@"\kerning").Append((int)Math.Round(state.Kerning * 2, MidpointRounding.AwayFromZero));
			}
			if (state.Position > 0)
			{
				builder.Append(@"\up").Append((int)Math.Round(state.Position * 2, MidpointRounding.AwayFromZero));
			}
			else if (state.Position < 0)
			{
				builder.Append(@"\dn").Append((int)Math.Round(-state.Position * 2, MidpointRounding.AwayFromZero));
			}

			if (RequiresCharacterMetadata(state))
			{
				AppendCharacterMetadata(builder, state);
			}
			else
			{
				builder.Append(' ');
			}
		}

		private static string? GetUnderlineControl(global::Microsoft.UI.Text.UnderlineType underline)
			=> underline switch
			{
				global::Microsoft.UI.Text.UnderlineType.Single => "ul",
				global::Microsoft.UI.Text.UnderlineType.Words => "ulw",
				global::Microsoft.UI.Text.UnderlineType.Double => "uldb",
				global::Microsoft.UI.Text.UnderlineType.Dotted => "uld",
				global::Microsoft.UI.Text.UnderlineType.Dash => "uldash",
				global::Microsoft.UI.Text.UnderlineType.DashDot => "uldashd",
				global::Microsoft.UI.Text.UnderlineType.DashDotDot => "uldashdd",
				global::Microsoft.UI.Text.UnderlineType.Wave => "ulwave",
				global::Microsoft.UI.Text.UnderlineType.Thick => "ulth",
				global::Microsoft.UI.Text.UnderlineType.Thin => "ulhair",
				global::Microsoft.UI.Text.UnderlineType.DoubleWave => "ululdbwave",
				global::Microsoft.UI.Text.UnderlineType.HeavyWave => "ulhwave",
				global::Microsoft.UI.Text.UnderlineType.LongDash => "ulldash",
				global::Microsoft.UI.Text.UnderlineType.ThickDash => "ulthdash",
				global::Microsoft.UI.Text.UnderlineType.ThickDashDot => "ulthdashd",
				global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot => "ulthdashdd",
				global::Microsoft.UI.Text.UnderlineType.ThickDotted => "ulthd",
				global::Microsoft.UI.Text.UnderlineType.ThickLongDash => "ulthldash",
				_ => null,
			};

		private static bool RequiresCharacterMetadata(CharacterFormatState state)
			=> state.FontStretch != global::Windows.UI.Text.FontStretch.Normal
				|| RequiresLanguageMetadata(state.LanguageTag)
				|| RequiresScriptMetadata(state)
				|| state.Background is { A: < byte.MaxValue }
				|| !IsStandardWeight(state);

		private static void AppendLanguageAndScriptControls(BoundedRtfBuilder builder, CharacterFormatState state)
		{
			var hasLanguage = TryGetLanguageLcid(state.LanguageTag, out var lcid);
			if (hasLanguage)
			{
				if (IsEastAsianScript(state.TextScript))
				{
					builder.Append(@"\langfe").Append(lcid);
				}
				else
				{
					builder.Append(@"\lang").Append(lcid);
				}
			}

			builder.Append(state.TextScript switch
			{
				global::Microsoft.UI.Text.TextScript.Ansi => @"\loch",
				global::Microsoft.UI.Text.TextScript.ShiftJis
					or global::Microsoft.UI.Text.TextScript.GB2312
					or global::Microsoft.UI.Text.TextScript.Hangul
					or global::Microsoft.UI.Text.TextScript.Big5
					or global::Microsoft.UI.Text.TextScript.Jamo
					or global::Microsoft.UI.Text.TextScript.Yi => @"\dbch",
				global::Microsoft.UI.Text.TextScript.Hebrew
					or global::Microsoft.UI.Text.TextScript.Arabic
					or global::Microsoft.UI.Text.TextScript.Syriac
					or global::Microsoft.UI.Text.TextScript.Thaana
					or global::Microsoft.UI.Text.TextScript.NKo
					or global::Microsoft.UI.Text.TextScript.Osmanya => @"\rtlch",
				global::Microsoft.UI.Text.TextScript.Default => @"\ltrch",
				_ => @"\hich",
			});
		}

		private static bool RequiresLanguageMetadata(string languageTag)
			=> languageTag.Length != 0
				&& (!TryGetLanguageLcid(languageTag, out var lcid)
					|| !TryGetLanguageTag(lcid, out var roundTrip)
					|| !string.Equals(languageTag, roundTrip, StringComparison.OrdinalIgnoreCase));

		private static bool RequiresScriptMetadata(CharacterFormatState state)
		{
			if (state.TextScript is global::Microsoft.UI.Text.TextScript.Default
				or global::Microsoft.UI.Text.TextScript.Ansi)
			{
				return false;
			}

			return !TryGetLanguageLcid(state.LanguageTag, out var lcid)
				|| ResolveTextScript(lcid, fallback: global::Microsoft.UI.Text.TextScript.Default) != state.TextScript;
		}

		private static bool IsEastAsianScript(global::Microsoft.UI.Text.TextScript script)
			=> script is global::Microsoft.UI.Text.TextScript.ShiftJis
				or global::Microsoft.UI.Text.TextScript.GB2312
				or global::Microsoft.UI.Text.TextScript.Hangul
				or global::Microsoft.UI.Text.TextScript.Big5
				or global::Microsoft.UI.Text.TextScript.Jamo
				or global::Microsoft.UI.Text.TextScript.Yi;

		private static bool TryGetLanguageLcid(string languageTag, out int lcid)
		{
			lcid = 0;
			if (languageTag.Length == 0 || languageTag.Length > CharacterFormatState.MaxLanguageTagLength)
			{
				return false;
			}

			try
			{
				var culture = CultureInfo.GetCultureInfo(languageTag);
				lcid = culture.LCID;
				return lcid is > 0 and <= ushort.MaxValue
					&& lcid != CultureInfo.InvariantCulture.LCID
					&& lcid != 0x1000
					&& culture.Name.Length != 0;
			}
			catch (CultureNotFoundException)
			{
				return false;
			}
		}

		private static bool TryGetLanguageTag(int lcid, out string languageTag)
		{
			languageTag = string.Empty;
			if (lcid is <= 0 or > ushort.MaxValue || lcid == CultureInfo.InvariantCulture.LCID || lcid == 0x1000)
			{
				return false;
			}

			try
			{
				var culture = CultureInfo.GetCultureInfo(lcid);
				if (culture.Name.Length == 0 || culture.Name.Length > CharacterFormatState.MaxLanguageTagLength)
				{
					return false;
				}
				languageTag = culture.Name;
				return true;
			}
			catch (CultureNotFoundException)
			{
				return false;
			}
		}

		private static global::Microsoft.UI.Text.TextScript ResolveTextScript(
			int? lcid,
			global::Microsoft.UI.Text.TextScript fallback)
		{
			if (lcid is not { } value || !TryGetLanguageTag(value, out var languageTag))
			{
				return fallback;
			}

			CultureInfo culture;
			try
			{
				culture = CultureInfo.GetCultureInfo(languageTag);
			}
			catch (CultureNotFoundException)
			{
				return fallback;
			}

			var primary = languageTag.Split('-')[0];
			return primary switch
			{
				"he" or "yi" => global::Microsoft.UI.Text.TextScript.Hebrew,
				"ar" or "fa" or "ur" or "ps" => global::Microsoft.UI.Text.TextScript.Arabic,
				"syr" => global::Microsoft.UI.Text.TextScript.Syriac,
				"dv" => global::Microsoft.UI.Text.TextScript.Thaana,
				"th" => global::Microsoft.UI.Text.TextScript.Thai,
				"ja" => global::Microsoft.UI.Text.TextScript.ShiftJis,
				"ko" => global::Microsoft.UI.Text.TextScript.Hangul,
				"zh" when languageTag.Contains("Hant", StringComparison.OrdinalIgnoreCase)
					|| culture.TextInfo.ANSICodePage == 950 => global::Microsoft.UI.Text.TextScript.Big5,
				"zh" => global::Microsoft.UI.Text.TextScript.GB2312,
				_ => culture.TextInfo.ANSICodePage switch
				{
					1250 => global::Microsoft.UI.Text.TextScript.EastEurope,
					1251 => global::Microsoft.UI.Text.TextScript.Cyrillic,
					1253 => global::Microsoft.UI.Text.TextScript.Greek,
					1254 => global::Microsoft.UI.Text.TextScript.Turkish,
					1255 => global::Microsoft.UI.Text.TextScript.Hebrew,
					1256 => global::Microsoft.UI.Text.TextScript.Arabic,
					1257 => global::Microsoft.UI.Text.TextScript.Baltic,
					1258 => global::Microsoft.UI.Text.TextScript.Vietnamese,
					874 => global::Microsoft.UI.Text.TextScript.Thai,
					932 => global::Microsoft.UI.Text.TextScript.ShiftJis,
					936 => global::Microsoft.UI.Text.TextScript.GB2312,
					949 => global::Microsoft.UI.Text.TextScript.Hangul,
					950 => global::Microsoft.UI.Text.TextScript.Big5,
					_ => fallback,
				},
			};
		}

		private static bool IsStandardWeight(CharacterFormatState state)
			=> !state.WeightExplicit && !state.Bold && state.Weight == 400
				|| state.WeightExplicit && !state.Bold && state.Weight == 400
				|| state.WeightExplicit && state.Bold && state.Weight == 700;

		private static void AppendCharacterMetadata(BoundedRtfBuilder builder, CharacterFormatState state)
		{
			if (state.LanguageTag.Length > CharacterFormatState.MaxLanguageTagLength)
			{
				throw new ArgumentException("The rich text language tag is too long.", nameof(state));
			}

			builder.Append(@"{\*\unochar ")
				.Append(state.AllCaps ? '1' : '0').Append(',')
				.Append(state.Background is { } background ? PackColor(background).ToString(CultureInfo.InvariantCulture) : "-").Append(',')
				.Append((int)state.FontStretch).Append(',')
				.Append(state.Hidden ? '1' : '0').Append(',')
				.Append(state.Kerning.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(state.LanguageTag))).Append(',')
				.Append(state.Outline ? '1' : '0').Append(',')
				.Append(state.Position.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.ProtectedText ? '1' : '0').Append(',')
				.Append(state.SmallCaps ? '1' : '0').Append(',')
				.Append(state.Spacing.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.Subscript ? '1' : '0').Append(',')
				.Append(state.Superscript ? '1' : '0').Append(',')
				.Append((int)state.TextScript).Append(',')
				.Append(state.Weight).Append(',')
				.Append((int)state.Underline).Append(',')
				.Append(state.WeightExplicit ? '1' : '0')
				.Append('}');
		}

		private static uint PackColor(global::Windows.UI.Color color)
			=> (uint)color.A << 24 | (uint)color.R << 16 | (uint)color.G << 8 | color.B;

		private static void AppendParagraphControls(
			BoundedRtfBuilder builder,
			ParagraphFormatState state,
			Dictionary<RtfListKey, RtfListInfo> lists,
			ParagraphListMarkerState markerState,
			bool includeListText)
		{
			builder.Append(@"\pard");
			builder.Append(state.Alignment switch
			{
				global::Microsoft.UI.Text.ParagraphAlignment.Center => @"\qc",
				global::Microsoft.UI.Text.ParagraphAlignment.Right => @"\qr",
				global::Microsoft.UI.Text.ParagraphAlignment.Justify => @"\qj",
				_ => @"\ql",
			});
			AppendTwips(builder, "fi", state.FirstLineIndent);
			AppendTwips(builder, "li", state.LeftIndent);
			AppendTwips(builder, "ri", state.RightIndent);
			AppendTwips(builder, "sb", state.SpaceBefore);
			AppendTwips(builder, "sa", state.SpaceAfter);
			AppendLineSpacing(builder, state);
			foreach (var tab in state.Tabs)
			{
				AppendTab(builder, tab);
			}
			builder.Append(state.RightToLeft ? @"\rtlpar" : @"\ltrpar");
			if (state.KeepTogether)
			{
				builder.Append(@"\keep");
			}
			if (state.KeepWithNext)
			{
				builder.Append(@"\keepn");
			}
			if (state.NoLineNumber)
			{
				builder.Append(@"\noline");
			}
			if (state.PageBreakBefore)
			{
				builder.Append(@"\pagebb");
			}
			builder.Append(state.WidowControl ? @"\widctlpar" : @"\nowidctlpar");
			if (HasList(state) && lists.TryGetValue(RtfListKey.FromState(state), out var list))
			{
				builder.Append(@"\ls").Append(list.OverrideId)
					.Append(@"\ilvl").Append(Math.Clamp(state.ListLevelIndex, 0, MaxListLevels - 1));
			}
			builder.Append(' ');
			if (includeListText)
			{
				var marker = ParagraphListMarker.GetNext(state, markerState, out var hasList);
				if (hasList)
				{
					builder.Append(@"{\listtext ");
					if (marker is not null)
					{
						AppendEscapedAscii(builder, marker);
					}
					builder.Append(@"\tab}");
					AppendLegacyListControls(builder, state, marker);
				}
			}
			AppendParagraphMetadata(builder, state);
		}

		private static void AppendLegacyListControls(
			BoundedRtfBuilder builder,
			ParagraphFormatState state,
			string? marker)
		{
			var markerControl = state.ListType switch
			{
				global::Microsoft.UI.Text.MarkerType.Bullet => @"\pnlvlblt",
				global::Microsoft.UI.Text.MarkerType.Arabic => @"\pndec",
				global::Microsoft.UI.Text.MarkerType.UppercaseEnglishLetter => @"\pnucltr",
				global::Microsoft.UI.Text.MarkerType.LowercaseEnglishLetter => @"\pnlcltr",
				global::Microsoft.UI.Text.MarkerType.UppercaseRoman => @"\pnucrm",
				global::Microsoft.UI.Text.MarkerType.LowercaseRoman => @"\pnlcrm",
				global::Microsoft.UI.Text.MarkerType.BlackCircleWingding => @"\pnbcnum",
				global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding => @"\pnwcnum",
				global::Microsoft.UI.Text.MarkerType.UnicodeSequence => @"\pnseq",
				_ => null,
			};
			if (markerControl is null)
			{
				return;
			}

			builder.Append(@"{\pntext ");
			if (marker is not null)
			{
				AppendEscapedAscii(builder, marker);
			}
			builder.Append(@"\tab}{\*\pn\pnlvlbody\pnindent")
				.Append(Math.Max(0, (int)Math.Round(state.ListTab * 20, MidpointRounding.AwayFromZero)))
				.Append(@"\pnstart")
				.Append(state.ListType == global::Microsoft.UI.Text.MarkerType.UnicodeSequence
					? global::Microsoft.UI.Xaml.Controls.RichEditBox.IsValidListMarkerUnicodeScalar(state.ListStart)
						? state.ListStart
						: 0x2022
					: Math.Max(1, state.ListStart))
				.Append(markerControl)
				.Append(" }");
		}

		private static void AppendLineSpacing(BoundedRtfBuilder builder, ParagraphFormatState state)
		{
			switch (state.LineSpacingRule)
			{
				case global::Microsoft.UI.Text.LineSpacingRule.Single:
					builder.Append(@"\sl240\slmult1");
					break;
				case global::Microsoft.UI.Text.LineSpacingRule.OneAndHalf:
					builder.Append(@"\sl360\slmult1");
					break;
				case global::Microsoft.UI.Text.LineSpacingRule.Double:
					builder.Append(@"\sl480\slmult1");
					break;
				case global::Microsoft.UI.Text.LineSpacingRule.Multiple:
					builder.Append(@"\sl").Append(Math.Max(1, (int)Math.Round(state.LineSpacing * 240, MidpointRounding.AwayFromZero)))
						.Append(@"\slmult1");
					break;
				case global::Microsoft.UI.Text.LineSpacingRule.AtLeast:
					builder.Append(@"\sl").Append(Math.Max(1, (int)Math.Round(state.LineSpacing * 20, MidpointRounding.AwayFromZero)))
						.Append(@"\slmult0");
					break;
				case global::Microsoft.UI.Text.LineSpacingRule.Exactly:
					builder.Append(@"\sl-").Append(Math.Max(1, (int)Math.Round(state.LineSpacing * 20, MidpointRounding.AwayFromZero)))
						.Append(@"\slmult0");
					break;
			}
		}

		private static void AppendTab(BoundedRtfBuilder builder, ParagraphTab tab)
		{
			builder.Append(tab.Alignment switch
			{
				global::Microsoft.UI.Text.TabAlignment.Center => @"\tqc",
				global::Microsoft.UI.Text.TabAlignment.Right => @"\tqr",
				global::Microsoft.UI.Text.TabAlignment.Decimal => @"\tqdec",
				global::Microsoft.UI.Text.TabAlignment.Bar => @"\tb",
				_ => string.Empty,
			});
			builder.Append(tab.Leader switch
			{
				global::Microsoft.UI.Text.TabLeader.Dots => @"\tldot",
				global::Microsoft.UI.Text.TabLeader.Dashes => @"\tlhyph",
				global::Microsoft.UI.Text.TabLeader.Lines => @"\tlul",
				global::Microsoft.UI.Text.TabLeader.ThickLines => @"\tlth",
				global::Microsoft.UI.Text.TabLeader.Equals => @"\tleq",
				_ => string.Empty,
			});
			builder.Append(@"\tx").Append(Math.Max(0, (int)Math.Round(tab.Position * 20, MidpointRounding.AwayFromZero)));
		}

		private static void AppendParagraphMetadata(BoundedRtfBuilder builder, ParagraphFormatState state)
		{
			builder.Append(@"{\*\unopara ")
				.Append((int)state.Alignment).Append(',')
				.Append(state.FirstLineIndent.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.LeftIndent.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.RightIndent.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.SpaceBefore.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.SpaceAfter.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append((int)state.LineSpacingRule).Append(',')
				.Append(state.LineSpacing.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append((int)state.ListType).Append(',')
				.Append((int)state.ListStyle).Append(',')
				.Append((int)state.ListAlignment).Append(',')
				.Append(state.ListLevelIndex).Append(',')
				.Append(state.ListStart).Append(',')
				.Append(state.ListTab.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(state.KeepTogether ? '1' : '0').Append(',')
				.Append(state.KeepWithNext ? '1' : '0').Append(',')
				.Append(state.NoLineNumber ? '1' : '0').Append(',')
				.Append(state.PageBreakBefore ? '1' : '0').Append(',')
				.Append(state.RightToLeft ? '1' : '0').Append(',')
				.Append(state.WidowControl ? '1' : '0').Append(',')
				.Append((int)state.Style).Append(',')
				.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(SerializeTabs(state.Tabs))))
				.Append('}');
		}

		private static string SerializeTabs(IReadOnlyList<ParagraphTab> tabs)
			=> string.Join(';', tabs.Select(static tab => string.Create(
				CultureInfo.InvariantCulture,
				$"{tab.Position}|{(int)tab.Alignment}|{(int)tab.Leader}")));

		private static void AppendTwips(BoundedRtfBuilder builder, string control, float value)
		{
			if (value != 0)
			{
				builder.Append('\\').Append(control).Append((int)Math.Round(value * 20, MidpointRounding.AwayFromZero));
			}
		}

		private static void AppendInstruction(BoundedRtfBuilder builder, string link)
		{
			var start = link.Length > 0 && link[0] == '\ufddf' ? 1 : 0;
			AppendEscapedAscii(builder, link, start);
		}

		private static bool IsSafeHyperlink(string? link)
		{
			if (string.IsNullOrEmpty(link))
			{
				return false;
			}

			var start = link[0] == '\ufddf' ? 1 : 0;
			if (link.Length - start < 2 || link[start] != '"' || link[^1] != '"')
			{
				return false;
			}

			var target = link.Substring(start + 1, link.Length - start - 2);
			if (Uri.TryCreate(target, UriKind.Absolute, out var absolute))
			{
				return absolute.Scheme is "http" or "https" or "mailto";
			}

			return !target.StartsWith('/')
				&& !target.StartsWith('\\')
				&& !target.Contains(':')
				&& Uri.TryCreate(target, UriKind.Relative, out _);
		}

		private static void AppendQuotedInstruction(BoundedRtfBuilder builder, string value)
		{
			builder.Append('"');
			AppendEscapedAscii(builder, value);
			builder.Append('"');
		}

		private static void AppendInlineImage(BoundedRtfBuilder builder, InlineImageState image)
		{
			if (image.IsObjectFallback)
			{
				image.Validate();
				builder.Append(@"{\*\unoobject ")
					.Append(image.Width).Append(',')
					.Append(image.Height).Append(',')
					.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(image.AlternateText)))
					.Append(@"}\u-4?");
				return;
			}

			var data = image.GetRtfEncodedData(out var control);
			builder.Append(@"{\*\unoimage ")
				.Append(image.Width).Append(',')
				.Append(image.Height).Append(',')
				.Append(image.Ascent).Append(',')
				.Append((int)image.VerticalAlignment).Append(',')
				.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(image.AlternateText))).Append('}');

			builder.Append(@"{\pict")
				.Append('\\').Append(control)
				.Append(@"\picw").Append(image.Width)
				.Append(@"\pich").Append(image.Height)
				.Append(@"\picwgoal").Append(image.Width * 15)
				.Append(@"\pichgoal").Append(image.Height * 15).Append(' ');
			const string hex = "0123456789abcdef";
			foreach (var value in data)
			{
				builder.Append(hex[value >> 4]).Append(hex[value & 0x0f]);
			}
			builder.Append('}');
		}

		private static void AppendEscapedAscii(BoundedRtfBuilder builder, string value, int start = 0)
		{
			for (var i = start; i < value.Length; i++)
			{
				AppendTextCharacter(builder, value[i]);
			}
		}

		private static void AppendTextCharacter(BoundedRtfBuilder builder, char value)
		{
			switch (value)
			{
				case '\\':
				case '{':
				case '}':
					builder.Append('\\').Append(value);
					break;
				case '\r':
					builder.Append(@"\par ");
					break;
				case '\n':
					builder.Append(@"\line ");
					break;
				case '\t':
					builder.Append(@"\tab ");
					break;
				default:
					if (value >= ' ' && value <= '~')
					{
						builder.Append(value);
					}
					else
					{
						builder.Append(@"\u").Append((short)value).Append('?');
					}
					break;
			}
		}

		private static (Dictionary<int, RtfFont> Fonts, int? DefaultFontIndex, int DocumentCodePage) ParseFonts(string rtf, ParseWorkBudget workBudget)
		{
			var documentCodePage = TryReadHeaderControl(rtf, "ansicpg", workBudget, out var parsedCodePage)
				? ValidateCodePage(parsedCodePage)
				: 1252;
			var fonts = new Dictionary<int, RtfFont>();
			var fontTableStart = FindDestinationGroup(rtf, "fonttbl", workBudget, out var fontTableEnd);
			if (fontTableStart >= 0)
			{
				var position = fontTableStart;
				while (position < fontTableEnd)
				{
					if (rtf[position] != '{')
					{
						position++;
						continue;
					}

					var entryEnd = FindGroupEnd(rtf, position, fontTableEnd, workBudget);
					if (entryEnd < 0)
					{
						throw new ArgumentException("The RTF font table is malformed.", nameof(rtf));
					}

					if (TryParseFontEntry(
						rtf.AsSpan(position + 1, entryEnd - position - 1),
						workBudget,
						out var index,
						out var name,
						out var codePage,
						out var charset)
						&& IsSafeRtfFontName(name))
					{
						if (fonts.Count >= MaxParsedFonts && !fonts.ContainsKey(index))
						{
							throw new ArgumentException("The RTF contains too many fonts.", nameof(rtf));
						}
						fonts[index] = new RtfFont(name, codePage, charset);
					}

					position = entryEnd + 1;
				}
			}

			int? defaultFontIndex = TryReadHeaderControl(rtf, "deff", workBudget, out var parsedDefaultFontIndex)
				&& fonts.ContainsKey(parsedDefaultFontIndex)
					? parsedDefaultFontIndex
					: fonts.ContainsKey(0) ? 0 : null;
			return (fonts, defaultFontIndex, documentCodePage);
		}

		private static int FindDestinationGroup(string rtf, string destination, ParseWorkBudget workBudget, out int groupEnd)
		{
			var depth = 0;
			for (var i = 0; i < rtf.Length; i++)
			{
				switch (rtf[i])
				{
					case '\\':
						workBudget.RecordControl();
						SkipControl(rtf, ref i);
						break;
					case '{':
						depth++;
						var probe = i + 1;
						if (probe + 1 < rtf.Length && rtf[probe] == '\\' && rtf[probe + 1] == '*')
						{
							workBudget.RecordControl();
							probe += 2;
						}
						if (probe < rtf.Length && rtf[probe] == '\\')
						{
							workBudget.RecordControl();
							if (TryReadControlWord(rtf, ref probe, out var word, out _, out _)
								&& word.SequenceEqual(destination))
							{
								groupEnd = FindGroupEnd(rtf, i, rtf.Length, workBudget);
								return groupEnd >= 0 ? probe : -1;
							}
						}
						break;
					case '}':
						depth--;
						if (depth < 0)
						{
							groupEnd = -1;
							return -1;
						}
						break;
				}
			}

			groupEnd = -1;
			return -1;
		}

		private static int FindGroupEnd(string rtf, int groupStart, int limit, ParseWorkBudget workBudget)
		{
			var depth = 0;
			for (var i = groupStart; i < limit; i++)
			{
				if (rtf[i] == '\\')
				{
					workBudget.RecordControl();
					SkipControl(rtf, ref i);
				}
				else if (rtf[i] == '{')
				{
					depth++;
				}
				else if (rtf[i] == '}' && --depth == 0)
				{
					return i;
				}
			}

			return -1;
		}

		private static bool TryParseFontEntry(
			ReadOnlySpan<char> entry,
			ParseWorkBudget workBudget,
			out int index,
			out string name,
			out int? codePage,
			out int? charset)
		{
			index = 0;
			name = string.Empty;
			codePage = null;
			charset = null;
			var builder = new StringBuilder();
			var foundIndex = false;
			var depth = 0;
			for (var i = 0; i < entry.Length; i++)
			{
				var value = entry[i];
				if (value == '{')
				{
					depth++;
					continue;
				}
				if (value == '}')
				{
					depth--;
					continue;
				}
				if (value == '\\')
				{
					workBudget.RecordControl();
					if (++i >= entry.Length)
					{
						break;
					}
					if (entry[i] is '\\' or '{' or '}')
					{
						if (depth == 0)
						{
							builder.Append(entry[i]);
						}
						continue;
					}
					if (entry[i] == '\'')
					{
						if (i + 2 < entry.Length && byte.TryParse(entry.Slice(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var encoded) && depth == 0)
						{
							builder.Append(DecodeWindows1252(encoded));
						}
						i = Math.Min(entry.Length - 1, i + 2);
						continue;
					}
					if (!char.IsLetter(entry[i]))
					{
						continue;
					}

					var wordStart = i;
					while (i + 1 < entry.Length && char.IsLetter(entry[i + 1]))
					{
						i++;
					}
					var word = entry.Slice(wordStart, i - wordStart + 1);
					var negative = i + 1 < entry.Length && entry[i + 1] == '-';
					if (negative)
					{
						i++;
					}
					var numberStart = i + 1;
					while (i + 1 < entry.Length && char.IsDigit(entry[i + 1]))
					{
						i++;
					}
					if (depth == 0 && word.SequenceEqual("f") && i >= numberStart
						&& int.TryParse(entry.Slice(numberStart, i - numberStart + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex))
					{
						index = negative ? -parsedIndex : parsedIndex;
						foundIndex = index >= 0;
					}
					else if (depth == 0 && word.SequenceEqual("cpg") && i >= numberStart
						&& int.TryParse(entry.Slice(numberStart, i - numberStart + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedCodePage))
					{
						codePage = ValidateCodePage(negative ? -parsedCodePage : parsedCodePage);
					}
					else if (depth == 0 && word.SequenceEqual("fcharset") && i >= numberStart
						&& int.TryParse(entry.Slice(numberStart, i - numberStart + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedCharset))
					{
						charset = negative ? -parsedCharset : parsedCharset;
						codePage ??= GetCodePageForCharset(charset.Value);
					}
					if (i + 1 < entry.Length && entry[i + 1] == ' ')
					{
						i++;
					}
					if (word.SequenceEqual("bin"))
					{
						var numberLength = i < entry.Length && entry[i] == ' '
							? i - numberStart
							: i - numberStart + 1;
						if (negative
							|| numberLength <= 0
							|| !int.TryParse(entry.Slice(numberStart, numberLength), NumberStyles.None, CultureInfo.InvariantCulture, out var binaryLength))
						{
							throw new ArgumentException("The RTF binary payload length is invalid.");
						}
						if (binaryLength > entry.Length - i - 1)
						{
							throw new ArgumentException("The RTF binary payload is truncated.");
						}
						i += binaryLength;
					}
					continue;
				}

				if (depth == 0)
				{
					if (value == ';')
					{
						name = builder.ToString().Trim();
						return foundIndex && name.Length > 0;
					}
					if (builder.Length >= MaxFontNameLength)
					{
						return false;
					}
					builder.Append(value);
				}
			}

			return false;
		}

		private static int? GetCodePageForCharset(int charset)
			=> charset switch
			{
				0 or 1 => null,
				77 => 10000,
				128 => 932,
				129 => 949,
				134 => 936,
				136 => 950,
				161 => 1253,
				162 => 1254,
				163 => 1258,
				177 => 1255,
				178 => 1256,
				186 => 1257,
				204 => 1251,
				222 => 874,
				238 => 1250,
				255 => 850,
				_ => null,
			};

		private static bool TryReadHeaderControl(string rtf, string control, ParseWorkBudget workBudget, out int parameter)
		{
			parameter = 0;
			var depth = 0;
			for (var i = 0; i < rtf.Length; i++)
			{
				if (rtf[i] == '{')
				{
					depth++;
				}
				else if (rtf[i] == '}')
				{
					depth--;
				}
				else if (rtf[i] == '\\')
				{
					workBudget.RecordControl();
					var position = i;
					var parsed = TryReadControlWord(rtf, ref position, out var word, out var hasParameter, out var value);
					if (!parsed && i + 1 < rtf.Length && char.IsLetter(rtf[i + 1]))
					{
						throw new ArgumentException("The RTF control parameter is invalid.", nameof(rtf));
					}
					if (parsed
						&& depth == 1
						&& hasParameter
						&& word.SequenceEqual(control))
					{
						parameter = value;
						return true;
					}
					if (parsed && word.SequenceEqual("bin"))
					{
						if (!hasParameter
							|| value < 0
							|| rtf.AsSpan(i).StartsWith(@"\bin-", StringComparison.Ordinal))
						{
							throw new ArgumentException("The RTF binary payload length is invalid.", nameof(rtf));
						}
						if (value > rtf.Length - position)
						{
							throw new ArgumentException("The RTF binary payload is truncated.", nameof(rtf));
						}
						i = position + value - 1;
					}
					else
					{
						i = Math.Max(i, position - 1);
					}
				}
			}

			return false;
		}

		private static bool TryReadControlWord(
			string rtf,
			ref int position,
			out ReadOnlySpan<char> word,
			out bool hasParameter,
			out int parameter)
		{
			word = default;
			hasParameter = false;
			parameter = 0;
			if (position >= rtf.Length || rtf[position] != '\\' || position + 1 >= rtf.Length || !char.IsLetter(rtf[position + 1]))
			{
				return false;
			}

			var start = ++position;
			while (position < rtf.Length && char.IsLetter(rtf[position]))
			{
				position++;
			}
			word = rtf.AsSpan(start, position - start);
			var negative = position < rtf.Length && rtf[position] == '-';
			if (negative)
			{
				position++;
			}
			var numberStart = position;
			while (position < rtf.Length && char.IsDigit(rtf[position]))
			{
				position++;
			}
			hasParameter = position > numberStart;
			if (hasParameter && !int.TryParse(rtf.AsSpan(numberStart, position - numberStart), NumberStyles.None, CultureInfo.InvariantCulture, out parameter))
			{
				return false;
			}
			if (negative)
			{
				parameter = -parameter;
			}
			if (position < rtf.Length && rtf[position] == ' ')
			{
				position++;
			}
			return true;
		}

		private static void SkipControl(string rtf, ref int position)
		{
			if (position + 1 >= rtf.Length)
			{
				throw new ArgumentException("The RTF contains an incomplete control token.", nameof(rtf));
			}
			if (rtf[position + 1] == '\'')
			{
				if (position + 3 >= rtf.Length
					|| !TryDecodeHexByte(rtf[position + 2], rtf[position + 3], out _))
				{
					throw new ArgumentException("The RTF contains an invalid escaped byte.", nameof(rtf));
				}
				position += 3;
				return;
			}
			var controlPosition = position;
			var negativeBinaryLength = rtf.AsSpan(position).StartsWith(@"\bin-", StringComparison.Ordinal);
			if (TryReadControlWord(rtf, ref controlPosition, out var word, out var hasParameter, out var parameter))
			{
				if (word.SequenceEqual("bin"))
				{
					if (!hasParameter || parameter < 0 || negativeBinaryLength)
					{
						throw new ArgumentException("The RTF binary payload length is invalid.", nameof(rtf));
					}
					if (parameter > rtf.Length - controlPosition)
					{
						throw new ArgumentException("The RTF binary payload is truncated.", nameof(rtf));
					}
					position = controlPosition + parameter - 1;
				}
				else
				{
					position = controlPosition - 1;
				}
			}
			else if (char.IsLetter(rtf[position + 1]))
			{
				throw new ArgumentException("The RTF control parameter is invalid.", nameof(rtf));
			}
			else
			{
				position++;
			}
		}

		private static bool IsSafeRtfFontName(string name)
		{
			if (name.Length is 0 or > MaxFontNameLength
				|| Uri.TryCreate(name, UriKind.Absolute, out _)
				|| name.Contains(';')
				|| name.Contains('/')
				|| name.Contains('\\'))
			{
				return false;
			}

			foreach (var character in name)
			{
				if (char.IsControl(character))
				{
					return false;
				}
			}

			return true;
		}

		private static Dictionary<int, global::Windows.UI.Color> ParseColors(string rtf, ParseWorkBudget workBudget)
		{
			var colors = new Dictionary<int, global::Windows.UI.Color>();
			var colorTableStart = FindDestinationGroup(rtf, "colortbl", workBudget, out var colorTableEnd);
			if (colorTableStart < 0)
			{
				return colors;
			}

			var index = 0;
			var depth = 0;
			int? red = null;
			int? green = null;
			int? blue = null;
			for (var position = colorTableStart; position < colorTableEnd; position++)
			{
				var value = rtf[position];
				if (value == '{')
				{
					depth++;
					continue;
				}
				if (value == '}')
				{
					depth--;
					continue;
				}
				if (value == '\\')
				{
					workBudget.RecordControl();
					var controlPosition = position;
					if (TryReadControlWord(rtf, ref controlPosition, out var word, out var hasParameter, out var parameter))
					{
						if (depth == 0
							&& (word.SequenceEqual("red")
								|| word.SequenceEqual("green")
								|| word.SequenceEqual("blue")))
						{
							if (!hasParameter || parameter is < 0 or > 255)
							{
								throw new ArgumentException("The RTF color table is invalid.", nameof(rtf));
							}

							if (word.SequenceEqual("red"))
							{
								red = parameter;
							}
							else if (word.SequenceEqual("green"))
							{
								green = parameter;
							}
							else
							{
								blue = parameter;
							}
						}
						position = controlPosition - 1;
					}
					else
					{
						SkipControl(rtf, ref position);
					}
					continue;
				}
				if (depth != 0 || value != ';')
				{
					continue;
				}

				if (index >= MaxParsedColors)
				{
					throw new ArgumentException("The RTF contains too many colors.", nameof(rtf));
				}
				if (red is not null || green is not null || blue is not null)
				{
					colors[index] = global::Windows.UI.Color.FromArgb(
						255,
						(byte)(red ?? 0),
						(byte)(green ?? 0),
						(byte)(blue ?? 0));
				}
				index++;
				red = green = blue = null;
			}
			return colors;
		}

		private static Dictionary<int, RtfParsedList> ParseLists(
			string rtf,
			Dictionary<int, RtfFont> fonts,
			ParseWorkBudget workBudget)
		{
			var definitions = new Dictionary<int, List<RtfParsedListLevel>>();
			var listTableStart = FindDestinationGroup(rtf, "listtable", workBudget, out var listTableEnd);
			if (listTableStart >= 0)
			{
				foreach (var group in EnumerateImmediateGroups(rtf, listTableStart, listTableEnd, workBudget))
				{
					if (!string.Equals(group.Destination, "list", StringComparison.Ordinal)
						|| !TryFindControlParameter(rtf, group.Start, group.End, "listid", workBudget, out var listId))
					{
						continue;
					}

					if (definitions.Count >= MaxParsedLists && !definitions.ContainsKey(listId))
					{
						throw new ArgumentException("The RTF contains too many list definitions.", nameof(rtf));
					}

					var levels = new List<RtfParsedListLevel>();
					foreach (var child in EnumerateImmediateGroups(rtf, group.ContentStart, group.End, workBudget))
					{
						if (string.Equals(child.Destination, "listlevel", StringComparison.Ordinal))
						{
							if (levels.Count >= MaxListLevels)
							{
								throw new ArgumentException("The RTF list contains too many levels.", nameof(rtf));
							}
							levels.Add(ParseListLevel(rtf, child, fonts, workBudget));
						}
					}

					if (levels.Count > 0)
					{
						definitions[listId] = levels;
					}
				}
			}

			var lists = new Dictionary<int, RtfParsedList>();
			var overrideTableStart = FindDestinationGroup(rtf, "listoverridetable", workBudget, out var overrideTableEnd);
			if (overrideTableStart < 0)
			{
				return lists;
			}

			foreach (var group in EnumerateImmediateGroups(rtf, overrideTableStart, overrideTableEnd, workBudget))
			{
				if (!string.Equals(group.Destination, "listoverride", StringComparison.Ordinal)
					|| !TryFindControlParameter(rtf, group.Start, group.End, "listid", workBudget, out var listId)
					|| !TryFindControlParameter(rtf, group.Start, group.End, "ls", workBudget, out var overrideId)
					|| !definitions.TryGetValue(listId, out var levels))
				{
					continue;
				}

				if (lists.Count >= MaxParsedLists && !lists.ContainsKey(overrideId))
				{
					throw new ArgumentException("The RTF contains too many list overrides.", nameof(rtf));
				}
				lists[overrideId] = new RtfParsedList(levels);
			}

			return lists;
		}

		private static RtfParsedListLevel ParseListLevel(
			string rtf,
			RtfGroup group,
			Dictionary<int, RtfFont> fonts,
			ParseWorkBudget workBudget)
		{
			var numberFormat = TryFindControlParameter(rtf, group.Start, group.End, "levelnfc", workBudget, out var parsedNumberFormat)
				? parsedNumberFormat
				: TryFindControlParameter(rtf, group.Start, group.End, "levelnfcn", workBudget, out parsedNumberFormat)
					? parsedNumberFormat
					: 0;
			var alignment = TryFindControlParameter(rtf, group.Start, group.End, "leveljc", workBudget, out var parsedAlignment)
				? parsedAlignment
				: TryFindControlParameter(rtf, group.Start, group.End, "leveljcn", workBudget, out parsedAlignment)
					? parsedAlignment
					: 0;
			var start = TryFindControlParameter(rtf, group.Start, group.End, "levelstartat", workBudget, out var parsedStart)
				? parsedStart
				: 1;
			var tab = TryFindControlParameter(rtf, group.Start, group.End, "tx", workBudget, out var parsedTab)
				? Math.Clamp(parsedTab / 20f, 0, MaxParagraphMetric)
				: 0;
			var fontIndex = TryFindControlParameter(rtf, group.Start, group.End, "f", workBudget, out var parsedFontIndex)
				? parsedFontIndex
				: (int?)null;
			var levelText = ParseLevelText(rtf, group, fontIndex, workBudget);
			var markerType = GetMarkerType(numberFormat, levelText, fonts, out var markerStart);
			return new RtfParsedListLevel(
				markerType,
				ParseListStyle(levelText),
				alignment switch
				{
					1 => global::Microsoft.UI.Text.MarkerAlignment.Center,
					2 => global::Microsoft.UI.Text.MarkerAlignment.Right,
					_ => global::Microsoft.UI.Text.MarkerAlignment.Left,
				},
				markerType == global::Microsoft.UI.Text.MarkerType.UnicodeSequence
					? markerStart
					: Math.Max(0, start),
				tab);
		}

		private static global::Microsoft.UI.Text.MarkerType GetMarkerType(
			int numberFormat,
			LevelTextInfo levelText,
			Dictionary<int, RtfFont> fonts,
			out int markerStart)
		{
			markerStart = 1;
			if (numberFormat == 23
				&& !levelText.HasNumberPlaceholder
				&& TryGetSingleUnicodeScalar(levelText.Text.Trim(), out var scalar))
			{
				var fontName = levelText.FontIndex is { } fontIndex && fonts.TryGetValue(fontIndex, out var font)
					? font.Name
					: string.Empty;
				var glyph = scalar is >= 0xf000 and <= 0xf0ff ? scalar & 0xff : scalar;
				if (fontName.StartsWith("Wingdings", StringComparison.OrdinalIgnoreCase))
				{
					if (glyph == 'l')
					{
						return global::Microsoft.UI.Text.MarkerType.BlackCircleWingding;
					}
					if (glyph == 'n')
					{
						return global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding;
					}
				}
				if (scalar == 0x25cf)
				{
					return global::Microsoft.UI.Text.MarkerType.BlackCircleWingding;
				}
				if (scalar == 0x25cb)
				{
					return global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding;
				}
				if (scalar is >= 0x278a and <= 0x2793)
				{
					return global::Microsoft.UI.Text.MarkerType.BlackCircleWingding;
				}
				if (scalar is >= 0x2780 and <= 0x2789)
				{
					return global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding;
				}
				if (scalar is 0x2022 or 0xf0b7)
				{
					return global::Microsoft.UI.Text.MarkerType.Bullet;
				}
				if (global::Microsoft.UI.Xaml.Controls.RichEditBox.IsValidListMarkerUnicodeScalar(scalar))
				{
					markerStart = scalar;
					return global::Microsoft.UI.Text.MarkerType.UnicodeSequence;
				}
			}

			return numberFormat switch
			{
				1 => global::Microsoft.UI.Text.MarkerType.UppercaseRoman,
				2 => global::Microsoft.UI.Text.MarkerType.LowercaseRoman,
				3 => global::Microsoft.UI.Text.MarkerType.UppercaseEnglishLetter,
				4 => global::Microsoft.UI.Text.MarkerType.LowercaseEnglishLetter,
				10 => global::Microsoft.UI.Text.MarkerType.JapanSimplifiedChinese,
				14 => global::Microsoft.UI.Text.MarkerType.ArabicWide,
				18 => global::Microsoft.UI.Text.MarkerType.CircledNumber,
				23 => global::Microsoft.UI.Text.MarkerType.Bullet,
				34 => global::Microsoft.UI.Text.MarkerType.TraditionalChinese,
				38 => global::Microsoft.UI.Text.MarkerType.SimplifiedChinese,
				41 => global::Microsoft.UI.Text.MarkerType.JapanKorea,
				45 => global::Microsoft.UI.Text.MarkerType.Hebrew,
				46 => global::Microsoft.UI.Text.MarkerType.ArabicDictionary,
				48 => global::Microsoft.UI.Text.MarkerType.ArabicAbjad,
				49 => global::Microsoft.UI.Text.MarkerType.DevanagariVowel,
				50 => global::Microsoft.UI.Text.MarkerType.DevanagariConsonant,
				51 => global::Microsoft.UI.Text.MarkerType.DevanagariNumeric,
				53 => global::Microsoft.UI.Text.MarkerType.ThaiAlphabetic,
				54 => global::Microsoft.UI.Text.MarkerType.ThaiNumeric,
				_ => global::Microsoft.UI.Text.MarkerType.Arabic,
			};
		}

		private static LevelTextInfo ParseLevelText(
			string rtf,
			RtfGroup listLevel,
			int? inheritedFontIndex,
			ParseWorkBudget workBudget)
		{
			foreach (var child in EnumerateImmediateGroups(rtf, listLevel.ContentStart, listLevel.End, workBudget))
			{
				if (!string.Equals(child.Destination, "leveltext", StringComparison.Ordinal))
				{
					continue;
				}

				var text = new StringBuilder();
				var fontIndex = inheritedFontIndex;
				var unicodeSkipCount = 1;
				var unicodeFallbackRemaining = 0;
				var lengthPrefixConsumed = false;
				var hasNumberPlaceholder = false;
				for (var position = child.ContentStart; position < child.End; position++)
				{
					if (rtf[position] == '\\')
					{
						workBudget.RecordControl();
						if (position + 3 < child.End
							&& rtf[position + 1] == '\''
							&& TryDecodeHexByte(rtf[position + 2], rtf[position + 3], out var encoded))
						{
							position += 3;
							if (!lengthPrefixConsumed)
							{
								lengthPrefixConsumed = true;
								continue;
							}
							if (unicodeFallbackRemaining > 0)
							{
								unicodeFallbackRemaining--;
								continue;
							}
							if (encoded == 0)
							{
								hasNumberPlaceholder = true;
							}
							else
							{
								text.Append((char)encoded);
							}
						}
						else
						{
							var controlPosition = position;
							if (TryReadControlWord(
								rtf,
								ref controlPosition,
								out var word,
								out var hasParameter,
								out var parameter))
							{
								if (word.SequenceEqual("uc") && hasParameter)
								{
									unicodeSkipCount = Math.Max(0, parameter);
								}
								else if (word.SequenceEqual("u") && hasParameter)
								{
									text.Append((char)(short)parameter);
									unicodeFallbackRemaining = unicodeSkipCount;
								}
								else if (word.SequenceEqual("f") && hasParameter)
								{
									fontIndex = parameter;
								}
								position = controlPosition - 1;
							}
							else
							{
								SkipControl(rtf, ref position);
							}
						}
					}
					else if (rtf[position] == ';')
					{
						break;
					}
					else if (!char.IsControl(rtf[position]))
					{
						if (!lengthPrefixConsumed)
						{
							lengthPrefixConsumed = true;
						}
						else if (unicodeFallbackRemaining > 0)
						{
							unicodeFallbackRemaining--;
						}
						else
						{
							text.Append(rtf[position]);
						}
					}
				}

				return new LevelTextInfo(text.ToString(), hasNumberPlaceholder, fontIndex);
			}

			return new LevelTextInfo(string.Empty, false, inheritedFontIndex);
		}

		private static global::Microsoft.UI.Text.MarkerStyle ParseListStyle(LevelTextInfo levelText)
		{
			if (!levelText.HasNumberPlaceholder)
			{
				return global::Microsoft.UI.Text.MarkerStyle.Plain;
			}
			if (levelText.Text.Length >= 2 && levelText.Text[0] == '(' && levelText.Text[^1] == ')')
			{
				return global::Microsoft.UI.Text.MarkerStyle.Parentheses;
			}
			if (levelText.Text.Length > 0 && levelText.Text[^1] == ')')
			{
				return global::Microsoft.UI.Text.MarkerStyle.Parenthesis;
			}
			if (levelText.Text.Length > 0 && levelText.Text[^1] == '-')
			{
				return global::Microsoft.UI.Text.MarkerStyle.Minus;
			}
			if (levelText.Text.Length == 0)
			{
				return global::Microsoft.UI.Text.MarkerStyle.Plain;
			}
			return global::Microsoft.UI.Text.MarkerStyle.Period;
		}

		private static bool TryGetSingleUnicodeScalar(string value, out int scalar)
		{
			scalar = 0;
			if (value.Length == 1 && !char.IsSurrogate(value[0]))
			{
				scalar = value[0];
				return true;
			}
			if (value.Length == 2 && char.IsSurrogatePair(value[0], value[1]))
			{
				scalar = char.ConvertToUtf32(value[0], value[1]);
				return true;
			}
			return false;
		}

		private static IEnumerable<RtfGroup> EnumerateImmediateGroups(
			string rtf,
			int start,
			int end,
			ParseWorkBudget workBudget)
		{
			var depth = 0;
			for (var position = start; position < end; position++)
			{
				if (rtf[position] == '\\')
				{
					workBudget.RecordControl();
					SkipControl(rtf, ref position);
				}
				else if (rtf[position] == '{')
				{
					if (depth == 0)
					{
						var groupEnd = FindGroupEnd(rtf, position, end, workBudget);
						if (groupEnd < 0)
						{
							throw new ArgumentException("The RTF list table is malformed.", nameof(rtf));
						}

						if (TryGetGroupDestination(rtf, position, groupEnd, workBudget, out var destination, out var contentStart))
						{
							yield return new RtfGroup(position, groupEnd, contentStart, destination);
						}
						position = groupEnd;
					}
					else
					{
						depth++;
					}
				}
				else if (rtf[position] == '}')
				{
					depth--;
				}
			}
		}

		private static bool TryGetGroupDestination(
			string rtf,
			int groupStart,
			int groupEnd,
			ParseWorkBudget workBudget,
			out string destination,
			out int contentStart)
		{
			destination = string.Empty;
			contentStart = groupStart + 1;
			var position = contentStart;
			if (position + 1 < groupEnd && rtf[position] == '\\' && rtf[position + 1] == '*')
			{
				workBudget.RecordControl();
				position += 2;
			}
			if (position >= groupEnd || rtf[position] != '\\')
			{
				return false;
			}

			workBudget.RecordControl();
			if (!TryReadControlWord(rtf, ref position, out var destinationSpan, out _, out _))
			{
				return false;
			}
			destination = MaterializeControlWord(destinationSpan);
			contentStart = position;
			return true;
		}

		private static string MaterializeControlWord(ReadOnlySpan<char> word)
		{
			_controlWordStringAllocationCount++;
			return word.ToString();
		}

		private static bool TryFindControlParameter(
			string rtf,
			int start,
			int end,
			string control,
			ParseWorkBudget workBudget,
			out int parameter)
		{
			parameter = 0;
			var depth = 0;
			for (var position = start; position <= end; position++)
			{
				if (rtf[position] == '{')
				{
					depth++;
				}
				else if (rtf[position] == '}')
				{
					depth--;
				}
				else if (rtf[position] == '\\')
				{
					workBudget.RecordControl();
					var controlPosition = position;
					if (TryReadControlWord(rtf, ref controlPosition, out var word, out var hasParameter, out var value))
					{
						if (depth == 1 && hasParameter && word.SequenceEqual(control))
						{
							parameter = value;
							return true;
						}
						position = controlPosition - 1;
					}
					else
					{
						SkipControl(rtf, ref position);
					}
				}
			}

			return false;
		}

		private static void CloseFrame(
			string rtf,
			List<ParserFrame> stack,
			ParsedFragmentBuilder output,
			ref int imageBytes,
			ref long imagePixels,
			ParseBudget budget)
		{
			var closed = stack[stack.Count - 1];
			stack.RemoveAt(stack.Count - 1);
			if (closed.PreserveOpaqueDestination
				&& !stack.Exists(static frame => frame.PreserveOpaqueDestination))
			{
				var preservedGroup = rtf.Substring(closed.GroupStart, closed.GroupEnd - closed.GroupStart + 1);
				if (!ContainsUnsafePreservedDestination(preservedGroup))
				{
					output.AddOpaqueGroup(preservedGroup, closed.ProjectedStart);
				}
			}
			if (closed.IsObjectGroup && closed.ObjectContext is { } objectContext)
			{
				AppendObjectFallback(objectContext, closed.State, output, budget);
			}
			else if (closed.Destination == ParserDestination.FieldInstruction)
			{
				var instruction = closed.DestinationText.ToString().Trim();
				var link = ParseHyperlinkInstruction(instruction);
				if (link is not null)
				{
					for (var i = stack.Count - 1; i >= 0; i--)
					{
						if (stack[i].IsField)
						{
							stack[i].FieldUrl = link.Value.Link;
							stack[i].FieldAnchor = link.Value.Anchor;
							stack[i].FieldIdentity = new RichEditTextObjectIdentity();
							break;
						}
					}
				}
			}
			else if (closed.Destination == ParserDestination.InlineImage
				&& TryParseInlineImageMetadata(closed.DestinationText.ToString(), out var image))
			{
				stack[stack.Count - 1].PendingInlineImage = image;
			}
			else if (closed.Destination == ParserDestination.ObjectFallback
				&& TryParseObjectFallbackMetadata(closed.DestinationText.ToString(), out var objectFallback))
			{
				stack[stack.Count - 1].PendingInlineImage = objectFallback;
			}
			else if (closed.Destination == ParserDestination.ObjectClass && closed.ObjectContext is { } classContext)
			{
				classContext.ObjectClass = NormalizeObjectMetadata(closed.DestinationText.ToString());
			}
			else if (closed.Destination == ParserDestination.ObjectName && closed.ObjectContext is { } nameContext)
			{
				nameContext.ObjectName = NormalizeObjectMetadata(closed.DestinationText.ToString());
			}
			else if (closed.Destination == ParserDestination.Picture && closed.IsPictureGroup)
			{
				budget.RecordPictureAttempt();
				if (TryParsePicture(closed, out var picture))
				{
					if (stack[stack.Count - 1].PendingInlineImage is { } metadata)
					{
						picture.Width = metadata.Width;
						picture.Height = metadata.Height;
						picture.Ascent = metadata.Ascent;
						picture.VerticalAlignment = metadata.VerticalAlignment;
						picture.AlternateText = metadata.AlternateText;
						picture.Validate();
						stack[stack.Count - 1].PendingInlineImage = null;
					}
					ValidateParsedImageBudget(picture, ref imageBytes, ref imagePixels);
					if (closed.InObjectResult && closed.ObjectContext is { } pictureContext)
					{
						pictureContext.SetPicture(picture, closed.State.Character, closed.State.Paragraph);
					}
					else
					{
						AppendParsedImage(picture, closed.State.Character, closed.State.Paragraph, output, budget);
					}
				}
			}
			else if (closed.Destination == ParserDestination.CharacterFormat)
			{
				TryApplyCharacterMetadata(closed.DestinationText.ToString(), stack[stack.Count - 1].State.Character);
			}
			else if (closed.Destination == ParserDestination.ParagraphFormat)
			{
				TryApplyParagraphMetadata(closed.DestinationText.ToString(), stack[stack.Count - 1].State.Paragraph);
			}
			else if (closed.Destination == ParserDestination.TerminalParagraph)
			{
				output.MarkExplicitTerminalParagraphState();
			}
			else if (closed.Destination == ParserDestination.LegacyList)
			{
				var source = closed.State.Paragraph;
				var target = stack[stack.Count - 1].State.Paragraph;
				target.ListType = source.ListType is global::Microsoft.UI.Text.MarkerType.Undefined or global::Microsoft.UI.Text.MarkerType.None
					? global::Microsoft.UI.Text.MarkerType.Arabic
					: source.ListType;
				target.ListStyle = source.ListStyle == global::Microsoft.UI.Text.MarkerStyle.Undefined
					? global::Microsoft.UI.Text.MarkerStyle.Period
					: source.ListStyle;
				target.ListAlignment = source.ListAlignment == global::Microsoft.UI.Text.MarkerAlignment.Undefined
					? global::Microsoft.UI.Text.MarkerAlignment.Left
					: source.ListAlignment;
				target.ListLevelIndex = Math.Max(0, source.ListLevelIndex);
				target.ListStart = source.ListStart;
				target.ListTab = source.ListTab;
			}
		}

		private static void AppendObjectFallback(
			ObjectContext context,
			ParserState objectState,
			ParsedFragmentBuilder output,
			ParseBudget budget)
		{
			if (context.Picture is { } picture)
			{
				if (string.IsNullOrEmpty(picture.AlternateText))
				{
					picture.AlternateText = !string.IsNullOrEmpty(context.ObjectName)
						? context.ObjectName
						: context.ObjectClass ?? string.Empty;
				}
				AppendParsedImage(
					picture,
					context.PictureCharacter ?? objectState.Character,
					context.PictureParagraph ?? objectState.Paragraph,
					output,
					budget);
				return;
			}

			if (context.ResultText.Length > 0)
			{
				var character = context.ResultCharacter ?? objectState.Character;
				var paragraph = context.ResultParagraph ?? objectState.Paragraph;
				foreach (var value in context.ResultText.ToString())
				{
					output.Append(value, character, paragraph, budget);
				}
				return;
			}

			var alternateText = !string.IsNullOrEmpty(context.ObjectName)
				? context.ObjectName
				: !string.IsNullOrEmpty(context.ObjectClass)
					? context.ObjectClass
					: "Object";
			var placeholder = InlineImageState.CreateObjectFallback(
				context.WidthGoal > 0 ? Math.Max(1, context.WidthGoal / 15) : null,
				context.HeightGoal > 0 ? Math.Max(1, context.HeightGoal / 15) : null,
				alternateText);
			AppendParsedImage(placeholder, objectState.Character, objectState.Paragraph, output, budget);
		}

		private static void AppendParsedImage(
			InlineImageState image,
			CharacterFormatState character,
			ParagraphFormatState paragraph,
			ParsedFragmentBuilder output,
			ParseBudget budget)
		{
			budget.RecordTextObject();
			var state = character.Clone();
			state.InlineImage = image;
			state.Link = null;
			state.LinkAnchor = null;
			state.TextObjectIdentity = new RichEditTextObjectIdentity();
			output.Append('\ufffc', state, paragraph, budget, takeCharacterOwnership: true);
		}

		private static string NormalizeObjectMetadata(string value)
		{
			var normalized = value.Trim();
			return normalized.Length <= InlineImageState.MaxAlternateTextLength
				? normalized
				: normalized[..InlineImageState.MaxAlternateTextLength];
		}

		private static void ValidateParsedImageBudget(InlineImageState image, ref int bytes, ref long pixels)
		{
			bytes = checked(bytes + image.EncodedLength);
			pixels = checked(pixels + image.GetDecodedPixelCount());
			if (bytes > MaxParsedImageBytes || pixels > MaxParsedImagePixels)
			{
				throw new ArgumentException("The RTF contains too much image data.");
			}
		}

		private static bool TryApplyCharacterMetadata(string value, CharacterFormatState state)
		{
			var fields = value.Trim().Split(',');
			if (fields.Length is < 15 or > 17
				|| fields[5].Length > MaxEncodedLanguageTagLength
				|| !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fontStretch)
				|| !float.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var kerning)
				|| !float.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var position)
				|| !float.TryParse(fields[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var spacing)
				|| !int.TryParse(fields[13], NumberStyles.Integer, CultureInfo.InvariantCulture, out var textScript)
				|| !int.TryParse(fields[14], NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight))
			{
				return false;
			}
			if (!float.IsFinite(kerning)
				|| !float.IsFinite(position)
				|| !float.IsFinite(spacing)
				|| Math.Abs(kerning) > 4096
				|| Math.Abs(position) > 4096
				|| Math.Abs(spacing) > 4096
				|| weight is < 0 or > 999)
			{
				return false;
			}

			global::Windows.UI.Color? background = null;
			if (fields[1] != "-")
			{
				if (!uint.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var packed))
				{
					return false;
				}

				background = global::Windows.UI.Color.FromArgb(
					(byte)(packed >> 24),
					(byte)(packed >> 16),
					(byte)(packed >> 8),
					(byte)packed);
			}

			string languageTag;
			try
			{
				languageTag = Encoding.UTF8.GetString(Convert.FromBase64String(fields[5]));
			}
			catch (FormatException)
			{
				return false;
			}
			if (languageTag.Length > CharacterFormatState.MaxLanguageTagLength)
			{
				throw new ArgumentException("The RTF language tag is too long.");
			}

			try
			{
				state.AllCaps = fields[0] == "1";
				state.Background = background;
				state.FontStretch = (global::Windows.UI.Text.FontStretch)fontStretch;
				state.Hidden = fields[3] == "1";
				state.Kerning = kerning;
				state.LanguageTag = languageTag;
				state.Outline = fields[6] == "1";
				state.Position = position;
				state.ProtectedText = fields[8] == "1";
				state.SmallCaps = fields[9] == "1";
				state.Spacing = spacing;
				state.Subscript = fields[11] == "1";
				state.Superscript = fields[12] == "1";
				state.TextScript = (global::Microsoft.UI.Text.TextScript)textScript;
				state.Weight = weight;
				state.Bold = weight >= 600;
				if (fields.Length >= 16
					&& int.TryParse(fields[15], NumberStyles.Integer, CultureInfo.InvariantCulture, out var underline))
				{
					state.Underline = (global::Microsoft.UI.Text.UnderlineType)underline;
				}
				state.WeightExplicit = fields.Length < 17 || fields[16] == "1";
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		private static bool TryParseInlineImageMetadata(string value, out InlineImageState image)
		{
			image = new InlineImageState();
			var fields = value.Trim().Split(',');
			if (fields.Length != 5
				|| !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
				|| !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
				|| !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ascent)
				|| !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var alignment))
			{
				return false;
			}

			try
			{
				image.Width = width;
				image.Height = height;
				image.Ascent = ascent;
				image.VerticalAlignment = (global::Microsoft.UI.Text.VerticalCharacterAlignment)alignment;
				image.AlternateText = Encoding.UTF8.GetString(Convert.FromBase64String(fields[4]));
				if (image.Width is < 0 or > InlineImageState.MaxDimension
					|| image.Height is < 0 or > InlineImageState.MaxDimension
					|| image.Ascent is < 0 or > InlineImageState.MaxDimension
					|| !Enum.IsDefined(image.VerticalAlignment)
					|| image.AlternateText.Length > InlineImageState.MaxAlternateTextLength)
				{
					return false;
				}
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private static bool TryParseObjectFallbackMetadata(string value, out InlineImageState image)
		{
			image = new InlineImageState();
			var fields = value.Trim().Split(',');
			if (fields.Length != 3
				|| !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
				|| !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
			{
				return false;
			}

			try
			{
				var alternateText = Encoding.UTF8.GetString(Convert.FromBase64String(fields[2]));
				if (width is <= 0 or > InlineImageState.MaxDimension
					|| height is <= 0 or > InlineImageState.MaxDimension
					|| alternateText.Length > InlineImageState.MaxAlternateTextLength)
				{
					return false;
				}

				image = InlineImageState.CreateObjectFallback(width, height, alternateText);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private static bool TryParsePicture(ParserFrame frame, out InlineImageState image)
		{
			image = new InlineImageState();
			if (frame.PicturePayload is not { } payload || !payload.TryGetBytes(out var data))
			{
				return false;
			}

			var width = frame.PictureWidthGoal > 0
				? Math.Max(1, frame.PictureWidthGoal / 15)
				: frame.PictureWidth > 0 ? frame.PictureWidth : (int?)null;
			var height = frame.PictureHeightGoal > 0
				? Math.Max(1, frame.PictureHeightGoal / 15)
				: frame.PictureHeight > 0 ? frame.PictureHeight : (int?)null;
			return InlineImageState.TryCreate(
				data,
				width,
				height,
				height,
				global::Microsoft.UI.Text.VerticalCharacterAlignment.Baseline,
				alternateText: string.Empty,
				frame.PictureEncoding,
				out image);
		}

		private static bool TryApplyParagraphMetadata(string value, ParagraphFormatState state)
		{
			var fields = value.Trim().Split(',');
			if (fields.Length != 22
				|| fields[21].Length > MaxEncodedParagraphTabsLength
				|| !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var alignment)
				|| !float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var firstLineIndent)
				|| !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var leftIndent)
				|| !float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var rightIndent)
				|| !float.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var spaceBefore)
				|| !float.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var spaceAfter)
				|| !int.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineSpacingRule)
				|| !float.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var lineSpacing)
				|| !int.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listType)
				|| !int.TryParse(fields[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listStyle)
				|| !int.TryParse(fields[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listAlignment)
				|| !int.TryParse(fields[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listLevelIndex)
				|| !int.TryParse(fields[12], NumberStyles.Integer, CultureInfo.InvariantCulture, out var listStart)
				|| !float.TryParse(fields[13], NumberStyles.Float, CultureInfo.InvariantCulture, out var listTab)
				|| !int.TryParse(fields[20], NumberStyles.Integer, CultureInfo.InvariantCulture, out var style))
			{
				return false;
			}

			var parsedAlignment = (global::Microsoft.UI.Text.ParagraphAlignment)alignment;
			var parsedLineSpacingRule = (global::Microsoft.UI.Text.LineSpacingRule)lineSpacingRule;
			var parsedListType = (global::Microsoft.UI.Text.MarkerType)listType;
			var parsedListStyle = (global::Microsoft.UI.Text.MarkerStyle)listStyle;
			var parsedListAlignment = (global::Microsoft.UI.Text.MarkerAlignment)listAlignment;
			var parsedStyle = (global::Microsoft.UI.Text.ParagraphStyle)style;
			if (!float.IsFinite(firstLineIndent)
				|| !float.IsFinite(leftIndent)
				|| !float.IsFinite(rightIndent)
				|| !float.IsFinite(spaceBefore)
				|| !float.IsFinite(spaceAfter)
				|| !float.IsFinite(lineSpacing)
				|| !float.IsFinite(listTab)
				|| Math.Abs(firstLineIndent) > MaxParagraphMetric
				|| Math.Abs(leftIndent) > MaxParagraphMetric
				|| Math.Abs(rightIndent) > MaxParagraphMetric
				|| Math.Abs(spaceBefore) > MaxParagraphMetric
				|| Math.Abs(spaceAfter) > MaxParagraphMetric
				|| Math.Abs(lineSpacing) > MaxParagraphMetric
				|| listLevelIndex < 0
				|| listTab is < 0 or > MaxParagraphMetric
				|| !Enum.IsDefined(parsedAlignment)
				|| !Enum.IsDefined(parsedLineSpacingRule)
				|| parsedLineSpacingRule == global::Microsoft.UI.Text.LineSpacingRule.Percent
				|| !Enum.IsDefined(parsedListType)
				|| !Enum.IsDefined(parsedListStyle)
				|| !Enum.IsDefined(parsedListAlignment)
				|| !Enum.IsDefined(parsedStyle)
				|| fields[14] is not ("0" or "1")
				|| fields[15] is not ("0" or "1")
				|| fields[16] is not ("0" or "1")
				|| fields[17] is not ("0" or "1")
				|| fields[18] is not ("0" or "1")
				|| fields[19] is not ("0" or "1"))
			{
				return false;
			}

			List<ParagraphTab> tabs;
			try
			{
				tabs = ParseTabs(Encoding.UTF8.GetString(Convert.FromBase64String(fields[21])));
			}
			catch (FormatException)
			{
				return false;
			}

			try
			{
				state.Alignment = parsedAlignment;
				state.FirstLineIndent = firstLineIndent;
				state.LeftIndent = leftIndent;
				state.RightIndent = rightIndent;
				state.SpaceBefore = spaceBefore;
				state.SpaceAfter = spaceAfter;
				state.LineSpacingRule = parsedLineSpacingRule;
				state.LineSpacing = lineSpacing;
				state.ListType = parsedListType;
				state.ListStyle = parsedListStyle;
				state.ListAlignment = parsedListAlignment;
				state.ListLevelIndex = listLevelIndex;
				state.ListStart = listStart;
				state.ListTab = listTab;
				state.KeepTogether = fields[14] == "1";
				state.KeepWithNext = fields[15] == "1";
				state.NoLineNumber = fields[16] == "1";
				state.PageBreakBefore = fields[17] == "1";
				state.RightToLeft = fields[18] == "1";
				state.WidowControl = fields[19] == "1";
				state.Style = parsedStyle;
				state.SetTabs(tabs);
				return true;
			}
			catch (Exception error) when (error is FormatException or ArgumentException)
			{
				return false;
			}
		}

		private static List<ParagraphTab> ParseTabs(string value)
		{
			var tabs = new List<ParagraphTab>();
			var start = 0;
			while (start < value.Length)
			{
				var separator = value.IndexOf(';', start);
				var end = separator >= 0 ? separator : value.Length;
				var entry = value.AsSpan(start, end - start);
				if (!entry.IsEmpty)
				{
					if (tabs.Count >= ParagraphFormatState.MaxTabs)
					{
						throw new ArgumentException("The RTF contains too many paragraph tabs.");
					}

					var firstSeparator = entry.IndexOf('|');
					var secondSeparator = firstSeparator >= 0 ? entry[(firstSeparator + 1)..].IndexOf('|') : -1;
					if (firstSeparator < 0 || secondSeparator < 0)
					{
						throw new FormatException("Invalid paragraph tab metadata.");
					}
					secondSeparator += firstSeparator + 1;
					if (entry[(secondSeparator + 1)..].IndexOf('|') >= 0
						|| !float.TryParse(entry[..firstSeparator], NumberStyles.Float, CultureInfo.InvariantCulture, out var position)
						|| !int.TryParse(entry[(firstSeparator + 1)..secondSeparator], NumberStyles.Integer, CultureInfo.InvariantCulture, out var alignment)
						|| !int.TryParse(entry[(secondSeparator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var leader)
						|| !float.IsFinite(position)
						|| position is < 0 or > MaxParagraphMetric)
					{
						throw new FormatException("Invalid paragraph tab metadata.");
					}

					tabs.Add(new ParagraphTab(
						position,
						(global::Microsoft.UI.Text.TabAlignment)alignment,
						(global::Microsoft.UI.Text.TabLeader)leader));
				}

				start = end + 1;
			}

			return tabs;
		}

		private static RtfHyperlink? ParseHyperlinkInstruction(string instruction)
		{
			var tokens = TokenizeFieldInstruction(instruction);
			if (tokens.Count == 0 || !string.Equals(tokens[0], "HYPERLINK", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			string? target = null;
			string? anchor = null;
			for (var i = 1; i < tokens.Count; i++)
			{
				var token = tokens[i];
				if (token.StartsWith('\\'))
				{
					if (string.Equals(token, "\\l", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Count)
					{
						anchor = tokens[++i];
					}
					else if ((string.Equals(token, "\\o", StringComparison.OrdinalIgnoreCase)
							|| string.Equals(token, "\\t", StringComparison.OrdinalIgnoreCase))
						&& i + 1 < tokens.Count
						&& !tokens[i + 1].StartsWith('\\'))
					{
						i++;
					}
					continue;
				}

				target ??= token;
			}

			target ??= anchor;
			if (string.IsNullOrEmpty(target))
			{
				return null;
			}
			if (target.Any(char.IsControl)
				|| anchor?.Any(char.IsControl) == true)
			{
				return null;
			}

			return new RtfHyperlink($"\"{target.Replace("\"", "\\\"", StringComparison.Ordinal)}\"", anchor);
		}

		private static List<string> TokenizeFieldInstruction(string instruction)
		{
			var tokens = new List<string>();
			for (var position = 0; position < instruction.Length;)
			{
				while (position < instruction.Length && char.IsWhiteSpace(instruction[position]))
				{
					position++;
				}
				if (position >= instruction.Length)
				{
					break;
				}

				var builder = new StringBuilder();
				if (instruction[position] == '"')
				{
					position++;
					var closed = false;
					while (position < instruction.Length)
					{
						var value = instruction[position++];
						if (value == '"')
						{
							closed = true;
							break;
						}
						if (value == '\\' && position < instruction.Length && instruction[position] is '\\' or '"')
						{
							value = instruction[position++];
						}
						builder.Append(value);
					}
					if (!closed)
					{
						return new List<string>();
					}
				}
				else
				{
					while (position < instruction.Length && !char.IsWhiteSpace(instruction[position]))
					{
						builder.Append(instruction[position++]);
					}
				}

				if (builder.Length > 0)
				{
					tokens.Add(builder.ToString());
				}
			}

			return tokens;
		}

		private static void ParseControl(
			string rtf,
			ref int index,
			List<ParserFrame> stack,
			Dictionary<int, RtfFont> fonts,
			Dictionary<int, global::Windows.UI.Color> colors,
			Dictionary<int, RtfParsedList> lists,
			ParsedFragmentBuilder output,
			ParseBudget budget,
			ParseWorkBudget workBudget)
		{
			var controlStart = index;
			var projectedStart = output.TextLength;
			workBudget.RecordControl();
			if (++index >= rtf.Length)
			{
				return;
			}

			var symbol = rtf[index];
			if (symbol is '\\' or '{' or '}')
			{
				var currentFrame = stack[stack.Count - 1];
				if (currentFrame.State.DecoderHasPendingBytes)
				{
					AppendEncodedByte((byte)symbol, currentFrame, output, budget);
				}
				else
				{
					AppendParsedCharacter(symbol, currentFrame, output, budget: budget);
				}
				return;
			}

			if (symbol == '\'')
			{
				if (index + 2 >= rtf.Length || !TryDecodeHexByte(rtf[index + 1], rtf[index + 2], out var encoded))
				{
					throw new ArgumentException("The RTF contains an invalid escaped byte.", nameof(rtf));
				}
				index += 2;
				AppendEncodedByte(encoded, stack[stack.Count - 1], output, budget);
				return;
			}

			if (!char.IsLetter(symbol))
			{
				FlushDecoder(stack[stack.Count - 1], output, budget);
				if (symbol == '*')
				{
					stack[stack.Count - 1].StarDestination = true;
				}
				else if (symbol == '~')
				{
					AppendParsedCharacter('\u00a0', stack[stack.Count - 1], output, budget: budget);
				}
				else if (symbol == '-')
				{
					AppendParsedCharacter('\u00ad', stack[stack.Count - 1], output, budget: budget);
				}
				else if (symbol == '_')
				{
					AppendParsedCharacter('\u2011', stack[stack.Count - 1], output, budget: budget);
				}
				else if (stack[stack.Count - 1].Destination == ParserDestination.FieldInstruction)
				{
					AppendParsedCharacter(symbol, stack[stack.Count - 1], output, skipFallback: false, budget: budget);
				}
				return;
			}

			var wordStart = index;
			while (index + 1 < rtf.Length && char.IsLetter(rtf[index + 1]))
			{
				index++;
			}
			var word = rtf.AsSpan(wordStart, index - wordStart + 1);
			var negative = index + 1 < rtf.Length && rtf[index + 1] == '-';
			if (negative)
			{
				index++;
			}
			var numberStart = index + 1;
			while (index + 1 < rtf.Length && char.IsDigit(rtf[index + 1]))
			{
				index++;
			}
			var hasParameter = index >= numberStart;
			var parameter = 0;
			if (hasParameter
				&& !int.TryParse(rtf.AsSpan(numberStart, index - numberStart + 1), NumberStyles.None, CultureInfo.InvariantCulture, out parameter))
			{
				throw new ArgumentException("The RTF control parameter is invalid.", nameof(rtf));
			}
			if (negative)
			{
				parameter = -parameter;
			}
			if (index + 1 < rtf.Length && rtf[index + 1] == ' ')
			{
				index++;
			}

			var frame = stack[stack.Count - 1];
			FlushDecoder(frame, output, budget);
			if (word.SequenceEqual("bin"))
			{
				if (!hasParameter || parameter < 0 || negative)
				{
					throw new ArgumentException("The RTF binary payload length is invalid.", nameof(rtf));
				}
				if (parameter > rtf.Length - index - 1)
				{
					throw new ArgumentException("The RTF binary payload is truncated.", nameof(rtf));
				}
				if (frame.Destination == ParserDestination.Picture && frame.PicturePayload is { } picturePayload)
				{
					picturePayload.AppendBinary(rtf.AsSpan(index + 1, parameter));
				}
				index += parameter;
				return;
			}
			if (frame.Destination == ParserDestination.FieldInstruction)
			{
				if (word.SequenceEqual("uc") && hasParameter)
				{
					frame.State.UnicodeSkipCount = Math.Max(0, parameter);
				}
				else if (word.SequenceEqual("u") && hasParameter)
				{
					AppendParsedCharacter(
						(char)(short)parameter,
						frame,
						output,
						skipFallback: false,
						budget: budget);
					frame.UnicodeFallbackRemaining = frame.State.UnicodeSkipCount;
				}
				else
				{
					AppendFieldInstructionControl(frame, word, hasParameter, parameter);
				}
				return;
			}

			HandleControl(word, hasParameter, parameter, stack, fonts, colors, lists, output, budget);
			if (IsTableControl(word))
			{
				output.AddTableControl(
					word,
					rtf.Substring(controlStart, index - controlStart + 1),
					projectedStart,
					output.TextLength - projectedStart);
			}
		}

		private static void AppendFieldInstructionControl(
			ParserFrame frame,
			ReadOnlySpan<char> word,
			bool hasParameter,
			int parameter)
		{
			if (frame.DestinationText.Length + word.Length + 16 > MaxFieldInstructionLength)
			{
				throw new ArgumentException("The RTF field instruction is too large.");
			}

			frame.DestinationText.Append('\\').Append(word);
			if (hasParameter)
			{
				frame.DestinationText.Append(parameter.ToString(CultureInfo.InvariantCulture));
			}
			frame.DestinationText.Append(' ');
		}

		private static void HandleControl(
			ReadOnlySpan<char> word,
			bool hasParameter,
			int parameter,
			List<ParserFrame> stack,
			Dictionary<int, RtfFont> fonts,
			Dictionary<int, global::Windows.UI.Color> colors,
			Dictionary<int, RtfParsedList> lists,
			ParsedFragmentBuilder output,
			ParseBudget budget)
		{
			var frame = stack[stack.Count - 1];
			var state = frame.State;
			if (frame.Destination == ParserDestination.Ignore)
			{
				if (word.SequenceEqual("ud") && frame.IsUnicodeAlternativeBranch)
				{
					frame.Destination = frame.UnicodeAlternativeDestination;
				}
				else if ((IsIgnoredStandardDestination(word) || frame.StarDestination)
					&& IsPreservableDestination(word))
				{
					frame.PreserveOpaqueDestination = true;
				}
				frame.StarDestination = false;
				return;
			}
			if (IsIgnoredStandardDestination(word))
			{
				frame.Destination = ParserDestination.Ignore;
				frame.PreserveOpaqueDestination = IsPreservableDestination(word);
				frame.StarDestination = false;
				return;
			}

			switch (word)
			{
				case "fonttbl":
				case "colortbl":
				case "listtable":
				case "listoverridetable":
				case "listtext":
				case "pntext":
				case "pntxta":
				case "pntxtb":
				case "stylesheet":
				case "info":
					frame.Destination = ParserDestination.Ignore;
					break;
				case "object":
					frame.Destination = ParserDestination.Object;
					frame.IsObjectGroup = true;
					frame.ObjectContext = new ObjectContext();
					break;
				case "objclass":
					frame.Destination = ParserDestination.ObjectClass;
					break;
				case "objname":
					frame.Destination = ParserDestination.ObjectName;
					break;
				case "objdata":
					frame.Destination = ParserDestination.Ignore;
					break;
				case "result":
					if (frame.ObjectContext is not null)
					{
						frame.Destination = ParserDestination.ObjectResult;
						frame.InObjectResult = true;
					}
					else
					{
						frame.Destination = ParserDestination.Ignore;
					}
					break;
				case "objw" when hasParameter:
					if (frame.ObjectContext is { } widthContext)
					{
						widthContext.WidthGoal = Math.Clamp(parameter, 0, InlineImageState.MaxDimension * 15);
					}
					break;
				case "objh" when hasParameter:
					if (frame.ObjectContext is { } heightContext)
					{
						heightContext.HeightGoal = Math.Clamp(parameter, 0, InlineImageState.MaxDimension * 15);
					}
					break;
				case "pict":
					frame.Destination = ParserDestination.Picture;
					frame.IsPictureGroup = true;
					frame.PicturePayload = new PicturePayload();
					break;
				case "pngblip":
					frame.PictureEncoding = InlineImageEncoding.Png;
					break;
				case "jpegblip":
					frame.PictureEncoding = InlineImageEncoding.Jpeg;
					break;
				case "dibitmap":
				case "wbitmap":
					frame.PictureEncoding = InlineImageEncoding.Dib;
					break;
				case "picw" when hasParameter:
					frame.PictureWidth = parameter;
					break;
				case "pich" when hasParameter:
					frame.PictureHeight = parameter;
					break;
				case "picwgoal" when hasParameter:
					frame.PictureWidthGoal = parameter;
					break;
				case "pichgoal" when hasParameter:
					frame.PictureHeightGoal = parameter;
					break;
				case "field":
					frame.IsField = true;
					break;
				case "fldinst":
					frame.Destination = ParserDestination.FieldInstruction;
					break;
				case "unoimage":
					frame.Destination = ParserDestination.InlineImage;
					break;
				case "unoobject":
					frame.Destination = ParserDestination.ObjectFallback;
					break;
				case "unochar":
					frame.Destination = ParserDestination.CharacterFormat;
					break;
				case "unopara":
					frame.Destination = ParserDestination.ParagraphFormat;
					break;
				case "unoterminal":
					frame.Destination = ParserDestination.TerminalParagraph;
					break;
				case "pn":
					frame.Destination = ParserDestination.LegacyList;
					break;
				case "upr":
					frame.IsUnicodeAlternative = true;
					break;
				case "ud" when frame.IsUnicodeAlternativeBranch:
					frame.Destination = frame.UnicodeAlternativeDestination;
					break;
				case "fldrslt":
					frame.Destination = ParserDestination.Normal;
					for (var i = stack.Count - 2; i >= 0; i--)
					{
						if (stack[i].IsField)
						{
							state.Character.Link = stack[i].FieldUrl;
							state.Character.LinkAnchor = stack[i].FieldAnchor;
							state.Character.TextObjectIdentity = stack[i].FieldIdentity;
							break;
						}
					}
					break;
				case "plain":
					var link = state.Character.Link;
					var linkAnchor = state.Character.LinkAnchor;
					var textObjectIdentity = state.Character.TextObjectIdentity;
					state.Character = new CharacterFormatState
					{
						Link = link,
						LinkAnchor = linkAnchor,
						TextObjectIdentity = textObjectIdentity,
						Name = state.DefaultFontName,
						LanguageTag = state.DefaultLanguageLcid is { } defaultLanguage
							&& TryGetLanguageTag(defaultLanguage, out var defaultLanguageTag)
								? defaultLanguageTag
								: string.Empty,
					};
					state.LanguageLcid = state.DefaultLanguageLcid;
					state.EastAsianLanguageLcid = state.DefaultEastAsianLanguageLcid;
					state.CurrentCharacterSet = RtfCharacterSet.Default;
					state.CurrentFontIndex = state.DefaultFontIndex;
					state.SetCodePage(state.DefaultFontIndex is { } defaultFontIndex
						&& fonts.TryGetValue(defaultFontIndex, out var defaultFont)
							? defaultFont.CodePage ?? state.DocumentCodePage
							: state.DocumentCodePage);
					break;
				case "ansi":
					state.DocumentCodePage = 1252;
					state.SetCodePage(1252);
					break;
				case "ansicpg" when hasParameter:
					state.DocumentCodePage = ValidateCodePage(parameter);
					state.SetCodePage(state.CurrentFontIndex is { } currentFontIndex
						&& fonts.TryGetValue(currentFontIndex, out var currentFont)
							? currentFont.CodePage ?? state.DocumentCodePage
							: state.DocumentCodePage);
					break;
				case "cpg" when hasParameter:
					state.SetCodePage(ValidateCodePage(parameter));
					break;
				case "pard":
					state.Paragraph = new ParagraphFormatState();
					state.LineSpacingTwips = null;
					state.LineSpacingMultiple = null;
					state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Left;
					state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Spaces;
					state.CurrentListOverride = null;
					break;
				case "caps": state.Character.AllCaps = !hasParameter || parameter != 0; break;
				case "b":
					state.Character.Bold = !hasParameter || parameter != 0;
					state.Character.Weight = state.Character.Bold ? 700 : 400;
					state.Character.WeightExplicit = true;
					break;
				case "highlight" when hasParameter: state.Character.Background = colors.TryGetValue(parameter, out var background) ? background : null; break;
				case "v": state.Character.Hidden = !hasParameter || parameter != 0; break;
				case "i": state.Character.Italic = !hasParameter || parameter != 0; break;
				case "outl": state.Character.Outline = !hasParameter || parameter != 0; break;
				case "protect": state.Character.ProtectedText = !hasParameter || parameter != 0; break;
				case "scaps": state.Character.SmallCaps = !hasParameter || parameter != 0; break;
				case "ul": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Single); break;
				case "ulw": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Words); break;
				case "uldb": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Double); break;
				case "uld": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Dotted); break;
				case "uldash": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Dash); break;
				case "uldashd": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.DashDot); break;
				case "uldashdd": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.DashDotDot); break;
				case "ulwave": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Wave); break;
				case "ulth": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Thick); break;
				case "ulhair": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.Thin); break;
				case "uldbwave":
				case "ululdbwave": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.DoubleWave); break;
				case "ulhwave": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.HeavyWave); break;
				case "ulldash": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.LongDash); break;
				case "ulthdash": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.ThickDash); break;
				case "ulthdashd": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.ThickDashDot); break;
				case "ulthdashdd": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot); break;
				case "ulthd": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.ThickDotted); break;
				case "ulthldash": state.Character.Underline = ResolveUnderline(hasParameter, parameter, global::Microsoft.UI.Text.UnderlineType.ThickLongDash); break;
				case "ulnone": state.Character.Underline = global::Microsoft.UI.Text.UnderlineType.None; break;
				case "strike": state.Character.Strikethrough = !hasParameter || parameter != 0; break;
				case "sub": state.Character.Subscript = true; state.Character.Superscript = false; break;
				case "super": state.Character.Superscript = true; state.Character.Subscript = false; break;
				case "nosupersub": state.Character.Subscript = state.Character.Superscript = false; break;
				case "f" when hasParameter:
				case "af" when hasParameter:
					if (fonts.TryGetValue(parameter, out var font))
					{
						state.Character.Name = font.Name;
						state.CurrentFontIndex = parameter;
						state.SetCodePage(font.CodePage ?? state.DocumentCodePage);
					}
					break;
				case "deflang" when hasParameter:
					ApplyDefaultLanguage(state, parameter, eastAsian: false);
					break;
				case "deflangfe" when hasParameter:
					ApplyDefaultLanguage(state, parameter, eastAsian: true);
					break;
				case "lang" when hasParameter:
					ApplyLanguage(state, parameter, eastAsian: false);
					break;
				case "langfe" when hasParameter:
					ApplyLanguage(state, parameter, eastAsian: true);
					break;
				case "rtlch":
					ApplyCharacterSet(state, RtfCharacterSet.RightToLeft, fonts);
					break;
				case "ltrch":
					ApplyCharacterSet(state, RtfCharacterSet.LeftToRight, fonts);
					break;
				case "loch":
					ApplyCharacterSet(state, RtfCharacterSet.LowAnsi, fonts);
					break;
				case "hich":
					ApplyCharacterSet(state, RtfCharacterSet.HighAnsi, fonts);
					break;
				case "dbch":
					ApplyCharacterSet(state, RtfCharacterSet.DoubleByte, fonts);
					break;
				case "fs" when hasParameter: state.Character.Size = Math.Clamp(parameter / 2f, 0, 4096); break;
				case "cf" when hasParameter: state.Character.Foreground = colors.TryGetValue(parameter, out var color) ? color : null; break;
				case "expndtw" when hasParameter: state.Character.Spacing = Math.Clamp(parameter / 20f, -4096, 4096); break;
				case "kerning" when hasParameter: state.Character.Kerning = Math.Clamp(parameter / 2f, 0, 4096); break;
				case "up" when hasParameter: state.Character.Position = Math.Clamp(parameter / 2f, 0, 4096); break;
				case "dn" when hasParameter: state.Character.Position = -Math.Clamp(parameter / 2f, 0, 4096); break;
				case "ql": state.Paragraph.Alignment = global::Microsoft.UI.Text.ParagraphAlignment.Left; break;
				case "qc": state.Paragraph.Alignment = global::Microsoft.UI.Text.ParagraphAlignment.Center; break;
				case "qr": state.Paragraph.Alignment = global::Microsoft.UI.Text.ParagraphAlignment.Right; break;
				case "qj": state.Paragraph.Alignment = global::Microsoft.UI.Text.ParagraphAlignment.Justify; break;
				case "fi" when hasParameter: state.Paragraph.FirstLineIndent = Math.Clamp(parameter / 20f, -MaxParagraphMetric, MaxParagraphMetric); break;
				case "li" when hasParameter: state.Paragraph.LeftIndent = Math.Clamp(parameter / 20f, -MaxParagraphMetric, MaxParagraphMetric); break;
				case "ri" when hasParameter: state.Paragraph.RightIndent = Math.Clamp(parameter / 20f, -MaxParagraphMetric, MaxParagraphMetric); break;
				case "sb" when hasParameter: state.Paragraph.SpaceBefore = Math.Clamp(parameter / 20f, -MaxParagraphMetric, MaxParagraphMetric); break;
				case "sa" when hasParameter: state.Paragraph.SpaceAfter = Math.Clamp(parameter / 20f, -MaxParagraphMetric, MaxParagraphMetric); break;
				case "sl" when hasParameter:
					state.LineSpacingTwips = parameter;
					ApplyLineSpacing(state);
					break;
				case "slmult" when hasParameter:
					state.LineSpacingMultiple = parameter != 0;
					ApplyLineSpacing(state);
					break;
				case "tqc": state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Center; break;
				case "tqr": state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Right; break;
				case "tqdec": state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Decimal; break;
				case "tb": state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Bar; break;
				case "tldot": state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Dots; break;
				case "tlhyph": state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Dashes; break;
				case "tlul": state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Lines; break;
				case "tlth": state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.ThickLines; break;
				case "tleq": state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Equals; break;
				case "tx" when hasParameter:
					AddParagraphTab(state, parameter);
					break;
				case "tqclear":
					state.Paragraph.SetTabs(Array.Empty<ParagraphTab>());
					state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Left;
					state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Spaces;
					break;
				case "rtlpar": state.Paragraph.RightToLeft = true; break;
				case "ltrpar": state.Paragraph.RightToLeft = false; break;
				case "keep": state.Paragraph.KeepTogether = !hasParameter || parameter != 0; break;
				case "keepn": state.Paragraph.KeepWithNext = !hasParameter || parameter != 0; break;
				case "pagebb": state.Paragraph.PageBreakBefore = !hasParameter || parameter != 0; break;
				case "noline": state.Paragraph.NoLineNumber = !hasParameter || parameter != 0; break;
				case "widctlpar": state.Paragraph.WidowControl = !hasParameter || parameter != 0; break;
				case "nowidctlpar": state.Paragraph.WidowControl = false; break;
				case "ls" when hasParameter:
					state.CurrentListOverride = parameter;
					ApplyListDefinition(state, lists);
					break;
				case "ilvl" when hasParameter:
					state.Paragraph.ListLevelIndex = Math.Clamp(parameter, 0, MaxListLevels - 1);
					ApplyListDefinition(state, lists);
					break;
				case "pnlvlblt":
					state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.Bullet;
					state.Paragraph.ListStyle = global::Microsoft.UI.Text.MarkerStyle.Plain;
					break;
				case "pnlvlbody":
					if (state.Paragraph.ListType is global::Microsoft.UI.Text.MarkerType.Undefined or global::Microsoft.UI.Text.MarkerType.None)
					{
						state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.Arabic;
					}
					break;
				case "pndec": state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.Arabic; break;
				case "pnucltr": state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.UppercaseEnglishLetter; break;
				case "pnlcltr": state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.LowercaseEnglishLetter; break;
				case "pnucrm": state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.UppercaseRoman; break;
				case "pnlcrm": state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.LowercaseRoman; break;
				case "pnbcnum":
					state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.BlackCircleWingding;
					state.Paragraph.ListStyle = global::Microsoft.UI.Text.MarkerStyle.Plain;
					break;
				case "pnwcnum":
					state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding;
					state.Paragraph.ListStyle = global::Microsoft.UI.Text.MarkerStyle.Plain;
					break;
				case "pnseq":
					state.Paragraph.ListType = global::Microsoft.UI.Text.MarkerType.UnicodeSequence;
					state.Paragraph.ListStyle = global::Microsoft.UI.Text.MarkerStyle.Plain;
					break;
				case "pnstart" when hasParameter: state.Paragraph.ListStart = Math.Max(0, parameter); break;
				case "pnindent" when hasParameter: state.Paragraph.ListTab = Math.Clamp(parameter / 20f, 0, MaxParagraphMetric); break;
				case "pnql": state.Paragraph.ListAlignment = global::Microsoft.UI.Text.MarkerAlignment.Left; break;
				case "pnqc": state.Paragraph.ListAlignment = global::Microsoft.UI.Text.MarkerAlignment.Center; break;
				case "pnqr": state.Paragraph.ListAlignment = global::Microsoft.UI.Text.MarkerAlignment.Right; break;
				case "uc" when hasParameter: state.UnicodeSkipCount = Math.Max(0, parameter); break;
				case "u" when hasParameter:
					AppendParsedCharacter((char)(short)parameter, frame, output, skipFallback: false, budget: budget);
					frame.UnicodeFallbackRemaining = state.UnicodeSkipCount;
					break;
				case "par": AppendParsedCharacter('\r', frame, output, skipFallback: false, budget: budget); break;
				case "line":
				case "softline": AppendParsedCharacter('\n', frame, output, skipFallback: false, budget: budget); break;
				case "tab":
				case "cell":
				case "nestcell": AppendParsedCharacter('\t', frame, output, skipFallback: false, budget: budget); break;
				case "row":
				case "nestrow": AppendParsedCharacter('\r', frame, output, skipFallback: false, budget: budget); break;
				case "emdash": AppendParsedCharacter('\u2014', frame, output, budget: budget); break;
				case "endash": AppendParsedCharacter('\u2013', frame, output, budget: budget); break;
				case "emspace": AppendParsedCharacter('\u2003', frame, output, budget: budget); break;
				case "enspace": AppendParsedCharacter('\u2002', frame, output, budget: budget); break;
				case "bullet": AppendParsedCharacter('\u2022', frame, output, budget: budget); break;
				case "lquote": AppendParsedCharacter('\u2018', frame, output, budget: budget); break;
				case "rquote": AppendParsedCharacter('\u2019', frame, output, budget: budget); break;
				case "ldblquote": AppendParsedCharacter('\u201c', frame, output, budget: budget); break;
				case "rdblquote": AppendParsedCharacter('\u201d', frame, output, budget: budget); break;
				case "ltrmark": AppendParsedCharacter('\u200e', frame, output, budget: budget); break;
				case "rtlmark": AppendParsedCharacter('\u200f', frame, output, budget: budget); break;
				default:
					if (frame.StarDestination)
					{
						frame.Destination = ParserDestination.Ignore;
						frame.PreserveOpaqueDestination = IsPreservableDestination(word);
					}
					break;
			}
			frame.StarDestination = false;
		}

		private static void ApplyLanguage(ParserState state, int lcid, bool eastAsian)
		{
			var normalized = NormalizeLanguageLcid(lcid);
			if (eastAsian)
			{
				state.EastAsianLanguageLcid = normalized;
				if (state.CurrentCharacterSet != RtfCharacterSet.DoubleByte)
				{
					return;
				}
			}
			else
			{
				state.LanguageLcid = normalized;
				if (state.CurrentCharacterSet == RtfCharacterSet.DoubleByte)
				{
					return;
				}
			}

			state.Character.LanguageTag = normalized is { } value && TryGetLanguageTag(value, out var languageTag)
				? languageTag
				: string.Empty;
		}

		private static void ApplyDefaultLanguage(ParserState state, int lcid, bool eastAsian)
		{
			var normalized = NormalizeLanguageLcid(lcid);
			if (eastAsian)
			{
				state.DefaultEastAsianLanguageLcid = normalized;
				if (state.EastAsianLanguageLcid is null)
				{
					ApplyLanguage(state, lcid, eastAsian: true);
				}
			}
			else
			{
				state.DefaultLanguageLcid = normalized;
				if (state.LanguageLcid is null)
				{
					ApplyLanguage(state, lcid, eastAsian: false);
				}
			}
		}

		private static int? NormalizeLanguageLcid(int lcid)
			=> lcid is > 0 and <= ushort.MaxValue ? lcid : null;

		private static void ApplyCharacterSet(
			ParserState state,
			RtfCharacterSet characterSet,
			Dictionary<int, RtfFont> fonts)
		{
			state.CurrentCharacterSet = characterSet;
			var languageLcid = characterSet == RtfCharacterSet.DoubleByte
				? state.EastAsianLanguageLcid ?? state.LanguageLcid
				: state.LanguageLcid;
			state.Character.LanguageTag = languageLcid is { } lcid && TryGetLanguageTag(lcid, out var languageTag)
				? languageTag
				: state.Character.LanguageTag;
			state.Character.TextScript = characterSet switch
			{
				RtfCharacterSet.LeftToRight => global::Microsoft.UI.Text.TextScript.Default,
				RtfCharacterSet.LowAnsi => global::Microsoft.UI.Text.TextScript.Ansi,
				RtfCharacterSet.HighAnsi => ResolveTextScript(
					languageLcid,
					global::Microsoft.UI.Text.TextScript.Default),
				RtfCharacterSet.RightToLeft => ResolveTextScript(
					languageLcid,
					global::Microsoft.UI.Text.TextScript.Arabic),
				RtfCharacterSet.DoubleByte => ResolveTextScript(
					languageLcid,
					GetTextScriptForCurrentFont(state, fonts)),
				_ => state.Character.TextScript,
			};
		}

		private static global::Microsoft.UI.Text.TextScript GetTextScriptForCurrentFont(
			ParserState state,
			Dictionary<int, RtfFont> fonts)
		{
			var codePage = state.CurrentFontIndex is { } fontIndex && fonts.TryGetValue(fontIndex, out var font)
				? font.CodePage
				: state.CodePage;
			return codePage switch
			{
				932 => global::Microsoft.UI.Text.TextScript.ShiftJis,
				936 => global::Microsoft.UI.Text.TextScript.GB2312,
				949 => global::Microsoft.UI.Text.TextScript.Hangul,
				950 => global::Microsoft.UI.Text.TextScript.Big5,
				_ => global::Microsoft.UI.Text.TextScript.Default,
			};
		}

		private static bool IsIgnoredStandardDestination(ReadOnlySpan<char> word)
		{
			switch (word)
			{
				case "header":
				case "headerl":
				case "headerr":
				case "headerf":
				case "footer":
				case "footerl":
				case "footerr":
				case "footerf":
				case "footnote":
				case "ftncn":
				case "ftnsep":
				case "ftnsepc":
				case "aftncn":
				case "aftnsep":
				case "aftnsepc":
				case "annotation":
				case "atnauthor":
				case "atndate":
				case "atnid":
				case "atnparent":
				case "atnref":
				case "atntime":
				case "bkmkstart":
				case "bkmkend":
				case "xe":
				case "tc":
				case "txe":
				case "rxe":
				case "title":
				case "subject":
				case "author":
				case "manager":
				case "company":
				case "operator":
				case "category":
				case "keywords":
				case "comment":
				case "doccomm":
				case "hlinkbase":
				case "creatim":
				case "revtim":
				case "printim":
				case "buptim":
				case "version":
				case "vern":
				case "edmins":
				case "nofpages":
				case "nofwords":
				case "nofchars":
				case "nofcharsws":
				case "id":
				case "filetbl":
				case "file":
				case "fname":
				case "revtbl":
				case "rsidtbl":
				case "protusertbl":
				case "protstart":
				case "protend":
				case "xmlnstbl":
				case "latentstyles":
				case "themedata":
				case "colorschememapping":
				case "generator":
				case "template":
				case "private":
				case "userprops":
				case "docvar":
				case "listpicture":
				case "listname":
				case "liststylename":
				case "pnseclvl":
				case "background":
				case "shp":
				case "shpinst":
				case "shprslt":
				case "shptxt":
				case "sp":
				case "sn":
				case "sv":
				case "do":
				case "dptxbxtext":
				case "xmlopen":
				case "xmlclose":
				case "xmlname":
				case "xmlattrname":
				case "xmlattrvalue":
				case "datastore":
				case "datafield":
				case "databinding":
				case "customxml":
				case "smarttag":
				case "factoidname":
				case "factoidtype":
				case "htmltag":
				case "mhtmltag":
				case "formfield":
				case "ffname":
				case "ffdeftext":
				case "ffl":
				case "ffentrymcr":
				case "ffexitmcr":
				case "ffformat":
				case "ffhelptext":
				case "ffstattext":
				case "fontemb":
				case "fontfile":
				case "falt":
				case "panose":
				case "gridtbl":
				case "keycode":
				case "blipuid":
				case "picprop":
				case "passwordhash":
				case "passwordsalt":
				case "propname":
				case "proptype":
				case "staticval":
				case "linkval":
				case "mailmerge":
					return true;
				default:
					return false;
			}
		}

		private static bool IsPreservableDestination(ReadOnlySpan<char> word)
		{
			switch (word)
			{
				case "header":
				case "headerl":
				case "headerr":
				case "headerf":
				case "footer":
				case "footerl":
				case "footerr":
				case "footerf":
				case "footnote":
				case "ftncn":
				case "ftnsep":
				case "ftnsepc":
				case "aftncn":
				case "aftnsep":
				case "aftnsepc":
				case "annotation":
				case "atnauthor":
				case "atndate":
				case "atnid":
				case "atnparent":
				case "atnref":
				case "atntime":
				case "bkmkstart":
				case "bkmkend":
				case "title":
				case "subject":
				case "author":
				case "manager":
				case "company":
				case "operator":
				case "category":
				case "keywords":
				case "comment":
				case "doccomm":
				case "creatim":
				case "revtim":
				case "printim":
				case "buptim":
				case "version":
				case "vern":
				case "edmins":
				case "nofpages":
				case "nofwords":
				case "nofchars":
				case "nofcharsws":
				case "generator":
					return true;
				default:
					return false;
			}
		}

		private static bool IsUnsafePreservedDestination(ReadOnlySpan<char> word)
			=> word.SequenceEqual("object")
				|| word.SequenceEqual("objdata")
				|| word.SequenceEqual("objclass")
				|| word.SequenceEqual("objname")
				|| word.SequenceEqual("password")
				|| word.StartsWith("passwordhash", StringComparison.Ordinal)
				|| word.SequenceEqual("passwordsalt")
				|| word.StartsWith("prot", StringComparison.Ordinal)
				|| word.SequenceEqual("security")
				|| word.SequenceEqual("private")
				|| word.SequenceEqual("datastore")
				|| word.SequenceEqual("datafield")
				|| word.SequenceEqual("databinding")
				|| word.SequenceEqual("field")
				|| word.SequenceEqual("fldinst")
				|| word.SequenceEqual("dde")
				|| word.SequenceEqual("ddeauto")
				|| word.SequenceEqual("includetext")
				|| word.SequenceEqual("includepicture");

		private static bool ContainsUnsafePreservedDestination(string rtf)
		{
			for (var position = 0; position < rtf.Length; position++)
			{
				if (rtf[position] != '\\')
				{
					continue;
				}

				var controlPosition = position;
				if (TryReadControlWord(rtf, ref controlPosition, out var word, out _, out _))
				{
					if (IsUnsafePreservedDestination(word))
					{
						return true;
					}
					position = controlPosition - 1;
				}
				else
				{
					SkipControl(rtf, ref position);
				}
			}
			return false;
		}

		private static bool IsTableControl(ReadOnlySpan<char> word)
		{
			if (word.SequenceEqual("trowd")
				|| word.SequenceEqual("cell")
				|| word.SequenceEqual("row")
				|| word.SequenceEqual("nestcell")
				|| word.SequenceEqual("nestrow")
				|| word.SequenceEqual("nesttableprops")
				|| word.SequenceEqual("nonesttables")
				|| word.SequenceEqual("intbl")
				|| word.SequenceEqual("itap"))
			{
				return true;
			}

			return word.StartsWith("tr", StringComparison.Ordinal)
				|| word.StartsWith("cl", StringComparison.Ordinal)
				|| word.SequenceEqual("cellx");
		}

		private static void ApplyLineSpacing(ParserState state)
		{
			if (state.LineSpacingTwips is not { } twips)
			{
				return;
			}

			if (twips == 0)
			{
				state.Paragraph.LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.Single;
				state.Paragraph.LineSpacing = 0;
			}
			else if (state.LineSpacingMultiple == true)
			{
				var multiple = Math.Abs(twips) / 240f;
				if (Math.Abs(multiple - 1f) < 0.001f)
				{
					state.Paragraph.LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.Single;
					state.Paragraph.LineSpacing = 0;
				}
				else if (Math.Abs(multiple - 1.5f) < 0.001f)
				{
					state.Paragraph.LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.OneAndHalf;
					state.Paragraph.LineSpacing = 0;
				}
				else if (Math.Abs(multiple - 2f) < 0.001f)
				{
					state.Paragraph.LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.Double;
					state.Paragraph.LineSpacing = 0;
				}
				else
				{
					state.Paragraph.LineSpacingRule = global::Microsoft.UI.Text.LineSpacingRule.Multiple;
					state.Paragraph.LineSpacing = Math.Clamp(multiple, 0, MaxParagraphMetric);
				}
			}
			else
			{
				state.Paragraph.LineSpacingRule = twips < 0
					? global::Microsoft.UI.Text.LineSpacingRule.Exactly
					: global::Microsoft.UI.Text.LineSpacingRule.AtLeast;
				state.Paragraph.LineSpacing = Math.Clamp(Math.Abs(twips) / 20f, 0, MaxParagraphMetric);
			}
		}

		private static void AddParagraphTab(ParserState state, int twips)
		{
			if (twips < 0 || state.Paragraph.Tabs.Count >= ParagraphFormatState.MaxTabs)
			{
				throw new ArgumentException("The RTF paragraph tab is invalid.");
			}

			var tabs = new List<ParagraphTab>(state.Paragraph.Tabs.Count + 1);
			tabs.AddRange(state.Paragraph.Tabs);
			tabs.Add(new ParagraphTab(
				Math.Clamp(twips / 20f, 0, MaxParagraphMetric),
				state.PendingTabAlignment,
				state.PendingTabLeader));
			state.Paragraph.SetTabs(tabs);
			state.PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Left;
			state.PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Spaces;
		}

		private static void ApplyListDefinition(ParserState state, Dictionary<int, RtfParsedList> lists)
		{
			if (state.CurrentListOverride is not { } overrideId
				|| !lists.TryGetValue(overrideId, out var list)
				|| list.Levels.Count == 0)
			{
				return;
			}

			var levelIndex = Math.Clamp(state.Paragraph.ListLevelIndex, 0, list.Levels.Count - 1);
			var level = list.Levels[levelIndex];
			state.Paragraph.ListType = level.Type;
			state.Paragraph.ListStyle = level.Style;
			state.Paragraph.ListAlignment = level.Alignment;
			state.Paragraph.ListLevelIndex = levelIndex;
			state.Paragraph.ListStart = level.Start;
			state.Paragraph.ListTab = level.Tab;
		}

		private static global::Microsoft.UI.Text.UnderlineType ResolveUnderline(
			bool hasParameter,
			int parameter,
			global::Microsoft.UI.Text.UnderlineType underline)
			=> hasParameter && parameter == 0 ? global::Microsoft.UI.Text.UnderlineType.None : underline;

		private static void AppendParsedCharacter(
			char value,
			ParserFrame frame,
			ParsedFragmentBuilder output,
			bool skipFallback = true,
			ParseBudget? budget = null)
		{
			budget ??= new ParseBudget(HardMaxParsedCharacters, truncateAtLimit: false);
			if (skipFallback && frame.UnicodeFallbackRemaining > 0)
			{
				frame.UnicodeFallbackRemaining--;
				return;
			}

			if (frame.Destination is ParserDestination.Ignore or ParserDestination.LegacyList)
			{
				return;
			}
			if (frame.Destination == ParserDestination.Object)
			{
				return;
			}
			if (frame.Destination == ParserDestination.TerminalParagraph)
			{
				return;
			}

			if (frame.Destination == ParserDestination.Picture)
			{
				if (!char.IsWhiteSpace(value))
				{
					(frame.PicturePayload ??= new PicturePayload()).AppendHex(value);
				}
				return;
			}

			if (frame.Destination is ParserDestination.ObjectClass or ParserDestination.ObjectName)
			{
				if (frame.DestinationText.Length >= MaxObjectMetadataLength)
				{
					throw new ArgumentException("The RTF object metadata is too large.");
				}
				frame.DestinationText.Append(value);
				return;
			}

			if (frame.Destination == ParserDestination.ObjectResult && frame.ObjectContext is { } objectContext)
			{
				objectContext.AppendResult(value, frame.State.Character, frame.State.Paragraph);
				return;
			}

			if (frame.Destination is ParserDestination.FieldInstruction or ParserDestination.InlineImage or ParserDestination.ObjectFallback or ParserDestination.CharacterFormat or ParserDestination.ParagraphFormat)
			{
				var limit = frame.Destination switch
				{
					ParserDestination.FieldInstruction => MaxFieldInstructionLength,
					ParserDestination.InlineImage => MaxInlineImageMetadataLength,
					ParserDestination.ObjectFallback => MaxInlineImageMetadataLength,
					ParserDestination.CharacterFormat => MaxCharacterMetadataLength,
					ParserDestination.ParagraphFormat => MaxParagraphMetadataLength,
					_ => 0,
				};
				if (frame.DestinationText.Length >= limit)
				{
					throw new ArgumentException("The RTF destination metadata is too large.");
				}
				frame.DestinationText.Append(value);
				return;
			}

			if (value == '\ufffc' && frame.PendingInlineImage is { } pendingInlineImage)
			{
				frame.PendingInlineImage = null;
				AppendParsedImage(pendingInlineImage, frame.State.Character, frame.State.Paragraph, output, budget);
			}
			else
			{
				output.Append(value, frame.State.Character, frame.State.Paragraph, budget);
			}
		}

		private static void AppendEncodedByte(
			byte value,
			ParserFrame frame,
			ParsedFragmentBuilder output,
			ParseBudget budget)
		{
			if (frame.UnicodeFallbackRemaining > 0)
			{
				frame.UnicodeFallbackRemaining--;
				return;
			}

			if (frame.Destination == ParserDestination.Ignore)
			{
				return;
			}
			if (frame.Destination == ParserDestination.Picture)
			{
				(frame.PicturePayload ??= new PicturePayload()).AppendBinary(value);
				return;
			}

			var state = frame.State;
			state.DecoderByteBuffer[0] = value;
			try
			{
				state.Decoder.Convert(
					state.DecoderByteBuffer,
					0,
					1,
					state.DecoderCharacterBuffer,
					0,
					state.DecoderCharacterBuffer.Length,
					flush: false,
					out var bytesUsed,
					out var charactersUsed,
					out _);
				state.DecoderPendingByteCount += bytesUsed;
				if (charactersUsed > 0)
				{
					state.DecoderPendingByteCount = 0;
					for (var i = 0; i < charactersUsed; i++)
					{
						AppendParsedCharacter(
							state.DecoderCharacterBuffer[i],
							frame,
							output,
							skipFallback: false,
							budget: budget);
					}
				}
			}
			catch (DecoderFallbackException error)
			{
				throw new ArgumentException("The RTF contains invalid encoded text.", nameof(value), error);
			}
		}

		private static void FlushDecoder(
			ParserFrame frame,
			ParsedFragmentBuilder output,
			ParseBudget budget)
		{
			if (!frame.State.DecoderHasPendingBytes)
			{
				return;
			}

			var state = frame.State;
			try
			{
				state.Decoder.Convert(
					Array.Empty<byte>(),
					0,
					0,
					state.DecoderCharacterBuffer,
					0,
					state.DecoderCharacterBuffer.Length,
					flush: true,
					out _,
					out var charactersUsed,
					out _);
				for (var i = 0; i < charactersUsed; i++)
				{
					AppendParsedCharacter(
						state.DecoderCharacterBuffer[i],
						frame,
						output,
						skipFallback: false,
						budget: budget);
				}
				state.ResetDecoder();
			}
			catch (DecoderFallbackException error)
			{
				throw new ArgumentException("The RTF contains an incomplete encoded character.", nameof(frame), error);
			}
		}

		private sealed class ParsedFragmentBuilder
		{
			private readonly StringBuilder _text = new();
			private readonly List<FormatRun> _characterRuns = new();
			private readonly List<ParagraphRun> _paragraphRuns = new();
			private readonly List<RtfPreservedEntry> _opaqueEntries = new();
			private readonly List<PreservedTableRegion> _tableRegions = new();
			private readonly Stack<PreservedTableRegion> _openTableRegions = new();
			private bool _hasExplicitTerminalParagraphState;
			private int _preservedLength;
			private int _preservedEntryCount;
			private int _nextRegionId = 1;
			private int _nextSequence;

			internal int TextLength => _text.Length;

			internal void Append(
				char value,
				CharacterFormatState character,
				ParagraphFormatState paragraph,
				ParseBudget budget,
				bool takeCharacterOwnership = false)
			{
				if (!budget.CanAppend(_text.Length))
				{
					return;
				}

				var addCharacterRun = _characterRuns.Count == 0
					|| !CharacterFormatState.CanCoalesce(_characterRuns[^1].Format, character);
				var addParagraphRun = _paragraphRuns.Count == 0
					|| !_paragraphRuns[^1].Format.Equals(paragraph);
				budget.RecordFormattingRuns(addCharacterRun, addParagraphRun);
				_hasExplicitTerminalParagraphState = false;
				_text.Append(value);
				if (addCharacterRun)
				{
					_characterRuns.Add(new FormatRun(
						1,
						takeCharacterOwnership ? character : character.Clone()));
				}
				else
				{
					_characterRuns[^1].Length++;
				}

				if (addParagraphRun)
				{
					_paragraphRuns.Add(new ParagraphRun(1, paragraph.Clone()));
				}
				else
				{
					_paragraphRuns[^1].Length++;
				}
			}

			internal void AddOpaqueGroup(string rtf, int anchor)
			{
				if (rtf.Length > MaxPreservedRtfGroupLength)
				{
					throw new ArgumentException("An opaque RTF destination is too large.");
				}
				RecordPreservedLength(rtf.Length);
				_opaqueEntries.Add(new RtfPreservedEntry(
					rtf,
					anchor,
					0,
					anchor,
					0,
					_nextRegionId++,
					0,
					_nextSequence++));
			}

			internal void AddTableControl(
				ReadOnlySpan<char> word,
				string rtf,
				int anchor,
				int projectedLength)
			{
				PreservedTableRegion? region;
				if (word.SequenceEqual("trowd"))
				{
					var parentRegionId = _openTableRegions.Count == 0 ? 0 : _openTableRegions.Peek().Id;
					region = new PreservedTableRegion(_nextRegionId++, parentRegionId, anchor);
					_tableRegions.Add(region);
					_openTableRegions.Push(region);
				}
				else
				{
					region = _openTableRegions.Count == 0 ? null : _openTableRegions.Peek();
				}

				if (region is null)
				{
					return;
				}

				RecordPreservedLength(rtf.Length);
				region.Entries.Add(new PendingTableEntry(rtf, anchor, projectedLength, _nextSequence++));
				if (word.SequenceEqual("row") || word.SequenceEqual("nestrow"))
				{
					region.End = anchor + projectedLength;
					region.IsClosed = true;
					_openTableRegions.Pop();
				}
			}

			internal RichTextFragment Build(
				ParagraphFormatState terminalParagraphState,
				bool canUseTerminalParagraphState)
			{
				var metadata = new List<RtfPreservedEntry>(_opaqueEntries);
				foreach (var region in _tableRegions)
				{
					if (!region.IsClosed || region.End <= region.Start)
					{
						continue;
					}
					foreach (var entry in region.Entries)
					{
						metadata.Add(new RtfPreservedEntry(
							entry.Rtf,
							entry.Anchor,
							entry.ProjectedLength,
							region.Start,
							region.End - region.Start,
							region.Id,
							region.ParentRegionId,
							entry.Sequence));
					}
				}
				metadata.Sort(static (left, right) =>
				{
					var anchor = left.Anchor.CompareTo(right.Anchor);
					return anchor != 0 ? anchor : left.Sequence.CompareTo(right.Sequence);
				});

				return new(
					_text.ToString(),
					_characterRuns,
					_paragraphRuns,
					terminalParagraphState,
					canUseTerminalParagraphState,
					canUseTerminalParagraphState && _hasExplicitTerminalParagraphState,
					metadata.Count == 0 ? RtfPreservedMetadata.Empty : new RtfPreservedMetadata(metadata));
			}

			internal void MarkExplicitTerminalParagraphState()
				=> _hasExplicitTerminalParagraphState = true;

			private void RecordPreservedLength(int length)
			{
				if (++_preservedEntryCount > MaxPreservedRtfEntries
					|| length > MaxPreservedRtfTotalLength - _preservedLength)
				{
					throw new ArgumentException("The RTF contains too much preserved metadata.");
				}
				_preservedLength += length;
			}

			private sealed class PreservedTableRegion
			{
				internal PreservedTableRegion(int id, int parentRegionId, int start)
				{
					Id = id;
					ParentRegionId = parentRegionId;
					Start = start;
				}

				internal int Id { get; }
				internal int ParentRegionId { get; }
				internal int Start { get; }
				internal int End { get; set; }
				internal bool IsClosed { get; set; }
				internal List<PendingTableEntry> Entries { get; } = new();
			}

			private readonly record struct PendingTableEntry(
				string Rtf,
				int Anchor,
				int ProjectedLength,
				int Sequence);
		}

		private sealed class ParseBudget
		{
			private readonly int _maxCharacters;
			private readonly bool _truncateAtLimit;
			private int _formatTransitions;
			private int _characterRuns;
			private int _paragraphRuns;
			private int _pictureAttempts;
			private int _textObjects;

			internal ParseBudget(int maxCharacters, bool truncateAtLimit)
			{
				_maxCharacters = maxCharacters;
				_truncateAtLimit = truncateAtLimit;
			}

			internal bool WasTruncated { get; private set; }

			internal void RecordFormattingRuns(bool characterRun, bool paragraphRun)
			{
				if ((characterRun || paragraphRun) && ++_formatTransitions > MaxParsedFormatRuns)
				{
					throw new ArgumentException("The RTF contains too many formatting transitions.");
				}
				if (characterRun && ++_characterRuns > MaxParsedFormatRuns)
				{
					throw new ArgumentException("The RTF contains too many character-formatting runs.");
				}
				if (paragraphRun && ++_paragraphRuns > MaxParsedFormatRuns)
				{
					throw new ArgumentException("The RTF contains too many paragraph-formatting runs.");
				}
			}

			internal bool CanAppend(int textLength)
			{
				if (textLength < _maxCharacters)
				{
					return true;
				}

				if (!_truncateAtLimit)
				{
					throw new ArgumentException("The RTF text content is too large.");
				}

				WasTruncated = true;
				return false;
			}

			internal void RecordTextObject()
			{
				if (++_textObjects > MaxParsedImages)
				{
					throw new ArgumentException("The RTF contains too many embedded text objects.");
				}
			}

			internal void RecordPictureAttempt()
			{
				if (++_pictureAttempts > MaxParsedImages)
				{
					throw new ArgumentException("The RTF contains too many embedded picture attempts.");
				}
			}
		}

		private sealed class ParseWorkBudget
		{
			private int _controlTokens;

			internal void RecordControl()
			{
				if (++_controlTokens > MaxParsedControlTokens)
				{
					throw new ArgumentException("The RTF contains too many control tokens.");
				}
			}
		}

		private static bool TryDecodeHexByte(char high, char low, out byte value)
		{
			if (!TryDecodeHexNibble(high, out var highNibble) || !TryDecodeHexNibble(low, out var lowNibble))
			{
				value = 0;
				return false;
			}

			value = (byte)((highNibble << 4) | lowNibble);
			return true;
		}

		private static bool TryDecodeHexNibble(char value, out int nibble)
		{
			if (value is >= '0' and <= '9')
			{
				nibble = value - '0';
				return true;
			}

			if (value is >= 'a' and <= 'f')
			{
				nibble = value - 'a' + 10;
				return true;
			}

			if (value is >= 'A' and <= 'F')
			{
				nibble = value - 'A' + 10;
				return true;
			}

			nibble = 0;
			return false;
		}

		private static char DecodeWindows1252(byte value)
		{
			const string replacements = "\u20ac\u0081\u201a\u0192\u201e\u2026\u2020\u2021\u02c6\u2030\u0160\u2039\u0152\u008d\u017d\u008f\u0090\u2018\u2019\u201c\u201d\u2022\u2013\u2014\u02dc\u2122\u0161\u203a\u0153\u009d\u017e\u0178";
			return value is >= 0x80 and <= 0x9f ? replacements[value - 0x80] : (char)value;
		}

		private static int ValidateCodePage(int codePage)
		{
			if (codePage <= 0)
			{
				throw new ArgumentException("The RTF code page is invalid.", nameof(codePage));
			}

			_ = GetRtfEncoding(codePage);
			return codePage;
		}

		private static Encoding GetRtfEncoding(int codePage)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			try
			{
				return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
			}
			catch (ArgumentException error)
			{
				throw new ArgumentException("The RTF code page is unsupported.", nameof(codePage), error);
			}
		}

		private readonly record struct RtfFont(string Name, int? CodePage, int? Charset);
		private readonly record struct RtfHyperlink(string Link, string? Anchor);
		private readonly record struct RtfGroup(int Start, int End, int ContentStart, string Destination);
		private readonly record struct LevelTextInfo(string Text, bool HasNumberPlaceholder, int? FontIndex);
		private readonly record struct RtfParsedListLevel(
			global::Microsoft.UI.Text.MarkerType Type,
			global::Microsoft.UI.Text.MarkerStyle Style,
			global::Microsoft.UI.Text.MarkerAlignment Alignment,
			int Start,
			float Tab);
		private sealed record RtfParsedList(List<RtfParsedListLevel> Levels);
		private readonly record struct RtfListKey(
			global::Microsoft.UI.Text.MarkerType Type,
			global::Microsoft.UI.Text.MarkerStyle Style,
			global::Microsoft.UI.Text.MarkerAlignment Alignment,
			int Level,
			int Start,
			float Tab)
		{
			internal static RtfListKey FromState(ParagraphFormatState state)
				=> new(
					state.ListType,
					state.ListStyle == global::Microsoft.UI.Text.MarkerStyle.Undefined
						? global::Microsoft.UI.Text.MarkerStyle.Period
						: state.ListStyle,
					state.ListAlignment == global::Microsoft.UI.Text.MarkerAlignment.Undefined
						? global::Microsoft.UI.Text.MarkerAlignment.Left
						: state.ListAlignment,
					Math.Clamp(state.ListLevelIndex, 0, MaxListLevels - 1),
					state.ListStart,
					state.ListTab);
		}
		private readonly record struct RtfListInfo(int ListId, int OverrideId);

		private enum ParserDestination
		{
			Normal,
			Ignore,
			Object,
			ObjectResult,
			ObjectClass,
			ObjectName,
			FieldInstruction,
			InlineImage,
			ObjectFallback,
			Picture,
			CharacterFormat,
			ParagraphFormat,
			TerminalParagraph,
			LegacyList,
		}

		private enum RtfCharacterSet
		{
			Default,
			LeftToRight,
			RightToLeft,
			LowAnsi,
			HighAnsi,
			DoubleByte,
		}

		private sealed class ParserState
		{
			public CharacterFormatState Character = new();
			public ParagraphFormatState Paragraph = new();
			public int UnicodeSkipCount = 1;
			public string? DefaultFontName;
			public int? DefaultFontIndex;
			public int? CurrentFontIndex;
			public int? DefaultLanguageLcid;
			public int? DefaultEastAsianLanguageLcid;
			public int? LanguageLcid;
			public int? EastAsianLanguageLcid;
			public RtfCharacterSet CurrentCharacterSet;
			public int DocumentCodePage = 1252;
			public int CodePage = 1252;
			public Decoder Decoder = GetRtfEncoding(1252).GetDecoder();
			public readonly byte[] DecoderByteBuffer = new byte[1];
			public readonly char[] DecoderCharacterBuffer = new char[4];
			public int DecoderPendingByteCount;
			public int? LineSpacingTwips;
			public bool? LineSpacingMultiple;
			public global::Microsoft.UI.Text.TabAlignment PendingTabAlignment = global::Microsoft.UI.Text.TabAlignment.Left;
			public global::Microsoft.UI.Text.TabLeader PendingTabLeader = global::Microsoft.UI.Text.TabLeader.Spaces;
			public int? CurrentListOverride;

			public bool DecoderHasPendingBytes => DecoderPendingByteCount != 0;

			public void SetCodePage(int codePage)
			{
				CodePage = codePage;
				Decoder = GetRtfEncoding(codePage).GetDecoder();
				DecoderPendingByteCount = 0;
			}

			public void ResetDecoder()
			{
				Decoder.Reset();
				DecoderPendingByteCount = 0;
			}

			public ParserState Clone()
				=> new()
				{
					Character = Character.Clone(),
					Paragraph = Paragraph.Clone(),
					UnicodeSkipCount = UnicodeSkipCount,
					DefaultFontName = DefaultFontName,
					DefaultFontIndex = DefaultFontIndex,
					CurrentFontIndex = CurrentFontIndex,
					DefaultLanguageLcid = DefaultLanguageLcid,
					DefaultEastAsianLanguageLcid = DefaultEastAsianLanguageLcid,
					LanguageLcid = LanguageLcid,
					EastAsianLanguageLcid = EastAsianLanguageLcid,
					CurrentCharacterSet = CurrentCharacterSet,
					DocumentCodePage = DocumentCodePage,
					CodePage = CodePage,
					Decoder = GetRtfEncoding(CodePage).GetDecoder(),
					LineSpacingTwips = LineSpacingTwips,
					LineSpacingMultiple = LineSpacingMultiple,
					PendingTabAlignment = PendingTabAlignment,
					PendingTabLeader = PendingTabLeader,
					CurrentListOverride = CurrentListOverride,
				};
		}

		private sealed class ParserFrame
		{
			public ParserState State;
			public ParserDestination Destination;
			public bool StarDestination;
			public bool PreserveOpaqueDestination;
			public int GroupStart;
			public int GroupEnd;
			public int ProjectedStart;
			public bool IsField;
			public bool IsObjectGroup;
			public bool InObjectResult;
			public string? FieldUrl;
			public string? FieldAnchor;
			public RichEditTextObjectIdentity? FieldIdentity;
			public ObjectContext? ObjectContext;
			public InlineImageState? PendingInlineImage;
			public int UnicodeFallbackRemaining;
			public bool IsPictureGroup;
			public bool IsUnicodeAlternative;
			public bool IsUnicodeAlternativeBranch;
			public int UnicodeAlternativeChildCount;
			public ParserDestination UnicodeAlternativeDestination;
			public InlineImageEncoding PictureEncoding;
			public PicturePayload? PicturePayload;
			public int PictureWidth;
			public int PictureHeight;
			public int PictureWidthGoal;
			public int PictureHeightGoal;
			public StringBuilder DestinationText = new();

			public ParserFrame(ParserState state)
			{
				State = state;
			}

			public ParserFrame CreateChild()
			{
				var child = new ParserFrame(State.Clone())
				{
					Destination = Destination,
					InObjectResult = InObjectResult,
					ObjectContext = ObjectContext,
					PictureEncoding = PictureEncoding,
					PicturePayload = PicturePayload,
				};
				if (IsUnicodeAlternative)
				{
					child.IsUnicodeAlternativeBranch = ++UnicodeAlternativeChildCount == 2;
					child.UnicodeAlternativeDestination = Destination;
					child.Destination = ParserDestination.Ignore;
				}
				return child;
			}
		}

		private sealed class ObjectContext
		{
			public string? ObjectClass;
			public string? ObjectName;
			public int WidthGoal;
			public int HeightGoal;
			public InlineImageState? Picture;
			public CharacterFormatState? PictureCharacter;
			public ParagraphFormatState? PictureParagraph;
			public StringBuilder ResultText { get; } = new();
			public CharacterFormatState? ResultCharacter;
			public ParagraphFormatState? ResultParagraph;

			public void SetPicture(
				InlineImageState picture,
				CharacterFormatState character,
				ParagraphFormatState paragraph)
			{
				if (Picture is null)
				{
					Picture = picture;
					PictureCharacter = character.Clone();
					PictureParagraph = paragraph.Clone();
				}
			}

			public void AppendResult(
				char value,
				CharacterFormatState character,
				ParagraphFormatState paragraph)
			{
				if (ResultText.Length >= MaxObjectResultTextLength)
				{
					throw new ArgumentException("The RTF object result text is too large.");
				}

				ResultCharacter ??= character.Clone();
				ResultParagraph ??= paragraph.Clone();
				ResultText.Append(value);
			}
		}

		private sealed class PicturePayload
		{
			private readonly StringBuilder _hex = new();
			private List<byte>? _binary;

			internal void AppendHex(char value)
			{
				if (_hex.Length >= InlineImageState.MaxEncodedBytes * 2)
				{
					throw new ArgumentException("The RTF picture payload is too large.");
				}
				_hex.Append(value);
			}

			internal void AppendBinary(byte value)
			{
				_binary ??= new List<byte>();
				if (_binary.Count >= InlineImageState.MaxEncodedBytes)
				{
					throw new ArgumentException("The RTF picture payload is too large.");
				}
				_binary.Add(value);
			}

			internal void AppendBinary(ReadOnlySpan<char> value)
			{
				for (var i = 0; i < value.Length; i++)
				{
					if (value[i] > byte.MaxValue)
					{
						throw new ArgumentException("The RTF binary picture payload is invalid.");
					}
					AppendBinary((byte)value[i]);
				}
			}

			internal bool TryGetBytes(out byte[] data)
			{
				data = Array.Empty<byte>();
				if (_binary is { Count: > 0 })
				{
					if (_hex.Length != 0)
					{
						return false;
					}
					data = _binary.ToArray();
					return true;
				}
				if (_hex.Length == 0 || _hex.Length % 2 != 0)
				{
					return false;
				}

				data = GC.AllocateUninitializedArray<byte>(_hex.Length / 2);
				for (var i = 0; i < data.Length; i++)
				{
					if (!TryDecodeHexByte(_hex[i * 2], _hex[(i * 2) + 1], out data[i]))
					{
						data = Array.Empty<byte>();
						return false;
					}
				}
				return true;
			}
		}

		private sealed class BoundedRtfBuilder
		{
			private readonly StringBuilder _builder = new();
			private readonly int _maxLength;

			public BoundedRtfBuilder(int maxLength)
			{
				if (maxLength <= 0 || maxLength > MaxRtfOutputLength)
				{
					throw new ArgumentOutOfRangeException(nameof(maxLength));
				}

				_maxLength = maxLength;
			}

			public BoundedRtfBuilder Append(char value)
			{
				EnsureAvailable(1);
				_builder.Append(value);
				return this;
			}

			public BoundedRtfBuilder Append(string? value)
			{
				if (value is not null)
				{
					EnsureAvailable(value.Length);
					_builder.Append(value);
				}
				return this;
			}

			public BoundedRtfBuilder Append(int value)
				=> Append(value.ToString(CultureInfo.InvariantCulture));

			private void EnsureAvailable(int additionalLength)
			{
				if (additionalLength > _maxLength - _builder.Length)
				{
					throw new ArgumentException("The RTF output is too large.");
				}
			}

			public override string ToString() => _builder.ToString();
		}
	}
}
