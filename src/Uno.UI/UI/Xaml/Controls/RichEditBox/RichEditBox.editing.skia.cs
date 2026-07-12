#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	// Interactive keyboard editing for RichEditBox on Skia.
	//
	// RichEditBox drives its OWN caret/selection state and a small key dispatcher that reuses the
	// shared, control-agnostic navigation/edit handlers in TextViewEditor (the same ones TextBox uses).
	// Text mutations are applied to the functional Text Object Model via RichEditTextDocument.ReplaceRange
	// (which preserves the character-format run model and records undo), and the caret/selection are
	// rendered through the shared DisplayBlock exactly like TextBox does through ITextBoxViewHost.
	partial class RichEditBox : ITextViewEditorHost
	{
		private static readonly VirtualKeyModifiers _platformCtrlKey = DeviceTargetHelper.PlatformCommandModifier;

		private TextViewEditor? _editorField;
		private TextViewEditor Editor => _editorField ??= new TextViewEditor(this);

		// Source of truth for the interactive caret/selection, in the same shape as TextBox._selection.
		private (int start, int length, bool selectionEndsAtTheStart) _selection;
		private float _caretXOffset;
		private bool _caretBlinkVisible;
		private bool _caretTimerHooked;
		private readonly DispatcherTimer _caretTimer = new() { Interval = TimeSpan.FromSeconds(0.5) };
		private CompositionBrush? _cachedCaretBrush;
		private Color _cachedCaretColor;
		private char? _pendingHighSurrogate;
		private bool _isProcessingSelectionChanging;
		private int _selectionSyncDeferralDepth;

		#region ITextViewEditorHost

		TextBoxView ITextViewEditorHost.TextBoxView => _textBoxView!;

		string ITextViewEditorHost.Text => GetPlainTextContent();

		// Reflects an active pointer drag so the shared keyboard handlers correctly bail out mid-drag.
		bool ITextViewEditorHost.HasPointerCapture => _hasPointerCapture;

		float ITextViewEditorHost.CaretXOffset => _caretXOffset;

		// RichEditBox uses the document's snapshot-based undo, so it does not track typing runs.
		void ITextViewEditorHost.TrySetCurrentlyTyping(bool value) { }

		// Undo is recorded by RichEditTextDocument.ReplaceRange, so these are intentionally no-ops.
		void ITextViewEditorHost.CommitReplace(string oldText, string newText, int caret) { }

		void ITextViewEditorHost.CommitDelete(string oldText, string newText, int selectionStart, int selectionLength) { }

		#endregion

		#region Caret lifecycle

		internal void StartCaret()
		{
			if (_textBoxView is null)
			{
				return;
			}

			// Honor any programmatic selection set before focus; clamp it to the current content.
			var length = GetPlainTextContent().Length;
			var selStart = Math.Clamp(Document.Selection.StartPosition, 0, length);
			var selEnd = Math.Clamp(Document.Selection.EndPosition, 0, length);
			_selection = (selStart, selEnd - selStart, false);
			Document.SetSelectionRangeInternal(selStart, selEnd);
			_caretXOffset = (float)_textBoxView.DisplayBlock.ParsedText.GetRectForIndex(selEnd).Left;

			_caretBlinkVisible = true;
			EnsureCaretTimerHooked();
			_caretTimer.Start();
			UpdateDisplaySelection();
		}

		internal void StopCaret()
		{
			_caretTimer.Stop();
			_caretBlinkVisible = false;
			_pendingHighSurrogate = null;
			UpdateDisplaySelection();
		}

		internal void ResumeCaret()
		{
			if (_textBoxView is not { } view)
			{
				return;
			}

			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(_selection.start + _selection.length, start, textLength);
			var isBackward = _selection.selectionEndsAtTheStart;
			var caret = isBackward ? start : end;

			_selection = (start, end - start, isBackward);
			Document.SetSelectionRangeInternal(start, end);
			_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			_caretBlinkVisible = true;
			EnsureCaretTimerHooked();
			_caretTimer.Start();
			UpdateDisplaySelection();
		}

		private void EnsureCaretTimerHooked()
		{
			if (!_caretTimerHooked)
			{
				_caretTimer.Tick += OnCaretTimerTick;
				_caretTimerHooked = true;
			}
		}

		private void OnCaretTimerTick(object? sender, object e)
		{
			if (IsLoaded && FocusState != FocusState.Unfocused)
			{
				_caretBlinkVisible = !_caretBlinkVisible;
				UpdateDisplaySelection();
			}
		}

		#endregion

		#region Key handling

		private protected override void OnPostKeyDown(KeyRoutedEventArgs args)
		{
			base.OnPostKeyDown(args);
			OnPostKeyDownSkia(args);
		}

		private void OnPostKeyDownSkia(KeyRoutedEventArgs args)
		{
			if (_textBoxView is null || FocusState == FocusState.Unfocused)
			{
				return;
			}

			if (_pendingHighSurrogate is not null
				&& (args.UnicodeKey is not { } nextUnicode || !char.IsLowSurrogate(nextUnicode)))
			{
				_pendingHighSurrogate = null;
			}

			// Move to the possibly-negative selection-length format used by the shared handlers.
			var (selectionStart, selectionLength) = _selection.selectionEndsAtTheStart
				? (_selection.start + _selection.length, -_selection.length)
				: (_selection.start, _selection.length);

			var text = GetPlainTextContent();
			var oldText = text;
			var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
			var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);
			var rtl = _textBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft;

			// Text commands: always return from this switch, never break.
			switch (args.Key)
			{
				case VirtualKey.Z when ctrl:
					args.Handled = true;
					DocumentUndoInteractive();
					return;
				case VirtualKey.Y when ctrl:
					args.Handled = true;
					DocumentRedoInteractive();
					return;
				case VirtualKey.X when ctrl:
					args.Handled = true;
					CutSelectionToClipboard();
					return;
				case VirtualKey.C when ctrl:
				case VirtualKey.Insert when ctrl:
					args.Handled = true;
					CopySelectionToClipboard();
					return;
				case VirtualKey.V when ctrl:
				case VirtualKey.Insert when shift:
					args.Handled = true;
					PasteFromClipboard();
					return;
				case VirtualKey.B when ctrl:
					if (TryToggleFormattingAccelerator(DisabledFormattingAccelerators.Bold))
					{
						args.Handled = true;
					}
					return;
				case VirtualKey.I when ctrl:
					if (TryToggleFormattingAccelerator(DisabledFormattingAccelerators.Italic))
					{
						args.Handled = true;
					}
					return;
				case VirtualKey.U when ctrl:
					if (TryToggleFormattingAccelerator(DisabledFormattingAccelerators.Underline))
					{
						args.Handled = true;
					}
					return;
				case VirtualKey.Escape:
					return;
				case VirtualKey.LeftShift:
				case VirtualKey.RightShift:
				case VirtualKey.Shift:
				case VirtualKey.Control:
				case VirtualKey.LeftControl:
				case VirtualKey.RightControl:
				case VirtualKey.Menu:
				case VirtualKey.LeftMenu:
				case VirtualKey.RightMenu:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					return;
			}

			switch (args.Key)
			{
				case VirtualKey.Up:
					if (ctrl && DeviceTargetHelper.UsesAppleKeyboardLayout)
					{
						Editor.KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					}
					else
					{
						Editor.KeyDownUpArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					}
					break;
				case VirtualKey.Down:
					if (ctrl && DeviceTargetHelper.UsesAppleKeyboardLayout)
					{
						Editor.KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					}
					else
					{
						Editor.KeyDownDownArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					}
					break;
				case VirtualKey.Left when !rtl:
				case VirtualKey.Right when rtl:
					Editor.KeyDownLeftArrow(args, text, shift, ctrl, ref selectionStart, ref selectionLength);
					SnapActiveSelectionToTextElementStart(text, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Left when rtl:
				case VirtualKey.Right when !rtl:
					Editor.KeyDownRightArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					SnapActiveSelectionToTextElementEnd(text, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Home:
					Editor.KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.End:
					Editor.KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Back when !IsReadOnly:
					if (!_hasPointerCapture && selectionLength == 0 && selectionStart > 0 && !IsWordDelete(args, ctrl))
					{
						var previous = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementStart(text, selectionStart - 1);
						var current = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementEnd(text, selectionStart);
						selectionLength = current - previous;
						selectionStart = previous;
					}
					Editor.KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Delete when !IsReadOnly:
					if (!_hasPointerCapture && selectionLength == 0 && selectionStart < text.Length && !shift && !IsWordDelete(args, ctrl))
					{
						var current = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementStart(text, selectionStart);
						var next = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementEnd(text, selectionStart + 1);
						selectionStart = current;
						selectionLength = next - current;
					}
					Editor.KeyDownDelete(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.A when ctrl:
					args.Handled = true;
					selectionStart = 0;
					selectionLength = text.Length;
					break;
				default:
					// During an active IME composition, the platform drives text through the composition
					// callbacks; swallow the redundant char-insertion key so it isn't typed twice.
					if (ShouldSwallowKeyDuringComposition)
					{
						return;
					}

					var isEnterKey = args.UnicodeKey is '\r' or '\n' || args.Key == VirtualKey.Enter;
					var altHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
					var ctrlHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control);
					var isAltGr = !DeviceTargetHelper.UsesAppleKeyboardLayout && ctrlHeld && altHeld;
					var hasShortcutModifier = !isAltGr && (
						ctrlHeld ||
						args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Windows) ||
						(!DeviceTargetHelper.UsesAppleKeyboardLayout && altHeld));
					if (!IsReadOnly && !hasShortcutModifier && args.UnicodeKey is { } key && (!isEnterKey || AcceptsReturn))
					{
						var start = Math.Min(selectionStart, selectionStart + selectionLength);
						var end = Math.Max(selectionStart, selectionStart + selectionLength);
						string input;

						if (char.IsHighSurrogate(key))
						{
							_pendingHighSurrogate = key;
							args.Handled = true;
							return;
						}
						else if (char.IsLowSurrogate(key))
						{
							if (_pendingHighSurrogate is not { } highSurrogate)
							{
								args.Handled = true;
								return;
							}

							input = string.Concat(highSurrogate, key);
							_pendingHighSurrogate = null;
						}
						else if (key is '\n')
						{
							// RichEditBox is multiline and normalizes newlines to \r like WinUI.
							input = "\r";
						}
						else
						{
							input = key.ToString();
						}

						args.Handled = true;

						// Route the typed character through CharacterCasing, then clamp against MaxLength
						// (accounting for the selection being replaced). Replacing a non-empty selection
						// frees room, so only a caret already at MaxLength is blocked.
						var insert = ClampInsertToMaxLength(CoerceCasing(input), text.Length, start, end);
						if (insert.Length == 0 && start == end)
						{
							break;
						}

						text = text[..start] + insert + text[end..];
						selectionStart = start + insert.Length;
						selectionLength = 0;
						break;
					}
					else
					{
						return;
					}
			}

			selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart));
			selectionLength = Math.Max(-selectionStart, Math.Min(text.Length - selectionStart, selectionLength));

			var caretXOffset = _caretXOffset;

			if (!string.Equals(text, oldText, StringComparison.Ordinal))
			{
				ApplyTextDiff(oldText, text);
			}

			SetInteractiveSelection(selectionStart, selectionLength);

			// Preserve the sticky horizontal caret offset when moving up/down.
			if (args.Key is VirtualKey.Up or VirtualKey.Down)
			{
				_caretXOffset = caretXOffset;
			}
		}

		private static bool IsWordDelete(KeyRoutedEventArgs args, bool ctrl)
			=> DeviceTargetHelper.UsesAppleKeyboardLayout
				? args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu)
				: ctrl;

		private static void SnapActiveSelectionToTextElementStart(string text, bool extend, ref int selectionStart, ref int selectionLength)
		{
			var activeEnd = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementStart(text, selectionStart + selectionLength);
			if (extend)
			{
				selectionLength = activeEnd - selectionStart;
			}
			else
			{
				selectionStart = activeEnd;
				selectionLength = 0;
			}
		}

		private static void SnapActiveSelectionToTextElementEnd(string text, bool extend, ref int selectionStart, ref int selectionLength)
		{
			var activeEnd = global::Microsoft.UI.Text.TextUnitNavigation.GetTextElementEnd(text, selectionStart + selectionLength);
			if (extend)
			{
				selectionLength = activeEnd - selectionStart;
			}
			else
			{
				selectionStart = activeEnd;
				selectionLength = 0;
			}
		}

		#endregion

		#region Edit application & selection

		/// <summary>
		/// Applies the control's <see cref="CharacterCasing"/> to newly entered text (typed or pasted).
		/// Only the incoming text is coerced — existing content is never re-cased — matching WinUI.
		/// </summary>
		internal string CoerceCasing(string value)
		{
			if (value.Length == 0)
			{
				return value;
			}

			return CharacterCasing switch
			{
				CharacterCasing.Upper => value.ToUpper(global::System.Globalization.CultureInfo.CurrentCulture),
				CharacterCasing.Lower => value.ToLower(global::System.Globalization.CultureInfo.CurrentCulture),
				_ => value,
			};
		}

		/// <summary>
		/// Clamps <paramref name="insert"/> so replacing the [<paramref name="start"/>,<paramref name="end"/>)
		/// span in a document of <paramref name="currentLength"/> characters keeps the total within
		/// <see cref="MaxLength"/>. A non-positive MaxLength means unlimited.
		/// </summary>
		internal string ClampInsertToMaxLength(string insert, int currentLength, int start, int end)
		{
			var maxLength = MaxLength;
			if (maxLength <= 0)
			{
				return insert;
			}

			var room = maxLength - (currentLength - (end - start));
			if (room <= 0)
			{
				return string.Empty;
			}

			return insert.Length <= room
				? insert
				: global::Microsoft.UI.Text.TextUnitNavigation.TruncateToUtf16Boundary(insert, room);
		}

		/// <summary>
		/// Applies the single contiguous change between <paramref name="oldText"/> and
		/// <paramref name="newText"/> through the document's ReplaceRange so the character-format run
		/// model outside the edit is preserved and the change is recorded on the undo history.
		/// </summary>
		private void ApplyTextDiff(string oldText, string newText)
		{
			var max = Math.Min(oldText.Length, newText.Length);

			var prefix = 0;
			while (prefix < max && oldText[prefix] == newText[prefix])
			{
				prefix++;
			}

			var suffix = 0;
			while (suffix < max - prefix && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
			{
				suffix++;
			}

			var start = prefix;
			var oldEnd = oldText.Length - suffix;
			var insert = newText.Substring(prefix, newText.Length - suffix - prefix);

			RunWithDeferredSelectionSync(() => Document.ReplaceRange(start, oldEnd, insert));
		}

		private void RunWithDeferredSelectionSync(Action action)
		{
			var originalSelection = _selection;
			_selectionSyncDeferralDepth++;
			try
			{
				action();
			}
			catch
			{
				RestoreSelectionSilently(originalSelection);
				throw;
			}
			finally
			{
				_selectionSyncDeferralDepth--;
			}
		}

		private void SetInteractiveSelection(int selectionStart, int selectionLength)
		{
			var caret = selectionStart + selectionLength;
			var start = Math.Min(selectionStart, caret);
			var length = Math.Abs(selectionLength);
			ProcessSelectionChange(start, start + length, selectionLength < 0, proposalAlreadyInTom: false, raiseForSameRange: false);
		}

		/// <summary>
		/// Clamps and re-renders the interactive selection after a document text change (e.g. a
		/// programmatic SetText). Keeps the exposed TOM selection and the interactive state coherent
		/// regardless of focus.
		/// </summary>
		internal void OnDocumentTextChangedInteractive()
		{
			if (_isProcessingSelectionChanging
				|| _selectionSyncDeferralDepth > 0
				|| Document.IsSelectionMutationInProgress)
			{
				return;
			}

			var length = GetPlainTextContent().Length;
			var start = Math.Clamp(Document.Selection.StartPosition, 0, length);
			var end = Math.Clamp(Document.Selection.EndPosition, start, length);
			ProcessSelectionChange(start, end, _selection.selectionEndsAtTheStart && start != end, proposalAlreadyInTom: true, raiseForSameRange: false);
		}

		/// <summary>
		/// Syncs the interactive caret/selection from the Text Object Model when the programmatic
		/// <see cref="RichEditTextDocument.Selection"/> is changed through its public API (the reverse of
		/// the control pushing into the TOM). Does not push back into the TOM.
		/// </summary>
		internal void OnTomSelectionChanged()
		{
			if (_isProcessingSelectionChanging)
			{
				return;
			}

			var length = GetPlainTextContent().Length;
			var start = Math.Clamp(Document.Selection.StartPosition, 0, length);
			var end = Math.Clamp(Document.Selection.EndPosition, start, length);
			ProcessSelectionChange(start, end, selectionEndsAtTheStart: false, proposalAlreadyInTom: true, raiseForSameRange: true);
		}

		private void ProcessSelectionChange(int proposedStart, int proposedEnd, bool selectionEndsAtTheStart, bool proposalAlreadyInTom, bool raiseForSameRange)
		{
			var originalSelection = _selection;
			var textLength = GetPlainTextContent().Length;
			proposedStart = Math.Clamp(proposedStart, 0, textLength);
			proposedEnd = Math.Clamp(proposedEnd, proposedStart, textLength);
			var selectionChanged = proposedStart != originalSelection.start
				|| proposedEnd != originalSelection.start + originalSelection.length;

			if (!proposalAlreadyInTom)
			{
				Document.SetSelectionRangeInternal(proposedStart, proposedEnd, clearPendingCaretFormat: false);
			}

			if (selectionChanged || raiseForSameRange)
			{
				bool cancelled;
				var selectionChangeVersion = Document.SelectionChangeVersion;
				try
				{
					try
					{
						_isProcessingSelectionChanging = true;
						cancelled = RaiseSelectionChangingIsCancelled(proposedStart, proposedEnd - proposedStart);
					}
					finally
					{
						_isProcessingSelectionChanging = false;
					}
				}
				catch
				{
					RestoreSelectionSilently(originalSelection);
					throw;
				}

				textLength = GetPlainTextContent().Length;
				var handlerStart = Math.Clamp(Document.Selection.StartPosition, 0, textLength);
				var handlerEnd = Math.Clamp(Document.Selection.EndPosition, handlerStart, textLength);
				var selectionChangedByHandler = Document.SelectionChangeVersion != selectionChangeVersion;
				if (cancelled && !selectionChangedByHandler)
				{
					RestoreSelectionSilently(originalSelection);
					return;
				}

				proposedStart = handlerStart;
				proposedEnd = handlerEnd;
				selectionEndsAtTheStart = selectionChangedByHandler
					? false
					: selectionEndsAtTheStart && proposedStart != proposedEnd;
			}

			Document.SetSelectionRangeInternal(proposedStart, proposedEnd, clearPendingCaretFormat: false);
			Document.ClearPendingCaretFormatIfMoved(proposedStart, proposedEnd);
			CommitInteractiveSelection(proposedStart, proposedEnd, selectionEndsAtTheStart);
		}

		private void RestoreSelectionSilently((int start, int length, bool selectionEndsAtTheStart) selection)
		{
			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(selection.start, 0, textLength);
			var end = Math.Clamp(selection.start + selection.length, start, textLength);
			Document.SetSelectionRangeInternal(start, end, clearPendingCaretFormat: false);
			CommitInteractiveSelection(start, end, selection.selectionEndsAtTheStart && start != end, raiseSelectionChanged: false);
		}

		private void CommitInteractiveSelection(int start, int end, bool selectionEndsAtTheStart, bool raiseSelectionChanged = true)
		{
			_selection = (start, end - start, selectionEndsAtTheStart);
			if (!raiseSelectionChanged)
			{
				_lastRaisedSelection = (start, end - start);
			}

			var caret = selectionEndsAtTheStart ? start : end;
			if (_textBoxView is { } view)
			{
				_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			}

			_caretBlinkVisible = true;
			if (FocusState != FocusState.Unfocused && !IsReadOnly)
			{
				EnsureCaretTimerHooked();
				_caretTimer.Start();
			}

			UpdateDisplaySelection();
		}

		private void DocumentUndoInteractive()
		{
			if (!IsReadOnly && Document.CanUndo())
			{
				Document.Undo();
			}
		}

		private void DocumentRedoInteractive()
		{
			if (!IsReadOnly && Document.CanRedo())
			{
				Document.Redo();
			}
		}

		#endregion

		#region Rendering

		private void UpdateDisplaySelection()
		{
			// Raise SelectionChanged from this universal selection choke point, before the layout
			// guard, so caret/selection changes notify even if the view is not laid out yet. The
			// de-dupe against the last-raised span keeps focus-only re-renders from firing spuriously.
			RaiseSelectionChangedIfNeeded();

			IsCaretRenderedForTesting = false;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			// During BatchDisplayUpdates the logical document/selection may advance while the DisplayBlock
			// intentionally still contains the previous text. Clamp only the rendered range until the batch
			// is applied; the TOM selection remains unchanged.
			var displayedLength = displayBlock.Text?.Length ?? 0;
			var renderedStart = Math.Clamp(_selection.start, 0, displayedLength);
			var renderedEnd = Math.Clamp(_selection.start + _selection.length, renderedStart, displayedLength);
			displayBlock.Selection = new TextBlock.Range(renderedStart, renderedEnd);

			var focused = FocusState != FocusState.Unfocused;
			displayBlock.RenderSelection = focused || _selection.length > 0;

			if (focused && _selection.length == 0 && !IsReadOnly && _caretBlinkVisible)
			{
				displayBlock.RenderCaret = (renderedStart, GetOpaqueCaretBrush());
				IsCaretRenderedForTesting = true;
			}
			else
			{
				displayBlock.RenderCaret = null;
			}

			((IBlock)displayBlock).Invalidate(false);

			var visual = displayBlock.Visual;
			visual.Compositor.InvalidateRender(visual);
		}

		/// <summary>
		/// Gets a fully opaque composition brush derived from the control's Foreground for caret rendering,
		/// mirroring the approach used by TextBox (Uno does not support WinUI's DestInvert caret compositing).
		/// </summary>
		private CompositionBrush GetOpaqueCaretBrush()
		{
			var compositor = Compositor.GetSharedCompositor();
			if (Foreground is SolidColorBrush scb)
			{
				var color = scb.Color;
				if (color.A < 255)
				{
					color = Color.FromArgb(255, color.R, color.G, color.B);
				}

				if (_cachedCaretBrush is not null && _cachedCaretColor == color)
				{
					return _cachedCaretBrush;
				}

				_cachedCaretColor = color;
				_cachedCaretBrush = compositor.CreateColorBrush(color);
				return _cachedCaretBrush;
			}

			_cachedCaretBrush = null;
			_cachedCaretColor = default;
			return DefaultBrushes.TextForegroundBrush.GetOrCreateCompositionBrush(compositor);
		}

		#endregion

		#region Test hooks

		internal int SelectionStartForTesting => _selection.start;

		internal int SelectionLengthForTesting => _selection.length;

		internal bool IsSelectionBackwardForTesting => _selection.selectionEndsAtTheStart;

		internal bool IsCaretRenderedForTesting { get; private set; }

		#endregion
	}
}
