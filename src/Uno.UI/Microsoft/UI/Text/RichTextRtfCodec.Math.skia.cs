#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Windows.UI;

namespace Microsoft.UI.Text
{
	internal static partial class RichTextRtfCodec
	{
		private const int MaxMathRtfWork = MaxRtfInputLength * 4;
		private const string MathFallbackDestination = "unomathml";
		private static readonly XNamespace MathNamespace = MathDocument.NamespaceName;

		internal static string WriteMath(MathDocument document, int maxOutputLength = MaxRtfOutputLength)
		{
			ArgumentNullException.ThrowIfNull(document);

			var colors = CollectMathColors(document.Root);
			var colorIndices = new Dictionary<Color, int>();
			for (var index = 0; index < colors.Count; index++)
			{
				colorIndices[colors[index]] = index + 1;
			}

			var builder = new BoundedRtfBuilder(maxOutputLength);
			builder.Append(@"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033");
			builder.Append(@"{\fonttbl{\f0\fnil\fcharset0 Cambria Math;}{\f1\fnil Segoe UI Variable;}}");
			builder.Append(@"{\colortbl ;");
			foreach (var color in colors)
			{
				builder.Append(@"\red").Append(color.R)
					.Append(@"\green").Append(color.G)
					.Append(@"\blue").Append(color.B)
					.Append(';');
			}
			builder.Append('}');
			builder.Append(@"{\*\generator Riched20 3.2.0000}{\*\mmathPr\mmathFont0\mdefJc3\mwrapIndent1440 }");
			if (RequiresMathFallback(document.Root))
			{
				var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(document.CanonicalMathML));
				builder.Append(@"{\*\").Append(MathFallbackDestination).Append(' ').Append(encoded).Append('}');
			}
			builder.Append(@"\viewkind4\uc1 \pard\tx720{\mmath{\*\moMathPara{\*\moMath\f0\fs21 ");
			AppendMathSequence(builder, document.Root.Children, colorIndices);
			builder.Append(@"}}}\par}");
			return builder.ToString();
		}

		internal static bool TryReadMath(
			string rtf,
			CharacterFormatState defaultCharacterFormat,
			ParagraphFormatState defaultParagraphFormat,
			int maxCharacters,
			bool truncateAtLimit,
			out MathDocument? document,
			out RichTextFragment fragment)
		{
			document = null;
			fragment = RichTextFragment.Empty();
			if (string.IsNullOrWhiteSpace(rtf) || rtf.Length > MaxRtfInputLength)
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			var (rootStart, rootEnd) = ValidateFraming(rtf);
			var workBudget = new ParseWorkBudget();
			var budget = new MathRtfBudget();
			if (!TryFindMathGroup(rtf, rootStart + 1, rootEnd, "mmath", workBudget, budget, out var mathGroup))
			{
				return false;
			}

			MathDocument parsed;
			if (TryFindMathGroup(
				rtf,
				rootStart + 1,
				rootEnd,
				MathFallbackDestination,
				workBudget,
				budget,
				out var fallbackGroup))
			{
				parsed = ParseMathFallback(rtf, fallbackGroup, workBudget, budget);
			}
			else
			{
				if (!TryFindMathGroup(
					rtf,
					mathGroup.ContentStart,
					mathGroup.End,
					"moMath",
					workBudget,
					budget,
					out var officeMathGroup))
				{
					throw new ArgumentException("The RTF math content is malformed.", nameof(rtf));
				}

				var colors = ParseColors(rtf, new ParseWorkBudget());
				var codePage = TryReadHeaderControl(rtf, "ansicpg", new ParseWorkBudget(), out var parsedCodePage)
					? ValidateCodePage(parsedCodePage)
					: 1252;
				var parser = new MathRtfParser(rtf, colors, codePage, workBudget, budget);
				var nodes = parser.ParseSequence(
					officeMathGroup.ContentStart,
					officeMathGroup.End,
					MathRtfStyle.Default,
					depth: 1);
				var root = new XElement(
					MathNamespace + "math",
					new XAttribute(XNamespace.Xmlns + "mml", MathDocument.NamespaceName),
					new XAttribute("display", "block"),
					nodes);
				parsed = MathDocument.Parse(root.ToString(SaveOptions.DisableFormatting));
			}

			var parsedFragment = parsed.CreateFragment(defaultCharacterFormat, defaultParagraphFormat);
			maxCharacters = Math.Clamp(maxCharacters, 0, HardMaxParsedCharacters);
			if (parsedFragment.Text.Length > maxCharacters)
			{
				if (!truncateAtLimit)
				{
					throw new ArgumentException("The RTF text exceeds the import limit.", nameof(rtf));
				}

				var truncatedLength = TextUnitNavigation.TruncateToUtf16Limit(parsedFragment.Text, maxCharacters).Length;
				fragment = parsedFragment.Slice(
					0,
					truncatedLength,
					defaultParagraphFormat.Clone(),
					hasExplicitTerminalParagraphState: false);
				return true;
			}

			document = parsed;
			fragment = parsedFragment;
			return true;
		}

		internal static bool TryReadMath(
			byte[] rtf,
			CharacterFormatState defaultCharacterFormat,
			ParagraphFormatState defaultParagraphFormat,
			int maxCharacters,
			bool truncateAtLimit,
			out MathDocument? document,
			out RichTextFragment fragment)
		{
			ArgumentNullException.ThrowIfNull(rtf);
			if (rtf.Length == 0 || rtf.Length > MaxRtfInputLength)
			{
				throw new ArgumentException("The stream does not contain RTF.", nameof(rtf));
			}

			return TryReadMath(
				Encoding.Latin1.GetString(rtf),
				defaultCharacterFormat,
				defaultParagraphFormat,
				maxCharacters,
				truncateAtLimit,
				out document,
				out fragment);
		}

