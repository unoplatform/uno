﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Media;
using System.Diagnostics;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls;

using SelectionDetails = (int start, int length, bool selectionEndsAtTheStart);

public partial class TextBox
{
	private readonly bool _isSkiaTextBox = !FeatureConfiguration.TextBox.UseOverlayOnSkia;

	private CaretWithStemAndThumb _selectionStartThumbfulCaret;
	private CaretWithStemAndThumb _selectionEndThumbfulCaret;
	private TextBoxView _textBoxView;
	private static ITextBoxNotificationsProviderSingleton _textBoxNotificationsSingleton;

	private bool _deleteButtonVisibilityChangedSinceLastUpdateScrolling = true;


	private SelectionDetails _selection;
	private float _caretXOffset; // this is not necessarily the visual offset of the caret, but where the caret is logically supposed to be when moving up and down with the keyboard, even if the caret is temporarily elsewhere
	private CaretDisplayMode _caretMode = CaretDisplayMode.ThumblessCaretHidden;

	private bool _inSelectInternal;

	private (int start, int length)? _pendingSelection;

	private bool _clearHistoryOnTextChanged = true;

	private readonly VirtualKeyModifiers _platformCtrlKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;

	// We track what constitutes one typing "action" that can be undone/redone. The general gist is that
	// any sequence of characters (with backspace allowed) without any navigation moves (pointer click, arrow keys, etc.)
	// will be one "run"/"action". However, there are some arbitrary exceptions, so that is only a rule of thumb.
	private bool _currentlyTyping;
	private bool _suppressCurrentlyTyping;
	private SelectionDetails _selectionWhenTypingStarted;
	private string _textWhenTypingStarted;

	private int _historyIndex;
	private readonly List<HistoryRecord> _history = new(); // the selection of an action is what was selected right before it happened. Might turn out to be unnecessary.

	private (int hashCode, List<(int start, int length)> chunks) _cachedChunks = (-1, new());

