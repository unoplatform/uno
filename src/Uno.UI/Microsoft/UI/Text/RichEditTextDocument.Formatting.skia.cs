#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// Run-model internals for the functional character-formatting layer of the RichEditBox Text
	// Object Model. The document keeps a list of contiguous FormatRuns whose lengths always sum to
	// the plain-text length; edits splice the runs and formatting operations split/merge them.
	public partial class RichEditTextDocument
	{
		private List<FormatRun> _runs = new();

		// The document's default character formatting: the basis for newly inserted text and empty
		// documents (see DefaultFormatState). Exposed via Get/SetDefaultCharacterFormat. This is
		// document-level configuration and is intentionally not part of the undo snapshot.
		private readonly CharacterFormatState _defaultCharacterFormat = new();

		/// <summary>The current formatting runs, reconciled to the plain-text length (for rendering).</summary>
		internal IReadOnlyList<FormatRun> FormatRuns
		{
			get
			{
				SyncRunsToLength(_plainText.Length);
				return _runs;
			}
		}

		private CharacterFormatState DefaultFormatState() => _defaultCharacterFormat.Clone();

		/// <summary>Expands the runs into one (shared) state reference per character.</summary>
		private List<CharacterFormatState> ExpandRunsRaw()
		{
			var list = new List<CharacterFormatState>();
			foreach (var run in _runs)
			{
				for (var i = 0; i < run.Length; i++)
				{
					list.Add(run.Format);
				}
			}

			return list;
		}

		/// <summary>Groups consecutive equal states into runs, each owning a private clone of its state.</summary>
		private static List<FormatRun> BuildRunsFromStates(List<CharacterFormatState> states)
		{
			var runs = new List<FormatRun>();
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

				runs.Add(new FormatRun(i - start, representative.Clone()));
			}

			return runs;
		}

		/// <summary>Reconciles the run lengths so they sum exactly to <paramref name="length"/>.</summary>
		private void SyncRunsToLength(int length)
		{
			var current = 0;
			foreach (var run in _runs)
			{
				current += run.Length;
			}

			if (current == length)
			{
				return;
			}

			var expanded = ExpandRunsRaw();
			if (expanded.Count < length)
			{
				while (expanded.Count < length)
				{
					expanded.Add(DefaultFormatState());
				}
			}
			else if (expanded.Count > length)
			{
				expanded.RemoveRange(length, expanded.Count - length);
			}

			_runs = BuildRunsFromStates(expanded);
		}

		/// <summary>Resets formatting to a single default run of <paramref name="length"/> characters.</summary>
		private void ResetRuns(int length)
			=> _runs = length > 0
				? new List<FormatRun> { new(length, DefaultFormatState()) }
				: new List<FormatRun>();

		/// <summary>
		/// Splices the run model to match a text edit that removed <paramref name="removeLength"/>
		/// characters at <paramref name="start"/> and inserted <paramref name="insertLength"/> new ones.
		/// Must be called while <see cref="_runs"/> still reflect the pre-edit text length.
		/// </summary>
		private void SpliceRuns(int start, int removeLength, int insertLength)
		{
			var expanded = ExpandRunsRaw();
			var oldLength = expanded.Count;
			start = Math.Clamp(start, 0, oldLength);
			var removeEnd = Math.Clamp(start + removeLength, start, oldLength);

			CharacterFormatState insertFormat;
			if (insertLength > 0)
			{
				// Inserted text inherits the formatting of the character to its left, or (at the very
				// start) the character to its right, falling back to the default when the doc is empty.
				insertFormat = start > 0
					? expanded[start - 1].Clone()
					: (oldLength > 0 ? expanded[0].Clone() : DefaultFormatState());
			}
			else
			{
				insertFormat = DefaultFormatState();
			}

			var result = new List<CharacterFormatState>(oldLength - (removeEnd - start) + insertLength);
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

			_runs = BuildRunsFromStates(result);
		}

		private void ApplyFormatOverRange(int start, int end, Action<CharacterFormatState> apply)
		{
			SyncRunsToLength(_plainText.Length);
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			if (start >= end)
			{
				return;
			}

			var expanded = ExpandRunsRaw();
			for (var i = start; i < end; i++)
			{
				var clone = expanded[i].Clone();
				apply(clone);
				expanded[i] = clone;
			}

			_runs = BuildRunsFromStates(expanded);
		}

		/// <summary>
		/// Builds a tri-state character format describing the formatting over [start, end): each tracked
		/// property is the common value where the characters agree, otherwise "undefined".
		/// </summary>
		internal UnoTextCharacterFormat GetFormatOverRange(int start, int end)
		{
			SyncRunsToLength(_plainText.Length);
			var length = _plainText.Length;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			var format = new UnoTextCharacterFormat();
			if (length == 0)
			{
				format.LoadFrom(DefaultFormatState());
				return format;
			}

			var expanded = ExpandRunsRaw();
			if (start == end)
			{
				// A degenerate range reports the formatting that newly typed text would take.
				format.LoadFrom(start > 0 ? expanded[start - 1] : expanded[0]);
				return format;
			}

			var first = expanded[start];
			bool boldUniform = true, italicUniform = true, strikeUniform = true, underlineUniform = true,
				foregroundUniform = true, sizeUniform = true, nameUniform = true;
			for (var i = start + 1; i < end; i++)
			{
				var s = expanded[i];
				boldUniform &= s.Bold == first.Bold;
				italicUniform &= s.Italic == first.Italic;
				strikeUniform &= s.Strikethrough == first.Strikethrough;
				underlineUniform &= s.Underline == first.Underline;
				foregroundUniform &= Nullable.Equals(s.Foreground, first.Foreground);
				sizeUniform &= s.Size.Equals(first.Size);
				nameUniform &= string.Equals(s.Name, first.Name, StringComparison.Ordinal);
			}

			format.BoldEffect = boldUniform ? Effect(first.Bold) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.ItalicEffect = italicUniform ? Effect(first.Italic) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.StrikethroughEffect = strikeUniform ? Effect(first.Strikethrough) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.UnderlineValue = underlineUniform ? first.Underline : global::Microsoft.UI.Text.UnderlineType.Undefined;
			if (foregroundUniform && first.Foreground is { } fg)
			{
				format.ForegroundValue = fg;
				format.ForegroundDefined = true;
			}

			if (sizeUniform && first.Size > 0)
			{
				format.SizeValue = first.Size;
				format.SizeDefined = true;
			}

			if (nameUniform && !string.IsNullOrEmpty(first.Name))
			{
				format.NameValue = first.Name;
				format.NameDefined = true;
			}

			return format;
		}

		/// <summary>Applies the defined properties of <paramref name="format"/> over [start, end).</summary>
		internal void SetFormatOverRange(int start, int end, UnoTextCharacterFormat format)
			=> MutateWithUndo(() => ApplyFormatOverRange(start, end, state => ApplyCharacterFormatToState(state, format)));

		/// <summary>Writes the defined properties of <paramref name="format"/> into <paramref name="state"/>.</summary>
		private static void ApplyCharacterFormatToState(CharacterFormatState state, UnoTextCharacterFormat format)
		{
			if (format.BoldEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Bold = format.BoldEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (format.ItalicEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Italic = format.ItalicEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (format.StrikethroughEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Strikethrough = format.StrikethroughEffect == global::Microsoft.UI.Text.FormatEffect.On;
			}

			if (format.UnderlineValue != global::Microsoft.UI.Text.UnderlineType.Undefined)
			{
				state.Underline = format.UnderlineValue;
			}

			if (format.ForegroundDefined)
			{
				state.Foreground = format.ForegroundValue;
			}

			if (format.SizeDefined)
			{
				state.Size = format.SizeValue;
			}

			if (format.NameDefined)
			{
				state.Name = format.NameValue;
			}
		}

		/// <summary>Gets the document's default character format as a live (bound) format object.</summary>
		public global::Microsoft.UI.Text.ITextCharacterFormat GetDefaultCharacterFormat()
		{
			var format = new UnoTextCharacterFormat();
			format.LoadFrom(_defaultCharacterFormat);
			format.BindApply(ApplyDefaultCharacterFormat);
			return format;
		}

		/// <summary>Sets the document's default character format from the defined properties of <paramref name="value"/>.</summary>
		public void SetDefaultCharacterFormat(global::Microsoft.UI.Text.ITextCharacterFormat value)
		{
			if (value is UnoTextCharacterFormat format)
			{
				ApplyDefaultCharacterFormat(format);
			}
		}

		// Writes the defined properties of the (default-bound) format into the document default. This
		// does not retroactively re-format existing runs; it only changes the basis for future text.
		internal void ApplyDefaultCharacterFormat(UnoTextCharacterFormat format)
			=> ApplyCharacterFormatToState(_defaultCharacterFormat, format);

		private static global::Microsoft.UI.Text.FormatEffect Effect(bool value)
			=> value ? global::Microsoft.UI.Text.FormatEffect.On : global::Microsoft.UI.Text.FormatEffect.Off;

		internal static List<FormatRun> CloneRuns(List<FormatRun> runs)
		{
			var list = new List<FormatRun>(runs.Count);
			foreach (var run in runs)
			{
				list.Add(run.Clone());
			}

			return list;
		}

		internal static bool RunsEqual(List<FormatRun> a, List<FormatRun> b)
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
	}
}
