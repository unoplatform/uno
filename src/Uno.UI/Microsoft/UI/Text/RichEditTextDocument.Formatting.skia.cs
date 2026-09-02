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
		private readonly IndexedRunCollection<FormatRun> _runs = new(
			static run => run.Length,
			static (run, length) => run.Length = length);
		private int _visualCharacterFormattingLength;

		// The document's default character formatting: the basis for newly inserted text and empty
		// documents (see DefaultFormatState). Exposed via Get/SetDefaultCharacterFormat. This is
		// document-level configuration and is intentionally not part of undo history.
		private readonly CharacterFormatState _defaultCharacterFormat = new();

		// Pending caret ("insertion point") character format. When a character format is applied at a
		// collapsed caret it is not written to any existing character but remembered here and applied to
		// the next inserted text at this position (WinUI's insertion-point format). Cleared when the
		// caret moves elsewhere or once consumed by an insert. Transient — not part of undo history.
		private CharacterFormatState? _pendingCaretFormat;
		private int _pendingCaretPosition = -1;

		/// <summary>The current formatting runs, reconciled to the plain-text length (for rendering).</summary>
		internal IndexedRunCollection<FormatRun> FormatRuns
		{
			get
			{
				SyncRunsToLength(_textBuffer.Length);
				return _runs;
			}
		}

		internal int[] GetCharacterFormatBoundaries()
		{
			SyncRunsToLength(_textBuffer.Length);
			if (_runs.Count == 0)
			{
				return new[] { 0 };
			}

			var boundaries = new int[_runs.Count + 1];
			for (var i = 0; i < _runs.Count; i++)
			{
				boundaries[i] = GetRunStart(i);
			}
			boundaries[^1] = _runs.TotalLength;
			return boundaries;
		}

		internal int CharacterRunCount
		{
			get
			{
				SyncRunsToLength(_textBuffer.Length);
				return _runs.Count;
			}
		}

		internal bool HasVisualCharacterFormatting => _visualCharacterFormattingLength != 0;

		private CharacterFormatState DefaultFormatState()
		{
			var state = _defaultCharacterFormat.Clone();
			if (IsMathMode)
			{
				state.Name = MathRenderingFontFamilyName;
			}

			return state;
		}

		private void SetRuns(List<FormatRun> runs)
		{
			_runs.Reset(runs);
			_visualCharacterFormattingLength = 0;
			foreach (var run in _runs)
			{
				if (HasVisualCharacterFormattingState(run.Format))
				{
					_visualCharacterFormattingLength = checked(_visualCharacterFormattingLength + run.Length);
				}
			}
		}

		private int GetRunStart(int runIndex) => _runs.GetStart(runIndex);

		internal int FindCharacterRunIndexForRender(int position)
		{
			SyncRunsToLength(_textBuffer.Length);
			return position == _textBuffer.Length ? _runs.Count : FindRunIndex(position);
		}

		internal int GetCharacterRunStartForRender(int runIndex) => GetRunStart(runIndex);

		internal int GetCharacterRunEndForRender(int runIndex) => _runs.GetEnd(runIndex);

		private int FindRunIndex(int position)
		{
			if ((uint)position >= (uint)_runs.TotalLength)
			{
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			return _runs.FindIndex(position);
		}

		private CharacterFormatState GetFormatAt(int position) => _runs[FindRunIndex(position)].Format;

		private static void AppendRun(List<FormatRun> runs, int length, CharacterFormatState format, bool clone = true)
		{
			if (length <= 0)
			{
				return;
			}

			if (runs.Count > 0 && CharacterFormatState.CanCoalesce(runs[^1].Format, format))
			{
				runs[^1].Length += length;
			}
			else
			{
				runs.Add(new FormatRun(length, clone ? format.Clone() : format));
			}
		}

		private void ReplaceRuns(int start, int end, IReadOnlyList<FormatRun> insertedRuns)
		{
			var length = _runs.TotalLength;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);

			var startRun = start == length ? _runs.Count : FindRunIndex(start);
			var startOffset = startRun == _runs.Count ? 0 : start - GetRunStart(startRun);
			var endRun = end == length ? _runs.Count : FindRunIndex(end);
			var endOffset = endRun == _runs.Count ? 0 : end - GetRunStart(endRun);
			var removeCount = endRun - startRun + (endRun < _runs.Count && endOffset > 0 ? 1 : 0);
			var replacement = new List<FormatRun>(insertedRuns.Count + 2);
			var removedVisualLength = 0;
			for (var i = 0; i < removeCount; i++)
			{
				var removed = _runs[startRun + i];
				if (HasVisualCharacterFormattingState(removed.Format))
				{
					removedVisualLength = checked(removedVisualLength + removed.Length);
				}
			}

			if (startRun < _runs.Count && startOffset > 0)
			{
				AppendRun(replacement, startOffset, _runs[startRun].Format);
			}

			for (var i = 0; i < insertedRuns.Count; i++)
			{
				AppendRun(replacement, insertedRuns[i].Length, insertedRuns[i].Format);
			}

			if (endRun < _runs.Count && endOffset > 0)
			{
				AppendRun(replacement, _runs[endRun].Length - endOffset, _runs[endRun].Format);
			}

			_runs.ReplaceRange(startRun, removeCount, replacement);

			var replacementVisualLength = 0;
			foreach (var run in replacement)
			{
				if (HasVisualCharacterFormattingState(run.Format))
				{
					replacementVisualLength = checked(replacementVisualLength + run.Length);
				}
			}
			_visualCharacterFormattingLength = checked(
				_visualCharacterFormattingLength - removedVisualLength + replacementVisualLength);
			CoalesceRunsAt(startRun);
		}

		private static bool HasVisualCharacterFormattingState(CharacterFormatState format)
			=> format.Bold
				|| format.AllCaps
				|| format.WeightExplicit
				|| format.Weight != 400
				|| format.Background is not null
				|| format.Hidden
				|| format.Italic
				|| format.FontStretch != global::Windows.UI.Text.FontStretch.Normal
				|| format.Kerning != 0
				|| !string.IsNullOrEmpty(format.LanguageTag)
				|| format.Outline
				|| format.Position != 0
				|| format.SmallCaps
				|| format.Strikethrough
				|| format.Subscript
				|| format.Superscript
				|| format.TextScript is not global::Microsoft.UI.Text.TextScript.Default
					and not global::Microsoft.UI.Text.TextScript.Undefined
				|| format.Underline is not global::Microsoft.UI.Text.UnderlineType.None
					and not global::Microsoft.UI.Text.UnderlineType.Undefined
				|| format.Foreground is not null
				|| format.Spacing != 0
				|| format.Size > 0
				|| !string.IsNullOrEmpty(format.Name)
				|| format.Link is not null
				|| format.InlineImage is not null;

		internal bool IsVisualCharacterFormattingProfileValid()
		{
			var length = 0;
			foreach (var run in _runs)
			{
				if (HasVisualCharacterFormattingState(run.Format))
				{
					length += run.Length;
				}
			}
			return length == _visualCharacterFormattingLength;
		}

		private void CoalesceRunsAt(int index)
		{
			index = Math.Max(1, index);
			var end = Math.Min(_runs.Count - 1, index + 2);
			while (index <= end && index < _runs.Count)
			{
				if (CharacterFormatState.CanCoalesce(_runs[index - 1].Format, _runs[index].Format))
				{
					_runs.SetLength(index - 1, checked(_runs[index - 1].Length + _runs[index].Length));
					_runs.RemoveAt(index);
					end--;
				}
				else
				{
					index++;
				}
			}
		}

		private List<FormatRun> TransformRuns(int start, int end, Action<CharacterFormatState> apply)
		{
			var transformed = new List<FormatRun>();
			if (start >= end)
			{
				return transformed;
			}

			var cursor = _runs.GetCursor(FindRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				var intersectionStart = Math.Max(start, runStart);
				var intersectionEnd = Math.Min(end, runEnd);
				if (intersectionStart < intersectionEnd)
				{
					var state = cursor.Current.Format.Clone();
					apply(state);
					AppendRun(transformed, intersectionEnd - intersectionStart, state, clone: false);
				}
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}

			return transformed;
		}

		/// <summary>Reconciles the run lengths so they sum exactly to <paramref name="length"/>.</summary>
		private void SyncRunsToLength(int length)
		{
			var current = _runs.TotalLength;

			if (current == length)
			{
				return;
			}

			if (current < length)
			{
				var appended = new List<FormatRun> { new(length - current, DefaultFormatState()) };
				ReplaceRuns(current, current, appended);
			}
			else
			{
				ReplaceRuns(length, current, Array.Empty<FormatRun>());
			}
		}

		/// <summary>Resets formatting to a single default run of <paramref name="length"/> characters.</summary>
		private void ResetRuns(int length)
			=> SetRuns(length > 0
				? new List<FormatRun> { new(length, DefaultFormatState()) }
				: new List<FormatRun>());

		private void ApplyUnicodeBidiScripts(int start, string text)
		{
			if (text.Length == 0)
			{
				return;
			}

			SyncRunsToLength(_textBuffer.Length);
			var baseFormat = GetFormatAt(start).Clone();
			var replacement = new List<FormatRun>();
			var segmentStart = 0;
			var segmentScript = GetUnicodeBidiScript(text, 0, out var codeUnitLength);
			var position = codeUnitLength;
			var transitions = 0;
			while (position < text.Length)
			{
				var script = GetUnicodeBidiScript(text, position, out codeUnitLength);
				if (script != segmentScript)
				{
					AppendUnicodeBidiRun(replacement, position - segmentStart, baseFormat, segmentScript);
					segmentStart = position;
					segmentScript = script;
					if (++transitions >= 4096)
					{
						segmentScript = null;
						position = text.Length;
						break;
					}
				}
				position += codeUnitLength;
			}

			AppendUnicodeBidiRun(replacement, text.Length - segmentStart, baseFormat, segmentScript);
			ReplaceRuns(start, start + text.Length, replacement);
		}

		private static void AppendUnicodeBidiRun(
			List<FormatRun> runs,
			int length,
			CharacterFormatState baseFormat,
			global::Microsoft.UI.Text.TextScript? script)
		{
			var format = baseFormat.Clone();
			if (script is { } resolvedScript)
			{
				format.TextScript = resolvedScript;
			}
			AppendRun(runs, length, format, clone: false);
		}

		private static global::Microsoft.UI.Text.TextScript? GetUnicodeBidiScript(
			string text,
			int index,
			out int codeUnitLength)
		{
			var value = text[index];
			var isPair = char.IsHighSurrogate(value)
				&& index + 1 < text.Length
				&& char.IsLowSurrogate(text[index + 1]);
			var codePoint = isPair ? char.ConvertToUtf32(value, text[index + 1]) : value;
			codeUnitLength = isPair ? 2 : 1;
			if (codePoint is >= 0x0590 and <= 0x05ff or >= 0xfb1d and <= 0xfb4f)
			{
				return global::Microsoft.UI.Text.TextScript.Hebrew;
			}
			if (codePoint is >= 0x0600 and <= 0x06ff
				or >= 0x0750 and <= 0x077f
				or >= 0x0870 and <= 0x089f
				or >= 0x08a0 and <= 0x08ff
				or >= 0xfb50 and <= 0xfdff
				or >= 0xfe70 and <= 0xfeff
				or >= 0x1ee00 and <= 0x1eeff)
			{
				return global::Microsoft.UI.Text.TextScript.Arabic;
			}
			if (codePoint is >= 0x0700 and <= 0x074f or >= 0x0860 and <= 0x086f)
			{
				return global::Microsoft.UI.Text.TextScript.Syriac;
			}
			if (codePoint is >= 0x0780 and <= 0x07bf)
			{
				return global::Microsoft.UI.Text.TextScript.Thaana;
			}

			return null;
		}

		/// <summary>
		/// Splices the run model to match a text edit that removed <paramref name="removeLength"/>
		/// characters at <paramref name="start"/> and inserted <paramref name="insertLength"/> new ones.
		/// Must be called while <see cref="_runs"/> still reflect the pre-edit text length.
		/// </summary>
		private void SpliceRuns(int start, int removeLength, int insertLength, bool preferForwardFormat = false, bool unlink = false, bool unhide = false)
		{
			var oldLength = _runs.TotalLength;
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
						? GetFormatAt(removeEnd).Clone()
						: start > 0
							? GetFormatAt(start - 1).Clone()
							: (oldLength > 0 ? GetFormatAt(0).Clone() : DefaultFormatState());
				}
			}
			else
			{
				insertFormat = DefaultFormatState();
			}

			insertFormat.InlineImage = null;
			if (insertFormat.Link is null)
			{
				insertFormat.TextObjectIdentity = null;
			}
			if (unlink)
			{
				insertFormat.Link = null;
				insertFormat.LinkAnchor = null;
				insertFormat.TextObjectIdentity = null;
			}
			if (unhide)
			{
				insertFormat.Hidden = false;
			}

			var insertedRuns = insertLength > 0
				? new[] { new FormatRun(insertLength, insertFormat) }
				: Array.Empty<FormatRun>();
			ReplaceRuns(start, removeEnd, insertedRuns);
		}

		private void ApplyFormatOverRange(int start, int end, Action<CharacterFormatState> apply)
		{
			SyncRunsToLength(_textBuffer.Length);
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			if (start >= end)
			{
				return;
			}

			ReplaceRuns(start, end, TransformRuns(start, end, apply));
		}

		/// <summary>
		/// Builds a tri-state character format describing the formatting over [start, end): each tracked
		/// property is the common value where the characters agree, otherwise "undefined".
		/// </summary>
		internal UnoTextCharacterFormat GetFormatOverRange(int start, int end, global::Microsoft.UI.Text.RangeGravity gravity = global::Microsoft.UI.Text.RangeGravity.UIBehavior)
		{
			SyncRunsToLength(_textBuffer.Length);
			var length = _textBuffer.Length;
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

			if (start == end)
			{
				// A degenerate range reports the formatting that newly typed text would take.
				var preferForward = gravity is global::Microsoft.UI.Text.RangeGravity.Forward or global::Microsoft.UI.Text.RangeGravity.Inward;
				format.LoadFrom(preferForward && start < length
					? GetFormatAt(start)
					: (start > 0 ? GetFormatAt(start - 1) : GetFormatAt(0)));
				return format;
			}

			var firstRunIndex = FindRunIndex(start);
			var first = _runs[firstRunIndex].Format;
			bool allCapsUniform = true, backgroundUniform = true, boldUniform = true,
				fontStretchUniform = true, hiddenUniform = true, italicUniform = true,
				kerningUniform = true, languageTagUniform = true, outlineUniform = true,
				positionUniform = true, protectedUniform = true, smallCapsUniform = true,
				spacingUniform = true, strikeUniform = true, subscriptUniform = true,
				superscriptUniform = true, textScriptUniform = true, underlineUniform = true,
				foregroundUniform = true, sizeUniform = true, nameUniform = true, weightUniform = true, linkUniform = true;
			var cursor = _runs.GetCursor(firstRunIndex + 1);
			while (cursor.IsValid && cursor.Start < end)
			{
				var s = cursor.Current.Format;
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
				cursor.MoveNext();
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
			SyncRunsToLength(_textBuffer.Length);
			var length = _textBuffer.Length;
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

			format = (UnoTextCharacterFormat)format.GetClone();
			ResolveRangeToggleEffects(format, GetFormatOverRange(start, end, gravity));
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

			MutateWithUndo(
				() => ApplyFormatOverRange(start, end, state => ApplyCharacterFormatToState(state, format)),
				characterRange: new HistoryRange(start, end));
		}

		private static void ResolveRangeToggleEffects(UnoTextCharacterFormat requested, UnoTextCharacterFormat current)
		{
			requested.AllCapsEffect = ResolveRangeToggle(requested.AllCapsEffect, current.AllCapsEffect);
			requested.BoldEffect = ResolveRangeToggle(requested.BoldEffect, current.BoldEffect);
			requested.HiddenEffect = ResolveRangeToggle(requested.HiddenEffect, current.HiddenEffect);
			requested.ItalicEffect = ResolveRangeToggle(requested.ItalicEffect, current.ItalicEffect);
			requested.OutlineEffect = ResolveRangeToggle(requested.OutlineEffect, current.OutlineEffect);
			requested.ProtectedTextEffect = ResolveRangeToggle(requested.ProtectedTextEffect, current.ProtectedTextEffect);
			requested.SmallCapsEffect = ResolveRangeToggle(requested.SmallCapsEffect, current.SmallCapsEffect);
			requested.StrikethroughEffect = ResolveRangeToggle(requested.StrikethroughEffect, current.StrikethroughEffect);
			requested.SubscriptEffect = ResolveRangeToggle(requested.SubscriptEffect, current.SubscriptEffect);
			requested.SuperscriptEffect = ResolveRangeToggle(requested.SuperscriptEffect, current.SuperscriptEffect);
		}

		private static global::Microsoft.UI.Text.FormatEffect ResolveRangeToggle(
			global::Microsoft.UI.Text.FormatEffect requested,
			global::Microsoft.UI.Text.FormatEffect current)
			=> requested == global::Microsoft.UI.Text.FormatEffect.Toggle
				? current == global::Microsoft.UI.Text.FormatEffect.Off
					? global::Microsoft.UI.Text.FormatEffect.On
					: global::Microsoft.UI.Text.FormatEffect.Off
				: requested;

		private bool WouldOnlyRemoveProtection(int start, int end, UnoTextCharacterFormat format)
		{
			if (start >= end)
			{
				return true;
			}

			var runIndex = FindRunIndex(start);
			while (runIndex < _runs.Count && GetRunStart(runIndex) < end)
			{
				var original = _runs[runIndex].Format;
				var candidate = original.Clone();
				ApplyCharacterFormatToState(candidate, format);
				candidate.ProtectedText = original.ProtectedText;
				if (!candidate.Equals(original))
				{
					return false;
				}
				runIndex++;
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

			if (_runs.Count == 0)
			{
				return DefaultFormatState();
			}

			var length = _runs.TotalLength;
			var index = Math.Clamp(preferForward && position < length ? position : (position > 0 ? position - 1 : 0), 0, length - 1);
			return GetFormatAt(index).Clone();
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
			SyncRunsToLength(_textBuffer.Length);
			linkStart = Math.Clamp(start, 0, _textBuffer.Length);
			linkEnd = Math.Clamp(end, linkStart, _textBuffer.Length);
			if (_textBuffer.Length == 0)
			{
				return string.Empty;
			}

			var probe = linkStart < linkEnd
				? linkStart
				: linkStart < _textBuffer.Length && GetFormatAt(linkStart).Link is not null
					? linkStart
					: Math.Max(0, linkStart - 1);
			var runIndex = FindRunIndex(probe);
			var link = _runs[runIndex].Format.Link;
			var anchor = _runs[runIndex].Format.LinkAnchor;
			var identity = _runs[runIndex].Format.TextObjectIdentity;
			if (link is null)
			{
				return string.Empty;
			}

			var firstRun = runIndex;
			while (firstRun > 0
				&& IsSameLink(_runs[firstRun - 1].Format))
			{
				firstRun--;
			}

			var lastRun = runIndex;
			while (lastRun + 1 < _runs.Count
				&& IsSameLink(_runs[lastRun + 1].Format))
			{
				lastRun++;
			}

			linkStart = GetRunStart(firstRun);
			linkEnd = _runs.GetEnd(lastRun);
			return link;

			bool IsSameLink(CharacterFormatState candidate)
				=> identity is not null
					? ReferenceEquals(candidate.TextObjectIdentity, identity)
					: string.Equals(candidate.Link, link, StringComparison.Ordinal)
						&& string.Equals(candidate.LinkAnchor, anchor, StringComparison.Ordinal);
		}

		internal void SetLink(int start, int end, string? value)
		{
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			if (start == end)
			{
				throw new ArgumentException("A link requires a nondegenerate range.", nameof(start));
			}
			ThrowIfNotEditable(start, end);

			var normalized = NormalizeLink(value);
			var identity = normalized is null ? null : new RichEditTextObjectIdentity();
			MutateWithUndo(
				() => ApplyFormatOverRange(start, end, state =>
				{
					state.Link = normalized;
					state.LinkAnchor = null;
					state.TextObjectIdentity = identity;
				}),
				characterRange: new HistoryRange(start, end));
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
		/// current state (WinUI's tomToggle). Undefined leaves it unchanged. Nondegenerate ranges
		/// normalize Toggle against their aggregate format before applying it to individual states.
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

		private static List<FormatRun> BuildRunsFromFragment(
			IReadOnlyList<FormatRun> source,
			int length,
			CharacterFormatState fallback,
			bool unhide = false,
			bool unlink = false)
		{
			var runs = new List<FormatRun>();
			var remaining = length;
			CharacterFormatState? previousSource = null;
			RichEditTextObjectIdentity? insertedIdentity = null;
			foreach (var run in source)
			{
				var runLength = Math.Min(remaining, run.Length);
				if (runLength <= 0)
				{
					break;
				}

				var state = run.Format.Clone();
				if (unhide)
				{
					state.Hidden = false;
				}
				if (unlink)
				{
					state.Link = null;
					state.LinkAnchor = null;
					state.TextObjectIdentity = null;
				}
				else if (state.Link is not null || state.InlineImage is not null)
				{
					if (previousSource is null || !CharacterFormatState.IsSameTextObject(previousSource, run.Format))
					{
						insertedIdentity = new RichEditTextObjectIdentity();
					}
					state.TextObjectIdentity = insertedIdentity;
				}
				else
				{
					state.TextObjectIdentity = null;
					insertedIdentity = null;
				}

				AppendRun(runs, runLength, state, clone: false);
				previousSource = run.Format;
				remaining -= runLength;
			}

			if (remaining > 0)
			{
				AppendRun(runs, remaining, fallback);
			}

			return runs;
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
