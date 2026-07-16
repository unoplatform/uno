#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Microsoft.UI.Text
{
	// Standard RTF transport for the managed RichEditBox model. The supported subset covers Unicode
	// text, the persisted character/paragraph properties, and friendly-name hyperlinks. Unsupported
	// destinations are skipped, allowing documents from native RichEdit to degrade to the modeled data.
	internal static partial class RichTextRtfCodec
	{
		private const int MaxRtfLength = 4 * 1024 * 1024;
		internal const int MaxRtfOutputLength = 16 * 1024 * 1024;
		private const int MaxGroupDepth = 256;
		private const int MaxFontNameLength = 256;
		private const int MaxParsedCharacters = 262_144;
		private const int MaxParsedImages = 128;
		private const int MaxParsedImageBytes = 16 * 1024 * 1024;
		private const long MaxParsedImagePixels = 32L * 1024 * 1024;
		private const int MaxCharacterMetadataLength = 4 * 1024;
		private const int MaxParagraphMetadataLength = 16 * 1024;
		private const int MaxInlineImageMetadataLength = 72 * 1024;
		private const int MaxFieldInstructionLength = 16 * 1024;
		private const int MaxEncodedLanguageTagLength = 2 * 1024;
		private const int MaxEncodedParagraphTabsLength = 8 * 1024;

		[GeneratedRegex(@"\{\\f(?<index>\d+)\s+(?<name>[^;{}]*);\}", RegexOptions.CultureInvariant)]
		private static partial Regex FontRegex();

		[GeneratedRegex(@"\\red(?<red>\d+)\\green(?<green>\d+)\\blue(?<blue>\d+);", RegexOptions.CultureInvariant)]
		private static partial Regex ColorRegex();

		internal static string Write(RichTextFragment fragment, int maxOutputLength = MaxRtfOutputLength)
		{
			var fonts = CollectFonts(fragment.CharacterStates);
			var colors = CollectColors(fragment.CharacterStates);
			var builder = new BoundedRtfBuilder(maxOutputLength);
			builder.Append(@"{\rtf1\ansi\deff0");
			AppendFontTable(builder, fonts);
			AppendColorTable(builder, colors);
			builder.Append(@"\viewkind4\uc1 ");

			string? openLink = null;
			CharacterFormatState? previousCharacter = null;
			ParagraphFormatState? previousParagraph = null;
			for (var i = 0; i < fragment.Text.Length; i++)
			{
				var character = i < fragment.CharacterStates.Count ? fragment.CharacterStates[i] : new CharacterFormatState();
				var paragraph = i < fragment.ParagraphStates.Count ? fragment.ParagraphStates[i] : new ParagraphFormatState();
				if (character.InlineImage is { } image)
				{
					if (openLink is not null)
					{
						builder.Append("}}");
						openLink = null;
					}

					if (previousParagraph is null || !previousParagraph.Equals(paragraph))
					{
						AppendParagraphControls(builder, paragraph);
						previousParagraph = paragraph;
					}

					AppendInlineImage(builder, image);
					previousCharacter = null;
					continue;
				}

				if (!string.Equals(openLink, character.Link, StringComparison.Ordinal))
				{
					if (openLink is not null)
					{
						builder.Append("}}");
					}

					openLink = character.Link;
					if (openLink is not null)
					{
						builder.Append(@"{\field{\*\fldinst HYPERLINK ");
						AppendInstruction(builder, openLink);
						builder.Append(@"}{\fldrslt ");
					}
				}

				if (previousParagraph is null || !previousParagraph.Equals(paragraph))
				{
					AppendParagraphControls(builder, paragraph);
					previousParagraph = paragraph;
				}

				if (previousCharacter is null || !previousCharacter.Equals(character))
				{
					AppendCharacterControls(builder, character, fonts, colors);
					previousCharacter = character;
				}

				AppendTextCharacter(builder, fragment.Text[i]);
			}

			if (openLink is not null)
			{
				builder.Append("}}");
			}

			builder.Append('}');
			return builder.ToString();
		}

		internal static RichTextFragment Read(string rtf, int maxCharacters = MaxParsedCharacters)
		{
			if (string.IsNullOrWhiteSpace(rtf)
				|| rtf.Length > MaxRtfLength
				|| !rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.Ordinal))
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			var fonts = ParseFonts(rtf);
			maxCharacters = Math.Clamp(maxCharacters, 0, MaxParsedCharacters);
			var colors = ParseColors(rtf);
			var text = new StringBuilder();
			var characterStates = new List<CharacterFormatState>();
			var paragraphStates = new List<ParagraphFormatState>();
			var stack = new List<ParserFrame> { new(new ParserState()) };
			var unicodeFallback = 0;
			var imageCount = 0;
			var imageBytes = 0;
			long imagePixels = 0;

			for (var i = 0; i < rtf.Length; i++)
			{
				var value = rtf[i];
				if (value == '{')
				{
					if (stack.Count >= MaxGroupDepth)
					{
						throw new ArgumentException("The RTF group nesting is too deep.", nameof(rtf));
					}

					stack.Add(stack[stack.Count - 1].CreateChild());
					continue;
				}

				if (value == '}')
				{
					if (stack.Count == 1)
					{
						throw new ArgumentException("The RTF contains an unmatched closing group.", nameof(rtf));
					}

					CloseFrame(
						stack,
						text,
						characterStates,
						paragraphStates,
						ref imageCount,
						ref imageBytes,
						ref imagePixels,
						maxCharacters);
					continue;
				}

				if (value == '\\')
				{
					ParseControl(rtf, ref i, stack, fonts, colors, text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters);
					continue;
				}

				if (value is '\r' or '\n')
				{
					continue;
				}

				AppendParsedCharacter(value, stack[stack.Count - 1], text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters: maxCharacters);
			}

			if (stack.Count != 1)
			{
				throw new ArgumentException("The RTF contains an unterminated group.", nameof(rtf));
			}

			return new RichTextFragment(text.ToString(), characterStates, paragraphStates);
		}

		private static Dictionary<string, int> CollectFonts(List<CharacterFormatState> states)
		{
			var fonts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Segoe UI"] = 0 };
			foreach (var state in states)
			{
				if (!string.IsNullOrEmpty(state.Name) && !fonts.ContainsKey(state.Name))
				{
					fonts[state.Name] = fonts.Count;
				}
			}

			return fonts;
		}

		private static Dictionary<global::Windows.UI.Color, int> CollectColors(List<CharacterFormatState> states)
		{
			var colors = new Dictionary<global::Windows.UI.Color, int>();
			foreach (var state in states)
			{
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

		private static void AppendFontTable(BoundedRtfBuilder builder, Dictionary<string, int> fonts)
		{
			builder.Append(@"{\fonttbl");
			foreach (var pair in fonts)
			{
				builder.Append(@"{\f").Append(pair.Value).Append(' ');
				AppendEscapedAscii(builder, pair.Key);
				builder.Append(";}");
			}
			builder.Append('}');
		}

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
			builder.Append(state.AllCaps ? @"\caps" : @"\caps0");
			builder.Append(state.Bold ? @"\b" : @"\b0");
			builder.Append(state.Hidden ? @"\v" : @"\v0");
			builder.Append(state.Italic ? @"\i" : @"\i0");
			builder.Append(state.Outline ? @"\outl" : @"\outl0");
			builder.Append(state.ProtectedText ? @"\protect" : @"\protect0");
			builder.Append(state.SmallCaps ? @"\scaps" : @"\scaps0");
			builder.Append(state.Underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined ? @"\ul" : @"\ulnone");
			builder.Append(state.Strikethrough ? @"\strike" : @"\strike0");
			builder.Append(state.Superscript ? @"\super" : state.Subscript ? @"\sub" : @"\nosupersub");
			builder.Append(@"\f").Append(!string.IsNullOrEmpty(state.Name) && fonts.TryGetValue(state.Name, out var font) ? font : 0);
			builder.Append(@"\fs").Append(state.Size > 0 ? Math.Max(1, (int)Math.Round(state.Size * 2, MidpointRounding.AwayFromZero)) : 24);
			builder.Append(@"\cf").Append(state.Foreground is { } color && colors.TryGetValue(color, out var colorIndex) ? colorIndex : 0);
			builder.Append(@"\highlight").Append(state.Background is { } background && colors.TryGetValue(background, out var backgroundIndex) ? backgroundIndex : 0);
			builder.Append(@"\expndtw").Append((int)Math.Round(state.Spacing * 20, MidpointRounding.AwayFromZero));
			builder.Append(@"\kerning").Append((int)Math.Round(state.Kerning * 2, MidpointRounding.AwayFromZero));
			if (state.Position > 0)
			{
				builder.Append(@"\up").Append((int)Math.Round(state.Position * 2, MidpointRounding.AwayFromZero));
			}
			else if (state.Position < 0)
			{
				builder.Append(@"\dn").Append((int)Math.Round(-state.Position * 2, MidpointRounding.AwayFromZero));
			}

			AppendCharacterMetadata(builder, state);
		}

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

		private static void AppendParagraphControls(BoundedRtfBuilder builder, ParagraphFormatState state)
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
			builder.Append(' ');
			AppendParagraphMetadata(builder, state);
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

		private static void AppendInlineImage(BoundedRtfBuilder builder, InlineImageState image)
		{
			builder.Append(@"{\*\unoimage ")
				.Append(image.Width).Append(',')
				.Append(image.Height).Append(',')
				.Append(image.Ascent).Append(',')
				.Append((int)image.VerticalAlignment).Append(',')
				.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(image.AlternateText))).Append('}');

			builder.Append(@"{\pict")
				.Append(IsJpeg(image.Data) ? @"\jpegblip" : @"\pngblip")
				.Append(@"\picw").Append(image.Width)
				.Append(@"\pich").Append(image.Height)
				.Append(@"\picwgoal").Append(image.Width * 15)
				.Append(@"\pichgoal").Append(image.Height * 15).Append(' ');
			const string hex = "0123456789abcdef";
			foreach (var value in image.Data)
			{
				builder.Append(hex[value >> 4]).Append(hex[value & 0x0f]);
			}
			builder.Append('}');
		}

		private static bool IsJpeg(byte[] data)
			=> data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;

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

		private static Dictionary<int, string> ParseFonts(string rtf)
		{
			var fonts = new Dictionary<int, string>();
			foreach (Match match in FontRegex().Matches(rtf))
			{
				if (!int.TryParse(match.Groups["index"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
				{
					throw new ArgumentException("The RTF font index is invalid.", nameof(rtf));
				}

				var name = match.Groups["name"].Value.Trim();
				if (IsSafeRtfFontName(name))
				{
					fonts[index] = name;
				}
			}
			return fonts;
		}

		private static bool IsSafeRtfFontName(string name)
		{
			if (name.Length is 0 or > MaxFontNameLength
				|| Uri.TryCreate(name, UriKind.Absolute, out _)
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

		private static Dictionary<int, global::Windows.UI.Color> ParseColors(string rtf)
		{
			var colors = new Dictionary<int, global::Windows.UI.Color>();
			var index = 1;
			foreach (Match match in ColorRegex().Matches(rtf))
			{
				if (!byte.TryParse(match.Groups["red"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var red)
					|| !byte.TryParse(match.Groups["green"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var green)
					|| !byte.TryParse(match.Groups["blue"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var blue))
				{
					throw new ArgumentException("The RTF color table is invalid.", nameof(rtf));
				}

				colors[index++] = global::Windows.UI.Color.FromArgb(
					255,
					red,
					green,
					blue);
			}
			return colors;
		}

		private static void CloseFrame(
			List<ParserFrame> stack,
			StringBuilder text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates,
			ref int imageCount,
			ref int imageBytes,
			ref long imagePixels,
			int maxCharacters)
		{
			var closed = stack[stack.Count - 1];
			stack.RemoveAt(stack.Count - 1);
			if (closed.Destination == ParserDestination.FieldInstruction)
			{
				var instruction = closed.DestinationText.ToString().Trim();
				var link = ParseHyperlinkInstruction(instruction);
				if (link is not null)
				{
					for (var i = stack.Count - 1; i >= 0; i--)
					{
						if (stack[i].IsField)
						{
							stack[i].FieldUrl = link;
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
			else if (closed.Destination == ParserDestination.Picture)
			{
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
					ValidateParsedImageBudget(picture, ref imageCount, ref imageBytes, ref imagePixels);
					if (!CanAppendParsedCharacter(text, maxCharacters))
					{
						return;
					}
					var state = closed.State.Character.Clone();
					state.InlineImage = picture;
					state.Link = null;
					text.Append('\ufffc');
					characterStates.Add(state);
					paragraphStates.Add(closed.State.Paragraph.Clone());
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
		}

		private static void ValidateParsedImageBudget(InlineImageState image, ref int count, ref int bytes, ref long pixels)
		{
			count++;
			bytes = checked(bytes + image.EncodedLength);
			pixels = checked(pixels + image.GetDecodedPixelCount());
			if (count > MaxParsedImages || bytes > MaxParsedImageBytes || pixels > MaxParsedImagePixels)
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
			catch (Exception error) when (error is FormatException or ArgumentException)
			{
				return false;
			}
		}

		private static bool TryParsePicture(ParserFrame frame, out InlineImageState image)
		{
			image = new InlineImageState();
			var hex = frame.DestinationText;
			if (hex.Length == 0 || hex.Length % 2 != 0 || hex.Length / 2 > InlineImageState.MaxEncodedBytes)
			{
				return false;
			}

			var data = GC.AllocateUninitializedArray<byte>(hex.Length / 2);
			for (var i = 0; i < data.Length; i++)
			{
				if (!byte.TryParse(hex.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out data[i]))
				{
					return false;
				}
			}

			using var encoded = SKData.CreateCopy(data);
			using var codec = SKCodec.Create(encoded);
			if (codec is null)
			{
				return false;
			}

			var width = frame.PictureWidthGoal > 0
				? Math.Max(1, frame.PictureWidthGoal / 15)
				: frame.PictureWidth > 0 ? frame.PictureWidth : codec.Info.Width;
			var height = frame.PictureHeightGoal > 0
				? Math.Max(1, frame.PictureHeightGoal / 15)
				: frame.PictureHeight > 0 ? frame.PictureHeight : codec.Info.Height;
			image.Data = data;
			image.Width = width;
			image.Height = height;
			image.Ascent = height;
			image.VerticalAlignment = global::Microsoft.UI.Text.VerticalCharacterAlignment.Baseline;
			try
			{
				image.Validate();
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
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
				state.Alignment = (global::Microsoft.UI.Text.ParagraphAlignment)alignment;
				state.FirstLineIndent = firstLineIndent;
				state.LeftIndent = leftIndent;
				state.RightIndent = rightIndent;
				state.SpaceBefore = spaceBefore;
				state.SpaceAfter = spaceAfter;
				state.LineSpacingRule = (global::Microsoft.UI.Text.LineSpacingRule)lineSpacingRule;
				state.LineSpacing = lineSpacing;
				state.ListType = (global::Microsoft.UI.Text.MarkerType)listType;
				state.ListStyle = (global::Microsoft.UI.Text.MarkerStyle)listStyle;
				state.ListAlignment = (global::Microsoft.UI.Text.MarkerAlignment)listAlignment;
				state.ListLevelIndex = listLevelIndex;
				state.ListStart = listStart;
				state.ListTab = listTab;
				state.KeepTogether = fields[14] == "1";
				state.KeepWithNext = fields[15] == "1";
				state.NoLineNumber = fields[16] == "1";
				state.PageBreakBefore = fields[17] == "1";
				state.RightToLeft = fields[18] == "1";
				state.WidowControl = fields[19] == "1";
				state.Style = (global::Microsoft.UI.Text.ParagraphStyle)style;
				state.SetTabs(tabs);
				return true;
			}
			catch (FormatException)
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
						|| !float.IsFinite(position))
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

		private static string? ParseHyperlinkInstruction(string instruction)
		{
			const string prefix = "HYPERLINK";
			if (!instruction.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var value = instruction.Substring(prefix.Length).Trim();
			var quote = value.IndexOf('"');
			var lastQuote = value.LastIndexOf('"');
			if (quote < 0 || lastQuote <= quote)
			{
				return null;
			}

			var link = value.Substring(quote + 1, lastQuote - quote - 1);
			return Uri.TryCreate(link, UriKind.Absolute, out var uri)
				&& uri.Scheme is "http" or "https"
					? $"\"{link}\""
					: null;
		}

		private static void ParseControl(
			string rtf,
			ref int index,
			List<ParserFrame> stack,
			Dictionary<int, string> fonts,
			Dictionary<int, global::Windows.UI.Color> colors,
			StringBuilder text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates,
			ref int unicodeFallback,
			int maxCharacters)
		{
			if (++index >= rtf.Length)
			{
				return;
			}

			var symbol = rtf[index];
			if (symbol is '\\' or '{' or '}')
			{
				AppendParsedCharacter(symbol, stack[stack.Count - 1], text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters: maxCharacters);
				return;
			}

			if (symbol == '\'')
			{
				if (index + 2 < rtf.Length && byte.TryParse(rtf.Substring(index + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var encoded))
				{
					index += 2;
					AppendParsedCharacter(DecodeWindows1252(encoded), stack[stack.Count - 1], text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters: maxCharacters);
				}
				return;
			}

			if (!char.IsLetter(symbol))
			{
				if (symbol == '*')
				{
					stack[stack.Count - 1].StarDestination = true;
				}
				else if (symbol == '~')
				{
					AppendParsedCharacter('\u00a0', stack[stack.Count - 1], text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters: maxCharacters);
				}
				return;
			}

			var wordStart = index;
			while (index + 1 < rtf.Length && char.IsLetter(rtf[index + 1]))
			{
				index++;
			}
			var word = rtf.Substring(wordStart, index - wordStart + 1);
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

			HandleControl(word, hasParameter, parameter, stack, fonts, colors, text, characterStates, paragraphStates, ref unicodeFallback, maxCharacters);
		}

		private static void HandleControl(
			string word,
			bool hasParameter,
			int parameter,
			List<ParserFrame> stack,
			Dictionary<int, string> fonts,
			Dictionary<int, global::Windows.UI.Color> colors,
			StringBuilder text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates,
			ref int unicodeFallback,
			int maxCharacters)
		{
			var frame = stack[stack.Count - 1];
			var state = frame.State;
			switch (word)
			{
				case "fonttbl":
				case "colortbl":
				case "stylesheet":
				case "info":
				case "object":
					frame.Destination = ParserDestination.Ignore;
					break;
				case "pict":
					frame.Destination = ParserDestination.Picture;
					break;
				case "pngblip":
				case "jpegblip":
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
				case "unochar":
					frame.Destination = ParserDestination.CharacterFormat;
					break;
				case "unopara":
					frame.Destination = ParserDestination.ParagraphFormat;
					break;
				case "fldrslt":
					frame.Destination = ParserDestination.Normal;
					for (var i = stack.Count - 2; i >= 0; i--)
					{
						if (stack[i].IsField)
						{
							state.Character.Link = stack[i].FieldUrl;
							break;
						}
					}
					break;
				case "plain":
					var link = state.Character.Link;
					state.Character = new CharacterFormatState { Link = link };
					break;
				case "pard": state.Paragraph = new ParagraphFormatState(); break;
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
				case "ul": state.Character.Underline = !hasParameter || parameter != 0 ? global::Microsoft.UI.Text.UnderlineType.Single : global::Microsoft.UI.Text.UnderlineType.None; break;
				case "ulnone": state.Character.Underline = global::Microsoft.UI.Text.UnderlineType.None; break;
				case "strike": state.Character.Strikethrough = !hasParameter || parameter != 0; break;
				case "sub": state.Character.Subscript = true; state.Character.Superscript = false; break;
				case "super": state.Character.Superscript = true; state.Character.Subscript = false; break;
				case "nosupersub": state.Character.Subscript = state.Character.Superscript = false; break;
				case "f" when hasParameter: state.Character.Name = fonts.TryGetValue(parameter, out var font) ? font : null; break;
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
				case "fi" when hasParameter: state.Paragraph.FirstLineIndent = parameter / 20f; break;
				case "li" when hasParameter: state.Paragraph.LeftIndent = parameter / 20f; break;
				case "ri" when hasParameter: state.Paragraph.RightIndent = parameter / 20f; break;
				case "sb" when hasParameter: state.Paragraph.SpaceBefore = parameter / 20f; break;
				case "sa" when hasParameter: state.Paragraph.SpaceAfter = parameter / 20f; break;
				case "uc" when hasParameter: state.UnicodeSkipCount = Math.Max(0, parameter); break;
				case "u" when hasParameter:
					AppendParsedCharacter((char)(short)parameter, frame, text, characterStates, paragraphStates, ref unicodeFallback, skipFallback: false, maxCharacters);
					unicodeFallback = state.UnicodeSkipCount;
					break;
				case "par": AppendParsedCharacter('\r', frame, text, characterStates, paragraphStates, ref unicodeFallback, skipFallback: false, maxCharacters); break;
				case "line": AppendParsedCharacter('\n', frame, text, characterStates, paragraphStates, ref unicodeFallback, skipFallback: false, maxCharacters); break;
				case "tab": AppendParsedCharacter('\t', frame, text, characterStates, paragraphStates, ref unicodeFallback, skipFallback: false, maxCharacters); break;
				default:
					if (frame.StarDestination)
					{
						frame.Destination = ParserDestination.Ignore;
					}
					break;
			}
			frame.StarDestination = false;
		}

		private static void AppendParsedCharacter(
			char value,
			ParserFrame frame,
			StringBuilder text,
			List<CharacterFormatState> characterStates,
			List<ParagraphFormatState> paragraphStates,
			ref int unicodeFallback,
			bool skipFallback = true,
			int maxCharacters = MaxParsedCharacters)
		{
			if (skipFallback && unicodeFallback > 0)
			{
				unicodeFallback--;
				return;
			}

			if (frame.Destination == ParserDestination.Ignore)
			{
				return;
			}

			if (frame.Destination == ParserDestination.Picture)
			{
				if (!char.IsWhiteSpace(value))
				{
					frame.DestinationText.Append(value);
				}
				return;
			}

			if (!CanAppendParsedCharacter(text, maxCharacters))
			{
				return;
			}

			if (frame.Destination is ParserDestination.FieldInstruction or ParserDestination.InlineImage or ParserDestination.CharacterFormat or ParserDestination.ParagraphFormat)
			{
				var limit = frame.Destination switch
				{
					ParserDestination.FieldInstruction => MaxFieldInstructionLength,
					ParserDestination.InlineImage => MaxInlineImageMetadataLength,
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

			text.Append(value);
			characterStates.Add(frame.State.Character.Clone());
			paragraphStates.Add(frame.State.Paragraph.Clone());
		}

		private static bool CanAppendParsedCharacter(StringBuilder text, int maxCharacters)
		{
			if (text.Length < maxCharacters)
			{
				return true;
			}

			if (maxCharacters >= MaxParsedCharacters)
			{
				throw new ArgumentException("The RTF text content is too large.");
			}

			return false;
		}

		private static char DecodeWindows1252(byte value)
		{
			const string replacements = "\u20ac\u0081\u201a\u0192\u201e\u2026\u2020\u2021\u02c6\u2030\u0160\u2039\u0152\u008d\u017d\u008f\u0090\u2018\u2019\u201c\u201d\u2022\u2013\u2014\u02dc\u2122\u0161\u203a\u0153\u009d\u017e\u0178";
			return value is >= 0x80 and <= 0x9f ? replacements[value - 0x80] : (char)value;
		}

		private enum ParserDestination
		{
			Normal,
			Ignore,
			FieldInstruction,
			InlineImage,
			Picture,
			CharacterFormat,
			ParagraphFormat,
		}

		private sealed class ParserState
		{
			public CharacterFormatState Character = new();
			public ParagraphFormatState Paragraph = new();
			public int UnicodeSkipCount = 1;

			public ParserState Clone()
				=> new()
				{
					Character = Character.Clone(),
					Paragraph = Paragraph.Clone(),
					UnicodeSkipCount = UnicodeSkipCount,
				};
		}

		private sealed class ParserFrame
		{
			public ParserState State;
			public ParserDestination Destination;
			public bool StarDestination;
			public bool IsField;
			public string? FieldUrl;
			public InlineImageState? PendingInlineImage;
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
				=> new(State.Clone())
				{
					Destination = Destination,
				};
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