	private readonly DispatcherTimer _timer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(0.5)
	};

	private MenuFlyout _contextMenu;
	private readonly Dictionary<ContextMenuItem, MenuFlyoutItem> _flyoutItems = new();

	private Rect? _lastEndCaretRect;
	private Rect? _lastStartCaretRect;

	internal bool IsBackwardSelection => _selection.selectionEndsAtTheStart;

	internal TextBoxView TextBoxView => _textBoxView;

	internal ContentControl ContentElement => _contentElement;

	internal CaretDisplayMode CaretMode
	{
		get => _caretMode;
		private set
		{
			if (_caretMode != value)
			{
				_caretMode = value;
				UpdateDisplaySelection();
				TextBoxView?.DisplayBlock.InvalidateInlines(false);
				if (value is CaretDisplayMode.ThumblessCaretShowing)
				{
					_timer.Start(); // restart
				}
				else if (value is CaretDisplayMode.CaretWithThumbsBothEndsShowing
						 or CaretDisplayMode.CaretWithThumbsOnlyEndShowing)
				{
					_timer.Stop();
				}
			}
		}
	}

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty CanUndoProperty { get; } = CreateCanUndoProperty();

	public bool CanUndo
	{
		get => GetCanUndoValue();
		private set => SetCanUndoValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty CanRedoProperty { get; } = CreateCanRedoProperty();

	public bool CanRedo
	{
		get => GetCanRedoValue();
		private set => SetCanRedoValue(value);
	}

	private void UpdateCanUndoRedo()
	{
		CanUndo = _historyIndex > 0;
		CanRedo = _historyIndex < _history.Count - 1;
	}

	private void TrySetCurrentlyTyping(bool newValue)
	{
		if (newValue == _currentlyTyping || _suppressCurrentlyTyping)
		{
			return;
		}

		if (newValue)
		{
			_textWhenTypingStarted = Text;
			_selectionWhenTypingStarted = (
				_selection.start,
				_selection.length,
				_selection.selectionEndsAtTheStart);
		}
		else
		{
			_historyIndex++;
			_history.RemoveAllAt(_historyIndex);
			_history.Add(new HistoryRecord(
				new ReplaceAction(_textWhenTypingStarted, Text, _selection.start),
				_selectionWhenTypingStarted.start,
				_selectionWhenTypingStarted.length,
				_selectionWhenTypingStarted.selectionEndsAtTheStart));
			UpdateCanUndoRedo();
		}

		_currentlyTyping = newValue;
	}

	partial void OnUnloadedPartial()
	{
		_timer.Stop();
		_selectionStartThumbfulCaret?.Hide();
		_selectionEndThumbfulCaret?.Hide();
		CaretMode = CaretDisplayMode.ThumblessCaretHidden;
	}

	partial void OnForegroundColorChangedPartial(Brush newValue) => TextBoxView?.OnForegroundChanged(newValue);

	partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush) => TextBoxView?.OnSelectionHighlightColorChanged(brush);

	partial void UpdateFontPartial()
	{
		TextBoxView?.UpdateFont();
	}

	partial void OnInputScopeChangedPartial(InputScope newValue) => TextBoxView?.UpdateProperties();

	partial void OnIsSpellCheckEnabledChangedPartial(bool newValue) => TextBoxView?.UpdateProperties();

	partial void OnIsTextPredictionEnabledChangedPartial(bool newValue) => TextBoxView?.UpdateProperties();

	partial void OnMaxLengthChangedPartial(int newValue) => TextBoxView?.UpdateMaxLength();

	partial void OnFlowDirectionChangedPartial()
	{
		TextBoxView?.SetFlowDirectionAndTextAlignment();
	}

	partial void OnTextWrappingChangedPartial()
	{
		TextBoxView?.SetWrapping();
		if (_contentElement is ScrollViewer sv)
		{
			// This is to work around sv giving infinite width. This has the unfortunate problem of resetting
			// locally-set values and/or changes in the template.
			sv.HorizontalScrollBarVisibility = TextWrapping == TextWrapping.NoWrap ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
		}
	}

	partial void SetInputReturnTypePlatform(InputReturnType inputReturnType)
	{
		TextBoxView?.UpdateProperties();
	}

	partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
	{
		TextBoxView?.SetFlowDirectionAndTextAlignment();
	}

	private static SKPaint _spareCaretPaint = new SKPaint();

	private void UpdateTextBoxView()
	{
		_textBoxView ??= new TextBoxView(this);
		if (ContentElement != null)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			if (ContentElement.Content != displayBlock)
			{
				ContentElement.Content = displayBlock;

				if (_isSkiaTextBox)
				{
					_selectionStartThumbfulCaret = new();
					_selectionEndThumbfulCaret = new();

					foreach (var caret in (ReadOnlySpan<CaretWithStemAndThumb>)[_selectionStartThumbfulCaret, _selectionEndThumbfulCaret])
					{
						caret.PointerPressed += CaretOnPointerPressed;
						caret.PointerReleased += CaretOnPointerReleased;
						caret.PointerMoved += CaretOnPointerMoved;
						caret.PointerCanceled += ClearCaretPointerState;
						caret.PointerCaptureLost += ClearCaretPointerState;
					}

					var inlines = displayBlock.Inlines;

					inlines.DrawingStarted += () =>
					{
						_lastStartCaretRect = null;
						_lastEndCaretRect = null;
					};

					inlines.DrawingFinished += () =>
					{
						// Only invalidate the carets after drawing is complete
						// to avoid modifying the children visuals while they are being enumerated.
						NativeDispatcher.Main.Enqueue(() =>
						{
							UpdateFlyoutPosition();
						}, NativeDispatcherPriority.Normal);
					};

					inlines.CaretFound += args =>
					{
						if ((CaretMode == CaretDisplayMode.CaretWithThumbsOnlyEndShowing && args.endCaret) ||
							CaretMode == CaretDisplayMode.ThumblessCaretShowing)
						{
							var caretRect = args.rect;
							var compositor = _visual.Compositor;
							var brush = DefaultBrushes.TextForegroundBrush.GetOrCreateCompositionBrush(compositor);
							var caretPaint = _spareCaretPaint;

							caretPaint.Reset();

							brush.UpdatePaint(caretPaint, caretRect.ToSKRect());
							args.canvas.DrawRect(
								new SKRect((float)caretRect.Left, (float)caretRect.Top, (float)caretRect.Right,
									(float)caretRect.Bottom), caretPaint);
						}

						if (args.endCaret)
						{
							_lastEndCaretRect = args.rect;
						}
						else
						{
							_lastStartCaretRect = args.rect;
						}
					};
				}
			}

			TextBoxView.SetTextNative(Text);
		}
	}

	private void UpdateFlyoutPosition()
	{
		if (CaretMode is CaretDisplayMode.CaretWithThumbsOnlyEndShowing or CaretDisplayMode.CaretWithThumbsBothEndsShowing)
		{
			foreach (var (rectNullable, caret) in (ReadOnlySpan<(Rect?, CaretWithStemAndThumb)>)[(_lastStartCaretRect, _selectionStartThumbfulCaret), (_lastEndCaretRect, _selectionEndThumbfulCaret)])
			{
				if (rectNullable is not { } rect)
				{
					caret.Hide();
					continue;
				}
				var left = rect.GetMidX() - caret.Width / 2;
				caret.Height = rect.Height + 16;
				var transform = TextBoxView.DisplayBlock.TransformToVisual(null);
				if (transform.TransformBounds(rect).IntersectWith(this.GetAbsoluteBoundsRect()) is not null)
				{
					caret.ShowAt(transform.TransformPoint(new Point(left, rect.Top)), XamlRoot);
				}
			}
		}
		else
		{
			_selectionStartThumbfulCaret.Hide();
			_selectionEndThumbfulCaret.Hide();
		}
	}

	partial void OnFocusStateChangedPartial(FocusState focusState, bool initial)
	{
		TextBoxView?.OnFocusStateChanged(focusState);

		if (_isSkiaTextBox)
		{
			if (focusState != FocusState.Unfocused)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
				_textBoxNotificationsSingleton?.OnFocused(this);
			}
			else
			{
				TrySetCurrentlyTyping(false);
				CaretMode = CaretDisplayMode.ThumblessCaretHidden;
				if (!initial)
				{
					_textBoxNotificationsSingleton?.OnUnfocused(this);
				}
				_timer.Stop();
			}
			UpdateDisplaySelection();
		}
	}

#if false // Removing temporarily. We'll need to add it back.
	// TODO: Discuss this public API.
	public static void FinishAutofillContext(bool shouldSave)
	{
		_textBoxNotificationsSingleton?.FinishAutofillContext(shouldSave);
	}