		private static MathDocument ParseMathFallback(
			string rtf,
			RtfGroup group,
			ParseWorkBudget workBudget,
			MathRtfBudget budget)
		{
			var encoded = DecodeMathText(
				rtf,
				group.ContentStart,
				group.End,
				1252,
				workBudget,
				budget,
				MathRtfStyle.Default,
				out _).Trim();
			if (encoded.Length == 0 || encoded.Length > ((MathDocument.MaxInputLength + 2) / 3 * 4) + 4)
			{
				throw new ArgumentException("The RTF math fallback is invalid.", nameof(rtf));
			}

			try
			{
				var bytes = Convert.FromBase64String(encoded);
				if (bytes.Length > MathDocument.MaxInputLength)
				{
					throw new ArgumentException("The RTF math fallback is too large.", nameof(rtf));
				}
				var mathML = new UTF8Encoding(false, true).GetString(bytes);
				return MathDocument.Parse(mathML);
			}
			catch (FormatException error)
			{
				throw new ArgumentException("The RTF math fallback is invalid.", nameof(rtf), error);
			}
			catch (DecoderFallbackException error)
			{
				throw new ArgumentException("The RTF math fallback encoding is invalid.", nameof(rtf), error);
			}
		}

		private static void AppendMathSequence(
			BoundedRtfBuilder builder,
			IReadOnlyList<MathNode> nodes,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			foreach (var node in nodes)
			{
				AppendMathNode(builder, node, colorIndices);
			}
		}

		private static void AppendMathNode(
			BoundedRtfBuilder builder,
			MathNode node,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			switch (node)
			{
				case MathRowNode row:
					AppendMathSequence(builder, row.Children, colorIndices);
					break;
				case MathTokenNode token:
					builder.Append(@"{\mr");
					AppendMathStyle(builder, token.Style, colorIndices);
					builder.Append(' ');
					AppendMathText(builder, token.ProjectionText);
					builder.Append('}');
					break;
				case MathFractionNode fraction:
					builder.Append(@"{\mf{\mfPr{\mctrlPr\f0\fs21 }}{\mnum");
					AppendMathArgument(builder, fraction.Numerator, colorIndices);
					builder.Append(@"}{\mden");
					AppendMathArgument(builder, fraction.Denominator, colorIndices);
					builder.Append("}}");
					break;
				case MathRadicalNode radical:
					builder.Append(@"{\mrad{\mradPr{\mctrlPr\f0\fs21\tomAlign0 }");
					if (radical.Degree is null)
					{
						builder.Append(@"{\mdegHide on}");
					}
					builder.Append(@"}{\mdeg");
					if (radical.Degree is { } degree)
					{
						AppendMathArgument(builder, degree, colorIndices);
					}
					builder.Append(@"}{\me");
					AppendMathArgument(builder, radical.Radicand, colorIndices);
					builder.Append("}}");
					break;
				case MathScriptNode script:
					AppendMathScript(builder, script, colorIndices);
					break;
				case MathFencedNode fenced:
					builder.Append(@"{\md{\mdPr{\mctrlPr\f0\fs21 }{\mbegChr ");
					AppendMathText(builder, fenced.Open);
					builder.Append(@"}{\mendChr ");
					AppendMathText(builder, fenced.Close);
					builder.Append(@"}}{\me");
					AppendMathArgument(builder, fenced.Content, colorIndices);
					builder.Append("}}");
					break;
				case MathTableNode table:
					AppendMathMatrix(builder, table, colorIndices);
					break;
				case MathOverUnderNode overUnder:
					AppendMathOverUnder(builder, overUnder, colorIndices);
					break;
				case MathMultiScriptsNode multiScripts:
					AppendMathPreScripts(builder, multiScripts, 0, colorIndices);
					break;
			}
		}

		private static void AppendMathArgument(
			BoundedRtfBuilder builder,
			MathNode node,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			builder.Append(' ');
			AppendMathNode(builder, node, colorIndices);
		}

