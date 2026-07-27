#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// Projects the RichEditBox Text Object Model's character-format run model onto the shared
	// DisplayBlock (a TextBlock). Documents with a bounded fragment count use retained XAML inlines
	// and incremental splices. Larger documents stream the same indexed runs through a custom layout
	// with one reusable Run, preserving rich semantics without retaining unbounded XAML objects.
	partial class RichEditBox
	{
		private const double DipsPerPoint = 96d / 72d;
		private const int MaxRetainedInlineFragments = 8192;
		private const double ScriptFontScale = 0.65;
		private const double SuperscriptOffsetEm = 0.35;
		private const double SubscriptOffsetEm = -0.15;
		private const int MaxRenderPositionDeltas = 128;
		private const int MaxRenderFragmentTextLength = 4096;
		private bool _lastRenderWasRich;
		private bool _usesBoundedRichLayout;
		private readonly List<RenderedFragment> _renderedFragments = new();
		private readonly List<RenderPositionDelta> _renderPositionDeltas = new();
		private TextBlock? _renderedFragmentOwner;
		private RichTextLayoutSource? _richTextLayoutSource;
		private FontFamily? _mathFontFamily;
		private int _renderedTextLength;
		private long _renderedTextVersion = -1;
		private long _renderedCharacterFormatVersion = -1;
		private long _renderedParagraphFormatVersion = -1;
		private int _renderFragmentCreationCount;
		private int _renderFragmentSpecificationCount;
		private int _renderSpliceCount;
		private int _renderFullDiffCount;
		private int _boundedRichLayoutCreateCount;
		private long _boundedRichLayoutRunVisitCount;
		private long _renderPositionGeneration;
		private long _renderPositionBaseGeneration;

		private sealed class RenderedFragment
		{
			internal required int Start;
			internal required int Length;
			internal required string SourceText;
			internal required CharacterFormatState CharacterFormat;
			internal required ParagraphFormatState ParagraphFormat;
			internal required ParagraphLayoutInfo? ParagraphLayout;
			internal required global::Microsoft.UI.Xaml.TextAlignment? ParagraphAlignment;
			internal required FlowDirection FlowDirection;
			internal required Inline Inline;
			internal required Run Run;
			internal required long PositionGeneration;
		}

		private readonly record struct RenderFragmentSpec(
			int Start,
			int Length,
			string SourceText,
			CharacterFormatState CharacterFormat,
			ParagraphFormatState ParagraphFormat,
			ParagraphLayoutInfo? ParagraphLayout,
			global::Microsoft.UI.Xaml.TextAlignment? ParagraphAlignment,
			FlowDirection FlowDirection);

		private readonly record struct RenderPositionDelta(
			int OldStart,
			int OldEnd,
			int NewStart,
			int TextLengthDelta);

		internal int RenderFragmentCreationCount => _renderFragmentCreationCount;

		internal int RenderFragmentSpecificationCount => _renderFragmentSpecificationCount;

		internal int RenderSpliceCount => _renderSpliceCount;

		internal int RenderFullDiffCount => _renderFullDiffCount;

		internal bool UsesBoundedRichLayout => _usesBoundedRichLayout;

		internal int BoundedRichLayoutCreateCount => _boundedRichLayoutCreateCount;

		internal long BoundedRichLayoutRunVisitCount => _boundedRichLayoutRunVisitCount;

		internal int BoundedRichLayoutRetainedInlineCount => _richTextLayoutSource?.RetainedInlineCount ?? 0;

		internal int BoundedRichLayoutRetainedResourceCount => _richTextLayoutSource?.RetainedResourceCount ?? 0;

		internal int BoundedRichLayoutCachedParagraphCount => _richTextLayoutSource?.CachedParagraphCount ?? 0;

		internal int BoundedRichLayoutParagraphRebuildCount => _richTextLayoutSource?.ParagraphLayoutRebuildCount ?? 0;

		internal long BoundedRichLayoutShapingOperationCount => _richTextLayoutSource?.ShapingOperationCount ?? 0;

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
			var textLength = document.TextLength;
			var runs = document.FormatRuns;
			var paragraphRuns = document.ParagraphRuns;
			var terminalParagraph = document.TerminalParagraphFormat;
			var renderParagraphAlignments = document.HasMixedParagraphAlignments;
			var renderParagraphLayouts = document.HasVisualParagraphFormatting;
			var hasListFormatting = document.HasListParagraphFormatting;
			var renderInvalidation = document.ConsumeRenderInvalidation();
			var block = _textBoxView.DisplayBlock;
			if (!ReferenceEquals(_renderedFragmentOwner, block))
			{
				_renderedFragmentOwner = block;
				_renderedFragments.Clear();
				_renderPositionDeltas.Clear();
				_renderPositionBaseGeneration = _renderPositionGeneration;
				_renderedTextLength = 0;
				_lastRenderWasRich = false;
				_usesBoundedRichLayout = false;
			}
			var mathLayout = GetMathLayout(document.StructuredMath);
			var useBoundedRichLayout = mathLayout is null
				&& (_usesBoundedRichLayout && renderInvalidation is not { Full: true }
					|| CountRenderFragments(
						document,
						runs,
						paragraphRuns,
						0,
						textLength,
						renderParagraphLayouts,
						MaxRetainedInlineFragments) > MaxRetainedInlineFragments);
			RichTextLayoutSource? richTextLayoutSource = null;
			if (useBoundedRichLayout)
			{
				richTextLayoutSource = _richTextLayoutSource ??= new RichTextLayoutSource(this);
				richTextLayoutSource.Synchronize(
					document,
					runs,
					paragraphRuns,
					renderParagraphAlignments,
					renderParagraphLayouts,
					hasListFormatting,
					renderInvalidation);
			}
			block.CustomTextLayout = useBoundedRichLayout
				? richTextLayoutSource!
				: mathLayout;
			block.FontFamily = document.IsMathMode
				? _mathFontFamily ??= new FontFamily(global::Microsoft.UI.Text.RichEditTextDocument.MathFontFamilyName)
				: FontFamily;
			block.DefaultTabStop = document.DefaultTabStop * 4f / 3f;
			block.AlignmentIncludesTrailingWhitespace = document.AlignmentIncludesTrailingWhitespace;
			block.IgnoreTrailingCharacterSpacing = document.IgnoreTrailingCharacterSpacing;
			if (renderParagraphLayouts)
			{
				var terminalListState = hasListFormatting
					? BuildListMarkerState(document, paragraphRuns)
					: new ParagraphListMarkerState();
				var endingLayout = CreateParagraphLayout(terminalParagraph, terminalListState);
				if (!ParagraphLayoutsEqual(block.EndingParagraphLayout, endingLayout))
				{
					block.EndingParagraphLayout = endingLayout;
				}
			}
			else if (block.EndingParagraphLayout is not null)
			{
				block.EndingParagraphLayout = null;
			}
			block.EndingParagraphAlignment = renderParagraphAlignments
				&& TryMapParagraphAlignment(terminalParagraph.Alignment, out var terminalAlignment)
					? terminalAlignment
					: null;
			if (useBoundedRichLayout)
			{
				if (!_usesBoundedRichLayout)
				{
					block.Inlines.Clear();
				}
				_renderedFragments.Clear();
				_renderPositionDeltas.Clear();
				_renderPositionBaseGeneration = _renderPositionGeneration;
				_renderedTextLength = textLength;
				_usesBoundedRichLayout = true;
				block.InvalidateMeasure();
			}
			else
			{
				_usesBoundedRichLayout = false;
				_ = RenderRuns(
					block,
					document,
					textLength,
					runs,
					paragraphRuns,
					renderParagraphAlignments,
					renderParagraphLayouts,
					hasListFormatting,
					renderInvalidation);
			}

			if (_textBoxView.Extension is { } extension
				&& document.TextVersion != _renderedTextVersion)
			{
				if (_renderedTextVersion >= 0
					&& renderInvalidation is { Full: false } textInvalidation)
				{
					extension.ReplaceText(
						textInvalidation.OldStart,
						textInvalidation.OldEnd - textInvalidation.OldStart,
						document.GetTextInRange(textInvalidation.NewStart, textInvalidation.NewEnd));
				}
				else
				{
					extension.SetText(document.PlainText);
				}
			}
			_lastRenderWasRich = true;

			_renderedTextVersion = document.TextVersion;
			_renderedCharacterFormatVersion = document.CharacterFormatVersion;
			_renderedParagraphFormatVersion = document.ParagraphFormatVersion;
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

		private bool RenderRuns(
			TextBlock block,
			global::Microsoft.UI.Text.RichEditTextDocument document,
			int textLength,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts,
			bool hasListFormatting,
			global::Microsoft.UI.Text.RichEditTextDocument.RenderInvalidation? invalidation)
		{
			var inlines = block.Inlines;
			var requiresFullDiff = !_lastRenderWasRich
				|| _renderedFragments.Count == 0
				|| invalidation is { Full: true }
				|| hasListFormatting
				|| invalidation is null
					&& (_renderedTextVersion != document.TextVersion
						|| _renderedCharacterFormatVersion != document.CharacterFormatVersion
						|| _renderedParagraphFormatVersion != document.ParagraphFormatVersion);
			if (!requiresFullDiff
				&& invalidation is null
				&& _renderedTextVersion == document.TextVersion
				&& _renderedCharacterFormatVersion == document.CharacterFormatVersion
				&& _renderedParagraphFormatVersion == document.ParagraphFormatVersion)
			{
				return true;
			}

			var oldFirst = 0;
			var oldLast = _renderedFragments.Count;
			var newStart = 0;
			var newEnd = textLength;
			if (!requiresFullDiff && invalidation is { } localInvalidation)
			{
				FindAffectedFragmentRange(localInvalidation.OldStart, localInvalidation.OldEnd, out oldFirst, out oldLast);
				oldFirst = Math.Max(0, oldFirst - 1);
				oldLast = Math.Min(_renderedFragments.Count, oldLast + 1);
				var oldSliceStart = GetRenderedFragmentStart(_renderedFragments[oldFirst]);
				var oldSliceEnd = GetRenderedFragmentEnd(_renderedFragments[oldLast - 1]);
				var textLengthDelta = textLength - _renderedTextLength;
				newStart = MapOldRenderPosition(oldSliceStart, localInvalidation, textLengthDelta, mapEnd: false);
				newEnd = MapOldRenderPosition(oldSliceEnd, localInvalidation, textLengthDelta, mapEnd: true);
				newStart = Math.Clamp(newStart, 0, textLength);
				newEnd = Math.Clamp(newEnd, newStart, textLength);
			}
			else
			{
				_renderFullDiffCount++;
			}

			var specs = BuildRenderFragmentSpecs(
				document,
				runs,
				paragraphRuns,
				newStart,
				newEnd,
				renderParagraphAlignments,
				renderParagraphLayouts);
			var resultingCount = _renderedFragments.Count - (oldLast - oldFirst) + specs.Count;
			if (resultingCount > MaxRetainedInlineFragments)
			{
				return false;
			}

			var textLengthChange = textLength - _renderedTextLength;
			if (!requiresFullDiff && invalidation is { } positionInvalidation && textLengthChange != 0)
			{
				_renderPositionDeltas.Add(new RenderPositionDelta(
					positionInvalidation.OldStart,
					positionInvalidation.OldEnd,
					positionInvalidation.NewStart,
					textLengthChange));
				_renderPositionGeneration++;
			}

			var commonPrefix = 0;
			var oldCount = oldLast - oldFirst;
			while (commonPrefix < oldCount
				&& commonPrefix < specs.Count
				&& RenderFragmentMatches(_renderedFragments[oldFirst + commonPrefix], specs[commonPrefix]))
			{
				UpdateRenderedFragment(_renderedFragments[oldFirst + commonPrefix], specs[commonPrefix]);
				commonPrefix++;
			}

			var commonSuffix = 0;
			while (commonSuffix < oldCount - commonPrefix
				&& commonSuffix < specs.Count - commonPrefix
				&& RenderFragmentMatches(
					_renderedFragments[oldLast - commonSuffix - 1],
					specs[specs.Count - commonSuffix - 1]))
			{
				UpdateRenderedFragment(
					_renderedFragments[oldLast - commonSuffix - 1],
					specs[specs.Count - commonSuffix - 1]);
				commonSuffix++;
			}

			var removeIndex = oldFirst + commonPrefix;
			var removeCount = oldCount - commonPrefix - commonSuffix;
			var insertCount = specs.Count - commonPrefix - commonSuffix;
			var insertedFragments = new List<RenderedFragment>(insertCount);
			var insertedInlines = new List<Inline>(insertCount);
			for (var i = 0; i < insertCount; i++)
			{
				var fragment = CreateRenderedFragment(specs[commonPrefix + i], block.FontSize);
				insertedFragments.Add(fragment);
				insertedInlines.Add(fragment.Inline);
			}

			var updateKnownText = document.TextVersion != _renderedTextVersion;
			var knownText = updateKnownText
				? GetUpdatedDisplayText(block.Text, document, invalidation, requiresFullDiff)
				: null;
			if (_renderedFragments.Count == 0)
			{
				inlines.ReplaceRange(0, inlines.Count, insertedInlines, knownText, updateKnownText);
				_renderedFragments.AddRange(insertedFragments);
				_renderSpliceCount++;
			}
			else if (removeCount != 0 || insertCount != 0)
			{
				inlines.ReplaceRange(removeIndex, removeCount, insertedInlines, knownText, updateKnownText);
				if (removeCount != 0)
				{
					_renderedFragments.RemoveRange(removeIndex, removeCount);
				}
				if (insertCount != 0)
				{
					_renderedFragments.InsertRange(removeIndex, insertedFragments);
				}
				_renderSpliceCount++;
			}

			if (requiresFullDiff)
			{
				_renderPositionDeltas.Clear();
				_renderPositionBaseGeneration = _renderPositionGeneration;
			}
			else if (_renderPositionDeltas.Count >= MaxRenderPositionDeltas)
			{
				CompactRenderPositions();
			}
			_renderedTextLength = textLength;
			return true;
		}

		private string GetUpdatedDisplayText(
			string currentText,
			global::Microsoft.UI.Text.RichEditTextDocument document,
			global::Microsoft.UI.Text.RichEditTextDocument.RenderInvalidation? invalidation,
			bool requiresFullDiff)
		{
			if (requiresFullDiff
				|| invalidation is not { Full: false } textInvalidation
				|| currentText.Length != _renderedTextLength
				|| textInvalidation.OldStart < 0
				|| textInvalidation.OldEnd < textInvalidation.OldStart
				|| textInvalidation.OldEnd > currentText.Length)
			{
				return document.PlainText;
			}

			var replacement = document.GetTextInRange(textInvalidation.NewStart, textInvalidation.NewEnd);
			return string.Concat(
				currentText.AsSpan(0, textInvalidation.OldStart),
				replacement.AsSpan(),
				currentText.AsSpan(textInvalidation.OldEnd));
		}

		private List<RenderFragmentSpec> BuildRenderFragmentSpecs(
			global::Microsoft.UI.Text.RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			int start,
			int end,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts)
			=> new(EnumerateRenderFragmentSpecs(
				document,
				runs,
				paragraphRuns,
				start,
				end,
				renderParagraphAlignments,
				renderParagraphLayouts));

		private IEnumerable<RenderFragmentSpec> EnumerateRenderFragmentSpecs(
			global::Microsoft.UI.Text.RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			int start,
			int end,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts)
		{
			if (start >= end)
			{
				yield break;
			}

			var position = start;
			var characterRuns = runs.GetCursor(document.FindCharacterRunIndexForRender(position));
			var paragraphRunsCursor = paragraphRuns.GetCursor(document.FindParagraphRunIndexForRender(position));
			var paragraphEnd = 0;
			ParagraphLayoutInfo? paragraphLayout = null;
			var listState = new ParagraphListMarkerState();
			while (position < end && characterRuns.IsValid && paragraphRunsCursor.IsValid)
			{
				var characterRun = characterRuns.Current;
				var paragraphRun = paragraphRunsCursor.Current;
				var hasList = renderParagraphLayouts
					&& paragraphRun.Format.ListType is not global::Microsoft.UI.Text.MarkerType.None
						and not global::Microsoft.UI.Text.MarkerType.Undefined;
				if (renderParagraphLayouts && (paragraphLayout is null || hasList && position >= paragraphEnd))
				{
					paragraphEnd = hasList ? document.GetParagraphEndForRender(position) : end;
					paragraphLayout = CreateParagraphLayout(paragraphRun.Format, listState);
				}

				var characterRunEnd = characterRuns.End;
				var paragraphRunEnd = paragraphRunsCursor.End;
				var requiresPerParagraphMarker = hasList && paragraphRun.Format.ListLevelIndex > 0;
				var length = GetRenderFragmentLength(
					document,
					position,
					end,
					characterRunEnd,
					paragraphRunEnd,
					requiresPerParagraphMarker ? paragraphEnd : end,
					characterRun.Format.InlineImage is not null);
				if (length <= 0)
				{
					if (position >= characterRunEnd)
					{
						characterRuns.MoveNext();
					}
					if (position >= paragraphRunEnd)
					{
						paragraphRunsCursor.MoveNext();
						paragraphLayout = null;
					}
					continue;
				}

				global::Microsoft.UI.Xaml.TextAlignment? paragraphAlignment = null;
				if (renderParagraphAlignments && TryMapParagraphAlignment(paragraphRun.Format.Alignment, out var mappedAlignment))
				{
					paragraphAlignment = mappedAlignment;
				}
				var flowDirection = renderParagraphLayouts && paragraphRun.Format.RightToLeft
					? FlowDirection.RightToLeft
					: FlowDirection.LeftToRight;
				yield return new RenderFragmentSpec(
					position,
					length,
					document.GetTextInRange(position, position + length),
					characterRun.Format,
					paragraphRun.Format,
					renderParagraphLayouts ? paragraphLayout : null,
					paragraphAlignment,
					flowDirection);
				_renderFragmentSpecificationCount++;

				position += length;
				if (position >= characterRunEnd)
				{
					characterRuns.MoveNext();
				}
				if (position >= paragraphRunEnd)
				{
					paragraphRunsCursor.MoveNext();
					paragraphLayout = null;
				}
			}
		}

		private int CountRenderFragments(
			global::Microsoft.UI.Text.RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			int start,
			int end,
			bool renderParagraphLayouts,
			int stopAfter)
		{
			if (start >= end)
			{
				return 0;
			}

			var count = 0;
			var position = start;
			var characterRuns = runs.GetCursor(document.FindCharacterRunIndexForRender(position));
			var paragraphRunsCursor = paragraphRuns.GetCursor(document.FindParagraphRunIndexForRender(position));
			var paragraphEnd = 0;
			while (position < end && characterRuns.IsValid && paragraphRunsCursor.IsValid)
			{
				var characterRun = characterRuns.Current;
				var paragraphRun = paragraphRunsCursor.Current;
				var hasList = renderParagraphLayouts
					&& paragraphRun.Format.ListType is not global::Microsoft.UI.Text.MarkerType.None
						and not global::Microsoft.UI.Text.MarkerType.Undefined;
				if (hasList && position >= paragraphEnd)
				{
					paragraphEnd = document.GetParagraphEndForRender(position);
				}

				var characterRunEnd = characterRuns.End;
				var paragraphRunEnd = paragraphRunsCursor.End;
				var length = GetRenderFragmentLength(
					document,
					position,
					end,
					characterRunEnd,
					paragraphRunEnd,
					hasList && paragraphRun.Format.ListLevelIndex > 0 ? paragraphEnd : end,
					characterRun.Format.InlineImage is not null);
				if (length <= 0)
				{
					if (position >= characterRunEnd)
					{
						characterRuns.MoveNext();
					}
					if (position >= paragraphRunEnd)
					{
						paragraphRunsCursor.MoveNext();
					}
					continue;
				}

				count++;
				if (count > stopAfter)
				{
					return count;
				}
				position += length;
				if (position >= characterRunEnd)
				{
					characterRuns.MoveNext();
				}
				if (position >= paragraphRunEnd)
				{
					paragraphRunsCursor.MoveNext();
				}
			}

			return count;
		}

		private static int GetRenderFragmentLength(
			global::Microsoft.UI.Text.RichEditTextDocument document,
			int position,
			int end,
			int characterRunEnd,
			int paragraphRunEnd,
			int paragraphEnd,
			bool hasInlineImage)
		{
			var length = Math.Min(
				end - position,
				Math.Min(characterRunEnd - position, paragraphRunEnd - position));
			length = Math.Min(length, MaxRenderFragmentTextLength);
			if (position + length < end
				&& length == MaxRenderFragmentTextLength)
			{
				var candidateEnd = position + length;
				var textElementStart = document.GetTextElementStart(candidateEnd);
				if (textElementStart > position)
				{
					length = textElementStart - position;
				}
				else
				{
					var textElementEnd = document.GetTextElementEnd(candidateEnd);
					var runEnd = Math.Min(end, Math.Min(characterRunEnd, paragraphRunEnd));
					if (textElementEnd <= runEnd)
					{
						length = textElementEnd - position;
					}
				}
			}
			length = Math.Min(length, paragraphEnd - position);
			if (hasInlineImage)
			{
				length = Math.Min(length, 1);
			}
			return length;
		}

		private RenderedFragment CreateRenderedFragment(RenderFragmentSpec spec, double inheritedFontSize)
		{
			var run = CreateRun(spec.SourceText, spec.CharacterFormat, inheritedFontSize);
			run.ParagraphAlignment = spec.ParagraphAlignment;
			run.ParagraphLayout = spec.ParagraphLayout;
			run.FlowDirection = spec.FlowDirection;

			Inline inline;
			if (spec.CharacterFormat.Link is not null)
			{
				var hyperlink = new Hyperlink();
				hyperlink.Inlines.Add(run);
				inline = hyperlink;
			}
			else
			{
				inline = run;
			}

			_renderFragmentCreationCount++;
			return new RenderedFragment
			{
				Start = spec.Start,
				Length = spec.Length,
				SourceText = spec.SourceText,
				CharacterFormat = spec.CharacterFormat,
				ParagraphFormat = spec.ParagraphFormat,
				ParagraphLayout = spec.ParagraphLayout,
				ParagraphAlignment = spec.ParagraphAlignment,
				FlowDirection = spec.FlowDirection,
				Inline = inline,
				Run = run,
				PositionGeneration = _renderPositionGeneration,
			};
		}

		private static bool RenderFragmentMatches(RenderedFragment fragment, RenderFragmentSpec spec)
			=> string.Equals(fragment.SourceText, spec.SourceText, StringComparison.Ordinal)
				&& (fragment.CharacterFormat.InlineImage is null && spec.CharacterFormat.InlineImage is null
					? fragment.CharacterFormat.Equals(spec.CharacterFormat)
					: ReferenceEquals(fragment.CharacterFormat, spec.CharacterFormat))
				&& fragment.ParagraphFormat.Equals(spec.ParagraphFormat)
				&& fragment.ParagraphAlignment == spec.ParagraphAlignment
				&& fragment.FlowDirection == spec.FlowDirection
				&& ParagraphLayoutsEqual(fragment.ParagraphLayout, spec.ParagraphLayout);

		private void UpdateRenderedFragment(RenderedFragment fragment, RenderFragmentSpec spec)
		{
			fragment.Start = spec.Start;
			fragment.Length = spec.Length;
			fragment.SourceText = spec.SourceText;
			fragment.CharacterFormat = spec.CharacterFormat;
			fragment.ParagraphFormat = spec.ParagraphFormat;
			fragment.ParagraphAlignment = spec.ParagraphAlignment;
			fragment.FlowDirection = spec.FlowDirection;
			fragment.PositionGeneration = _renderPositionGeneration;
		}

		private void FindAffectedFragmentRange(int start, int end, out int first, out int last)
		{
			first = 0;
			last = _renderedFragments.Count;
			if (_renderedFragments.Count == 0)
			{
				return;
			}

			var low = 0;
			var high = _renderedFragments.Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				var fragment = _renderedFragments[middle];
				if (GetRenderedFragmentEnd(fragment) <= start)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			first = Math.Min(low, _renderedFragments.Count - 1);

			if (start == end)
			{
				last = first + 1;
				return;
			}

			low = first;
			high = _renderedFragments.Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (GetRenderedFragmentStart(_renderedFragments[middle]) < end)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			last = Math.Max(first + 1, low);
		}

		private int GetRenderedFragmentStart(RenderedFragment fragment)
		{
			if (fragment.PositionGeneration == _renderPositionGeneration)
			{
				return fragment.Start;
			}
			if (fragment.PositionGeneration < _renderPositionBaseGeneration)
			{
				throw new InvalidOperationException("The rendered fragment position generation is no longer available.");
			}

			var firstDelta = checked((int)(fragment.PositionGeneration - _renderPositionBaseGeneration));
			for (var i = firstDelta; i < _renderPositionDeltas.Count; i++)
			{
				var delta = _renderPositionDeltas[i];
				if (fragment.Start <= delta.OldStart)
				{
					continue;
				}
				if (fragment.Start >= delta.OldEnd)
				{
					fragment.Start += delta.TextLengthDelta;
				}
				else
				{
					fragment.Start = delta.NewStart;
				}
			}
			fragment.PositionGeneration = _renderPositionGeneration;
			return fragment.Start;
		}

		private int GetRenderedFragmentEnd(RenderedFragment fragment)
			=> GetRenderedFragmentStart(fragment) + fragment.Length;

		private void CompactRenderPositions()
		{
			foreach (var fragment in _renderedFragments)
			{
				_ = GetRenderedFragmentStart(fragment);
			}
			_renderPositionDeltas.Clear();
			_renderPositionBaseGeneration = _renderPositionGeneration;
		}

		private static int MapOldRenderPosition(
			int position,
			global::Microsoft.UI.Text.RichEditTextDocument.RenderInvalidation invalidation,
			int textLengthDelta,
			bool mapEnd)
		{
			if (invalidation.OldStart == invalidation.OldEnd && position == invalidation.OldStart)
			{
				return mapEnd ? invalidation.NewEnd : invalidation.NewStart;
			}
			if (position <= invalidation.OldStart)
			{
				return position;
			}
			if (position >= invalidation.OldEnd)
			{
				return position + textLengthDelta;
			}
			return mapEnd ? invalidation.NewEnd : invalidation.NewStart;
		}

		private static ParagraphListMarkerState BuildListMarkerState(
			global::Microsoft.UI.Text.RichEditTextDocument document,
			IReadOnlyList<ParagraphRun> paragraphRuns)
		{
			var state = new ParagraphListMarkerState();
			var paragraphRunIndex = 0;
			var paragraphRunOffset = 0;
			for (var position = 0; position < document.TextLength;)
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
				var paragraphEnd = document.GetParagraphEndForRender(position);
				paragraphRunOffset += paragraphEnd - position;
				position = paragraphEnd;
			}

			return state;
		}

		private static ParagraphLayoutInfo CreateParagraphLayout(ParagraphFormatState format, ParagraphListMarkerState listState)
		{
			var marker = ParagraphListMarker.GetNext(format, listState, out var hasList);

			var lineSpacing = format.LineSpacingRule is global::Microsoft.UI.Text.LineSpacingRule.AtLeast or global::Microsoft.UI.Text.LineSpacingRule.Exactly
				? format.LineSpacing * (float)DipsPerPoint
				: format.LineSpacing;
			var tabs = format.Tabs.Count == 0
				? Array.Empty<ParagraphTabLayoutInfo>()
				: new ParagraphTabLayoutInfo[format.Tabs.Count];
			for (var i = 0; i < tabs.Length; i++)
			{
				var tab = format.Tabs[i];
				tabs[i] = new ParagraphTabLayoutInfo(
					tab.Position * (float)DipsPerPoint,
					tab.Alignment,
					tab.Leader);
			}
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
				Tabs = tabs,
			};
		}

		private static bool ParagraphLayoutsEqual(ParagraphLayoutInfo? left, ParagraphLayoutInfo? right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}
			if (left is null || right is null
				|| left.LeftIndent != right.LeftIndent
				|| left.RightIndent != right.RightIndent
				|| left.FirstLineIndent != right.FirstLineIndent
				|| left.SpaceBefore != right.SpaceBefore
				|| left.SpaceAfter != right.SpaceAfter
				|| left.LineSpacingRule != right.LineSpacingRule
				|| left.LineSpacing != right.LineSpacing
				|| left.RightToLeft != right.RightToLeft
				|| left.IsList != right.IsList
				|| !string.Equals(left.MarkerText, right.MarkerText, StringComparison.Ordinal)
				|| left.ListTab != right.ListTab
				|| left.MarkerAlignment != right.MarkerAlignment
				|| left.Tabs.Length != right.Tabs.Length)
			{
				return false;
			}

			for (var i = 0; i < left.Tabs.Length; i++)
			{
				if (left.Tabs[i] != right.Tabs[i])
				{
					return false;
				}
			}

			return true;
		}

		internal static string? FormatListMarker(
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
				global::Microsoft.UI.Text.MarkerType.UnicodeSequence when IsValidListMarkerUnicodeScalar(number) => char.ConvertFromUtf32(number),
				global::Microsoft.UI.Text.MarkerType.CircledNumber => ToCircledNumber(number, black: false),
				global::Microsoft.UI.Text.MarkerType.BlackCircleWingding => ToWingdingCircledNumber(number, black: true),
				global::Microsoft.UI.Text.MarkerType.WhiteCircleWingding => ToWingdingCircledNumber(number, black: false),
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

		internal static bool IsValidListMarkerUnicodeScalar(int value)
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

		private static string ToWingdingCircledNumber(int number, bool black)
		{
			if (number is >= 1 and <= 10)
			{
				return char.ConvertFromUtf32((black ? 0x278a : 0x2780) + number - 1);
			}
			return ToCircledNumber(number, black);
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

		private static Run CreateRun(string text, CharacterFormatState format, double inheritedFontSize)
		{
			var run = new Run
			{
				Text = format.AllCaps ? ToUpperPreservingUtf16Length(text, format.LanguageTag) : text,
				RichEditKerningThreshold = format.Kerning,
				RichEditLanguageTag = string.IsNullOrEmpty(format.LanguageTag) ? null : format.LanguageTag,
				RichEditTextScript = format.TextScript,
				RichEditSmallCaps = format.SmallCaps && !format.AllCaps,
				RichEditOutline = format.Outline,
			};
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

			var sourceFontSize = format.Size > 0 ? format.Size * DipsPerPoint : inheritedFontSize;
			run.RichEditBaselineOffset = format.Position * (float)DipsPerPoint;
			if (format.Superscript || format.Subscript)
			{
				run.FontSize = sourceFontSize * ScriptFontScale;
				run.RichEditBaselineOffset += (float)(sourceFontSize * (format.Superscript ? SuperscriptOffsetEm : SubscriptOffsetEm));
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

		private static string ToUpperPreservingUtf16Length(string text, string languageTag)
		{
			CultureInfo culture;
			var isTurkic = languageTag.Equals("tr", StringComparison.OrdinalIgnoreCase)
				|| languageTag.StartsWith("tr-", StringComparison.OrdinalIgnoreCase)
				|| languageTag.Equals("az", StringComparison.OrdinalIgnoreCase)
				|| languageTag.StartsWith("az-", StringComparison.OrdinalIgnoreCase);
			if (string.IsNullOrEmpty(languageTag))
			{
				culture = CultureInfo.InvariantCulture;
			}
			else
			{
				try
				{
					culture = CultureInfo.GetCultureInfo(languageTag);
				}
				catch (CultureNotFoundException)
				{
					culture = CultureInfo.InvariantCulture;
				}
			}

			char[]? transformed = null;
			// TOM positions are UTF-16 offsets. Per-code-unit casing deliberately avoids expansions
			// such as "ß" -> "SS", which would invalidate selection and hit-testing indices.
			for (var i = 0; i < text.Length; i++)
			{
				var upper = isTurkic
					? text[i] switch
					{
						'i' => '\u0130',
						'\u0131' => 'I',
						_ => char.ToUpper(text[i], culture),
					}
					: char.ToUpper(text[i], culture);
				if (upper != text[i])
				{
					transformed ??= text.ToCharArray();
					transformed[i] = upper;
				}
			}

			return transformed is null ? text : new string(transformed);
		}

		internal bool AreRenderedFragmentsValid()
		{
			if (_renderPositionBaseGeneration + _renderPositionDeltas.Count != _renderPositionGeneration)
			{
				return false;
			}
			if (!_lastRenderWasRich)
			{
				return _renderedFragments.Count == 0;
			}
			if (_textBoxView?.DisplayBlock is not { } block)
			{
				return false;
			}
			if (_usesBoundedRichLayout)
			{
				return _renderedFragments.Count == 0
					&& ReferenceEquals(block.CustomTextLayout, _richTextLayoutSource)
					&& block.Inlines.Count == 0;
			}
			if (block.Inlines.Count != _renderedFragments.Count)
			{
				return false;
			}

			var position = 0;
			for (var i = 0; i < _renderedFragments.Count; i++)
			{
				var fragment = _renderedFragments[i];
				if (GetRenderedFragmentStart(fragment) != position
					|| fragment.Length != fragment.SourceText.Length
					|| !ReferenceEquals(fragment.Inline, block.Inlines[i])
					|| !string.Equals(
						GetPlainTextSlice(position, fragment.Length),
						fragment.SourceText,
						StringComparison.Ordinal))
				{
					return false;
				}
				position += fragment.Length;
			}

			return position == GetPlainTextLength();
		}
	}
}