#endif

	partial void SelectPartial(int start, int length)
	{
		TrySetCurrentlyTyping(false);

		if (!_inSelectInternal)
		{
			// SelectInternal sets _selectionEndsAtTheStart and _caretXOffset on its own
			_selection.selectionEndsAtTheStart = false;
			_caretXOffset = (float)(DisplayBlockInlines?.GetRectForIndex(start + length).Left ?? 0);
		}

		var selection = (start, length, _selection.selectionEndsAtTheStart);
		var selectionChanged = selection != _selection;
		_selection = selection;

		// Even when using Skia TextBox, we may need to call Select,
		// which will update the native input in case of Wasm Skia for example.
		TextBoxView?.Select(start, length);

		if (_isSkiaTextBox)
		{
			if (length == 0 && CaretMode == CaretDisplayMode.CaretWithThumbsBothEndsShowing)
			{
				// It doesn't make sense to have 2 caret ends when there's no selection.
				CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}
			else if (CaretMode is CaretDisplayMode.ThumblessCaretHidden)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
			else if (CaretMode is CaretDisplayMode.ThumblessCaretShowing)
			{
				_timer.Start(); // restart
			}

			if (selectionChanged)
			{
				UpdateScrolling();
			}
			UpdateDisplaySelection();
		}
	}

	partial void SelectAllPartial() => Select(0, Text.Length);

	public int SelectionStart
	{
		get => _isSkiaTextBox ? _selection.start : TextBoxView?.GetSelectionStart() ?? 0;
		set => Select(start: value, length: SelectionLength);
	}

	public int SelectionLength
	{
		get => _isSkiaTextBox ? _selection.length : TextBoxView?.GetSelectionLength() ?? 0;
		set => Select(SelectionStart, value);
	}

	private void UpdateDisplaySelection()
	{
		if (_isSkiaTextBox && TextBoxView?.DisplayBlock.Inlines is { } inlines)
		{
			inlines.Selection = (SelectionStart, SelectionStart + SelectionLength);
			var isFocused = FocusState != FocusState.Unfocused || (_contextMenu?.IsOpen ?? false);
			inlines.RenderSelection = isFocused;
			var caretShowing = (CaretMode is CaretDisplayMode.ThumblessCaretShowing && _selection.length == 0) || CaretMode is CaretDisplayMode.CaretWithThumbsOnlyEndShowing or CaretDisplayMode.CaretWithThumbsBothEndsShowing;
			inlines.RenderCaret =
				isFocused &&
				caretShowing &&
				(CaretMode is CaretDisplayMode.CaretWithThumbsBothEndsShowing || !IsReadOnly) && // If read only, we only show carets on touch.
				!FeatureConfiguration.TextBox.HideCaret;
		}
	}

	private void UpdateScrolling() => UpdateScrolling(true);

	/// <summary>
	/// Scrolls the <see cref="_contentElement"/> so that the caret is inside the visible viewport
	/// </summary>
	/// <remarks>
	/// By default, only the selection end moves, while the selection start stays fixed. This is not the
	/// case when dragging the caret thumb, in which case both ends can move. This case requires an
	/// explicit call to this method with <see cref="putSelectionEndInVisibleViewport"/> = false.
	/// </remarks>>
	private void UpdateScrolling(bool putSelectionEndInVisibleViewport)
	{
		if (_isSkiaTextBox && _contentElement is ScrollViewer sv)
		{
			var horizontalOffset = sv.HorizontalOffset;
			var verticalOffset = sv.VerticalOffset;

			var (selectionStart, selectionEnd) = _selection.selectionEndsAtTheStart ? (_selection.start + _selection.length, _selection.start) : (_selection.start, _selection.start + _selection.length);
			var index = putSelectionEndInVisibleViewport ? selectionEnd : selectionStart;

			var caretRect = DisplayBlockInlines.GetRectForIndex(index) with { Width = InlineCollection.CaretThickness };

			// Because the caret is only a single-pixel wide, and because screens can't draw in fractions of a pixel,
			// we need to add Math.Ceiling to ensure that the caret is (fully) included in the visible viewport. This
			// Math.Ceiling sometimes horizontal overscrolling, but it's more acceptable than sometimes not showing the caret.
			var newHorizontalOffset = horizontalOffset.AtMost(caretRect.Left).AtLeast(Math.Ceiling(caretRect.Right - sv.ViewportWidth + InlineCollection.CaretThickness));

			var newVerticalOffset = verticalOffset.AtMost(caretRect.Top).AtLeast(caretRect.Bottom - sv.ViewportHeight);

			sv.ChangeView(newHorizontalOffset, newVerticalOffset, null);
		}
	}

	partial void OnKeyDownPartial(KeyRoutedEventArgs args)
	{
		if (!_isSkiaTextBox)
		{
			OnKeyDownInternal(args);
			return;
		}

		base.OnKeyDown(args);

		if (_selection.length != 0 &&
			args.Key is not (VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right))
		{
			// On WinUI, pressing anything except arrow keys will immediately make the caret thumbless.
			// Even shift + arrow keys will make the caret thumbless (because it's a shift _then_ an arrow key).
			CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		}

		// Note: On windows ** only KeyDown ** is handled (not KeyUp)

		// move to possibly-negative selection length format
		var (selectionStart, selectionLength) = _selection.selectionEndsAtTheStart ? (_selection.start + _selection.length, -_selection.length) : (_selection.start, _selection.length);

		var text = Text;
		var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
		var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);
		switch (args.Key)
		{
			case VirtualKey.Up:
				// on macOS start of document is `Command` and `Up`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownUpArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Down:
				// on macOS end of document is `Command` and `Down`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownDownArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Left:
				KeyDownLeftArrow(args, text, shift, ctrl, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Right:
				KeyDownRightArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Home:
				KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.End:
				KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			// TODO: PageUp/Down
			case VirtualKey.Back when !IsReadOnly:
				KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Delete when !IsReadOnly:
				KeyDownDelete(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.A when ctrl:
				if (!HasPointerCapture)
				{
					args.Handled = true;
					TrySetCurrentlyTyping(false);
					selectionStart = 0;
					selectionLength = text.Length;
				}
				break;
			case VirtualKey.Z when ctrl:
				if (!HasPointerCapture)
				{
					args.Handled = true;
					Undo();
				}
				return;
			case VirtualKey.Y when ctrl:
				if (!HasPointerCapture)
				{
					args.Handled = true;
					Redo();
				}
				return;
			case VirtualKey.X when ctrl:
				CutSelectionToClipboard();
				selectionLength = 0;
				text = Text;
				break;
			case VirtualKey.V when ctrl:
			case VirtualKey.Insert when shift:
				PasteFromClipboard(); // async so doesn't actually do anything right now
				break;
			case VirtualKey.C when ctrl:
			case VirtualKey.Insert when ctrl:
				CopySelectionToClipboard();
				break;
			case VirtualKey.Escape:
				if (HasPointerCapture)
				{
					args.Handled = true;
					ReleasePointerCaptures();
				}
				break;
			case VirtualKey.LeftShift:
			case VirtualKey.RightShift:
			case VirtualKey.Shift:
			case VirtualKey.Control:
			case VirtualKey.LeftControl:
			case VirtualKey.RightControl:
				// No-op when pressing these key specifically.
				break;
			default:
				if (!IsReadOnly && !HasPointerCapture && args.UnicodeKey is { } c && (AcceptsReturn || args.UnicodeKey is not '\r' or '\n'))
				{
					TrySetCurrentlyTyping(true);
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);

					if (c is '\n')
					{
						// TextBox autoconverts to \r, like WinUI
						c = '\r';
					}

					text = text[..start] + c + text[end..];
					selectionStart = start + 1;
					selectionLength = 0;
				}
				break;
		}

		selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart));
		selectionLength = Math.Max(-selectionStart, Math.Min(text.Length - selectionStart, selectionLength));

		var caretXOffset = _caretXOffset;

		_suppressCurrentlyTyping = true;
		_clearHistoryOnTextChanged = false;
		if (!HasPointerCapture)
		{
			_pendingSelection = (selectionStart, selectionLength);
		}
		ProcessTextInput(text);
		_clearHistoryOnTextChanged = true;
		_suppressCurrentlyTyping = false;

		// don't change the caret offset when moving up and down
		if (args.Key is VirtualKey.Up or VirtualKey.Down)
		{
			// this condition is accurate in the case of hitting Down on the last line
			// or up on the first line. On WinUI, the caret offset won't change.
			_caretXOffset = caretXOffset;
		}
	}

	internal void SetPendingSelection(int selectionStart, int selectionLength)
		=> _pendingSelection = (selectionStart, selectionLength);

	private void KeyDownBack(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is `option` + `delete` (same location as backspace on PC keyboards) that removes the previous word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (HasPointerCapture)
		{
			return;
		}
		if (selectionLength != 0)
		{
			TrySetCurrentlyTyping(false);
			TrySetCurrentlyTyping(true);

			var start = Math.Min(selectionStart, selectionStart + selectionLength);
			var end = Math.Max(selectionStart, selectionStart + selectionLength);
			text = text[..start] + text[end..];
			selectionLength = 0;
			selectionStart = start;
		}
		else if (selectionStart != 0)
		{
			if (ctrl)
			{
				// ctrl always ends the previous typing run
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// idempotent call to make sure we're starting a new typing run if we're not in one already
				TrySetCurrentlyTyping(true);
			}

			var oldText = text;
			var index = ctrl ? FindChunkAt(selectionStart, false).start : selectionStart - 1;
			text = text[..index] + text[selectionStart..];
			selectionStart = index;

			if (ctrl)
			{
				// typing after ctrl starts a new run, and not a part of the ctrl-backspace run
				CommitAction(new ReplaceAction(oldText, text, selectionStart));
			}
		}
	}

	private void KeyDownUpArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// TODO ctrl+up
		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetUpDownResult(text, selectionStart, selectionLength, shift, up: true);
		if (shift)
		{
			selectionLength = newEnd - selectionStart;
		}
		else
		{
			selectionStart = newEnd;
			selectionLength = 0;
		}

		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownDownArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// TODO ctrl+down
		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetUpDownResult(text, selectionStart, selectionLength, shift, up: false);
		if (shift)
		{
			selectionLength = newEnd - selectionStart;
		}
		else
		{
			selectionStart = newEnd;
			selectionLength = 0;
		}

		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownLeftArrow(KeyRoutedEventArgs args, string text, bool shift, bool ctrl, ref int selectionStart, ref int selectionLength)
	{
		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		if (!shift && selectionStart == 0 && selectionLength == 0 || shift && selectionStart + selectionLength == 0)
		{
			return;
		}

		args.Handled = true;

		if (shift)
		{
			var end = selectionStart + selectionLength;
			if (ctrl)
			{
				end = FindChunkAt(end, false).start;
			}
			else
			{
				end--;
			}

			selectionLength = end - selectionStart;
		}
		else
		{
			if (selectionLength == 0)
			{
				selectionStart = ctrl ? FindChunkAt(selectionStart, false).start : selectionStart - 1;
			}
			else
			{
				selectionStart = Math.Min(selectionStart, selectionStart + selectionLength);
			}
			selectionLength = 0;
		}
	}

	private void KeyDownRightArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is:
		// * `option` + `right` that moves to the next word
		// * `shift` + `option` + `right` that select the next word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var moveOutRight = !shift && selectionStart == text.Length && selectionLength == 0 || shift && selectionStart + selectionLength == Text.Length;
		if (!moveOutRight)
		{
			args.Handled = true;

			if (shift)
			{
				var end = selectionStart + selectionLength;
				if (ctrl)
				{
					var chunk = FindChunkAt(end, true);
					end = chunk.start + chunk.length;
				}
				else
				{
					end++;
				}

				selectionLength = end - selectionStart;
			}
			else
			{
				if (selectionLength == 0)
				{
					if (ctrl)
					{
						var chunk = FindChunkAt(selectionStart, true);
						selectionStart = chunk.start + chunk.length;
					}
					else
					{
						selectionStart += 1;
					}
				}
				else
				{
					selectionStart = Math.Max(selectionStart, selectionStart + selectionLength);
				}
				selectionLength = 0;
			}
		}
	}

	private void KeyDownHome(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		if (shift)
		{
			selectionLength = ctrl ? -selectionStart : GetLineAt(text, selectionStart, selectionLength).start - selectionStart;
		}
		else
		{
			selectionStart = ctrl ? 0 : GetLineAt(text, selectionStart, selectionLength).start;
			selectionLength = 0;
		}
		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownEnd(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		if (HasPointerCapture)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		if (shift)
		{
			if (ctrl)
			{
				selectionLength = text.Length - selectionStart;
			}
			else
			{
				var line = GetLineAt(text, selectionStart, selectionLength);
				selectionLength = line.start + line.length - selectionStart;
			}
		}
		else
		{
			if (ctrl)
			{
				selectionStart = text.Length;
			}
			else
			{
				var line = GetLineAt(text, selectionStart, selectionLength);
				selectionStart = line.start + line.length;
				if (line.length > 0 && selectionStart < text.Length && text[selectionStart - 1] == '\r')
				{
					// a newline is part of the line just before it, but End shouldn't go past the newline
					selectionStart--;
				}
			}
			selectionLength = 0;
		}
		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownDelete(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is `option` + `delete>` that removes the next word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (HasPointerCapture)
		{
			return;
		}
		TrySetCurrentlyTyping(false);
		args.Handled = true;
		var oldText = text;
		if (selectionLength != 0)
		{
			var start = Math.Min(selectionStart, selectionStart + selectionLength);
			var end = Math.Max(selectionStart, selectionStart + selectionLength);
			text = text[..start] + text[end..];
			CommitAction(new DeleteAction(oldText, text, selectionStart, selectionLength));
			selectionLength = 0;
			selectionStart = start;
		}
		else if (selectionStart != text.Length)
		{
			if (shift)
			{
				// On WinUI, shift-delete doesn't do anything if nothing is selected for some reason
				// We still end the previous typing run
				return;
			}
			int index;
			if (ctrl)
			{
				var chunk = FindChunkAt(selectionStart, true);
				index = chunk.start + chunk.length;
			}
			else
			{
				index = selectionStart + 1;
			}
			text = text[..selectionStart] + text[index..];
			// On WinUI, when ctrl-delete is Undone, the deleted text actually gets selected even though initially, nothing was selected
			CommitAction(new DeleteAction(oldText, text, selectionStart, ctrl ? index - selectionStart : 0));
		}
	}

	/// <summary>
	/// Takes a possibly-negative selection length, indicating a selection that goes backwards.
	/// This makes the calculations a lot more natural.
	/// </summary>
	internal void SelectInternal(int selectionStart, int selectionLength)
	{
		_inSelectInternal = true;
		_selection.selectionEndsAtTheStart = selectionLength < 0;
		if (DisplayBlockInlines is { }) // this check is important because on start up, the Inlines haven't been created yet.
		{
			_caretXOffset = selectionLength >= 0 ?
				(float)DisplayBlockInlines.GetRectForIndex(selectionStart + selectionLength).Left :
				(float)DisplayBlockInlines.GetRectForIndex(selectionStart + selectionLength).Right;
		}
		Select(Math.Min(selectionStart, selectionStart + selectionLength), Math.Abs(selectionLength));
		_inSelectInternal = false;
	}

	private void TimerOnTick(object sender, object e)
	{
		if (IsLoaded && IsFocused)
		{
			if (CaretMode == CaretDisplayMode.ThumblessCaretHidden)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
			else if (CaretMode == CaretDisplayMode.ThumblessCaretShowing)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretHidden;
			}
			UpdateDisplaySelection();
		}
	}

	/// <summary>
	/// The parameters here use the possibly-negative length format
	/// </summary>
	private (int start, int length) GetLineAt(string text, int selectionStart, int selectionLength)
	{
		if (Text.Length == 0)
		{
			return (0, 0);
		}

		var lines = DisplayBlockInlines.GetLineIntervals();
		global::System.Diagnostics.CI.Assert(lines.Count > 0);

		var end = selectionStart + selectionLength;

		foreach (var line in lines)
		{
			if (line.start <= end && end < line.start + line.length)
			{
				return line;
			}
		}

		// end == Text.Length
		return lines[^1];
	}

	/// <summary>
	/// There are 2 concepts of a "line", there's a line that ends at end-of-text, \r, \n, etc.
	/// and then there's an actual rendered line that may end due to wrapping and not a line break.
	/// This method cares about the second kind of lines.
	/// </summary>
	private int GetUpDownResult(string text, int selectionStart, int selectionLength, bool shift, bool up)
	{
		if (text.Length == 0)
		{
			return 0;
		}
		var startLine = GetLineAt(text, selectionStart, 0);
		var endLine = GetLineAt(text, selectionStart + selectionLength, 0);
		var lines = DisplayBlockInlines.GetLineIntervals();
		var startLineIndex = lines.IndexOf(startLine);
		var endLineIndex = lines.IndexOf(endLine);

		if (up && shift && endLineIndex == 0)
		{
			return 0; // first line, goes to the beginning
		}
		else if (!up && shift && endLineIndex == lines.Count - 1)
		{
			return text.Length; // last line, goes to the end
		}
		else if (!up && !shift && (startLineIndex == lines.Count - 1 || endLineIndex == lines.Count - 1))
		{
			return text.Length; // last line, goes to the end
		}

		var newLineIndex = up ?
			selectionLength < 0 || shift ? Math.Max(0, endLineIndex - 1) : Math.Max(0, startLineIndex - 1) :
			selectionLength > 0 || shift ? Math.Min(lines.Count, endLineIndex + 1) : Math.Min(lines.Count, startLineIndex + 1);

		var rect = DisplayBlockInlines.GetRectForIndex(selectionStart + selectionLength);
		var x = _caretXOffset;
		var y = (newLineIndex + 0.5) * rect.Height; // 0.5 is to get the center of the line, rect.Height is line height
		var index = Math.Max(0, DisplayBlockInlines.GetIndexAt(new Point(x, y), true, true));
		if (text.Length > index - 1
			&& index - 1 >= 0
			&& index == lines[newLineIndex].start + lines[newLineIndex].length
			&& (text[index - 1] == '\r' || text[index - 1] == ' '))
		{
			// if we're past \r or space, we will actually be at the beginning of the next line, so we take a step back
			index--;
		}

		return index;
	}

	private InlineCollection DisplayBlockInlines => TextBoxView?.DisplayBlock.Inlines;

	/// <param name="right">Where to look for a chunk to the right or left of the caret when the caret is between chunks</param>
	private (int start, int length) FindChunkAt(int index, bool right)
	{
		if (Text.GetHashCode() != _cachedChunks.hashCode)
		{
			GenerateChunks();
		}

		var i = 0;
		foreach (var chunk in _cachedChunks.chunks)
		{
			if (chunk.start < index && chunk.start + chunk.length > index
				|| chunk.start == index && right
				|| chunk.start + chunk.length == index && !right)
			{
				return chunk;
			}

			i += chunk.length;
		}

		return _cachedChunks.chunks.Count > 0 ? _cachedChunks.chunks[^1] : (0, 0);
	}

	private void GenerateChunks()
	{
		var text = Text;

		_cachedChunks.hashCode = text.GetHashCode();
		var chunks = _cachedChunks.chunks;

		chunks.Clear();

		// a chunk is possible (continuous letters/numbers or continuous non-letters/non-numbers) then possible spaces.
		// \r and \t are always their own chunks
		var length = text.Length;
		for (var i = 0; i < length;)
		{
			var start = i;
			var c = text[i];
			if (c is '\r' or '\t')
			{
				i++;
			}
			else if (c == ' ')
			{
				while (i < length && text[i] == ' ')
				{
					i++;
				}
			}
			else if (char.IsLetterOrDigit(text[i]))
			{
				while (i < length && char.IsLetterOrDigit(text[i]))
				{
					i++;
				}
				while (i < length && text[i] == ' ')
				{
					i++;
				}
			}
			else
			{
				while (i < length && !char.IsLetterOrDigit(text[i]) && text[i] != ' ' && text[i] != '\r')
				{
					i++;
				}
				while (i < length && text[i] == ' ')
				{
					i++;
				}
			}

			chunks.Add((start, i - start));
		}
	}

	/// <summary>
	/// There are 2 concepts of a "line", there's a line that ends at end-of-text, \r, \n, etc.
	/// and then there's an actual rendered line that may end due to wrapping and not a line break.
	/// StartOfLine and EndOfLine care about the first kind of lines.
	/// </summary>
	private int StartOfLine(int i)
	{
		var text = Text;

		i--;
		for (; i >= 0; i--)
		{
			var c = text[i];
			if (c == '\r')
			{
				break;
			}
		}

		return i + 1;
	}

	private int EndOfLine(int i)
	{
		var index = Text.IndexOf('\r', i);
		return index == -1 ? Text.Length - 1 : index;
	}

	partial void InitializePartial()
	{
		_ = ApiExtensibility.CreateInstance(null, out _textBoxNotificationsSingleton);
	}

	partial void OnTextChangedPartial()
	{
		if (_isSkiaTextBox)
		{
			if (_pendingSelection is { } selection)
			{
				SelectInternal(selection.start, selection.length);
			}
			else
			{
				SelectInternal(0, 0);
			}

			if (_clearHistoryOnTextChanged)
			{
				ClearUndoRedoHistory();
			}

			_textBoxNotificationsSingleton?.NotifyValueChanged(this);
		}
	}

	private string RemoveLF(string baseString)
	{

		var builder = new StringBuilder();
		for (int i = 0; i < baseString.Length; i++)
		{
			var c = baseString[i];
			if (c == '\n')
			{
				builder.Append('\r');
			}
			else if (c == '\r' && i + 1 < baseString.Length && baseString[i + 1] == '\n')
			{
				if (_pendingSelection is { } selection)
				{
					var (start, end) = (selection.start, selection.start + selection.length);
					if (start > i)
					{
						start--;
					}
					if (end > i)
					{
						end--;
					}
					_pendingSelection = (start, end - start);
				}

				builder.Append('\r');
				i++;
			}
			else
			{
				builder.Append(c);
			}
		}

		baseString = builder.ToString();
		return baseString;
	}

	partial void PasteFromClipboardPartial(string adjustedClipboardText, int selectionStart, string newText)
	{
		if (_isSkiaTextBox)
		{
			if (_currentlyTyping)
			{
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// we only commit an action if we were not typing, because if we were typing and we now set CurrentlyTyping = false,
				// we will already get a new action from the setter, so we don't need to commit another one here.
				CommitAction(new ReplaceAction(Text, newText, selectionStart));
			}

			_pendingSelection = (selectionStart + adjustedClipboardText.Length, 0);
		}
	}

	partial void CutSelectionToClipboardPartial()
	{
		if (_isSkiaTextBox)
		{
			if (_currentlyTyping)
			{
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// we only commit an action if we were not typing, because if we were typing and we now set CurrentlyTyping = false,
				// we will already get a new action from the setter, so we don't need to commit another one here.
				CommitAction(new ReplaceAction(Text, Text.Remove(SelectionStart, SelectionLength), SelectionStart + SelectionLength));
			}
			_pendingSelection = (_selection.start, 0);
		}
	}

	private void EnsureHistory()
	{
		if (_history.Count == 0)
		{
			_history.Add(new HistoryRecord(SentinelAction.Instance, _selection.start, _selection.length, _selection.selectionEndsAtTheStart));
		}
		_historyIndex = Math.Max(0, Math.Min(_history.Count - 1, _historyIndex));
		UpdateCanUndoRedo();
	}

	public void ClearUndoRedoHistory()
	{
		TrySetCurrentlyTyping(false);
		_history.Clear();
		EnsureHistory();
	}

	/// <summary>
	/// Adds a new Action at the present point in history and deletes the old "future"
	/// </summary>
	private void CommitAction(TextBoxAction action)
	{
		_historyIndex++;
		_history.RemoveAllAt(_historyIndex);
		_history.Add(new HistoryRecord(action, _selection.start, _selection.length, _selection.selectionEndsAtTheStart));
		UpdateCanUndoRedo();
	}

	public void Undo()
	{
		if (!_isSkiaTextBox)
		{
			return;
		}

		TrySetCurrentlyTyping(false);
		if (_historyIndex == 0 || HasPointerCapture)
		{
			return;
		}

		var currentAction = _history[_historyIndex];
		_historyIndex--;

		_clearHistoryOnTextChanged = false;
		switch (currentAction.Action)
		{
			case ReplaceAction r:
				// remember that we use the possibly-negative format in _pendingSelection
				_pendingSelection = currentAction.SelectionEndsAtTheStart ?
					(currentAction.SelectionStart + currentAction.SelectionLength, -currentAction.SelectionLength) :
					(currentAction.SelectionStart, currentAction.SelectionLength);
				ProcessTextInput(r.OldText);
				break;
			case DeleteAction d:
				_pendingSelection = (d.UndoSelectionStart, d.UndoSelectionLength);
				ProcessTextInput(d.OldText);
				break;
			case SentinelAction:
				break;
			default:
				global::System.Diagnostics.CI.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
				break;
		}
		_clearHistoryOnTextChanged = true;
		UpdateCanUndoRedo();
	}

	public void Redo()
	{
		if (!_isSkiaTextBox)
		{
			return;
		}

		if (_historyIndex == _history.Count - 1 || HasPointerCapture)
		{
			return;
		}

		TrySetCurrentlyTyping(false);

		_historyIndex++;
		var currentAction = _history[_historyIndex];

		_clearHistoryOnTextChanged = false;
		switch (currentAction.Action)
		{
			case ReplaceAction r:
				_pendingSelection = (r.caretIndexAfterReplacement, 0); // we always have an empty selection here.
				ProcessTextInput(r.NewText);
				break;
			case DeleteAction d:
				_pendingSelection = (Math.Min(d.UndoSelectionStart, d.UndoSelectionStart + d.UndoSelectionLength), 0);
				ProcessTextInput(d.NewText);
				break;
			case SentinelAction:
				break;
			default:
				global::System.Diagnostics.CI.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
				break;
		}
		_clearHistoryOnTextChanged = true;
		UpdateCanUndoRedo();
	}

	internal override bool IsDelegatingFocusToTemplateChild()
		=> OperatingSystem.IsBrowser();

	internal enum CaretDisplayMode
	{
		ThumblessCaretHidden,
		ThumblessCaretShowing,
		CaretWithThumbsOnlyEndShowing,
		CaretWithThumbsBothEndsShowing
	}

	private record struct HistoryRecord(TextBoxAction Action, int SelectionStart, int SelectionLength, bool SelectionEndsAtTheStart);

	private abstract record TextBoxAction;

	/// <summary>
	/// Instead of remembered what was removed and what was added in place, we just remember the initial and final states
	/// as well as where the caret will be if we Redo. This is used by typing, paste, etc.
	/// </summary>
	private record ReplaceAction(string OldText, string NewText, int caretIndexAfterReplacement) : TextBoxAction;

	/// <summary>
	/// Unlike other forms of text modification, Delete doesn't follow the simple undo sequence of *unapply modification* -> *select what was selected when the action happened*
	/// So we need to specifically need to remember what selection to go to when we Undo depending on how we got here (e.g. ctrl vs no ctrl)
	/// Selection uses the possibly-negative format
	/// </summary>
	private record DeleteAction(string OldText, string NewText, int UndoSelectionStart, int UndoSelectionLength) : TextBoxAction;

	/// <summary>
	/// Probably unnecessary, but we pad the bottom of the history as it makes index manipulation easier (the invariant we
	/// get is that history is never empty)
	/// </summary>
	private record SentinelAction : TextBoxAction
	{
		private SentinelAction() { }
		public static SentinelAction Instance { get; } = new SentinelAction();
	}

	private sealed class TextBoxCommand(Action action) : ICommand
	{
		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter) => action();

#pragma warning disable 67 // An event was declared but never used in the class in which it was declared.
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67 // An event was declared but never used in the class in which it was declared.
	}

	private class CaretWithStemAndThumb : Grid
	{
		// This is equal to the default system accent color on Windows.
		// This is, however, a constant color that doesn't depend on the
		// current system accent color. Changing the accent color does NOT
		// change the thumb color on WinUI, only the selection color.
		private static readonly Color ThumbFillColor = Colors.FromARGB("FF0078D7");

		private readonly Ellipse _thumb;
		private readonly Ellipse _thumbRing;
		private readonly Rectangle _stem;
		private Popup _popup;

		public PointerPoint LastPointerDown { get; set; }

		public CaretWithStemAndThumb()
		{
			// Numbers and colors below are partially measured by hand from WinUI and partially made up to be reasonable.

			Background = new SolidColorBrush(Colors.Transparent); // to hit-test positively everywhere in the grid

			Width = 16;

			RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Pixel) });

			_thumb = new Ellipse
			{
				Fill = new SolidColorBrush(Colors.White),
				Width = 16,
				Height = 16
			};

			_thumbRing = new Ellipse
			{
				Stroke = new SolidColorBrush(ThumbFillColor),
				StrokeThickness = 2,
				Width = 14,
				Height = 14,
				Margin = new Thickness(1)
			};

			_stem = new Rectangle
			{
				Visibility = Visibility.Collapsed,
				IsHitTestVisible = false,
				HorizontalAlignment = HorizontalAlignment.Center,
				Stroke = new SolidColorBrush(ThumbFillColor),
				Width = 2
			};

			Grid.SetRow(_stem, 0);
			Grid.SetRow(_thumb, 1);
			Grid.SetRow(_thumbRing, 1);

			Children.Add(_stem);
			Children.Add(_thumb);
			Children.Add(_thumbRing);
		}

		public void SetStemVisible(bool visible) => _stem.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

		public void ShowAt(Point p, XamlRoot xamlRoot)
		{
			_popup ??= new Popup
			{
				Child = this,
				IsLightDismissEnabled = false,
				XamlRoot = xamlRoot
			};
			_popup.PopupPanel.Visual.ZIndex = VisualTree.TextBoxTouchKnobPopupZIndex;

			_popup.HorizontalOffset = p.X;
			_popup.VerticalOffset = p.Y;
			if (!_popup.IsOpen)
			{
				_popup.IsOpen = true;
				// We don't have an event that fires when we actually render,
				// so we have to settle for this somewhat-inaccurate approximation
				// of dispatching an update call whenever InvalidateRender fires.
				xamlRoot.RenderInvalidated += OnInvalidateRender;
			}
		}

		private void OnInvalidateRender()
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				if (FocusManager.GetFocusedElement(XamlRoot!) is TextBox textBox)
				{
					textBox.UpdateFlyoutPosition();
				}
			}, NativeDispatcherPriority.Idle);
		}

		public void Hide()
		{
			if (XamlRoot is { })
			{
				XamlRoot.RenderInvalidated -= OnInvalidateRender;
			}
			if (_popup is not null)
			{
				_popup.IsOpen = false;
			}
		}
	}
}