		private static void AppendMathScript(
			BoundedRtfBuilder builder,
			MathScriptNode script,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			var control = script.Subscript is not null && script.Superscript is not null
				? "msSubSup"
				: script.Subscript is not null ? "msSub" : "msSup";
			builder.Append(@"{\").Append(control)
				.Append(@"{\").Append(control).Append(@"Pr{\mctrlPr\f0\fs21 }}{\me");
			AppendMathArgument(builder, script.Base, colorIndices);
			builder.Append('}');
			if (script.Subscript is { } subscript)
			{
				builder.Append(@"{\msub");
				AppendMathArgument(builder, subscript, colorIndices);
				builder.Append('}');
			}
			if (script.Superscript is { } superscript)
			{
				builder.Append(@"{\msup");
				AppendMathArgument(builder, superscript, colorIndices);
				builder.Append('}');
			}
			builder.Append('}');
		}

		private static void AppendMathMatrix(
			BoundedRtfBuilder builder,
			MathTableNode table,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			var columns = table.Rows.Count == 0 ? 0 : table.Rows.Max(row => row.Cells.Count);
			builder.Append(@"{\mm{\mmPr{\mctrlPr\f0\fs21\tomAlign0 }{\mplcHide on}{\mmcs{\mmc{\mmcPr{\mmcJc center}{\mcount ")
				.Append(columns)
				.Append("}}}}}");
			foreach (var row in table.Rows)
			{
				builder.Append(@"{\mmr");
				foreach (var cell in row.Cells)
				{
					builder.Append(@"{\me");
					AppendMathArgument(builder, cell, colorIndices);
					builder.Append('}');
				}
				builder.Append('}');
			}
			builder.Append('}');
		}

		private static void AppendMathOverUnder(
			BoundedRtfBuilder builder,
			MathOverUnderNode node,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			if (node.Kind == MathOverUnderKind.Mover)
			{
				builder.Append(@"{\macc{\maccPr{\mctrlPr\f0\fs21 }{\mchr ");
				AppendMathText(builder, GetAccentCharacter(node.Over));
				builder.Append(@"}}{\me");
				AppendMathArgument(builder, node.Base, colorIndices);
				builder.Append("}}");
				return;
			}

			if (node.Kind == MathOverUnderKind.Munder)
			{
				builder.Append(@"{\mbar{\mbarPr{\mctrlPr\f0\fs21 }}{\me");
				AppendMathArgument(builder, node.Base, colorIndices);
				builder.Append("}}");
				return;
			}

			if (node.Kind == MathOverUnderKind.Nary)
			{
				builder.Append(@"{\mnary{\mnaryPr{\mctrlPr\f0\fs21\tomAlign129 }{\mchr ");
				AppendMathText(builder, GetSingleTokenText(node.Base, "∑"));
				builder.Append(@"}{\mlimLoc undOvr}}{\msub");
				AppendOptionalMathArgument(builder, node.Under, colorIndices);
				builder.Append(@"}{\msup");
				AppendOptionalMathArgument(builder, node.Over, colorIndices);
				builder.Append(@"}{\me");
				AppendOptionalMathArgument(builder, node.Operand, colorIndices);
				builder.Append("}}");
				return;
			}

			var approximation = new MathScriptNode(node.Style, node.Base, node.Under, node.Over);
			AppendMathScript(builder, approximation, colorIndices);
		}

		private static void AppendMathPreScripts(
			BoundedRtfBuilder builder,
			MathMultiScriptsNode node,
			int index,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			if (index >= node.Prescripts.Count)
			{
				AppendMathNode(builder, node.Body, colorIndices);
				return;
			}

			var pair = node.Prescripts[index];
			builder.Append(@"{\msPre{\msPrePr{\mctrlPr\f0\fs21 }}{\msub");
			AppendOptionalMathArgument(builder, pair.Subscript, colorIndices);
			builder.Append(@"}{\msup");
			AppendOptionalMathArgument(builder, pair.Superscript, colorIndices);
			builder.Append(@"}{\me ");
			AppendMathPreScripts(builder, node, index + 1, colorIndices);
			builder.Append("}}");
		}

		private static void AppendOptionalMathArgument(
			BoundedRtfBuilder builder,
			MathNode? node,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			if (node is not null)
			{
				AppendMathArgument(builder, node, colorIndices);
			}
		}

		private static string GetAccentCharacter(MathNode? node)
			=> GetSingleTokenText(node, "-") == "-" ? "\u0305" : GetSingleTokenText(node, "\u0305");

		private static string GetSingleTokenText(MathNode? node, string fallback)
			=> node is MathTokenNode token && token.Text.EnumerateRunes().Take(2).Count() == 1
				? token.Text
				: fallback;

		private static void AppendMathStyle(
			BoundedRtfBuilder builder,
			MathStyle style,
			IReadOnlyDictionary<Color, int> colorIndices)
		{
			switch (style.Variant)
			{
				case MathVariant.Normal:
					builder.Append(@"\b0\i0");
					break;
				case MathVariant.Bold:
					builder.Append(@"\b\i0");
					break;
				case MathVariant.Italic:
					builder.Append(@"\b0\i");
					break;
				case MathVariant.BoldItalic:
					builder.Append(@"\b\i");
					break;
			}

			if (style.Foreground is { } foreground && colorIndices.TryGetValue(foreground, out var foregroundIndex))
			{
				builder.Append(@"\cf").Append(foregroundIndex);
			}
			if (style.Background is { } background && colorIndices.TryGetValue(background, out var backgroundIndex))
			{
				builder.Append(@"\highlight").Append(backgroundIndex);
			}
		}

		private static void AppendMathText(BoundedRtfBuilder builder, string value)
		{
			foreach (var character in value)
			{
				AppendTextCharacter(builder, character);
			}
		}

		private static List<Color> CollectMathColors(MathNode root)
		{
			var colors = new List<Color> { Colors.Black };
			var seen = new HashSet<Color> { Colors.Black };
			CollectMathColors(root, colors, seen);
			return colors;
		}

		private static void CollectMathColors(MathNode node, List<Color> colors, HashSet<Color> seen)
		{
			if (node.Style.Foreground is { } foreground && seen.Add(foreground))
			{
				colors.Add(foreground);
			}
			if (node.Style.Background is { } background && seen.Add(background))
			{
				colors.Add(background);
			}

			switch (node)
			{
				case MathRowNode row:
					foreach (var child in row.Children)
					{
						CollectMathColors(child, colors, seen);
					}
					break;
				case MathFractionNode fraction:
					CollectMathColors(fraction.Numerator, colors, seen);
					CollectMathColors(fraction.Denominator, colors, seen);
					break;
				case MathRadicalNode radical:
					CollectMathColors(radical.Radicand, colors, seen);
					if (radical.Degree is { } degree)
					{
						CollectMathColors(degree, colors, seen);
					}
					break;
				case MathScriptNode script:
					CollectMathColors(script.Base, colors, seen);
					if (script.Subscript is { } subscript)
					{
						CollectMathColors(subscript, colors, seen);
					}
					if (script.Superscript is { } superscript)
					{
						CollectMathColors(superscript, colors, seen);
					}
					break;
				case MathFencedNode fenced:
					CollectMathColors(fenced.Content, colors, seen);
					break;
				case MathTableNode table:
					foreach (var row in table.Rows)
					{
						foreach (var cell in row.Cells)
						{
							CollectMathColors(cell, colors, seen);
						}
					}
					break;
				case MathOverUnderNode overUnder:
					CollectMathColors(overUnder.Base, colors, seen);
					if (overUnder.Under is { } under)
					{
						CollectMathColors(under, colors, seen);
					}
					if (overUnder.Over is { } over)
					{
						CollectMathColors(over, colors, seen);
					}
					if (overUnder.Operand is { } operand)
					{
						CollectMathColors(operand, colors, seen);
					}
					break;
				case MathMultiScriptsNode multiScripts:
					CollectMathColors(multiScripts.Body, colors, seen);
					foreach (var pair in multiScripts.Prescripts)
					{
						if (pair.Subscript is { } preSubscript)
						{
							CollectMathColors(preSubscript, colors, seen);
						}
						if (pair.Superscript is { } preSuperscript)
						{
							CollectMathColors(preSuperscript, colors, seen);
						}
					}
					break;
			}
		}

		private static bool RequiresMathFallback(MathNode node)
		{
			if (node.Style.Foreground is { A: < byte.MaxValue }
				|| node.Style.Background is { A: < byte.MaxValue })
			{
				return true;
			}
			if (node is not MathTokenNode && node.Style != MathStyle.Default)
			{
				return true;
			}

			switch (node)
			{
				case MathRowNode row:
					return row.Children.Any(RequiresMathFallback);
				case MathTokenNode token:
					return token.FenceFalse
						|| token.Style.Variant == MathVariant.Italic
						|| InferTokenKind(token.Text) != token.Kind;
				case MathFractionNode fraction:
					return RequiresMathFallback(fraction.Numerator)
						|| RequiresMathFallback(fraction.Denominator);
				case MathRadicalNode radical:
					return RequiresMathFallback(radical.Radicand)
						|| radical.Degree is { } degree && RequiresMathFallback(degree);
				case MathScriptNode script:
					return RequiresMathFallback(script.Base)
						|| script.Subscript is { } subscript && RequiresMathFallback(subscript)
						|| script.Superscript is { } superscript && RequiresMathFallback(superscript);
				case MathFencedNode fenced:
					return fenced.Open.EnumerateRunes().Take(2).Count() != 1
						|| fenced.Close.EnumerateRunes().Take(2).Count() != 1
						|| RequiresMathFallback(fenced.Content);
				case MathTableNode table:
					var columns = table.Rows.Count == 0 ? 0 : table.Rows[0].Cells.Count;
					return table.Rows.Count == 0
						|| columns == 0
						|| table.Rows.Any(row => row.Cells.Count != columns || row.Cells.Any(RequiresMathFallback));
				case MathOverUnderNode overUnder:
					return overUnder.Kind switch
					{
						MathOverUnderKind.Mover => overUnder.Over is not MathTokenNode { Kind: MathTokenKind.Operator } overToken
							|| GetSingleTokenText(overToken, string.Empty).Length == 0
							|| RequiresMathFallback(overToken)
							|| RequiresMathFallback(overUnder.Base),
						MathOverUnderKind.Munder => overUnder.Under is not MathTokenNode { Kind: MathTokenKind.Operator, Text: "_" } underToken
							|| RequiresMathFallback(underToken)
							|| RequiresMathFallback(overUnder.Base),
						MathOverUnderKind.Nary => overUnder.Base is not MathTokenNode { Kind: MathTokenKind.Operator } naryToken
							|| GetSingleTokenText(naryToken, string.Empty).Length == 0
							|| RequiresMathFallback(naryToken)
							|| overUnder.Under is { } under && RequiresMathFallback(under)
							|| overUnder.Over is { } over && RequiresMathFallback(over)
							|| overUnder.Operand is { } operand && RequiresMathFallback(operand),
						_ => true,
					};
				case MathMultiScriptsNode multiScripts:
					return multiScripts.Prescripts.Count > 1
						|| RequiresMathFallback(multiScripts.Body)
						|| multiScripts.Prescripts.Any(pair =>
							pair.Subscript is { } subscript && RequiresMathFallback(subscript)
							|| pair.Superscript is { } superscript && RequiresMathFallback(superscript));
				default:
					return true;
			}
		}

		private static MathTokenKind InferTokenKind(string text)
		{
			if (text.Length == 0)
			{
				return MathTokenKind.Text;
			}

			var allLetters = true;
			var allDigits = true;
			var hasWhitespace = false;
			foreach (var rune in text.EnumerateRunes())
			{
				allLetters &= Rune.IsLetter(rune);
				allDigits &= Rune.IsDigit(rune);
				hasWhitespace |= Rune.IsWhiteSpace(rune);
			}

			return allLetters
				? MathTokenKind.Identifier
				: allDigits
					? MathTokenKind.Number
					: !hasWhitespace ? MathTokenKind.Operator : MathTokenKind.Text;
		}

		private static bool TryFindMathGroup(
			string rtf,
			int start,
			int end,
			string destination,
			ParseWorkBudget workBudget,
			MathRtfBudget budget,
			out RtfGroup group)
		{
			for (var position = start; position < end; position++)
			{
				budget.RecordWork();
				if (rtf[position] == '\\')
				{
					workBudget.RecordControl();
					SkipControl(rtf, ref position);
					continue;
				}
				if (rtf[position] != '{')
				{
					continue;
				}

				budget.RecordGroup();
				if (TryGetGroupDestination(rtf, position, end, workBudget, out var actual, out var contentStart)
					&& string.Equals(actual, destination, StringComparison.Ordinal))
				{
					var groupEnd = FindMathGroupEnd(rtf, position, end, workBudget, budget);
					group = new RtfGroup(position, groupEnd, contentStart, actual);
					return true;
				}
			}

			group = default;
			return false;
		}

		private static int FindMathGroupEnd(
			string rtf,
			int groupStart,
			int limit,
			ParseWorkBudget workBudget,
			MathRtfBudget budget)
		{
			var depth = 0;
			for (var position = groupStart; position < limit; position++)
			{
				budget.RecordWork();
				if (rtf[position] == '\\')
				{
					workBudget.RecordControl();
					SkipControl(rtf, ref position);
				}
				else if (rtf[position] == '{')
				{
					depth++;
				}
				else if (rtf[position] == '}' && --depth == 0)
				{
					return position;
				}
			}

			throw new ArgumentException("The RTF math group is malformed.", nameof(rtf));
		}

		private static string DecodeMathText(
			string rtf,
			int start,
			int end,
			int codePage,
			ParseWorkBudget workBudget,
			MathRtfBudget budget,
			MathRtfStyle initialStyle,
			out MathRtfStyle finalStyle)
		{
			var builder = new StringBuilder();
			var style = initialStyle;
			var unicodeSkipCount = 1;
			var encoding = GetRtfEncoding(codePage);
			var position = start;
			while (position < end)
			{
				budget.RecordWork();
				var value = rtf[position];
				if (value == '{')
				{
					var groupEnd = FindMathGroupEnd(rtf, position, end, workBudget, budget);
					position = groupEnd + 1;
					continue;
				}
				if (value is '\r' or '\n')
				{
					position++;
					continue;
				}
				if (value != '\\')
				{
					builder.Append(value);
					position++;
					continue;
				}

				workBudget.RecordControl();
				if (position + 1 >= end)
				{
					throw new ArgumentException("The RTF math control is incomplete.", nameof(rtf));
				}
				var symbol = rtf[position + 1];
				if (symbol is '\\' or '{' or '}')
				{
					builder.Append(symbol);
					position += 2;
					continue;
				}
				if (symbol == '\'')
				{
					if (position + 3 >= end
						|| !TryDecodeHexByte(rtf[position + 2], rtf[position + 3], out var encoded))
					{
						throw new ArgumentException("The RTF math escaped byte is invalid.", nameof(rtf));
					}
					try
					{
						builder.Append(encoding.GetString(new[] { encoded }));
					}
					catch (DecoderFallbackException error)
					{
						throw new ArgumentException("The RTF math encoded text is invalid.", nameof(rtf), error);
					}
					position += 4;
					continue;
				}
				if (!char.IsLetter(symbol))
				{
					if (symbol == '~')
					{
						builder.Append(' ');
					}
					position += 2;
					continue;
				}

				var controlPosition = position;
				if (!TryReadControlWord(rtf, ref controlPosition, out var word, out var hasParameter, out var parameter))
				{
					throw new ArgumentException("The RTF math control parameter is invalid.", nameof(rtf));
				}
				position = controlPosition;
				if (word.SequenceEqual("u"))
				{
					if (!hasParameter)
					{
						throw new ArgumentException("The RTF math Unicode escape is invalid.", nameof(rtf));
					}
					builder.Append((char)(short)parameter);
					SkipMathUnicodeFallback(rtf, ref position, end, unicodeSkipCount, workBudget, budget);
				}
				else if (word.SequenceEqual("uc") && hasParameter)
				{
					unicodeSkipCount = Math.Clamp(parameter, 0, 16);
				}
				else if (word.SequenceEqual("b"))
				{
					style = style with { Bold = !hasParameter || parameter != 0 };
				}
				else if (word.SequenceEqual("i"))
				{
					style = style with { Italic = !hasParameter || parameter != 0 };
				}
				else if (word.SequenceEqual("cf") && hasParameter)
				{
					style = style with { ForegroundIndex = parameter };
				}
				else if (word.SequenceEqual("highlight") && hasParameter)
				{
					style = style with { BackgroundIndex = parameter };
				}
				else if (word.SequenceEqual("plain"))
				{
					style = MathRtfStyle.Default;
				}
			}

			finalStyle = style;
			return builder.ToString();
		}

		private static void SkipMathUnicodeFallback(
			string rtf,
			ref int position,
			int end,
			int count,
			ParseWorkBudget workBudget,
			MathRtfBudget budget)
		{
			while (count > 0 && position < end)
			{
				budget.RecordWork();
				if (rtf[position] == '\\')
				{
					workBudget.RecordControl();
					if (position + 1 < end && rtf[position + 1] == '\''
						&& position + 3 < end
						&& TryDecodeHexByte(rtf[position + 2], rtf[position + 3], out _))
					{
						position += 4;
					}
					else if (position + 1 < end && rtf[position + 1] is '\\' or '{' or '}')
					{
						position += 2;
					}
					else
					{
						var controlPosition = position;
						if (!TryReadControlWord(rtf, ref controlPosition, out _, out _, out _))
						{
							position += Math.Min(2, end - position);
						}
						else
						{
							position = controlPosition;
						}
					}
				}
				else
				{
					position++;
				}
				count--;
			}
		}

		private readonly record struct MathRtfStyle(
			bool? Bold,
			bool? Italic,
			int? ForegroundIndex,
			int? BackgroundIndex)
		{
			internal static MathRtfStyle Default => new(null, null, null, null);
		}

		private sealed class MathRtfBudget
		{
			private int _groups;
			private int _work;

			internal void RecordGroup()
			{
				if (++_groups > MaxParsedGroups)
				{
					throw new ArgumentException("The RTF math content contains too many groups.");
				}
			}

			internal void RecordWork()
			{
				if (++_work > MaxMathRtfWork)
				{
					throw new ArgumentException("The RTF math content is too complex.");
				}
			}
		}

		private sealed class MathRtfParser
		{
			private readonly string _rtf;
			private readonly IReadOnlyDictionary<int, Color> _colors;
			private readonly int _codePage;
			private readonly ParseWorkBudget _workBudget;
			private readonly MathRtfBudget _budget;
			private int _nodes;

			internal MathRtfParser(
				string rtf,
				IReadOnlyDictionary<int, Color> colors,
				int codePage,
				ParseWorkBudget workBudget,
				MathRtfBudget budget)
			{
				_rtf = rtf;
				_colors = colors;
				_codePage = codePage;
				_workBudget = workBudget;
				_budget = budget;
			}

			internal List<XElement> ParseSequence(int start, int end, MathRtfStyle initialStyle, int depth)
			{
				if (depth > MathDocument.MaxDepth)
				{
					throw new ArgumentException("The RTF math content is too deeply nested.");
				}

				var result = new List<XElement>();
				var text = new StringBuilder();
				var style = initialStyle;
				var unicodeSkipCount = 1;
				var position = start;
				while (position < end)
				{
					_budget.RecordWork();
					if (_rtf[position] == '{')
					{
						FlushText(result, text, style);
						var groupEnd = FindMathGroupEnd(_rtf, position, end, _workBudget, _budget);
						_budget.RecordGroup();
						if (TryGetGroupDestination(
							_rtf,
							position,
							groupEnd,
							_workBudget,
							out var destination,
							out var contentStart))
						{
							var group = new RtfGroup(position, groupEnd, contentStart, destination);
							result.AddRange(ParseGroup(group, style, depth + 1));
						}
						else
						{
							result.AddRange(ParseSequence(position + 1, groupEnd, style, depth + 1));
						}
						position = groupEnd + 1;
						continue;
					}
					if (_rtf[position] == '}')
					{
						throw new ArgumentException("The RTF math group is malformed.", nameof(_rtf));
					}

					var segmentStart = position;
					while (position < end && _rtf[position] is not ('{' or '}'))
					{
						position++;
					}
					AppendInlineSegment(
						segmentStart,
						position,
						ref style,
						ref unicodeSkipCount,
						result,
						text);
				}
				FlushText(result, text, style);
				return result;
			}

			private void AppendInlineSegment(
				int start,
				int end,
				ref MathRtfStyle style,
				ref int unicodeSkipCount,
				List<XElement> result,
				StringBuilder text)
			{
				var encoding = GetRtfEncoding(_codePage);
				var position = start;
				while (position < end)
				{
					_budget.RecordWork();
					var value = _rtf[position];
					if (value is '\r' or '\n')
					{
						position++;
						continue;
					}
					if (value != '\\')
					{
						text.Append(value);
						position++;
						continue;
					}

					_workBudget.RecordControl();
					if (position + 1 >= end)
					{
						throw new ArgumentException("The RTF math control is incomplete.", nameof(_rtf));
					}
					var symbol = _rtf[position + 1];
					if (symbol is '\\' or '{' or '}')
					{
						text.Append(symbol);
						position += 2;
						continue;
					}
					if (symbol == '\'')
					{
						if (position + 3 >= end
							|| !TryDecodeHexByte(_rtf[position + 2], _rtf[position + 3], out var encoded))
						{
							throw new ArgumentException("The RTF math escaped byte is invalid.", nameof(_rtf));
						}
						try
						{
							text.Append(encoding.GetString(new[] { encoded }));
						}
						catch (DecoderFallbackException error)
						{
							throw new ArgumentException("The RTF math encoded text is invalid.", nameof(_rtf), error);
						}
						position += 4;
						continue;
					}
					if (!char.IsLetter(symbol))
					{
						if (symbol == '~')
						{
							text.Append(' ');
						}
						position += 2;
						continue;
					}

					var controlPosition = position;
					if (!TryReadControlWord(_rtf, ref controlPosition, out var word, out var hasParameter, out var parameter))
					{
						throw new ArgumentException("The RTF math control parameter is invalid.", nameof(_rtf));
					}
					position = controlPosition;
					if (word.SequenceEqual("u"))
					{
						if (!hasParameter)
						{
							throw new ArgumentException("The RTF math Unicode escape is invalid.", nameof(_rtf));
						}
						text.Append((char)(short)parameter);
						SkipMathUnicodeFallback(
							_rtf,
							ref position,
							end,
							unicodeSkipCount,
							_workBudget,
							_budget);
					}
					else if (word.SequenceEqual("uc") && hasParameter)
					{
						unicodeSkipCount = Math.Clamp(parameter, 0, 16);
					}
					else if (word.SequenceEqual("b"))
					{
						FlushText(result, text, style);
						style = style with { Bold = !hasParameter || parameter != 0 };
					}
					else if (word.SequenceEqual("i"))
					{
						FlushText(result, text, style);
						style = style with { Italic = !hasParameter || parameter != 0 };
					}
					else if (word.SequenceEqual("cf") && hasParameter)
					{
						FlushText(result, text, style);
						style = style with { ForegroundIndex = parameter };
					}
					else if (word.SequenceEqual("highlight") && hasParameter)
					{
						FlushText(result, text, style);
						style = style with { BackgroundIndex = parameter };
					}
					else if (word.SequenceEqual("plain"))
					{
						FlushText(result, text, style);
						style = MathRtfStyle.Default;
					}
				}
			}

			private IReadOnlyList<XElement> ParseGroup(RtfGroup group, MathRtfStyle style, int depth)
			{
				switch (group.Destination)
				{
					case "mr":
					case "me":
					case "mnum":
					case "mden":
					case "msub":
					case "msup":
					case "mdeg":
					case "moMath":
					case "moMathPara":
					case "mmath":
						return ParseSequence(group.ContentStart, group.End, style, depth);
					case "mf":
						return Single(ParseFraction(group, style, depth));
					case "msSub":
						return Single(ParseScript(group, style, depth, hasSubscript: true, hasSuperscript: false));
					case "msSup":
						return Single(ParseScript(group, style, depth, hasSubscript: false, hasSuperscript: true));
					case "msSubSup":
						return Single(ParseScript(group, style, depth, hasSubscript: true, hasSuperscript: true));
					case "mrad":
						return Single(ParseRadical(group, style, depth));
					case "md":
						return Single(ParseDelimiter(group, style, depth));
					case "mm":
						return Single(ParseMatrix(group, style, depth));
					case "macc":
						return Single(ParseAccent(group, style, depth));
					case "mbar":
						return Single(ParseBar(group, style, depth));
					case "mnary":
						return ParseNary(group, style, depth);
					case "msPre":
						return Single(ParsePreScripts(group, style, depth));
				}

				if (IsMathPropertyDestination(group.Destination) || IsIgnorableGroup(group))
				{
					return Array.Empty<XElement>();
				}

				return ParseSequence(group.ContentStart, group.End, style, depth);
			}

			private XElement ParseFraction(RtfGroup group, MathRtfStyle style, int depth)
				=> Element(
					"mfrac",
					ParseRequiredArgument(group, "mnum", style, depth),
					ParseRequiredArgument(group, "mden", style, depth));

			private XElement ParseScript(
				RtfGroup group,
				MathRtfStyle style,
				int depth,
				bool hasSubscript,
				bool hasSuperscript)
			{
				var children = new List<object>
				{
					ParseRequiredArgument(group, "me", style, depth),
				};
				if (hasSubscript)
				{
					children.Add(ParseRequiredArgument(group, "msub", style, depth));
				}
				if (hasSuperscript)
				{
					children.Add(ParseRequiredArgument(group, "msup", style, depth));
				}

				return Element(hasSubscript && hasSuperscript ? "msubsup" : hasSubscript ? "msub" : "msup", children);
			}

			private XElement ParseRadical(RtfGroup group, MathRtfStyle style, int depth)
			{
				var radicand = ParseRequiredArgument(group, "me", style, depth);
				var degree = ParseOptionalArgument(group, "mdeg", style, depth);
				var hidesDegree = TryFindDescendant(group, "mdegHide", out _);
				return hidesDegree || degree is null || IsEmptyRow(degree)
					? Element("msqrt", radicand)
					: Element("mroot", radicand, degree);
			}

			private XElement ParseDelimiter(RtfGroup group, MathRtfStyle style, int depth)
			{
				var content = ParseRequiredArgument(group, "me", style, depth);
				var open = ReadPropertyText(group, "mbegChr", "(");
				var close = ReadPropertyText(group, "mendChr", ")");
				var element = content.Name.LocalName == "mrow"
					? Element("mfenced", content.Elements().Cast<object>())
					: Element("mfenced", content);
				element.SetAttributeValue("separators", string.Empty);
				if (open != "(")
				{
					element.SetAttributeValue("open", open);
				}
				if (close != ")")
				{
					element.SetAttributeValue("close", close);
				}
				return element;
			}

			private XElement ParseMatrix(RtfGroup group, MathRtfStyle style, int depth)
			{
				var rows = new List<XElement>();
				foreach (var rowGroup in GetImmediateGroups(group))
				{
					if (rowGroup.Destination != "mmr")
					{
						continue;
					}

					var cells = new List<XElement>();
					foreach (var cellGroup in GetImmediateGroups(rowGroup))
					{
						if (cellGroup.Destination == "me")
						{
							cells.Add(Element("mtd", Collapse(ParseSequence(
								cellGroup.ContentStart,
								cellGroup.End,
								style,
								depth + 1))));
						}
					}
					if (cells.Count == 0)
					{
						throw new ArgumentException("The RTF math matrix row has no cells.", nameof(_rtf));
					}
					rows.Add(Element("mtr", cells));
				}
				if (rows.Count == 0)
				{
					throw new ArgumentException("The RTF math matrix has no rows.", nameof(_rtf));
				}

				return Element("mtable", rows);
			}

			private XElement ParseAccent(RtfGroup group, MathRtfStyle style, int depth)
			{
				var content = ParseRequiredArgument(group, "me", style, depth);
				var accent = ReadPropertyText(group, "mchr", "\u0305");
				var operatorText = accent is "\u0304" or "\u0305" ? "¯" : accent;
				var element = Element("mover", content, Element("mo", operatorText));
				element.SetAttributeValue("accent", "true");
				return element;
			}

			private XElement ParseBar(RtfGroup group, MathRtfStyle style, int depth)
			{
				var element = Element(
					"munder",
					ParseRequiredArgument(group, "me", style, depth),
					Element("mo", "_"));
				element.SetAttributeValue("accentunder", "false");
				return element;
			}

			private IReadOnlyList<XElement> ParseNary(RtfGroup group, MathRtfStyle style, int depth)
			{
				var @operator = ReadPropertyText(group, "mchr", "∑");
				var under = ParseOptionalArgument(group, "msub", style, depth) ?? Element("mrow");
				var over = ParseOptionalArgument(group, "msup", style, depth) ?? Element("mrow");
				var operand = ParseOptionalArgument(group, "me", style, depth) ?? Element("mrow");
				var result = new List<XElement>
				{
					Element("munderover", Element("mo", @operator), under, over),
				};
				if (!IsEmptyRow(operand))
				{
					result.Add(operand);
				}
				return result;
			}

			private XElement ParsePreScripts(RtfGroup group, MathRtfStyle style, int depth)
			{
				var body = ParseRequiredArgument(group, "me", style, depth);
				if (body.Name.LocalName is "msub" or "msup" or "msubsup")
				{
					body = Element("mrow", body);
				}
				var subscript = ParseOptionalArgument(group, "msub", style, depth);
				var superscript = ParseOptionalArgument(group, "msup", style, depth);
				return Element(
					"mmultiscripts",
					body,
					Element("mprescripts"),
					subscript ?? Element("none"),
					superscript ?? Element("none"));
			}

			private XElement ParseRequiredArgument(RtfGroup group, string destination, MathRtfStyle style, int depth)
				=> ParseOptionalArgument(group, destination, style, depth)
					?? throw new ArgumentException($"The RTF math {group.Destination} element is missing {destination}.", nameof(_rtf));

			private XElement? ParseOptionalArgument(RtfGroup group, string destination, MathRtfStyle style, int depth)
			{
				foreach (var child in GetImmediateGroups(group))
				{
					if (child.Destination == destination)
					{
						return Collapse(ParseSequence(child.ContentStart, child.End, style, depth + 1));
					}
				}
				return null;
			}

			private string ReadPropertyText(RtfGroup group, string destination, string fallback)
			{
				if (!TryFindDescendant(group, destination, out var property))
				{
					return fallback;
				}

				var text = DecodeMathText(
					_rtf,
					property.ContentStart,
					property.End,
					_codePage,
					_workBudget,
					_budget,
					MathRtfStyle.Default,
					out _).Trim();
				return text.Length == 0 ? fallback : text;
			}

			private bool TryFindDescendant(RtfGroup group, string destination, out RtfGroup result)
				=> TryFindMathGroup(
					_rtf,
					group.ContentStart,
					group.End,
					destination,
					_workBudget,
					_budget,
					out result);

			private List<RtfGroup> GetImmediateGroups(RtfGroup group)
			{
				var groups = new List<RtfGroup>();
				var position = group.ContentStart;
				while (position < group.End)
				{
					_budget.RecordWork();
					if (_rtf[position] == '\\')
					{
						_workBudget.RecordControl();
						SkipControl(_rtf, ref position);
						position++;
						continue;
					}
					if (_rtf[position] != '{')
					{
						position++;
						continue;
					}

					var groupEnd = FindMathGroupEnd(_rtf, position, group.End, _workBudget, _budget);
					_budget.RecordGroup();
					if (TryGetGroupDestination(
						_rtf,
						position,
						groupEnd,
						_workBudget,
						out var destination,
						out var contentStart))
					{
						groups.Add(new RtfGroup(position, groupEnd, contentStart, destination));
					}
					position = groupEnd + 1;
				}
				return groups;
			}

			private void FlushText(List<XElement> result, StringBuilder text, MathRtfStyle style)
			{
				if (text.Length == 0)
				{
					return;
				}

				var value = text.ToString();
				text.Clear();
				if (string.IsNullOrWhiteSpace(value))
				{
					return;
				}

				foreach (var token in TokenizeMathText(value))
				{
					if (string.IsNullOrWhiteSpace(token.Text))
					{
						continue;
					}

					var name = token.Kind switch
					{
						MathTokenKind.Identifier => "mi",
						MathTokenKind.Number => "mn",
						MathTokenKind.Operator => "mo",
						_ => "mtext",
					};
					var element = Element(name, token.Text);
					if (token.Kind == MathTokenKind.Identifier)
					{
						var variant = token.Variant;
						if (variant == MathVariant.Normal)
						{
							variant = (style.Bold, style.Italic) switch
							{
								(true, true) => MathVariant.BoldItalic,
								(true, _) => MathVariant.Bold,
								(_, true) => MathVariant.Unspecified,
								_ => MathVariant.Normal,
							};
						}
						var variantName = variant switch
						{
							MathVariant.Normal => "normal",
							MathVariant.Bold => "bold",
							MathVariant.Italic => "italic",
							MathVariant.BoldItalic => "bold-italic",
							_ => null,
						};
						if (variantName is not null)
						{
							element.SetAttributeValue("mathvariant", variantName);
						}
					}
					if (style.ForegroundIndex is { } foregroundIndex
						&& _colors.TryGetValue(foregroundIndex, out var foreground))
					{
						element.SetAttributeValue("mathcolor", FormatMathColor(foreground));
					}
					if (style.BackgroundIndex is { } backgroundIndex
						&& _colors.TryGetValue(backgroundIndex, out var background))
					{
						element.SetAttributeValue("mathbackground", FormatMathColor(background));
					}

					result.Add(element);
				}
			}

			private static IReadOnlyList<MathRtfToken> TokenizeMathText(string value)
			{
				var tokens = new List<MathRtfToken>();
				var builder = new StringBuilder();
				var currentKind = MathTokenKind.Text;
				var currentVariant = MathVariant.Normal;
				foreach (var rune in value.EnumerateRunes())
				{
					string mappedText;
					MathVariant variant;
					if (TryMapMathAlphanumeric(rune.Value, out var mapped, out var runeVariant))
					{
						mappedText = mapped.ToString();
						variant = runeVariant;
					}
					else
					{
						mappedText = rune.ToString();
						variant = MathVariant.Normal;
					}

					var kind = InferTokenKind(mappedText);
					if (builder.Length > 0 && (kind != currentKind || variant != currentVariant))
					{
						tokens.Add(new MathRtfToken(builder.ToString(), currentKind, currentVariant));
						builder.Clear();
					}
					currentKind = kind;
					currentVariant = variant;
					builder.Append(mappedText);
				}
				if (builder.Length > 0)
				{
					tokens.Add(new MathRtfToken(builder.ToString(), currentKind, currentVariant));
				}
				return tokens;
			}

			private static bool TryMapMathAlphanumeric(int scalar, out char mapped, out MathVariant variant)
			{
				if (scalar is >= 0x1D400 and <= 0x1D419)
				{
					mapped = (char)('A' + scalar - 0x1D400);
					variant = MathVariant.Bold;
					return true;
				}
				if (scalar is >= 0x1D41A and <= 0x1D433)
				{
					mapped = (char)('a' + scalar - 0x1D41A);
					variant = MathVariant.Bold;
					return true;
				}
				if (scalar is >= 0x1D434 and <= 0x1D44D)
				{
					mapped = (char)('A' + scalar - 0x1D434);
					variant = MathVariant.Unspecified;
					return true;
				}
				if (scalar is >= 0x1D44E and <= 0x1D467)
				{
					mapped = (char)('a' + scalar - 0x1D44E);
					variant = MathVariant.Unspecified;
					return true;
				}
				if (scalar is >= 0x1D468 and <= 0x1D481)
				{
					mapped = (char)('A' + scalar - 0x1D468);
					variant = MathVariant.BoldItalic;
					return true;
				}
				if (scalar is >= 0x1D482 and <= 0x1D49B)
				{
					mapped = (char)('a' + scalar - 0x1D482);
					variant = MathVariant.BoldItalic;
					return true;
				}
				if (scalar is >= 0x1D7CE and <= 0x1D7D7)
				{
					mapped = (char)('0' + scalar - 0x1D7CE);
					variant = MathVariant.Bold;
					return true;
				}
				if (scalar == 0x210E)
				{
					mapped = 'h';
					variant = MathVariant.Unspecified;
					return true;
				}

				mapped = default;
				variant = MathVariant.Unspecified;
				return false;
			}

			private static string FormatMathColor(Color color)
				=> color.A == byte.MaxValue
					? FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}")
					: FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}");

			private XElement Collapse(IReadOnlyList<XElement> nodes)
				=> nodes.Count switch
				{
					0 => Element("mrow"),
					1 => nodes[0],
					_ => Element("mrow", nodes),
				};

			private static bool IsEmptyRow(XElement element)
				=> element.Name.LocalName == "mrow" && !element.HasElements && string.IsNullOrEmpty(element.Value);

			private static bool IsMathPropertyDestination(string destination)
				=> destination.EndsWith("Pr", StringComparison.Ordinal)
					|| destination is "mctrlPr" or "mchr" or "mbegChr" or "mendChr"
						or "msepChr" or "mdegHide" or "mlimLoc" or "mplcHide"
						or "mmcs" or "mmc" or "mmcPr" or "mmcJc" or "mcount";

			private bool IsIgnorableGroup(RtfGroup group)
				=> group.Start + 2 < group.End
					&& _rtf[group.Start + 1] == '\\'
					&& _rtf[group.Start + 2] == '*';

			private void RecordNode()
			{
				if (++_nodes > MathDocument.MaxNodeCount)
				{
					throw new ArgumentException("The RTF math content contains too many nodes.", nameof(_rtf));
				}
			}

			private XElement Element(string name, params object[] content)
			{
				RecordNode();
				return new XElement(MathNamespace + name, content);
			}

			private XElement Element(string name, IEnumerable<object> content)
			{
				RecordNode();
				return new XElement(MathNamespace + name, content);
			}

			private static IReadOnlyList<XElement> Single(XElement element) => new[] { element };

			private readonly record struct MathRtfToken(
				string Text,
				MathTokenKind Kind,
				MathVariant Variant);
		}
	}
}
