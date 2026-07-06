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
	//
	// TODO Uno: IME composition is a subsequent increment. Undo grouping (typing runs) is handled
	// coarsely for now: each edit records one snapshot on the document's history.
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
					break;
				case VirtualKey.Left when rtl:
				case VirtualKey.Right when !rtl:
					Editor.KeyDownRightArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Home:
					Editor.KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.End:
					Editor.KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Back when !IsReadOnly:
					Editor.KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.Delete when !IsReadOnly:
					Editor.KeyDownDelete(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
					break;
				case VirtualKey.A when ctrl:
					args.Handled = true;
					selectionStart = 0;
					selectionLength = text.Length;
					break;
				default:
					var isEnterKey = args.UnicodeKey is '\r' or '\n' || args.Key == VirtualKey.Enter;
					var altHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
					var ctrlHeld = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control);
					var isAltGr = !DeviceTargetHelper.UsesAppleKeyboardLayout && ctrlHeld && altHeld;
					var hasShortcutModifier = !isAltGr && (
						ctrlHeld ||
						args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Windows) ||
						(!DeviceTargetHelper.UsesAppleKeyboardLayout && altHeld));
					if (!IsReadOnly && !hasShortcutModifier && args.UnicodeKey is { } key)
					{
						var start = Math.Min(selectionStart, selectionStart + selectionLength);
						var end = Math.Max(selectionStart, selectionStart + selectionLength);

						if (key is '\n')
						{
							// RichEditBox is multiline and normalizes newlines to \r like WinUI.
							key = '\r';
						}

						args.Handled = true;
						text = text[..start] + key + text[end..];
						selectionStart = start + 1;
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

		#endregion

		#region Edit application & selection

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

			Document.ReplaceRange(start, oldEnd, insert);
		}

		private void SetInteractiveSelection(int selectionStart, int selectionLength)
		{
			var caret = selectionStart + selectionLength;
			var start = Math.Min(selectionStart, caret);
			var length = Math.Abs(selectionLength);
			_selection = (start, length, selectionLength < 0);

			if (_textBoxView is { } view)
			{
				_caretXOffset = (float)view.DisplayBlock.ParsedText.GetRectForIndex(caret).Left;
			}

			// Mirror the interactive caret/selection into the Text Object Model for programmatic reads.
			Document.SetSelectionRangeInternal(start, start + length);

			// Keep the caret solid and the blink phase reset while the user is actively editing/moving.
			_caretBlinkVisible = true;
			if (FocusState != FocusState.Unfocused)
			{
				EnsureCaretTimerHooked();
				_caretTimer.Start();
			}

			UpdateDisplaySelection();
		}

		/// <summary>
		/// Clamps and re-renders the interactive selection after a document text change (e.g. a
		/// programmatic SetText while focused). No-op when unfocused.
		/// </summary>
		internal void OnDocumentTextChangedInteractive()
		{
			if (_textBoxView is null || FocusState == FocusState.Unfocused)
			{
				return;
			}

			var length = GetPlainTextContent().Length;
			var start = Math.Clamp(_selection.start, 0, length);
			var selLength = Math.Clamp(_selection.length, 0, length - start);
			_selection = (start, selLength, _selection.selectionEndsAtTheStart);
			Document.SetSelectionRangeInternal(start, start + selLength);
			UpdateDisplaySelection();
		}

		private void DocumentUndoInteractive()
		{
			if (Document.CanUndo())
			{
				Document.Undo();
				ClampSelectionToContent();
			}
		}

		private void DocumentRedoInteractive()
		{
			if (Document.CanRedo())
			{
				Document.Redo();
				ClampSelectionToContent();
			}
		}

		private void ClampSelectionToContent()
		{
			var length = GetPlainTextContent().Length;
			var caret = Math.Clamp(_selection.selectionEndsAtTheStart ? _selection.start : _selection.start + _selection.length, 0, length);
			SetInteractiveSelection(caret, 0);
		}

		#endregion

		#region Rendering

		private void UpdateDisplaySelection()
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			displayBlock.Selection = new TextBlock.Range(_selection.start, _selection.start + _selection.length);

			var focused = FocusState != FocusState.Unfocused;
			displayBlock.RenderSelection = focused;

			if (focused && _selection.length == 0 && !IsReadOnly && _caretBlinkVisible)
			{
				displayBlock.RenderCaret = (_selection.start, GetOpaqueCaretBrush());
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

		#endregion
	}
}
