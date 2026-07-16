#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox Text Object Model document for Skia.
	//
	// This increment provides a working text core: SetText/GetText round-trip, a functional Text
	// Object Model surface (GetRange/Selection returning UnoTextRange/UnoTextSelection) that navigates
	// and edits the buffer and drives the owning RichEditBox's shared rendering, a functional
	// character-formatting run model (see RichEditTextDocument.Formatting.skia.cs and
	// UnoTextRange.CharacterFormat), a functional paragraph-formatting run model (see
	// RichEditTextDocument.ParagraphFormatting.skia.cs and UnoTextRange.ParagraphFormat), a
	// snapshot-based undo/redo stack over text and both formatting models
	// (CanUndo/CanRedo/Undo/Redo/UndoLimit) with grouping (BeginUndoGroup/EndUndoGroup), and display
	// batching (BatchDisplayUpdates/ApplyDisplayUpdates).
	//
	// Standard RTF transport, stream load/save, links, inline images, and MathML interchange are
	// supported. Full OpenType math layout, advanced RTF destinations, and visual projection of
	// non-alignment paragraph properties remain.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private readonly List<HistoryEntry> _undoStack = new();
		private readonly List<HistoryEntry> _redoStack = new();
		private readonly List<WeakReference<UnoTextRange>> _ranges = new();
		private string _plainText = string.Empty;
		private UnoTextSelection? _selection;
		private int _undoLimit = 100;
		private const long MaxFragmentImagePixels = 8L * 1024 * 1024;
		private const int MaxFragmentImageBytes = 4 * 1024 * 1024;
		private const int MaxFragmentImageCount = 64;
		private const long MaxUndoCost = 4 * 1024 * 1024;

		// Undo grouping: while a group is open, individual edits do not each push an undo entry.
		// Instead, a single snapshot captured when the outermost group opened is committed on close,
		// so BeginUndoGroup/EndUndoGroup collapse a run of edits into one undoable action.
		private int _undoGroupDepth;
		private Snapshot? _undoGroupSnapshot;
		private List<TextEdit>? _undoGroupTextEdits;

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

		// A point-in-time copy of the document used for undo/redo: the plain text plus deep clones of
		// the character-formatting runs and paragraph-formatting runs.
		private sealed record Snapshot(string Text, List<FormatRun> Runs, List<ParagraphRun> ParagraphRuns, string? MathML);

		// Exact text-coordinate mutation: remove RemoveLength characters at Start, then insert
		// InsertLength characters. History entries store the ordered edits that transform the current
		// document into their target snapshot, so repeated text and noncontiguous undo groups rebase live
		// ranges at the actual edit locations instead of inferring one ambiguous prefix/suffix diff.
		private readonly record struct TextEdit(int Start, int RemoveLength, int InsertLength);

		private sealed record HistoryEntry(Snapshot Target, List<TextEdit> RebaseEdits);

		/// <summary>The current plain-text content of the document.</summary>
		internal string PlainText => _plainText;

		/// <summary>The number of characters in the plain-text buffer.</summary>
		internal int TextLength => _plainText.Length;

		/// <summary>Returns the substring of the plain-text buffer between two clamped positions.</summary>
		internal string GetTextInRange(int start, int end)
		{
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			return _plainText.Substring(start, end - start);
		}

		internal string GetTextInRange(int start, int end, global::Microsoft.UI.Text.TextGetOptions options)
		{
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			if (start < end && options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.AdjustCrlf))
			{
				start = TextUnitNavigation.GetTextElementStart(_plainText, start);
			}

			var useObjectText = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.UseObjectText);
			var noHidden = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden);
			var text = useObjectText || noHidden
				? GetFilteredTextInRange(start, end, useObjectText, noHidden)
				: _plainText.Substring(start, end - start);
			if (options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.AllowFinalEop) && end == _plainText.Length)
			{
				text += '\r';
			}

			return ConvertTextForGetOptions(text, options);
		}

		private string GetFilteredTextInRange(int start, int end, bool useObjectText, bool noHidden)
		{
			SyncRunsToLength(_plainText.Length);
			var states = ExpandRunsRaw();
			var builder = new global::System.Text.StringBuilder();
			for (var i = start; i < end; i++)
			{
				if (noHidden && states[i].Hidden)
				{
					continue;
				}

				if (useObjectText && _plainText[i] == '\ufffc' && states[i].InlineImage is { } image)
				{
					builder.Append(image.AlternateText);
				}
				else
				{
					builder.Append(_plainText[i]);
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Replaces the plain-text between <paramref name="start"/> and <paramref name="end"/> with
		/// <paramref name="replacement"/>, splices the formatting runs accordingly and re-renders. Used
		/// by <see cref="UnoTextRange"/> editing.
		/// </summary>
		internal int ReplaceRange(int start, int end, string replacement, UnoTextRange? sourceRange = null, bool unlink = false, bool unhide = false)
		{
			var originalLength = _plainText.Length;
			start = Math.Clamp(start, 0, originalLength);
			end = Math.Clamp(end, start, originalLength);
			ThrowIfNotEditable(start, end, sourceRange?.UsesForwardCharacterFormatting == true);
			var insert = NormalizeLineEndings(replacement ?? string.Empty);
			insert = _owner.ClampInsertToMaxLength(insert, originalLength, start, end);

			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, _selection);
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				MutateWithUndo(() =>
				{
					var text = _plainText;

					// Keep the run model aligned with the pre-edit text, then splice it in lock-step with the
					// text edit so inserted characters inherit the neighbouring formatting.
					SyncRunsToLength(text.Length);
					SyncParagraphRunsToLength(text.Length);
					SpliceRuns(start, end - start, insert.Length, sourceRange?.UsesForwardCharacterFormatting == true, unlink, unhide);
					SpliceParagraphRuns(start, end - start, insert.Length);
					_plainText = string.Concat(text.AsSpan(0, start), insert.AsSpan(), text.AsSpan(end));
					NormalizeParagraphRuns();
					_mathML = null;
				}, () => RebaseRanges(start, end, insert.Length, sourceRange), new TextEdit(start, end - start, insert.Length));
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			// The pending caret format (if any) has now been consumed by the splice above, or the caret
			// context has changed by an edit that didn't consume it; either way it no longer applies.
			ClearPendingCaretFormat();
			return insert.Length;
		}

		internal void TrackRange(UnoTextRange range) => _ranges.Add(new WeakReference<UnoTextRange>(range));

		internal string CoerceTypedText(string value) => _owner.CoerceCasing(NormalizeLineEndings(value));

		internal int GetImportCharacterLimit(int start, int end)
		{
			var maxLength = _owner.MaxLength;
			return maxLength <= 0
				? 262_144
				: Math.Max(0, maxLength - (_plainText.Length - (end - start)));
		}

		internal bool IsSelectionMutationInProgress => _selectionMutationDepth > 0;

		internal long SelectionChangeVersion => _selectionChangeVersion;

		internal bool IsOwnerReadOnly => _owner.IsReadOnly;

		internal RichTextFragment CaptureFragment(int start, int end)
		{
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			SyncRunsToLength(_plainText.Length);
			SyncParagraphRunsToLength(_plainText.Length);

			var characterStates = ExpandRunsRaw();
			var paragraphStates = ExpandParagraphRunsRaw();
			return new RichTextFragment(
				_plainText.Substring(start, end - start),
				CloneCharacterStates(characterStates, start, end),
				CloneParagraphStates(paragraphStates, start, end));
		}

		internal RichTextFragment CaptureFragment(int start, int end, bool noHidden)
		{
			var fragment = CaptureFragment(start, end);
			if (!noHidden)
			{
				return fragment;
			}

			var text = new global::System.Text.StringBuilder(fragment.Text.Length);
			var characterStates = new List<CharacterFormatState>();
			var paragraphStates = new List<ParagraphFormatState>();
			for (var i = 0; i < fragment.Text.Length; i++)
			{
				if (i < fragment.CharacterStates.Count && fragment.CharacterStates[i].Hidden)
				{
					continue;
				}

				text.Append(fragment.Text[i]);
				characterStates.Add(i < fragment.CharacterStates.Count ? fragment.CharacterStates[i].Clone() : DefaultFormatState());
				paragraphStates.Add(i < fragment.ParagraphStates.Count ? fragment.ParagraphStates[i].Clone() : DefaultParagraphState());
			}

			return new RichTextFragment(text.ToString(), characterStates, paragraphStates);
		}

		internal int ReplaceRangeWithFragment(int start, int end, RichTextFragment fragment, UnoTextRange? sourceRange, bool unhide = false)
		{
			ValidateFragment(fragment);
			var originalLength = _plainText.Length;
			start = Math.Clamp(start, 0, originalLength);
			end = Math.Clamp(end, start, originalLength);
			ValidateResultingImageBudget(start, end, fragment);
			ThrowIfNotEditable(start, end, sourceRange?.UsesForwardCharacterFormatting == true);
			var insert = _owner.ClampInsertToMaxLength(fragment.Text, originalLength, start, end);
			var insertedLength = insert.Length;
			var textChanged = end - start != insertedLength
				|| !_plainText.AsSpan(start, end - start).Equals(insert.AsSpan(), StringComparison.Ordinal);
			var selectionMutation = sourceRange is not null && ReferenceEquals(sourceRange, _selection);
			if (selectionMutation)
			{
				_selectionMutationDepth++;
			}

			try
			{
				MutateWithUndo(() =>
				{
					SyncRunsToLength(originalLength);
					SyncParagraphRunsToLength(originalLength);
					var oldCharacterStates = ExpandRunsRaw();
					var oldParagraphStates = ExpandParagraphRunsRaw();
					var removeLength = end - start;
					var characterStates = new List<CharacterFormatState>(originalLength - removeLength + insertedLength);
					var paragraphStates = new List<ParagraphFormatState>(originalLength - removeLength + insertedLength);

					for (var i = 0; i < start; i++)
					{
						characterStates.Add(oldCharacterStates[i]);
						paragraphStates.Add(oldParagraphStates[i]);
					}

					var characterBasis = start > 0
						? oldCharacterStates[start - 1]
						: (originalLength > 0 ? oldCharacterStates[0] : DefaultFormatState());
					var paragraphBasis = start > 0
						? oldParagraphStates[start - 1]
						: (originalLength > 0 ? oldParagraphStates[0] : DefaultParagraphState());
					for (var i = 0; i < insertedLength; i++)
					{
						var character = i < fragment.CharacterStates.Count
							? fragment.CharacterStates[i].Clone()
							: characterBasis.Clone();
						if (unhide)
						{
							character.Hidden = false;
						}
						characterStates.Add(character);
						paragraphStates.Add(i < fragment.ParagraphStates.Count
							? fragment.ParagraphStates[i].Clone()
							: paragraphBasis.Clone());
					}

					for (var i = end; i < originalLength; i++)
					{
						characterStates.Add(oldCharacterStates[i]);
						paragraphStates.Add(oldParagraphStates[i]);
					}

					_plainText = string.Concat(_plainText.AsSpan(0, start), insert.AsSpan(), _plainText.AsSpan(end));
					_runs = BuildRunsFromStates(characterStates);
					_paragraphRuns = BuildParagraphRunsFromStates(paragraphStates);
					NormalizeParagraphRuns();
					_mathML = null;
				}, () =>
				{
					if (textChanged)
					{
						RebaseRanges(start, end, insertedLength, sourceRange);
					}
				}, new TextEdit(start, end - start, insertedLength));
			}
			finally
			{
				if (selectionMutation)
				{
					_selectionMutationDepth--;
				}
			}

			ClearPendingCaretFormat();
			return insertedLength;
		}

		internal bool IsRangeProtected(int start, int end, bool preferForwardAtCaret = false)
		{
			SyncRunsToLength(_plainText.Length);
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			if (start == end)
			{
				if (_plainText.Length == 0)
				{
					return false;
				}

				var caretStates = ExpandRunsRaw();
				var index = preferForwardAtCaret && start < caretStates.Count
					? start
					: start > 0 ? start - 1 : 0;
				return caretStates[index].ProtectedText;
			}

			var states = ExpandRunsRaw();
			for (var i = start; i < end; i++)
			{
				if (states[i].ProtectedText)
				{
					return true;
				}
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

		private void SetDocumentFragment(RichTextFragment fragment, string? mathML = null)
		{
			var oldLength = _plainText.Length;
			ThrowIfNotEditable(0, oldLength);
			var text = NormalizeLineEndings(fragment.Text);
			var maxLength = _owner.MaxLength;
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = TextUnitNavigation.TruncateToUtf16Boundary(text, maxLength);
			}

			var selection = (UnoTextRange)Selection;
			var selectionWasNonzero = selection.StartPosition != 0 || selection.EndPosition != 0;
			var documentChanged = MutateWithUndo(() =>
			{
				var characterStates = new List<CharacterFormatState>(text.Length);
				var paragraphStates = new List<ParagraphFormatState>(text.Length);
				for (var i = 0; i < text.Length; i++)
				{
					characterStates.Add(i < fragment.CharacterStates.Count ? fragment.CharacterStates[i].Clone() : DefaultFormatState());
					paragraphStates.Add(i < fragment.ParagraphStates.Count ? fragment.ParagraphStates[i].Clone() : DefaultParagraphState());
				}

				_plainText = text;
				_runs = BuildRunsFromStates(characterStates);
				_paragraphRuns = BuildParagraphRunsFromStates(paragraphStates);
				_mathML = mathML;
				RebaseRanges(0, oldLength, text.Length, sourceRange: null);
				selection.SetRangeInternal(0, 0);
			}, textEdit: new TextEdit(0, oldLength, text.Length));
			if (!documentChanged && selectionWasNonzero)
			{
				_owner.OnDocumentTextChangedInteractive();
			}
			ClearPendingCaretFormat();
		}

		private static List<CharacterFormatState> CloneCharacterStates(List<CharacterFormatState> states, int start, int end)
		{
			var result = new List<CharacterFormatState>(end - start);
			for (var i = start; i < end; i++)
			{
				result.Add(states[i].Clone());
			}

			return result;
		}

		private static List<ParagraphFormatState> CloneParagraphStates(List<ParagraphFormatState> states, int start, int end)
		{
			var result = new List<ParagraphFormatState>(end - start);
			for (var i = start; i < end; i++)
			{
				result.Add(states[i].Clone());
			}

			return result;
		}

		private static void ValidateFragment(RichTextFragment fragment)
		{
			var images = 0;
			var imageBytes = 0;
			long imagePixels = 0;
			for (var i = 0; i < fragment.CharacterStates.Count; i++)
			{
				if (fragment.CharacterStates[i].InlineImage is { } image)
				{
					images++;
					imageBytes = checked(imageBytes + image.EncodedLength);
					imagePixels = checked(imagePixels + image.GetDecodedPixelCount());
				}
			}

			for (var i = 0; i < fragment.ParagraphStates.Count; i++)
			{
				if (fragment.ParagraphStates[i].Tabs.Count > ParagraphFormatState.MaxTabs)
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
			SyncRunsToLength(_plainText.Length);
			var images = 0;
			var imageBytes = 0;
			long imagePixels = 0;
			var states = ExpandRunsRaw();
			for (var i = 0; i < states.Count; i++)
			{
				if ((i < start || i >= end) && states[i].InlineImage is { } image)
				{
					AddImageCost(image, ref images, ref imageBytes, ref imagePixels);
				}
			}

			foreach (var state in fragment.CharacterStates)
			{
				if (state.InlineImage is { } image)
				{
					AddImageCost(image, ref images, ref imageBytes, ref imagePixels);
				}
			}

			if (images > MaxFragmentImageCount
				|| imageBytes > MaxFragmentImageBytes
				|| imagePixels > MaxFragmentImagePixels)
			{
				throw new ArgumentException("The document contains too much image data.", nameof(fragment));
			}
		}

		private static void AddImageCost(InlineImageState image, ref int images, ref int bytes, ref long pixels)
		{
			images++;
			bytes = checked(bytes + image.EncodedLength);
			pixels = checked(pixels + image.GetDecodedPixelCount());
		}

		private void RebaseRanges(int editStart, int editEnd, int insertLength, UnoTextRange? sourceRange, int? documentLength = null)
		{
			var rebasedDocumentLength = documentLength ?? _plainText.Length;
			for (var i = _ranges.Count - 1; i >= 0; i--)
			{
				if (!_ranges[i].TryGetTarget(out var range))
				{
					_ranges.RemoveAt(i);
					continue;
				}

				if (!ReferenceEquals(range, sourceRange))
				{
					range.RebaseAfterEdit(editStart, editEnd, insertLength, rebasedDocumentLength);
				}
			}
		}

		/// <summary>
		/// Runs a buffer/formatting mutation, capturing a before-snapshot for undo (unless nothing
		/// changed or undo is disabled), clearing the redo stack and re-rendering the owning control.
		/// </summary>
		private bool MutateWithUndo(Action mutate, Action? onChanged = null, TextEdit? textEdit = null)
		{
			var before = CaptureSnapshot();
			mutate();
			var textChanged = !string.Equals(_plainText, before.Text, StringComparison.Ordinal);
			var runsChanged = !RunsEqual(_runs, before.Runs);
			var paragraphRunsChanged = !ParagraphRunsEqual(_paragraphRuns, before.ParagraphRuns);
			if (!textChanged
				&& (runsChanged || paragraphRunsChanged)
				&& string.Equals(_mathML, before.MathML, StringComparison.Ordinal))
			{
				_mathML = null;
			}

			if (!textChanged
				&& !runsChanged
				&& !paragraphRunsChanged
				&& string.Equals(_mathML, before.MathML, StringComparison.Ordinal))
			{
				return false;
			}

			onChanged?.Invoke();

			if (_undoLimit != 0)
			{
				if (_undoGroupDepth > 0)
				{
					// Inside an open undo group: the group's single snapshot (captured at
					// BeginUndoGroup) is committed on close; each edit still invalidates redo.
					if (textChanged && textEdit is { } groupEdit)
					{
						_undoGroupTextEdits?.Add(groupEdit);
					}

					_redoStack.Clear();
				}
				else
				{
					var forwardEdits = textChanged && textEdit is { } edit
						? new List<TextEdit> { edit }
						: new List<TextEdit>();
					_undoStack.Add(new HistoryEntry(before, InvertTextEdits(forwardEdits)));
					TrimUndoStack();
					_redoStack.Clear();
				}
			}

			RequestRender(textChanged);
			return true;
		}

		// Trigger a re-render of the shared DisplayBlock, deferring while display updates are batched.
		private void RequestRender(bool isContentChanging)
		{
			if (_batchDepth > 0)
			{
				_pendingRender = true;
				_pendingContentChange |= isContentChanging;
				return;
			}

			_owner.OnDocumentTextChanged(isContentChanging);
		}

		private Snapshot CaptureSnapshot() => new(_plainText, CloneRuns(_runs), CloneParagraphRuns(_paragraphRuns), _mathML);

		private HistoryEntry RestoreHistoryEntry(HistoryEntry entry)
		{
			var current = CaptureSnapshot();
			var documentLength = current.Text.Length;
			foreach (var edit in entry.RebaseEdits)
			{
				var editEnd = edit.Start + edit.RemoveLength;
				documentLength += edit.InsertLength - edit.RemoveLength;
				RebaseRanges(edit.Start, editEnd, edit.InsertLength, sourceRange: null, documentLength);
			}

			_plainText = entry.Target.Text;
			_runs = CloneRuns(entry.Target.Runs);
			_paragraphRuns = CloneParagraphRuns(entry.Target.ParagraphRuns);
			_mathML = entry.Target.MathML;
			return new HistoryEntry(current, InvertTextEdits(entry.RebaseEdits));
		}

		private static List<TextEdit> InvertTextEdits(IReadOnlyList<TextEdit> edits)
		{
			var inverse = new List<TextEdit>(edits.Count);
			for (var i = edits.Count - 1; i >= 0; i--)
			{
				var edit = edits[i];
				inverse.Add(new TextEdit(edit.Start, edit.InsertLength, edit.RemoveLength));
			}

			return inverse;
		}

		private void TrimUndoStack()
		{
			long cost = 0;
			for (var i = _undoStack.Count - 1; i >= 0; i--)
			{
				cost += EstimateSnapshotCost(_undoStack[i].Target);
				if (cost > MaxUndoCost)
				{
					_undoStack.RemoveRange(0, i + 1);
					break;
				}
			}

			while (_undoLimit > 0 && _undoStack.Count > _undoLimit)
			{
				_undoStack.RemoveAt(0);
			}
		}

		private static long EstimateSnapshotCost(Snapshot snapshot)
		{
			long cost = snapshot.Text.Length * sizeof(char);
			foreach (var run in snapshot.Runs)
			{
				cost += 128;
				if (run.Format.InlineImage is { } image)
				{
					cost += image.EncodedLength + image.GetDecodedPixelCount() * 4;
				}
			}

			foreach (var run in snapshot.ParagraphRuns)
			{
				cost += 128 + run.Format.Tabs.Count * 16;
			}

			return cost;
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
		internal void SetSelectionRangeInternal(int start, int end, bool clearPendingCaretFormat = true)
		{
			if (clearPendingCaretFormat)
			{
				ClearPendingCaretFormatIfMoved(start, end);
			}

			((UnoTextRange)Selection).SetRangeInternal(start, end);
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
			=> _owner.TryGetLineBounds(position, out lineStart, out lineEnd, out lineIndex, out isLast);

		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetVerticalTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetPageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetPageTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetRangePageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> _owner.TryGetRangePageTarget(position, up, count, out target, out unitsMoved);

		internal bool TryGetVisibleRange(out int start, out int end)
			=> _owner.TryGetVisibleRange(out start, out end);

		// --- Geometry-backed coordinate mapping (delegates to the owning control's DisplayBlock layout) ---

		internal bool TryGetIndexRect(int index, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetIndexRect(index, options, out rect);

		internal bool TryGetIndexBaseline(int index, global::Microsoft.UI.Text.PointOptions options, out double baseline)
			=> _owner.TryGetIndexBaseline(index, options, out baseline);

		internal bool TryGetRangeRect(int start, int end, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetRangeRect(start, end, options, out rect);

		internal bool TryGetIndexFromPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, out int index)
			=> _owner.TryGetIndexFromPoint(point, options, out index);

		internal bool TryScrollRangeIntoView(int start, int end, global::Microsoft.UI.Text.PointOptions options)
			=> _owner.TryScrollRangeIntoView(start, end, options);

		/// <summary>
		/// Sets the text in this document to the specified plain text.
		/// </summary>
		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			ThrowIfNotEditable(0, _plainText.Length);

			if (options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf))
			{
				SetDocumentFragment(RichTextRtfCodec.Read(value ?? string.Empty, GetImportCharacterLimit(0, _plainText.Length)));
				return;
			}

			var text = NormalizeLineEndings(value ?? string.Empty);

			// WinUI clamps a programmatic SetText to the control's MaxLength (SetTextAdheresToMaxLength).
			var maxLength = _owner.MaxLength;
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = TextUnitNavigation.TruncateToUtf16Boundary(text, maxLength);
			}

			var oldLength = _plainText.Length;
			var selection = (UnoTextRange)Selection;
			var selectionWasNonzero = selection.StartPosition != 0 || selection.EndPosition != 0;
			var documentChanged = MutateWithUndo(() =>
			{
				_plainText = text;
				ResetRuns(text.Length);
				ResetParagraphRuns(text.Length);
				_mathML = null;
				RebaseRanges(0, oldLength, text.Length, sourceRange: null);
				selection.SetRangeInternal(0, 0);
			}, textEdit: new TextEdit(0, oldLength, text.Length));

			// A same-text/default-format SetText is a content no-op, but it still resets the selection.
			// Since MutateWithUndo does not render that case, publish the selection proposal explicitly.
			if (!documentChanged && selectionWasNonzero)
			{
				_owner.OnDocumentTextChangedInteractive();
			}
		}

		/// <summary>
		/// Gets the text in this document as plain text.
		/// </summary>
		public void GetText(global::Microsoft.UI.Text.TextGetOptions options, out string value)
		{
			value = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf)
				? RichTextRtfCodec.Write(CaptureFragment(0, _plainText.Length, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)))
				: GetTextInRange(0, _plainText.Length, options);
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

		/// <summary>
		/// Gets or sets the maximum number of actions that can be undone. Setting the limit to 0
		/// disables the undo/redo history.
		/// </summary>
		public uint UndoLimit
		{
			get => (uint)_undoLimit;
			set
			{
				_undoLimit = (int)Math.Min(value, int.MaxValue);
				if (_undoLimit == 0)
				{
					_undoStack.Clear();
					_redoStack.Clear();
				}
				else
				{
					TrimUndoStack();
				}
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the most recent undo action can be redone.
		/// </summary>
		public bool CanRedo() => _redoStack.Count > 0;

		/// <summary>
		/// Gets a value that indicates whether the most recent action can be undone.
		/// </summary>
		public bool CanUndo() => _undoStack.Count > 0;

		/// <summary>
		/// Redoes the most recent undo action.
		/// </summary>
		public void Redo()
		{
			ThrowIfNotEditable(0, _plainText.Length);
			if (_redoStack.Count == 0)
			{
				return;
			}

			var index = _redoStack.Count - 1;
			var next = _redoStack[index];
			_redoStack.RemoveAt(index);
			var isContentChanging = !string.Equals(_plainText, next.Target.Text, StringComparison.Ordinal);
			_undoStack.Add(RestoreHistoryEntry(next));
			RequestRender(isContentChanging);
		}

		/// <summary>
		/// Undoes the most recent action.
		/// </summary>
		public void Undo()
		{
			ThrowIfNotEditable(0, _plainText.Length);
			if (_undoStack.Count == 0)
			{
				return;
			}

			var index = _undoStack.Count - 1;
			var previous = _undoStack[index];
			_undoStack.RemoveAt(index);
			var isContentChanging = !string.Equals(_plainText, previous.Target.Text, StringComparison.Ordinal);
			_redoStack.Add(RestoreHistoryEntry(previous));
			RequestRender(isContentChanging);
		}

		/// <summary>
		/// Opens an undo group. Edits made until the matching <see cref="EndUndoGroup"/> are
		/// coalesced into a single undoable action. Groups may be nested.
		/// </summary>
		public void BeginUndoGroup()
		{
			if (_undoGroupDepth == 0)
			{
				_undoGroupSnapshot = CaptureSnapshot();
				_undoGroupTextEdits = new List<TextEdit>();
			}

			_undoGroupDepth++;
		}

		/// <summary>
		/// Closes the undo group opened by <see cref="BeginUndoGroup"/>. When the outermost group
		/// closes, the changes made since it opened are committed as a single undo entry.
		/// </summary>
		public void EndUndoGroup()
		{
			if (_undoGroupDepth == 0)
			{
				// Unbalanced EndUndoGroup: ignore, matching WinUI's tolerance of extra calls.
				return;
			}

			_undoGroupDepth--;
			if (_undoGroupDepth > 0)
			{
				return;
			}

			var groupStart = _undoGroupSnapshot;
			var groupTextEdits = _undoGroupTextEdits ?? new List<TextEdit>();
			_undoGroupSnapshot = null;
			_undoGroupTextEdits = null;
			if (groupStart is null || _undoLimit == 0)
			{
				return;
			}

			if (string.Equals(_plainText, groupStart.Text, StringComparison.Ordinal)
				&& RunsEqual(_runs, groupStart.Runs)
				&& ParagraphRunsEqual(_paragraphRuns, groupStart.ParagraphRuns)
				&& string.Equals(_mathML, groupStart.MathML, StringComparison.Ordinal))
			{
				// The group made no net change; nothing to record.
				return;
			}

			_undoStack.Add(new HistoryEntry(groupStart, InvertTextEdits(groupTextEdits)));
			TrimUndoStack();
			_redoStack.Clear();
		}

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
