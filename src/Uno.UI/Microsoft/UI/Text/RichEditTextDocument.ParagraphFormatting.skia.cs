#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// Run-model internals for the functional paragraph-formatting layer of the RichEditBox Text
	// Object Model. Paragraph formatting is stored per-character (mirroring the character run model in
	// RichEditTextDocument.Formatting.skia.cs) so it splices in lock-step with text edits and
	// participates in the same undo deltas. Writes always cover whole paragraphs (split on
	// \r / \n / \r\n / U+2029, each paragraph including its trailing break), so every character
	// inside a paragraph shares the same ParagraphFormatState. A separate state represents the
	// implicit zero-length terminal paragraph after a final break (and the sole paragraph when empty).
	//
	// Paragraph formatting is projected by the RichEditBox render path and also participates in
	// get/set/clone/undo.
	public partial class RichEditTextDocument
	{
		private readonly IndexedRunCollection<ParagraphRun> _paragraphRuns = new(
			static run => run.Length,
			static (run, length) => run.Length = length);
		private ParagraphFormatState _terminalParagraphFormat = new();
		private readonly Dictionary<global::Microsoft.UI.Text.ParagraphAlignment, int> _paragraphAlignmentLengths = new();
		private int _visualParagraphFormattingLength;
		private int _listParagraphFormattingLength;

		internal IndexedRunCollection<ParagraphRun> ParagraphRuns
		{
			get
			{
				SyncParagraphRunsToLength(_textBuffer.Length);
				return _paragraphRuns;
			}
		}

		internal ParagraphFormatState TerminalParagraphFormat
		{
			get
			{
				SyncParagraphRunsToLength(_textBuffer.Length);
				return _terminalParagraphFormat;
			}
		}

		internal int ParagraphRunCount
		{
			get
			{
				SyncParagraphRunsToLength(_textBuffer.Length);
				return _paragraphRuns.Count;
			}
		}

		internal bool HasVisualParagraphFormatting
			=> _visualParagraphFormattingLength != 0 || HasVisualParagraphFormattingState(_terminalParagraphFormat);

		internal bool HasListParagraphFormatting
			=> _listParagraphFormattingLength != 0 || HasListParagraphFormattingState(_terminalParagraphFormat);

		internal bool HasMixedParagraphAlignments
		{
			get
			{
				if (_paragraphAlignmentLengths.Count == 0)
				{
					return false;
				}
				if (_paragraphAlignmentLengths.Count > 1)
				{
					return true;
				}

				using var enumerator = _paragraphAlignmentLengths.Keys.GetEnumerator();
				_ = enumerator.MoveNext();
				return TextUnitNavigation.EndsInParagraphBreak(_textBuffer)
					&& _terminalParagraphFormat.Alignment != enumerator.Current;
			}
		}

		// The document's default paragraph formatting: the basis for newly inserted text and empty
		// documents (see DefaultParagraphState). Exposed via Get/SetDefaultParagraphFormat. Like the
		// default character format, this is document-level configuration and is not part of undo history.
		private readonly ParagraphFormatState _defaultParagraphFormat = new();

		private ParagraphFormatState DefaultParagraphState() => _defaultParagraphFormat.Clone();

		private void SetParagraphRuns(List<ParagraphRun> runs)
		{
			_paragraphRuns.Reset(runs);
			ResetParagraphRenderProfile();
			foreach (var run in _paragraphRuns)
			{
				AddParagraphRenderProfile(run, 1);
			}
		}

		private int GetParagraphRunStart(int runIndex) => _paragraphRuns.GetStart(runIndex);

		internal int FindParagraphRunIndexForRender(int position)
		{
			SyncParagraphRunsToLength(_textBuffer.Length);
			return position == _textBuffer.Length ? _paragraphRuns.Count : FindParagraphRunIndex(position);
		}

		internal int GetParagraphRunStartForRender(int runIndex) => GetParagraphRunStart(runIndex);

		internal int GetParagraphRunEndForRender(int runIndex) => _paragraphRuns.GetEnd(runIndex);

		internal int GetParagraphStartForRender(int position) => GetParagraphStart(position);

		internal int GetParagraphEndForRender(int position) => GetParagraphEnd(position);

		private int FindParagraphRunIndex(int position)
		{
			if ((uint)position >= (uint)_paragraphRuns.TotalLength)
			{
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			return _paragraphRuns.FindIndex(position);
		}

		private ParagraphFormatState GetParagraphFormatAt(int position) => _paragraphRuns[FindParagraphRunIndex(position)].Format;

		private static void AppendParagraphRun(List<ParagraphRun> runs, int length, ParagraphFormatState format, bool clone = true)
		{
			if (length <= 0)
			{
				return;
			}

			if (runs.Count > 0 && runs[^1].Format.Equals(format))
			{
				runs[^1].Length += length;
			}
			else
			{
				runs.Add(new ParagraphRun(length, clone ? format.Clone() : format));
			}
		}

		private void ReplaceParagraphRuns(int start, int end, IReadOnlyList<ParagraphRun> insertedRuns)
		{
			var length = _paragraphRuns.TotalLength;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			var startRun = start == length ? _paragraphRuns.Count : FindParagraphRunIndex(start);
			var startOffset = startRun == _paragraphRuns.Count ? 0 : start - GetParagraphRunStart(startRun);
			var endRun = end == length ? _paragraphRuns.Count : FindParagraphRunIndex(end);
			var endOffset = endRun == _paragraphRuns.Count ? 0 : end - GetParagraphRunStart(endRun);
			var removeCount = endRun - startRun + (endRun < _paragraphRuns.Count && endOffset > 0 ? 1 : 0);
			var replacement = new List<ParagraphRun>(insertedRuns.Count + 2);
			for (var i = 0; i < removeCount; i++)
			{
				AddParagraphRenderProfile(_paragraphRuns[startRun + i], -1);
			}

			if (startRun < _paragraphRuns.Count && startOffset > 0)
			{
				AppendParagraphRun(replacement, startOffset, _paragraphRuns[startRun].Format);
			}

			for (var i = 0; i < insertedRuns.Count; i++)
			{
				AppendParagraphRun(replacement, insertedRuns[i].Length, insertedRuns[i].Format);
			}

			if (endRun < _paragraphRuns.Count && endOffset > 0)
			{
				AppendParagraphRun(replacement, _paragraphRuns[endRun].Length - endOffset, _paragraphRuns[endRun].Format);
			}

			_paragraphRuns.ReplaceRange(startRun, removeCount, replacement);

			foreach (var run in replacement)
			{
				AddParagraphRenderProfile(run, 1);
			}
			CoalesceParagraphRunsAt(startRun);
		}

		private void ResetParagraphRenderProfile()
		{
			_paragraphAlignmentLengths.Clear();
			_visualParagraphFormattingLength = 0;
			_listParagraphFormattingLength = 0;
		}

		private void AddParagraphRenderProfile(ParagraphRun run, int direction)
		{
			var lengthDelta = checked(run.Length * direction);
			if (_paragraphAlignmentLengths.TryGetValue(run.Format.Alignment, out var alignmentLength))
			{
				var updatedLength = checked(alignmentLength + lengthDelta);
				if (updatedLength == 0)
				{
					_paragraphAlignmentLengths.Remove(run.Format.Alignment);
				}
				else
				{
					_paragraphAlignmentLengths[run.Format.Alignment] = updatedLength;
				}
			}
			else if (lengthDelta > 0)
			{
				_paragraphAlignmentLengths.Add(run.Format.Alignment, lengthDelta);
			}
			else
			{
				throw new InvalidOperationException("The paragraph alignment profile is inconsistent.");
			}

			if (HasVisualParagraphFormattingState(run.Format))
			{
				_visualParagraphFormattingLength = checked(_visualParagraphFormattingLength + lengthDelta);
			}
			if (HasListParagraphFormattingState(run.Format))
			{
				_listParagraphFormattingLength = checked(_listParagraphFormattingLength + lengthDelta);
			}
		}

		private static bool HasVisualParagraphFormattingState(ParagraphFormatState format)
			=> format.FirstLineIndent != 0
				|| format.LeftIndent != 0
				|| format.RightIndent != 0
				|| format.SpaceBefore != 0
				|| format.SpaceAfter != 0
				|| format.RightToLeft
				|| format.LineSpacingRule is not global::Microsoft.UI.Text.LineSpacingRule.Single
					and not global::Microsoft.UI.Text.LineSpacingRule.Undefined
				|| HasListParagraphFormattingState(format)
				|| format.Tabs.Count != 0;

		private static bool HasListParagraphFormattingState(ParagraphFormatState format)
			=> format.ListType is not global::Microsoft.UI.Text.MarkerType.None
				and not global::Microsoft.UI.Text.MarkerType.Undefined;

		internal bool IsParagraphRenderProfileValid()
		{
			var alignments = new Dictionary<global::Microsoft.UI.Text.ParagraphAlignment, int>();
			var visualLength = 0;
			var listLength = 0;
			foreach (var run in _paragraphRuns)
			{
				alignments.TryGetValue(run.Format.Alignment, out var alignmentLength);
				alignments[run.Format.Alignment] = alignmentLength + run.Length;
				if (HasVisualParagraphFormattingState(run.Format))
				{
					visualLength += run.Length;
				}
				if (HasListParagraphFormattingState(run.Format))
				{
					listLength += run.Length;
				}
			}

			if (visualLength != _visualParagraphFormattingLength
				|| listLength != _listParagraphFormattingLength
				|| alignments.Count != _paragraphAlignmentLengths.Count)
			{
				return false;
			}

			foreach (var pair in alignments)
			{
				if (!_paragraphAlignmentLengths.TryGetValue(pair.Key, out var length)
					|| length != pair.Value)
				{
					return false;
				}
			}

			return true;
		}

		private void CoalesceParagraphRunsAt(int index)
		{
			index = Math.Max(1, index);
			var end = Math.Min(_paragraphRuns.Count - 1, index + 2);
			while (index <= end && index < _paragraphRuns.Count)
			{
				if (_paragraphRuns[index - 1].Format.Equals(_paragraphRuns[index].Format))
				{
					_paragraphRuns.SetLength(
						index - 1,
						checked(_paragraphRuns[index - 1].Length + _paragraphRuns[index].Length));
					_paragraphRuns.RemoveAt(index);
					end--;
				}
				else
				{
					index++;
				}
			}
		}

		/// <summary>Reconciles the paragraph-run lengths so they sum exactly to <paramref name="length"/>.</summary>
		private void SyncParagraphRunsToLength(int length)
		{
			var current = _paragraphRuns.TotalLength;

			if (current == length)
			{
				return;
			}

			if (current < length)
			{
				// New characters inherit the paragraph formatting of the character to their left (the
				// paragraph they are extending), falling back to the default for an empty document.
				var fill = current > 0 ? GetParagraphFormatAt(current - 1) : _terminalParagraphFormat;
				ReplaceParagraphRuns(current, current, new[] { new ParagraphRun(length - current, fill.Clone()) });
			}
			else
			{
				ReplaceParagraphRuns(length, current, Array.Empty<ParagraphRun>());
			}

			SyncTerminalParagraphToMaterializedLastParagraph();
		}

		/// <summary>Resets paragraph formatting to a single default run of <paramref name="length"/> characters.</summary>
		private void ResetParagraphRuns(int length)
		{
			_terminalParagraphFormat = DefaultParagraphState();
			SetParagraphRuns(length > 0
				? new List<ParagraphRun> { new(length, _terminalParagraphFormat.Clone()) }
				: new List<ParagraphRun>());
		}

		/// <summary>
		/// Splices the paragraph-run model to match a text edit that removed <paramref name="removeLength"/>
		/// characters at <paramref name="start"/> and inserted <paramref name="insertLength"/> new ones.
		/// Must be called while <see cref="_paragraphRuns"/> still reflect the pre-edit text length.
		/// </summary>
		private void SpliceParagraphRuns(TextStoryBuffer oldText, int start, int removeLength, int insertLength)
		{
			var oldLength = _paragraphRuns.TotalLength;
			start = Math.Clamp(start, 0, oldLength);
			var removeEnd = Math.Clamp(start + removeLength, start, oldLength);

			// Inserted text (and the caret state retained by a tail deletion) inherits the paragraph
			// containing the edit position. At a paragraph start that is the right-hand paragraph;
			// at end-of-story it is the explicit terminal paragraph.
			var insertFormat = start < oldLength && TextUnitNavigation.IsParagraphStart(oldText, start)
				? GetParagraphFormatAt(start).Clone()
				: start == oldLength
					? _terminalParagraphFormat.Clone()
					: start > 0
						? GetParagraphFormatAt(start - 1).Clone()
						: (oldLength > 0 ? GetParagraphFormatAt(0).Clone() : _terminalParagraphFormat.Clone());

			var insertedRuns = insertLength > 0
				? new[] { new ParagraphRun(insertLength, insertFormat) }
				: Array.Empty<ParagraphRun>();
			ReplaceParagraphRuns(start, removeEnd, insertedRuns);
			if (removeEnd == oldLength)
			{
				_terminalParagraphFormat = insertFormat.Clone();
			}
		}

		private void NormalizeParagraphRunsAroundEdit(int editStart, int insertedLength)
		{
			SyncParagraphRunsToLength(_textBuffer.Length);
			if (_textBuffer.Length == 0)
			{
				return;
			}

			var start = GetParagraphStart(Math.Clamp(editStart, 0, _textBuffer.Length));
			var endProbe = Math.Clamp(editStart + insertedLength, start, _textBuffer.Length);
			var end = GetParagraphEnd(endProbe);
			var position = start;
			while (position < end)
			{
				var paragraphEnd = GetParagraphEnd(position);
				var format = GetParagraphFormatAt(position);
				ReplaceParagraphRuns(position, paragraphEnd, new[] { new ParagraphRun(paragraphEnd - position, format.Clone()) });
				position = paragraphEnd;
			}

			SyncTerminalParagraphToMaterializedLastParagraph();
		}

		private void SyncTerminalParagraphToMaterializedLastParagraph()
		{
			if (_textBuffer.Length > 0 && !TextUnitNavigation.EndsInParagraphBreak(_textBuffer))
			{
				_terminalParagraphFormat = GetParagraphFormatAt(_textBuffer.Length - 1).Clone();
			}
		}

		private int GetParagraphStart(int position)
			=> _textBuffer.FindParagraphStart(position);

		private int GetParagraphEnd(int position)
			=> _textBuffer.FindParagraphEnd(position);

		private bool TryGetParagraphSpan(int start, int end, out int paraStart, out int paraEnd, out bool includesTerminal)
		{
			var length = _textBuffer.Length;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);
			var endProbe = end > start ? end - 1 : start;
			paraStart = GetParagraphStart(start);
			paraEnd = GetParagraphEnd(endProbe);
			includesTerminal = paraStart == length && paraEnd == length;
			return true;
		}

		private void ApplyParagraphFormatOverRange(int start, int end, bool includesTerminal, UnoTextParagraphFormat paragraphFormat)
		{
			SyncParagraphRunsToLength(_textBuffer.Length);
			var transformedStates = new Dictionary<ParagraphFormatState, ParagraphFormatState>();
			var transformedTabs = new Dictionary<IReadOnlyList<ParagraphTab>, ParagraphFormatState>(ReferenceEqualityComparer.Instance);

			ParagraphFormatState Transform(ParagraphFormatState source)
			{
				if (!transformedStates.TryGetValue(source, out var format))
				{
					format = source.Clone();
					paragraphFormat.ApplyScalarsTo(format);
					if (paragraphFormat.UpdatesTabs && transformedTabs.TryGetValue(source.Tabs, out var sharedTabs))
					{
						format.ShareTabsFrom(sharedTabs);
					}
					else if (paragraphFormat.UpdatesTabs)
					{
						paragraphFormat.ApplyTabsTo(format);
						transformedTabs.Add(source.Tabs, format);
					}
					transformedStates.Add(source, format);
				}
				return format;
			}

			if (start < end)
			{
				var transformedRuns = new List<ParagraphRun>();
				var position = start;
				while (position < end)
				{
					var paragraphEnd = Math.Min(end, GetParagraphEnd(position));
					AppendParagraphRun(
						transformedRuns,
						paragraphEnd - position,
						Transform(GetParagraphFormatAt(position)),
						clone: false);
					position = paragraphEnd;
				}
				ReplaceParagraphRuns(start, end, transformedRuns);
			}

			if (includesTerminal
				|| end == _textBuffer.Length && !TextUnitNavigation.EndsInParagraphBreak(_textBuffer))
			{
				var source = includesTerminal ? _terminalParagraphFormat : GetParagraphFormatAt(end - 1);
				_terminalParagraphFormat = Transform(source).Clone();
			}
		}

		/// <summary>
		/// Builds a tri-state paragraph format describing the formatting over the paragraphs touched by
		/// [start, end): each property is the common value where the paragraphs agree, otherwise "undefined".
		/// </summary>
		internal UnoTextParagraphFormat GetParagraphFormatOverRange(int start, int end)
		{
			SyncParagraphRunsToLength(_textBuffer.Length);
			var format = new UnoTextParagraphFormat();
			TryGetParagraphSpan(start, end, out var paragraphStart, out var paragraphEnd, out var includesTerminal);
			var first = includesTerminal
				? _terminalParagraphFormat
				: GetParagraphFormatAt(paragraphStart);
			var nextParagraph = includesTerminal ? paragraphEnd : GetParagraphEnd(paragraphStart);
			if (nextParagraph >= paragraphEnd)
			{
				// A single paragraph is fully uniform: report all of its resolved values.
				format.LoadFrom(first);
				return format;
			}

			bool alignmentU = true, firstIndentU = true, leftIndentU = true, rightIndentU = true,
				spaceBeforeU = true, spaceAfterU = true, lineRuleU = true, lineSpacingU = true,
				listTypeU = true, listStyleU = true, listAlignU = true, listLevelU = true, listStartU = true,
				listTabU = true, keepTogetherU = true, keepWithNextU = true, noLineNumberU = true,
				pageBreakU = true, rtlU = true, widowU = true, styleU = true, tabsU = true;

			var position = nextParagraph;
			while (position < paragraphEnd)
			{
				var s = GetParagraphFormatAt(position);
				alignmentU &= s.Alignment == first.Alignment;
				firstIndentU &= s.FirstLineIndent.Equals(first.FirstLineIndent);
				leftIndentU &= s.LeftIndent.Equals(first.LeftIndent);
				rightIndentU &= s.RightIndent.Equals(first.RightIndent);
				spaceBeforeU &= s.SpaceBefore.Equals(first.SpaceBefore);
				spaceAfterU &= s.SpaceAfter.Equals(first.SpaceAfter);
				lineRuleU &= s.LineSpacingRule == first.LineSpacingRule;
				lineSpacingU &= s.LineSpacing.Equals(first.LineSpacing);
				listTypeU &= s.ListType == first.ListType;
				listStyleU &= s.ListStyle == first.ListStyle;
				listAlignU &= s.ListAlignment == first.ListAlignment;
				listLevelU &= s.ListLevelIndex == first.ListLevelIndex;
				listStartU &= s.ListStart == first.ListStart;
				listTabU &= s.ListTab.Equals(first.ListTab);
				keepTogetherU &= s.KeepTogether == first.KeepTogether;
				keepWithNextU &= s.KeepWithNext == first.KeepWithNext;
				noLineNumberU &= s.NoLineNumber == first.NoLineNumber;
				pageBreakU &= s.PageBreakBefore == first.PageBreakBefore;
				rtlU &= s.RightToLeft == first.RightToLeft;
				widowU &= s.WidowControl == first.WidowControl;
				styleU &= s.Style == first.Style;
				tabsU &= TabsEqual(s.Tabs, first.Tabs);
				position = GetParagraphEnd(position);
			}

			format.AlignmentValue = alignmentU ? first.Alignment : global::Microsoft.UI.Text.ParagraphAlignment.Undefined;
			if (firstIndentU)
			{
				format.FirstLineIndentValue = first.FirstLineIndent;
				format.FirstLineIndentDefined = true;
			}

			if (leftIndentU)
			{
				format.LeftIndentValue = first.LeftIndent;
				format.LeftIndentDefined = true;
			}

			if (rightIndentU)
			{
				format.RightIndentValue = first.RightIndent;
				format.RightIndentDefined = true;
			}

			if (spaceBeforeU)
			{
				format.SpaceBeforeValue = first.SpaceBefore;
				format.SpaceBeforeDefined = true;
			}

			if (spaceAfterU)
			{
				format.SpaceAfterValue = first.SpaceAfter;
				format.SpaceAfterDefined = true;
			}

			format.LineSpacingRuleValue = lineRuleU ? first.LineSpacingRule : global::Microsoft.UI.Text.LineSpacingRule.Undefined;
			if (lineSpacingU)
			{
				format.LineSpacingValue = first.LineSpacing;
				format.LineSpacingDefined = true;
			}

			format.ListTypeValue = listTypeU ? first.ListType : global::Microsoft.UI.Text.MarkerType.Undefined;
			format.ListTypeDefined = listTypeU;
			format.ListStyleValue = listStyleU ? first.ListStyle : global::Microsoft.UI.Text.MarkerStyle.Undefined;
			format.ListStyleDefined = listStyleU;
			format.ListAlignmentValue = listAlignU ? first.ListAlignment : global::Microsoft.UI.Text.MarkerAlignment.Undefined;
			if (listLevelU)
			{
				format.ListLevelIndexValue = first.ListLevelIndex;
				format.ListLevelIndexDefined = true;
			}

			if (listStartU)
			{
				format.ListStartValue = first.ListStart;
				format.ListStartDefined = true;
			}

			if (listTabU)
			{
				format.ListTabValue = first.ListTab;
				format.ListTabDefined = true;
			}

			format.KeepTogetherEffect = keepTogetherU ? Effect(first.KeepTogether) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.KeepWithNextEffect = keepWithNextU ? Effect(first.KeepWithNext) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.NoLineNumberEffect = noLineNumberU ? Effect(first.NoLineNumber) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.PageBreakBeforeEffect = pageBreakU ? Effect(first.PageBreakBefore) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.RightToLeftEffect = rtlU ? Effect(first.RightToLeft) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.WidowControlEffect = widowU ? Effect(first.WidowControl) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.StyleValue = styleU ? first.Style : global::Microsoft.UI.Text.ParagraphStyle.Undefined;
			if (tabsU)
			{
				format.TabsValue = new List<ParagraphTab>(first.Tabs);
				format.TabsDefined = true;
			}

			return format;
		}

		/// <summary>Applies the defined properties of <paramref name="format"/> over every paragraph touched by [start, end).</summary>
		internal void SetParagraphFormatOverRange(int start, int end, UnoTextParagraphFormat format)
		{
			TryGetParagraphSpan(start, end, out var protectedStart, out var protectedEnd, out var includesTerminal);
			ThrowIfNotEditable(protectedStart, protectedEnd);

			MutateWithUndo(() =>
			{
				ApplyParagraphFormatOverRange(protectedStart, protectedEnd, includesTerminal, format);
			}, paragraphRange: new HistoryRange(protectedStart, protectedEnd));
		}

		/// <summary>Gets the document's default paragraph format as a live (bound) format object.</summary>
		public global::Microsoft.UI.Text.ITextParagraphFormat GetDefaultParagraphFormat()
		{
			var format = new UnoTextParagraphFormat();
			format.LoadFrom(_defaultParagraphFormat);
			format.BindApply(ApplyDefaultParagraphFormat);
			return format;
		}

		/// <summary>Sets the document's default paragraph format from the defined properties of <paramref name="value"/>.</summary>
		public void SetDefaultParagraphFormat(global::Microsoft.UI.Text.ITextParagraphFormat value)
		{
			if (value is UnoTextParagraphFormat format)
			{
				ApplyDefaultParagraphFormat(format);
			}
		}

		// Writes the defined properties of the (default-bound) format into the document default. This
		// does not retroactively re-format existing paragraphs; it only changes the basis for future text.
		internal void ApplyDefaultParagraphFormat(UnoTextParagraphFormat format)
			=> format.ApplyTo(_defaultParagraphFormat);

		/// <summary>
		/// Resolves the single paragraph alignment shared by every paragraph in the document, or
		/// <c>null</c> when the document has no paragraphs or the paragraphs disagree. Because paragraph
		/// runs are maximal spans of equal <see cref="ParagraphFormatState"/> (and each paragraph holds a
		/// uniform state), the set of distinct alignments across runs equals the set across paragraphs, so
		/// checking that all runs agree answers "do all paragraphs share one alignment". Used by the
		/// RichEditBox render path to retain the block-level fast path for uniformly aligned documents.
		/// </summary>
		internal global::Microsoft.UI.Text.ParagraphAlignment? GetUniformParagraphAlignment()
		{
			if (_paragraphRuns.Count == 0)
			{
				return _terminalParagraphFormat.Alignment;
			}

			if (_paragraphAlignmentLengths.Count != 1)
			{
				return null;
			}

			using var enumerator = _paragraphAlignmentLengths.Keys.GetEnumerator();
			_ = enumerator.MoveNext();
			var alignment = enumerator.Current;
			if (TextUnitNavigation.EndsInParagraphBreak(_textBuffer)
				&& _terminalParagraphFormat.Alignment != alignment)
			{
				return null;
			}

			return alignment;
		}

		internal static List<ParagraphRun> CloneParagraphRuns(List<ParagraphRun> runs)
		{
			var list = new List<ParagraphRun>(runs.Count);
			foreach (var run in runs)
			{
				list.Add(run.Clone());
			}

			return list;
		}

		private static List<ParagraphRun> BuildParagraphRunsFromFragment(
			IReadOnlyList<ParagraphRun> source,
			int length,
			ParagraphFormatState fallback)
		{
			var runs = new List<ParagraphRun>();
			var remaining = length;
			foreach (var run in source)
			{
				var runLength = Math.Min(remaining, run.Length);
				if (runLength <= 0)
				{
					break;
				}

				AppendParagraphRun(runs, runLength, run.Format);
				remaining -= runLength;
			}

			if (remaining > 0)
			{
				AppendParagraphRun(runs, remaining, fallback);
			}

			return runs;
		}

		internal static bool ParagraphRunsEqual(List<ParagraphRun> a, List<ParagraphRun> b)
		{
			if (a.Count != b.Count)
			{
				return false;
			}

			for (var i = 0; i < a.Count; i++)
			{
				if (a[i].Length != b[i].Length || !a[i].Format.Equals(b[i].Format))
				{
					return false;
				}
			}

			return true;
		}

		internal static bool TabsEqual(IReadOnlyList<ParagraphTab> a, IReadOnlyList<ParagraphTab> b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if (a.Count != b.Count)
			{
				return false;
			}

			for (var i = 0; i < a.Count; i++)
			{
				if (!a[i].Equals(b[i]))
				{
					return false;
				}
			}

			return true;
		}
	}
}
