#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	internal sealed class TrackedTextRangeRegistration
	{
		internal TrackedTextRangeRegistration(UnoTextRange range, long generation)
		{
			Range = new WeakReference<UnoTextRange>(range);
			Generation = generation;
		}

		internal WeakReference<UnoTextRange> Range { get; }

		internal long Generation { get; set; }
	}

	// Text, formatting runs, tracked ranges, preserved metadata, and undo state mutate in lock-step.
	public partial class RichEditTextDocument
	{
		internal readonly record struct RenderInvalidation(
			int OldStart,
			int OldEnd,
			int NewStart,
			int NewEnd,
			bool ParagraphSemanticsChanged,
			bool Full);

		private const int MaxRangeEditLogEntries = 128;
		private const int RangeRegistrationSweepInterval = 256;
		private readonly RichEditBox _owner;
		private readonly List<TrackedTextRangeRegistration> _ranges = new();
		private readonly Dictionary<long, int> _trackedRangeGenerations = new();
		private long _minimumTrackedRangeGeneration = long.MaxValue;
		private readonly List<RangeEditDelta> _rangeEditLog = new();
		private readonly List<RangeEditLogSegment> _rangeEditLogSegments = new();
		private readonly TextElementBoundaryCache _textElementBoundaryCache = new();
		private readonly TextRangeUnitBoundaryCache _textRangeUnitBoundaryCache = new();
		private readonly TextStoryBuffer _textBuffer = new();
		private RtfPreservedMetadata _preservedRtfMetadata = RtfPreservedMetadata.Empty;
		private bool _preservedRtfMetadataEditApplied;
		private long _characterFormatVersion;
		private long _paragraphFormatVersion;
		private long _automationVersion;
		private long _rangeEditGeneration;
		private long _rangeEditLogBaseGeneration;
		private int _rangeRegistrationsSinceSweep;
		private long _rangeRebaseApplicationCount;
		private int _rangeEditLogCompactionCount;
		private int _deadRangeCleanupCount;
		private RenderInvalidation? _pendingRenderInvalidation;
		private UnoTextSelection? _selection;
		private const long MaxFragmentImagePixels = 8L * 1024 * 1024;
		private const int MaxFragmentImageBytes = 4 * 1024 * 1024;
		private const int MaxFragmentImageCount = 64;

		// Display batching: while batched, render requests are coalesced and applied once the
		// outermost ApplyDisplayUpdates balances the matching BatchDisplayUpdates.
		private int _batchDepth;
		private bool _pendingRender;
		private bool _pendingContentChange;
		private int _selectionMutationDepth;
		private long _selectionChangeVersion;

		internal RichEditTextDocument(RichEditBox owner)
		{
			_owner = owner;
		}

		/// <summary>The current plain-text content of the document.</summary>
		internal string PlainText => _textBuffer.GetText();

		/// <summary>The number of characters in the plain-text buffer.</summary>
		internal int TextLength => _textBuffer.Length;

		/// <summary>The story character count, including the virtual final paragraph mark.</summary>
		internal int StoryLength => _textBuffer.Length + 1;

		internal bool HasPendingDisplayUpdates => _batchDepth > 0 && _pendingRender;

		internal bool AreRunIndexesValid()
		{
			if (!_runs.AreInvariantsValid() || !_paragraphRuns.AreInvariantsValid())
			{
				return false;
			}

			var end = 0;
			for (var i = 0; i < _runs.Count; i++)
			{
				if (_runs[i].Length <= 0
					|| _runs[i].Format.InlineImage is not null && _runs[i].Length != 1
					|| i > 0 && CharacterFormatState.CanCoalesce(_runs[i - 1].Format, _runs[i].Format))
				{
					return false;
				}
				end += _runs[i].Length;
				if (_runs.GetEnd(i) != end)
				{
					return false;
				}
			}
			if (end != _textBuffer.Length)
			{
				return false;
			}

			end = 0;
			for (var i = 0; i < _paragraphRuns.Count; i++)
			{
				if (_paragraphRuns[i].Length <= 0
					|| i > 0 && _paragraphRuns[i - 1].Format.Equals(_paragraphRuns[i].Format))
				{
					return false;
				}
				end += _paragraphRuns[i].Length;
				if (_paragraphRuns.GetEnd(i) != end)
				{
					return false;
				}
			}

			return end == _textBuffer.Length;
		}

		internal bool AreRenderProfilesValid()
			=> IsVisualCharacterFormattingProfileValid() && IsParagraphRenderProfileValid();

		internal TextElementBoundaryView TextElementBoundaries
			=> _textElementBoundaryCache.Get(PlainText, TextVersion);

		internal long TextVersion => _textBuffer.Version;

		internal long CharacterFormatVersion => _characterFormatVersion;

		internal long ParagraphFormatVersion => _paragraphFormatVersion;

		internal int TextElementBoundaryRebuildCount => _textElementBoundaryCache.RebuildCount;

		internal int TextElementBoundaryStorageBytes => _textElementBoundaryCache.StorageBytes;

		internal int UnitBoundaryCacheRebuildCount => _textRangeUnitBoundaryCache.RebuildCount;

		internal int GetUnitBoundaryCacheRebuildCount(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> _textRangeUnitBoundaryCache.GetRebuildCount(unit);

		internal int GetUnitBoundaryOwnedStorageBytes(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> GetUnitBoundaries(unit)?.OwnedStorageBytes ?? 0;

		internal RenderInvalidation? ConsumeRenderInvalidation()
		{
			var invalidation = _pendingRenderInvalidation;
			_pendingRenderInvalidation = null;
			return invalidation;
		}

		internal long AutomationVersion => _automationVersion;

		internal long RangeEditGeneration => _rangeEditGeneration;

		internal Func<string, global::Microsoft.UI.Text.LetterCase, string>? ChangeCaseMapperForTesting { get; set; }

		internal string ChangeCaseText(string text, global::Microsoft.UI.Text.LetterCase value)
			=> ChangeCaseMapperForTesting?.Invoke(text, value)
				?? (value == global::Microsoft.UI.Text.LetterCase.Upper
					? text.ToUpperInvariant()
					: text.ToLowerInvariant());

		internal int PendingRangeEditCount => _rangeEditLog.Count;

		internal long RetainedRangeEditCount
			=> _rangeEditGeneration - GetRetainedRangeEditBaseGeneration();

		internal int RangeEditLogSegmentCount => _rangeEditLogSegments.Count;

		internal int TrackedRangeReferenceCount => _ranges.Count;

		internal long RangeRebaseApplicationCount => _rangeRebaseApplicationCount;

		internal int RangeEditLogCompactionCount => _rangeEditLogCompactionCount;

		internal int DeadRangeCleanupCount => _deadRangeCleanupCount;

		internal int TextBufferPieceCount => _textBuffer.PieceCount;

		internal int TextBufferTreeHeight => _textBuffer.TreeHeight;

		internal int TextBufferCompactionCount => _textBuffer.CompactionCount;

		internal long TextBufferCompactedCharacterCount => _textBuffer.CompactedCharacterCount;

		internal int TextBufferFullMaterializationCount => _textBuffer.FullMaterializationCount;

		internal int CharacterRunIndexTreeHeight => _runs.TreeHeight;

		internal int ParagraphRunIndexTreeHeight => _paragraphRuns.TreeHeight;

		internal bool AreTextBufferInvariantsValid() => _textBuffer.AreInvariantsValid();

		internal void ResetTextBufferDiagnosticsForTesting() => _textBuffer.ResetDiagnostics();

		internal bool AreRangeEditLogInvariantsValid()
		{
			if (_rangeEditLogBaseGeneration + _rangeEditLog.Count != _rangeEditGeneration)
			{
				return false;
			}

			var retainedBaseGeneration = GetRetainedRangeEditBaseGeneration();
			foreach (var registration in _ranges)
			{
				if (registration.Range.TryGetTarget(out var range)
					&& (range.RangeEditGeneration < retainedBaseGeneration
						|| range.RangeEditGeneration > _rangeEditGeneration))
				{
					return false;
				}
			}

			var expectedGeneration = retainedBaseGeneration;
			foreach (var segment in _rangeEditLogSegments)
			{
				if (segment.BaseGeneration != expectedGeneration)
				{
					return false;
				}
				expectedGeneration = segment.EndGeneration;
			}

			return expectedGeneration == _rangeEditLogBaseGeneration;
		}

		internal void ResetRangeTrackingCountersForTesting()
		{
			_rangeRebaseApplicationCount = 0;
			_rangeEditLogCompactionCount = 0;
			_deadRangeCleanupCount = 0;
		}

		internal int GetTextElementStart(int position)
			=> _textBuffer.GetTextElementStart(position);

		internal int GetTextElementEnd(int position)
			=> _textBuffer.GetTextElementEnd(position);

		internal bool TryGetLineLayoutStamp(out long layoutVersion, out double width)
			=> _owner.TryGetLineLayoutStamp(out layoutVersion, out width);

		internal bool TryGetRangeRectangles(int start, int end, PointOptions options, out global::Windows.Foundation.Rect[] rectangles)
			=> _owner.TryGetRangeRectangles(
				Math.Clamp(start, 0, TextLength),
				Math.Clamp(end, 0, TextLength),
				options,
				out rectangles);

		internal List<(int start, int length)>? GetTextChunks(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			var chunks = TextUnitNavigation.GetChunks(
				_textBuffer,
				unit,
				unit == global::Microsoft.UI.Text.TextRangeUnit.Word ? TextElementBoundaries : default);
			if (chunks is null || unit != global::Microsoft.UI.Text.TextRangeUnit.Paragraph)
			{
				return chunks;
			}

			if (chunks.Count == 0 || TextUnitNavigation.EndsInParagraphBreak(_textBuffer))
			{
				chunks.Add((TextLength, 1));
			}
			else
			{
				var last = chunks[chunks.Count - 1];
				chunks[chunks.Count - 1] = (last.start, last.length + 1);
			}

			return chunks;
		}

		private void SetPlainTextCore(string text)
			=> _textBuffer.Reset(text);

		private void RecordRenderInvalidation(
			int oldStart,
			int oldEnd,
			int newStart,
			int newEnd,
			bool paragraphSemanticsChanged,
			bool full)
		{
			if (_pendingRenderInvalidation is not null)
			{
				_pendingRenderInvalidation = new RenderInvalidation(0, 0, 0, 0, ParagraphSemanticsChanged: true, Full: true);
				return;
			}

			_pendingRenderInvalidation = new RenderInvalidation(
				oldStart,
				oldEnd,
				newStart,
				newEnd,
				paragraphSemanticsChanged,
				full);
		}

		private static bool ContainsParagraphBreak(string text)
		{
			foreach (var character in text)
			{
				if (character is '\r' or '\n' or '\u2029')
				{
					return true;
				}
			}

			return false;
		}

		internal char GetCharacterAt(int position) => _textBuffer[position];

		internal bool TryGetRuneAt(int position, out global::System.Text.Rune value)
			=> _textBuffer.TryGetRuneAt(position, out value);

		internal int IndexOfText(
			string value,
			int start,
			int count,
			StringComparison comparison)
			=> _textBuffer.IndexOf(value, start, count, comparison);

		internal bool TextRangeEquals(
			int start,
			string value,
			StringComparison comparison)
			=> _textBuffer.RangeEquals(start, value, comparison);

		internal int GetHardLineBreakLengthAt(int position)
			=> TextUnitNavigation.GetHardLineBreakLengthAt(_textBuffer, position);

		internal int GetHardLineBreakLengthEndingAt(int position)
			=> TextUnitNavigation.GetHardLineBreakLengthEndingAt(_textBuffer, position);

		internal (int start, int length) GetLogicalLineChunk(int position)
			=> TextUnitNavigation.GetLogicalLineChunk(_textBuffer, position);

		/// <summary>Returns the substring of the plain-text buffer between two clamped positions.</summary>
		internal string GetTextInRange(int start, int end)
		{
			var includesFinalEop = end > _textBuffer.Length;
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			var text = _textBuffer.Slice(start, end - start);
			return includesFinalEop ? text + '\r' : text;
		}

		internal string GetTextInRange(int start, int end, global::Microsoft.UI.Text.TextGetOptions options)
		{
			var includesFinalEop = end > _textBuffer.Length;
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			if (start < end)
			{
				if (start > 0
					&& start < _textBuffer.Length
					&& char.IsLowSurrogate(_textBuffer[start])
					&& char.IsHighSurrogate(_textBuffer[start - 1]))
				{
					start--;
				}
				if (end > 0
					&& end < _textBuffer.Length
					&& char.IsHighSurrogate(_textBuffer[end - 1])
					&& char.IsLowSurrogate(_textBuffer[end]))
				{
					end++;
				}
			}
			if (start < end && options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.AdjustCrlf))
			{
				start = GetTextElementStart(start);
			}

			var useObjectText = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseObjectText);
			var noHidden = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden);
			var includeNumbering = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.IncludeNumbering);
			var text = includeNumbering
				? GetNumberedTextInRange(start, end, useObjectText, noHidden)
				: useObjectText || noHidden
					? GetFilteredTextInRange(start, end, useObjectText, noHidden)
					: _textBuffer.Slice(start, end - start);
			if (includesFinalEop)
			{
				text += '\r';
			}

			return ConvertTextForGetOptions(text, options);
		}

		private string GetNumberedTextInRange(int start, int end, bool useObjectText, bool noHidden)
		{
			if (start >= end || _textBuffer.Length == 0)
			{
				return string.Empty;
			}

			SyncParagraphRunsToLength(_textBuffer.Length);
			var markerState = new ParagraphListMarkerState();
			var builder = new global::System.Text.StringBuilder();
			for (var paragraphStart = 0; paragraphStart < _textBuffer.Length && paragraphStart < end;)
			{
				var paragraphBreak = _textBuffer.IndexOf('\r', paragraphStart, _textBuffer.Length - paragraphStart);
				var paragraphEnd = paragraphBreak < 0 ? _textBuffer.Length : paragraphBreak + 1;
				var format = GetParagraphFormatAt(paragraphStart);
				var marker = ParagraphListMarker.GetNext(format, markerState, out var hasList);
				var intersectionStart = Math.Max(start, paragraphStart);
				var intersectionEnd = Math.Min(end, paragraphEnd);
				if (intersectionStart < intersectionEnd)
				{
					if (intersectionStart == paragraphStart && hasList && marker is not null)
					{
						builder.Append(marker).Append('\t');
					}

					builder.Append(useObjectText || noHidden
						? GetFilteredTextInRange(intersectionStart, intersectionEnd, useObjectText, noHidden)
						: _textBuffer.Slice(intersectionStart, intersectionEnd - intersectionStart));
				}

				paragraphStart = paragraphEnd;
			}

			return builder.ToString();
		}

		private string GetFilteredTextInRange(int start, int end, bool useObjectText, bool noHidden)
		{
			SyncRunsToLength(_textBuffer.Length);
			var builder = new global::System.Text.StringBuilder();
			if (start >= end)
			{
				return string.Empty;
			}

			var cursor = _runs.GetCursor(FindRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				var intersectionStart = Math.Max(start, runStart);
				var intersectionEnd = Math.Min(end, runEnd);
				var state = cursor.Current.Format;
				if (!(noHidden && state.Hidden))
				{
					if (useObjectText && state.InlineImage is { } image)
					{
						for (var i = intersectionStart; i < intersectionEnd; i++)
						{
							builder.Append(_textBuffer[i] == '\ufffc' ? image.AlternateText : _textBuffer[i]);
						}
					}
					else
					{
						_textBuffer.AppendTo(builder, intersectionStart, intersectionEnd - intersectionStart);
					}
				}
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}

			return builder.ToString();
		}

		/// <summary>
		/// Replaces the plain-text between <paramref name="start"/> and <paramref name="end"/> with
		/// <paramref name="replacement"/>, splices the formatting runs accordingly and re-renders. Used
		/// by <see cref="UnoTextRange"/> editing.
		/// </summary>
		internal int ReplaceRange(
			int start,
			int end,
			string replacement,
			UnoTextRange? sourceRange = null,
			bool unlink = false,
			bool unhide = false,
			TextHistoryKind historyKind = TextHistoryKind.None,
			bool forceHistory = false,
			bool checkTextLimit = true,
			bool unicodeBidi = false)
		{
			var originalLength = _textBuffer.Length;
			start = Math.Clamp(start, 0, originalLength);
			end = Math.Clamp(end, start, originalLength);
			ThrowIfTextRangeMutationNotEditable(start, end, sourceRange);
			var insert = NormalizeLineEndings(replacement ?? string.Empty);
			if (checkTextLimit)
			{
				insert = _owner.ClampInsertToMaxLength(
					insert,
					originalLength,
					start,
					end,
					preserveSurrogatePair: false);
			}

			if (IsMathMode)
			{
				if (historyKind == TextHistoryKind.Typing
					&& (_mathDocument is null
						|| _mathDocument.LiveInputText is not null
							&& start == end
							&& start == _mathDocument.Projection.Length))
				{
					var prospective = _mathDocument?.LiveInputText is { } currentLiveInput
						? currentLiveInput + insert
						: _textBuffer.Slice(0, start)
							+ insert
							+ _textBuffer.Slice(end, originalLength - end);
					if (prospective.Length <= MathDocument.MaxProjectionLength
						&& MathDocument.TryConvertUnicodeMath(prospective, out var convertedDocument))
					{
						var linearInput = prospective[..^1];
						var conversionStart = start;
						if (!string.Equals(_mathDocument?.LiveInputText, linearInput, StringComparison.Ordinal))
						{
							var linear = MathDocument.CreateLinearUnicodeMath(linearInput);
							ReplaceRangeWithMathDocument(
								new MathEditResult(
									linear,
									new MathTextSpan(0, originalLength),
									linear.Projection.Length,
									linear.Projection.Length),
								sourceRange,
								historyKind,
								forceHistory);
							if (sourceRange is UnoTextSelection typedSelection)
							{
								typedSelection.SetRangeInternal(
									linear.Projection.Length,
									linear.Projection.Length,
									selectionEndsAtTheStart: false);
								FinalizeHistorySelection();
								conversionStart = linear.Projection.Length;
							}
						}

						BreakHistoryCoalescing();
						return ReplaceRangeWithMathDocument(
							new MathEditResult(
								convertedDocument,
								new MathTextSpan(0, _textBuffer.Length),
								convertedDocument.Projection.Length,
								Math.Max(0, convertedDocument.Projection.Length - conversionStart)),
							sourceRange,
							historyKind,
							forceHistory);
					}

					if (prospective.Length <= MathDocument.MaxProjectionLength)
					{
						var converted = MathDocument.CreateLinearUnicodeMath(prospective);
						var edit = new MathEditResult(
							converted,
							new MathTextSpan(0, originalLength),
							converted.Projection.Length,
							Math.Max(0, converted.Projection.Length - start));
						return ReplaceRangeWithMathDocument(
							edit,
							sourceRange,
							historyKind,
							forceHistory);
					}
				}

				if (_mathDocument is { } mathDocument)
				{
					if (mathDocument.TouchesStructuralMarker(start, end))
					{
						return ReplaceStructuredMathWithPlainText(
							insert,
							sourceRange,
							historyKind,
							forceHistory);
					}
					if (mathDocument.TryApplyTextEdit(start, end, insert, out var mathEdit))
					{
						return ReplaceRangeWithMathDocument(
							mathEdit,
							sourceRange,
							historyKind,
							forceHistory);
					}
				}
			}

			var selection = (UnoTextSelection)Selection;
			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, selection);
			var selectionStartBefore = selection.StartPosition;
			var selectionEndBefore = selection.EndPosition;
			var selectionRebased = false;
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				var changed = MutateWithUndo(() =>
				{
					// Keep the run model aligned with the pre-edit text, then splice it in lock-step with the
					// text edit so inserted characters inherit the neighbouring formatting.
					SyncRunsToLength(originalLength);
					SyncParagraphRunsToLength(originalLength);
					SpliceRuns(start, end - start, insert.Length, sourceRange?.UsesForwardCharacterFormatting == true, unlink, unhide);
					SpliceParagraphRuns(_textBuffer, start, end - start, insert.Length);
					_preservedRtfMetadata = _preservedRtfMetadata.ApplyEdit(start, end - start, insert.Length);
					_preservedRtfMetadataEditApplied = true;
					_textBuffer.Replace(start, end - start, insert);
					if (unicodeBidi)
					{
						ApplyUnicodeBidiScripts(start, insert);
					}
					NormalizeParagraphRunsAroundEdit(start, insert.Length);
					_mathDocument = null;
					_mathMLUnavailable = false;
				}, () => RebaseRanges(start, end, insert.Length, sourceRange),
				new TextEdit(start, end - start, insert.Length),
				historyKind: historyKind,
				forceHistory: forceHistory);
				if (!changed)
				{
					RebaseRanges(start, end, insert.Length, sourceRange);
				}
				selectionRebased = selection.StartPosition != selectionStartBefore
					|| selection.EndPosition != selectionEndBefore;
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			if (selectionRebased)
			{
				_owner.OnDocumentTextChangedInteractive();
			}

			// The pending caret format (if any) has now been consumed by the splice above, or the caret
			// context has changed by an edit that didn't consume it; either way it no longer applies.
			ClearPendingCaretFormat();
			return insert.Length;
		}

		private int ReplaceRangeWithMathDocument(
			MathEditResult edit,
			UnoTextRange? sourceRange,
			TextHistoryKind historyKind,
			bool forceHistory)
		{
			var fragment = edit.Document.CreateFragment(DefaultFormatState(), DefaultParagraphState());
			var selection = (UnoTextSelection)Selection;
			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, selection);
			var selectionStartBefore = selection.StartPosition;
			var selectionEndBefore = selection.EndPosition;
			var selectionRebased = false;
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				var changed = MutateWithUndo(() =>
					{
						var characterRuns = BuildRunsFromFragment(
							fragment.CharacterRuns,
							fragment.Text.Length,
							DefaultFormatState());
						var paragraphRuns = BuildParagraphRunsFromFragment(
							fragment.ParagraphRuns,
							fragment.Text.Length,
							DefaultParagraphState());
						SetPlainTextCore(fragment.Text);
						SetRuns(characterRuns);
						SetParagraphRuns(paragraphRuns);
						_preservedRtfMetadata = RtfPreservedMetadata.Empty;
						_preservedRtfMetadataEditApplied = true;
						_terminalParagraphFormat = fragment.Text.Length > 0
							? GetParagraphFormatAt(fragment.Text.Length - 1).Clone()
							: DefaultParagraphState();
						_mathDocument = edit.Document;
						_mathMLUnavailable = false;
					},
					() => RebaseRanges(
						edit.ReplacedSpan.Start,
						edit.ReplacedSpan.End,
						edit.InsertedProjectionLength,
						sourceRange),
					new TextEdit(
						edit.ReplacedSpan.Start,
						edit.ReplacedSpan.Length,
						edit.InsertedProjectionLength),
					historyKind: historyKind,
					forceHistory: forceHistory);
				if (!changed)
				{
					RebaseRanges(
						edit.ReplacedSpan.Start,
						edit.ReplacedSpan.End,
						edit.InsertedProjectionLength,
						sourceRange);
				}
				selectionRebased = selection.StartPosition != selectionStartBefore
					|| selection.EndPosition != selectionEndBefore;
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			if (selectionRebased)
			{
				_owner.OnDocumentTextChangedInteractive();
			}
			ClearPendingCaretFormat();
			return edit.CallerInsertedLength;
		}

		private int ReplaceStructuredMathWithPlainText(
			string replacement,
			UnoTextRange? sourceRange,
			TextHistoryKind historyKind,
			bool forceHistory)
		{
			var originalLength = _textBuffer.Length;
			var fragment = RichTextFragment.CreateSingleRun(
				replacement,
				DefaultFormatState(),
				DefaultParagraphState());
			var selection = (UnoTextSelection)Selection;
			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, selection);
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				MutateWithUndo(() =>
					{
						SetPlainTextCore(replacement);
						_preservedRtfMetadata = RtfPreservedMetadata.Empty;
						_preservedRtfMetadataEditApplied = true;
						SetRuns(BuildRunsFromFragment(
							fragment.CharacterRuns,
							replacement.Length,
							DefaultFormatState()));
						SetParagraphRuns(BuildParagraphRunsFromFragment(
							fragment.ParagraphRuns,
							replacement.Length,
							DefaultParagraphState()));
						_terminalParagraphFormat = replacement.Length > 0
							? GetParagraphFormatAt(replacement.Length - 1).Clone()
							: DefaultParagraphState();
						_mathDocument = null;
						_mathMLUnavailable = true;
					},
					() => RebaseRanges(0, originalLength, replacement.Length, sourceRange),
					new TextEdit(0, originalLength, replacement.Length),
					historyKind: historyKind,
					forceHistory: forceHistory);
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			ClearPendingCaretFormat();
			return replacement.Length;
		}

		internal void TrackRange(UnoTextRange range)
		{
			range.SetRangeEditGeneration(_rangeEditGeneration);
			var registration = new TrackedTextRangeRegistration(range, _rangeEditGeneration);
			range.SetTrackingRegistration(registration);
			_ranges.Add(registration);
			AddTrackedRangeGeneration(_rangeEditGeneration);
			if (++_rangeRegistrationsSinceSweep >= RangeRegistrationSweepInterval)
			{
				SweepDeadRanges();
			}
		}

		internal string CoerceTypedText(string value) => _owner.CoerceCasing(NormalizeLineEndings(value));

		internal string NormalizeImportedPlainText(string value, int start, int end)
		{
			var importLimit = GetClipboardImportCharacterLimit(start, end);
			if (importLimit == 0 || value.Length == 0)
			{
				return string.Empty;
			}

			// CRLF is the only normalization that contracts input, so twice the output limit plus
			// one UTF-16 scalar is enough source to produce the entire bounded result.
			var sourceLimit = Math.Min(value.Length, checked(importLimit * 2 + 2));
			var boundedSource = TextUnitNavigation.TruncateToUtf16Boundary(value, sourceLimit);
			return TextUnitNavigation.TruncateToUtf16Boundary(CoerceTypedText(boundedSource), importLimit);
		}

		internal int GetImportCharacterLimit(int start, int end)
		{
			var maxImportLength = GetRtfImportSafetyLimit();
			var maxLength = _owner.MaxLength;
			return maxLength <= 0
				? maxImportLength
				: Math.Min(maxImportLength, Math.Max(0, maxLength - (_textBuffer.Length - (end - start))));
		}

		internal int GetClipboardImportCharacterLimit(int start, int end)
			=> GetImportCharacterLimit(start, end);

		internal bool ShouldTruncateClipboardImportAtLimit(int start, int end)
			=> _owner.MaxLength > 0
				&& Math.Max(0, _owner.MaxLength - (_textBuffer.Length - (end - start))) < GetRtfImportSafetyLimit();

		private static int GetRtfImportSafetyLimit()
			=> global::Uno.UI.FeatureConfiguration.RichEditBox.MaxRtfImportCharacters;

		internal bool IsSelectionMutationInProgress => _selectionMutationDepth > 0;

		internal long SelectionChangeVersion => _selectionChangeVersion;

		internal bool IsOwnerReadOnly => _owner.IsReadOnly;

		internal RichTextFragment CaptureFragment(int start, int end)
		{
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			SyncRunsToLength(_textBuffer.Length);
			SyncParagraphRunsToLength(_textBuffer.Length);

			return CaptureFragment(start, end, _textBuffer.Slice(start, end - start));
		}

		private RichTextFragment CaptureFragment(int start, int end, string text)
		{
			return new RichTextFragment(
				text,
				CaptureCharacterRuns(start, end),
				CaptureParagraphRuns(start, end),
				GetTerminalParagraphForFragment(start, end),
				preservedRtfMetadata: _preservedRtfMetadata.Slice(start, end - start));
		}

		private bool CanPossiblyEncodeRtf(int start, int end)
		{
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			SyncRunsToLength(_textBuffer.Length);
			var minimumLength = 256L + end - start;
			if (start < end)
			{
				var cursor = _runs.GetCursor(FindRunIndex(start));
				while (cursor.IsValid && cursor.Start < end)
				{
					var intersectionLength = Math.Min(end, cursor.End) - Math.Max(start, cursor.Start);
					if (cursor.Current.Format.InlineImage is { } image)
					{
						minimumLength = checked(
							minimumLength + (2L * image.EncodedLength + 32) * intersectionLength);
					}
					else
					{
						minimumLength = checked(minimumLength + 4);
					}
					if (minimumLength > RichTextRtfCodec.MaxRtfOutputLength)
					{
						return false;
					}
					cursor.MoveNext();
				}
			}

			return minimumLength <= RichTextRtfCodec.MaxRtfOutputLength;
		}

		private List<FormatRun> CaptureCharacterRuns(int start, int end)
		{
			var result = new List<FormatRun>();
			if (start >= end)
			{
				return result;
			}

			var cursor = _runs.GetCursor(FindRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				var count = Math.Min(end, runEnd) - Math.Max(start, runStart);
				AppendRun(result, count, cursor.Current.Format);
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}

			return result;
		}

		private List<ParagraphRun> CaptureParagraphRuns(int start, int end)
		{
			var result = new List<ParagraphRun>();
			if (start >= end)
			{
				return result;
			}

			var cursor = _paragraphRuns.GetCursor(FindParagraphRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				var count = Math.Min(end, runEnd) - Math.Max(start, runStart);
				AppendParagraphRun(result, count, cursor.Current.Format);
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}

			return result;
		}

		private ParagraphFormatState GetTerminalParagraphForFragment(int start, int end)
		{
			if (end == _textBuffer.Length)
			{
				return _terminalParagraphFormat.Clone();
			}

			if (end > start)
			{
				return GetParagraphFormatAt(end - 1).Clone();
			}

			return end < _textBuffer.Length
				? GetParagraphFormatAt(end).Clone()
				: _terminalParagraphFormat.Clone();
		}

		internal RichTextFragment CaptureFragment(int start, int end, bool noHidden)
		{
			if (!noHidden)
			{
				return CaptureFragment(start, end);
			}

			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			SyncRunsToLength(_textBuffer.Length);
			SyncParagraphRunsToLength(_textBuffer.Length);
			var text = new global::System.Text.StringBuilder(end - start);
			var characterRuns = new List<FormatRun>();
			var paragraphRuns = new List<ParagraphRun>();
			if (start < end)
			{
				var characterRunIndex = FindRunIndex(start);
				var paragraphRunIndex = FindParagraphRunIndex(start);
				var position = start;
				while (position < end)
				{
					var characterEnd = _runs.GetEnd(characterRunIndex);
					var paragraphEnd = _paragraphRuns.GetEnd(paragraphRunIndex);
					var segmentEnd = Math.Min(end, Math.Min(characterEnd, paragraphEnd));
					var character = _runs[characterRunIndex].Format;
					var paragraph = _paragraphRuns[paragraphRunIndex].Format;
					if (!character.Hidden)
					{
						var segmentLength = segmentEnd - position;
						_textBuffer.AppendTo(text, position, segmentLength);
						AppendRun(characterRuns, segmentLength, character);
						AppendParagraphRun(paragraphRuns, segmentLength, paragraph);
					}

					position = segmentEnd;
					if (position == characterEnd)
					{
						characterRunIndex++;
					}
					if (position == paragraphEnd)
					{
						paragraphRunIndex++;
					}
				}
			}

			return new RichTextFragment(
				text.ToString(),
				characterRuns,
				paragraphRuns,
				GetTerminalParagraphForFragment(start, end),
				true);
		}

		internal RichTextFragment CreateInlineImageFragment(int start, InlineImageState image)
		{
			start = Math.Clamp(start, 0, _textBuffer.Length);
			SyncParagraphRunsToLength(_textBuffer.Length);
			var paragraphBasis = start < _textBuffer.Length && TextUnitNavigation.IsParagraphStart(_textBuffer, start)
				? GetParagraphFormatAt(start)
				: start == _textBuffer.Length
					? _terminalParagraphFormat
					: start > 0
						? GetParagraphFormatAt(start - 1)
						: (_textBuffer.Length > 0 ? GetParagraphFormatAt(0) : _terminalParagraphFormat);
			return RichTextFragment.CreateSingleRun(
				"\ufffc",
				new CharacterFormatState
				{
					InlineImage = image,
					TextObjectIdentity = new RichEditTextObjectIdentity(),
				},
				paragraphBasis);
		}

		internal int ReplaceRangeWithFragment(
			int start,
			int end,
			RichTextFragment fragment,
			UnoTextRange? sourceRange,
			bool unhide = false,
			bool unlink = false,
			TextHistoryKind historyKind = TextHistoryKind.None,
			bool forceHistory = false,
			bool checkTextLimit = true)
		{
			ValidateFragment(fragment);
			var originalLength = _textBuffer.Length;
			start = Math.Clamp(start, 0, originalLength);
			end = Math.Clamp(end, start, originalLength);
			ValidateResultingImageBudget(start, end, fragment);
			ThrowIfTextRangeMutationNotEditable(start, end, sourceRange);
			var insert = checkTextLimit
				? _owner.ClampInsertToMaxLength(
					fragment.Text,
					originalLength,
					start,
					end,
					preserveSurrogatePair: false)
				: fragment.Text;
			var insertedLength = insert.Length;
			var selection = (UnoTextSelection)Selection;
			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, selection);
			var selectionStartBefore = selection.StartPosition;
			var selectionEndBefore = selection.EndPosition;
			var selectionRebased = false;
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				var changed = MutateWithUndo(() =>
				{
					SyncRunsToLength(originalLength);
					SyncParagraphRunsToLength(originalLength);
					var characterBasis = start > 0
						? GetFormatAt(start - 1)
						: (originalLength > 0 ? GetFormatAt(0) : DefaultFormatState());
					var paragraphBasis = start < originalLength && TextUnitNavigation.IsParagraphStart(_textBuffer, start)
						? GetParagraphFormatAt(start)
						: start == originalLength
							? _terminalParagraphFormat
							: start > 0
								? GetParagraphFormatAt(start - 1)
								: (originalLength > 0 ? GetParagraphFormatAt(0) : _terminalParagraphFormat);
					var insertedCharacterRuns = BuildRunsFromFragment(fragment.CharacterRuns, insertedLength, characterBasis, unhide, unlink);
					var insertedParagraphRuns = BuildParagraphRunsFromFragment(fragment.ParagraphRuns, insertedLength, paragraphBasis);
					ReplaceRuns(start, end, insertedCharacterRuns);
					ReplaceParagraphRuns(start, end, insertedParagraphRuns);

					var insertedMetadata = insertedLength == fragment.Text.Length
						? fragment.PreservedRtfMetadata
						: RtfPreservedMetadata.Empty;
					_preservedRtfMetadata = _preservedRtfMetadata.ApplyEdit(
						start,
						end - start,
						insertedLength,
						insertedMetadata);
					_preservedRtfMetadataEditApplied = true;
					_textBuffer.Replace(start, end - start, insert);
					if (end == originalLength)
					{
						_terminalParagraphFormat = insertedLength == fragment.Text.Length && fragment.HasExplicitTerminalParagraphState
							? fragment.TerminalParagraphState.Clone()
							: insertedLength > 0
								? GetParagraphFormatAt(start + insertedLength - 1).Clone()
								: paragraphBasis.Clone();
					}
					NormalizeParagraphRunsAroundEdit(start, insertedLength);
					_mathDocument = null;
					_mathMLUnavailable = false;
				}, () => RebaseRanges(start, end, insertedLength, sourceRange),
				new TextEdit(start, end - start, insertedLength),
				historyKind: historyKind,
				forceHistory: forceHistory);
				if (!changed)
				{
					RebaseRanges(start, end, insertedLength, sourceRange);
				}
				selectionRebased = selection.StartPosition != selectionStartBefore
					|| selection.EndPosition != selectionEndBefore;
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			if (selectionRebased)
			{
				_owner.OnDocumentTextChangedInteractive();
			}

			ClearPendingCaretFormat();
			return insertedLength;
		}

		internal bool IsRangeProtected(int start, int end, bool preferForwardAtCaret = false)
		{
			SyncRunsToLength(_textBuffer.Length);
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			if (start == end)
			{
				if (_textBuffer.Length == 0)
				{
					return false;
				}

				var index = preferForwardAtCaret && start < _textBuffer.Length
					? start
					: start > 0 ? start - 1 : 0;
				return GetFormatAt(index).ProtectedText;
			}

			var runIndex = FindRunIndex(start);
			while (runIndex < _runs.Count && GetRunStart(runIndex) < end)
			{
				if (_runs[runIndex].Format.ProtectedText)
				{
					return true;
				}
				runIndex++;
			}

			return false;
		}

		internal void ThrowIfNotEditable(int start, int end, bool preferForwardAtCaret = false)
		{
			if (IsOwnerReadOnly || IsRangeProtected(start, end, preferForwardAtCaret))
			{
				throw new UnauthorizedAccessException("The text range cannot be edited.");
			}
		}

		private void ThrowIfTextRangeMutationNotEditable(int start, int end, UnoTextRange? sourceRange)
		{
			if (IsOwnerReadOnly
				|| sourceRange is null && IsRangeProtected(start, end))
			{
				throw new UnauthorizedAccessException("The text range cannot be edited.");
			}
		}

		private void SetDocumentFragment(
			RichTextFragment fragment,
			MathDocument? mathDocument = null,
			bool forceHistory = false,
			bool checkTextLimit = true)
		{
			var oldLength = _textBuffer.Length;
			ThrowIfNotEditable(0, oldLength);
			ValidateFragment(fragment);
			var text = NormalizeRichTextLineEndings(fragment.Text);
			var maxLength = checkTextLimit ? _owner.MaxLength : 0;
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = TextUnitNavigation.TruncateToUtf16Limit(text, maxLength);
			}

			var selection = (UnoTextSelection)Selection;
			var selectionWasNonzero = selection.StartPosition != 0 || selection.EndPosition != 0;
			var documentChanged = MutateWithUndo(() =>
			{
				var characterRuns = BuildRunsFromFragment(fragment.CharacterRuns, text.Length, DefaultFormatState());
				var paragraphRuns = BuildParagraphRunsFromFragment(fragment.ParagraphRuns, text.Length, DefaultParagraphState());

				SetPlainTextCore(text);
				SetRuns(characterRuns);
				SetParagraphRuns(paragraphRuns);
				_preservedRtfMetadata = text.Length == fragment.Text.Length
					? fragment.PreservedRtfMetadata
					: fragment.PreservedRtfMetadata.Slice(0, text.Length);
				_preservedRtfMetadataEditApplied = true;
				_terminalParagraphFormat = text.Length == fragment.Text.Length && fragment.HasExplicitTerminalParagraphState
					? fragment.TerminalParagraphState.Clone()
					: text.Length > 0 ? GetParagraphFormatAt(text.Length - 1).Clone() : DefaultParagraphState();
				_mathDocument = mathDocument;
				_mathMLUnavailable = false;
				RebaseRanges(0, oldLength, text.Length, sourceRange: null);
				selection.SetRangeInternal(0, 0, selectionEndsAtTheStart: false);
			}, textEdit: new TextEdit(0, oldLength, text.Length), forceHistory: forceHistory);
			if (!documentChanged && selectionWasNonzero)
			{
				_owner.OnDocumentTextChangedInteractive();
			}
			ClearPendingCaretFormat();
		}

		private static void ValidateFragment(RichTextFragment fragment)
		{
			if (!fragment.AreRunInvariantsValid())
			{
				throw new ArgumentException("The rich-text formatting runs are inconsistent.", nameof(fragment));
			}

			var images = 0;
			var imageBytes = 0;
			long imagePixels = 0;
			foreach (var run in fragment.CharacterRuns)
			{
				if (run.Format.InlineImage is { } image)
				{
					AddImageCost(image, run.Length, ref images, ref imageBytes, ref imagePixels);
				}
			}

			foreach (var run in fragment.ParagraphRuns)
			{
				if (run.Format.Tabs.Count > ParagraphFormatState.MaxTabs)
				{
					throw new ArgumentException("The rich text contains too many paragraph tabs.", nameof(fragment));
				}
			}

			if (images > MaxFragmentImageCount
				|| imageBytes > MaxFragmentImageBytes
				|| imagePixels > MaxFragmentImagePixels)
			{
				throw new ArgumentException("The rich text contains too much image data.", nameof(fragment));
			}
		}

		private void ValidateResultingImageBudget(int start, int end, RichTextFragment fragment)
		{
			SyncRunsToLength(_textBuffer.Length);
			var images = 0;
			var imageBytes = 0;
			long imagePixels = 0;
			for (var runIndex = 0; runIndex < _runs.Count; runIndex++)
			{
				if (_runs[runIndex].Format.InlineImage is not { } image)
				{
					continue;
				}

				var runStart = GetRunStart(runIndex);
				var runEnd = _runs.GetEnd(runIndex);
				var retained = Math.Max(0, Math.Min(start, runEnd) - runStart)
					+ Math.Max(0, runEnd - Math.Max(end, runStart));
				AddImageCost(image, retained, ref images, ref imageBytes, ref imagePixels);
			}

			foreach (var run in fragment.CharacterRuns)
			{
				if (run.Format.InlineImage is { } image)
				{
					AddImageCost(image, run.Length, ref images, ref imageBytes, ref imagePixels);
				}
			}

			if (images > MaxFragmentImageCount
				|| imageBytes > MaxFragmentImageBytes
				|| imagePixels > MaxFragmentImagePixels)
			{
				throw new ArgumentException("The document contains too much image data.", nameof(fragment));
			}
		}

		private static void AddImageCost(
			InlineImageState image,
			int occurrences,
			ref int images,
			ref int bytes,
			ref long pixels)
		{
			images = checked(images + occurrences);
			bytes = checked(bytes + image.EncodedLength * occurrences);
			pixels = checked(pixels + image.GetDecodedPixelCount() * occurrences);
		}

		private readonly record struct RangeEditDelta(
			int Start,
			int End,
			int InsertLength,
			int DocumentLength);

		private abstract record RangeEditLogSegment(long BaseGeneration, int Count)
		{
			internal long EndGeneration => BaseGeneration + Count;
		}

		private sealed record RangeEditLogLeaf(long BaseGeneration, RangeEditDelta[] Deltas)
			: RangeEditLogSegment(BaseGeneration, Deltas.Length);

		private sealed record RangeEditLogBranch(
			long BaseGeneration,
			RangeEditLogSegment Left,
			RangeEditLogSegment Right)
			: RangeEditLogSegment(BaseGeneration, checked(Left.Count + Right.Count));

		internal void EnsureRangeCurrent(UnoTextRange range)
		{
			var generation = range.RangeEditGeneration;
			if (generation == _rangeEditGeneration)
			{
				return;
			}

			var retainedBaseGeneration = GetRetainedRangeEditBaseGeneration();
			if (generation < retainedBaseGeneration)
			{
				throw new InvalidOperationException("The tracked range edit generation is no longer available.");
			}

			var originalGeneration = generation;
			foreach (var segment in _rangeEditLogSegments)
			{
				ApplyRangeEditSegment(segment, range, ref generation);
			}

			var firstDelta = checked((int)Math.Max(0, generation - _rangeEditLogBaseGeneration));
			for (var i = firstDelta; i < _rangeEditLog.Count; i++)
			{
				var delta = _rangeEditLog[i];
				range.RebaseAfterEdit(
					delta.Start,
					delta.End,
					delta.InsertLength,
					delta.DocumentLength,
					_rangeEditLogBaseGeneration + i + 1);
				_rangeRebaseApplicationCount++;
				generation = _rangeEditLogBaseGeneration + i + 1;
			}
			UpdateTrackedRangeGeneration(range, originalGeneration, generation);
		}

		internal void CompactTrackedRangesForTesting()
		{
			CompactRangeEditLog();
			SweepDeadRanges();
		}

		private void RebaseRanges(int editStart, int editEnd, int insertLength, UnoTextRange? sourceRange, int? documentLength = null)
		{
			if (!_preservedRtfMetadataEditApplied)
			{
				_preservedRtfMetadata = _preservedRtfMetadata.ApplyEdit(
					editStart,
					editEnd - editStart,
					insertLength);
			}
			_preservedRtfMetadataEditApplied = false;
			var rebasedDocumentLength = documentLength ?? _textBuffer.Length;
			_rangeEditLog.Add(new RangeEditDelta(editStart, editEnd, insertLength, rebasedDocumentLength));
			_rangeEditGeneration++;

			if (sourceRange is not null)
			{
				var sourceGeneration = sourceRange.RangeEditGeneration;
				sourceRange.SetRangeEditGeneration(_rangeEditGeneration);
				UpdateTrackedRangeGeneration(sourceRange, sourceGeneration, _rangeEditGeneration);
			}
			if (_selection is { } selection && !ReferenceEquals(selection, sourceRange))
			{
				EnsureRangeCurrent(selection);
			}

			if (_rangeEditLog.Count >= MaxRangeEditLogEntries)
			{
				CompactRangeEditLog();
			}
		}

		private void CompactRangeEditLog()
		{
			if (_rangeEditLog.Count == 0)
			{
				return;
			}

			_rangeEditLogSegments.Add(new RangeEditLogLeaf(
				_rangeEditLogBaseGeneration,
				_rangeEditLog.ToArray()));
			_rangeEditLog.Clear();
			_rangeEditLogBaseGeneration = _rangeEditGeneration;
			_rangeEditLogCompactionCount++;

			while (_rangeEditLogSegments.Count >= 2)
			{
				var right = _rangeEditLogSegments[^1];
				var left = _rangeEditLogSegments[^2];
				if (left.Count != right.Count || left.EndGeneration != right.BaseGeneration)
				{
					break;
				}

				_rangeEditLogSegments.RemoveRange(_rangeEditLogSegments.Count - 2, 2);
				_rangeEditLogSegments.Add(new RangeEditLogBranch(left.BaseGeneration, left, right));
			}

			TrimRangeEditLog();
		}

		private void ApplyRangeEditSegment(
			RangeEditLogSegment segment,
			UnoTextRange range,
			ref long generation)
		{
			if (generation >= segment.EndGeneration)
			{
				return;
			}

			if (segment is RangeEditLogBranch branch)
			{
				ApplyRangeEditSegment(branch.Left, range, ref generation);
				ApplyRangeEditSegment(branch.Right, range, ref generation);
				return;
			}

			var leaf = (RangeEditLogLeaf)segment;
			var firstDelta = checked((int)Math.Max(0, generation - leaf.BaseGeneration));
			for (var i = firstDelta; i < leaf.Deltas.Length; i++)
			{
				var delta = leaf.Deltas[i];
				var appliedGeneration = leaf.BaseGeneration + i + 1;
				range.RebaseAfterEdit(
					delta.Start,
					delta.End,
					delta.InsertLength,
					delta.DocumentLength,
					appliedGeneration);
				_rangeRebaseApplicationCount++;
				generation = appliedGeneration;
			}
		}

		private long GetRetainedRangeEditBaseGeneration()
			=> _rangeEditLogSegments.Count == 0
				? _rangeEditLogBaseGeneration
				: _rangeEditLogSegments[0].BaseGeneration;

		private void AddTrackedRangeGeneration(long generation)
		{
			_trackedRangeGenerations.TryGetValue(generation, out var count);
			_trackedRangeGenerations[generation] = count + 1;
			_minimumTrackedRangeGeneration = Math.Min(_minimumTrackedRangeGeneration, generation);
		}

		private void RemoveTrackedRangeGeneration(long generation)
		{
			if (!_trackedRangeGenerations.TryGetValue(generation, out var count))
			{
				return;
			}
			if (count == 1)
			{
				_trackedRangeGenerations.Remove(generation);
				if (generation == _minimumTrackedRangeGeneration)
				{
					RecomputeMinimumTrackedRangeGeneration();
				}
			}
			else
			{
				_trackedRangeGenerations[generation] = count - 1;
			}
		}

		private void RecomputeMinimumTrackedRangeGeneration()
		{
			_minimumTrackedRangeGeneration = long.MaxValue;
			foreach (var generation in _trackedRangeGenerations.Keys)
			{
				_minimumTrackedRangeGeneration = Math.Min(_minimumTrackedRangeGeneration, generation);
			}
		}

		private void UpdateTrackedRangeGeneration(UnoTextRange range, long oldGeneration, long newGeneration)
		{
			if (oldGeneration == newGeneration || range.TrackingRegistration is not { } registration)
			{
				return;
			}

			RemoveTrackedRangeGeneration(registration.Generation);
			registration.Generation = newGeneration;
			AddTrackedRangeGeneration(newGeneration);
		}

		private void TrimRangeEditLog()
		{
			var minimumGeneration = Math.Min(_rangeEditGeneration, _minimumTrackedRangeGeneration);

			for (var i = 0; i < _rangeEditLogSegments.Count;)
			{
				var trimmed = TrimRangeEditSegment(_rangeEditLogSegments[i], minimumGeneration);
				if (trimmed is null)
				{
					_rangeEditLogSegments.RemoveAt(i);
				}
				else
				{
					_rangeEditLogSegments[i] = trimmed;
					break;
				}
			}
		}

		private static RangeEditLogSegment? TrimRangeEditSegment(
			RangeEditLogSegment segment,
			long minimumGeneration)
		{
			if (minimumGeneration <= segment.BaseGeneration)
			{
				return segment;
			}
			if (minimumGeneration >= segment.EndGeneration)
			{
				return null;
			}
			if (segment is RangeEditLogBranch branch)
			{
				var left = TrimRangeEditSegment(branch.Left, minimumGeneration);
				var right = TrimRangeEditSegment(branch.Right, minimumGeneration);
				if (left is null)
				{
					return right;
				}
				if (right is null)
				{
					return left;
				}
				return new RangeEditLogBranch(left.BaseGeneration, left, right);
			}

			var leaf = (RangeEditLogLeaf)segment;
			var offset = checked((int)(minimumGeneration - leaf.BaseGeneration));
			var deltas = new RangeEditDelta[leaf.Deltas.Length - offset];
			Array.Copy(leaf.Deltas, offset, deltas, 0, deltas.Length);
			return new RangeEditLogLeaf(minimumGeneration, deltas);
		}

		private void SweepDeadRanges()
		{
			for (var i = _ranges.Count - 1; i >= 0; i--)
			{
				var registration = _ranges[i];
				if (!registration.Range.TryGetTarget(out _))
				{
					_ranges.RemoveAt(i);
					RemoveTrackedRangeGeneration(registration.Generation);
					_deadRangeCleanupCount++;
				}
			}

			_rangeRegistrationsSinceSweep = 0;
		}

		// Trigger a re-render of the shared DisplayBlock, deferring while display updates are batched.
		private void RequestRender(bool isContentChanging)
		{
			_automationVersion++;
			if (_batchDepth > 0)
			{
				_pendingRender = true;
				_pendingContentChange |= isContentChanging;
				return;
			}

			_owner.OnDocumentTextChanged(isContentChanging);
		}

		/// <summary>
		/// Gets a text range for the specified range of text positions.
		/// </summary>
		public global::Microsoft.UI.Text.ITextRange GetRange(int startPosition, int endPosition)
			=> new UnoTextRange(this, startPosition, endPosition);

		/// <summary>
		/// Gets a degenerate text range at the character position nearest the specified point.
		/// </summary>
		public global::Microsoft.UI.Text.ITextRange GetRangeFromPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options)
		{
			const global::Microsoft.UI.Text.PointOptions invalidOptions =
				global::Microsoft.UI.Text.PointOptions.AllowOffClient
				| global::Microsoft.UI.Text.PointOptions.NoHorizontalScroll
				| global::Microsoft.UI.Text.PointOptions.NoVerticalScroll;
			if ((options & invalidOptions) != 0)
			{
				throw new ArgumentException(nameof(options));
			}

			if (_owner.TryGetIndexFromPoint(point, options, out var index))
			{
				return GetRange(index, index);
			}

			return GetRange(0, 0);
		}

		/// <summary>
		/// Gets the current text selection as an <see cref="ITextSelection"/>.
		/// </summary>
		public global::Microsoft.UI.Text.ITextSelection Selection => _selection ??= new UnoTextSelection(this);

		/// <summary>
		/// Mirrors the owning control's interactive caret/selection into <see cref="Selection"/> without
		/// triggering the drag semantics of the public position setters. Used by the interactive editor.
		/// </summary>
		internal void SetSelectionRangeInternal(int start, int end, bool clearPendingCaretFormat = true, bool selectionEndsAtTheStart = false)
		{
			if (clearPendingCaretFormat)
			{
				ClearPendingCaretFormatIfMoved(start, end);
			}

			((UnoTextSelection)Selection).SetRangeInternal(start, end, selectionEndsAtTheStart);
		}

		/// <summary>
		/// Raised by <see cref="UnoTextSelection"/> when the programmatic selection changes through the
		/// public API, so the owning control can sync its interactive caret/selection and re-render.
		/// This is the reverse of <see cref="SetSelectionRangeInternal"/> and is not called by it.
		/// </summary>
		internal void NotifySelectionChanged()
		{
			// The owner resolves SelectionChanging cancellation/reentrancy before deciding whether the
			// accepted selection moved away from a pending insertion-point format.
			_selectionChangeVersion++;
			_owner.OnTomSelectionChanged();
		}

		internal void NotifySelectionDirectionChanged()
			=> _owner.OnTomSelectionDirectionChanged();

		// Programmatic Selection.Copy/Cut/Paste (ITextSelection) route here so the owning control raises
		// its CopyingToClipboard / CuttingToClipboard / Paste events. They operate directly on the TOM
		// selection even when the control is unfocused, without changing its interactive direction.
		internal void CopySelectionToClipboardViaControl(UnoTextSelection selection)
			=> _owner.CopyTomSelectionToClipboard(selection);

		internal void CutSelectionToClipboardViaControl(UnoTextSelection selection)
		{
			_owner.CutTomSelectionToClipboard(selection);
		}

		internal bool TryBeginSelectionPasteViaControl()
		{
			if (!_owner.TryBeginTomSelectionPaste())
			{
				return false;
			}

			// WinUI paste is synchronous. Uno's clipboard read is asynchronous, but scheduling a
			// selection paste is still an explicit handler mutation and therefore overrides Cancel.
			_selectionChangeVersion++;
			return true;
		}

		// --- Geometry-backed line navigation (delegates to the owning control's DisplayBlock layout) ---

		internal bool TryGetLineBounds(int position, out int lineStart, out int lineEnd, out int lineIndex, out bool isLast)
			=> _owner.TryGetLineBounds(Math.Clamp(position, 0, TextLength), out lineStart, out lineEnd, out lineIndex, out isLast);

		internal bool TryGetLineBounds(
			int position,
			bool atEndOfLine,
			out int lineStart,
			out int lineEnd,
			out int lineIndex,
			out bool isLast)
			=> _owner.TryGetLineBounds(
				Math.Clamp(position, 0, TextLength),
				atEndOfLine,
				out lineStart,
				out lineEnd,
				out lineIndex,
				out isLast);

		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetVerticalTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetVerticalTarget(
			int position,
			bool up,
			int count,
			bool atEndOfLine,
			ref double? desiredX,
			out int target,
			out int unitsMoved,
			out bool targetAtEndOfLine)
			=> _owner.TryGetVerticalTarget(
				position,
				up,
				count,
				atEndOfLine,
				ref desiredX,
				out target,
				out unitsMoved,
				out targetAtEndOfLine);

		internal bool TryGetPageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetPageTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetPageTarget(
			int position,
			bool up,
			int count,
			bool atEndOfLine,
			ref double? desiredX,
			out int target,
			out int unitsMoved,
			out bool targetAtEndOfLine)
			=> _owner.TryGetPageTarget(
				position,
				up,
				count,
				atEndOfLine,
				ref desiredX,
				out target,
				out unitsMoved,
				out targetAtEndOfLine);

		internal bool TryGetRangePageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetRangePageTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetVisibleRange(out int start, out int end)
			=> _owner.TryGetVisibleRange(out start, out end);

		internal bool IsVisualLineEnd(int position)
			=> _owner.IsVisualLineEnd(position);

		// --- Geometry-backed coordinate mapping (delegates to the owning control's DisplayBlock layout) ---

		internal bool TryGetIndexRect(int index, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetIndexRect(Math.Clamp(index, 0, TextLength), options, out rect);

		internal bool TryGetIndexBaseline(int index, global::Microsoft.UI.Text.PointOptions options, out double baseline)
			=> _owner.TryGetIndexBaseline(Math.Clamp(index, 0, TextLength), options, out baseline);

		internal bool TryGetRangeRect(int start, int end, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetRangeRect(
				Math.Clamp(start, 0, TextLength),
				Math.Clamp(end, 0, StoryLength),
				options,
				out rect);

		internal bool TryGetRangeGeometry(
			int start,
			int end,
			global::Microsoft.UI.Text.PointOptions options,
			bool isSelection,
			out global::Microsoft.UI.Xaml.Controls.RichEditTextGeometryHitResult result)
			=> _owner.TryGetRangeGeometry(
				Math.Clamp(start, 0, TextLength),
				Math.Clamp(end, 0, StoryLength),
				options,
				isSelection,
				out result);

		internal bool TryGetIndexFromPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, out int index)
			=> _owner.TryGetIndexFromPoint(point, options, out index);

		internal bool TryScrollRangeIntoView(int start, int end, global::Microsoft.UI.Text.PointOptions options)
			=> _owner.TryScrollRangeIntoView(
				Math.Clamp(start, 0, TextLength),
				Math.Clamp(end, 0, TextLength),
				options);

		/// <summary>
		/// Sets the text in this document to the specified plain text.
		/// </summary>
		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			ThrowIfNotEditable(0, _textBuffer.Length);

			if (options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf))
			{
				MathDocument? mathDocument = null;
				RichTextFragment fragment;
				if (string.IsNullOrEmpty(value))
				{
					fragment = RichTextFragment.Empty();
				}
				else if (IsMathMode
					&& RichTextRtfCodec.TryReadMath(
						value,
						DefaultFormatState(),
						DefaultParagraphState(),
						GetSetTextImportCharacterLimit(0, _textBuffer.Length, options),
						ShouldTruncateSetTextImportAtLimit(0, _textBuffer.Length, options),
						out mathDocument,
						out var mathFragment))
				{
					fragment = mathFragment;
				}
				else
				{
					fragment = RichTextRtfCodec.Read(
						value,
						GetSetTextImportCharacterLimit(0, _textBuffer.Length, options),
						ShouldTruncateSetTextImportAtLimit(0, _textBuffer.Length, options));
				}
				fragment = ApplyRtfSetOptions(fragment, options);
				SetDocumentFragment(
					fragment,
					mathDocument,
					forceHistory: true,
					checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit));
				return;
			}

			var text = NormalizeLineEndings(value ?? string.Empty);

			// TOM only observes MaxLength when CheckTextLimit is requested.
			var maxLength = options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit)
				? _owner.MaxLength
				: 0;
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = TextUnitNavigation.TruncateToUtf16Limit(text, maxLength);
			}

			var oldLength = _textBuffer.Length;
			var selection = (UnoTextSelection)Selection;
			var selectionWasNonzero = selection.StartPosition != 0 || selection.EndPosition != 0;
			var documentChanged = MutateWithUndo(() =>
			{
				SetPlainTextCore(text);
				ResetRuns(text.Length);
				_preservedRtfMetadata = RtfPreservedMetadata.Empty;
				_preservedRtfMetadataEditApplied = true;
				if (options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.UnicodeBidi))
				{
					ApplyUnicodeBidiScripts(0, text);
				}
				ResetParagraphRuns(text.Length);
				_mathDocument = null;
				_mathMLUnavailable = false;
				RebaseRanges(0, oldLength, text.Length, sourceRange: null);
				selection.SetRangeInternal(0, 0, selectionEndsAtTheStart: false);
			}, textEdit: new TextEdit(0, oldLength, text.Length), forceHistory: true);

			// A same-text/default-format SetText is a content no-op, but it still resets the selection.
			// Since MutateWithUndo does not render that case, publish the selection proposal explicitly.
			if (!documentChanged && selectionWasNonzero)
			{
				_owner.OnDocumentTextChangedInteractive();
			}
		}

		internal int GetSetTextImportCharacterLimit(
			int start,
			int end,
			global::Microsoft.UI.Text.TextSetOptions options)
			=> options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit)
				? GetImportCharacterLimit(start, end)
				: GetRtfImportSafetyLimit();

		internal bool ShouldTruncateSetTextImportAtLimit(
			int start,
			int end,
			global::Microsoft.UI.Text.TextSetOptions options)
			=> options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit)
				&& _owner.MaxLength > 0
				&& Math.Max(0, _owner.MaxLength - (_textBuffer.Length - (end - start))) < GetRtfImportSafetyLimit();

		private RichTextFragment ApplyRtfSetOptions(
			RichTextFragment fragment,
			global::Microsoft.UI.Text.TextSetOptions options)
		{
			var unlink = options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unlink);
			var unhide = options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unhide);
			if (unlink || unhide)
			{
				fragment = fragment.TransformCharacterFormats(state =>
				{
					if (unlink)
					{
						state.Link = null;
						state.LinkAnchor = null;
						state.TextObjectIdentity = null;
					}
					if (unhide)
					{
						state.Hidden = false;
					}
				});
			}

			if (fragment.Text.Length == 0 || fragment.Text[^1] != '\r')
			{
				return fragment;
			}

			if (!options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.ApplyRtfDocumentDefaults))
			{
				return fragment.PreservesTerminalParagraphStateOnImport
					? fragment
					: fragment.WithTerminalParagraph(DefaultParagraphState());
			}

			var terminal = fragment.GetParagraphFormatAt(fragment.Text.Length - 1).Clone();
			return fragment.Slice(0, fragment.Text.Length - 1, terminal, hasExplicitTerminalParagraphState: true);
		}

		/// <summary>
		/// Gets the text in this document as plain text.
		/// </summary>
		public void GetText(global::Microsoft.UI.Text.TextGetOptions options, out string value)
		{
			if (IsMathMode && !options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf))
			{
				throw new ArgumentException("Math-only documents can only be retrieved as RTF.", nameof(options));
			}

			var convertsLineEndings = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseLf)
				|| options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseCrlf);
			var effectiveOptions = !convertsLineEndings
				|| options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.AllowFinalEop)
					? options | global::Microsoft.UI.Text.TextGetOptions.AllowFinalEop
					: options & ~global::Microsoft.UI.Text.TextGetOptions.AllowFinalEop;
			var rangeEnd = !convertsLineEndings
				|| options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.AllowFinalEop)
					? StoryLength
					: TextLength;
			value = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf)
				? IsMathMode
					? RichTextRtfCodec.WriteMath(_mathDocument ?? MathDocument.FromPlainText(PlainText))
					: RichTextRtfCodec.Write(CaptureFragment(0, _textBuffer.Length, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)))
				: GetTextInRange(
					0,
					rangeEnd,
					effectiveOptions);
		}

		private static string ConvertTextForGetOptions(string text, global::Microsoft.UI.Text.TextGetOptions options)
		{
			var useLf = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseLf);
			var useCrlf = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseCrlf);
			if (useLf && useCrlf)
			{
				throw new ArgumentException("UseLf and UseCrlf cannot be combined.", nameof(options));
			}

			return useLf
				? text.Replace('\r', '\n')
				: useCrlf
					? text.Replace("\r", "\r\n")
					: text;
		}

		private static string NormalizeLineEndings(string value)
			=> value.Replace("\r\n", "\r").Replace('\n', '\r');

		private static string NormalizeRichTextLineEndings(string value)
			=> value.Replace("\r\n", "\r");

		/// <summary>
		/// Pauses rendering of the document until the matching <see cref="ApplyDisplayUpdates"/> is
		/// called. Calls may be nested. Returns the current nesting count.
		/// </summary>
		public int BatchDisplayUpdates() => ++_batchDepth;

		/// <summary>
		/// Resumes rendering paused by <see cref="BatchDisplayUpdates"/>, applying any pending update
		/// once the outermost batch closes. Returns the remaining nesting count.
		/// </summary>
		public int ApplyDisplayUpdates()
		{
			if (_batchDepth > 0)
			{
				_batchDepth--;
			}

			if (_batchDepth == 0 && _pendingRender)
			{
				var isContentChanging = _pendingContentChange;
				_pendingRender = false;
				_pendingContentChange = false;
				_owner.OnDocumentTextChanged(isContentChanging);
			}

			return _batchDepth;
		}
	}
}
