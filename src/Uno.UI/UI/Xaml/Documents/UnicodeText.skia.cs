#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using Windows.UI.Text;
using HarfBuzzSharp;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Buffers;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI;
using Uno.UI.Dispatching;
using Buffer = HarfBuzzSharp.Buffer;
using FontWeights = Microsoft.UI.Text.FontWeights;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText : IParsedText
{
	// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
	private const float FallbackTabStopWidth = 48;
	private const byte UBIDI_DEFAULT_LTR = 0xfe;
	private const byte UBIDI_DEFAULT_RTL = 0xff;
	private const int UBIDI_LTR = 0;
	private const int UBIDI_RTL = 1;
	private const string HorizontalEllipsis = "\u2026";

	internal interface IFontCacheUpdateListener
	{
		void Invalidate();
	}

	private record struct Line(int start, int end, LinkedListNode<Cluster> clusterStart, LinkedListNode<Cluster> clusterLast, float width, float widthWithoutTrailingSpaces, float lineHeight, float baselineOffset, TextAlignment? textAlignment = null, bool hasEllipsis = false, ParagraphLayoutInfo? paragraphLayout = null, bool isFirstLineOfParagraph = false, bool isLastLineOfParagraph = false);
	private readonly record struct TextDecorationDrawInfo(float X1, float X2, float Y, float Thickness, float FontSize, SKColor Color, global::Microsoft.UI.Text.UnderlineType Style);

	private record struct Glyph(GlyphPosition GlyphPosition, uint Codepoint);

	private record struct Cluster(
		int start,
		int end,
		LinkedListNode<Glyph> glyphStart,
		LinkedListNode<Glyph> glyphLast,
		FontDetails fontDetails,
		float width,
		float characterSpacing,
		bool hidden,
		bool containsOnlyWhitespace,
		bool containsTab,
		bool rtl,
		int lineIndex,
		int indexInLine,
		InlineObjectInfo? inlineObject = null)
	{
		public static Cluster Create(string _text, int indexStart, int indexEnd, LinkedListNode<Glyph> glyphsStart, LinkedListNode<Glyph> glyphsLast, FontDetails fontDetails, float characterSpacing, bool hidden)
		{
			var clusterContainsTab = false;
			var clusterContainsOnlyWhitespace = true;
			for (int i = indexStart; i < indexEnd; i++)
			{
				clusterContainsTab |= _text[i] == '\t';
				clusterContainsOnlyWhitespace &= char.IsWhiteSpace(_text[i]);
			}

			float clusterWidth = 0;
			for (var glyphNode = glyphsStart; glyphNode != glyphsLast.Next; glyphNode = glyphNode.Next)
			{
				clusterWidth += AdvanceToPixels(glyphNode!.Value.GlyphPosition.XAdvance, fontDetails) + (clusterContainsTab ? 0 : characterSpacing);
			}

			return new(indexStart, indexEnd, glyphsStart, glyphsLast, fontDetails, hidden ? 0 : clusterWidth, characterSpacing, hidden, clusterContainsOnlyWhitespace, clusterContainsTab, false, -1, -1);
		}
	}

	private static readonly Lazy<ISpellCheckingService?> _spellCheckingService = new(() =>
	{
		if (ApiExtensibility.CreateInstance<ISpellCheckingService>(typeof(UnicodeText), out var service))
		{
			return service;
		}
		else
		{
			typeof(UnicodeText).LogWarn()?.Warn($"No implementation of {nameof(ISpellCheckingService)} was found. Spell checking will be disabled. To enable spell checking, add the 'SpellChecking' UnoFeature.");
			return null;
		}
	});

	private static readonly LRUCache<int, SKTypeface?> _skFontManagerDefaultMatchCharacterCache = new(1000); // most languages need much less than 1000 unique Unicode codepoints
	private static readonly Brush _blackBrush = new SolidColorBrush(Colors.Black);
	private static readonly SKPaint _spareDrawPaint = new() { IsStroke = false, IsAntialias = true };
	private static readonly SKPaint _spareTextDecorationPaint = new() { Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Butt, IsAntialias = true };
	private static readonly SKPaint _spareSpellCheckPaint = new() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, IsAntialias = true };
	private static readonly SKPaint _spareCompositionUnderlinePaint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
	private static readonly SKPaint _spareInlineObjectPaint = new() { IsAntialias = true };
	private static readonly Dictionary<int, HashSet<IFontCacheUpdateListener>> _codepointToListeners = new();
	private static readonly Dictionary<string, HashSet<IFontCacheUpdateListener>> _fontFamilyToListeners = new();
	private readonly string _text;
	private readonly List<(float prefixSummedHeight, List<(float sumUntilAfterCluster, Cluster cluster)> prefixSummedWidths, Line line)> _xyTable;
	private readonly List<(int start, int end, LinkedListNode<Cluster> cluster)> _indexToCluster;
	private readonly TextAlignment _textAlignment;
	private readonly FontDetails _defaultFontDetails;
	private readonly List<Line> _lines;
	private readonly float? _endingNewLineLineHeight;
	private readonly float _endingLineContentTop;
	private readonly float _endingLineBaselineOffset;
	private readonly ParagraphLayoutInfo? _endingParagraphLayout;
	private readonly TextAlignment? _endingParagraphAlignment;
	private readonly Brush? _defaultForeground;
	private readonly bool _rtl;
	private readonly List<(int start, int end, Hyperlink hyperlink)> _hyperlinkRanges;
	private readonly List<int> _wordBoundaries;
	private readonly List<LinkedListNode<Cluster>> _clustersInLogicalOrder;
	private readonly LinkedList<Glyph> _glyphs;
	private readonly List<(int end, FlowDirection direction)> _bidiBreaks;
	private readonly List<(int end, Brush? foreground, global::Windows.UI.Color? background, float characterSpacing, bool hidden, FlowDirection direction, TextDecorations decorations, global::Microsoft.UI.Text.UnderlineType? underlineType)> _runBreaks;
	private static readonly SKPaint _spareCharacterBackgroundPaint = new() { IsAntialias = true };
	private readonly List<(int correctionStart, int correctionEnd)?>? _corrections;
	private readonly Size _availableSize;

	internal unsafe UnicodeText(
		Size availableSize,
		Inline[] inlines, // only leaf nodes
		FontDetails defaultFontDetails, // only used for a final empty line, otherwise the FontDetails are read from the inline
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		FlowDirection flowDirection,
		TextAlignment? textAlignment, // null to determine from text.
		TextWrapping textWrapping,
		TextTrimming textTrimming,
		bool isSpellCheckEnabled,
		IFontCacheUpdateListener fontListener,
		bool includeTrailingWhitespaceInMeasurement,
		float defaultTabStop,
		ParagraphLayoutInfo? endingParagraphLayout,
		TextAlignment? endingParagraphAlignment,
		Brush? defaultForeground,
		out Size calculatedSize)
	{
		CI.Assert(maxLines >= 0);
		_endingParagraphLayout = endingParagraphLayout;
		_endingParagraphAlignment = endingParagraphAlignment;
		_defaultForeground = defaultForeground;
		var tabStopWidth = defaultTabStop > 0 ? defaultTabStop : FallbackTabStopWidth;

		var stringBuilder = new StringBuilder();
		_hyperlinkRanges = new List<(int start, int end, Hyperlink hyperlink)>();
		_runBreaks = new List<(int end, Brush? foreground, global::Windows.UI.Color? background, float characterSpacing, bool hidden, FlowDirection direction, TextDecorations decorations, global::Microsoft.UI.Text.UnderlineType? underlineType)>();
		var scriptBreaks = new List<int>();
		var fontBreaks = new List<(int end, FontDetails fontDetails)>();
		var lineOpportunityBreaks = new List<int>();
		Dictionary<int, InlineObjectInfo>? inlineObjects = null;
		List<(int end, TextAlignment alignment)>? paragraphAlignments = null;
		List<(int end, ParagraphLayoutInfo layout)>? paragraphLayouts = null;

		foreach (var inline in inlines)
		{
			var inlineText = inline.GetText();
			if (string.IsNullOrEmpty(inlineText))
			{
				continue;
			}

			var inlineStart = stringBuilder.Length;
			stringBuilder.Append(inlineText);
			if (inline is Run { InlineObject: { } inlineObject })
			{
				(inlineObjects ??= new())[inlineStart] = inlineObject;
			}
			if (inline is Run { ParagraphAlignment: { } paragraphAlignment })
			{
				(paragraphAlignments ??= new()).Add((inlineStart + inlineText.Length, paragraphAlignment));
			}
			if (inline is Run { ParagraphLayout: { } paragraphLayout })
			{
				(paragraphLayouts ??= new()).Add((inlineStart + inlineText.Length, paragraphLayout));
			}

			var currentFontDetails = inline.FontInfo;
			int currentScript = 0;
			void ProcessNormalRange(int rangeStart, int rangeEnd)
			{
				for (var i = rangeStart; i < rangeEnd; i += char.IsSurrogate(inlineText, i) ? 2 : 1)
				{
					FontDetails newFontDetails;
					var codepoint = char.ConvertToUtf32(inlineText, i);
					var newScript = ICU.GetMethod<ICU.uscript_getScript>()(codepoint, out var errorCode);
					ICU.CheckErrorCode<ICU.uscript_getScript>(errorCode);

					if (newScript != currentScript)
					{
						currentScript = newScript;
						if (i != 0)
						{
							scriptBreaks.Add(inlineStart + i);
						}
					}

					if (!inline.FontInfo.SKFont.ContainsGlyph(codepoint))
					{
						newFontDetails = GetFallbackFont(codepoint, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle, fontListener) ?? inline.FontInfo;
					}
					else
					{
						newFontDetails = inline.FontInfo;
					}

					if (newFontDetails != currentFontDetails)
					{
						if (i != 0)
						{
							fontBreaks.Add((inlineStart + i, currentFontDetails));
						}
						currentFontDetails = newFontDetails;
					}
				}
			}

			if (OperatingSystem.IsBrowser() && ContainsEmojiCandidate(inlineText))
			{
				var graphemeStarts = StringInfo.ParseCombiningCharacters(inlineText);
				for (var graphemeIndex = 0; graphemeIndex < graphemeStarts.Length; graphemeIndex++)
				{
					var graphemeStart = graphemeStarts[graphemeIndex];
					var graphemeEnd = graphemeIndex + 1 < graphemeStarts.Length
						? graphemeStarts[graphemeIndex + 1]
						: inlineText.Length;
					int? emojiCodepoint = null;
					var requestsEmojiPresentation = false;
					var requestsTextPresentation = false;
					var requiresEmojiFallback = false;
					for (var i = graphemeStart; i < graphemeEnd; i += char.IsSurrogate(inlineText, i) ? 2 : 1)
					{
						var codepoint = char.ConvertToUtf32(inlineText, i);
						requestsEmojiPresentation |= codepoint == 0xFE0F;
						requestsTextPresentation |= codepoint == 0xFE0E;
						if (NotoFontFallbackService.IsEmojiCodepoint(codepoint))
						{
							var needsFallback = codepoint >= 0x1F000 || !inline.FontInfo.SKFont.ContainsGlyph(codepoint);
							if (emojiCodepoint is null || needsFallback && !requiresEmojiFallback)
							{
								emojiCodepoint = codepoint;
							}
							requiresEmojiFallback |= needsFallback;
						}
					}

					var useEmojiFont = emojiCodepoint is not null
						&& !requestsTextPresentation
						&& (requestsEmojiPresentation || requiresEmojiFallback);
					if (useEmojiFont)
					{
						var emojiFont = GetFallbackFont(
							emojiCodepoint!.Value,
							(float)inline.FontSize,
							inline.FontWeight,
							inline.FontStretch,
							inline.FontStyle,
							fontListener,
							preferFallbackService: true) ?? inline.FontInfo;
						if (emojiFont != currentFontDetails)
						{
							if (graphemeStart != 0)
							{
								fontBreaks.Add((inlineStart + graphemeStart, currentFontDetails));
							}
							currentFontDetails = emojiFont;
						}
					}
					else
					{
						ProcessNormalRange(graphemeStart, graphemeEnd);
					}
				}
			}
			else
			{
				ProcessNormalRange(0, inlineText.Length);
			}

			scriptBreaks.Add(inlineStart + inlineText.Length);
			var characterSpacing = (float)inline.FontSize * inline.CharacterSpacing / 1000;
			var run = inline as Run;
			_runBreaks.Add((inlineStart + inlineText.Length, inline.Foreground, run?.CharacterBackground, characterSpacing, run?.IsHidden == true, run?.FlowDirection ?? flowDirection, inline.TextDecorations, run?.RichEditUnderlineType));
			fontBreaks.Add((inlineStart + inlineText.Length, currentFontDetails));

			if (TryGetHyperLink(inline) is { } hyperLink)
			{
				_hyperlinkRanges.Add((inlineStart, inlineStart + inlineText.Length, hyperLink));
			}
		}

		_text = stringBuilder.ToString();
		if (_text.Length == 0)
		{
			_lines = [];
			_defaultFontDetails = defaultFontDetails;
			_rtl = flowDirection is FlowDirection.RightToLeft;
			_textAlignment = textAlignment ?? (flowDirection is FlowDirection.RightToLeft ? TextAlignment.Right : TextAlignment.Left);
			_wordBoundaries = [];
			var (naturalHeight, naturalBaseline) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true);
			_endingNewLineLineHeight = endingParagraphLayout is null
				? naturalHeight
				: ApplyLineSpacingRule(naturalHeight, endingParagraphLayout);
			_endingLineContentTop = endingParagraphLayout?.SpaceBefore ?? 0;
			_endingLineBaselineOffset = naturalBaseline + (_endingNewLineLineHeight.Value - naturalHeight) / 2;
			calculatedSize = new Size(
				GetParagraphLeftInset(endingParagraphLayout, firstLine: true) + GetParagraphRightInset(endingParagraphLayout, firstLine: true),
				_endingLineContentTop + _endingNewLineLineHeight.Value + (endingParagraphLayout?.SpaceAfter ?? 0));
			_availableSize = availableSize;
			_xyTable = [];
			_indexToCluster = [];
			_clustersInLogicalOrder = [];
			_glyphs = [];
			_bidiBreaks = [];
			return;
		}

		// Line breaking is a document-level operation. Computing boundaries per inline can split a
		// CRLF pair when character formatting changes between its two code units.
		AppendBoundaries(/* Line */ 2, _text, 0, lineOpportunityBreaks);

		_bidiBreaks = new List<(int end, FlowDirection direction)>();
		var embeddingLevels = ArrayPool<byte>.Shared.Rent(_text.Length);
		using var embeddingLevelsDisposable = new DisposableStruct<byte[]>(static embeddingLevels => ArrayPool<byte>.Shared.Return(embeddingLevels), embeddingLevels);
		for (int i = 0; i < _runBreaks.Count; i++)
		{
			var (start, count) = i == 0 ? (0, _runBreaks[0].end) : (_runBreaks[i - 1].end, _runBreaks[i].end - _runBreaks[i - 1].end);
			var direction = _runBreaks[i].direction;
			var level = flowDirection is FlowDirection.LeftToRight
				? (direction is FlowDirection.LeftToRight ? 0 : 1)
				: (direction is FlowDirection.RightToLeft ? 1 : 2); // 2 and not 0 because embedding must increase nesting level when switching direction inside RTL paragraph
			Array.Fill(embeddingLevels, (byte)level, start, count);
		}

		using var _ = ICU.CreateBiDiAndSetPara(_text, 0, _text.Length, flowDirection is FlowDirection.RightToLeft ? UBIDI_DEFAULT_RTL : UBIDI_DEFAULT_LTR, out var bidi, embeddingLevels);
		var runCount = ICU.GetMethod<ICU.ubidi_countRuns>()(bidi, out var countRunsErrorCode);
		ICU.CheckErrorCode<ICU.ubidi_countRuns>(countRunsErrorCode);
		_rtl = ICU.GetMethod<ICU.ubidi_getParaLevel>()(bidi) is UBIDI_RTL;
		for (var bidiRunIndex = 0; bidiRunIndex < runCount; bidiRunIndex++)
		{
			var level = ICU.GetMethod<ICU.ubidi_getVisualRun>()(bidi, bidiRunIndex, out var logicalStart, out var length);
			CI.Assert(level is UBIDI_LTR or UBIDI_RTL);
			_bidiBreaks.Add((logicalStart + length, level is UBIDI_RTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight));

			if (textAlignment is null && logicalStart == 0)
			{
				textAlignment = level is UBIDI_RTL ? TextAlignment.Right : TextAlignment.Left;
			}
		}

		_glyphs = new LinkedList<Glyph>();
		var clusterBreaks = new LinkedList<Cluster>();
		foreach (var shapingRun in EnumerateShapingRuns(_runBreaks, scriptBreaks, _bidiBreaks, fontBreaks))
		{
			using var buffer = new Buffer();
			buffer.AddUtf16(_text.AsSpan(shapingRun.start, shapingRun.end - shapingRun.start));
			buffer.GuessSegmentProperties();
			buffer.Direction = shapingRun.direction is FlowDirection.RightToLeft ? Direction.RightToLeft : Direction.LeftToRight;
			shapingRun.fontDetails.Font.Shape(buffer);
			var positions = buffer.GetGlyphPositionSpan();
			var infos = buffer.GetGlyphInfoSpan();
			var count = buffer.Length;

			if (count == 0)
			{
				// Even though textRun is nonempty and fontDetails contains a font that can shape all the characters in it,
				// Font.Shape may still decide to yield 0 glyphs.
				_glyphs.AddLast(new Glyph { GlyphPosition = new GlyphPosition(), Codepoint = 0 });
			}
			else
			{
				CI.Assert((buffer.Direction is Direction.LeftToRight && infos[0].Cluster == 0) || (buffer.Direction is Direction.RightToLeft && infos[^1].Cluster == 0));
				if (buffer.Direction is Direction.LeftToRight)
				{
					for (var index = 0; index < infos.Length; index++)
					{
						var info = infos[index];
						var position = positions[index];
						if (index > 0 && info.Cluster != infos[index - 1].Cluster)
						{
							clusterBreaks.AddLast(Cluster.Create(
								_text,
								clusterBreaks.Last?.Value.end ?? 0,
								(int)(shapingRun.start + infos[index].Cluster),
								clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
								_glyphs.Last!,
								shapingRun.fontDetails,
								shapingRun.characterSpacing,
								shapingRun.hidden));
						}
						_glyphs.AddLast(new Glyph { GlyphPosition = position, Codepoint = info.Codepoint });
					}
				}
				else
				{
					for (var index = infos.Length - 1; index >= 0; index--)
					{
						var info = infos[index];
						var position = positions[index];
						if (index < infos.Length - 1 && info.Cluster != infos[index + 1].Cluster)
						{
							clusterBreaks.AddLast(Cluster.Create(
								_text,
								clusterBreaks.Last?.Value.end ?? 0,
								(int)(shapingRun.start + infos[index].Cluster),
								clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
								_glyphs.Last!,
								shapingRun.fontDetails,
								shapingRun.characterSpacing,
								shapingRun.hidden));
						}
						_glyphs.AddLast(new Glyph { GlyphPosition = position, Codepoint = info.Codepoint });
					}
				}

				clusterBreaks.AddLast(Cluster.Create(
					_text,
					clusterBreaks.Last?.Value.end ?? 0,
					shapingRun.end,
					clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
					_glyphs.Last!,
					shapingRun.fontDetails,
					shapingRun.characterSpacing,
					shapingRun.hidden));
			}
		}

		if (inlineObjects is not null)
		{
			for (var node = clusterBreaks.First; node is not null; node = node.Next)
			{
				if (node.Value.end == node.Value.start + 1 && inlineObjects.TryGetValue(node.Value.start, out var inlineObject))
				{
					node.Value = node.Value with
					{
						width = inlineObject.Width,
						containsOnlyWhitespace = false,
						inlineObject = inlineObject,
					};
				}
			}
		}

		var lines = new List<Line>();
		{ // line breaking
			float lineWidth = 0;
			float lineWidthWithoutTrailingSpaces = 0;
			int currentLineEnd = -1;
			LinkedListNode<Cluster>? currentLineClusterLast = null;
			FontDetails? maxHeightFontDetailsInCurrentLine = null;
			// a "chunk" is a contiguous sequence of clusters with no line breaking opportunities that is being tested as a potential addition to the current line.
			float chunkUnderTestWidth = 0;
			float chunkUnderTestTrailingSpaceWidth = 0;
			bool chunkUnderTestContainsOnlyWhitespace = true;
			FontDetails? maxHeightFontDetailsInChunkUnderTest = null;
			// Paragraph layout indent tracking: effective width = availableSize.Width - indents
			int paragraphLayoutIndex = 0;
			bool isFirstLineOfCurrentParagraph = true;
			float effectiveAvailableWidth = (float)availableSize.Width;
			var text = _text;
			float GetEffectiveAvailableWidth(ParagraphLayoutInfo layout, bool firstLine)
				=> Math.Max(0, (float)availableSize.Width
					- GetParagraphLeftInset(layout, firstLine)
					- GetParagraphRightInset(layout, firstLine));
			if (paragraphLayouts is not null && paragraphLayouts.Count > 0)
			{
				var pl = paragraphLayouts[0].layout;
				effectiveAvailableWidth = GetEffectiveAvailableWidth(pl, firstLine: true);
			}
			LinkedListNode<Cluster>? currentClusterBreak = clusterBreaks.First!;
			// After emitting a line, update effective width for paragraph indents.
			void UpdateEffectiveWidthAfterLine(int lineEnd)
			{
				if (paragraphLayouts is null)
				{
					return;
				}
				// Advance paragraph layout index if needed
				while (paragraphLayoutIndex < paragraphLayouts.Count - 1 && paragraphLayouts[paragraphLayoutIndex].end <= lineEnd)
				{
					paragraphLayoutIndex++;
				}
				isFirstLineOfCurrentParagraph = global::Microsoft.UI.Text.TextUnitNavigation.IsParagraphBreakAt(text, lineEnd);
				var pl = paragraphLayouts[paragraphLayoutIndex].layout;
				effectiveAvailableWidth = GetEffectiveAvailableWidth(pl, isFirstLineOfCurrentParagraph);
			}
			for (var lineOpportunityBreakIndex = 0; lineOpportunityBreakIndex < lineOpportunityBreaks.Count; lineOpportunityBreakIndex++)
			{
				while (currentClusterBreak?.Value.end <= lineOpportunityBreaks[lineOpportunityBreakIndex])
				{
					var oldValues = (chunkUnderTestWidth, chunkUnderTestTrailingSpaceWidth, maxHeightFontDetailsInChunkUnderTest, chunkUnderTestContainsOnlyWhitespace);

					var clusterWidth = currentClusterBreak.Value.containsTab
						? ((int)((lineWidth + chunkUnderTestWidth) / tabStopWidth) + 1) * tabStopWidth - (lineWidth + chunkUnderTestWidth)
						: currentClusterBreak.Value.width;

					chunkUnderTestWidth += clusterWidth;
					if (currentClusterBreak.Value is { containsOnlyWhitespace: true, containsTab: false })
					{
						chunkUnderTestTrailingSpaceWidth += clusterWidth;
					}
					else
					{
						chunkUnderTestTrailingSpaceWidth = 0;
					}

					chunkUnderTestContainsOnlyWhitespace &= currentClusterBreak.Value is { containsOnlyWhitespace: true, containsTab: false };

					if (maxHeightFontDetailsInChunkUnderTest is null || maxHeightFontDetailsInChunkUnderTest.LineHeight < currentClusterBreak.Value.fontDetails.LineHeight)
					{
						maxHeightFontDetailsInChunkUnderTest = currentClusterBreak.Value.fontDetails;
					}

					if (textWrapping is TextWrapping.Wrap && chunkUnderTestWidth - chunkUnderTestTrailingSpaceWidth > effectiveAvailableWidth)
					{
						// The chunk being built can't fit on an empty line on its own — we have to break mid-chunk.
						// If it can fit on a line, then it will be moved as a whole to the next line during the
						// line breaking opportunity check below.
						if (currentLineEnd != -1)
						{
							// If anything is already committed on the current line, flush it first so the chunk starts on a fresh line.
							var (currentLineH, currentLineB) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine!, lines.Count == 0, false);
							lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast!, lineWidth, lineWidthWithoutTrailingSpaces, currentLineH, currentLineB));
							UpdateEffectiveWidthAfterLine(currentLineEnd);
						}

						float width;
						float widthWithoutTrailingSpaces;
						FontDetails fontDetails;
						int end;
						LinkedListNode<Cluster> clusterLast;
						if (oldValues.maxHeightFontDetailsInChunkUnderTest is null) // this cluster is the only cluster in the chunk
						{
							width = chunkUnderTestWidth;
							widthWithoutTrailingSpaces = chunkUnderTestWidth - chunkUnderTestTrailingSpaceWidth;
							fontDetails = maxHeightFontDetailsInChunkUnderTest;
							end = currentClusterBreak.Value.end;
							clusterLast = currentClusterBreak;
							if (currentClusterBreak.Value.containsTab)
							{
								// commit the final computed width of this tab stop
								currentClusterBreak.Value = currentClusterBreak.Value with { width = clusterWidth };
							}
						}
						else
						{
							width = oldValues.chunkUnderTestWidth;
							widthWithoutTrailingSpaces = oldValues.chunkUnderTestWidth - oldValues.chunkUnderTestTrailingSpaceWidth;
							fontDetails = oldValues.maxHeightFontDetailsInChunkUnderTest!;
							end = currentClusterBreak.Value.start;
							clusterLast = currentClusterBreak.Previous!;
						}

						var (h, b) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, fontDetails, lines.Count == 0, false);
						lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, end, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, clusterLast, width, widthWithoutTrailingSpaces, h, b));
						UpdateEffectiveWidthAfterLine(end);
						lineWidth = 0;
						lineWidthWithoutTrailingSpaces = 0;
						currentLineEnd = -1;
						currentLineClusterLast = null;
						maxHeightFontDetailsInCurrentLine = null;
						chunkUnderTestWidth = 0;
						chunkUnderTestTrailingSpaceWidth = 0;
						chunkUnderTestContainsOnlyWhitespace = true;
						maxHeightFontDetailsInChunkUnderTest = null;

						if (oldValues.maxHeightFontDetailsInChunkUnderTest is null)
						{
							currentClusterBreak = currentClusterBreak.Next!;
							continue;
						}
						lineOpportunityBreakIndex--;
						break;
					}

					// cannot break line mid cluster, so only consider this a line break opportunity if the cluster ends with a line break opportunity
					// A mandatory line break cannot occur mid cluster
					// WinUI can always break after tabs, even in scenarios where ICU doesn't consider it a line break opportunity, so we follow suit
					if (currentClusterBreak.Value.end == lineOpportunityBreaks[lineOpportunityBreakIndex] || currentClusterBreak.Value.containsTab)
					{
						if (lineWidth + chunkUnderTestWidth - chunkUnderTestTrailingSpaceWidth > effectiveAvailableWidth)
						{
							if (textWrapping is not TextWrapping.NoWrap && currentLineEnd != -1)
							{
								var (h, b) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine!, lines.Count == 0, false);
								lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast!, lineWidth, lineWidthWithoutTrailingSpaces, h, b));
								UpdateEffectiveWidthAfterLine(currentLineEnd);
								lineWidth = 0;
								lineWidthWithoutTrailingSpaces = 0;
								maxHeightFontDetailsInCurrentLine = null;
								currentLineEnd = -1;
								currentLineClusterLast = null;
								if (currentClusterBreak.Value.containsTab) // each "chunk" contains at most one tab, and always at the end
								{
									// recalculate the width of the tab
									chunkUnderTestWidth -= clusterWidth;
									clusterWidth = ((int)(chunkUnderTestWidth / tabStopWidth) + 1) * tabStopWidth - chunkUnderTestWidth;
									chunkUnderTestWidth += clusterWidth;
								}
							}
						}

						currentLineEnd = currentClusterBreak.Value.end;
						currentLineClusterLast = currentClusterBreak;
						lineWidth += chunkUnderTestWidth;
						if (!chunkUnderTestContainsOnlyWhitespace)
						{
							lineWidthWithoutTrailingSpaces = lineWidth - chunkUnderTestTrailingSpaceWidth;
						}
						if (maxHeightFontDetailsInCurrentLine is null || maxHeightFontDetailsInCurrentLine.LineHeight < maxHeightFontDetailsInChunkUnderTest.LineHeight)
						{
							maxHeightFontDetailsInCurrentLine = maxHeightFontDetailsInChunkUnderTest;
						}

						chunkUnderTestWidth = 0;
						chunkUnderTestTrailingSpaceWidth = 0;
						chunkUnderTestContainsOnlyWhitespace = true;
						maxHeightFontDetailsInChunkUnderTest = null;

						if (currentClusterBreak.Value.containsTab) // each "chunk" contains at most one tab, and always at the end
						{
							// commit the final computed width of this tab stop
							currentClusterBreak.Value = currentClusterBreak.Value with { width = clusterWidth };
						}

						if (IsLineBreak(_text, currentClusterBreak.Value.end))
						{
							var (h, b) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine, lines.Count == 0, false);
							lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast, lineWidth, lineWidthWithoutTrailingSpaces, h, b));
							UpdateEffectiveWidthAfterLine(currentLineEnd);
							lineWidth = 0;
							lineWidthWithoutTrailingSpaces = 0;
							maxHeightFontDetailsInCurrentLine = null;
							currentLineEnd = -1;
							currentLineClusterLast = null;
						}

						if (currentClusterBreak.Value.end == _text.Length && currentLineEnd != -1)
						{
							var (h, b) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine ?? defaultFontDetails, lines.Count == 0, true);
							lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast!, lineWidth, lineWidthWithoutTrailingSpaces, h, b));
						}
					}

					currentClusterBreak = currentClusterBreak.Next;
				}
			}
		}

		if (inlineObjects is not null)
		{
			AdjustLinesForInlineObjects(lines);
		}
		if (paragraphAlignments is not null)
		{
			ApplyParagraphAlignments(lines, paragraphAlignments);
		}
		if (paragraphLayouts is not null)
		{
			ApplyParagraphLayouts(lines, paragraphLayouts, _text);
		}
		ApplyParagraphJustification(lines, _text, (float)availableSize.Width, textAlignment!.Value);

		var textEndsInLineBreak = IsLineBreak(_text, _text.Length);
		var (terminalNaturalHeight, terminalNaturalBaseline) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true);
		var terminalEffectiveHeight = endingParagraphLayout is null
			? terminalNaturalHeight
			: ApplyLineSpacingRule(terminalNaturalHeight, endingParagraphLayout);
		var terminalBlockHeight = (endingParagraphLayout?.SpaceBefore ?? 0)
			+ terminalEffectiveHeight
			+ (endingParagraphLayout?.SpaceAfter ?? 0);
		float totalHeight = 0;
		int nextTrimPointLookupStart = 0;
		var endedEarly = false;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			totalHeight += GetLineBlockHeight(line);
			var nextLineHeight = lineIndex < lines.Count - 1
				? GetLineBlockHeight(lines[lineIndex + 1])
				: textEndsInLineBreak
					? terminalBlockHeight
					: 0;
			var actualLineCount = lines.Count + (textEndsInLineBreak ? 1 : 0);
			var isEarlyLastLine = (maxLines > 0 && maxLines < actualLineCount && lineIndex == maxLines - 1) || (lineIndex < actualLineCount - 1 && nextLineHeight + totalHeight > availableSize.Height);

			var lineWidth = line.width;
			LinkedListNode<Cluster> lastClusterIncludedInLine = line.clusterLast;
			var trimAvailableWidth = line.paragraphLayout is { } trimPl
				? Math.Max(0, (float)availableSize.Width
					- GetParagraphLeftInset(trimPl, line.isFirstLineOfParagraph)
					- GetParagraphRightInset(trimPl, line.isFirstLineOfParagraph))
				: (float)availableSize.Width;
			if (textTrimming is TextTrimming.CharacterEllipsis or TextTrimming.WordEllipsis && (line.widthWithoutTrailingSpaces > trimAvailableWidth || isEarlyLastLine))
			{
				IEnumerable<LinkedListNode<Cluster>> possibleTrimPoints;
				if (textTrimming is TextTrimming.WordEllipsis)
				{
					(possibleTrimPoints, nextTrimPointLookupStart) = EnumeratePossibleWordTrimmingBreaks(line, lineOpportunityBreaks, nextTrimPointLookupStart);
				}
				else
				{
					possibleTrimPoints = EnumeratePossibleCharacterTrimmingBreaks(line);
				}

				using var enumerator = possibleTrimPoints.GetEnumerator();
				var hasMore = enumerator.MoveNext();
				while (hasMore)
				{
					var trimPoint = enumerator.Current;
					hasMore = enumerator.MoveNext();

					// don't add ellipsis after white space, include the whitespace in the trimmed-out portion instead
					while (trimPoint.Value is { containsOnlyWhitespace: true, containsTab: false } && trimPoint.Value.start > line.start)
					{
						trimPoint = trimPoint.Previous!;
						if (hasMore && enumerator.Current == trimPoint)
						{
							hasMore = enumerator.MoveNext();
						}
					}

					while (lastClusterIncludedInLine != trimPoint)
					{
						lineWidth -= lastClusterIncludedInLine.Value.width;
						lastClusterIncludedInLine = lastClusterIncludedInLine.Previous!;
					}

					using var buffer = new Buffer();
					buffer.AddUtf16(HorizontalEllipsis);
					buffer.GuessSegmentProperties();
					var trimFontDetails = trimPoint.Value.fontDetails;
					if (!trimFontDetails.SKFont.ContainsGlyph(HorizontalEllipsis[0]))
					{
						trimFontDetails = GetFallbackFont(HorizontalEllipsis[0], trimFontDetails.SKFontSize, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, fontListener) ?? trimFontDetails;
					}
					trimFontDetails.Font.Shape(buffer); // This can be cached and reused across trim points, but it's not expected to be a hotspot
					var trimGlyphPositions = buffer.GetGlyphPositionSpan();
					var trimGlyphInfos = buffer.GetGlyphInfoSpan();
					float ellipsisWidth = 0;
					foreach (var trimGlyphPosition in trimGlyphPositions)
					{
						ellipsisWidth += AdvanceToPixels(trimGlyphPosition.XAdvance, trimFontDetails);
					}
					if (lineWidth + ellipsisWidth <= trimAvailableWidth || !hasMore)
					{
						var clusterEnd = line.clusterLast.Next;
						for (var clusterNode = trimPoint.Next; clusterNode is not null && clusterNode != clusterEnd;)
						{
							var next = clusterNode.Next;
							clusterBreaks.Remove(clusterNode);
							clusterNode = next;
						}

						var ellipsisGlyphList = new LinkedList<Glyph>();
						for (var i = 0; i < trimGlyphInfos.Length; i++)
						{
							ellipsisGlyphList.AddLast(new Glyph(trimGlyphPositions[i], trimGlyphInfos[i].Codepoint));
						}

						var ellipsisCluster = new Cluster(
							trimPoint.Value.end,
							isEarlyLastLine ? _text.Length : line.end,
							ellipsisGlyphList.First!,
							ellipsisGlyphList.Last!,
							trimFontDetails,
							ellipsisWidth,
							0,
							false,
							false,
							false,
							line.paragraphLayout?.RightToLeft ?? _rtl,
							-1,
							-1);
						var ellipsisNode = trimPoint.List!.AddAfter(trimPoint, ellipsisCluster);
						lines[lineIndex] = line = line with { clusterLast = ellipsisNode, widthWithoutTrailingSpaces = lineWidth + ellipsisWidth, width = lineWidth + ellipsisWidth, hasEllipsis = true, end = ellipsisCluster.end };
						break;
					}
				}
			}

			if (isEarlyLastLine)
			{
				lines = lines[..(lineIndex + 1)];
				endedEarly = true;
				break;
			}
		}

		var hasEndingParagraphLine = !endedEarly && lines[^1].end == _text.Length && textEndsInLineBreak;
		if (hasEndingParagraphLine)
		{
			_endingNewLineLineHeight = terminalEffectiveHeight;
			_endingLineBaselineOffset = terminalNaturalBaseline + (terminalEffectiveHeight - terminalNaturalHeight) / 2;
		}
		else
		{
			_endingNewLineLineHeight = null;
			_endingLineBaselineOffset = 0;
		}

		float maxLineWidth = 0;
		_indexToCluster = new List<(int start, int end, LinkedListNode<Cluster> cluster)>();
		_clustersInLogicalOrder = new();
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var paragraphLeft = line.paragraphLayout is { } widthLayout
				? GetParagraphLeftInset(widthLayout, line.isFirstLineOfParagraph)
				: 0;
			var paragraphRight = line.paragraphLayout is { } rightLayout ? GetParagraphRightInset(rightLayout, line.isFirstLineOfParagraph) : 0;
			var measuredLineRight = paragraphLeft + (includeTrailingWhitespaceInMeasurement ? line.width : line.widthWithoutTrailingSpaces) + paragraphRight;
			if (line is { isFirstLineOfParagraph: true, paragraphLayout: { IsList: true, MarkerText.Length: > 0 } markerLayout })
			{
				line.clusterStart.Value.fontDetails.SKFont.MeasureText(markerLayout.MarkerText, out var markerBounds);
				var markerAnchor = GetParagraphMarkerAnchor(markerLayout, (float)availableSize.Width);
				var markerRight = markerLayout.MarkerAlignment switch
				{
					global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor + markerBounds.Width,
					global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor + markerBounds.Width / 2,
					_ => markerAnchor,
				};
				measuredLineRight = Math.Max(measuredLineRight, markerRight);
			}
			maxLineWidth = Math.Max(maxLineWidth, measuredLineRight);
			for (var node = line.clusterStart; ; node = node.Next!)
			{
				node.Value = node.Value with { lineIndex = lineIndex };
				_indexToCluster.Add((node.Value.start, node.Value.end, node));
				_clustersInLogicalOrder.Add(node);
				if (node == line.clusterLast)
				{
					break;
				}
			}
		}
		if (hasEndingParagraphLine)
		{
			var endingWidth = GetParagraphLeftInset(endingParagraphLayout, firstLine: true)
				+ GetParagraphRightInset(endingParagraphLayout, firstLine: true);
			if (endingParagraphLayout is { IsList: true, MarkerText.Length: > 0 } endingMarkerLayout)
			{
				defaultFontDetails.SKFont.MeasureText(endingMarkerLayout.MarkerText, out var markerBounds);
				var markerAnchor = GetParagraphMarkerAnchor(endingMarkerLayout, (float)availableSize.Width);
				var markerRight = endingMarkerLayout.MarkerAlignment switch
				{
					global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor + markerBounds.Width,
					global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor + markerBounds.Width / 2,
					_ => markerAnchor,
				};
				endingWidth = Math.Max(endingWidth, markerRight);
			}
			maxLineWidth = Math.Max(maxLineWidth, endingWidth);
		}

		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var lineRtl = line.paragraphLayout?.RightToLeft ?? _rtl;
			var ellipsisCluster = line.hasEllipsis ? line.clusterLast : null;
			var limit = line.hasEllipsis ? line.clusterLast.Value.start : line.clusterLast.Value.end;
			var lineBidi = ICU.GetMethod<ICU.ubidi_open>()();
			ICU.GetMethod<ICU.ubidi_setLine>()(bidi, line.start, limit, lineBidi, out var setLineErrorCode);
			ICU.CheckErrorCode<ICU.ubidi_setLine>(setLineErrorCode);
			using var lineBidiDisposable = new DisposableStruct<IntPtr>(static bidi => ICU.GetMethod<ICU.ubidi_close>()(bidi), lineBidi);

			var logicalToVisualMap = ArrayPool<int>.Shared.Rent(_text.Length);
			using var logicalToVisualMapDisposable = new DisposableStruct<int[]>(static logicalToVisualMap => ArrayPool<int>.Shared.Return(logicalToVisualMap), logicalToVisualMap);

			fixed (int* logicalToVisualMapPtr = logicalToVisualMap)
			{
				ICU.GetMethod<ICU.ubidi_getLogicalMap>()(lineBidi, (IntPtr)logicalToVisualMapPtr, out var getLogicalMapErrorCode);
				ICU.CheckErrorCode<ICU.ubidi_getLogicalMap>(getLogicalMapErrorCode);
			}

			var levels = ICU.GetMethod<ICU.ubidi_getLevels>()(lineBidi, out var getLevelsErrorCode);
			ICU.CheckErrorCode<ICU.ubidi_getLevels>(getLevelsErrorCode);
			var levelsSpan = new Span<byte>(levels.ToPointer(), limit - line.start);

			var clusterBeforeBegin = line.clusterStart.Previous;

			var nodes = new List<LinkedListNode<Cluster>>();
			for (var (clusterNode, end) = (line.clusterStart, (ellipsisCluster ?? line.clusterLast.Next)); clusterNode != end;)
			{
				clusterNode!.Value = clusterNode.Value with { rtl = levelsSpan[clusterNode.Value.start - line.start] % 2 == 1 };
				var next = clusterNode.Next;
				nodes.Add(clusterNode);
				clusterBreaks.Remove(clusterNode);
				clusterNode = next;
			}

			if (ellipsisCluster is not null)
			{
				clusterBreaks.Remove(ellipsisCluster);
				ellipsisCluster.Value = ellipsisCluster.Value with { indexInLine = lineRtl ? 0 : nodes.Count };
			}

			nodes.Sort((node1, node2) => logicalToVisualMap[node1.Value.start - line.start].CompareTo(logicalToVisualMap[node2.Value.start - line.start]));

			for (var index = 0; index < nodes.Count; index++)
			{
				var clusterNode = nodes[index];
				clusterNode.Value = clusterNode.Value with { indexInLine = index + (ellipsisCluster is not null && lineRtl ? 1 : 0) };
				var anchorNode = index == 0 ? clusterBeforeBegin : nodes[index - 1];
				if (anchorNode is null)
				{
					clusterBreaks.AddFirst(clusterNode);
				}
				else
				{
					clusterBreaks.AddAfter(anchorNode, clusterNode);
				}
			}

			var newFirstNode = nodes[0];
			var newLastNode = nodes[^1];
			if (ellipsisCluster is not null)
			{
				if (!lineRtl)
				{
					clusterBreaks.AddAfter(nodes[^1], ellipsisCluster);
					newLastNode = ellipsisCluster;
				}
				else
				{
					clusterBreaks.AddBefore(nodes[0], ellipsisCluster);
					newFirstNode = ellipsisCluster;
				}
			}
			lines[lineIndex] = line = line with { clusterStart = newFirstNode, clusterLast = newLastNode };
		}

		_xyTable = new List<(float prefixSummedHeight, List<(float sumUntilAfterCluster, Cluster cluster)> prefixSummedWidths, Line line)>(lines.Count);
		float prefixSummedHeight = 0;
		for (var lineIdx = 0; lineIdx < lines.Count; lineIdx++)
		{
			var line = lines[lineIdx];
			prefixSummedHeight += GetLineBlockHeight(line);

			var prefixSummedWidths = new List<(float sumUntilAfterCluster, Cluster cluster)>();
			float sumUntilAfterCluster = 0;
			for (var (node, end) = (line.clusterStart, line.clusterLast.Next); node != end; node = node.Next!)
			{
				sumUntilAfterCluster += node.Value.width;
				prefixSummedWidths.Add((sumUntilAfterCluster, node.Value));
			}

			_xyTable.Add((prefixSummedHeight, prefixSummedWidths, line));
		}

		_lines = lines;
		_defaultFontDetails = defaultFontDetails;
		_textAlignment = textAlignment!.Value;
		_wordBoundaries = GetWords(_text);
		_corrections = isSpellCheckEnabled ? _spellCheckingService.Value?.SpellCheck(_wordBoundaries, _text) : null;
		// Use _xyTable's final prefixSummedHeight which includes paragraph spacing
		var finalHeight = _xyTable.Count > 0 ? _xyTable[^1].prefixSummedHeight : 0;
		_endingLineContentTop = hasEndingParagraphLine
			? finalHeight + (endingParagraphLayout?.SpaceBefore ?? 0)
			: 0;
		if (hasEndingParagraphLine)
		{
			finalHeight += (endingParagraphLayout?.SpaceBefore ?? 0)
				+ _endingNewLineLineHeight!.Value
				+ (endingParagraphLayout?.SpaceAfter ?? 0);
		}
		calculatedSize = new Size(maxLineWidth, finalHeight);
		_availableSize = availableSize;
	}

	private static IEnumerable<LinkedListNode<Cluster>> EnumeratePossibleCharacterTrimmingBreaks(Line line)
	{
		for (var i = line.clusterLast; ; i = i.Previous!)
		{
			if (i == line.clusterStart)
			{
				yield break;
			}

			yield return i;
		}
	}

	private static (IEnumerable<LinkedListNode<Cluster>> possibleTrimPoints, int nextLookupStart) EnumeratePossibleWordTrimmingBreaks(Line line, List<int> lineBreakOpportunities, int lineBreakOpportunitiesLookupStart)
	{
		var possibleTrimPoints = new Stack<LinkedListNode<Cluster>>();
		var currentCluster = line.clusterStart;
		for (var currentlineBreakOpportunity = lineBreakOpportunities[lineBreakOpportunitiesLookupStart];
			 lineBreakOpportunitiesLookupStart < lineBreakOpportunities.Count && (currentlineBreakOpportunity = lineBreakOpportunities[lineBreakOpportunitiesLookupStart]) <= line.end;
			 lineBreakOpportunitiesLookupStart++)
		{
			while (currentCluster.Value.end < currentlineBreakOpportunity)
			{
				currentCluster = currentCluster.Next!;
			}

			if (currentCluster.Value.end == currentlineBreakOpportunity)
			{
				possibleTrimPoints.Push(currentCluster);
			}
		}

		return (possibleTrimPoints, lineBreakOpportunitiesLookupStart);
	}

	private static List<(int start, int end, FontDetails fontDetails, float characterSpacing, bool hidden, FlowDirection direction)> EnumerateShapingRuns(
		List<(int end, Brush? foreground, global::Windows.UI.Color? background, float characterSpacing, bool hidden, FlowDirection direction, TextDecorations decorations, global::Microsoft.UI.Text.UnderlineType? underlineType)> runBreaks,
		List<int> scriptBreaks,
		List<(int end, FlowDirection direction)> bidiBreaks,
		List<(int end, FontDetails fontDetails)> fontBreaks)
	{
		var shapingRuns = new List<(int start, int end, FontDetails fontDetails, float characterSpacing, bool hidden, FlowDirection direction)>();

		int currentRunBreakIndex = 0;
		int currentScriptBreakIndex = 0;
		int currentBidiBreakIndex = 0;
		int currentFontBreakIndex = 0;

		var start = 0;

		while (currentRunBreakIndex < runBreaks.Count)
		{
			var nextRunBreak = runBreaks[currentRunBreakIndex];
			var nextScriptBreak = scriptBreaks[currentScriptBreakIndex];
			var nextBidiBreak = bidiBreaks[currentBidiBreakIndex].end;
			var nextFontBreak = fontBreaks[currentFontBreakIndex].end;

			var nextBreak = Math.Min(Math.Min(nextRunBreak.end, nextScriptBreak), Math.Min(nextBidiBreak, nextFontBreak));

			if (nextBreak > start)
			{
				shapingRuns.Add((start, nextBreak, fontBreaks[currentFontBreakIndex].fontDetails, nextRunBreak.characterSpacing, nextRunBreak.hidden, bidiBreaks[currentBidiBreakIndex].direction));
				start = nextBreak;
			}

			if (nextBreak == nextRunBreak.end)
			{
				currentRunBreakIndex++;
			}
			if (nextBreak == nextScriptBreak)
			{
				currentScriptBreakIndex++;
			}
			if (nextBreak == nextBidiBreak)
			{
				currentBidiBreakIndex++;
			}
			if (nextBreak == nextFontBreak)
			{
				currentFontBreakIndex++;
			}
		}

		return shapingRuns;
	}

	public void Draw(in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret, // null to skip drawing a caret
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange)
	{
		var highlighterSlicer = new RangeSlicer<(CompositionBrush? background, Brush foreground)>(0, _text.Length);
		foreach (var highlighter in highlighters)
		{
			foreach (var range in highlighter.Ranges)
			{
				if (range.Length != 0 && range.StartIndex < _text.Length && _text.Length > 0)
				{
					highlighterSlicer.Mark(
						Math.Min(range.StartIndex, _text.Length),
						Math.Min(range.StartIndex + range.Length, _text.Length),
						(highlighter.Background?.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()),
							highlighter.Foreground ?? _blackBrush));
				}
			}
		}

		Dictionary<SKColor, Dictionary<SKFont, (List<ushort> glyphs, List<SKPoint> positions)>> _colorToFontToGlyphs = new();
		List<TextDecorationDrawInfo> textDecorations = new();
		List<(SKPath path, float strokeThickness)> spellCheckUnderlines = new();
		List<(float x1, float x2, float y, SKColor color)> compositionUnderlines = new();
		List<(SKImage image, SKRect destination)>? inlineObjectImages = null;

		SKRect? caretRect = default;

		var runBreakIndex = 0;
		var wordBoundariesIndex = 0;
		var highlighterSlices = highlighterSlicer.GetSegments();
		var highlighterIndex = 0;
		for (var clusterIndex = 0; clusterIndex < _clustersInLogicalOrder.Count; clusterIndex++)
		{
			var cluster = _clustersInLogicalOrder[clusterIndex];
			while (highlighterSlices[highlighterIndex].End <= cluster.Value.start)
			{
				highlighterIndex++;
			}

			var highlighter = highlighterSlices[highlighterIndex];

			while (_runBreaks[runBreakIndex].end <= cluster.Value.start)
			{
				runBreakIndex++;
			}

			while (_wordBoundaries[wordBoundariesIndex] <= cluster.Value.start)
			{
				wordBoundariesIndex++;
			}

			var lineIndex = cluster.Value.lineIndex;
			var line = _lines[lineIndex];
			var y = GetLineContentTop(lineIndex);
			var unalignedX = cluster.Value.indexInLine == 0
				? 0
				: _xyTable[lineIndex].prefixSummedWidths[cluster.Value.indexInLine - 1].sumUntilAfterCluster;
			var alignmentOffset = GetAlignmentOffsetForLine(line);
			var positionAcc = new SKPoint(unalignedX + alignmentOffset, y + line.baselineOffset);
			var fontDetails = cluster.Value.fontDetails;

			if (!cluster.Value.hidden && cluster.Value.inlineObject is { } inlineObject)
			{
				if (inlineObject.Image is { } image)
				{
					var imageY = GetInlineObjectTop(inlineObject, line.lineHeight, line.baselineOffset);
					(inlineObjectImages ??= new()).Add((
						image,
						new SKRect(
							unalignedX + alignmentOffset,
							y + imageY,
							unalignedX + alignmentOffset + inlineObject.Width,
							y + imageY + inlineObject.Height)));
				}
			}
			else if (!cluster.Value.hidden && !cluster.Value.containsTab && (!cluster.Value.containsOnlyWhitespace || FeatureConfiguration.TextBlock.RenderWhiteSpace))
			{
				var color = BrushToColor(highlighter.Value.foreground is { } h ? h : _runBreaks[runBreakIndex].foreground, session.Opacity);
				if (!_colorToFontToGlyphs.TryGetValue(color, out var fontToGlyphs))
				{
					_colorToFontToGlyphs[color] = fontToGlyphs = new Dictionary<SKFont, (List<ushort> glyphs, List<SKPoint> positions)>();
				}
				if (!fontToGlyphs.TryGetValue(fontDetails.SKFont, out var glyphsAndPositions))
				{
					fontToGlyphs[fontDetails.SKFont] = glyphsAndPositions = (new List<ushort>(), new List<SKPoint>());
				}
				var glyphs = glyphsAndPositions.glyphs;
				var positions = glyphsAndPositions.positions;

				for (var glyphNode = cluster.Value.glyphStart; ; glyphNode = glyphNode.Next!)
				{
					var glyph = glyphNode.Value;
					glyphs.Add((ushort)glyph.Codepoint);
					positions.Add(new SKPoint(positionAcc.X + AdvanceToPixels(glyph.GlyphPosition.XOffset, fontDetails),
						positionAcc.Y + AdvanceToPixels(glyph.GlyphPosition.YOffset, fontDetails)));
					positionAcc.X += AdvanceToPixels(glyph.GlyphPosition.XAdvance, fontDetails) + cluster.Value.characterSpacing;
					if (cluster.Value.glyphLast == glyphNode)
					{
						break;
					}
				}
			}

			// Floor every edge and +1 the trailing edges so adjacent background
			// rects always overlap by 1 px, preventing antialiased-edge seams.
			var backgroundRect = new SKRect(
				MathF.Floor(unalignedX + alignmentOffset),
				MathF.Floor(y),
				MathF.Floor(unalignedX + alignmentOffset + cluster.Value.width) + 1,
				MathF.Floor(y + line.lineHeight) + 1);
			if (!cluster.Value.hidden && _runBreaks[runBreakIndex].background is { } characterBackground)
			{
				_spareCharacterBackgroundPaint.Color = new SKColor(
					characterBackground.R,
					characterBackground.G,
					characterBackground.B,
					(byte)(characterBackground.A * session.Opacity));
				session.Canvas.DrawRect(backgroundRect, _spareCharacterBackgroundPaint);
			}
			if (!cluster.Value.hidden)
			{
				highlighter.Value.background?.Paint(session.Canvas, session.Opacity, backgroundRect);
			}

			if (!cluster.Value.hidden && cluster.Value.width > 0)
			{
				var run = _runBreaks[runBreakIndex];
				var metrics = fontDetails.SKFontMetrics;
				var foreground = BrushToColor(run.foreground, session.Opacity);
				var underline = run.underlineType
					?? (run.decorations.HasFlag(TextDecorations.Underline)
						? global::Microsoft.UI.Text.UnderlineType.Single
						: global::Microsoft.UI.Text.UnderlineType.None);
				if (underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined
					&& (underline != global::Microsoft.UI.Text.UnderlineType.Words || !cluster.Value.containsOnlyWhitespace))
				{
					textDecorations.Add(new TextDecorationDrawInfo(
						unalignedX + alignmentOffset,
						unalignedX + alignmentOffset + cluster.Value.width,
						y + line.baselineOffset + (metrics.UnderlinePosition ?? 0),
						metrics.UnderlineThickness ?? 1,
						fontDetails.SKFontSize,
						foreground,
						underline));
				}

				if (run.decorations.HasFlag(TextDecorations.Strikethrough))
				{
					textDecorations.Add(new TextDecorationDrawInfo(
						unalignedX + alignmentOffset,
						unalignedX + alignmentOffset + cluster.Value.width,
						y + line.baselineOffset + (metrics.StrikeoutPosition ?? fontDetails.SKFontSize / -2),
						metrics.StrikeoutThickness ?? 1,
						fontDetails.SKFontSize,
						foreground,
						global::Microsoft.UI.Text.UnderlineType.Single));
				}
			}

			if (!cluster.Value.hidden && _corrections?[wordBoundariesIndex] is { } correction)
			{
				var correctionIndexBase = wordBoundariesIndex == 0 ? 0 : _wordBoundaries[wordBoundariesIndex - 1];
				if (correctionIndexBase + correction.correctionStart <= cluster.Value.start && correctionIndexBase + correction.correctionEnd >= cluster.Value.end)
				{
					var fontSize = fontDetails.SKFontSize;
					var scale = fontSize / 12.0f;
					var step = 4 * scale;
					var amplitude = 2 * scale;
					var yOffset = 2 * scale;

					var p = new SKPath();
					var underlineY = y + line.baselineOffset + yOffset;
					var underlineLeftX = unalignedX + alignmentOffset;
					var underlineRightX = underlineLeftX + cluster.Value.width;
					p.MoveTo(underlineLeftX, underlineY);
					var x = underlineLeftX;
					var up = true;
					var segments = 0;
					while (x + step < underlineRightX && segments++ < 4096)
					{
						x += step;
						var yWave = underlineY + (up ? -amplitude : amplitude);
						p.LineTo(x, yWave);
						up = !up;
					}
					p.LineTo(underlineRightX, underlineY);

					spellCheckUnderlines.Add((p, scale));
				}
			}

			if (!cluster.Value.hidden && compositionRange is var (compStart, compLen) && compLen > 0)
			{
				var compEnd = compStart + compLen;
				if (cluster.Value.start < compEnd && cluster.Value.end > compStart)
				{
					// Place the underline just below the baseline
					var underlineY = y + line.baselineOffset + fontDetails.SKFontSize / 6.0f;
					var underlineLeftX = unalignedX + alignmentOffset;
					var underlineRightX = underlineLeftX + cluster.Value.width;
					var foreColor = BrushToColor(_runBreaks[runBreakIndex].foreground, session.Opacity);
					compositionUnderlines.Add((underlineLeftX, underlineRightX, underlineY, foreColor));
				}
			}

			if (caret is var (caretIndex, _, caretThickness))
			{
				if (caretIndex >= cluster.Value.start && caretIndex < cluster.Value.end)
				{
					caretRect = cluster.Value.rtl
						? new SKRect(cluster.Value.width + alignmentOffset + unalignedX - caretThickness, y, cluster.Value.width + alignmentOffset + unalignedX, y + line.lineHeight)
						: new SKRect(alignmentOffset + unalignedX, y, alignmentOffset + unalignedX + caretThickness, y + line.lineHeight);
				}
				else if (_endingNewLineLineHeight is null && caretIndex >= cluster.Value.start && clusterIndex == _clustersInLogicalOrder.Count - 1)
				{
					caretRect = cluster.Value.rtl
						? new SKRect(alignmentOffset + unalignedX - caretThickness, y, alignmentOffset + unalignedX, y + line.lineHeight)
						: new SKRect(cluster.Value.width + alignmentOffset + unalignedX, y, cluster.Value.width + alignmentOffset + unalignedX + caretThickness, y + line.lineHeight);
				}
			}
		}

		// This would probably be more efficient with SKCanvas::DrawGlyphs, but it isn't exposed in SkiaSharp
		using var textBlobBuilder = new SKTextBlobBuilder();
		foreach (var (color, fontToGlyphs) in _colorToFontToGlyphs)
		{
			_spareDrawPaint.Color = color;
			foreach (var (font, (glyphs, positions)) in fontToGlyphs)
			{
				textBlobBuilder.AddPositionedRun(CollectionsMarshal.AsSpan(glyphs), font, CollectionsMarshal.AsSpan(positions));
				session.Canvas.DrawText(textBlobBuilder.Build(), 0, 0, _spareDrawPaint); // SKTextBlobBuilder::Build resets the builder
			}
		}

		DrawTextDecorations(session.Canvas, textDecorations);

		// Draw paragraph list markers on the first visual line of each paragraph
		DrawParagraphMarkers(session);

		_spareInlineObjectPaint.Color = SKColors.White.WithAlpha((byte)Math.Clamp(session.Opacity * byte.MaxValue, 0, byte.MaxValue));
		foreach (var (image, destination) in inlineObjectImages ?? [])
		{
			session.Canvas.DrawImage(image, destination, _spareInlineObjectPaint);
		}

		foreach (var (path, strokeThickness) in spellCheckUnderlines)
		{
			_spareSpellCheckPaint.StrokeWidth = strokeThickness;
			session.Canvas.DrawPath(path, _spareSpellCheckPaint);
		}

		foreach (var (x1, x2, underlineY, color) in compositionUnderlines)
		{
			_spareCompositionUnderlinePaint.Color = color;
			session.Canvas.DrawLine(x1, underlineY, x2, underlineY, _spareCompositionUnderlinePaint);
		}

		if (caretRect is null && caret?.index == _text.Length) // ending new line or empty text
		{
			var alignmentOffset = GetAlignmentOffsetForLine(null);
			var top = _endingLineContentTop;
			var height = _endingNewLineLineHeight ?? _defaultFontDetails.LineHeight;
			caretRect = IsLineRightToLeft(null)
				? new SKRect(alignmentOffset - caret.Value.thickness, top, alignmentOffset, top + height)
				: new SKRect(alignmentOffset, top, alignmentOffset + caret.Value.thickness, top + height);
		}

		if (caretRect is not null)
		{
			caret!.Value.brush.Paint(session.Canvas, session.Opacity, caretRect.Value);
		}
	}

	private void DrawParagraphMarkers(in Visual.PaintingSession session)
	{
		var runBreakIndex = 0;
		for (var lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
		{
			var line = _lines[lineIndex];
			if (!line.isFirstLineOfParagraph
				|| line.paragraphLayout is not { MarkerText.Length: > 0 } layout)
			{
				continue;
			}

			while (runBreakIndex < _runBreaks.Count - 1 && _runBreaks[runBreakIndex].end <= line.start)
			{
				runBreakIndex++;
			}

			var font = line.clusterStart.Value.fontDetails.SKFont;
			DrawParagraphMarker(
				session,
				layout,
				font,
				(float)_availableSize.Width,
				GetLineContentTop(lineIndex) + line.baselineOffset,
				_runBreaks[runBreakIndex].foreground);
		}

		if (_endingNewLineLineHeight is not null
			&& _endingParagraphLayout is { MarkerText.Length: > 0 } endingLayout)
		{
			DrawParagraphMarker(
				session,
				endingLayout,
				_defaultFontDetails.SKFont,
				(float)_availableSize.Width,
				_endingLineContentTop + _endingLineBaselineOffset,
				_defaultForeground);
		}
	}

	private static void DrawParagraphMarker(
		in Visual.PaintingSession session,
		ParagraphLayoutInfo layout,
		SKFont font,
		float totalWidth,
		float baseline,
		Brush? foreground)
	{
		font.MeasureText(layout.MarkerText, out var markerBounds);
		var markerAnchor = GetParagraphMarkerAnchor(layout, totalWidth);
		var markerLeft = layout.MarkerAlignment switch
		{
			global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor,
			global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor - markerBounds.Width / 2,
			_ => markerAnchor - markerBounds.Width,
		};
		_spareDrawPaint.Color = BrushToColor(foreground, session.Opacity);
		session.Canvas.DrawText(
			layout.MarkerText,
			markerLeft - markerBounds.Left,
			baseline,
			font,
			_spareDrawPaint);
	}

	private static void DrawTextDecorations(SKCanvas canvas, List<TextDecorationDrawInfo> decorations)
	{
		foreach (var decoration in decorations)
		{
			var thickness = Math.Max(0.5f, decoration.Thickness);
			if (IsThickUnderline(decoration.Style))
			{
				thickness = Math.Max(2, thickness * 2);
			}
			else if (decoration.Style == global::Microsoft.UI.Text.UnderlineType.Thin)
			{
				thickness = Math.Max(0.5f, thickness / 2);
			}

			_spareTextDecorationPaint.Color = decoration.Color;
			_spareTextDecorationPaint.StrokeWidth = thickness;
			_spareTextDecorationPaint.StrokeCap = SKStrokeCap.Butt;
			_spareTextDecorationPaint.PathEffect = null;
			switch (decoration.Style)
			{
				case global::Microsoft.UI.Text.UnderlineType.Double:
					var separation = Math.Max(1, thickness * 1.5f);
					canvas.DrawLine(decoration.X1, decoration.Y - separation / 2, decoration.X2, decoration.Y - separation / 2, _spareTextDecorationPaint);
					canvas.DrawLine(decoration.X1, decoration.Y + separation / 2, decoration.X2, decoration.Y + separation / 2, _spareTextDecorationPaint);
					break;
				case global::Microsoft.UI.Text.UnderlineType.Wave:
				case global::Microsoft.UI.Text.UnderlineType.HeavyWave:
					DrawWave(canvas, decoration, thickness, 0);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DoubleWave:
					DrawWave(canvas, decoration, thickness, -Math.Max(1, thickness));
					DrawWave(canvas, decoration, thickness, Math.Max(1, thickness));
					break;
				case global::Microsoft.UI.Text.UnderlineType.Dotted:
				case global::Microsoft.UI.Text.UnderlineType.ThickDotted:
					DrawDashedLine(canvas, decoration, thickness, [thickness, thickness * 2], SKStrokeCap.Round);
					break;
				case global::Microsoft.UI.Text.UnderlineType.Dash:
				case global::Microsoft.UI.Text.UnderlineType.ThickDash:
					DrawDashedLine(canvas, decoration, thickness, [thickness * 4, thickness * 2], SKStrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DashDot:
				case global::Microsoft.UI.Text.UnderlineType.ThickDashDot:
					DrawDashedLine(canvas, decoration, thickness, [thickness * 4, thickness * 2, thickness, thickness * 2], SKStrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DashDotDot:
				case global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot:
					DrawDashedLine(canvas, decoration, thickness, [thickness * 4, thickness * 2, thickness, thickness * 2, thickness, thickness * 2], SKStrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.LongDash:
				case global::Microsoft.UI.Text.UnderlineType.ThickLongDash:
					DrawDashedLine(canvas, decoration, thickness, [thickness * 8, thickness * 3], SKStrokeCap.Butt);
					break;
				default:
					canvas.DrawLine(decoration.X1, decoration.Y, decoration.X2, decoration.Y, _spareTextDecorationPaint);
					break;
			}
		}

		_spareTextDecorationPaint.PathEffect = null;
		_spareTextDecorationPaint.StrokeCap = SKStrokeCap.Butt;
	}

	private static void DrawDashedLine(SKCanvas canvas, TextDecorationDrawInfo decoration, float thickness, float[] intervals, SKStrokeCap cap)
	{
		using var pathEffect = SKPathEffect.CreateDash(intervals, 0);
		_spareTextDecorationPaint.StrokeWidth = thickness;
		_spareTextDecorationPaint.StrokeCap = cap;
		_spareTextDecorationPaint.PathEffect = pathEffect;
		canvas.DrawLine(decoration.X1, decoration.Y, decoration.X2, decoration.Y, _spareTextDecorationPaint);
		_spareTextDecorationPaint.PathEffect = null;
	}

	private static void DrawWave(SKCanvas canvas, TextDecorationDrawInfo decoration, float thickness, float yOffset)
	{
		var amplitude = Math.Max(1, decoration.FontSize / 12);
		var step = amplitude * 2;
		using var path = new SKPath();
		path.MoveTo(decoration.X1, decoration.Y + yOffset);
		var x = decoration.X1;
		var up = true;
		var segments = 0;
		while (x + step < decoration.X2 && segments++ < 4096)
		{
			x += step;
			path.LineTo(x, decoration.Y + yOffset + (up ? -amplitude : amplitude));
			up = !up;
		}
		path.LineTo(decoration.X2, decoration.Y + yOffset);
		_spareTextDecorationPaint.StrokeWidth = thickness;
		canvas.DrawPath(path, _spareTextDecorationPaint);
	}

	private static bool IsThickUnderline(global::Microsoft.UI.Text.UnderlineType style)
		=> style is global::Microsoft.UI.Text.UnderlineType.Thick
			or global::Microsoft.UI.Text.UnderlineType.HeavyWave
			or global::Microsoft.UI.Text.UnderlineType.ThickDash
			or global::Microsoft.UI.Text.UnderlineType.ThickDashDot
			or global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot
			or global::Microsoft.UI.Text.UnderlineType.ThickDotted
			or global::Microsoft.UI.Text.UnderlineType.ThickLongDash;

	public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(int correctionStart, int correctionEnd)
	{
		if (_spellCheckingService.Value is { } spellCheckingService)
		{
			return spellCheckingService.GetSpellCheckSuggestions(_text, _wordBoundaries, correctionStart, correctionEnd);
		}
		else
		{
			return null;
		}
	}

	private static SKColor BrushToColor(Brush? brush, float opacity)
	{
		var color = SKColors.Black;
		if (brush is SolidColorBrush scb)
		{
			var scbColor = scb.Color;
			color = new SKColor(
				red: scbColor.R,
				green: scbColor.G,
				blue: scbColor.B,
				alpha: (byte)(scbColor.A * scb.Opacity * opacity));
		}
		else if (brush is GradientBrush gb)
		{
			var gbColor = gb.FallbackColorWithOpacity;
			color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}
		else if (brush is XamlCompositionBrushBase xcbb)
		{
			var gbColor = xcbb.FallbackColorWithOpacity;
			color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}

		return color;
	}


	private static Hyperlink? TryGetHyperLink(Inline inline)
	{
		DependencyObject? parent = inline;
		while (parent is TextElement textElement)
		{
			if (parent is Hyperlink hyperlink)
			{
				return hyperlink;
			}
			parent = textElement.GetParent() as DependencyObject;
		}

		return null;
	}

	public Rect GetRectForIndex(int index)
	{
		index = Math.Min(index, _text.Length);

		if (index == 0)
		{
			double alignmentOffset = string.IsNullOrEmpty(_text) ? GetAlignmentOffsetForLine(null) : GetAlignmentOffsetForLine(_lines[0]);
			return new Rect(
				alignmentOffset,
				string.IsNullOrEmpty(_text) ? _endingLineContentTop : GetLineContentTop(0),
				0,
				string.IsNullOrEmpty(_text) ? _endingNewLineLineHeight ?? _defaultFontDetails.LineHeight : _lines[0].lineHeight);
		}

		if (index == _text.Length)
		{
			var (alignmentOffset, lineWidth, height, y) = _endingNewLineLineHeight is { } endingNewLineLineHeight
				? (GetAlignmentOffsetForLine(null), 0, endingNewLineLineHeight, _endingLineContentTop)
				: (GetAlignmentOffsetForLine(_lines[^1]), _lines[^1].width, _lines[^1].lineHeight, GetLineContentTop(_lines.Count - 1));
			return IsLineRightToLeft(null) ?
				new Rect(alignmentOffset, y, 0, height) :
				new Rect(alignmentOffset + lineWidth, y, 0, height);
		}
		else
		{
			var clusterIndex = _indexToCluster.BinarySearch(
				(index, index, null!),
				Comparer<(int start, int end, LinkedListNode<Cluster> cluster)>.Create(static (a, b) => a.start.CompareTo(b.start)));

			if (clusterIndex < 0)
			{
				clusterIndex = ~clusterIndex - 1;
			}

			var cluster = _indexToCluster[clusterIndex].cluster;
			var lineIndex = cluster.Value.lineIndex;
			var line = _lines[lineIndex];
			var indexInLine = cluster.Value.indexInLine;

			var alignmentOffset = GetAlignmentOffsetForLine(line);
			var y = GetLineContentTop(lineIndex);
			var unalignedX = indexInLine == 0 ? (IsLineRightToLeft(line) ? line.width : 0) : _xyTable[lineIndex].prefixSummedWidths[indexInLine - 1].sumUntilAfterCluster;
			return new Rect(alignmentOffset + unalignedX, y, cluster.Value.width, line.lineHeight);
		}
	}

	public double GetBaselineForIndex(int index)
	{
		index = Math.Clamp(index, 0, _text.Length);
		if (_text.Length == 0)
		{
			return _endingLineContentTop + _endingLineBaselineOffset;
		}

		if (index == _text.Length && _endingNewLineLineHeight is not null)
		{
			return _endingLineContentTop + _endingLineBaselineOffset;
		}

		var lineIndex = GetLineAt(index).lineIndex;
		var line = _lines[lineIndex];
		var lineTop = GetLineContentTop(lineIndex);
		return lineTop + line.baselineOffset;
	}

	public int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection)
	{
		if (_text.Length == 0)
		{
			return extendedSelection ? 0 : -1;
		}

		if (p.Y < 0)
		{
			return extendedSelection ? 0 : -1;
		}

		var contentBottom = _endingNewLineLineHeight is { } endingHeight
			? _endingLineContentTop + endingHeight + (_endingParagraphLayout?.SpaceAfter ?? 0)
			: _xyTable[^1].prefixSummedHeight;
		if (p.Y >= contentBottom)
		{
			if (!extendedSelection)
			{
				return -1;
			}
			var lastLineRtl = IsLineRightToLeft(_lines[^1]);
			if (lastLineRtl && p.X > GetAlignmentOffsetForLine(_lines[^1]) + _lines[^1].width || !lastLineRtl && p.X < GetAlignmentOffsetForLine(_lines[^1]))
			{
				// corner case: bottom left (or bottom right if rtl) of the box, we can either go to the beginning or the end.
				// we match winui and go to the beginning.

				return 0;
			}
			else
			{
				return _text.Length - (ignoreEndingNewLine ? TrailingCRLFInLine(_endingNewLineLineHeight is null ? _lines[^1] : null) : 0);
			}
		}

		if (p.Y >= _xyTable[^1].prefixSummedHeight)
		{
			return extendedSelection ? _lines[^1].end - (ignoreEndingNewLine ? TrailingCRLFInLine(_lines[^1]) : 0) : -1;
		}

		var lineIndex = _xyTable.BinarySearch(
			((float)p.Y, null!, default),
			Comparer<(float prefixSummedHeight, List<(float sumUntilAfterCluster, Cluster cluster)> prefixSummedWidths, Line line)>.Create(
				static (a, b) => a.prefixSummedHeight.CompareTo(b.prefixSummedHeight)));

		if (lineIndex < 0)
		{
			lineIndex = ~lineIndex;
		}

		var line = _xyTable[lineIndex].line;
		var alignmentOffset = GetAlignmentOffsetForLine(line);

		if (p.X < alignmentOffset)
		{
			return extendedSelection
				? (IsLineRightToLeft(line) ? line.end - (ignoreEndingNewLine ? TrailingCRLFInLine(line) : 0) : line.start)
				: -1;
		}

		if (p.X >= alignmentOffset + line.width)
		{
			return extendedSelection
				? (IsLineRightToLeft(line) ? line.start : line.end - (ignoreEndingNewLine ? TrailingCRLFInLine(line) : 0))
				: -1;
		}

		var prefixSummedWidths = _xyTable[lineIndex].prefixSummedWidths;
		var clusterIndex = prefixSummedWidths.BinarySearch(
			((float)p.X - GetAlignmentOffsetForLine(line), default),
			Comparer<(float sumUntilAfterCluster, Cluster cluster)>.Create(
				static (a, b) => a.sumUntilAfterCluster.CompareTo(b.sumUntilAfterCluster)));

		if (clusterIndex < 0)
		{
			clusterIndex = ~clusterIndex;
		}

		var cluster = prefixSummedWidths[clusterIndex].cluster;
		var right = prefixSummedWidths[clusterIndex].sumUntilAfterCluster;
		var left = right - cluster.width;
		var closerToLeftEdge = p.X - GetAlignmentOffsetForLine(line) - left < right - (p.X - GetAlignmentOffsetForLine(line));
		var index = cluster.rtl
			? closerToLeftEdge ? cluster.end : cluster.start
			: closerToLeftEdge ? cluster.start : cluster.end;
		return index - (ignoreEndingNewLine && line.end == index ? TrailingCRLFInLine(line) : 0);
	}

	public Hyperlink? GetHyperlinkAt(Point point)
	{
		var index = GetIndexAt(point, false, false);
		if (index == -1)
		{
			return null;
		}

		if (_hyperlinkRanges.Count == 0)
		{
			return null;
		}

		var rangeIndex = _hyperlinkRanges.BinarySearch(
			(index, 0, null!),
			Comparer<(int start, int length, Hyperlink hyperlink)>.Create(static (a, b) => a.start.CompareTo(b.start)));

		if (rangeIndex < 0)
		{
			rangeIndex = ~rangeIndex - 1;
		}

		if (rangeIndex < 0)
		{
			return null;
		}

		var range = _hyperlinkRanges[rangeIndex];
		return index >= range.start && index < range.end ? range.hyperlink : null;
	}

	/// <param name="right">when on a word boundary, decides whether to return the left or the right word</param>
	public (int start, int length) GetWordAt(int index, bool right)
	{
		if (index == 0)
		{
			if (_wordBoundaries.Count == 0)
			{
				return (0, 0);
			}
			else
			{
				return (0, _wordBoundaries[0]);
			}
		}
		else if (index == _text.Length)
		{
			if (_wordBoundaries.Count == 1)
			{
				return (0, _text.Length);
			}
			else
			{
				return (_wordBoundaries[^2], _text.Length - _wordBoundaries[^2]);
			}
		}
		else
		{
			var prevBoundary = 0;
			foreach (var boundary in _wordBoundaries)
			{
				if (index < boundary || boundary == index && !right)
				{
					return (prevBoundary, boundary - prevBoundary);
				}
				prevBoundary = boundary;
			}
			throw new UnreachableException();
		}
	}

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
	{
		if (_text.Length == 0 || _lines.Count == 0)
		{
			return (0, 0, true, true, 0);
		}
		if (index >= _lines[^1].end)
		{
			if (_endingNewLineLineHeight is not null)
			{
				return (_lines[^1].end, _text.Length - _lines[^1].end, false, true, _lines.Count);
			}
			else
			{
				return (_lines[^1].start, _lines[^1].end - _lines[^1].start, _lines.Count == 1, true, _lines.Count - 1);
			}
		}

		var lineIndex = _lines.BinarySearch(
			new Line { end = index + 1 },
			Comparer<Line>.Create(static (a, b) => a.end.CompareTo(b.end)));

		if (lineIndex < 0)
		{
			lineIndex = ~lineIndex;
		}

		var line = _lines[lineIndex];
		return (line.start, line.end - line.start, lineIndex == 0, _endingNewLineLineHeight is null && lineIndex == _lines.Count - 1, lineIndex);
	}

	public bool IsBaseDirectionRightToLeft => _rtl;

	private static List<int> GetWords(string text)
	{
		var boundaries = new List<int>();
		AppendBoundaries(/* Word */ 1, text, 0, boundaries);
		var ret = new List<int> { boundaries[0] };
		for (var index = 1; index < boundaries.Count; index++)
		{
			var boundary = boundaries[index];

			if (boundary - ret[^1] == 1 && (char.IsPunctuation(text[boundary - 1]) || char.IsSymbol(text[boundary - 1])) && (char.IsPunctuation(text[ret[^1] - 1]) || char.IsSymbol(text[ret[^1] - 1])))
			{
				ret.RemoveAt(ret.Count - 1);
			}
			else if (Enumerable.Range(ret[^1], boundary - ret[^1]).All(c => text[c] == ' ') && !char.IsWhiteSpace(text[ret[^1] - 1]))
			{
				ret.RemoveAt(ret.Count - 1);
			}
			ret.Add(boundary);
		}

		return ret;
	}

	private int TrailingCRLFInLine(Line? line)
	{
		if (_text.Length == 0)
		{
			return 0;
		}
		return global::Microsoft.UI.Text.TextUnitNavigation.GetHardLineBreakLengthEndingAt(
			_text,
			line?.end ?? _text.Length);
	}

	private float GetAlignmentOffsetForLine(Line? line)
	{
		var (lineWidth, lineWidthWithoutTrailingSpaces) = line is null ? (0, 0) : (line.Value.width, line.Value.widthWithoutTrailingSpaces);
		var totalWidth = (float)_availableSize.Width;
		var textAlignment = line?.textAlignment ?? _endingParagraphAlignment ?? _textAlignment;

		// Paragraph indent adjustments: left indent shifts the content area right,
		// right indent narrows the content area from the right. First-line indent
		// adds to (or subtracts from) the left indent on the paragraph's first visual line.
		var pl = line?.paragraphLayout ?? _endingParagraphLayout;
		var firstLine = line?.isFirstLineOfParagraph ?? true;
		var leftIndent = GetParagraphLeftInset(pl, firstLine);
		var rightIndent = GetParagraphRightInset(pl, firstLine);
		if (!float.IsFinite(totalWidth))
		{
			return leftIndent;
		}
		var contentWidth = Math.Max(0, totalWidth - leftIndent - rightIndent);

		float alignmentOffset;
		// Trailing whitespace at the end of a line is assigned the paragraph embedding level
		// So the trailing space is always on the left if RTL and always on the right if LTR
		if (IsLineRightToLeft(line))
		{
			alignmentOffset = textAlignment switch
			{
				TextAlignment.Center when lineWidthWithoutTrailingSpaces <= contentWidth => leftIndent + (contentWidth - lineWidthWithoutTrailingSpaces) / 2,
				TextAlignment.Right when lineWidthWithoutTrailingSpaces <= contentWidth => leftIndent + contentWidth - lineWidth,
				_ => leftIndent
			};
			alignmentOffset = Math.Min(alignmentOffset, totalWidth - lineWidth);
		}
		else
		{
			alignmentOffset = textAlignment switch
			{
				TextAlignment.Center when lineWidthWithoutTrailingSpaces <= contentWidth => leftIndent + (contentWidth - lineWidthWithoutTrailingSpaces) / 2,
				TextAlignment.Right when lineWidthWithoutTrailingSpaces <= contentWidth => leftIndent + contentWidth - lineWidthWithoutTrailingSpaces,
				_ => leftIndent
			};
			alignmentOffset = Math.Max(alignmentOffset, 0);
		}

		return alignmentOffset;
	}

	private bool IsLineRightToLeft(Line? line)
		=> line?.paragraphLayout?.RightToLeft ?? _endingParagraphLayout?.RightToLeft ?? _rtl;

	private static float GetParagraphMarkerAnchor(ParagraphLayoutInfo layout, float totalWidth)
		=> layout.RightToLeft
			? Math.Max(0, totalWidth - Math.Max(0, layout.RightIndent + layout.FirstLineIndent))
			: Math.Max(0, layout.LeftIndent + layout.FirstLineIndent);

	private static float GetParagraphLeftInset(ParagraphLayoutInfo? layout, bool firstLine)
	{
		if (layout is null)
		{
			return 0;
		}

		if (layout.RightToLeft)
		{
			return Math.Max(0, layout.LeftIndent);
		}

		var origin = firstLine ? Math.Max(0, layout.LeftIndent + layout.FirstLineIndent) : Math.Max(0, layout.LeftIndent);
		return firstLine && layout.IsList ? origin + Math.Max(0, layout.ListTab) : origin;
	}

	private static float GetParagraphRightInset(ParagraphLayoutInfo? layout, bool firstLine)
	{
		if (layout is null)
		{
			return 0;
		}

		if (!layout.RightToLeft)
		{
			return Math.Max(0, layout.RightIndent);
		}

		var origin = firstLine ? Math.Max(0, layout.RightIndent + layout.FirstLineIndent) : Math.Max(0, layout.RightIndent);
		return firstLine && layout.IsList ? origin + Math.Max(0, layout.ListTab) : origin;
	}

	private float GetLineContentTop(int lineIndex)
	{
		var line = _lines[lineIndex];
		return _xyTable[lineIndex].prefixSummedHeight - line.lineHeight - GetLineSpaceAfter(line);
	}

	private static float GetLineBlockHeight(Line line)
		=> GetLineSpaceBefore(line) + line.lineHeight + GetLineSpaceAfter(line);

	private static float GetLineSpaceBefore(Line line)
		=> line is { isFirstLineOfParagraph: true, paragraphLayout: { } layout } ? layout.SpaceBefore : 0;

	private static float GetLineSpaceAfter(Line line)
		=> line is { isLastLineOfParagraph: true, paragraphLayout: { } layout } ? layout.SpaceAfter : 0;

	private static FontDetails? GetFallbackFont(
		int codepoint,
		float fontSize,
		FontWeight fontWeight,
		FontStretch fontStretch,
		FontStyle fontStyle,
		IFontCacheUpdateListener fontListener,
		bool preferFallbackService = false)
	{
		FontDetails? TryGetFallbackServiceFont()
		{
			var fallbackFontTask = FontDetailsCache.GetFontForCodepoint(codepoint, fontSize, fontWeight, fontStretch, fontStyle);
			if (fallbackFontTask.IsCompleted)
			{
				return fallbackFontTask.IsCompletedSuccessfully ? fallbackFontTask.Result : null;
			}

			if (!_codepointToListeners.TryGetValue(codepoint, out var codepointListeners))
			{
				codepointListeners = new();
				_codepointToListeners[codepoint] = codepointListeners;
			}
			if (codepointListeners.Add(fontListener))
			{
				fallbackFontTask.ContinueWith(_ => NativeDispatcher.Main.Enqueue(() =>
				{
					codepointListeners.Remove(fontListener);
					fontListener.Invalidate();
				}));
			}

			return null;
		}

		if (preferFallbackService && TryGetFallbackServiceFont() is { } preferredFallback)
		{
			return preferredFallback;
		}

		var symbolsFontTask = FontDetailsCache.GetFont(FeatureConfiguration.Font.SymbolsFont, fontSize, fontWeight, fontStretch, fontStyle);
		if (symbolsFontTask.loadedTask.IsCompleted)
		{
			if (symbolsFontTask.loadedTask.IsCompletedSuccessfully
				&& symbolsFontTask.loadedTask.Result is { } symbolsFont
				&& symbolsFont.SKFont.ContainsGlyph(codepoint))
			{
				return symbolsFont;
			}
		}
		else
		{
			if (!_fontFamilyToListeners.TryGetValue(FeatureConfiguration.Font.SymbolsFont, out var fontFamilyListeners))
			{
				fontFamilyListeners = new();
				_fontFamilyToListeners[FeatureConfiguration.Font.SymbolsFont] = fontFamilyListeners;
			}
			if (fontFamilyListeners.Add(fontListener))
			{
				symbolsFontTask.loadedTask.ContinueWith(_ => NativeDispatcher.Main.Enqueue(() =>
				{
					fontFamilyListeners.Remove(fontListener);
					fontListener.Invalidate();
				}));
			}
		}

		if (!preferFallbackService && TryGetFallbackServiceFont() is { } fallbackFont)
		{
			return fallbackFont;
		}

		if (!_skFontManagerDefaultMatchCharacterCache.TryGetValue(codepoint, out var defaultSkiaFontTypeface))
		{
			defaultSkiaFontTypeface = _skFontManagerDefaultMatchCharacterCache[codepoint] = SKFontManager.Default.MatchCharacter(codepoint);
		}
		if (defaultSkiaFontTypeface is { } typeface)
		{
			return FontDetails.Create(typeface, fontSize);
		}

		return null;
	}

	private static bool ContainsEmojiCandidate(string text)
	{
		for (var i = 0; i < text.Length; i += char.IsSurrogate(text, i) ? 2 : 1)
		{
			if (NotoFontFallbackService.IsEmojiCodepoint(char.ConvertToUtf32(text, i)))
			{
				return true;
			}
		}

		return false;
	}

	private static unsafe void AppendBoundaries(int boundaryType, string text, int outputBaseOffset, List<int> list)
	{
		fixed (char* locale = &CultureInfo.CurrentUICulture.Name.GetPinnableReference())
		{
			fixed (char* textPtr = &text.GetPinnableReference())
			{
				var breakIterator = ICU.GetMethod<ICU.ubrk_open>()(boundaryType, (IntPtr)locale, (IntPtr)textPtr, text.Length, out int status);
				ICU.CheckErrorCode<ICU.ubrk_open>(status);
				ICU.GetMethod<ICU.ubrk_first>()(breakIterator);
				while (ICU.GetMethod<ICU.ubrk_next>()(breakIterator) is var next && next != /* UBRK_DONE */ -1)
				{
					list.Add(next + outputBaseOffset);
				}
				ICU.GetMethod<ICU.ubrk_close>()(breakIterator);
			}
		}
	}

	private static void AdjustLinesForInlineObjects(List<Line> lines)
	{
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var minTop = 0f;
			var maxBottom = line.lineHeight;
			for (var node = line.clusterStart; ; node = node.Next!)
			{
				if (node.Value.inlineObject is { } inlineObject)
				{
					var top = GetInlineObjectTop(inlineObject, line.lineHeight, line.baselineOffset);
					minTop = Math.Min(minTop, top);
					maxBottom = Math.Max(maxBottom, top + inlineObject.Height);
				}

				if (node == line.clusterLast)
				{
					break;
				}
			}

			if (minTop < 0 || maxBottom > line.lineHeight)
			{
				lines[lineIndex] = line with
				{
					lineHeight = maxBottom - minTop,
					baselineOffset = line.baselineOffset - minTop,
				};
			}
		}
	}

	private static void ApplyParagraphAlignments(List<Line> lines, List<(int end, TextAlignment alignment)> paragraphAlignments)
	{
		var alignmentIndex = 0;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			while (alignmentIndex < paragraphAlignments.Count - 1 && paragraphAlignments[alignmentIndex].end <= line.start)
			{
				alignmentIndex++;
			}

			lines[lineIndex] = line with { textAlignment = paragraphAlignments[alignmentIndex].alignment };
		}
	}

	private static void ApplyParagraphLayouts(List<Line> lines, List<(int end, ParagraphLayoutInfo layout)> paragraphLayouts, string text)
	{
		var layoutIndex = 0;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			while (layoutIndex < paragraphLayouts.Count - 1 && paragraphLayouts[layoutIndex].end <= line.start)
			{
				layoutIndex++;
			}

			var isFirstLineOfParagraph = lineIndex == 0
				|| global::Microsoft.UI.Text.TextUnitNavigation.IsParagraphBreakAt(text, lines[lineIndex - 1].end);
			var isLastLineOfParagraph = lineIndex == lines.Count - 1
				|| global::Microsoft.UI.Text.TextUnitNavigation.IsParagraphBreakAt(text, line.end);
			var layout = paragraphLayouts[layoutIndex].layout;
			var adjustedLineHeight = ApplyLineSpacingRule(line.lineHeight, layout);
			lines[lineIndex] = line with
			{
				paragraphLayout = layout,
				isFirstLineOfParagraph = isFirstLineOfParagraph,
				isLastLineOfParagraph = isLastLineOfParagraph,
				lineHeight = adjustedLineHeight,
				baselineOffset = line.baselineOffset + (adjustedLineHeight - line.lineHeight) / 2,
			};
		}
	}

	private static void ApplyParagraphJustification(List<Line> lines, string text, float totalWidth, TextAlignment defaultAlignment)
	{
		if (!float.IsFinite(totalWidth))
		{
			return;
		}

		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			if ((line.textAlignment ?? defaultAlignment) != TextAlignment.Justify
				|| lineIndex == lines.Count - 1
				|| global::Microsoft.UI.Text.TextUnitNavigation.IsHardLineBreakAt(text, line.end))
			{
				continue;
			}

			var contentWidth = Math.Max(0, totalWidth
				- GetParagraphLeftInset(line.paragraphLayout, line.isFirstLineOfParagraph)
				- GetParagraphRightInset(line.paragraphLayout, line.isFirstLineOfParagraph));
			var extraWidth = contentWidth - line.widthWithoutTrailingSpaces;
			if (!(extraWidth > 0))
			{
				continue;
			}

			var nonTrailingEnd = line.end - TrailingWhitespaceLength(text, line);
			var stretchableCharacters = 0;
			for (var node = line.clusterStart; ; node = node.Next!)
			{
				if (node.Value.end <= line.end
					&& node.Value is { containsOnlyWhitespace: true, containsTab: false }
					&& node.Value.end <= nonTrailingEnd)
				{
					stretchableCharacters += node.Value.end - node.Value.start;
				}
				if (node == line.clusterLast)
				{
					break;
				}
			}
			if (stretchableCharacters == 0)
			{
				continue;
			}

			for (var node = line.clusterStart; ; node = node.Next!)
			{
				if (node.Value is { containsOnlyWhitespace: true, containsTab: false }
					&& node.Value.end <= nonTrailingEnd)
				{
					var share = extraWidth * (node.Value.end - node.Value.start) / stretchableCharacters;
					node.Value = node.Value with { width = node.Value.width + share };
				}
				if (node == line.clusterLast)
				{
					break;
				}
			}

			lines[lineIndex] = line with
			{
				width = line.width + extraWidth,
				widthWithoutTrailingSpaces = contentWidth,
			};
		}
	}

	private static int TrailingWhitespaceLength(string text, Line line)
	{
		var position = line.end;
		while (position > line.start && char.IsWhiteSpace(text[position - 1])
			&& !global::Microsoft.UI.Text.TextUnitNavigation.IsHardLineBreakAt(text, position))
		{
			position--;
		}
		return line.end - position;
	}

	private static float ApplyLineSpacingRule(float naturalLineHeight, ParagraphLayoutInfo pl)
	{
		return pl.LineSpacingRule switch
		{
			global::Microsoft.UI.Text.LineSpacingRule.Single => naturalLineHeight,
			global::Microsoft.UI.Text.LineSpacingRule.OneAndHalf => naturalLineHeight * 1.5f,
			global::Microsoft.UI.Text.LineSpacingRule.Double => naturalLineHeight * 2f,
			global::Microsoft.UI.Text.LineSpacingRule.AtLeast => Math.Max(naturalLineHeight, pl.LineSpacing),
			global::Microsoft.UI.Text.LineSpacingRule.Exactly => pl.LineSpacing > 0 ? pl.LineSpacing : naturalLineHeight,
			global::Microsoft.UI.Text.LineSpacingRule.Multiple => pl.LineSpacing > 0 ? naturalLineHeight * pl.LineSpacing : naturalLineHeight,
			global::Microsoft.UI.Text.LineSpacingRule.Percent => pl.LineSpacing > 0 ? naturalLineHeight * pl.LineSpacing / 100f : naturalLineHeight,
			_ => naturalLineHeight,
		};
	}

	private static float GetInlineObjectTop(InlineObjectInfo inlineObject, float lineHeight, float baselineOffset)
		=> inlineObject.VerticalAlignment switch
		{
			global::Microsoft.UI.Text.VerticalCharacterAlignment.Top => 0,
			global::Microsoft.UI.Text.VerticalCharacterAlignment.Bottom => lineHeight - inlineObject.Height,
			_ => baselineOffset - (inlineObject.Ascent > 0 ? inlineObject.Ascent : inlineObject.Height),
		};

	// This method assumes that the FontDetails with the biggest LineHeight is also the one with the biggest SKFontMetrics.Top.
	// If that assumption is wrong, we will need an additional lazy parameter for the latter.
	private static (float lineHeight, float baselineOffset) GetLineHeightAndBaselineOffset(LineStackingStrategy lineStackingStrategy, float lineHeight, FontDetails fontDetailsWithMaxHeightInLine, bool isFirstLine, bool isLastLine)
	{
		if (lineStackingStrategy is LineStackingStrategy.MaxHeight || !(lineHeight > 0))
		{
			return (Math.Max(lineHeight, fontDetailsWithMaxHeightInLine.LineHeight), -fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent);
		}
		else if (lineStackingStrategy is LineStackingStrategy.BaselineToBaseline)
		{
			if (isFirstLine)
			{
				return (Math.Min(fontDetailsWithMaxHeightInLine.LineHeight, Math.Max(-fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent, lineHeight)), -fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent);
			}
			else
			{
				if (isLastLine)
				{
					return (lineHeight + fontDetailsWithMaxHeightInLine.SKFontMetrics.Descent, lineHeight);
				}
				else
				{
					return (lineHeight, lineHeight);
				}
			}
		}
		else if (lineStackingStrategy is LineStackingStrategy.BlockLineHeight)
		{
			return (lineHeight, lineHeight - fontDetailsWithMaxHeightInLine.SKFontMetrics.Descent);
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(lineStackingStrategy));
		}
	}

	private static float AdvanceToPixels(float xAdvance, FontDetails details) => xAdvance * details.TextScale.textScaleX;

	private static bool IsLineBreak(string text, int indexAfterLineBreakOpportunity)
		=> global::Microsoft.UI.Text.TextUnitNavigation.IsHardLineBreakAt(text, indexAfterLineBreakOpportunity);

	/// <summary>
	/// Returns the absolute text range of the misspelled word at the given text index,
	/// or null if the index is not on a misspelled word.
	/// </summary>
	public (int correctionStart, int correctionEnd)? GetCorrectionAtIndex(int textIndex)
	{
		if (_corrections is null || _wordBoundaries.Count == 0 || textIndex < 0 || textIndex > _text.Length)
		{
			return null;
		}

		var wordStart = 0;
		for (var i = 0; i < _wordBoundaries.Count; i++)
		{
			var wordEnd = _wordBoundaries[i];
			if (textIndex >= wordStart && textIndex < wordEnd)
			{
				if (i < _corrections.Count && _corrections[i] is { } correction)
				{
					// Convert word-relative offsets to absolute (same as rendering at line 1041)
					var absStart = wordStart + correction.correctionStart;
					var absEnd = wordStart + correction.correctionEnd;
					if (textIndex >= absStart && textIndex < absEnd)
					{
						return (absStart, absEnd);
					}
				}
				return null;
			}
			wordStart = wordEnd;
		}
		return null;
	}
}
