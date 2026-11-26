#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Uno.Buffers;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText
{
	/// <summary>
	/// The part of a text layout that doesn't depend on the available size: the text and its runs, fonts, bidi levels,
	/// shaped glyphs and clusters. A block keeps it so that a layout at another size only breaks the lines again.
	/// </summary>
	/// <remarks>Read-only once created: each layout copies the clusters, and shares everything else.</remarks>
	internal sealed class ShapedText
	{
		// What Create read, compared on reuse: a false miss only shapes again, a false hit would draw stale glyphs.
		// Null when the result can't be reused (the caller streams its inlines).
		private readonly InlineState[]? _inlineStates;
		private readonly FlowDirection _flowDirection;
		// ICU's line and word boundaries depend on the UI culture.
		private readonly string _cultureName;
		private readonly bool _allAscii;
		private readonly Cluster[]? _clusters;
		private List<int>? _lineOpportunityBreaks;
		private List<int>? _singleLineBreaks;
		private bool? _isAsciiWithoutLineBreaks;

		internal readonly string Text;
		internal readonly List<(int start, int end, Hyperlink hyperlink)> HyperlinkRanges;
		internal readonly List<RunBreak> RunBreaks;
		internal readonly Dictionary<int, InlineObjectInfo>? InlineObjects;
		internal readonly List<(int end, TextAlignment alignment)>? ParagraphAlignments;
		internal readonly List<(int end, ParagraphLayoutInfo layout)>? ParagraphLayouts;
		internal readonly bool HasBaselineOffsets;
		// ASCII in a left-to-right paragraph of left-to-right runs: a single left-to-right bidi run, without ICU.
		internal readonly bool TrivialLtr;
		internal readonly bool Rtl;
		internal readonly List<(int end, FlowDirection direction)> BidiBreaks;
		// The alignment a layout with no explicit alignment takes from the text: the start edge of the first logical bidi
		// run, relative to the flow direction.
		internal readonly TextAlignment AlignmentFromText;
		internal readonly LinkedList<Glyph> Glyphs;
		internal readonly StrongBox<List<int>?> WordBoundaries = new();

		private ShapedText(
			InlineState[]? inlineStates,
			FlowDirection flowDirection,
			string cultureName,
			bool allAscii,
			Cluster[]? clusters,
			string text,
			List<(int start, int end, Hyperlink hyperlink)> hyperlinkRanges,
			List<RunBreak> runBreaks,
			Dictionary<int, InlineObjectInfo>? inlineObjects,
			List<(int end, TextAlignment alignment)>? paragraphAlignments,
			List<(int end, ParagraphLayoutInfo layout)>? paragraphLayouts,
			bool hasBaselineOffsets,
			bool trivialLtr,
			bool rtl,
			List<(int end, FlowDirection direction)> bidiBreaks,
			TextAlignment alignmentFromText,
			LinkedList<Glyph> glyphs)
		{
			_inlineStates = inlineStates;
			_flowDirection = flowDirection;
			_cultureName = cultureName;
			_allAscii = allAscii;
			_clusters = clusters;
			Text = text;
			HyperlinkRanges = hyperlinkRanges;
			RunBreaks = runBreaks;
			InlineObjects = inlineObjects;
			ParagraphAlignments = paragraphAlignments;
			ParagraphLayouts = paragraphLayouts;
			HasBaselineOffsets = hasBaselineOffsets;
			TrivialLtr = trivialLtr;
			Rtl = rtl;
			BidiBreaks = bidiBreaks;
			AlignmentFromText = alignmentFromText;
			Glyphs = glyphs;
		}

		/// <summary>
		/// What a fresh shaping hands to the layout that asked for it, so that layout doesn't build them again: the open
		/// paragraph bidi (default for <see cref="TrivialLtr"/> text), which the layout disposes, and the clusters as a list
		/// it may change.
		/// </summary>
		internal readonly record struct FreshShaping(ParagraphBiDi BiDi, LinkedList<Cluster> Clusters);

		/// <summary>
		/// Returns <paramref name="previous"/> if it was shaped from the same inputs, otherwise shapes the text again.
		/// <paramref name="fresh"/> is set after shaping text that isn't empty, and the caller owns it.
		/// </summary>
		internal static ShapedText GetOrCreate(
			ShapedText? previous,
			Inline[] inlines,
			FlowDirection flowDirection,
			IFontCacheUpdateListener fontListener,
			ShapingCache? shapingCache,
			out FreshShaping? fresh)
		{
			if (previous is not null && previous.Matches(inlines, flowDirection))
			{
				fresh = null;
				return previous;
			}

			return Create(inlines, inlines.Length, flowDirection, fontListener, shapingCache, out fresh);
		}

		/// <summary>Shapes text that won't be reused (the inlines may be streamed).</summary>
		internal static ShapedText CreateOnce(
			IEnumerable<Inline> inlines,
			FlowDirection flowDirection,
			IFontCacheUpdateListener fontListener,
			ShapingCache? shapingCache,
			out FreshShaping? fresh)
			=> Create(inlines, reusableInlineCount: -1, flowDirection, fontListener, shapingCache, out fresh);

		/// <summary>A copy of the clusters, for a layout to break into lines.</summary>
		internal LinkedList<Cluster> CopyClusters()
			=> new(_clusters ?? throw new InvalidOperationException("Text shaped once has no clusters to copy."));

		internal List<int> GetLineOpportunityBreaks(TextWrapping textWrapping, TextTrimming textTrimming)
		{
			// NoWrap ASCII text without mandatory breaks never consults intermediate line-break opportunities
			// (no wrapping decisions, single line), so the ICU pass is skipped. Word trimming picks its ellipsis
			// position from the same list, so it still needs the real boundaries.
			if (_allAscii
				&& textWrapping is TextWrapping.NoWrap
				&& textTrimming is not TextTrimming.WordEllipsis
				&& (_isAsciiWithoutLineBreaks ??= IsAsciiWithoutLineBreaks(Text)))
			{
				return _singleLineBreaks ??= [Text.Length];
			}

			if (_lineOpportunityBreaks is null)
			{
				// Line breaking is a document-level operation. Computing boundaries per inline can split a
				// CRLF pair when character formatting changes between its two code units.
				var breaks = new List<int>();
				AppendBoundaries(/* Line */ 2, Text, 0, breaks);
				_lineOpportunityBreaks = breaks;
			}

			return _lineOpportunityBreaks;
		}

		private bool Matches(Inline[] inlines, FlowDirection flowDirection)
		{
			if (_inlineStates is null
				|| _flowDirection != flowDirection
				|| inlines.Length != _inlineStates.Length
				|| !string.Equals(_cultureName, CultureInfo.CurrentUICulture.Name, StringComparison.Ordinal))
			{
				return false;
			}

			for (var i = 0; i < inlines.Length; i++)
			{
				if (InlineState.Read(inlines[i], flowDirection) != _inlineStates[i])
				{
					return false;
				}
			}

			return true;
		}

		/// <param name="reusableInlineCount">The number of inlines, to keep what was read from them; -1 not to.</param>
		private static ShapedText Create(
			IEnumerable<Inline> inlines,
			int reusableInlineCount,
			FlowDirection flowDirection,
			IFontCacheUpdateListener fontListener,
			ShapingCache? shapingCache,
			out FreshShaping? fresh)
		{
			fresh = null;
			var cultureName = CultureInfo.CurrentUICulture.Name;
			var inlineStates = reusableInlineCount >= 0 ? new InlineState[reusableInlineCount] : null;
			var stringBuilder = new StringBuilder();
			var hyperlinkRanges = new List<(int start, int end, Hyperlink hyperlink)>();
			var runBreaks = new List<RunBreak>();
			var scriptBreaks = new List<int>();
			var fontBreaks = new List<(int end, FontDetails fontDetails)>();
			var allAscii = true;
			var allRunsLtr = true;
			string? singleInlineText = null;
			var nonEmptyInlines = 0;
			Dictionary<int, InlineObjectInfo>? inlineObjects = null;
			List<(int end, TextAlignment alignment)>? paragraphAlignments = null;
			List<(int end, ParagraphLayoutInfo layout)>? paragraphLayouts = null;
			var hasBaselineOffsets = false;

			var inlineIndex = 0;
			foreach (var inline in inlines)
			{
				var state = InlineState.Read(inline, flowDirection);
				if (inlineStates is not null)
				{
					inlineStates[inlineIndex] = state;
				}
				inlineIndex++;

				var inlineText = state.Text;
				if (string.IsNullOrEmpty(inlineText))
				{
					continue;
				}

				singleInlineText = ++nonEmptyInlines == 1 ? inlineText : null;
				var inlineStart = stringBuilder.Length;
				stringBuilder.Append(inlineText);
				if (state.InlineObject is { } inlineObject)
				{
					(inlineObjects ??= new())[inlineStart] = inlineObject;
				}
				if (state.ParagraphAlignment is { } paragraphAlignment)
				{
					(paragraphAlignments ??= new()).Add((inlineStart + inlineText.Length, paragraphAlignment));
				}
				if (state.ParagraphLayout is { } paragraphLayout)
				{
					(paragraphLayouts ??= new()).Add((inlineStart + inlineText.Length, paragraphLayout));
				}

				var currentFontDetails = state.FontInfo;
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

					if (!state.FontInfo.FontHandle.ContainsGlyph(codepoint))
					{
						// A character several scripts share (Common or Inherited, e.g. tatweel, the Arabic comma, combining
						// marks) stays in the run's fallback font: a font change mid-word splits the shaping run and breaks
						// joining. A letter of its own script still gets the fallback for that script.
						newFontDetails = newScript is UScriptCommon or UScriptInherited
							&& currentFontDetails != state.FontInfo
							&& currentFontDetails.FontHandle.ContainsGlyph(codepoint)
							? currentFontDetails
							: GetFallbackFont(codepoint, (float)state.FontSize, state.FontWeight, state.FontStretch, state.FontStyle, fontListener) ?? state.FontInfo;
					}
					else
					{
						newFontDetails = state.FontInfo;
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
				allRunsLtr &= state.Direction is FlowDirection.LeftToRight;
				hasBaselineOffsets |= state.Rich?.BaselineOffset is not (null or 0);
				runBreaks.Add(new RunBreak(
					inlineStart + inlineText.Length,
					state.Foreground,
					state.Direction,
					state.Decorations,
					state.CharacterSpacing,
					state.Rich));
				fontBreaks.Add((inlineStart + inlineText.Length, currentFontDetails));

				if (state.Hyperlink is { } hyperLink)
				{
					hyperlinkRanges.Add((inlineStart, inlineStart + inlineText.Length, hyperLink));
				}
			}

			var text = singleInlineText ?? stringBuilder.ToString();
			var bidiBreaks = new List<(int end, FlowDirection direction)>();
			var glyphs = new LinkedList<Glyph>();
			var trivialLtr = allAscii && allRunsLtr && flowDirection is FlowDirection.LeftToRight && paragraphLayouts is null;
			if (text.Length == 0)
			{
				return new ShapedText(
					inlineStates, flowDirection, cultureName, allAscii, [], text, hyperlinkRanges, runBreaks, inlineObjects, paragraphAlignments, paragraphLayouts, hasBaselineOffsets,
					trivialLtr, flowDirection is FlowDirection.RightToLeft, bidiBreaks, TextAlignment.Left, glyphs);
			}

			bool rtl;
			TextAlignment? alignmentFromText = null;
			// The paragraph bidi is handed to the layout that caused this shaping, for its per-line reordering.
			var paragraphBiDi = default(ParagraphBiDi);
			var handedOver = false;
			try
			{
				if (trivialLtr)
				{
					// ASCII has no RTL characters, so with an LTR paragraph and LTR runs the BiDi outcome is a single
					// LTR run — skip the ICU BiDi pass entirely (a real cost when short labels re-layout every frame).
					rtl = false;
					bidiBreaks.Add((text.Length, FlowDirection.LeftToRight));
					alignmentFromText = TextAlignment.Left;
				}
				else
				{
					paragraphBiDi = ParagraphBiDi.Create(text, runBreaks, flowDirection);
					var bidi = paragraphBiDi.Handle;
					var runCount = ICU.GetMethod<ICU.ubidi_countRuns>()(bidi, out var countRunsErrorCode);
					ICU.CheckErrorCode<ICU.ubidi_countRuns>(countRunsErrorCode);
					rtl = ICU.GetMethod<ICU.ubidi_getParaLevel>()(bidi) is UBIDI_RTL;
					for (var bidiRunIndex = 0; bidiRunIndex < runCount; bidiRunIndex++)
					{
						var level = ICU.GetMethod<ICU.ubidi_getVisualRun>()(bidi, bidiRunIndex, out var logicalStart, out var length);
						CI.Assert(level is UBIDI_LTR or UBIDI_RTL);
						bidiBreaks.Add((logicalStart + length, level is UBIDI_RTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight));

						if (alignmentFromText is null && logicalStart == 0)
						{
							// The content's start edge, expressed relative to the element's flow direction.
							alignmentFromText = (level is UBIDI_RTL) != (flowDirection is FlowDirection.RightToLeft) ? TextAlignment.Right : TextAlignment.Left;
						}
					}
				}

				var clusterBreaks = new LinkedList<Cluster>();
				foreach (var shapingRun in new ShapingRunEnumerator(runBreaks, scriptBreaks, bidiBreaks, fontBreaks))
				{
					var direction = shapingRun.direction is FlowDirection.RightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;
					var runText = text.AsSpan(shapingRun.start, shapingRun.end - shapingRun.start);
					ref readonly var run = ref CollectionsMarshal.AsSpan(runBreaks)[shapingRun.runIndex];
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
						glyphs.AddLast(new Glyph(0, 0, 0, 0));
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
										text,
										clusterBreaks.Last?.Value.end ?? 0,
										shapingRun.start + clusters[index],
										clusterBreaks.Last?.Value.glyphLast?.Next ?? glyphs.First!,
										glyphs.Last!,
										shapingRun.fontDetails,
										shapingRun.runIndex,
										run.characterSpacing,
										run.hidden));
								}
								glyphs.AddLast(new Glyph(glyphRun.Glyphs[index], glyphRun.Advances[index], glyphRun.Offsets[index].X, glyphRun.Offsets[index].Y));
							}
						}
						else
						{
							for (var index = count - 1; index >= 0; index--)
							{
								if (index < count - 1 && clusters[index] != clusters[index + 1])
								{
									clusterBreaks.AddLast(Cluster.Create(
										text,
										clusterBreaks.Last?.Value.end ?? 0,
										shapingRun.start + clusters[index],
										clusterBreaks.Last?.Value.glyphLast?.Next ?? glyphs.First!,
										glyphs.Last!,
										shapingRun.fontDetails,
										shapingRun.runIndex,
										run.characterSpacing,
										run.hidden));
								}
								glyphs.AddLast(new Glyph(glyphRun.Glyphs[index], glyphRun.Advances[index], glyphRun.Offsets[index].X, glyphRun.Offsets[index].Y));
							}
						}

						clusterBreaks.AddLast(Cluster.Create(
							text,
							clusterBreaks.Last?.Value.end ?? 0,
							shapingRun.end,
							clusterBreaks.Last?.Value.glyphLast?.Next ?? glyphs.First!,
							glyphs.Last!,
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

				Cluster[]? reusableClusters = null;
				if (inlineStates is not null)
				{
					reusableClusters = new Cluster[clusterBreaks.Count];
					clusterBreaks.CopyTo(reusableClusters, 0);
				}

				fresh = new FreshShaping(paragraphBiDi, clusterBreaks);
				handedOver = true;
				return new ShapedText(
					inlineStates, flowDirection, cultureName, allAscii, reusableClusters, text, hyperlinkRanges, runBreaks, inlineObjects, paragraphAlignments, paragraphLayouts, hasBaselineOffsets,
					trivialLtr, rtl, bidiBreaks, alignmentFromText ?? (rtl != (flowDirection is FlowDirection.RightToLeft) ? TextAlignment.Right : TextAlignment.Left), glyphs);
			}
			finally
			{
				if (!handedOver)
				{
					paragraphBiDi.Dispose();
				}
			}
		}
	}

	/// <summary>Everything <see cref="ShapedText"/> reads from an inline.</summary>
	private readonly record struct InlineState(
		Inline Inline,
		string? Text,
		FontDetails FontInfo,
		double FontSize,
		FontWeight FontWeight,
		FontStretch FontStretch,
		FontStyle FontStyle,
		Brush? Foreground,
		FlowDirection Direction,
		TextDecorations Decorations,
		float CharacterSpacing,
		RichRunState? Rich,
		InlineObjectInfo? InlineObject,
		TextAlignment? ParagraphAlignment,
		ParagraphLayoutInfo? ParagraphLayout,
		Hyperlink? Hyperlink)
	{
		// The brush is compared by reference, as the property system does: SolidColorBrush.Equals compares colors, and the
		// layout draws with the brush it keeps, so an equal brush would leave the old one drawing.
		public bool Equals(InlineState other)
			=> ReferenceEquals(Inline, other.Inline)
			&& string.Equals(Text, other.Text, StringComparison.Ordinal)
			&& Equals(FontInfo, other.FontInfo)
			&& FontSize.Equals(other.FontSize)
			&& FontWeight.Equals(other.FontWeight)
			&& FontStretch == other.FontStretch
			&& FontStyle == other.FontStyle
			&& ReferenceEquals(Foreground, other.Foreground)
			&& Direction == other.Direction
			&& Decorations == other.Decorations
			&& CharacterSpacing.Equals(other.CharacterSpacing)
			&& Equals(Rich, other.Rich)
			&& ReferenceEquals(InlineObject, other.InlineObject)
			&& ParagraphAlignment == other.ParagraphAlignment
			&& ReferenceEquals(ParagraphLayout, other.ParagraphLayout)
			&& ReferenceEquals(Hyperlink, other.Hyperlink);

		public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Inline), Text, FontInfo);

		internal static InlineState Read(Inline inline, FlowDirection flowDirection)
		{
			var run = inline as Run;
			return new(
				inline,
				inline.GetText(),
				inline.FontInfo,
				inline.FontSize,
				inline.FontWeight,
				inline.FontStretch,
				inline.FontStyle,
				inline.Foreground,
				run?.FlowDirection ?? flowDirection,
				inline.TextDecorations,
				(float)inline.FontSize * inline.CharacterSpacing / 1000,
				// Snapshot: a streamed RichEditBox layout reuses one Run across paragraphs.
				run is { HasRichFormat: true }
					? new RichRunState(
						run.CharacterBackground,
						run.IsHidden,
						run.RichEditUnderlineType,
						run.RichEditKerningThreshold,
						run.RichEditLanguageTag,
						run.RichEditTextScript,
						run.RichEditSmallCaps,
						run.RichEditBaselineOffset,
						run.RichEditOutline)
					: null,
				run?.InlineObject,
				run?.ParagraphAlignment,
				run?.ParagraphLayout,
				TryGetHyperLink(inline));
		}
	}

	/// <summary>An open ICU paragraph bidi and the embedding levels it reads; disposing it closes both.</summary>
	internal readonly struct ParagraphBiDi : IDisposable
	{
		private readonly byte[]? _embeddingLevels;

		private ParagraphBiDi(IntPtr handle, byte[] embeddingLevels)
		{
			Handle = handle;
			_embeddingLevels = embeddingLevels;
		}

		internal IntPtr Handle { get; }

		internal static ParagraphBiDi Create(string text, List<RunBreak> runBreaks, FlowDirection flowDirection)
		{
			var embeddingLevels = ArrayPool<byte>.Shared.Rent(text.Length);
			try
			{
				for (int i = 0; i < runBreaks.Count; i++)
				{
					var (start, count) = i == 0 ? (0, runBreaks[0].end) : (runBreaks[i - 1].end, runBreaks[i].end - runBreaks[i - 1].end);
					var direction = runBreaks[i].direction;
					var level = flowDirection is FlowDirection.LeftToRight
						? (direction is FlowDirection.LeftToRight ? 0 : 1)
						: (direction is FlowDirection.RightToLeft ? 1 : 2); // 2 and not 0 because embedding must increase nesting level when switching direction inside RTL paragraph
					Array.Fill(embeddingLevels, (byte)level, start, count);
				}

				// ICU keeps reading the levels until the bidi is closed, so they are returned with it.
				_ = ICU.CreateBiDiAndSetPara(text, 0, text.Length, flowDirection is FlowDirection.RightToLeft ? UBIDI_DEFAULT_RTL : UBIDI_DEFAULT_LTR, out var bidi, embeddingLevels);
				return new ParagraphBiDi(bidi, embeddingLevels);
			}
			catch
			{
				ArrayPool<byte>.Shared.Return(embeddingLevels);
				throw;
			}
		}

		public void Dispose()
		{
			if (Handle != IntPtr.Zero)
			{
				ICU.GetMethod<ICU.ubidi_close>()(Handle);
			}

			if (_embeddingLevels is not null)
			{
				ArrayPool<byte>.Shared.Return(_embeddingLevels);
			}
		}
	}
}
