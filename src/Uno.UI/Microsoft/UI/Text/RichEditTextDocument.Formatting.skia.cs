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

		private CharacterFormatState DefaultFormatState()
		{
			var state = _defaultCharacterFormat.Clone();
			if (IsMathMode)
			{
				state.Name = MathFontFamilyName;
			}

			return state;
		}

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
		private void SpliceRuns(int start, int removeLength, int insertLength, bool preferForwardFormat = false, bool unlink = false, bool unhide = false)
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
					insertFormat = preferForwardFormat && removeEnd < oldLength
						? expanded[removeEnd].Clone()
						: start > 0
							? expanded[start - 1].Clone()
							: (oldLength > 0 ? expanded[0].Clone() : DefaultFormatState());
				}
			}
			else
			{
				insertFormat = DefaultFormatState();
			}

			if (unlink)
			{
				insertFormat.Link = null;
			}
			if (unhide)
			{
				insertFormat.Hidden = false;
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
		internal UnoTextCharacterFormat GetFormatOverRange(int start, int end, global::Microsoft.UI.Text.RangeGravity gravity = global::Microsoft.UI.Text.RangeGravity.UIBehavior)
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
				var preferForward = gravity is global::Microsoft.UI.Text.RangeGravity.Forward or global::Microsoft.UI.Text.RangeGravity.Inward;
				format.LoadFrom(preferForward && start < length
					? expanded[start]
					: (start > 0 ? expanded[start - 1] : expanded[0]));
				return format;
			}

			var first = expanded[start];
			bool allCapsUniform = true, backgroundUniform = true, boldUniform = true,
				fontStretchUniform = true, hiddenUniform = true, italicUniform = true,
				kerningUniform = true, languageTagUniform = true, outlineUniform = true,
				positionUniform = true, protectedUniform = true, smallCapsUniform = true,
				spacingUniform = true, strikeUniform = true, subscriptUniform = true,
				superscriptUniform = true, textScriptUniform = true, underlineUniform = true,
				foregroundUniform = true, sizeUniform = true, nameUniform = true, weightUniform = true, linkUniform = true;
			for (var i = start + 1; i < end; i++)
			{
				var s = expanded[i];
				allCapsUniform &= s.AllCaps == first.AllCaps;
				backgroundUniform &= Nullable.Equals(s.Background, first.Background);
				boldUniform &= s.Bold == first.Bold;
				fontStretchUniform &= s.FontStretch == first.FontStretch;
				hiddenUniform &= s.Hidden == first.Hidden;
				italicUniform &= s.Italic == first.Italic;
				kerningUniform &= s.Kerning.Equals(first.Kerning);
				languageTagUniform &= string.Equals(s.LanguageTag, first.LanguageTag, StringComparison.Ordinal);
				outlineUniform &= s.Outline == first.Outline;
				positionUniform &= s.Position.Equals(first.Position);
				protectedUniform &= s.ProtectedText == first.ProtectedText;
				smallCapsUniform &= s.SmallCaps == first.SmallCaps;
				spacingUniform &= s.Spacing.Equals(first.Spacing);
				strikeUniform &= s.Strikethrough == first.Strikethrough;
				subscriptUniform &= s.Subscript == first.Subscript;
				superscriptUniform &= s.Superscript == first.Superscript;
				textScriptUniform &= s.TextScript == first.TextScript;
				underlineUniform &= s.Underline == first.Underline;
				foregroundUniform &= Nullable.Equals(s.Foreground, first.Foreground);
				sizeUniform &= s.Size.Equals(first.Size);
				nameUniform &= string.Equals(s.Name, first.Name, StringComparison.Ordinal);
				weightUniform &= s.Weight == first.Weight;
				linkUniform &= string.Equals(s.Link, first.Link, StringComparison.Ordinal);
			}

			format.AllCapsEffect = allCapsUniform ? Effect(first.AllCaps) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			if (backgroundUniform)
			{
				format.BackgroundDefined = true;
				format.BackgroundAutomatic = first.Background is null;
				if (first.Background is { } background)
				{
					format.BackgroundValue = background;
				}
			}

			format.BoldEffect = boldUniform ? Effect(first.Bold) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			if (fontStretchUniform)
			{
				format.FontStretchValue = first.FontStretch;
				format.FontStretchDefined = true;
			}

			format.HiddenEffect = hiddenUniform ? Effect(first.Hidden) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.ItalicEffect = italicUniform ? Effect(first.Italic) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			if (kerningUniform)
			{
				format.KerningValue = first.Kerning;
				format.KerningDefined = true;
			}

			if (languageTagUniform)
			{
				format.LanguageTagValue = first.LanguageTag;
				format.LanguageTagDefined = true;
			}

			format.OutlineEffect = outlineUniform ? Effect(first.Outline) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			if (positionUniform)
			{
				format.PositionValue = first.Position;
				format.PositionDefined = true;
			}

			format.ProtectedTextEffect = protectedUniform ? Effect(first.ProtectedText) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.SmallCapsEffect = smallCapsUniform ? Effect(first.SmallCaps) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			if (spacingUniform)
			{
				format.SpacingValue = first.Spacing;
				format.SpacingDefined = true;
			}

			format.StrikethroughEffect = strikeUniform ? Effect(first.Strikethrough) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.SubscriptEffect = subscriptUniform ? Effect(first.Subscript) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.SuperscriptEffect = superscriptUniform ? Effect(first.Superscript) : global::Microsoft.UI.Text.FormatEffect.Undefined;
			format.TextScriptValue = textScriptUniform ? first.TextScript : global::Microsoft.UI.Text.TextScript.Undefined;
			format.UnderlineValue = underlineUniform ? first.Underline : global::Microsoft.UI.Text.UnderlineType.Undefined;
			if (foregroundUniform)
			{
				format.ForegroundDefined = true;
				format.ForegroundAutomatic = first.Foreground is null;
				if (first.Foreground is { } fg)
				{
					format.ForegroundValue = fg;
				}
			}

			if (sizeUniform)
			{
				format.SizeValue = first.Size;
				format.SizeDefined = true;
			}

			if (nameUniform)
			{
				format.NameValue = first.Name;
				format.NameDefined = true;
			}

			if (weightUniform)
			{
				format.WeightValue = first.Weight;
				format.WeightDefined = true;
			}

			format.LinkTypeValue = linkUniform
				? (first.Link is null ? global::Microsoft.UI.Text.LinkType.NotALink : global::Microsoft.UI.Text.LinkType.FriendlyLinkName)
				: global::Microsoft.UI.Text.LinkType.Undefined;

			return format;
		}

		/// <summary>Applies the defined properties of <paramref name="format"/> over [start, end).</summary>
		internal void SetFormatOverRange(int start, int end, UnoTextCharacterFormat format, global::Microsoft.UI.Text.RangeGravity gravity = global::Microsoft.UI.Text.RangeGravity.UIBehavior)
		{
			SyncRunsToLength(_plainText.Length);
			var length = _plainText.Length;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			if (start == end)
			{
				ThrowIfNotEditable(start, end, gravity is global::Microsoft.UI.Text.RangeGravity.Forward or global::Microsoft.UI.Text.RangeGravity.Inward);

				// Applying a character format at a collapsed caret establishes the pending insertion-point
				// format (applied to the next typed/inserted text) rather than mutating any existing text.
				var preferForward = gravity is global::Microsoft.UI.Text.RangeGravity.Forward or global::Microsoft.UI.Text.RangeGravity.Inward;
				var basis = ResolveCaretBasisFormat(start, preferForward);
				ApplyCharacterFormatToState(basis, format);
				_pendingCaretFormat = basis;
				_pendingCaretPosition = start;
				return;
			}

			var onlyRemovingProtection = format.ProtectedTextEffect == global::Microsoft.UI.Text.FormatEffect.Off
				&& WouldOnlyRemoveProtection(start, end, format);
			if (IsOwnerReadOnly)
			{
				throw new UnauthorizedAccessException("The text range cannot be edited.");
			}
			if (!onlyRemovingProtection)
			{
				ThrowIfNotEditable(start, end);
			}

			MutateWithUndo(() => ApplyFormatOverRange(start, end, state => ApplyCharacterFormatToState(state, format)));
		}

		private bool WouldOnlyRemoveProtection(int start, int end, UnoTextCharacterFormat format)
		{
			var states = ExpandRunsRaw();
			for (var i = start; i < end; i++)
			{
				var original = states[i];
				var candidate = original.Clone();
				ApplyCharacterFormatToState(candidate, format);
				candidate.ProtectedText = original.ProtectedText;
				if (!candidate.Equals(original))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// The basis a pending caret format accumulates onto: an existing pending format at the same
		/// caret, else the character to the left (what newly typed text inherits), else the character to
		/// the right, else the document default.
		/// </summary>
		private CharacterFormatState ResolveCaretBasisFormat(int position, bool preferForward)
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

			var index = Math.Clamp(preferForward && position < expanded.Count ? position : (position > 0 ? position - 1 : 0), 0, expanded.Count - 1);
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

		internal string GetLink(int start, int end, out int linkStart, out int linkEnd)
		{
			SyncRunsToLength(_plainText.Length);
			linkStart = Math.Clamp(start, 0, _plainText.Length);
			linkEnd = Math.Clamp(end, linkStart, _plainText.Length);
			if (_plainText.Length == 0)
			{
				return string.Empty;
			}

			var states = ExpandRunsRaw();
			var probe = linkStart < linkEnd
				? linkStart
				: linkStart < states.Count && states[linkStart].Link is not null
					? linkStart
					: Math.Max(0, linkStart - 1);
			var link = states[probe].Link;
			if (link is null)
			{
				return string.Empty;
			}

			linkStart = probe;
			while (linkStart > 0 && string.Equals(states[linkStart - 1].Link, link, StringComparison.Ordinal))
			{
				linkStart--;
			}

			linkEnd = probe + 1;
			while (linkEnd < states.Count && string.Equals(states[linkEnd].Link, link, StringComparison.Ordinal))
			{
				linkEnd++;
			}

			return link;
		}

		internal void SetLink(int start, int end, string? value)
		{
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			if (start == end)
			{
				throw new ArgumentException("A link requires a nondegenerate range.", nameof(start));
			}
			ThrowIfNotEditable(start, end);

			var normalized = NormalizeLink(value);
			MutateWithUndo(() => ApplyFormatOverRange(start, end, state => state.Link = normalized));
		}

		private static string? NormalizeLink(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}

			var start = value[0] == '\ufddf' ? 1 : 0;
			if (value.Length - start < 2 || value[start] != '"' || value[value.Length - 1] != '"')
			{
				throw new ArgumentException("The URL must be enclosed in quotes.", nameof(value));
			}

			return value;
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
			if (format.AllCapsEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.AllCaps = ResolveEffect(format.AllCapsEffect, state.AllCaps);
			}

			if (format.BackgroundDefined)
			{
				state.Background = format.BackgroundAutomatic ? null : format.BackgroundValue;
			}

			if (format.BoldEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Bold = ResolveEffect(format.BoldEffect, state.Bold);
				state.WeightExplicit = true;
				if (!format.WeightDefined)
				{
					state.Weight = state.Bold ? 700 : 400;
				}
			}

			if (format.WeightDefined)
			{
				state.Weight = format.WeightValue;
				state.Bold = state.Weight >= 600;
				state.WeightExplicit = true;
			}

			if (format.FontStretchDefined)
			{
				state.FontStretch = format.FontStretchValue;
			}

			if (format.HiddenEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Hidden = ResolveEffect(format.HiddenEffect, state.Hidden);
			}

			if (format.ItalicEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Italic = ResolveEffect(format.ItalicEffect, state.Italic);
			}

			if (format.KerningDefined)
			{
				state.Kerning = format.KerningValue;
			}

			if (format.LanguageTagDefined)
			{
				state.LanguageTag = format.LanguageTagValue;
			}

			if (format.OutlineEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Outline = ResolveEffect(format.OutlineEffect, state.Outline);
			}

			if (format.PositionDefined)
			{
				state.Position = format.PositionValue;
			}

			if (format.ProtectedTextEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.ProtectedText = ResolveEffect(format.ProtectedTextEffect, state.ProtectedText);
			}

			if (format.SmallCapsEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.SmallCaps = ResolveEffect(format.SmallCapsEffect, state.SmallCaps);
			}

			if (format.SpacingDefined)
			{
				state.Spacing = format.SpacingValue;
			}

			if (format.StrikethroughEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Strikethrough = ResolveEffect(format.StrikethroughEffect, state.Strikethrough);
			}

			if (format.SubscriptEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Subscript = ResolveEffect(format.SubscriptEffect, state.Subscript);
			}

			if (format.SuperscriptEffect != global::Microsoft.UI.Text.FormatEffect.Undefined)
			{
				state.Superscript = ResolveEffect(format.SuperscriptEffect, state.Superscript);
			}

			if (format.TextScriptValue != global::Microsoft.UI.Text.TextScript.Undefined)
			{
				state.TextScript = format.TextScriptValue;
			}

			if (format.UnderlineValue != global::Microsoft.UI.Text.UnderlineType.Undefined)
			{
				state.Underline = format.UnderlineValue;
			}

			if (format.ForegroundDefined)
			{
				state.Foreground = format.ForegroundAutomatic ? null : format.ForegroundValue;
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
			format.LoadFrom(DefaultFormatState());
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
