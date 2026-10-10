#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Uno.Buffers;
using Uno.Disposables;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI;
using Uno.UI.Dispatching;
using FontWeights = Microsoft.UI.Text.FontWeights;


namespace Microsoft.UI.Xaml.Documents;

internal readonly record struct RichEditSpellingAnnotationInfo(
	int Start,
	int End,
	string Text,
	IReadOnlyList<string> Suggestions);

internal readonly partial struct UnicodeText : IParsedText
{
	// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
	private const float FallbackTabStopWidth = 48;
	private const byte UBIDI_DEFAULT_LTR = 0xfe;
	private const byte UBIDI_DEFAULT_RTL = 0xff;
	private const int UBIDI_LTR = 0;
	private const int UBIDI_RTL = 1;
	private const string HorizontalEllipsis = "\u2026";
	// Unicode rules P2-P3: the first strong character sets the paragraph level, otherwise the flow direction does.
	internal static bool IsRightToLeftParagraph(string text, FlowDirection flowDirection)
	{
		if (text.Length == 0)
		{
			return flowDirection is FlowDirection.RightToLeft;
		}

		using var _ = ICU.CreateBiDiAndSetPara(text, 0, text.Length, flowDirection is FlowDirection.RightToLeft ? UBIDI_DEFAULT_RTL : UBIDI_DEFAULT_LTR, out var bidi);
		return ICU.GetMethod<ICU.ubidi_getParaLevel>()(bidi) is UBIDI_RTL;
	}

	// Fallbacks for fonts that don't publish underline/strikeout metrics, as a fraction of the em
	// size so they track the font size. They approximate what real fonts publish (measured on
	// Segoe UI/Arial/Times New Roman/Consolas: ~0.05 em thickness, ~0.1 em under and ~0.25 em over
	// the baseline). Positions are the top edge of the line, positive downwards.
	private const float FallbackDecorationThicknessRatio = 1f / 20f;
	private const float FallbackUnderlinePositionRatio = 1f / 10f;
	private const float FallbackStrikethroughPositionRatio = -1f / 4f;

	internal interface IFontCacheUpdateListener
	{
		void Invalidate();
	}

	internal sealed class ShapingCache
	{
		private const int MaxEntries = 512;
		private const int MaxGlyphs = 64 * 1024;
		private readonly Dictionary<ShapingCacheKey, List<ShapingCacheEntry>> _entries = new();
		private int _entryCount;
		private int _glyphCount;

		internal long ShapeOperationCount { get; private set; }

		internal bool TryGet(
			ReadOnlySpan<char> text,
			ShapingCacheKey key,
			out GlyphRun run)
		{
			if (_entries.TryGetValue(key, out var candidates))
			{
				foreach (var candidate in candidates)
				{
					if (text.SequenceEqual(candidate.Text))
					{
						run = candidate.Run;
						return true;
					}
				}
			}

			run = default;
			return false;
		}

		internal void Add(ReadOnlySpan<char> text, ShapingCacheKey key, GlyphRun run)
		{
			if (_entryCount >= MaxEntries || _glyphCount + run.Count > MaxGlyphs)
			{
				return;
			}

			if (!_entries.TryGetValue(key, out var candidates))
			{
				candidates = new List<ShapingCacheEntry>();
				_entries.Add(key, candidates);
			}
			candidates.Add(new ShapingCacheEntry(text.ToString(), run));
			_entryCount++;
			_glyphCount += run.Count;
		}

		internal void RecordShapeOperation() => ShapeOperationCount++;
	}

	internal readonly record struct ShapingCacheKey(
		int TextHash,
		int TextLength,
		FontDetails FontDetails,
		TextDirection Direction,
		ShapingOptions Options);

	internal sealed record ShapingCacheEntry(string Text, GlyphRun Run);

	// Per-run formatting shared by every cluster of the run. RichEditBox-only state sits behind one reference, null for
	// plain TextBlock/TextBox runs.
	private readonly record struct RunBreak(int end, Brush? foreground, FlowDirection direction, TextDecorations decorations, float characterSpacing, RichRunState? rich)
	{
		public global::Windows.UI.Color? background => rich?.Background;
		public bool hidden => rich?.Hidden ?? false;
		public global::Microsoft.UI.Text.UnderlineType? underlineType => rich?.UnderlineType;
		public float? kerningThreshold => rich?.KerningThreshold;
		public string? languageTag => rich?.LanguageTag;
		public global::Microsoft.UI.Text.TextScript textScript => rich?.TextScript ?? global::Microsoft.UI.Text.TextScript.Default;
		public bool smallCaps => rich?.SmallCaps ?? false;
		public float baselineOffset => rich?.BaselineOffset ?? 0;
		public bool outline => rich?.Outline ?? false;
	}

	private sealed record RichRunState(
		global::Windows.UI.Color? Background,
		bool Hidden,
		global::Microsoft.UI.Text.UnderlineType? UnderlineType,
		float? KerningThreshold,
		string? LanguageTag,
		global::Microsoft.UI.Text.TextScript TextScript,
		bool SmallCaps,
		float BaselineOffset,
		bool Outline);

	private record struct Line(int start, int end, LinkedListNode<Cluster> clusterStart, LinkedListNode<Cluster> clusterLast, float width, float widthWithoutTrailingSpaces, float lineHeight, float baselineOffset, TextAlignment? textAlignment = null, bool hasEllipsis = false, ParagraphLayoutInfo? paragraphLayout = null, bool isFirstLineOfParagraph = false, bool isLastLineOfParagraph = false);
	private readonly record struct TextDecorationDrawInfo(float X1, float X2, float Y, float Thickness, float FontSize, Color Color, global::Microsoft.UI.Text.UnderlineType Style);

	// Positioned glyph in pixel space (offsets/advance already scaled by IFont.Shape).
	private record struct Glyph(ushort GlyphId, float XAdvance, float XOffset, float YOffset);

	// Run formatting (spacing, hidden, baseline offset, outline) is reached through runIndex rather than copied into
	// every cluster, mirroring WinUI where an LsRun points at its shared TextRunProperties. -1 means no run (ellipsis).
	private record struct Cluster(
		int start,
		int end,
		LinkedListNode<Glyph> glyphStart,
		LinkedListNode<Glyph> glyphLast,
		FontDetails fontDetails,
		float width,
		int runIndex,
		bool containsOnlyWhitespace,
		bool containsTab,
		bool rtl,
		int lineIndex,
		int indexInLine)
	{
		public static Cluster Create(
			string _text,
			int indexStart,
			int indexEnd,
			LinkedListNode<Glyph> glyphsStart,
			LinkedListNode<Glyph> glyphsLast,
			FontDetails fontDetails,
			int runIndex,
			float characterSpacing,
			bool hidden)
		{
			var clusterContainsTab = false;
			var clusterContainsOnlyWhitespace = true;
			for (int i = indexStart; i < indexEnd; i++)
			{
				clusterContainsTab |= _text[i] == '\t';
				clusterContainsOnlyWhitespace &= char.IsWhiteSpace(_text[i]);
			}

			var advance = GetGlyphAdvance(glyphsStart, glyphsLast);
			var clusterWidth = advance + GetEffectiveCharacterSpacing(characterSpacing, advance, clusterContainsTab, _text, indexStart);

			return new(indexStart, indexEnd, glyphsStart, glyphsLast, fontDetails, hidden ? 0 : clusterWidth, runIndex, clusterContainsOnlyWhitespace, clusterContainsTab, false, -1, -1);
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
	internal static ISpellCheckingService? SpellCheckingServiceOverrideForTesting { get; set; }
	private static ISpellCheckingService? SpellCheckingService
		=> SpellCheckingServiceOverrideForTesting ?? _spellCheckingService.Value;

	private static readonly Brush _blackBrush = new SolidColorBrush(Colors.Black);
	private static readonly Dictionary<int, HashSet<IFontCacheUpdateListener>> _codepointToListeners = new();
	private static readonly Dictionary<string, HashSet<IFontCacheUpdateListener>> _fontFamilyToListeners = new();
	private readonly string _text;
	private readonly List<(float prefixSummedHeight, List<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)> prefixSummedWidths, Line line)> _xyTable;
	private readonly List<(int start, int end, LinkedListNode<Cluster> cluster)> _indexToCluster;
	private readonly TextAlignment _textAlignment;
	private readonly FontDetails _defaultFontDetails;
	private readonly List<Line> _lines;
	private readonly float? _endingNewLineLineHeight;
	private readonly float _endingLineContentTop;
	private readonly float _endingLineBaselineOffset;
	private readonly ParagraphLayoutInfo? _endingParagraphLayout;
	// List markers shaped and measured during layout, reused by every paint.
	private readonly Dictionary<(IFont font, string text), MarkerShape>? _markerShapes;
	private readonly TextAlignment? _endingParagraphAlignment;
	private readonly Brush? _defaultForeground;
	private readonly bool _alignmentIncludesTrailingWhitespace;
	private readonly bool _rtl;
	private readonly List<(int start, int end, Hyperlink hyperlink)> _hyperlinkRanges;
	// Lazy: word boundaries feed selection/word-navigation/spell-check, never measure — computing them (an ICU
	// break-iterator pass) on every Text change is pure waste for labels that are only ever rendered.
	// (Boxed because this is a readonly struct.)
	private readonly global::System.Runtime.CompilerServices.StrongBox<List<int>?> _wordBoundaries = new();

	private List<int> WordBoundaries => _wordBoundaries.Value ??= _text.Length == 0 ? [] : GetWords(_text);
	private readonly List<LinkedListNode<Cluster>> _clustersInLogicalOrder;
	private readonly LinkedList<Glyph> _glyphs;
	private readonly List<(int end, FlowDirection direction)> _bidiBreaks;
	private readonly List<RunBreak> _runBreaks;
	private readonly List<(int correctionStart, int correctionEnd)?>? _corrections;
	// Keyed by the cluster's text index; null for plain text.
	private readonly Dictionary<int, InlineObjectInfo>? _inlineObjects;
	private readonly Dictionary<int, global::Microsoft.UI.Text.TabLeader>? _tabLeaders;
	private readonly Size _availableSize;
	private readonly bool _layoutUsesAvailableWidth;
	private readonly bool _layoutUsesAvailableHeight;

	internal unsafe UnicodeText(
		Size availableSize,
		IEnumerable<Inline> inlines, // only leaf nodes; custom layouts may stream a bounded reusable inline
		FontDetails defaultFontDetails, // only used for a final empty line, otherwise the FontDetails are read from the inline
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		TextLineBounds textLineBounds,
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
		bool alignmentIncludesTrailingWhitespace,
		bool ignoreTrailingCharacterSpacing,
		out Size calculatedSize,
		ShapingCache? shapingCache = null,
		bool suppressEndingNewLineLine = false)
	{
		CI.Assert(maxLines >= 0);
		_endingParagraphLayout = endingParagraphLayout;
		_endingParagraphAlignment = endingParagraphAlignment;
		_defaultForeground = defaultForeground;
		_alignmentIncludesTrailingWhitespace = alignmentIncludesTrailingWhitespace;
		var tabStopWidth = defaultTabStop > 0 ? defaultTabStop : FallbackTabStopWidth;

		var stringBuilder = new StringBuilder();
		_hyperlinkRanges = new List<(int start, int end, Hyperlink hyperlink)>();
		_runBreaks = new List<RunBreak>();
		var scriptBreaks = new List<int>();
		var fontBreaks = new List<(int end, FontDetails fontDetails)>();
		var lineOpportunityBreaks = new List<int>();
		var allAscii = true;
		var allRunsLtr = true;
		string? singleInlineText = null;
		var nonEmptyInlines = 0;
		Dictionary<int, InlineObjectInfo>? inlineObjects = null;
		List<(int end, TextAlignment alignment)>? paragraphAlignments = null;
		List<(int end, ParagraphLayoutInfo layout)>? paragraphLayouts = null;
		var hasBaselineOffsets = false;

		foreach (var inline in inlines)
		{
			var inlineText = inline.GetText();
			if (string.IsNullOrEmpty(inlineText))
			{
				continue;
			}

			singleInlineText = ++nonEmptyInlines == 1 ? inlineText : null;
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
			for (int i = 0, codepointLength; i < inlineText.Length; i += codepointLength)
			{
				FontDetails newFontDetails;
				var codepoint = ReadCodepoint(inlineText, i, out codepointLength);

				// ASCII shortcut: the whole ASCII range is Latin letters (USCRIPT_LATIN=25) or Script=Common (0),
				// so the per-character ICU P/Invoke — a dominant cost of re-laying-out short labels — is skippable.
				int newScript;
				if (codepoint < 0x80)
				{
					newScript = char.IsAsciiLetter((char)codepoint) ? 25 : 0;
				}
				else
				{
					allAscii = false;
					newScript = ICU.GetMethod<ICU.uscript_getScript>()(codepoint, out var errorCode);
					ICU.CheckErrorCode<ICU.uscript_getScript>(errorCode);
				}

				if (newScript != currentScript)
				{
					currentScript = newScript;
					if (i != 0)
					{
						scriptBreaks.Add(inlineStart + i);
					}
				}

				if (!inline.FontInfo.FontHandle.ContainsGlyph(codepoint))
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
			var characterSpacing = (float)inline.FontSize * inline.CharacterSpacing / 1000;
			var run = inline as Run;
			var runDirection = run?.FlowDirection ?? flowDirection;
			allRunsLtr &= runDirection is FlowDirection.LeftToRight;
			RichRunState? richState = null;
			if (run is { HasRichFormat: true })
			{
				// Snapshot: a streamed RichEditBox layout reuses one Run across paragraphs.
				richState = new RichRunState(
					run.CharacterBackground,
					run.IsHidden,
					run.RichEditUnderlineType,
					run.RichEditKerningThreshold,
					run.RichEditLanguageTag,
					run.RichEditTextScript,
					run.RichEditSmallCaps,
					run.RichEditBaselineOffset,
					run.RichEditOutline);
				hasBaselineOffsets |= run.RichEditBaselineOffset != 0;
			}
			_runBreaks.Add(new RunBreak(
				inlineStart + inlineText.Length,
				inline.Foreground,
				runDirection,
				inline.TextDecorations,
				characterSpacing,
				richState));
			fontBreaks.Add((inlineStart + inlineText.Length, currentFontDetails));

			if (TryGetHyperLink(inline) is { } hyperLink)
			{
				_hyperlinkRanges.Add((inlineStart, inlineStart + inlineText.Length, hyperLink));
			}
		}

		_text = singleInlineText ?? stringBuilder.ToString();
		if (_text.Length == 0)
		{
			_lines = [];
			_defaultFontDetails = defaultFontDetails;
			_rtl = flowDirection is FlowDirection.RightToLeft;
			_textAlignment = textAlignment ?? (flowDirection is FlowDirection.RightToLeft ? TextAlignment.Right : TextAlignment.Left);
			var (naturalHeight, naturalBaseline) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, defaultFontDetails, false, true);
			_endingNewLineLineHeight = endingParagraphLayout is null
				? naturalHeight
				: ApplyLineSpacingRule(naturalHeight, endingParagraphLayout);
			_endingLineContentTop = endingParagraphLayout?.SpaceBefore ?? 0;
			_endingLineBaselineOffset = naturalBaseline + (_endingNewLineLineHeight.Value - naturalHeight) / 2;
			calculatedSize = new Size(
				GetParagraphLeftInset(endingParagraphLayout, firstLine: true) + GetParagraphRightInset(endingParagraphLayout, firstLine: true),
				_endingLineContentTop + _endingNewLineLineHeight.Value + (endingParagraphLayout?.SpaceAfter ?? 0));
			_availableSize = availableSize;
			_layoutUsesAvailableWidth = _rtl // caret placement
				|| _textAlignment is TextAlignment.Center or TextAlignment.Right or TextAlignment.Justify
				|| endingParagraphAlignment is not null
				|| endingParagraphLayout is not null;
			_xyTable = [];
			_indexToCluster = [];
			_inlineObjects = null;
			_tabLeaders = null;
			_clustersInLogicalOrder = [];
			_glyphs = [];
			_bidiBreaks = [];
			return;
		}

		// Line breaking is a document-level operation. Computing boundaries per inline can split a
		// CRLF pair when character formatting changes between its two code units.
		// NoWrap ASCII text without mandatory breaks never consults intermediate line-break opportunities
		// (no wrapping decisions, single line), so the ICU pass is skipped. Word trimming picks its ellipsis
		// position from the same list, so it still needs the real boundaries.
		if (allAscii && textWrapping is TextWrapping.NoWrap && textTrimming is not TextTrimming.WordEllipsis && IsAsciiWithoutLineBreaks(_text))
		{
			lineOpportunityBreaks.Add(_text.Length);
		}
		else
		{
			AppendBoundaries(/* Line */ 2, _text, 0, lineOpportunityBreaks);
		}

		_bidiBreaks = new List<(int end, FlowDirection direction)>();
		var trivialLtr = allAscii && allRunsLtr && flowDirection is FlowDirection.LeftToRight && paragraphLayouts is null;
		// The paragraph BiDi handle must outlive the per-line reordering below; boxed so the deferred close
		// sees the handle assigned in the non-trivial branch (it stays default — no close — on the trivial path).
		var bidiBox = new global::System.Runtime.CompilerServices.StrongBox<IntPtr>();
		using var bidiDisposable = new DisposableStruct<global::System.Runtime.CompilerServices.StrongBox<IntPtr>>(
			static box => { if (box.Value != default) { ICU.GetMethod<ICU.ubidi_close>()(box.Value); } },
			bidiBox);
		IntPtr bidi = default;
		if (trivialLtr)
		{
			// ASCII has no RTL characters, so with an LTR paragraph and LTR runs the BiDi outcome is a single
			// LTR run — skip the ICU BiDi pass entirely (a real cost when short labels re-layout every frame).
			_rtl = false;
			_bidiBreaks.Add((_text.Length, FlowDirection.LeftToRight));
			textAlignment ??= TextAlignment.Left;
		}
		else
		{
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

			ICU.CreateBiDiAndSetPara(_text, 0, _text.Length, flowDirection is FlowDirection.RightToLeft ? UBIDI_DEFAULT_RTL : UBIDI_DEFAULT_LTR, out bidi, embeddingLevels);
			bidiBox.Value = bidi;
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
		}

		_glyphs = new LinkedList<Glyph>();
		var clusterBreaks = new LinkedList<Cluster>();
		foreach (var shapingRun in new ShapingRunEnumerator(_runBreaks, scriptBreaks, _bidiBreaks, fontBreaks))
		{
			var direction = shapingRun.direction is FlowDirection.RightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;
			var runText = _text.AsSpan(shapingRun.start, shapingRun.end - shapingRun.start);
			ref readonly var run = ref CollectionsMarshal.AsSpan(_runBreaks)[shapingRun.runIndex];
			var shapingOptions = GetShapingOptions(shapingRun.fontDetails, run.kerningThreshold, run.languageTag, run.textScript, run.smallCaps);
			GlyphRun glyphRun;
			if (shapingCache is null)
			{
				glyphRun = shapingRun.fontDetails.FontHandle.Shape(runText, direction, shapingOptions);
			}
			else
			{
				var cacheKey = new ShapingCacheKey(GetOrdinalHash(runText), runText.Length, shapingRun.fontDetails, direction, shapingOptions);
				if (!shapingCache.TryGet(runText, cacheKey, out glyphRun))
				{
					glyphRun = shapingRun.fontDetails.FontHandle.Shape(runText, direction, shapingOptions);
					shapingCache.RecordShapeOperation();
					shapingCache.Add(runText, cacheKey, glyphRun);
				}
			}
			var clusters = glyphRun.Clusters;
			var count = glyphRun.Count;

			if (count == 0)
			{
				// Even though textRun is nonempty and fontDetails contains a font that can shape all the characters in it,
				// shaping may still decide to yield 0 glyphs.
				_glyphs.AddLast(new Glyph(0, 0, 0, 0));
			}
			else
			{
				CI.Assert((direction is TextDirection.LeftToRight && clusters[0] == 0) || (direction is TextDirection.RightToLeft && clusters[^1] == 0));
				if (direction is TextDirection.LeftToRight)
				{
					for (var index = 0; index < count; index++)
					{
						if (index > 0 && clusters[index] != clusters[index - 1])
						{
							clusterBreaks.AddLast(Cluster.Create(
								_text,
								clusterBreaks.Last?.Value.end ?? 0,
								shapingRun.start + clusters[index],
								clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
								_glyphs.Last!,
								shapingRun.fontDetails,
								shapingRun.runIndex,
								run.characterSpacing,
								run.hidden));
						}
						_glyphs.AddLast(new Glyph(glyphRun.Glyphs[index], glyphRun.Advances[index], glyphRun.Offsets[index].X, glyphRun.Offsets[index].Y));
					}
				}
				else
				{
					for (var index = count - 1; index >= 0; index--)
					{
						if (index < count - 1 && clusters[index] != clusters[index + 1])
						{
							clusterBreaks.AddLast(Cluster.Create(
								_text,
								clusterBreaks.Last?.Value.end ?? 0,
								shapingRun.start + clusters[index],
								clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
								_glyphs.Last!,
								shapingRun.fontDetails,
								shapingRun.runIndex,
								run.characterSpacing,
								run.hidden));
						}
						_glyphs.AddLast(new Glyph(glyphRun.Glyphs[index], glyphRun.Advances[index], glyphRun.Offsets[index].X, glyphRun.Offsets[index].Y));
					}
				}

				clusterBreaks.AddLast(Cluster.Create(
					_text,
					clusterBreaks.Last?.Value.end ?? 0,
					shapingRun.end,
					clusterBreaks.Last?.Value.glyphLast?.Next ?? _glyphs.First!,
					_glyphs.Last!,
					shapingRun.fontDetails,
					shapingRun.runIndex,
					run.characterSpacing,
					run.hidden));
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
					};
				}
			}
		}

		Dictionary<int, global::Microsoft.UI.Text.TabLeader>? tabLeaders = null;
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
			(float width, global::Microsoft.UI.Text.TabLeader leader) GetTabMetrics(float currentX, LinkedListNode<Cluster> tabCluster)
			{
				var layout = paragraphLayouts is not null && paragraphLayouts.Count > 0
					? paragraphLayouts[paragraphLayoutIndex].layout
					: null;
				if (layout?.Tabs.Length > 0)
				{
					float? fullFieldWidth = null;
					float? decimalFieldWidth = null;
					foreach (var tab in layout.Tabs)
					{
						var fieldWidth = tab.Alignment switch
						{
							global::Microsoft.UI.Text.TabAlignment.Center => (fullFieldWidth ??= MeasureFollowingTabField(tabCluster, stopAtDecimal: false)) / 2,
							global::Microsoft.UI.Text.TabAlignment.Right => fullFieldWidth ??= MeasureFollowingTabField(tabCluster, stopAtDecimal: false),
							global::Microsoft.UI.Text.TabAlignment.Decimal => decimalFieldWidth ??= MeasureFollowingTabField(tabCluster, stopAtDecimal: true),
							_ => 0,
						};
						var width = tab.Position - currentX - fieldWidth;
						if (width > 0)
						{
							return (width, tab.Leader);
						}
					}
				}

				return (((int)(currentX / tabStopWidth) + 1) * tabStopWidth - currentX, global::Microsoft.UI.Text.TabLeader.Spaces);
			}

			float MeasureFollowingTabField(LinkedListNode<Cluster> tabCluster, bool stopAtDecimal)
			{
				var width = 0f;
				var count = 0;
				// Bound lookahead so malformed or very large documents cannot turn one tab into an
				// unbounded layout scan. Bar tabs use left-tab placement because UnicodeText has no
				// independent paragraph-rule primitive.
				for (var node = tabCluster.Next; node is not null && count++ < 4096; node = node.Next)
				{
					if (node.Value.containsTab || IsLineBreak(text, node.Value.end))
					{
						break;
					}
					if (stopAtDecimal)
					{
						var clusterText = text.AsSpan(node.Value.start, node.Value.end - node.Value.start);
						if (clusterText.IndexOfAny('.', ',') >= 0)
						{
							break;
						}
					}
					width += node.Value.width;
				}
				return width;
			}
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

					var tabMetrics = currentClusterBreak.Value.containsTab
						? GetTabMetrics(lineWidth + chunkUnderTestWidth, currentClusterBreak)
						: default;
					var clusterWidth = currentClusterBreak.Value.containsTab
						? tabMetrics.width
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
							var (currentLineH, currentLineB) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine!, lines.Count == 0, false);
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
								if (tabMetrics.leader != global::Microsoft.UI.Text.TabLeader.Spaces)
								{
									(tabLeaders ??= new())[currentClusterBreak.Value.start] = tabMetrics.leader;
								}
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

						var (h, b) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, fontDetails, lines.Count == 0, false);
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
								var (h, b) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine!, lines.Count == 0, false);
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
									tabMetrics = GetTabMetrics(chunkUnderTestWidth, currentClusterBreak);
									clusterWidth = tabMetrics.width;
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
							if (tabMetrics.leader != global::Microsoft.UI.Text.TabLeader.Spaces)
							{
								(tabLeaders ??= new())[currentClusterBreak.Value.start] = tabMetrics.leader;
							}
						}

						if (IsLineBreak(_text, currentClusterBreak.Value.end))
						{
							var (h, b) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine, lines.Count == 0, false);
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
							var (h, b) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, maxHeightFontDetailsInCurrentLine ?? defaultFontDetails, lines.Count == 0, true);
							lines.Add(new Line(lines.Count == 0 ? 0 : lines[^1].end, currentLineEnd, lines.Count == 0 ? clusterBreaks.First! : lines[^1].clusterLast.Next!, currentLineClusterLast!, lineWidth, lineWidthWithoutTrailingSpaces, h, b));
						}
					}

					currentClusterBreak = currentClusterBreak.Next;
				}
			}
		}

		if (hasBaselineOffsets)
		{
			AdjustLinesForBaselineOffsets(lines, _runBreaks);
		}
		if (inlineObjects is not null)
		{
			AdjustLinesForInlineObjects(lines, inlineObjects);
		}
		if (paragraphAlignments is not null)
		{
			ApplyParagraphAlignments(lines, paragraphAlignments);
		}
		if (paragraphLayouts is not null)
		{
			ApplyParagraphLayouts(lines, paragraphLayouts, _text);
		}
		if (ignoreTrailingCharacterSpacing)
		{
			RemoveTrailingCharacterSpacing(lines, _text, _runBreaks, inlineObjects);
		}
		ApplyParagraphJustification(lines, _text, (float)availableSize.Width, textAlignment!.Value);

		var textEndsInLineBreak = IsLineBreak(_text, _text.Length);
		// Lines that do not fit the available height are dropped below, so the height only matters past one line.
		_layoutUsesAvailableHeight = lines.Count + (textEndsInLineBreak ? 1 : 0) > 1;
		var (terminalNaturalHeight, terminalNaturalBaseline) = GetLineHeightAndBaselineOffset(textLineBounds, lineStackingStrategy, lineHeight, defaultFontDetails, false, true);
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

					var trimFontDetails = trimPoint.Value.fontDetails;
					if (!trimFontDetails.FontHandle.ContainsGlyph(HorizontalEllipsis[0]))
					{
						trimFontDetails = GetFallbackFont(HorizontalEllipsis[0], trimFontDetails.FontSize, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, fontListener) ?? trimFontDetails;
					}
					// This can be cached and reused across trim points, but it's not expected to be a hotspot.
					var trimGlyphRun = trimFontDetails.FontHandle.Shape(HorizontalEllipsis, TextDirection.LeftToRight);
					float ellipsisWidth = 0;
					for (var i = 0; i < trimGlyphRun.Count; i++)
					{
						ellipsisWidth += trimGlyphRun.Advances[i];
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
						for (var i = 0; i < trimGlyphRun.Count; i++)
						{
							ellipsisGlyphList.AddLast(new Glyph(trimGlyphRun.Glyphs[i], trimGlyphRun.Advances[i], trimGlyphRun.Offsets[i].X, trimGlyphRun.Offsets[i].Y));
						}

						var ellipsisCluster = new Cluster(
							trimPoint.Value.end,
							isEarlyLastLine ? _text.Length : line.end,
							ellipsisGlyphList.First!,
							ellipsisGlyphList.Last!,
							trimFontDetails,
							ellipsisWidth,
							-1,
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

		var hasEndingParagraphLine = !suppressEndingNewLineLine
			&& !endedEarly
			&& lines[^1].end == _text.Length
			&& textEndsInLineBreak;
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
		Dictionary<(IFont font, string text), MarkerShape>? markerShapes = null;
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
				var markerBounds = GetMarkerShape(ref markerShapes, line.clusterStart.Value.fontDetails.FontHandle, markerLayout.MarkerText).Ink;
				var markerAnchor = GetParagraphMarkerAnchor(markerLayout, (float)availableSize.Width);
				var markerRight = markerLayout.MarkerAlignment switch
				{
					global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor + markerBounds.Width,
					global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor + markerBounds.Width / 2,
					_ => markerAnchor,
				};
				measuredLineRight = Math.Max(measuredLineRight, (float)markerRight);
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
				var markerBounds = GetMarkerShape(ref markerShapes, defaultFontDetails.FontHandle, endingMarkerLayout.MarkerText).Ink;
				var markerAnchor = GetParagraphMarkerAnchor(endingMarkerLayout, (float)availableSize.Width);
				var markerRight = endingMarkerLayout.MarkerAlignment switch
				{
					global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor + markerBounds.Width,
					global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor + markerBounds.Width / 2,
					_ => markerAnchor,
				};
				endingWidth = Math.Max(endingWidth, (float)markerRight);
			}
			maxLineWidth = Math.Max(maxLineWidth, endingWidth);
		}

		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];

			if (trivialLtr)
			{
				// Pure-LTR line: visual order == logical order, so the ICU line-BiDi reordering is an identity
				// relink — only the per-cluster line metadata needs assigning.
				var trivialIndex = 0;
				for (var (clusterNode, end) = (line.clusterStart, line.clusterLast.Next); clusterNode != end; clusterNode = clusterNode.Next!)
				{
					clusterNode!.Value = clusterNode.Value with { rtl = false, indexInLine = trivialIndex++ };
				}
				continue;
			}
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

		_xyTable = new List<(float prefixSummedHeight, List<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)> prefixSummedWidths, Line line)>(lines.Count);
		float prefixSummedHeight = 0;
		for (var lineIdx = 0; lineIdx < lines.Count; lineIdx++)
		{
			var line = lines[lineIdx];
			prefixSummedHeight += GetLineBlockHeight(line);

			// Sized exactly and holding node references (not cluster copies): this table is built for every line of
			// every TextBlock layout, so growth reallocations and duplicated clusters dominated its cost.
			var clusterCount = 0;
			for (var (node, end) = (line.clusterStart, line.clusterLast.Next); node != end; node = node.Next!)
			{
				clusterCount++;
			}

			var prefixSummedWidths = new List<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)>(clusterCount);
			float sumUntilAfterCluster = 0;
			for (var (node, end) = (line.clusterStart, line.clusterLast.Next); node != end; node = node.Next!)
			{
				sumUntilAfterCluster += node.Value.width;
				prefixSummedWidths.Add((sumUntilAfterCluster, node));
			}

			_xyTable.Add((prefixSummedHeight, prefixSummedWidths, line));
		}

		_lines = lines;
		_markerShapes = markerShapes;
		_inlineObjects = inlineObjects;
		_tabLeaders = tabLeaders;
		_defaultFontDetails = defaultFontDetails;
		_textAlignment = textAlignment!.Value;
		_corrections = isSpellCheckEnabled ? SpellCheckingService?.SpellCheck(WordBoundaries, _text) : null;
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
		_layoutUsesAvailableWidth = textWrapping != TextWrapping.NoWrap
			|| textTrimming != TextTrimming.None
			|| _rtl
			|| _textAlignment is TextAlignment.Center or TextAlignment.Right or TextAlignment.Justify // see GetAlignmentOffsetForLine
			|| paragraphAlignments is not null
			|| paragraphLayouts is not null
			|| endingParagraphAlignment is not null
			|| endingParagraphLayout is not null;
	}

	/// <summary>
	/// Whether laying the same text out in <paramref name="availableSize"/> would give this exact result, because the
	/// dimensions that differ from the size this was parsed with are not ones the layout depends on.
	/// </summary>
	internal bool IsLayoutValidFor(Size availableSize)
		=> (!_layoutUsesAvailableWidth || availableSize.Width == _availableSize.Width)
		&& (!_layoutUsesAvailableHeight || availableSize.Height == _availableSize.Height);

	// Printable ASCII (plus tab) has no mandatory line breaks and needs no ICU break analysis for NoWrap text.
	private static bool IsAsciiWithoutLineBreaks(string text)
	{
		foreach (var c in text)
		{
			if ((c >= 0x7F || c < 0x20) && c != '\t')
			{
				return false;
			}
		}

		return true;
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

	// Walks the intersection of run, script, bidi and font breaks without materializing a list of shaping runs.
	private struct ShapingRunEnumerator
	{
		private readonly List<RunBreak> _runBreaks;
		private readonly List<int> _scriptBreaks;
		private readonly List<(int end, FlowDirection direction)> _bidiBreaks;
		private readonly List<(int end, FontDetails fontDetails)> _fontBreaks;
		private int _runBreakIndex;
		private int _scriptBreakIndex;
		private int _bidiBreakIndex;
		private int _fontBreakIndex;
		private int _start;

		public ShapingRunEnumerator(
			List<RunBreak> runBreaks,
			List<int> scriptBreaks,
			List<(int end, FlowDirection direction)> bidiBreaks,
			List<(int end, FontDetails fontDetails)> fontBreaks)
		{
			_runBreaks = runBreaks;
			_scriptBreaks = scriptBreaks;
			_bidiBreaks = bidiBreaks;
			_fontBreaks = fontBreaks;
			Current = default;
		}

		public (int start, int end, FontDetails fontDetails, FlowDirection direction, int runIndex) Current { get; private set; }

		public readonly ShapingRunEnumerator GetEnumerator() => this;

		public bool MoveNext()
		{
			while (_runBreakIndex < _runBreaks.Count)
			{
				var runIndex = _runBreakIndex;
				var nextRunBreak = _runBreaks[runIndex].end;
				var nextScriptBreak = _scriptBreaks[_scriptBreakIndex];
				var nextBidiBreak = _bidiBreaks[_bidiBreakIndex].end;
				var nextFontBreak = _fontBreaks[_fontBreakIndex].end;
				var nextBreak = Math.Min(Math.Min(nextRunBreak, nextScriptBreak), Math.Min(nextBidiBreak, nextFontBreak));
				var fontDetails = _fontBreaks[_fontBreakIndex].fontDetails;
				var direction = _bidiBreaks[_bidiBreakIndex].direction;

				if (nextBreak == nextRunBreak)
				{
					_runBreakIndex++;
				}
				if (nextBreak == nextScriptBreak)
				{
					_scriptBreakIndex++;
				}
				if (nextBreak == nextBidiBreak)
				{
					_bidiBreakIndex++;
				}
				if (nextBreak == nextFontBreak)
				{
					_fontBreakIndex++;
				}

				if (nextBreak > _start)
				{
					Current = (_start, nextBreak, fontDetails, direction, runIndex);
					_start = nextBreak;
					return true;
				}
			}

			return false;
		}
	}

	private static int GetOrdinalHash(ReadOnlySpan<char> text)
	{
		unchecked
		{
			var hash = (int)2166136261;
			foreach (var character in text)
			{
				hash = (hash ^ character) * 16777619;
			}
			return hash;
		}
	}

	// TOM kerning applies from a point-size threshold (0 = off); runs without one (TextBlock) keep the font's default kerning.
	private static ShapingOptions GetShapingOptions(FontDetails fontDetails, float? kerningThreshold, string? languageTag, global::Microsoft.UI.Text.TextScript textScript, bool smallCaps)
	{
		var fontSizeInPoints = fontDetails.FontSize * 72f / 96f;
		return new ShapingOptions
		{
			DisableKerning = kerningThreshold is { } threshold && (threshold <= 0 || fontSizeInPoints < threshold),
			SmallCaps = smallCaps,
			Language = string.IsNullOrEmpty(languageTag) ? null : languageTag,
			Script = GetScriptTag(textScript),
		};
	}

	private static string? GetScriptTag(global::Microsoft.UI.Text.TextScript textScript)
		=> textScript switch
		{
			global::Microsoft.UI.Text.TextScript.Ansi => "Latn",
			global::Microsoft.UI.Text.TextScript.EastEurope => "Latn",
			global::Microsoft.UI.Text.TextScript.Cyrillic => "Cyrl",
			global::Microsoft.UI.Text.TextScript.Greek => "Grek",
			global::Microsoft.UI.Text.TextScript.Turkish => "Latn",
			global::Microsoft.UI.Text.TextScript.Hebrew => "Hebr",
			global::Microsoft.UI.Text.TextScript.Arabic => "Arab",
			global::Microsoft.UI.Text.TextScript.Baltic => "Latn",
			global::Microsoft.UI.Text.TextScript.Vietnamese => "Latn",
			global::Microsoft.UI.Text.TextScript.Thai => "Thai",
			global::Microsoft.UI.Text.TextScript.ShiftJis => "Jpan",
			global::Microsoft.UI.Text.TextScript.GB2312 => "Hans",
			global::Microsoft.UI.Text.TextScript.Hangul => "Hang",
			global::Microsoft.UI.Text.TextScript.Big5 => "Hant",
			global::Microsoft.UI.Text.TextScript.Armenian => "Armn",
			global::Microsoft.UI.Text.TextScript.Syriac => "Syrc",
			global::Microsoft.UI.Text.TextScript.Thaana => "Thaa",
			global::Microsoft.UI.Text.TextScript.Devanagari => "Deva",
			global::Microsoft.UI.Text.TextScript.Bengali => "Beng",
			global::Microsoft.UI.Text.TextScript.Gurmukhi => "Guru",
			global::Microsoft.UI.Text.TextScript.Gujarati => "Gujr",
			global::Microsoft.UI.Text.TextScript.Oriya => "Orya",
			global::Microsoft.UI.Text.TextScript.Tamil => "Taml",
			global::Microsoft.UI.Text.TextScript.Telugu => "Telu",
			global::Microsoft.UI.Text.TextScript.Kannada => "Knda",
			global::Microsoft.UI.Text.TextScript.Malayalam => "Mlym",
			global::Microsoft.UI.Text.TextScript.Sinhala => "Sinh",
			global::Microsoft.UI.Text.TextScript.Lao => "Laoo",
			global::Microsoft.UI.Text.TextScript.Tibetan => "Tibt",
			global::Microsoft.UI.Text.TextScript.Myanmar => "Mymr",
			global::Microsoft.UI.Text.TextScript.Georgian => "Geor",
			global::Microsoft.UI.Text.TextScript.Jamo => "Hang",
			global::Microsoft.UI.Text.TextScript.Ethiopic => "Ethi",
			global::Microsoft.UI.Text.TextScript.Cherokee => "Cher",
			global::Microsoft.UI.Text.TextScript.Aboriginal => "Cans",
			global::Microsoft.UI.Text.TextScript.Ogham => "Ogam",
			global::Microsoft.UI.Text.TextScript.Runic => "Runr",
			global::Microsoft.UI.Text.TextScript.Khmer => "Khmr",
			global::Microsoft.UI.Text.TextScript.Mongolian => "Mong",
			global::Microsoft.UI.Text.TextScript.Braille => "Brai",
			global::Microsoft.UI.Text.TextScript.Yi => "Yiii",
			global::Microsoft.UI.Text.TextScript.Limbu => "Limb",
			global::Microsoft.UI.Text.TextScript.TaiLe => "Tale",
			global::Microsoft.UI.Text.TextScript.NewTaiLue => "Talu",
			global::Microsoft.UI.Text.TextScript.SylotiNagri => "Sylo",
			global::Microsoft.UI.Text.TextScript.Kharoshthi => "Khar",
			global::Microsoft.UI.Text.TextScript.Kayahli => "Kali",
			global::Microsoft.UI.Text.TextScript.Glagolitic => "Glag",
			global::Microsoft.UI.Text.TextScript.Lisu => "Lisu",
			global::Microsoft.UI.Text.TextScript.Vai => "Vaii",
			global::Microsoft.UI.Text.TextScript.NKo => "Nkoo",
			global::Microsoft.UI.Text.TextScript.Osmanya => "Osma",
			global::Microsoft.UI.Text.TextScript.PhagsPa => "Phag",
			global::Microsoft.UI.Text.TextScript.Gothic => "Goth",
			global::Microsoft.UI.Text.TextScript.Deseret => "Dsrt",
			global::Microsoft.UI.Text.TextScript.Tifinagh => "Tfng",
			_ => null,
		};

	// Wave metrics in units of fontSize / 12: half-period and half-height of the zigzag.
	// Sized so the wave band fits within the font's descent for typical fonts.
	private const float SpellCheckSquigglyStepScale = 3;
	private const float SpellCheckSquigglyAmplitudeScale = 1;

	/// <summary>
	/// Builds a single continuous spell-check zigzag from <paramref name="left"/> to <paramref name="right"/>.
	/// The wave phase is anchored at the left edge and the trailing partial half-wave is truncated by
	/// interpolation instead of snapping back to the midline, so the wave stays regular no matter where it ends.
	/// </summary>
	private static IGeometry BuildSpellCheckSquigglyPath(float midY, float left, float right, float scale)
	{
		var step = SpellCheckSquigglyStepScale * scale;
		var amplitude = SpellCheckSquigglyAmplitudeScale * scale;

		var path = GeometryFactory.Current.CreatePathBuilder();
		var x = left;
		var lastY = midY;
		path.MoveTo(new Vector2(x, lastY));
		var up = true;
		while (x + step < right)
		{
			x += step;
			lastY = midY + (up ? -amplitude : amplitude);
			path.LineTo(new Vector2(x, lastY));
			up = !up;
		}

		if (x < right)
		{
			var targetY = midY + (up ? -amplitude : amplitude);
			path.LineTo(new Vector2(right, lastY + (targetY - lastY) * ((right - x) / step)));
		}

		return path.Build();
	}

	// firstLine/lineCount exist for paragraphs broken across a page break. TextBlock never pages, so
	// they are always the full range here.
	public void Draw(UIElement owner, in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret, // null to skip drawing a caret
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange,
		int firstLine = 0,
		int lineCount = int.MaxValue)
	{
		global::System.Diagnostics.Debug.Assert(firstLine == 0 && lineCount == int.MaxValue, "TextBlock does not page its content.");
		Draw(owner, session, caret, highlighters, compositionRange, false, null);
	}

	internal void Draw(UIElement owner, in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret,
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange,
		bool suppressBackplate,
		Color? foregroundOverride)
	{
		var useHighContrastAdjustment = owner.UseHighContrastAdjustment();
		var effectiveOpacity = useHighContrastAdjustment && session.Opacity > 0
			? 1f
			: session.Opacity;
		var (highContrastForeground, highContrastBackground, highContrastSelectionForeground, highContrastSelectionBackground) =
			useHighContrastAdjustment
				? owner.GetHighContrastTextColors()
				: default;
		var highContrastBackplateColor = useHighContrastAdjustment
			? WithOpacity(highContrastBackground, effectiveOpacity)
			: default;

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

		Dictionary<(Color color, bool outline), Dictionary<IFont, (List<ushort> glyphs, List<Vector2> positions, float fontSize)>> colorAndOutlineToFontToGlyphs = new();
		List<TextDecorationDrawInfo> textDecorations = new();
		List<(float x1, float x2, float baseline, Color color, FontDetails font, global::Microsoft.UI.Text.TabLeader leader)>? tabLeaders = null;
		Dictionary<(int wordIndex, int lineIndex, float scale), (float left, float right, float y)> spellCheckUnderlines = new();
		List<(float x1, float x2, float y, Color color)> compositionUnderlines = new();
		List<(IImage image, object key, Rect destination)>? inlineObjectImages = null;
		List<(float x1, float x2, float top, float thickness, Color color)> textDecorationLines = new();

		Rect? caretRect = default;

		var runBreakIndex = 0;
		var wordBoundariesIndex = 0;
		var highlighterSlices = highlighterSlicer.GetSegments();
		var highlighterIndex = 0;
		Rect? pendingHighContrastBackplate = null;
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

			// Word boundaries only place spell-check squiggles; computing them runs ICU word breaking on the first draw.
			if (_corrections is not null)
			{
				while (WordBoundaries[wordBoundariesIndex] <= cluster.Value.start)
				{
					wordBoundariesIndex++;
				}
			}

			var lineIndex = cluster.Value.lineIndex;
			var line = _lines[lineIndex];
			var y = GetLineContentTop(lineIndex);
			var unalignedX = cluster.Value.indexInLine == 0
				? 0
				: _xyTable[lineIndex].prefixSummedWidths[cluster.Value.indexInLine - 1].sumUntilAfterCluster;
			var alignmentOffset = GetAlignmentOffsetForLine(line);
			var positionAcc = new Vector2(unalignedX + alignmentOffset, y + line.baselineOffset);
			var fontDetails = cluster.Value.fontDetails;
			var clusterHidden = GetRunHidden(cluster.Value, _runBreaks);
			var clusterBaselineOffset = GetRunBaselineOffset(cluster.Value, _runBreaks);
			var shouldRenderCluster = !clusterHidden && !cluster.Value.containsTab
				&& (!cluster.Value.containsOnlyWhitespace || FeatureConfiguration.TextBlock.RenderWhiteSpace);

			if (!clusterHidden && GetInlineObject(cluster.Value, _inlineObjects) is { } inlineObject)
			{
				if (inlineObject.Image is { } image)
				{
					var imageY = GetInlineObjectTop(inlineObject, line.lineHeight, line.baselineOffset);
					(inlineObjectImages ??= new()).Add((
						image,
						inlineObject.ImageKey ?? image,
						new Rect(
							unalignedX + alignmentOffset,
							y + imageY,
							inlineObject.Width,
							inlineObject.Height)));
				}
			}
			else if (shouldRenderCluster)
			{
				var color = GetForegroundColor(_runBreaks[runBreakIndex].foreground);
				var key = (color, GetRunOutline(cluster.Value, _runBreaks));
				if (!colorAndOutlineToFontToGlyphs.TryGetValue(key, out var fontToGlyphs))
				{
					colorAndOutlineToFontToGlyphs[key] = fontToGlyphs = new Dictionary<IFont, (List<ushort> glyphs, List<Vector2> positions, float fontSize)>();
				}
				if (!fontToGlyphs.TryGetValue(fontDetails.FontHandle, out var glyphsAndPositions))
				{
					fontToGlyphs[fontDetails.FontHandle] = glyphsAndPositions = (new List<ushort>(), new List<Vector2>(), fontDetails.FontSize);
				}
				var glyphs = glyphsAndPositions.glyphs;
				var positions = glyphsAndPositions.positions;

				var characterSpacing = GetClusterCharacterSpacing(cluster.Value, _runBreaks, _text);
				for (var glyphNode = cluster.Value.glyphStart; ; glyphNode = glyphNode.Next!)
				{
					var glyph = glyphNode.Value;
					glyphs.Add(glyph.GlyphId);
					positions.Add(new Vector2(positionAcc.X + glyph.XOffset, positionAcc.Y + glyph.YOffset - clusterBaselineOffset));
					positionAcc.X += glyph.XAdvance;
					if (cluster.Value.glyphLast == glyphNode)
					{
						positionAcc.X += characterSpacing;
						break;
					}
				}
			}
			else if (!clusterHidden
				&& cluster.Value.containsTab
				&& GetTabLeader(cluster.Value) is var tabLeader
				&& tabLeader != global::Microsoft.UI.Text.TabLeader.Spaces
				&& cluster.Value.width > 0)
			{
				(tabLeaders ??= new()).Add((
					unalignedX + alignmentOffset,
					unalignedX + alignmentOffset + cluster.Value.width,
					positionAcc.Y,
					GetForegroundColor(_runBreaks[runBreakIndex].foreground),
					fontDetails,
					tabLeader));
			}

			Color GetForegroundColor(Brush? runForeground)
				=> foregroundOverride ?? (useHighContrastAdjustment
					? WithOpacity(
						highlighter.Value.background is not null
							? highContrastSelectionForeground
							: highContrastForeground,
						effectiveOpacity)
					: BrushToColor(
						highlighter.Value.foreground is { } h ? h : runForeground,
						effectiveOpacity));

			// Floor every edge and +1 the trailing edges so adjacent background
			// rects always overlap by 1 px, preventing antialiased-edge seams.
			var backgroundRect = new Rect(
				new Point(MathF.Floor(unalignedX + alignmentOffset), MathF.Floor(y)),
				new Point(MathF.Floor(unalignedX + alignmentOffset + cluster.Value.width) + 1, MathF.Floor(y + line.lineHeight) + 1));
			if (!clusterHidden && !useHighContrastAdjustment && _runBreaks[runBreakIndex].background is { } characterBackground)
			{
				session.Session.DrawRect(backgroundRect, WithOpacity(characterBackground, effectiveOpacity));
			}
			if (useHighContrastAdjustment
				&& !suppressBackplate
				&& shouldRenderCluster
				&& highlighter.Value.background is null)
			{
				if (pendingHighContrastBackplate is { } pending
					&& CanMergeHighContrastBackplates(pending, backgroundRect))
				{
					pendingHighContrastBackplate = new Rect(
						new Point(Math.Min(pending.Left, backgroundRect.Left), pending.Top),
						new Point(Math.Max(pending.Right, backgroundRect.Right), pending.Bottom));
				}
				else
				{
					FlushHighContrastBackplate(session.Session, ref pendingHighContrastBackplate, highContrastBackplateColor);
					pendingHighContrastBackplate = backgroundRect;
				}
			}
			else if (useHighContrastAdjustment)
			{
				FlushHighContrastBackplate(session.Session, ref pendingHighContrastBackplate, highContrastBackplateColor);
			}

			if (!clusterHidden && highlighter.Value.background is { } selectionBackground)
			{
				if (useHighContrastAdjustment)
				{
					session.Session.DrawRect(backgroundRect, WithOpacity(highContrastSelectionBackground, effectiveOpacity));
				}
				else
				{
					selectionBackground.TryPaint(session.Session, effectiveOpacity, backgroundRect);
				}
			}

			if (!clusterHidden && _corrections?[wordBoundariesIndex] is { } correction)
			{
				var correctionIndexBase = wordBoundariesIndex == 0 ? 0 : WordBoundaries[wordBoundariesIndex - 1];
				if (correctionIndexBase + correction.correctionStart <= cluster.Value.start && correctionIndexBase + correction.correctionEnd >= cluster.Value.end)
				{
					// Only widen this word's underline span here; one continuous squiggly per word is
					// built and drawn after the cluster loop. Building a small zigzag per cluster
					// restarts the wave phase and snaps back to the midline at every cluster boundary,
					// which renders as an irregular scribble. A word wrapped across lines (or switching
					// fonts via fallback) gets one wave per (line, font) span.
					var scale = fontDetails.FontSize / 12.0f;
					// The text visual clips at y + lineHeight, so the whole wave band (midline ± amplitude
					// plus the stroke) must fit above the line bottom or its lower vertices get cut off and
					// the wave renders as disconnected peaks. Place it just below the baseline and clamp.
					var amplitude = SpellCheckSquigglyAmplitudeScale * scale;
					var underlineY = y + line.baselineOffset + 2.5f * scale;
					underlineY = Math.Min(underlineY, y + line.lineHeight - (amplitude + scale));
					var underlineLeftX = unalignedX + alignmentOffset;
					var underlineRightX = underlineLeftX + cluster.Value.width;
					spellCheckUnderlines[(wordBoundariesIndex, lineIndex, scale)] =
						spellCheckUnderlines.TryGetValue((wordBoundariesIndex, lineIndex, scale), out var span)
							? (Math.Min(span.left, underlineLeftX), Math.Max(span.right, underlineRightX), underlineY)
							: (underlineLeftX, underlineRightX, underlineY);
				}
			}

			if (!clusterHidden && compositionRange is var (compStart, compLen) && compLen > 0)
			{
				var compEnd = compStart + compLen;
				if (cluster.Value.start < compEnd && cluster.Value.end > compStart)
				{
					// Place the underline just below the baseline
					var underlineY = y + line.baselineOffset + fontDetails.FontSize / 6.0f;
					var underlineLeftX = unalignedX + alignmentOffset;
					var underlineRightX = underlineLeftX + cluster.Value.width;
					var foreColor = useHighContrastAdjustment
						? WithOpacity(
							highlighter.Value.background is not null
								? highContrastSelectionForeground
								: highContrastForeground,
							effectiveOpacity)
						: BrushToColor(_runBreaks[runBreakIndex].foreground, effectiveOpacity);
					compositionUnderlines.Add((underlineLeftX, underlineRightX, underlineY, foreColor));
				}
			}

			var runDecorations = _runBreaks[runBreakIndex].decorations;
			var underline = _runBreaks[runBreakIndex].underlineType
				?? ((runDecorations & TextDecorations.Underline) != 0
					? global::Microsoft.UI.Text.UnderlineType.Single
					: global::Microsoft.UI.Text.UnderlineType.None);
			var hasUnderline = underline is not global::Microsoft.UI.Text.UnderlineType.None and not global::Microsoft.UI.Text.UnderlineType.Undefined
				&& (underline != global::Microsoft.UI.Text.UnderlineType.Words || !cluster.Value.containsOnlyWhitespace);
			if (!clusterHidden && (runDecorations != TextDecorations.None || hasUnderline))
			{
				// Underline/strikethrough are filled rects whose top edge sits at baseline + the font's
				// decoration position and whose height is the font's decoration thickness, matching
				// DWriteTextRenderer::DrawUnderline/DrawStrikethrough, which offset the baseline by
				// DWRITE_UNDERLINE.offset and hand D2DTextDrawingContext::DrawLine a { 0, 0, width,
				// thickness } rect. The font's metrics report the same top-edge offsets (-post.underlinePosition
				// and -OS/2.yStrikeoutPosition scaled to the em size), so they are used as-is.

				// WinUI/DWrite do not decorate collapsed line-trailing whitespace, so clamp the line to the
				// visible content extent (widthWithoutTrailingSpaces, the same width the alignment uses).
				// Trailing whitespace is on the right for LTR and on the left for RTL.
				// TOM decorations retain the rich edit story's trailing whitespace instead of TextBlock's collapsed tail.
				var decorationWidth = _runBreaks[runBreakIndex].underlineType.HasValue
					? line.width
					: line.widthWithoutTrailingSpaces;
				float contentLeftX, contentRightX;
				if (_rtl)
				{
					contentRightX = alignmentOffset + line.width;
					contentLeftX = contentRightX - decorationWidth;
				}
				else
				{
					contentLeftX = alignmentOffset;
					contentRightX = contentLeftX + decorationWidth;
				}

				var decorationLeftX = Math.Max(unalignedX + alignmentOffset, contentLeftX);
				var decorationRightX = Math.Min(unalignedX + alignmentOffset + cluster.Value.width, contentRightX);

				if (decorationRightX > decorationLeftX)
				{
					var decorationBaseline = y + line.baselineOffset - clusterBaselineOffset;
					// The decoration follows the run's foreground, or the high-contrast foreground when the
					// backplate is active, matching D2DTextDrawingContext::HWRenderLines which resolves the
					// line brush through GetAlternativeForegroundBrush. Unlike glyphs, it is not affected by
					// a TextHighlighter foreground, since WinUI keeps the run brush on the decoration line.
					var decorationColor = foregroundOverride ?? (useHighContrastAdjustment
						? WithOpacity(highContrastForeground, effectiveOpacity)
						: BrushToColor(_runBreaks[runBreakIndex].foreground, effectiveOpacity));
					var decorationMetrics = fontDetails.FontHandle;
					var fallbackThickness = Math.Max(1f, fontDetails.FontSize * FallbackDecorationThicknessRatio);

					if (hasUnderline && underline is global::Microsoft.UI.Text.UnderlineType.Single or global::Microsoft.UI.Text.UnderlineType.Words)
					{
						AddDecoration(
							textDecorationLines,
							decorationLeftX,
							decorationRightX,
							decorationBaseline + (decorationMetrics.UnderlinePosition ?? fontDetails.FontSize * FallbackUnderlinePositionRatio),
							decorationMetrics.UnderlineThickness ?? fallbackThickness,
							decorationColor);
					}
					else if (hasUnderline)
					{
						textDecorations.Add(new TextDecorationDrawInfo(
							decorationLeftX,
							decorationRightX,
							decorationBaseline + (decorationMetrics.UnderlinePosition ?? fontDetails.FontSize * FallbackUnderlinePositionRatio),
							decorationMetrics.UnderlineThickness ?? fallbackThickness,
							fontDetails.FontSize,
							decorationColor,
							underline));
					}

					if ((runDecorations & TextDecorations.Strikethrough) != 0)
					{
						AddDecoration(
							textDecorationLines,
							decorationLeftX,
							decorationRightX,
							decorationBaseline + (decorationMetrics.StrikeoutPosition ?? fontDetails.FontSize * FallbackStrikethroughPositionRatio),
							decorationMetrics.StrikeoutThickness ?? fallbackThickness,
							decorationColor);
					}
				}
			}

			if (caret is var (caretIndex, _, caretThickness))
			{
				if (caretIndex >= cluster.Value.start && caretIndex < cluster.Value.end)
				{
					caretRect = cluster.Value.rtl
						? new Rect(new Point(cluster.Value.width + alignmentOffset + unalignedX - caretThickness, y), new Point(cluster.Value.width + alignmentOffset + unalignedX, y + line.lineHeight))
						: new Rect(new Point(alignmentOffset + unalignedX, y), new Point(alignmentOffset + unalignedX + caretThickness, y + line.lineHeight));
				}
				else if (_endingNewLineLineHeight is null && caretIndex >= cluster.Value.start && clusterIndex == _clustersInLogicalOrder.Count - 1)
				{
					caretRect = cluster.Value.rtl
						? new Rect(new Point(alignmentOffset + unalignedX - caretThickness, y), new Point(alignmentOffset + unalignedX, y + line.lineHeight))
						: new Rect(new Point(cluster.Value.width + alignmentOffset + unalignedX, y), new Point(cluster.Value.width + alignmentOffset + unalignedX + caretThickness, y + line.lineHeight));
				}
			}
		}

		var drawingSession = session.Session;

		FlushHighContrastBackplate(drawingSession, ref pendingHighContrastBackplate, highContrastBackplateColor);

		// WinUI renders the decoration lines before the glyphs (D2DTextDrawingContext::HWRender calls
		// HWRenderLines then HWRenderGlyphTextures), so a decoration never covers the text it belongs to.
		DrawTextDecorations(drawingSession, textDecorations);
		foreach (var (x1, x2, top, thickness, color) in textDecorationLines)
		{
			drawingSession.DrawRect(new Rect(new Point(x1, top), new Point(x2, top + thickness)), color);
		}

		// Outline glyphs are assembled into a path (drawn neutrally); color glyphs (emoji) become images.
		foreach (var ((color, outline), fontToGlyphs) in colorAndOutlineToFontToGlyphs)
		{
			var paintColor = color;
			foreach (var (font, (glyphs, positions, fontSize)) in fontToGlyphs)
			{
				var glyphSpan = CollectionsMarshal.AsSpan(glyphs);
				var positionSpan = CollectionsMarshal.AsSpan(positions);

				if (outline)
				{
					drawingSession.StrokeGlyphRun(font, glyphSpan, positionSpan, 0, paintColor, Math.Max(1, fontSize / 24));
				}
				else
				{
					drawingSession.DrawGlyphRun(font, glyphSpan, positionSpan, 0, paintColor);
				}
			}
		}

		DrawTabLeaders(drawingSession, tabLeaders);

		// Draw paragraph list markers on the first visual line of each paragraph
		DrawParagraphMarkers(
			session,
			foregroundOverride ?? (useHighContrastAdjustment ? WithOpacity(highContrastForeground, effectiveOpacity) : null));

		if (inlineObjectImages is not null)
		{
			foreach (var (image, key, destination) in inlineObjectImages)
			{
				GlyphRunRenderer.DrawImage(drawingSession, image, key, destination, effectiveOpacity);
			}
		}

		foreach (var ((_, _, scale), (left, right, midY)) in spellCheckUnderlines)
		{
			using var path = BuildSpellCheckSquigglyPath(midY, left, right, scale);
			// Widened here rather than handed to StrokePath, which implies flat caps: the wave has round joins and caps.
			using var stroke = path.GetStrokeFillGeometry(new StrokeStyle
			{
				Thickness = scale,
				StartCap = StrokeCap.Round,
				EndCap = StrokeCap.Round,
				LineJoin = StrokeJoin.Round,
			});
			drawingSession.DrawPath(stroke, WithOpacity(Colors.Red, effectiveOpacity));
		}

		foreach (var (x1, x2, underlineY, color) in compositionUnderlines)
		{
			drawingSession.DrawLine(
				new Vector2(x1, underlineY),
				new Vector2(x2, underlineY),
					color, 1);
		}

		if (caretRect is null && caret?.index == _text.Length) // ending new line or empty text
		{
			var alignmentOffset = GetAlignmentOffsetForLine(null);
			var top = _endingLineContentTop;
			var height = _endingNewLineLineHeight ?? _defaultFontDetails.LineHeight;
			caretRect = IsLineRightToLeft(null)
				? new Rect(new Point(alignmentOffset - caret.Value.thickness, top), new Point(alignmentOffset, top + height))
				: new Rect(new Point(alignmentOffset, top), new Point(alignmentOffset + caret.Value.thickness, top + height));
		}

		if (caretRect is not null)
		{
			caret!.Value.brush.TryPaint(session.Session, effectiveOpacity, caretRect.Value);
		}
	}

	private void DrawParagraphMarkers(in Visual.PaintingSession session, Color? foregroundOverride)
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

			var font = line.clusterStart.Value.fontDetails.FontHandle;
			DrawParagraphMarker(
				session,
				layout,
				font,
				(float)_availableSize.Width,
				GetLineContentTop(lineIndex) + line.baselineOffset,
				_runBreaks[runBreakIndex].foreground,
				foregroundOverride);
		}

		if (_endingNewLineLineHeight is not null
			&& _endingParagraphLayout is { MarkerText.Length: > 0 } endingLayout)
		{
			DrawParagraphMarker(
				session,
				endingLayout,
				_defaultFontDetails.FontHandle,
				(float)_availableSize.Width,
				_endingLineContentTop + _endingLineBaselineOffset,
				_defaultForeground,
				foregroundOverride);
		}
	}

	private sealed record MarkerShape(ushort[] Glyphs, Vector2[] Positions, Rect Ink);

	private static MarkerShape GetMarkerShape(ref Dictionary<(IFont font, string text), MarkerShape>? cache, IFont font, string text)
	{
		if (cache?.TryGetValue((font, text), out var shape) is true)
		{
			return shape;
		}

		var (glyphs, positions) = GlyphRunRenderer.Layout(font, text);
		shape = new MarkerShape(glyphs, positions, GlyphRunRenderer.MeasureInk(font, glyphs, positions));
		(cache ??= new())[(font, text)] = shape;
		return shape;
	}

	private void DrawParagraphMarker(
		in Visual.PaintingSession session,
		ParagraphLayoutInfo layout,
		IFont font,
		float totalWidth,
		float baseline,
		Brush? foreground,
		Color? foregroundOverride)
	{
		var cache = _markerShapes;
		var (glyphs, positions, markerBounds) = GetMarkerShape(ref cache, font, layout.MarkerText!);
		var markerAnchor = GetParagraphMarkerAnchor(layout, totalWidth);
		var markerLeft = layout.MarkerAlignment switch
		{
			global::Microsoft.UI.Text.MarkerAlignment.Left => markerAnchor,
			global::Microsoft.UI.Text.MarkerAlignment.Center => markerAnchor - markerBounds.Width / 2,
			_ => markerAnchor - markerBounds.Width,
		};
		session.Session.Save();
		session.Session.Translate((float)(markerLeft - markerBounds.Left), 0);
		session.Session.DrawGlyphRun(font, glyphs, positions, baseline, foregroundOverride ?? BrushToColor(foreground, session.Opacity));
		session.Session.Restore();
	}

	private static void DrawTextDecorations(IDrawingSession session, List<TextDecorationDrawInfo> decorations)
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

			switch (decoration.Style)
			{
				case global::Microsoft.UI.Text.UnderlineType.Double:
					var separation = Math.Max(1, thickness * 1.5f);
					DrawDecorationLine(session, decoration, thickness, -separation / 2);
					DrawDecorationLine(session, decoration, thickness, separation / 2);
					break;
				case global::Microsoft.UI.Text.UnderlineType.Wave:
				case global::Microsoft.UI.Text.UnderlineType.HeavyWave:
					DrawWave(session, decoration, thickness, 0);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DoubleWave:
					DrawWave(session, decoration, thickness, -Math.Max(1, thickness));
					DrawWave(session, decoration, thickness, Math.Max(1, thickness));
					break;
				// Dash intervals are in multiples of the stroke thickness.
				case global::Microsoft.UI.Text.UnderlineType.Dotted:
				case global::Microsoft.UI.Text.UnderlineType.ThickDotted:
					DrawDashedLine(session, decoration, thickness, [1, 2], StrokeCap.Round);
					break;
				case global::Microsoft.UI.Text.UnderlineType.Dash:
				case global::Microsoft.UI.Text.UnderlineType.ThickDash:
					DrawDashedLine(session, decoration, thickness, [4, 2], StrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DashDot:
				case global::Microsoft.UI.Text.UnderlineType.ThickDashDot:
					DrawDashedLine(session, decoration, thickness, [4, 2, 1, 2], StrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.DashDotDot:
				case global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot:
					DrawDashedLine(session, decoration, thickness, [4, 2, 1, 2, 1, 2], StrokeCap.Butt);
					break;
				case global::Microsoft.UI.Text.UnderlineType.LongDash:
				case global::Microsoft.UI.Text.UnderlineType.ThickLongDash:
					DrawDashedLine(session, decoration, thickness, [8, 3], StrokeCap.Butt);
					break;
				default:
					DrawDecorationLine(session, decoration, thickness, 0);
					break;
			}
		}
	}

	private static void DrawTabLeaders(
		IDrawingSession session,
		List<(float x1, float x2, float baseline, Color color, FontDetails font, global::Microsoft.UI.Text.TabLeader leader)>? leaders)
	{
		if (leaders is null)
		{
			return;
		}

		foreach (var (x1, x2, baseline, color, fontDetails, leader) in leaders)
		{
			if (leader is global::Microsoft.UI.Text.TabLeader.Lines or global::Microsoft.UI.Text.TabLeader.ThickLines)
			{
				var strokeWidth = leader == global::Microsoft.UI.Text.TabLeader.ThickLines ? 2 : 1;
				session.DrawLine(new Vector2(x1, baseline + 1), new Vector2(x2, baseline + 1), color, strokeWidth);
				continue;
			}

			var text = leader switch
			{
				global::Microsoft.UI.Text.TabLeader.Dots => ".",
				global::Microsoft.UI.Text.TabLeader.Dashes => "-",
				global::Microsoft.UI.Text.TabLeader.Equals => "=",
				_ => string.Empty,
			};
			if (text.Length == 0)
			{
				continue;
			}

			var font = fontDetails.FontHandle;
			var (glyphs, positions) = GlyphRunRenderer.Layout(font, text, out var textAdvance);
			var advance = Math.Max(1, textAdvance);
			var leaderGlyphs = new List<ushort>();
			var leaderPositions = new List<Vector2>();
			for (var x = x1; x + advance <= x2 && x - x1 < advance * 4096; x += advance)
			{
				for (var i = 0; i < glyphs.Length; i++)
				{
					leaderGlyphs.Add(glyphs[i]);
					leaderPositions.Add(positions[i] + new Vector2(x, baseline));
				}
			}

			session.DrawGlyphRun(font, CollectionsMarshal.AsSpan(leaderGlyphs), CollectionsMarshal.AsSpan(leaderPositions), 0, color);
		}
	}

	private static void DrawDecorationLine(IDrawingSession session, TextDecorationDrawInfo decoration, float thickness, float yOffset)
		=> session.DrawLine(new Vector2(decoration.X1, decoration.Y + yOffset), new Vector2(decoration.X2, decoration.Y + yOffset), decoration.Color, thickness);

	private static void DrawDashedLine(IDrawingSession session, TextDecorationDrawInfo decoration, float thickness, float[] dashArray, StrokeCap cap)
	{
		var builder = GeometryFactory.Current.CreatePathBuilder();
		builder.MoveTo(new Vector2(decoration.X1, decoration.Y));
		builder.LineTo(new Vector2(decoration.X2, decoration.Y));
		using var path = builder.Build();
		using var stroke = path.GetStrokeFillGeometry(new StrokeStyle
		{
			Thickness = thickness,
			StartCap = cap,
			EndCap = cap,
			DashCap = cap,
			DashArray = dashArray,
		});
		session.DrawPath(stroke, decoration.Color);
	}

	private static void DrawWave(IDrawingSession session, TextDecorationDrawInfo decoration, float thickness, float yOffset)
	{
		var amplitude = Math.Max(1, decoration.FontSize / 12);
		var step = amplitude * 2;
		var builder = GeometryFactory.Current.CreatePathBuilder();
		builder.MoveTo(new Vector2(decoration.X1, decoration.Y + yOffset));
		var x = decoration.X1;
		var up = true;
		var segments = 0;
		while (x + step < decoration.X2 && segments++ < 4096)
		{
			x += step;
			builder.LineTo(new Vector2(x, decoration.Y + yOffset + (up ? -amplitude : amplitude)));
			up = !up;
		}
		builder.LineTo(new Vector2(decoration.X2, decoration.Y + yOffset));
		using var path = builder.Build();
		session.StrokePath(path, decoration.Color, thickness);
	}

	private static bool IsThickUnderline(global::Microsoft.UI.Text.UnderlineType style)
		=> style is global::Microsoft.UI.Text.UnderlineType.Thick
			or global::Microsoft.UI.Text.UnderlineType.HeavyWave
			or global::Microsoft.UI.Text.UnderlineType.ThickDash
			or global::Microsoft.UI.Text.UnderlineType.ThickDashDot
			or global::Microsoft.UI.Text.UnderlineType.ThickDashDotDot
			or global::Microsoft.UI.Text.UnderlineType.ThickDotted
			or global::Microsoft.UI.Text.UnderlineType.ThickLongDash;

	// Decorations are collected per cluster, but abutting antialiased rects leave a seam where they
	// meet, so contiguous segments of the same line are merged. WinUI has the same granularity: LS and
	// DWrite emit one decoration rect per run per line, not per cluster. Only the last two entries can
	// match, since a run with both decorations appends them in pairs.
	private static void AddDecoration(List<(float x1, float x2, float top, float thickness, Color color)> decorations, float x1, float x2, float top, float thickness, Color color)
	{
		const float joinTolerance = 0.01f;

		for (var i = decorations.Count - 1; i >= 0 && i >= decorations.Count - 2; i--)
		{
			var previous = decorations[i];
			if (previous.top != top || previous.thickness != thickness || previous.color != color)
			{
				continue;
			}

			if (Math.Abs(previous.x2 - x1) <= joinTolerance)
			{
				decorations[i] = (previous.x1, Math.Max(previous.x2, x2), top, thickness, color);
				return;
			}

			if (Math.Abs(x2 - previous.x1) <= joinTolerance)
			{
				decorations[i] = (Math.Min(x1, previous.x1), previous.x2, top, thickness, color);
				return;
			}
		}

		decorations.Add((x1, x2, top, thickness, color));
	}

	public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(int correctionStart, int correctionEnd)
	{
		if (SpellCheckingService is { } spellCheckingService)
		{
			return spellCheckingService.GetSpellCheckSuggestions(_text, WordBoundaries, correctionStart, correctionEnd);
		}
		else
		{
			return null;
		}
	}

	internal IReadOnlyList<RichEditSpellingAnnotationInfo> GetSpellingAnnotations()
	{
		if (_corrections is null || WordBoundaries.Count == 0)
		{
			return Array.Empty<RichEditSpellingAnnotationInfo>();
		}

		var annotations = new List<RichEditSpellingAnnotationInfo>();
		var wordStart = 0;
		for (var i = 0; i < WordBoundaries.Count; i++)
		{
			var wordEnd = WordBoundaries[i];
			if (i < _corrections.Count && _corrections[i] is { } correction)
			{
				var start = Math.Clamp(wordStart + correction.correctionStart, wordStart, wordEnd);
				var end = Math.Clamp(wordStart + correction.correctionEnd, start, wordEnd);
				if (end > start)
				{
					var suggestions = GetSpellCheckSuggestions(start, end)?.suggestions;
					annotations.Add(new RichEditSpellingAnnotationInfo(
						start,
						end,
						_text.Substring(start, end - start),
						suggestions is { Count: > 0 } ? suggestions.ToArray() : Array.Empty<string>()));
				}
			}

			wordStart = wordEnd;
		}

		return annotations;
	}

	private static Color BrushToColor(Brush? brush, float opacity)
	{
		var color = Colors.Black;
		if (brush is SolidColorBrush scb)
		{
			var scbColor = scb.Color;
			color = Color.FromArgb(
				(byte)(scbColor.A * scb.Opacity * opacity),
				scbColor.R,
				scbColor.G,
				scbColor.B);
		}
		else if (brush is GradientBrush gb)
		{
			var gbColor = gb.FallbackColorWithOpacity;
			color = Color.FromArgb(
				(byte)(gbColor.A * opacity),
				gbColor.R,
				gbColor.G,
				gbColor.B);
		}
		else if (brush is XamlCompositionBrushBase xcbb)
		{
			var gbColor = xcbb.FallbackColorWithOpacity;
			color = Color.FromArgb(
				(byte)(gbColor.A * opacity),
				gbColor.R,
				gbColor.G,
				gbColor.B);
		}

		return color;
	}

	private static Color WithOpacity(Color color, float opacity) =>
		Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);

	private static bool CanMergeHighContrastBackplates(Rect current, Rect next) =>
		Math.Abs(current.Top - next.Top) <= 1f
		&& Math.Abs(current.Bottom - next.Bottom) <= 1f
		&& next.Left <= current.Right + 1
		&& next.Right >= current.Left - 1;

	private static void FlushHighContrastBackplate(IDrawingSession drawingSession, ref Rect? pendingBackplate, Color color)
	{
		if (pendingBackplate is { } backplate)
		{
			drawingSession.DrawRect(backplate, color);
			pendingBackplate = null;
		}
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
			return IsLineRightToLeft(_lines[^1]) ?
				new Rect(alignmentOffset, y, 0, height) :
				new Rect(alignmentOffset + lineWidth, y, 0, height);
		}
		else
		{
			return GetClusterRect(GetClusterAt(index));
		}
	}

	public TextGeometryPositionInfo GetGeometryPosition(int adjustedIndex)
	{
		var index = Math.Clamp(adjustedIndex, 0, _text.Length);
		if (_text.Length == 0 || index == _text.Length)
		{
			var caretRect = GetRectForIndex(index) with { Width = 0 };
			var finalKind = TextGeometryPositionKind.Caret
				| TextGeometryPositionKind.FinalEndOfParagraph
				| TextGeometryPositionKind.TrailingEdge;
			if (_text.Length > 0 && IsLineRightToLeft(_lines[^1]))
			{
				finalKind |= TextGeometryPositionKind.RightToLeft;
			}

			return new TextGeometryPositionInfo(caretRect, caretRect, finalKind);
		}

		var cluster = GetClusterAt(index).Value;
		var characterRect = GetClusterRect(cluster);
		var clusterLength = Math.Max(1, cluster.end - cluster.start);
		var logicalOffset = Math.Clamp(index - cluster.start, 0, clusterLength);
		var fraction = (double)logicalOffset / clusterLength;
		var caretX = cluster.rtl
			? characterRect.Right - (characterRect.Width * fraction)
			: characterRect.Left + (characterRect.Width * fraction);
		var kind = TextGeometryPositionKind.Text | TextGeometryPositionKind.Caret;
		if (GetInlineObject(cluster, _inlineObjects) is not null)
		{
			kind |= TextGeometryPositionKind.InlineObject;
		}
		if (cluster.rtl)
		{
			kind |= TextGeometryPositionKind.RightToLeft;
		}
		if (logicalOffset == 0)
		{
			kind |= TextGeometryPositionKind.LeadingEdge;
		}
		else if (logicalOffset == clusterLength)
		{
			kind |= TextGeometryPositionKind.TrailingEdge;
		}

		return new TextGeometryPositionInfo(
			characterRect,
			new Rect(caretX, characterRect.Y, 0, characterRect.Height),
			kind);
	}

	private LinkedListNode<Cluster> GetClusterAt(int index)
	{
		var clusterIndex = _indexToCluster.BinarySearch(
			(index, index, null!),
			Comparer<(int start, int end, LinkedListNode<Cluster> cluster)>.Create(static (a, b) => a.start.CompareTo(b.start)));

		if (clusterIndex < 0)
		{
			clusterIndex = ~clusterIndex - 1;
		}

		return _indexToCluster[Math.Max(0, clusterIndex)].cluster;
	}

	private Rect GetClusterRect(LinkedListNode<Cluster> clusterNode)
		=> GetClusterRect(clusterNode.Value);

	private Rect GetClusterRect(Cluster cluster)
	{
		var lineIndex = cluster.lineIndex;
		var line = _lines[lineIndex];
		var indexInLine = cluster.indexInLine;
		var alignmentOffset = GetAlignmentOffsetForLine(line);
		var y = GetLineContentTop(lineIndex);
		var unalignedX = indexInLine == 0
			? (IsLineRightToLeft(line) ? line.width : 0)
			: _xyTable[lineIndex].prefixSummedWidths[indexInLine - 1].sumUntilAfterCluster;
		return new Rect(alignmentOffset + unalignedX, y, cluster.width, line.lineHeight);
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

	public int VisualLineCount => Math.Max(1, _lines.Count + (_endingNewLineLineHeight is null ? 0 : 1));

	public TextVisualLineInfo GetVisualLine(int lineIndex)
	{
		if ((uint)lineIndex >= (uint)VisualLineCount)
		{
			throw new ArgumentOutOfRangeException(nameof(lineIndex));
		}

		if (lineIndex < _lines.Count)
		{
			var line = _lines[lineIndex];
			var top = GetLineContentTop(lineIndex);
			return new TextVisualLineInfo(
				line.start,
				line.end - line.start,
				lineIndex,
				new Rect(GetAlignmentOffsetForLine(line), top, line.width, line.lineHeight),
				top + line.baselineOffset,
				lineIndex == 0,
				lineIndex == VisualLineCount - 1);
		}

		var start = _lines.Count == 0 ? 0 : _lines[^1].end;
		var height = _endingNewLineLineHeight ?? _defaultFontDetails.LineHeight;
		return new TextVisualLineInfo(
			start,
			_text.Length - start,
			lineIndex,
			new Rect(GetAlignmentOffsetForLine(null), _endingLineContentTop, 0, height),
			_endingLineContentTop + _endingLineBaselineOffset,
			lineIndex == 0,
			true);
	}

	public int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection)
	{
		if (_text.Length == 0)
		{
			return extendedSelection ? 0 : -1;
		}

		var contentBottom = _endingNewLineLineHeight is { } endingHeight
			? _endingLineContentTop + endingHeight + (_endingParagraphLayout?.SpaceAfter ?? 0)
			: _xyTable[^1].prefixSummedHeight;
		var isAboveContent = p.Y < 0;
		var isBelowContent = p.Y >= contentBottom;
		if (!extendedSelection && (isAboveContent || isBelowContent))
		{
			return -1;
		}

		if (!isBelowContent && p.Y >= _xyTable[^1].prefixSummedHeight)
		{
			return extendedSelection ? _lines[^1].end - (ignoreEndingNewLine ? TrailingCRLFInLine(_lines[^1]) : 0) : -1;
		}

		int lineIndex;
		if (isAboveContent)
		{
			lineIndex = 0;
		}
		else if (isBelowContent)
		{
			lineIndex = _xyTable.Count - 1;
			var lastLine = _lines[^1];
			var lastLineRtl = IsLineRightToLeft(lastLine);
			var lastLineAlignmentOffset = GetAlignmentOffsetForLine(lastLine);
			if (lastLineRtl && p.X > lastLineAlignmentOffset + lastLine.width || !lastLineRtl && p.X < lastLineAlignmentOffset)
			{
				// corner case: bottom left (or bottom right if rtl) of the box, we can either go to the beginning or the end.
				// we match winui and go to the beginning.

				return 0;
			}

			return _text.Length - (ignoreEndingNewLine ? TrailingCRLFInLine(_endingNewLineLineHeight is null ? lastLine : null) : 0);
		}
		else
		{
			lineIndex = _xyTable.BinarySearch(
				((float)p.Y, null!, default),
				Comparer<(float prefixSummedHeight, List<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)> prefixSummedWidths, Line line)>.Create(
					static (a, b) => a.prefixSummedHeight.CompareTo(b.prefixSummedHeight)));

			if (lineIndex < 0)
			{
				lineIndex = ~lineIndex;
			}
			else if (lineIndex < _xyTable.Count - 1)
			{
				lineIndex++;
			}
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
			((float)p.X - GetAlignmentOffsetForLine(line), null!),
			Comparer<(float sumUntilAfterCluster, LinkedListNode<Cluster> cluster)>.Create(
				static (a, b) => a.sumUntilAfterCluster.CompareTo(b.sumUntilAfterCluster)));

		if (clusterIndex < 0)
		{
			clusterIndex = ~clusterIndex;
		}

		var cluster = prefixSummedWidths[clusterIndex].cluster.Value;
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
			if (WordBoundaries.Count == 0)
			{
				return (0, 0);
			}
			else
			{
				return (0, WordBoundaries[0]);
			}
		}
		else if (index == _text.Length)
		{
			if (WordBoundaries.Count == 1)
			{
				return (0, _text.Length);
			}
			else
			{
				return (WordBoundaries[^2], _text.Length - WordBoundaries[^2]);
			}
		}
		else
		{
			var prevBoundary = 0;
			foreach (var boundary in WordBoundaries)
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

	public float FirstLineBaseline => (float)GetBaselineForIndex(0);

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

		var alignmentWidth = _alignmentIncludesTrailingWhitespace ? lineWidth : lineWidthWithoutTrailingSpaces;
		var alignedStart = textAlignment switch
		{
			TextAlignment.Center when alignmentWidth <= contentWidth => leftIndent + (contentWidth - alignmentWidth) / 2,
			TextAlignment.Right when alignmentWidth <= contentWidth => leftIndent + contentWidth - alignmentWidth,
			_ => leftIndent,
		};

		// Logical trailing whitespace is visually on the left for RTL lines. When it is excluded from
		// alignment, offset the full glyph run so the visible extent—not the whitespace—hits the target.
		var visualTrailingOffset = !_alignmentIncludesTrailingWhitespace && IsLineRightToLeft(line)
			? lineWidth - lineWidthWithoutTrailingSpaces
			: 0;
		var alignmentOffset = alignedStart - visualTrailingOffset;
		return IsLineRightToLeft(line)
			? Math.Min(alignmentOffset, totalWidth - lineWidth)
			: Math.Max(alignmentOffset, 0);
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

	private static FontDetails? GetFallbackFont(int codepoint, float fontSize, FontWeight fontWeight, FontStretch fontStretch, FontStyle fontStyle, IFontCacheUpdateListener fontListener)
	{
		var symbolsFontTask = FontDetailsCache.GetFont(FeatureConfiguration.Font.SymbolsFont, fontSize, fontWeight, fontStretch, fontStyle);
		if (symbolsFontTask.loadedTask.IsCompleted)
		{
			if (symbolsFontTask.loadedTask.IsCompletedSuccessfully
				&& symbolsFontTask.loadedTask.Result is { } symbolsFont
				&& symbolsFont.FontHandle.ContainsGlyph(codepoint))
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

		// The provider resolves installed fonts synchronously and defers only when a fallback must be fetched (browser
		// Noto). A synchronously-resolved font is returned immediately; a deferred one registers a listener that
		// re-invalidates the text once it arrives.
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

	// A lone surrogate shows up when an inline boundary splits a pair or a pair arrives one code unit at a time
	// (typing, an IME commit). It reads as U+FFFD so script and font fallback see a valid code point.
	private static int ReadCodepoint(string text, int index, out int length)
	{
		if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
		{
			length = 2;
			return char.ConvertToUtf32(text[index], text[index + 1]);
		}

		length = 1;
		return char.IsSurrogate(text[index]) ? 0xFFFD : text[index];
	}

	private static float GetClusterCharacterSpacing(in Cluster cluster, List<RunBreak> runBreaks, string text)
		=> cluster.runIndex < 0
			? 0
			: GetEffectiveCharacterSpacing(
				CollectionsMarshal.AsSpan(runBreaks)[cluster.runIndex].characterSpacing,
				GetGlyphAdvance(cluster.glyphStart, cluster.glyphLast),
				cluster.containsTab,
				text,
				cluster.start);

	// Mirrors lineservicescallbacks.cpp LineServicesGetRunCharacterWidths: zero-advance glyphs and diacritics (not "spaceable") get no
	// spacing, and spacing never takes an advance below zero.
	private static float GetEffectiveCharacterSpacing(float characterSpacing, float advance, bool containsTab, string text, int start)
		=> characterSpacing == 0 || containsTab || advance <= 0 || IsCombiningMark(text, start)
			? 0
			: Math.Max(characterSpacing, -advance);

	private static float GetGlyphAdvance(LinkedListNode<Glyph> glyphStart, LinkedListNode<Glyph> glyphLast)
	{
		float advance = 0;
		for (var glyphNode = glyphStart; ; glyphNode = glyphNode.Next!)
		{
			advance += glyphNode.Value.XAdvance;
			if (glyphNode == glyphLast)
			{
				return advance;
			}
		}
	}

	private static bool IsCombiningMark(string text, int index)
		=> index < text.Length
			&& CharUnicodeInfo.GetUnicodeCategory(text, index) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

	private static bool GetRunHidden(in Cluster cluster, List<RunBreak> runBreaks)
		=> cluster.runIndex >= 0 && CollectionsMarshal.AsSpan(runBreaks)[cluster.runIndex].hidden;

	private static float GetRunBaselineOffset(in Cluster cluster, List<RunBreak> runBreaks)
		=> cluster.runIndex < 0 ? 0 : CollectionsMarshal.AsSpan(runBreaks)[cluster.runIndex].baselineOffset;

	private static bool GetRunOutline(in Cluster cluster, List<RunBreak> runBreaks)
		=> cluster.runIndex >= 0 && CollectionsMarshal.AsSpan(runBreaks)[cluster.runIndex].outline;

	// An inline object occupies a single U+FFFC cluster.
	private static InlineObjectInfo? GetInlineObject(in Cluster cluster, Dictionary<int, InlineObjectInfo>? inlineObjects)
		=> inlineObjects is not null && cluster.end == cluster.start + 1 && inlineObjects.TryGetValue(cluster.start, out var inlineObject)
			? inlineObject
			: null;

	private global::Microsoft.UI.Text.TabLeader GetTabLeader(in Cluster cluster)
		=> _tabLeaders is not null && _tabLeaders.TryGetValue(cluster.start, out var leader) ? leader : global::Microsoft.UI.Text.TabLeader.Spaces;

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

	private static void AdjustLinesForInlineObjects(List<Line> lines, Dictionary<int, InlineObjectInfo> inlineObjects)
	{
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var minTop = 0f;
			var maxBottom = line.lineHeight;
			for (var node = line.clusterStart; ; node = node.Next!)
			{
				if (GetInlineObject(node.Value, inlineObjects) is { } inlineObject)
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

	private static void AdjustLinesForBaselineOffsets(List<Line> lines, List<RunBreak> runBreaks)
	{
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var minTop = 0f;
			var maxBottom = line.lineHeight;
			for (var node = line.clusterStart; ; node = node.Next!)
			{
				var nodeBaselineOffset = GetRunBaselineOffset(node.Value, runBreaks);
				if (nodeBaselineOffset != 0)
				{
					var font = node.Value.fontDetails.FontHandle;
					minTop = Math.Min(minTop, line.baselineOffset + font.Ascent - nodeBaselineOffset);
					maxBottom = Math.Max(maxBottom, line.baselineOffset + font.Descent - nodeBaselineOffset);
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

	private static void RemoveTrailingCharacterSpacing(List<Line> lines, string text, List<RunBreak> runBreaks, Dictionary<int, InlineObjectInfo>? inlineObjects)
	{
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var contentEnd = line.end - global::Microsoft.UI.Text.TextUnitNavigation.GetHardLineBreakLengthEndingAt(text, line.end);
			if (contentEnd <= line.start)
			{
				continue;
			}

			var terminalCluster = line.clusterLast;
			while (terminalCluster is not null && terminalCluster.Value.start >= contentEnd)
			{
				terminalCluster = terminalCluster == line.clusterStart ? null : terminalCluster.Previous;
			}

			if (terminalCluster is null
				|| terminalCluster.Value.containsTab
				|| GetInlineObject(terminalCluster.Value, inlineObjects) is not null
				|| GetRunHidden(terminalCluster.Value, runBreaks)
				|| GetClusterCharacterSpacing(terminalCluster.Value, runBreaks, text) == 0)
			{
				continue;
			}

			var spacing = GetClusterCharacterSpacing(terminalCluster.Value, runBreaks, text);
			terminalCluster.Value = terminalCluster.Value with { width = terminalCluster.Value.width - spacing };
			lines[lineIndex] = line with
			{
				width = line.width - spacing,
				widthWithoutTrailingSpaces = terminalCluster.Value.containsOnlyWhitespace
					? line.widthWithoutTrailingSpaces
					: line.widthWithoutTrailingSpaces - spacing,
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

	// This method assumes that the FontDetails with the biggest LineHeight is also the one with the biggest ascent.
	// If that assumption is wrong, we will need an additional lazy parameter for the latter.
	private static (float lineHeight, float baselineOffset) GetLineHeightAndBaselineOffset(TextLineBounds textLineBounds, LineStackingStrategy lineStackingStrategy, float lineHeight, FontDetails fontDetailsWithMaxHeightInLine, bool isFirstLine, bool isLastLine)
	{
		// The font's baseline/descent are constrained by TextLineBounds before they drive line stacking.
		var (fontBaseline, fontLineSpacing) = fontDetailsWithMaxHeightInLine.GetTextLineBoundsMetrics(textLineBounds);
		var fontDescent = fontLineSpacing - fontBaseline;

		if (lineStackingStrategy is LineStackingStrategy.MaxHeight || !(lineHeight > 0))
		{
			return (Math.Max(lineHeight, fontLineSpacing), fontBaseline);
		}
		else if (lineStackingStrategy is LineStackingStrategy.BaselineToBaseline)
		{
			if (isFirstLine)
			{
				return (Math.Min(fontLineSpacing, Math.Max(fontBaseline, lineHeight)), fontBaseline);
			}
			else
			{
				if (isLastLine)
				{
					return (lineHeight + fontDescent, lineHeight);
				}
				else
				{
					return (lineHeight, lineHeight);
				}
			}
		}
		else if (lineStackingStrategy is LineStackingStrategy.BlockLineHeight)
		{
			return (lineHeight, lineHeight - fontDescent);
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(lineStackingStrategy));
		}
	}


	private static bool IsLineBreak(string text, int indexAfterLineBreakOpportunity)
		=> global::Microsoft.UI.Text.TextUnitNavigation.IsHardLineBreakAt(text, indexAfterLineBreakOpportunity);

	/// <summary>
	/// Returns the absolute text range of the misspelled word at the given text index,
	/// or null if the index is not on a misspelled word.
	/// </summary>
	public (int correctionStart, int correctionEnd)? GetCorrectionAtIndex(int textIndex)
	{
		if (_corrections is null || WordBoundaries.Count == 0 || textIndex < 0 || textIndex > _text.Length)
		{
			return null;
		}

		var wordStart = 0;
		for (var i = 0; i < WordBoundaries.Count; i++)
		{
			var wordEnd = WordBoundaries[i];
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
