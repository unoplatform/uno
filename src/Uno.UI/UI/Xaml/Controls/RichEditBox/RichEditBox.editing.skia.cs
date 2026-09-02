#nullable enable

using System;
using System.Threading.Tasks;
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
using Uno.Foundation.Logging;

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
		private int _pendingInteractiveLineFeedPosition = -1;
		private long _pendingInteractiveLineFeedTextVersion = -1;
		private long _pendingInteractiveLineFeedSelectionVersion = -1;
		private long _pendingInteractiveLineFeedInputStateVersion = -1;
		private long _interactiveInputStateVersion;
		private bool _textChangingInvalidatedLineFeed;
		private bool _isProcessingSelectionChanging;
		private int _selectionSyncDeferralDepth;
		private RichEditCaretDisplayMode _caretMode = RichEditCaretDisplayMode.ThumblessCaretHidden;

		internal RichEditCaretDisplayMode CaretMode
		{
			get => _caretMode;
			private set
			{
				if (_caretMode == value)
				{
					return;
				}

				_caretMode = value;
				_caretBlinkVisible = value == RichEditCaretDisplayMode.ThumblessCaretShowing;
				if (value == RichEditCaretDisplayMode.ThumblessCaretShowing)
				{
					EnsureCaretTimerHooked();
					_caretTimer.Start();
				}
				else
				{
					_caretTimer.Stop();
				}

				UpdateDisplaySelection();
				_gripperPresenter?.Update();
			}
		}

		#region ITextViewEditorHost

		TextBoxView ITextViewEditorHost.TextBoxView => _textBoxView!;

		string ITextViewEditorHost.Text => GetPlainTextContent();

		// Reflects an active pointer drag so the shared keyboard handlers correctly bail out mid-drag.
		bool ITextViewEditorHost.HasPointerCapture => _hasPointerCapture;

		float ITextViewEditorHost.CaretXOffset => _caretXOffset;

		bool ITextViewEditorHost.TryGetUpDownResult(
			int selectionStart,
			int selectionLength,
			bool shift,
			bool ctrl,
			bool up,
			out int result)
			=> TryGetInteractiveUpDownResult(selectionStart, selectionLength, shift, ctrl, up, out result);

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
			var length = GetPlainTextLength();
			var selStart = Math.Clamp(Document.Selection.StartPosition, 0, length);
			var selEnd = Math.Clamp(Document.Selection.EndPosition, 0, length);
			var selectionEndsAtTheStart = selStart != selEnd
				&& Document.Selection.Options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);
			_selection = (selStart, selEnd - selStart, selectionEndsAtTheStart);
			Document.SetSelectionRangeInternal(selStart, selEnd, selectionEndsAtTheStart: selectionEndsAtTheStart);
			var caret = selectionEndsAtTheStart ? selStart : selEnd;
			_caretXOffset = (float)_textBoxView.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;

			// Touch focus can be delivered after PointerReleased has already selected a word and
			// requested thumbs. TextBox performs its TouchTap after its synchronous focus update;
			// preserve the equivalent final state when RichEditBox's GotFocus arrives later.
			if (CaretMode is not RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing
				and not RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing)
			{
				CaretMode = RichEditCaretDisplayMode.ThumblessCaretShowing;
			}

			DispatchUpdateScrolling();
		}

		internal void StopCaret()
		{
			_pendingHighSurrogate = null;
			InvalidatePendingInteractiveLineFeed();
			CaretMode = RichEditCaretDisplayMode.ThumblessCaretHidden;
		}

		private void InvalidatePendingInteractiveLineFeed()
		{
			_pendingInteractiveLineFeedPosition = -1;
			_pendingInteractiveLineFeedTextVersion = -1;
			_pendingInteractiveLineFeedSelectionVersion = -1;
			_pendingInteractiveLineFeedInputStateVersion = -1;
			_interactiveInputStateVersion++;
		}

		internal void ResumeCaret()
		{
			if (_textBoxView is not { } view)
			{
				return;
			}

			var textLength = GetPlainTextLength();
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(_selection.start + _selection.length, start, textLength);
			var isBackward = _selection.selectionEndsAtTheStart && start != end;
			var caret = isBackward ? start : end;

			_selection = (start, end - start, isBackward);
			Document.SetSelectionRangeInternal(start, end, selectionEndsAtTheStart: isBackward);
			_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			CaretMode = RichEditCaretDisplayMode.ThumblessCaretShowing;
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
			if (IsLoaded
				&& FocusState != FocusState.Unfocused
				&& CaretMode == RichEditCaretDisplayMode.ThumblessCaretShowing)
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
				InvalidatePendingInteractiveLineFeed();
				return;
			}

			var pendingLineFeedPosition = _pendingInteractiveLineFeedPosition;
			var pendingLineFeedTextVersion = _pendingInteractiveLineFeedTextVersion;
			var pendingLineFeedSelectionVersion = _pendingInteractiveLineFeedSelectionVersion;
			var pendingLineFeedInputStateVersion = _pendingInteractiveLineFeedInputStateVersion;
			var previousInputStateVersion = _interactiveInputStateVersion;
			InvalidatePendingInteractiveLineFeed();
			_textChangingInvalidatedLineFeed = false;

			if (_pendingHighSurrogate is not null
				&& (args.UnicodeKey is not { } nextUnicode || !char.IsLowSurrogate(nextUnicode)))
			{
				_pendingHighSurrogate = null;
			}

			// Move to the possibly-negative selection-length format used by the shared handlers.
			var (selectionStart, selectionLength) = _selection.selectionEndsAtTheStart
				? (_selection.start + _selection.length, -_selection.length)
				: (_selection.start, _selection.length);

			var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
			var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);

			if (args.Key == VirtualKey.Enter && TryNavigateLinkAtCaret())
			{
				args.Handled = true;
				return;
			}

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

			if (TryProcessRangeEditKey(
				args,
				ctrl,
				shift,
				ref selectionStart,
				ref selectionLength,
				pendingLineFeedPosition,
				pendingLineFeedTextVersion,
				pendingLineFeedSelectionVersion,
				pendingLineFeedInputStateVersion,
				previousInputStateVersion))
			{
				return;
			}

			var text = GetPlainTextContent();
			var oldText = text;
			var historyKind = global::Microsoft.UI.Text.TextHistoryKind.None;
			var rtl = _textBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft;

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
					historyKind = global::Microsoft.UI.Text.TextHistoryKind.Backspace;
					if (!_hasPointerCapture && selectionLength == 0 && selectionStart > 0 && !IsWordDelete(args, ctrl))
					{
						var previous = Document.GetTextElementStart(selectionStart - 1);
						var current = Document.GetTextElementEnd(selectionStart);
						selectionLength = current - previous;
						selectionStart = previous;
					}
					Editor.KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Delete when !IsReadOnly:
					historyKind = global::Microsoft.UI.Text.TextHistoryKind.Delete;
					if (!_hasPointerCapture && selectionLength == 0 && selectionStart < text.Length && !shift && !IsWordDelete(args, ctrl))
					{
						var current = Document.GetTextElementStart(selectionStart);
						var next = Document.GetTextElementEnd(selectionStart + 1);
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
						historyKind = global::Microsoft.UI.Text.TextHistoryKind.Typing;
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

			var textChanged = !string.Equals(text, oldText, StringComparison.Ordinal);
			if (textChanged)
			{
				if (!ApplyTextDiff(oldText, text, historyKind, checkTextLimit: false))
				{
					return;
				}
			}

			SetInteractiveSelection(selectionStart, selectionLength);
			if (textChanged)
			{
				Document.FinalizeHistorySelection();
			}

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

		private bool TryProcessRangeEditKey(
			KeyRoutedEventArgs args,
			bool ctrl,
			bool shift,
			ref int selectionStart,
			ref int selectionLength,
			int pendingLineFeedPosition,
			long pendingLineFeedTextVersion,
			long pendingLineFeedSelectionVersion,
			long pendingLineFeedInputStateVersion,
			long previousInputStateVersion)
		{
			var textLength = GetPlainTextLength();
			var activeEnd = selectionStart + selectionLength;
			var start = Math.Clamp(Math.Min(selectionStart, activeEnd), 0, textLength);
			var end = Math.Clamp(Math.Max(selectionStart, activeEnd), start, textLength);
			var insert = string.Empty;
			var historyKind = global::Microsoft.UI.Text.TextHistoryKind.None;
			var armLineFeedCoalescing = false;
			var inputStateVersionBeforeMutation = _interactiveInputStateVersion;
			var selectionVersionBeforeMutation = Document.SelectionChangeVersion;

			switch (args.Key)
			{
				case VirtualKey.Back:
					if (IsReadOnly || _hasPointerCapture)
					{
						return true;
					}
					historyKind = global::Microsoft.UI.Text.TextHistoryKind.Backspace;
					if (start == end)
					{
						if (start == 0)
						{
							return true;
						}

						if (IsWordDelete(args, ctrl))
						{
							start = _textBoxView!.DisplayBlock.ParsedText.GetWordAt(start, false).start;
						}
						else
						{
							var caret = start;
							start = Document.GetTextElementStart(caret - 1);
							end = Document.GetTextElementEnd(caret);
						}
					}
					break;
				case VirtualKey.Delete:
					if (IsReadOnly || _hasPointerCapture)
					{
						return true;
					}
					args.Handled = true;
					historyKind = global::Microsoft.UI.Text.TextHistoryKind.Delete;
					if (start == end)
					{
						if (start == textLength || shift)
						{
							return true;
						}

						if (IsWordDelete(args, ctrl))
						{
							var word = _textBoxView!.DisplayBlock.ParsedText.GetWordAt(start, true);
							end = word.start + word.length;
						}
						else
						{
							var caret = start;
							start = Document.GetTextElementStart(caret);
							end = Document.GetTextElementEnd(caret + 1);
						}
					}
					break;
				default:
					if (ShouldSwallowKeyDuringComposition)
					{
						return true;
					}

					if (args.UnicodeKey is not { } key)
					{
						return false;
					}

					if (key == '\n'
						&& start == end
						&& pendingLineFeedPosition == start
						&& pendingLineFeedTextVersion == Document.TextVersion
						&& pendingLineFeedSelectionVersion == Document.SelectionChangeVersion
						&& pendingLineFeedInputStateVersion == previousInputStateVersion)
					{
						args.Handled = true;
						return true;
					}

					var isEnterKey = key is '\r' or '\n' || args.Key == VirtualKey.Enter;
					var altHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
					var ctrlHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control);
					var isAltGr = !DeviceTargetHelper.UsesAppleKeyboardLayout && ctrlHeld && altHeld;
					var hasShortcutModifier = !isAltGr && (
						ctrlHeld
						|| args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Windows)
						|| !DeviceTargetHelper.UsesAppleKeyboardLayout && altHeld);
					if (IsReadOnly
						|| hasShortcutModifier
						|| isEnterKey && !AcceptsReturn)
					{
						return false;
					}

					historyKind = global::Microsoft.UI.Text.TextHistoryKind.Typing;
					armLineFeedCoalescing = key == '\r';
					if (char.IsHighSurrogate(key))
					{
						_pendingHighSurrogate = key;
						args.Handled = true;
						return true;
					}
					if (char.IsLowSurrogate(key))
					{
						if (_pendingHighSurrogate is not { } highSurrogate)
						{
							args.Handled = true;
							return true;
						}

						insert = string.Concat(highSurrogate, key);
						_pendingHighSurrogate = null;
					}
					else
					{
						insert = key is '\n' ? "\r" : key.ToString();
					}

					args.Handled = true;
					insert = ClampInsertToMaxLength(CoerceCasing(insert), textLength, start, end);
					if (insert.Length == 0 && start == end)
					{
						return true;
					}
					break;
			}

			try
			{
				var insertedLength = 0;
				RunWithDeferredSelectionSync(() => insertedLength = Document.ReplaceRange(
					start,
					end,
					insert,
					checkTextLimit: false,
					historyKind: historyKind));
				var inputStateVersionBeforeSelection = _interactiveInputStateVersion;
				var targetSelection = (start: start + insertedLength, length: 0, selectionEndsAtTheStart: false);
				var selectionWillChange = _selection != targetSelection;
				SetInteractiveSelection(targetSelection.start, targetSelection.length);
				Document.FinalizeHistorySelection();
				var selectionUpdateWasExpected = _interactiveInputStateVersion
					== inputStateVersionBeforeSelection + (selectionWillChange ? 1 : 0);
				if (armLineFeedCoalescing
					&& insertedLength == 1
					&& !_textChangingInvalidatedLineFeed
					&& inputStateVersionBeforeMutation == inputStateVersionBeforeSelection
					&& selectionUpdateWasExpected
					&& selectionVersionBeforeMutation == Document.SelectionChangeVersion
					&& _selection == targetSelection
					&& FocusState != FocusState.Unfocused
					&& !IsReadOnly
					&& AcceptsReturn)
				{
					_pendingInteractiveLineFeedPosition = start + insertedLength;
					_pendingInteractiveLineFeedTextVersion = Document.TextVersion;
					_pendingInteractiveLineFeedSelectionVersion = Document.SelectionChangeVersion;
					_pendingInteractiveLineFeedInputStateVersion = _interactiveInputStateVersion;
				}
			}
			catch (UnauthorizedAccessException)
			{
			}

			return true;
		}

		private bool TryNavigateLinkAtCaret()
		{
			var caret = _selection.selectionEndsAtTheStart ? _selection.start : _selection.start + _selection.length;
			return TryNavigateLinkAt(caret);
		}

		internal bool TryNavigateLinkAt(int position)
		{
			var range = Document.GetRange(position, position);
			var link = range.Link;
			if (!TryGetLinkUri(link, out var uri))
			{
				return false;
			}

			_ = LaunchLinkAsync(uri);
			return true;
		}

		internal Func<Uri, Task<bool>>? LinkLauncherForTesting { get; set; }

		private async Task LaunchLinkAsync(Uri uri)
		{
			try
			{
				var launched = LinkLauncherForTesting is { } testLauncher
					? await testLauncher(uri)
					: await global::Windows.System.Launcher.LaunchUriAsync(uri);
				if (!launched)
				{
					typeof(RichEditBox).LogWarn()?.Warn("No handler accepted a RichEditBox hyperlink.");
				}
			}
			catch (Exception error)
			{
				typeof(RichEditBox).LogError()?.Error("Failed to launch a RichEditBox hyperlink.", error);
			}
		}

		internal static bool TryGetLinkUri(string link, out Uri uri)
		{
			uri = null!;
			if (string.IsNullOrEmpty(link))
			{
				return false;
			}

			var start = link[0] == '\ufddf' ? 1 : 0;
			if (link.Length - start >= 2
				&& link[start] == '"'
				&& link[^1] == '"'
				&& Uri.TryCreate(link.Substring(start + 1, link.Length - start - 2), UriKind.Absolute, out var parsed)
				&& (parsed.Scheme == Uri.UriSchemeHttp
					|| parsed.Scheme == Uri.UriSchemeHttps
					|| parsed.Scheme == Uri.UriSchemeMailto))
			{
				uri = parsed;
				return true;
			}

			return false;
		}

		private void SnapActiveSelectionToTextElementStart(string text, bool extend, ref int selectionStart, ref int selectionLength)
		{
			var activeEnd = Document.GetTextElementStart(selectionStart + selectionLength);
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

		private void SnapActiveSelectionToTextElementEnd(string text, bool extend, ref int selectionStart, ref int selectionLength)
		{
			var activeEnd = Document.GetTextElementEnd(selectionStart + selectionLength);
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
		internal string ClampInsertToMaxLength(
			string insert,
			int currentLength,
			int start,
			int end,
			bool preserveSurrogatePair = true)
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
				: preserveSurrogatePair
					? global::Microsoft.UI.Text.TextUnitNavigation.TruncateToUtf16Boundary(insert, room)
					: global::Microsoft.UI.Text.TextUnitNavigation.TruncateToUtf16Limit(insert, room);
		}

		/// <summary>
		/// Applies the single contiguous change between <paramref name="oldText"/> and
		/// <paramref name="newText"/> through the document's ReplaceRange so the character-format run
		/// model outside the edit is preserved and the change is recorded on the undo history.
		/// </summary>
		private bool ApplyTextDiff(
			string oldText,
			string newText,
			global::Microsoft.UI.Text.TextHistoryKind historyKind = global::Microsoft.UI.Text.TextHistoryKind.None,
			bool checkTextLimit = true)
			=> ApplyTextDiff(
				oldText,
				newText,
				GetTextDiff(oldText, newText),
				historyKind,
				checkTextLimit,
				out _);

		private bool ApplyTextDiff(
			string oldText,
			string newText,
			TextDiff diff,
			global::Microsoft.UI.Text.TextHistoryKind historyKind,
			bool checkTextLimit,
			out bool nativeTextNeedsCorrection)
		{
			var oldEnd = diff.Start + diff.RemovedLength;
			var nativeInsert = newText.Substring(diff.Start, diff.InsertedLength);
			var insert = CoerceCasing(nativeInsert);
			if (checkTextLimit)
			{
				insert = ClampInsertToMaxLength(insert, oldText.Length, diff.Start, oldEnd);
			}
			nativeTextNeedsCorrection = !string.Equals(nativeInsert, insert, StringComparison.Ordinal);

			try
			{
				var insertedLength = 0;
				RunWithDeferredSelectionSync(() =>
				{
					insertedLength = Document.ReplaceRange(
						diff.Start,
						oldEnd,
						insert,
						checkTextLimit: false,
						historyKind: historyKind);
				});
				var actualInsert = Document.GetTextInRange(diff.Start, diff.Start + insertedLength);
				nativeTextNeedsCorrection = !string.Equals(nativeInsert, actualInsert, StringComparison.Ordinal);
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				nativeTextNeedsCorrection = false;
				return false;
			}
		}

		internal int NativeSelectionStart => _selection.start;

		internal int NativeSelectionLength => _selection.length;

		internal bool NativeSelectionIsBackward => _selection.selectionEndsAtTheStart;

		internal void UpdateTextFromNative(string text, int selectionStart, int selectionLength)
			=> TryUpdateTextFromNative(text, selectionStart, selectionLength);

		internal bool TryUpdateTextFromNative(string text, int selectionStart, int selectionLength)
		{
			var oldText = GetPlainTextContent();
			var textChanged = !string.Equals(oldText, text, StringComparison.Ordinal);
			var diff = textChanged ? GetTextDiff(oldText, text) : default;
			var nativeTextNeedsCorrection = false;
			if (IsReadOnly
				|| (_isComposing && !_platformTextApplyInProgress && !_compositionAppliedByPlatform)
				|| (textChanged && !ApplyTextDiff(
					oldText,
					text,
					diff,
					GetNativeHistoryKind(diff),
					checkTextLimit: true,
					out nativeTextNeedsCorrection)))
			{
				RestoreNativeTextAndSelection(oldText);
				return false;
			}

			if (nativeTextNeedsCorrection)
			{
				var actualText = GetPlainTextContent();
				_textBoxView?.Extension?.SetText(actualText);
				var correction = GetTextDiff(text, actualText);
				var selectionEnd = RebaseNativePosition(selectionStart + selectionLength, correction);
				selectionStart = RebaseNativePosition(selectionStart, correction);
				selectionLength = selectionEnd - selectionStart;
			}

			SetInteractiveSelection(selectionStart, selectionLength);
			if (nativeTextNeedsCorrection)
			{
				_textBoxView?.Extension?.Select(_selection.start, _selection.length);
			}
			if (textChanged)
			{
				Document.FinalizeHistorySelection();
			}

			return true;
		}

		private void RestoreNativeTextAndSelection(string text)
		{
			_textBoxView?.Extension?.SetText(text);
			_textBoxView?.Extension?.Select(_selection.start, _selection.length);
		}

		private global::Microsoft.UI.Text.TextHistoryKind GetNativeHistoryKind(TextDiff diff)
		{
			if (diff.InsertedLength > 0)
			{
				return global::Microsoft.UI.Text.TextHistoryKind.Typing;
			}

			if (diff.RemovedLength == 0 || _selection.length != 0)
			{
				return global::Microsoft.UI.Text.TextHistoryKind.None;
			}

			var caret = _selection.start;
			if (diff.Start + diff.RemovedLength == caret)
			{
				return global::Microsoft.UI.Text.TextHistoryKind.Backspace;
			}
			if (diff.Start == caret)
			{
				return global::Microsoft.UI.Text.TextHistoryKind.Delete;
			}

			return global::Microsoft.UI.Text.TextHistoryKind.None;
		}

		private static TextDiff GetTextDiff(string oldText, string newText)
		{
			var max = Math.Min(oldText.Length, newText.Length);
			var prefix = 0;
			while (prefix < max && oldText[prefix] == newText[prefix])
			{
				prefix++;
			}

			var suffix = 0;
			while (suffix < max - prefix
				&& oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
			{
				suffix++;
			}

			return new TextDiff(
				prefix,
				oldText.Length - prefix - suffix,
				newText.Length - prefix - suffix);
		}

		private static int RebaseNativePosition(int position, TextDiff diff)
		{
			var nativeEnd = diff.Start + diff.RemovedLength;
			if (position <= diff.Start)
			{
				return position;
			}
			if (position >= nativeEnd)
			{
				return position + diff.InsertedLength - diff.RemovedLength;
			}

			return diff.Start + Math.Min(position - diff.Start, diff.InsertedLength);
		}

		private readonly record struct TextDiff(int Start, int RemovedLength, int InsertedLength);

		internal void SelectFromNative(int selectionStart, int selectionLength)
		{
			var nativeSelectionStart = _selection.selectionEndsAtTheStart
				? _selection.start + _selection.length
				: _selection.start;
			var nativeSelectionLength = _selection.selectionEndsAtTheStart
				? -_selection.length
				: _selection.length;
			if (selectionStart == nativeSelectionStart && selectionLength == nativeSelectionLength)
			{
				return;
			}
			if (_selection.selectionEndsAtTheStart
				&& selectionStart == _selection.start
				&& selectionLength == _selection.length)
			{
				// Native selection APIs report normalized bounds and cannot represent the active endpoint.
				// Ignore an echo of the current range rather than silently reversing its direction.
				return;
			}

			SetInteractiveSelection(selectionStart, selectionLength);
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
			Document.BreakHistoryCoalescing();
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

			var length = GetPlainTextLength();
			var tomStart = Document.Selection.StartPosition;
			var tomEnd = Document.Selection.EndPosition;
			var start = Math.Clamp(tomStart, 0, length);
			var end = Math.Clamp(tomEnd, start, length);
			ProcessSelectionChange(start, end, _selection.selectionEndsAtTheStart && start != end, proposalAlreadyInTom: true, raiseForSameRange: false);
			RestoreVirtualFinalEopSelection(tomStart, tomEnd, start, end, length);
		}

		/// <summary>
		/// Syncs the interactive caret/selection from the Text Object Model when the programmatic
		/// <see cref="RichEditTextDocument.Selection"/> is changed through its public API (the reverse of
		/// the control pushing into the TOM). Does not push back into the TOM.
		/// </summary>
		internal void OnTomSelectionChanged()
		{
			InvalidatePendingInteractiveLineFeed();
			if (_isProcessingSelectionChanging)
			{
				return;
			}

			var length = GetPlainTextLength();
			var tomStart = Document.Selection.StartPosition;
			var tomEnd = Document.Selection.EndPosition;
			var start = Math.Clamp(tomStart, 0, length);
			var end = Math.Clamp(tomEnd, start, length);
			var selectionEndsAtTheStart = start != end
				&& Document.Selection.Options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);
			ProcessSelectionChange(start, end, selectionEndsAtTheStart, proposalAlreadyInTom: true, raiseForSameRange: true);
			RestoreVirtualFinalEopSelection(tomStart, tomEnd, start, end, length);
		}

		internal void OnTomSelectionDirectionChanged()
		{
			InvalidatePendingInteractiveLineFeed();
			if (_isProcessingSelectionChanging)
			{
				return;
			}

			var length = GetPlainTextLength();
			var start = Math.Clamp(Document.Selection.StartPosition, 0, length);
			var end = Math.Clamp(Document.Selection.EndPosition, start, length);
			var selectionEndsAtTheStart = start != end
				&& Document.Selection.Options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);
			CommitInteractiveSelection(start, end, selectionEndsAtTheStart, raiseSelectionChanged: false);
		}

		private void RestoreVirtualFinalEopSelection(int tomStart, int tomEnd, int interactiveStart, int interactiveEnd, int textLength)
		{
			if (textLength > 0
				&& tomEnd == textLength + 1
				&& _selection.start == interactiveStart
				&& _selection.start + _selection.length == interactiveEnd)
			{
				Document.SetSelectionRangeInternal(
					tomStart,
					tomEnd,
					clearPendingCaretFormat: false,
					selectionEndsAtTheStart: false);
			}
		}

		private void ProcessSelectionChange(int proposedStart, int proposedEnd, bool selectionEndsAtTheStart, bool proposalAlreadyInTom, bool raiseForSameRange)
		{
			var originalSelection = _selection;
			var textLength = GetPlainTextLength();
			proposedStart = Math.Clamp(proposedStart, 0, textLength);
			proposedEnd = Math.Clamp(proposedEnd, proposedStart, textLength);
			var selectionChanged = proposedStart != originalSelection.start
				|| proposedEnd != originalSelection.start + originalSelection.length;

			if (!proposalAlreadyInTom)
			{
				Document.SetSelectionRangeInternal(
					proposedStart,
					proposedEnd,
					clearPendingCaretFormat: false,
					selectionEndsAtTheStart: selectionEndsAtTheStart);
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

				textLength = GetPlainTextLength();
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
					? proposedStart != proposedEnd
						&& Document.Selection.Options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive)
					: selectionEndsAtTheStart && proposedStart != proposedEnd;
			}

			Document.SetSelectionRangeInternal(
				proposedStart,
				proposedEnd,
				clearPendingCaretFormat: false,
				selectionEndsAtTheStart: selectionEndsAtTheStart);
			Document.ClearPendingCaretFormatIfMoved(proposedStart, proposedEnd);
			CommitInteractiveSelection(proposedStart, proposedEnd, selectionEndsAtTheStart);
		}

		private void RestoreSelectionSilently((int start, int length, bool selectionEndsAtTheStart) selection)
		{
			var textLength = GetPlainTextLength();
			var start = Math.Clamp(selection.start, 0, textLength);
			var end = Math.Clamp(selection.start + selection.length, start, textLength);
			Document.SetSelectionRangeInternal(
				start,
				end,
				clearPendingCaretFormat: false,
				selectionEndsAtTheStart: selection.selectionEndsAtTheStart && start != end);
			CommitInteractiveSelection(start, end, selection.selectionEndsAtTheStart && start != end, raiseSelectionChanged: false);
		}

		private void CommitInteractiveSelection(int start, int end, bool selectionEndsAtTheStart, bool raiseSelectionChanged = true)
		{
			var selection = (start, end - start, selectionEndsAtTheStart);
			var selectionChanged = selection != _selection;
			_selection = selection;
			if (selectionChanged)
			{
				InvalidatePendingInteractiveLineFeed();
			}
			if (!raiseSelectionChanged)
			{
				_lastRaisedSelection = (start, end - start);
			}

			var caret = selectionEndsAtTheStart ? start : end;
			if (_textBoxView is { } view)
			{
				_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			}

			if (end == start && CaretMode == RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing)
			{
				CaretMode = RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}
			else if (CaretMode == RichEditCaretDisplayMode.ThumblessCaretHidden && FocusState != FocusState.Unfocused)
			{
				CaretMode = RichEditCaretDisplayMode.ThumblessCaretShowing;
			}
			else if (CaretMode == RichEditCaretDisplayMode.ThumblessCaretShowing)
			{
				_caretBlinkVisible = true;
				EnsureCaretTimerHooked();
				_caretTimer.Start();
			}

			UpdateDisplaySelection();
			_textBoxView?.Select(start, end - start);
			if (selectionChanged)
			{
				UpdateScrolling();
			}
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

			var focused = FocusState != FocusState.Unfocused || _forceFocusedVisualState;
			displayBlock.RenderSelection = focused || _selection.length > 0;

			if (focused
				&& CaretMode == RichEditCaretDisplayMode.ThumblessCaretShowing
				&& Document.CaretType != global::Microsoft.UI.Text.CaretType.Null
				&& _selection.length == 0
				&& !IsReadOnly
				&& _caretBlinkVisible)
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

		internal enum RichEditCaretDisplayMode
		{
			ThumblessCaretHidden,
			ThumblessCaretShowing,
			CaretWithThumbsOnlyEndShowing,
			CaretWithThumbsBothEndsShowing,
		}

		#endregion
	}
}
