#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

		// Pending caret ("insertion point") character format. When a character format is applied at a
		// collapsed caret it is not written to any existing character but remembered here and applied to
		// the next inserted text at this position (WinUI's insertion-point format). Cleared when the
		// caret moves elsewhere or once consumed by an insert. Transient — not part of undo snapshots.
		private CharacterFormatState? _pendingCaretFormat;
		private int _pendingCaretPosition = -1;

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
				if (_pendingCaretFormat is { } pending && _pendingCaretPosition == start)
				{
					// Text typed at a caret carrying a pending insertion-point format takes that format.
					insertFormat = pending.Clone();
				}
				else
				{
					// Inserted text inherits the formatting of the character to its left, or (at the very
					// start) the character to its right, falling back to the default when the doc is empty.
					insertFormat = start > 0
						? expanded[start - 1].Clone()
						: (oldLength > 0 ? expanded[0].Clone() : DefaultFormatState());
				}
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

			// A collapsed caret carrying a pending insertion-point format reports that pending format.
			if (start == end && _pendingCaretFormat is { } pendingRead && _pendingCaretPosition == start)
			{
				format.LoadFrom(pendingRead);
				return format;
			}

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
		{
			SyncRunsToLength(_plainText.Length);
			var length = _plainText.Length;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			if (start == end)
			{
				// Applying a character format at a collapsed caret establishes the pending insertion-point
				// format (applied to the next typed/inserted text) rather than mutating any existing text.
				var basis = ResolveCaretBasisFormat(start);
				ApplyCharacterFormatToState(basis, format);
				_pendingCaretFormat = basis;
				_pendingCaretPosition = start;
				return;
			}

			MutateWithUndo(() => ApplyFormatOverRange(start, end, state => ApplyCharacterFormatToState(state, format)));
		}

		/// <summary>
		/// The basis a pending caret format accumulates onto: an existing pending format at the same
		/// caret, else the character to the left (what newly typed text inherits), else the character to
		/// the right, else the document default.
		/// </summary>
		private CharacterFormatState ResolveCaretBasisFormat(int position)
		{
			if (_pendingCaretFormat is { } pending && _pendingCaretPosition == position)
			{
				return pending.Clone();
			}

			var expanded = ExpandRunsRaw();
			if (expanded.Count == 0)
			{
				return DefaultFormatState();
			}

			var index = Math.Clamp(position > 0 ? position - 1 : 0, 0, expanded.Count - 1);
			return expanded[index].Clone();
		}

		/// <summary>Discards any pending caret insertion-point format.</summary>
		internal void ClearPendingCaretFormat()
		{
			_pendingCaretFormat = null;
			_pendingCaretPosition = -1;
		}

		/// <summary>Clears the pending caret format unless the selection is still the caret that owns it.</summary>
		internal void ClearPendingCaretFormatIfMoved(int start, int end)
		{
			if (!(start == end && start == _pendingCaretPosition))
			{
				ClearPendingCaretFormat();
			}
		}

		/// <summary>
		/// Resolves a tri-state <see cref="global::Microsoft.UI.Text.FormatEffect"/> against the current
		/// per-character/paragraph boolean state: On/Off set the value directly, while Toggle flips the
		/// current state (WinUI's tomToggle). Undefined leaves it unchanged — callers guard Undefined
		/// before calling, so a Toggle applied per character/paragraph flips each one independently.
		/// </summary>
		internal static bool ResolveEffect(global::Microsoft.UI.Text.FormatEffect effect, bool current)
			=> effect switch
			{
				global::Microsoft.UI.Text.FormatEffect.On => true,
				global::Microsoft.UI.Text.FormatEffect.Off => false,
				global::Microsoft.UI.Text.FormatEffect.Toggle => !current,
				_ => current,
			};

		/// <summary>Writes the defined properties of <paramref name="format"/> into <paramref name="state"/>.</summary>
		private static void ApplyCharacterFormatToState(CharacterFormatState state, UnoTextCharacterFormat format)
		{
			if (format.BoldEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Bold = ResolveEffect(format.BoldEffect, state.Bold);
			}

			if (format.ItalicEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Italic = ResolveEffect(format.ItalicEffect, state.Italic);
			}

			if (format.StrikethroughEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Strikethrough = ResolveEffect(format.StrikethroughEffect, state.Strikethrough);
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

		// --- Rich clipboard payload (in-process) --------------------------------------------------
		//
		// Copy/paste preserves per-character formatting via an app-private, in-process payload rather
		// than the OS clipboard: the plain text always goes to the real clipboard, while the format
		// runs for the copied span are serialized into a compact string and applied on the next paste
		// whose inserted text matches exactly. This round-trips within the app (across RichEditBox
		// instances) which is what ClipboardCopyFormat/TestClipboardCopyFormats exercise; a full
		// RTF/CF_RTF payload that survives cross-process is a documented follow-up.
		private const string RichClipboardHeader = "UNORICH1";

		/// <summary>
		/// Serializes the character-format runs spanning [start, end) into a compact, dependency-free
		/// string (header + ';'-separated runs, each 'len|bold|italic|underline|strike|fgDefined|a|r|g|b|size|nameBase64').
		/// </summary>
		internal string SerializeFormatRuns(int start, int end)
		{
			SyncRunsToLength(_plainText.Length);
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);

			var expanded = ExpandRunsRaw();
			var sb = new StringBuilder();
			sb.Append(RichClipboardHeader);

			var i = start;
			while (i < end)
			{
				var representative = expanded[i];
				var runStart = i;
				i++;
				while (i < end && expanded[i].Equals(representative))
				{
					i++;
				}

				sb.Append(';');
				AppendSerializedRun(sb, i - runStart, representative);
			}

			return sb.ToString();
		}

		private static void AppendSerializedRun(StringBuilder sb, int length, CharacterFormatState state)
		{
			sb.Append(length.ToString(CultureInfo.InvariantCulture)).Append('|');
			sb.Append(state.Bold ? '1' : '0').Append('|');
			sb.Append(state.Italic ? '1' : '0').Append('|');
			sb.Append(((int)state.Underline).ToString(CultureInfo.InvariantCulture)).Append('|');
			sb.Append(state.Strikethrough ? '1' : '0').Append('|');
			if (state.Foreground is { } fg)
			{
				sb.Append('1').Append('|');
				sb.Append(fg.A).Append('|').Append(fg.R).Append('|').Append(fg.G).Append('|').Append(fg.B).Append('|');
			}
			else
			{
				sb.Append("0|0|0|0|0|");
			}

			sb.Append(state.Size.ToString(CultureInfo.InvariantCulture)).Append('|');
			var name = state.Name ?? string.Empty;
			sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(name)));
		}

		/// <summary>
		/// Applies a payload produced by <see cref="SerializeFormatRuns"/> to [start, start + textLength).
		/// Returns false (and mutates nothing) when the payload is malformed or its total run length does
		/// not match <paramref name="textLength"/>, so the caller can fall back to plain-text paste.
		/// </summary>
		internal bool ApplySerializedFormatRuns(int start, string payload, int textLength)
		{
			if (string.IsNullOrEmpty(payload) || textLength <= 0)
			{
				return false;
			}

			var parts = payload.Split(';');
			if (parts.Length < 2 || !string.Equals(parts[0], RichClipboardHeader, StringComparison.Ordinal))
			{
				return false;
			}

			var states = new List<CharacterFormatState>(textLength);
			for (var p = 1; p < parts.Length; p++)
			{
				if (!TryParseSerializedRun(parts[p], out var length, out var state))
				{
					return false;
				}

				for (var k = 0; k < length; k++)
				{
					states.Add(state.Clone());
				}
			}

			if (states.Count != textLength)
			{
				return false;
			}

			SyncRunsToLength(_plainText.Length);
			start = Math.Clamp(start, 0, _plainText.Length);
			if (start + textLength > _plainText.Length)
			{
				return false;
			}

			MutateWithUndo(() =>
			{
				var expanded = ExpandRunsRaw();
				for (var k = 0; k < textLength; k++)
				{
					expanded[start + k] = states[k];
				}

				_runs = BuildRunsFromStates(expanded);
			});

			return true;
		}

		private static bool TryParseSerializedRun(string text, out int length, out CharacterFormatState state)
		{
			length = 0;
			state = new CharacterFormatState();

			var f = text.Split('|');
			if (f.Length != 12)
			{
				return false;
			}

			if (!int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out length) || length <= 0)
			{
				return false;
			}

			state.Bold = f[1] == "1";
			state.Italic = f[2] == "1";
			if (!int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var underline))
			{
				return false;
			}

			state.Underline = (global::Microsoft.UI.Text.UnderlineType)underline;
			state.Strikethrough = f[4] == "1";

			var foregroundDefined = f[5] == "1";
			if (!byte.TryParse(f[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a)
				|| !byte.TryParse(f[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)
				|| !byte.TryParse(f[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g)
				|| !byte.TryParse(f[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
			{
				return false;
			}

			state.Foreground = foregroundDefined ? global::Windows.UI.Color.FromArgb(a, r, g, b) : null;

			if (!float.TryParse(f[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
			{
				return false;
			}

			state.Size = size;

			try
			{
				var name = Encoding.UTF8.GetString(Convert.FromBase64String(f[11]));
				state.Name = string.IsNullOrEmpty(name) ? null : name;
			}
			catch (FormatException)
			{
				return false;
			}

			return true;
		}
	}
}
