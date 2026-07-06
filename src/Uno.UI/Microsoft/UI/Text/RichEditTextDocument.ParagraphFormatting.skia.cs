#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// Run-model internals for the functional paragraph-formatting layer of the RichEditBox Text
	// Object Model. Paragraph formatting is stored per-character (mirroring the character run model in
	// RichEditTextDocument.Formatting.skia.cs) so it splices in lock-step with text edits and
	// participates in the same undo snapshots. Writes always cover whole paragraphs (split on
	// \r / \n / \r\n, each paragraph including its trailing break), so every character inside a
	// paragraph shares the same ParagraphFormatState.
	//
	// TODO Uno: The shared DisplayBlock is a single TextBlock and cannot render per-paragraph
	// alignment/indents/spacing/lists, so this layer is model-only (faithful get/set/clone/undo).
	public partial class RichEditTextDocument
	{
		private List<ParagraphRun> _paragraphRuns = new();

		// The document's default paragraph formatting: the basis for newly inserted text and empty
		// documents (see DefaultParagraphState). Exposed via Get/SetDefaultParagraphFormat. Like the
		// default character format, this is document-level configuration and is not part of the undo snapshot.
		private readonly ParagraphFormatState _defaultParagraphFormat = new();

		private ParagraphFormatState DefaultParagraphState() => _defaultParagraphFormat.Clone();

		/// <summary>Expands the paragraph runs into one (shared) state reference per character.</summary>
		private List<ParagraphFormatState> ExpandParagraphRunsRaw()
		{
			var list = new List<ParagraphFormatState>();
			foreach (var run in _paragraphRuns)
			{
				for (var i = 0; i < run.Length; i++)
				{
					list.Add(run.Format);
				}
			}

			return list;
		}

		/// <summary>Groups consecutive equal states into runs, each owning a private clone of its state.</summary>
		private static List<ParagraphRun> BuildParagraphRunsFromStates(List<ParagraphFormatState> states)
		{
			var runs = new List<ParagraphRun>();
			var i = 0;
			while (i < states.Count)
			{
				var start = i;
				var representative = states[i];
				i++;
				while (i < states.Count && states[i].Equals(representative))
				{
					i++;
				}

				runs.Add(new ParagraphRun(i - start, representative.Clone()));
			}

			return runs;
		}

		/// <summary>Reconciles the paragraph-run lengths so they sum exactly to <paramref name="length"/>.</summary>
		private void SyncParagraphRunsToLength(int length)
		{
			var current = 0;
			foreach (var run in _paragraphRuns)
			{
				current += run.Length;
			}

			if (current == length)
			{
				return;
			}

			var expanded = ExpandParagraphRunsRaw();
			if (expanded.Count < length)
			{
				// New characters inherit the paragraph formatting of the character to their left (the
				// paragraph they are extending), falling back to the default for an empty document.
				var fill = expanded.Count > 0 ? expanded[expanded.Count - 1] : DefaultParagraphState();
				while (expanded.Count < length)
				{
					expanded.Add(fill);
				}
			}
			else if (expanded.Count > length)
			{
				expanded.RemoveRange(length, expanded.Count - length);
			}

			_paragraphRuns = BuildParagraphRunsFromStates(expanded);
		}

		/// <summary>Resets paragraph formatting to a single default run of <paramref name="length"/> characters.</summary>
		private void ResetParagraphRuns(int length)
			=> _paragraphRuns = length > 0
				? new List<ParagraphRun> { new(length, DefaultParagraphState()) }
				: new List<ParagraphRun>();

		/// <summary>
		/// Splices the paragraph-run model to match a text edit that removed <paramref name="removeLength"/>
		/// characters at <paramref name="start"/> and inserted <paramref name="insertLength"/> new ones.
		/// Must be called while <see cref="_paragraphRuns"/> still reflect the pre-edit text length.
		/// </summary>
		private void SpliceParagraphRuns(int start, int removeLength, int insertLength)
		{
			var expanded = ExpandParagraphRunsRaw();
			var oldLength = expanded.Count;
			start = Math.Clamp(start, 0, oldLength);
			var removeEnd = Math.Clamp(start + removeLength, start, oldLength);

			ParagraphFormatState insertFormat;
			if (insertLength > 0)
			{
				// Inserted text inherits the paragraph formatting of the character to its left, or (at
				// the very start) the character to its right, falling back to the default when empty.
				insertFormat = start > 0
					? expanded[start - 1].Clone()
					: (oldLength > 0 ? expanded[0].Clone() : DefaultParagraphState());
			}
			else
			{
				insertFormat = DefaultParagraphState();
			}

			var result = new List<ParagraphFormatState>(oldLength - (removeEnd - start) + insertLength);
			for (var i = 0; i < start; i++)
			{
				result.Add(expanded[i]);
			}

			for (var i = 0; i < insertLength; i++)
			{
				result.Add(insertFormat);
			}

			for (var i = removeEnd; i < oldLength; i++)
			{
				result.Add(expanded[i]);
			}

			_paragraphRuns = BuildParagraphRunsFromStates(result);
		}

		/// <summary>
		/// Maps the character range [start, end) to the whole-paragraph character span covering every
		/// paragraph the range touches, plus the first/last paragraph indices. Returns false for an
		/// empty document (there are no characters to carry paragraph state).
		/// </summary>
		private bool TryGetParagraphSpan(int start, int end, out int paraStart, out int paraEnd, out int firstPara, out int lastPara)
		{
			paraStart = 0;
			paraEnd = 0;
			firstPara = 0;
			lastPara = 0;

			var length = _plainText.Length;
			if (length == 0)
			{
				return false;
			}

			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			var chunks = TextUnitNavigation.GetParagraphChunks(_plainText);
			if (chunks.Count == 0)
			{
				return false;
			}

			firstPara = TextUnitNavigation.FindChunkIndex(chunks, Math.Clamp(start, 0, length - 1));
			var endProbe = end > start ? end - 1 : start;
			lastPara = TextUnitNavigation.FindChunkIndex(chunks, Math.Clamp(endProbe, 0, length - 1));
			paraStart = chunks[firstPara].start;
			paraEnd = chunks[lastPara].start + chunks[lastPara].length;
			return true;
		}

		private void ApplyParagraphFormatOverChars(int start, int end, Action<ParagraphFormatState> apply)
		{
			SyncParagraphRunsToLength(_plainText.Length);
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			if (start >= end)
			{
				return;
			}

			var expanded = ExpandParagraphRunsRaw();
			for (var i = start; i < end; i++)
			{
				var clone = expanded[i].Clone();
				apply(clone);
				expanded[i] = clone;
			}

			_paragraphRuns = BuildParagraphRunsFromStates(expanded);
		}

		/// <summary>
		/// Builds a tri-state paragraph format describing the formatting over the paragraphs touched by
		/// [start, end): each property is the common value where the paragraphs agree, otherwise "undefined".
		/// </summary>
		internal UnoTextParagraphFormat GetParagraphFormatOverRange(int start, int end)
		{
			SyncParagraphRunsToLength(_plainText.Length);
			var format = new UnoTextParagraphFormat();
			if (!TryGetParagraphSpan(start, end, out _, out _, out var firstPara, out var lastPara))
			{
				format.LoadFrom(DefaultParagraphState());
				return format;
			}

			var chunks = TextUnitNavigation.GetParagraphChunks(_plainText);
			var expanded = ExpandParagraphRunsRaw();

			ParagraphFormatState RepresentativeOf(int paraIndex) => expanded[chunks[paraIndex].start];

			var first = RepresentativeOf(firstPara);
			if (firstPara == lastPara)
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

			for (var p = firstPara + 1; p <= lastPara; p++)
			{
				var s = RepresentativeOf(p);
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
			format.ListStyleValue = listStyleU ? first.ListStyle : global::Microsoft.UI.Text.MarkerStyle.Undefined;
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
			MutateWithUndo(() =>
			{
				if (!TryGetParagraphSpan(start, end, out var paraStart, out var paraEnd, out _, out _))
				{
					return;
				}

				ApplyParagraphFormatOverChars(paraStart, paraEnd, format.ApplyTo);
			});
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

		internal static List<ParagraphRun> CloneParagraphRuns(List<ParagraphRun> runs)
		{
			var list = new List<ParagraphRun>(runs.Count);
			foreach (var run in runs)
			{
				list.Add(run.Clone());
			}

			return list;
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

		internal static bool TabsEqual(List<ParagraphTab> a, List<ParagraphTab> b)
		{
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
