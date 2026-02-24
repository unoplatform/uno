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
using Uno.UI;
using Uno.UI.Dispatching;
using Buffer = HarfBuzzSharp.Buffer;
using FontWeights = Microsoft.UI.Text.FontWeights;

namespace Microsoft.UI.Xaml.Documents;

// TODO tab stop handling with trimming etc
internal readonly partial struct UnicodeText : IParsedText
{
	// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
	private const float TabStopWidth = 48;
	private const byte UBIDI_DEFAULT_LTR = 0xfe;
	private const byte UBIDI_DEFAULT_RTL = 0xff;
	private const int UBIDI_LTR = 0;
	private const int UBIDI_RTL = 1;
	private const string HorizontalEllipsis = "\u2026";

	internal interface IFontCacheUpdateListener
	{
		void Invalidate();
	}

	private record struct Line(int start, int end, LinkedListNode<Cluster> clusterStart, LinkedListNode<Cluster> clusterLast, float width, float widthWithoutTrailingSpaces, float lineHeight, float baselineOffset, bool hasEllipsis = false);

	private record struct Glyph(GlyphPosition GlyphPosition, uint Codepoint);

	private record struct Cluster(
		int start,
		int end,
		LinkedListNode<Glyph> glyphStart,
		LinkedListNode<Glyph> glyphLast,
		FontDetails fontDetails,
		float width,
		bool containsOnlyWhitespace,
		bool containsTab,
		bool rtl,
		int lineIndex,
		int indexInLine)
	{
		public static Cluster Create(string _text, int indexStart, int indexEnd, LinkedListNode<Glyph> glyphsStart, LinkedListNode<Glyph> glyphsLast, FontDetails fontDetails)
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
				clusterWidth += AdvanceToPixels(glyphNode!.Value.GlyphPosition.XAdvance, fontDetails);
			}

			return new(indexStart, indexEnd, glyphsStart, glyphsLast, fontDetails, clusterWidth, clusterContainsOnlyWhitespace, clusterContainsTab, false, -1, -1);
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
			typeof(UnicodeText).LogError()?.Error($"No implementation of {nameof(ISpellCheckingService)} was found. Spell checking will be disabled.");
			return null;
		}
	});

	private static readonly Brush _blackBrush = new SolidColorBrush(Colors.Black);
	private static readonly SKPaint _spareDrawPaint = new() { IsStroke = false, IsAntialias = true };
	private static readonly SKPaint _spareSpellCheckPaint = new() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, IsAntialias = true };
	private static readonly Dictionary<int, HashSet<IFontCacheUpdateListener>> _codepointToListeners = new();
	private static readonly Dictionary<string, HashSet<IFontCacheUpdateListener>> _fontFamilyToListeners = new();
	private readonly string _text;
	private readonly List<(float prefixSummedHeight, List<(float sumUntilAfterCluster, Cluster cluster)> prefixSummedWidths, Line line)> _xyTable;
	private readonly List<(int start, int end, LinkedListNode<Cluster> cluster)> _indexToCluster;
	private readonly TextAlignment _textAlignment;
	private readonly FontDetails _defaultFontDetails;
	private readonly List<Line> _lines;
	private readonly float? _endingNewLineLineHeight;
	private readonly bool _rtl;
	private readonly List<(int start, int end, Hyperlink hyperlink)> _hyperlinkRanges;
	private readonly List<int> _wordBoundaries;
	private readonly List<LinkedListNode<Cluster>> _clustersInLogicalOrder;
	private readonly LinkedList<Glyph> _glyphs;
	private readonly List<(int end, FlowDirection direction)> _bidiBreaks;
	private readonly List<(int end, Brush? foreground, FlowDirection direction)> _runBreaks;
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
		out Size calculatedSize)
	{
		CI.Assert(maxLines >= 0);

		var stringBuilder = new StringBuilder();
		_hyperlinkRanges = new List<(int start, int end, Hyperlink hyperlink)>();
		_runBreaks = new List<(int end, Brush? foreground, FlowDirection direction)>();
		var scriptBreaks = new List<int>();
		var fontBreaks = new List<(int end, FontDetails fontDetails)>();
		var lineOpportunityBreaks = new List<int>();

		foreach (var inline in inlines)
		{
			var inlineText = inline.GetText();
			if (string.IsNullOrEmpty(inlineText))
			{
				continue;
			}

			var inlineStart = stringBuilder.Length;
			stringBuilder.Append(inlineText);

			AppendBoundaries( /* Line */ 2, inlineText, inlineStart, lineOpportunityBreaks);

			var currentFontDetails = inline.FontInfo;
			int currentScript = 0;
			for (var i = 0; i < inlineText.Length; i += char.IsSurrogate(inlineText, i) ? 2 : 1)
			{
				FontDetails newFontDetails;
				var codepoint = char.ConvertToUtf32(inlineText, i);

				var newScript = ICU.GetMethod<ICU.uscript_getScript>()(char.ConvertToUtf32(inlineText, i), out var errorCode);
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

			scriptBreaks.Add(inlineStart + inlineText.Length);
			_runBreaks.Add((inlineStart + inlineText.Length, inline.Foreground, (inline as Run)?.FlowDirection ?? flowDirection));
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
			_endingNewLineLineHeight = null;
			_defaultFontDetails = defaultFontDetails;
			_rtl = flowDirection is FlowDirection.RightToLeft;
			_textAlignment = textAlignment ?? (flowDirection is FlowDirection.RightToLeft ? TextAlignment.Right : TextAlignment.Left);
			_wordBoundaries = [];
			var emptyHeight = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true).lineHeight;
			calculatedSize = new Size(0, emptyHeight);
			_availableSize = availableSize;
			_xyTable = [];
			_indexToCluster = [];
			_clustersInLogicalOrder = [];
			_glyphs = [];
			_bidiBreaks = [];
			return;
		}

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
								shapingRun.fontDetails));
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
								shapingRun.fontDetails));
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
					shapingRun.fontDetails));
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
			LinkedListNode<Cluster>? currentClusterBreak = clusterBreaks.First!;
			for (var lineOpportunityBreakIndex = 0; lineOpportunityBreakIndex < lineOpportunityBreaks.Count; lineOpportunityBreakIndex++)
			{
				while (currentClusterBreak?.Value.end <= lineOpportunityBreaks[lineOpportunityBreakIndex])
				{
					var oldValues = (chunkUnderTestWidth, chunkUnderTestTrailingSpaceWidth, maxHeightFontDetailsInChunkUnderTest, chunkUnderTestContainsOnlyWhitespace);

					var clusterWidth = currentClusterBreak.Value.containsTab
						? ((int)((lineWidth + chunkUnderTestWidth) / TabStopWidth) + 1) * TabStopWidth - (lineWidth + chunkUnderTestWidth)
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

					if (currentLineEnd == -1 && textWrapping is TextWrapping.Wrap && lineWidth + chunkUnderTestWidth - chunkUnderTestTrailingSpaceWidth > availableSize.Width)
					{
						float width;
						float widthWithoutTrailingSpaces;
						FontDetails fontDetails;
						int end;
						LinkedListNode<Cluster> clusterLast;
						if (oldValues.maxHeightFontDetailsInChunkUnderTest is null) // this cluster is the only cluster in the line
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
						if (lineWidth + chunkUnderTestWidth - chunkUnderTestTrailingSpaceWidth > availableSize.Width)
						{
							if (textWrapping is not TextWrapping.NoWrap && currentLineEnd != -1)
							{
								var (h, b) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine!, lines.Count == 0, false);
								lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast!, lineWidth, lineWidthWithoutTrailingSpaces, h, b));
								lineWidth = 0;
								lineWidthWithoutTrailingSpaces = 0;
								maxHeightFontDetailsInCurrentLine = null;
								currentLineEnd = -1;
								currentLineClusterLast = null;
								if (currentClusterBreak.Value.containsTab) // each "chunk" contains at most one tab, and always at the end
								{
									// recalculate the width of the tab
									chunkUnderTestWidth -= clusterWidth;
									clusterWidth = ((int)(chunkUnderTestWidth / TabStopWidth) + 1) * TabStopWidth - chunkUnderTestWidth;
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

		var textEndsInLineBreak = IsLineBreak(_text, _text.Length);
		float totalHeight = 0;
		int nextTrimPointLookupStart = 0;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			totalHeight += line.lineHeight;
			var nextLineHeight = lineIndex < lines.Count - 1
				? lines[lineIndex + 1].lineHeight
				: textEndsInLineBreak
					? GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true).lineHeight
					: 0;
			var actualLineCount = lines.Count + (textEndsInLineBreak ? 1 : 0);
			var isEarlyLastLine = (maxLines > 0 && maxLines < actualLineCount && lineIndex == maxLines - 1) || (lineIndex < actualLineCount - 1 && nextLineHeight + totalHeight > availableSize.Height);

			var lineWidth = line.width;
			LinkedListNode<Cluster> lastClusterIncludedInLine = line.clusterLast;
			if (textTrimming is TextTrimming.CharacterEllipsis or TextTrimming.WordEllipsis && (line.widthWithoutTrailingSpaces > availableSize.Width || isEarlyLastLine))
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
					if (lineWidth + ellipsisWidth <= availableSize.Width || !hasMore)
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
							false,
							false,
							_rtl,
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
				break;
			}
		}

		_endingNewLineLineHeight = lines[^1].end == _text.Length && textEndsInLineBreak ? GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true).lineHeight : null;
		totalHeight += _endingNewLineLineHeight ?? 0;

		float maxLineWidthWithoutTrailingSpaces = 0;
		_indexToCluster = new List<(int start, int end, LinkedListNode<Cluster> cluster)>();
		_clustersInLogicalOrder = new();
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			maxLineWidthWithoutTrailingSpaces = Math.Max(maxLineWidthWithoutTrailingSpaces, line.widthWithoutTrailingSpaces);
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

		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
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
				ellipsisCluster.Value = ellipsisCluster.Value with { indexInLine = _rtl ? 0 : nodes.Count };
			}

			nodes.Sort((node1, node2) => logicalToVisualMap[node1.Value.start - line.start].CompareTo(logicalToVisualMap[node2.Value.start - line.start]));

			for (var index = 0; index < nodes.Count; index++)
			{
				var clusterNode = nodes[index];
				clusterNode.Value = clusterNode.Value with { indexInLine = index + (ellipsisCluster is not null && _rtl ? 1 : 0) };
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
				if (flowDirection is FlowDirection.LeftToRight)
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
		foreach (var line in lines)
		{
			prefixSummedHeight += line.lineHeight;

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
		calculatedSize = new Size(maxLineWidthWithoutTrailingSpaces, totalHeight);
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

	private static List<(int start, int end, FontDetails fontDetails, FlowDirection direction)> EnumerateShapingRuns(
		List<(int end, Brush? foreground, FlowDirection direction)> runBreaks,
		List<int> scriptBreaks,
		List<(int end, FlowDirection direction)> bidiBreaks,
		List<(int end, FontDetails fontDetails)> fontBreaks)
	{
		var shapingRuns = new List<(int start, int end, FontDetails fontDetails, FlowDirection direction)>();

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
				shapingRuns.Add((start, nextBreak, fontBreaks[currentFontBreakIndex].fontDetails, bidiBreaks[currentBidiBreakIndex].direction));
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
		IEnumerable<TextHighlighter> highlighters)
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
		List<(SKPath path, float strokeThickness)> spellCheckUnderlines = new();

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
			var y = _xyTable[lineIndex].prefixSummedHeight - line.lineHeight;
			var unalignedX = cluster.Value.indexInLine == 0
				? 0
				: _xyTable[lineIndex].prefixSummedWidths[cluster.Value.indexInLine - 1].sumUntilAfterCluster;
			var alignmentOffset = GetAlignmentOffsetForLine(line);
			var positionAcc = new SKPoint(unalignedX + alignmentOffset, y + line.baselineOffset);
			var fontDetails = cluster.Value.fontDetails;

			if (!cluster.Value.containsTab)
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
					positionAcc.X += AdvanceToPixels(glyph.GlyphPosition.XAdvance, fontDetails);
					if (cluster.Value.glyphLast == glyphNode)
					{
						break;
					}
				}
			}

			var backgroundRect = new SKRect(unalignedX + alignmentOffset, y, unalignedX + alignmentOffset + cluster.Value.width, y + line.lineHeight);
			highlighter.Value.background?.Paint(session.Canvas, session.Opacity, backgroundRect);

			if (_corrections?[wordBoundariesIndex] is { } correction)
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
					while (x + step < underlineRightX)
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

		foreach (var (path, strokeThickness) in spellCheckUnderlines)
		{
			_spareSpellCheckPaint.StrokeWidth = strokeThickness;
			session.Canvas.DrawPath(path, _spareSpellCheckPaint);
		}

		if (caretRect is null && caret?.index == _text.Length) // ending new line or empty text
		{
			var alignmentOffset = GetAlignmentOffsetForLine(null);
			var top = _text.Length == 0 ? 0 : _xyTable[^1].prefixSummedHeight;
			caretRect = _rtl
				? new SKRect(alignmentOffset - caret.Value.thickness, top, alignmentOffset, top + _defaultFontDetails.LineHeight)
				: new SKRect(alignmentOffset, top, alignmentOffset + caret.Value.thickness, top + _defaultFontDetails.LineHeight);
		}

		if (caretRect is not null)
		{
			caret!.Value.brush.Paint(session.Canvas, session.Opacity, caretRect.Value);
		}
	}

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
			return new Rect(alignmentOffset, 0, 0, _defaultFontDetails.LineHeight);
		}

		if (index == _text.Length)
		{
			var (alignmentOffset, lineWidth, height, y) = _endingNewLineLineHeight is { } endingNewLineLineHeight
				? (GetAlignmentOffsetForLine(null), 0, endingNewLineLineHeight, _xyTable[^1].prefixSummedHeight)
				: (GetAlignmentOffsetForLine(_lines[^1]), _lines[^1].width, _lines[^1].lineHeight, _xyTable[^1].prefixSummedHeight - _lines[^1].lineHeight);
			return _rtl ?
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
			var y = lineIndex == 0 ? 0 : _xyTable[lineIndex].prefixSummedHeight - line.lineHeight;
			var unalignedX = indexInLine == 0 ? (_rtl ? line.width : 0) : _xyTable[lineIndex].prefixSummedWidths[indexInLine - 1].sumUntilAfterCluster;
			return new Rect(alignmentOffset + unalignedX, y, cluster.Value.width, line.lineHeight);
		}
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

		if (p.Y >= _xyTable[^1].prefixSummedHeight + (_endingNewLineLineHeight ?? 0))
		{
			if (!extendedSelection)
			{
				return -1;
			}
			if (_rtl && p.X > GetAlignmentOffsetForLine(_lines[^1]) + _lines[^1].width || !_rtl && p.X < GetAlignmentOffsetForLine(_lines[^1]))
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
				? (_rtl ? line.end - (ignoreEndingNewLine ? TrailingCRLFInLine(line) : 0) : line.start)
				: -1;
		}

		if (p.X >= alignmentOffset + line.width)
		{
			return extendedSelection
				? (_rtl ? line.start : line.end - (ignoreEndingNewLine ? TrailingCRLFInLine(line) : 0))
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
			if (right)
			{
				return (_text.Length, 0);
			}
			else if (_wordBoundaries.Count == 1)
			{
				return (0, _text.Length);
			}
			else
			{
				return (_wordBoundaries[^2], _text.Length);
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
		}
		throw new UnreachableException();
	}

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
	{
		if (index == 0 || _text.Length == 0 || _lines.Count == 0)
		{
			return (0, 0, true, _lines.Count == 1 || (_lines.Count == 0 && _endingNewLineLineHeight is not null), 0);
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
		return line is null ? TrailingCRLFCount(_text, _text.Length) : TrailingCRLFCount(_text, line.Value.end);
	}

	private float GetAlignmentOffsetForLine(Line? line)
	{
		var (lineWidth, lineWidthWithoutTrailingSpaces) = line is null ? (0, 0) : (line.Value.width, line.Value.widthWithoutTrailingSpaces);
		var totalWidth = (float)_availableSize.Width;

		float alignmentOffset;
		// Trailing whitespace at the end of a line is assigned the paragraph embedding level
		// So the trailing space is always on the left if RTL and always on the right if LTR
		if (_rtl)
		{
			alignmentOffset = _textAlignment switch
			{
				TextAlignment.Center when lineWidthWithoutTrailingSpaces <= totalWidth => (totalWidth - lineWidthWithoutTrailingSpaces) / 2,
				TextAlignment.Right when lineWidthWithoutTrailingSpaces <= totalWidth => totalWidth - lineWidth,
				_ => 0
			};
			alignmentOffset = Math.Min(alignmentOffset, totalWidth - lineWidth);
		}
		else
		{
			alignmentOffset = _textAlignment switch
			{
				TextAlignment.Center when lineWidthWithoutTrailingSpaces <= totalWidth => (totalWidth - lineWidthWithoutTrailingSpaces) / 2,
				TextAlignment.Right when lineWidthWithoutTrailingSpaces <= totalWidth => totalWidth - lineWidthWithoutTrailingSpaces,
				_ => 0
			};
			alignmentOffset = Math.Max(alignmentOffset, 0);
		}

		return alignmentOffset;
	}

	private static FontDetails? GetFallbackFont(int codepoint, float fontSize, FontWeight fontWeight, FontStretch fontStretch, FontStyle fontStyle, IFontCacheUpdateListener fontListener)
	{
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

		var fallbackFontTask = FontDetailsCache.GetFontForCodepoint(codepoint, fontSize, fontWeight, fontStretch, fontStyle);
		if (fallbackFontTask.IsCompleted)
		{
			if (fallbackFontTask.IsCompletedSuccessfully
				&& fallbackFontTask.Result is { } fallbackFont)
			{
				return fallbackFont;
			}
		}
		else
		{
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
		}

		if (SKFontManager.Default.MatchCharacter(codepoint) is { } typeface)
		{
			return FontDetails.Create(typeface, fontSize);
		}

		return null;
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

	private static int TrailingCRLFCount(string str, int end)
	{
		if (str is [.., '\r', '\n'])
		{
			return 2;
		}
		else if (str[end - 1] == '\r' || str[end - 1] == '\n')
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}

	private static bool IsLineBreak(string text, int indexAfterLineBreakOpportunity)
	{
		// https://www.unicode.org/standard/reports/tr13/tr13-5.html

		switch (text[indexAfterLineBreakOpportunity - 1])
		{
			case '\u000A':
			case '\u000B':
			case '\u000C':
			case '\u000D':
			case '\u0085':
			case '\u2028':
			case '\u2029': // Paragraph separator (should apply paragraph formatting, i.e. paragraph spacing/indentation on new line, unlike other line
						   // breaks - could matter if/when Paragraph.TextIndent/RichTextBlock.TextIndent is implemented (UWP/WinUI conformance to this
						   // behavior was not tested).
				return true;
			default:
				return false;
		}
	}
}
