using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	private readonly bool _isSkiaTextBox = !FeatureConfiguration.TextBox.UseOverlayOnSkia;

	private TextBoxView _textBoxView;

	private readonly Rectangle _caretRect = new Rectangle();
	private readonly List<Rectangle> _cachedRects = new List<Rectangle>();
	private int _usedRects;
	private bool _rectsChanged;

	private (int start, int length) _selection;
	private bool _selectionEndsAtTheStart;
	private bool _showCaret = true;

	private (int start, int length)? _pendingSelection;

	private (PointerPoint point, int repeatedPresses) _lastPointerDown; // point is null before first press
	private bool _isPressed; // can still be false if the pointer is still pressed but Escape is pressed.

	private bool _clearHistoryOnTextChanged = true;

	private readonly VirtualKeyModifiers _platformCtrlKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;

	// We track what constitutes one typing "action" that can be undone/redone. The general gist is that
	// any sequence of characters (with backspace allowed) without any navigation moves (pointer click, arrow keys, etc.)
	// will be one "run"/"action". However, there are some arbitrary exceptions, so that is only a rule of thumb.
	private bool _currentlyTyping;
	private bool _suppressCurrentlyTyping;
	private (int selectionStart, int selectionLength, bool selectionEndsAtTheStart) _selectionWhenTypingStarted;
	private string _textWhenTypingStarted;

	private int _historyIndex;
	private List<HistoryRecord> _history = new(); // the selection of an action is what was selected right before it happened. Might turn out to be unnecessary.

	private (int start, int length, bool tripleTap)? _multiTapChunk;
	private (int hashCode, List<(int start, int length)> chunks) _cachedChunks = (-1, new());

	private readonly DispatcherTimer _timer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(0.5)
	};

	private MenuFlyout _contextMenu;
	private readonly Dictionary<ContextMenuItem, MenuFlyoutItem> _flyoutItems = new();

	internal TextBoxView TextBoxView => _textBoxView;

	internal ContentControl ContentElement => _contentElement;

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
				_selectionEndsAtTheStart);
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(!_isSkiaTextBox || _selection.length == 0);
			_historyIndex++;
			_history.RemoveAllAt(_historyIndex);
			_history.Add(new HistoryRecord(
				new ReplaceAction(_textWhenTypingStarted, Text, _selection.start),
				_selectionWhenTypingStarted.selectionStart,
				_selectionWhenTypingStarted.selectionLength,
				_selectionWhenTypingStarted.selectionEndsAtTheStart));
			UpdateCanUndoRedo();
		}

		_currentlyTyping = newValue;
	}

	partial void OnForegroundColorChangedPartial(Brush newValue) => TextBoxView?.OnForegroundChanged(newValue);

	partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush) => TextBoxView?.OnSelectionHighlightColorChanged(brush);

	partial void UpdateFontPartial()
	{
		TextBoxView?.UpdateFont();
	}

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


	partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
	{
		TextBoxView?.SetFlowDirectionAndTextAlignment();
	}

	private void UpdateTextBoxView()
	{
		_textBoxView ??= new TextBoxView(this);
		if (ContentElement != null)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			if (!_isSkiaTextBox)
			{
				if (ContentElement.Content != displayBlock)
				{
					ContentElement.Content = displayBlock;
				}
			}
			else
			{
				if (ContentElement.Content is not Grid { Name: "TextBoxViewGrid" })
				{
					var canvas = new Canvas
					{
						HorizontalAlignment = HorizontalAlignment.Left
					};
					var grid = new Grid
					{
						Name = "TextBoxViewGrid",
						Children =
						{
							canvas,
							displayBlock
						},
						RowDefinitions =
						{
							new RowDefinition { Height = GridLengthHelper.OneStar }
						}
					};

					displayBlock.LayoutUpdated += (_, _) => canvas.Width = Math.Ceiling(displayBlock.ActualWidth + Math.Ceiling(DisplayBlockInlines.AverageLineHeight * InlineCollection.CaretThicknessAsRatioOfLineHeight));

					bool lastFoundCaret = false;
					bool currentFoundCaret = false;

					var inlines = displayBlock.Inlines;
					inlines.DrawingStarted += () =>
					{
						currentFoundCaret = false;
						_rectsChanged = false;
						_usedRects = 0;
					};

					inlines.DrawingFinished += () =>
					{
						// we don't add rectangles to the canvas as they come. Instead,
						// we accumulate all the rects and compare with the preexisting canvas.Children
						// We only update if there is actually a change. This avoids a lot of problems
						// that lead to an infinite layout-invalidate-layout-invalidate loop
						if (_rectsChanged || _cachedRects.Count != _usedRects || lastFoundCaret != currentFoundCaret)
						{
							// Might be better to use some table-doubling logic, but shouldn't matter very much.
							// If so, don't forget to also change canvas.Children.AddRange(_cachedRects) below, which
							// assumes that _cachedRects now only has the needed Rectangles.
							_cachedRects.RemoveRange(_usedRects, _cachedRects.Count - _usedRects);

							canvas.Children.Clear();
							canvas.Children.AddRange(_cachedRects);

							if (currentFoundCaret)
							{
								canvas.Children.Add(_caretRect);
							}
						}

						lastFoundCaret = currentFoundCaret;
					};

					inlines.SelectionFound += t =>
					{
						var rect = t.rect;
						if (_cachedRects.Count <= _usedRects)
						{
							_rectsChanged = true;
							_cachedRects.Add(new Rectangle());
						}
						var rectangle = _cachedRects[_usedRects++];

						if (!ReferenceEquals(rectangle.Fill, SelectionHighlightColor))
						{
							_rectsChanged = true;
							rectangle.Fill = SelectionHighlightColor;
						}
						if (rectangle.Width != Math.Ceiling(rect.Width))
						{
							_rectsChanged = true;
							rectangle.Width = Math.Ceiling(rect.Width);
						}
						if (rectangle.Height != Math.Ceiling(rect.Height))
						{
							_rectsChanged = true;
							rectangle.Height = Math.Ceiling(rect.Height);
						}
						if ((double)rectangle.GetValue(Canvas.LeftProperty) != rect.Left)
						{
							_rectsChanged = true;
							rectangle.SetValue(Canvas.LeftProperty, rect.Left);
						}
						if ((double)rectangle.GetValue(Canvas.TopProperty) != rect.Top)
						{
							_rectsChanged = true;
							rectangle.SetValue(Canvas.TopProperty, rect.Top);
						}
					};

					inlines.CaretFound += rect =>
					{
						var oldCaretState = (_caretRect.Width, _caretRect.Height, (double)_caretRect.GetValue(Canvas.LeftProperty), (double)_caretRect.GetValue(Canvas.TopProperty), _caretRect.Fill);
						if (oldCaretState != (Math.Ceiling(rect.Width), Math.Ceiling(rect.Height), rect.Left, rect.Top, DefaultBrushes.TextForegroundBrush))
						{
							_rectsChanged = true;
							_caretRect.Width = Math.Ceiling(rect.Width);
							_caretRect.Height = Math.Ceiling(rect.Height);
							_caretRect.SetValue(Canvas.LeftProperty, rect.Left);
							_caretRect.SetValue(Canvas.TopProperty, rect.Top);
							_caretRect.Fill = DefaultBrushes.TextForegroundBrush;
						}

						currentFoundCaret = true;
					};

					ContentElement.Content = grid;
				}
			}

			TextBoxView.SetTextNative(Text);
		}
	}

	partial void OnFocusStateChangedPartial(FocusState focusState)
	{
		if (!_isSkiaTextBox)
		{
			TextBoxView?.OnFocusStateChanged(focusState);
		}
		else
		{
			if (focusState != FocusState.Unfocused)
			{
				_showCaret = true;
				_timer.Start();
			}
			else
			{
				TrySetCurrentlyTyping(false);
				_showCaret = false;
				_timer.Stop();
			}
			UpdateDisplaySelection();
		}
	}

	partial void SelectPartial(int start, int length)
	{
		TrySetCurrentlyTyping(false);
		_selectionEndsAtTheStart = false;
		_selection = (start, length);
		if (!_isSkiaTextBox)
		{
			TextBoxView?.Select(start, length);
		}
		else
		{
			_timer.Stop();
			_showCaret = true;
			_timer.Start();
			UpdateDisplaySelection();
			UpdateScrolling();
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
			inlines.RenderSelection = FocusState != FocusState.Unfocused || (_contextMenu?.IsOpen ?? false);
			inlines.RenderCaret = inlines.RenderSelection && _showCaret && !FeatureConfiguration.TextBox.HideCaret && !IsReadOnly && _selection.length == 0;
			inlines.CaretAtEndOfSelection = !_selectionEndsAtTheStart;
		}
	}

	private void UpdateScrolling()
	{
		if (_isSkiaTextBox && _contentElement is ScrollViewer sv)
		{
			var selectionEnd = _selectionEndsAtTheStart ? _selection.start : _selection.start + _selection.length;

			var horizontalOffset = sv.HorizontalOffset;
			var verticalOffset = sv.VerticalOffset;

			var rect = DisplayBlockInlines.GetRectForIndex(selectionEnd);

			// TODO: we are sometimes horizontally overscrolling, but it's more visually pleasant that underscrolling as we want the caret to be fully showing.
			var newHorizontalOffset = horizontalOffset.AtMost(rect.Left).AtLeast(Math.Ceiling(rect.Left - sv.ViewportWidth + Math.Ceiling(DisplayBlockInlines.AverageLineHeight * InlineCollection.CaretThicknessAsRatioOfLineHeight)));
			var newVerticalOffset = verticalOffset.AtMost(rect.Top).AtLeast(rect.Top - sv.ViewportWidth);

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

		// Note: On windows ** only KeyDown ** is handled (not KeyUp)

		// move to possibly-negative selection length format
		var (selectionStart, selectionLength) = _selectionEndsAtTheStart ? (_selection.start + _selection.length, -_selection.length) : (_selection.start, _selection.length);

		var text = Text;
		var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
		var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);
		switch (args.Key)
		{
			case VirtualKey.Up:
				KeyDownUpArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Down:
				KeyDownDownArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
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
				if (!_isPressed)
				{
					args.Handled = true;
					TrySetCurrentlyTyping(false);
					selectionStart = 0;
					selectionLength = text.Length;
				}
				break;
			case VirtualKey.Z when ctrl:
				if (!_isPressed)
				{
					args.Handled = true;
					Undo();
				}
				return;
			case VirtualKey.Y when ctrl:
				if (!_isPressed)
				{
					args.Handled = !_isPressed;
					Redo();
				}
				return;
			case VirtualKey.X when ctrl:
				CutSelectionToClipboard();
				selectionLength = 0;
				text = Text;
				break;
			case VirtualKey.V when ctrl:
				PasteFromClipboard(); // async so doesn't actually do anything right now
				break;
			case VirtualKey.C when ctrl:
				CopySelectionToClipboard();
				break;
			case VirtualKey.Escape:
				if (_isPressed)
				{
					args.Handled = true;
					_isPressed = false;
				}
				break;
			default:
				if (!IsReadOnly && !_isPressed && args.UnicodeKey is { } c && (AcceptsReturn || args.UnicodeKey != '\r'))
				{
					TrySetCurrentlyTyping(true);
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);

					text = text[..start] + c + text[end..];
					selectionStart = start + 1;
					selectionLength = 0;
				}
				break;
		}

		selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart));
		selectionLength = Math.Max(-selectionStart, Math.Min(text.Length - selectionStart, selectionLength));

		_suppressCurrentlyTyping = true;
		if (text == Text)
		{
			if (!_isPressed)
			{
				SelectInternal(selectionStart, selectionLength);
			}
		}
		else
		{
			_clearHistoryOnTextChanged = false;
			_pendingSelection = (selectionStart, selectionLength);
			ProcessTextInput(text);
			_clearHistoryOnTextChanged = true;
		}
		_suppressCurrentlyTyping = false;
	}
	private void KeyDownBack(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		if (_isPressed)
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
		if (_isPressed)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetUpResult(text, selectionStart, selectionLength, shift);
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
		if (_isPressed)
		{
			return;
		}
		if (Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetDownResult(text, selectionStart, selectionLength, shift);
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
		if (_isPressed)
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
		if (_isPressed)
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
		if (_isPressed)
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
		if (_isPressed)
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
		if (_isPressed)
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
	private void SelectInternal(int selectionStart, int selectionLength)
	{
		Select(Math.Min(selectionStart, selectionStart + selectionLength), Math.Abs(selectionLength));
		_selectionEndsAtTheStart = selectionLength < 0; // set here because Select clears it
		_pendingSelection = null;
		UpdateScrolling();
	}

	private void TimerOnTick(object sender, object e)
	{
		_showCaret = !_showCaret;
		UpdateDisplaySelection();
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		e.Handled = true;

		if (_isSkiaTextBox && _isPressed)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var point = e.GetCurrentPoint(displayBlock);
			var index = Math.Max(0, displayBlock.Inlines.GetIndexAt(point.Position, false, true));
			if (_multiTapChunk is { } mtc)
			{
				(int start, int length) chunk;
				if (mtc.tripleTap)
				{
					chunk = (StartOfLine(index), EndOfLine(index) + 1 - StartOfLine(index));
				}
				else
				{
					chunk = FindChunkAt(index, true);
				}

				if (chunk.start < mtc.start)
				{
					var start = mtc.start + mtc.length;
					var end = chunk.start;
					SelectInternal(start, end - start);
				}
				else if (chunk.start + chunk.length >= mtc.start + mtc.length)
				{
					var start = mtc.start;
					var end = chunk.start + chunk.length;
					SelectInternal(start, end - start);
				}
			}
			else
			{
				var selectionInternalStart = _selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
				SelectInternal(selectionInternalStart, index - selectionInternalStart);
			}
		}
	}

	// TODO: remove this context menu when TextCommandBarFlyout is implemented
	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);
		e.Handled = true;

		if (_isSkiaTextBox)
		{
			if (_contextMenu is null)
			{
				_contextMenu = new MenuFlyout();
				_contextMenu.Opened += (_, _) => UpdateDisplaySelection();

				_flyoutItems.Add(ContextMenuItem.Cut, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_CUT"), Command = new StandardUICommand(StandardUICommandKind.Cut) { Command = new TextBoxCommand(CutSelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Copy, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_COPY"), Command = new StandardUICommand(StandardUICommandKind.Copy) { Command = new TextBoxCommand(CopySelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Paste, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_PASTE"), Command = new StandardUICommand(StandardUICommandKind.Paste) { Command = new TextBoxCommand(PasteFromClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Undo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_UNDO"), Command = new StandardUICommand(StandardUICommandKind.Undo) { Command = new TextBoxCommand(Undo) } });
				_flyoutItems.Add(ContextMenuItem.Redo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_REDO"), Command = new StandardUICommand(StandardUICommandKind.Redo) { Command = new TextBoxCommand(Redo) } });
				_flyoutItems.Add(ContextMenuItem.SelectAll, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_SELECT_ALL"), Command = new StandardUICommand(StandardUICommandKind.SelectAll) { Command = new TextBoxCommand(SelectAll) } });
			}

			_contextMenu.Items.Clear();

			if (_selection.length == 0)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Paste]);
				if (CanUndo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Undo]);
				}
				if (CanRedo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Redo]);
				}
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.SelectAll]);
			}
			else
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Cut]);
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Copy]);
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Paste]);
				if (CanUndo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Undo]);
				}
				if (CanRedo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Redo]);
				}
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.SelectAll]);
			}

			_contextMenu.ShowAt(this, e.GetPosition(this));
		}
	}

	private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
	{
		var currentId = down.PointerId;
		var currentTs = down.Timestamp;
		var currentPosition = down.Position;

		return previousTap.id == currentId
			&& currentTs - previousTap.ts <= GestureRecognizer.MultiTapMaxDelayTicks
			&& !GestureRecognizer.Gesture.IsOutOfTapRange(previousTap.position, currentPosition);
	}

	partial void OnPointerPressedPartial(PointerRoutedEventArgs args)
	{
		TrySetCurrentlyTyping(false);
		if (_isSkiaTextBox
			&& args.GetCurrentPoint(null) is var currentPoint
			&& (!currentPoint.Properties.IsRightButtonPressed || SelectionLength == 0))
		{
			if (currentPoint.Properties.IsLeftButtonPressed
				&& _lastPointerDown.point is { } p
				&& IsMultiTapGesture((p.PointerId, p.Timestamp, p.Position), currentPoint))
			{
				// multiple left presses

				var displayBlock = TextBoxView.DisplayBlock;
				var index = Math.Max(0, displayBlock.Inlines.GetIndexAt(args.GetCurrentPoint(displayBlock).Position, false, true));

				if (_lastPointerDown.repeatedPresses == 1)
				{
					// triple tap

					var startOfLine = StartOfLine(index);
					Select(startOfLine, EndOfLine(index) + 1 - startOfLine);
					_multiTapChunk = (SelectionStart, SelectionLength, true);
					_lastPointerDown = (currentPoint, 2);
				}
				else // _lastPointerDown.repeatedPresses == 0 or 2
				{
					// double tap
					var chunk = FindChunkAt(index, true);
					Select(chunk.start, chunk.length);
					_multiTapChunk = (chunk.start, chunk.length, false);
					_lastPointerDown = (currentPoint, 1);
				}
			}
			else
			{
				// single click
				var displayBlock = TextBoxView.DisplayBlock;
				var index = Math.Max(0, displayBlock.Inlines.GetIndexAt(args.GetCurrentPoint(displayBlock).Position, true, true));
				Select(index, 0);
				_lastPointerDown = (currentPoint, 0);
			}

			_isPressed = currentPoint.Properties.IsLeftButtonPressed;
		}
	}

	partial void OnPointerReleasedPartial(PointerRoutedEventArgs args)
	{
		_isPressed = false;
		_multiTapChunk = null;
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;
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
		global::System.Diagnostics.Debug.Assert(lines.Count > 0);

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
	/// GetUpResult and GetDownResult care about the second kind of lines.
	/// </summary>
	private int GetUpResult(string text, int selectionStart, int selectionLength, bool shift)
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

		if (shift && endLineIndex == 0)
		{
			return 0; // first line, goes to the beginning
		}

		var newLineIndex = selectionLength < 0 || shift ? Math.Max(0, endLineIndex - 1) : Math.Max(0, startLineIndex - 1);

		var rect = DisplayBlockInlines.GetRectForIndex(selectionStart + selectionLength);
		var x = shift && selectionLength > 0 ? rect.Right : rect.Left;
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

	private int GetDownResult(string text, int selectionStart, int selectionLength, bool shift)
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

		if (!shift && (startLineIndex == lines.Count - 1 || endLineIndex == lines.Count - 1))
		{
			return text.Length; // last line, goes to the end
		}

		var newLineIndex = selectionLength > 0 || shift ? Math.Min(lines.Count, endLineIndex + 1) : Math.Min(lines.Count, startLineIndex + 1);

		var rect = DisplayBlockInlines.GetRectForIndex(selectionStart + selectionLength);
		var x = shift && selectionLength > 0 ? rect.Right : rect.Left;
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

		return (i, 0);
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
		}
	}

	partial void OnFocusStateChangedPartial2(FocusState focusState)
	{
		if (_isSkiaTextBox)
		{
			// this is needed so that we UpdateScrolling after the button appears/disappears.
			UpdateLayout();
			// Another round because a collapsed DeleteButton gets measured on the subsequent layout cycle.
			_contentElement?.InvalidateMeasure();
			UpdateLayout();
			UpdateScrolling();
		}
	}

	partial void PasteFromClipboardPartial(string clipboardText, int selectionStart, int selectionLength, string newText)
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

			if (Text == newText)
			{
				// OnTextChanged won't fire, so we immediately change the selection
				Select(selectionStart + clipboardText.Length, 0);
			}
			else
			{
				_pendingSelection = (selectionStart + clipboardText.Length, 0);
			}
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
			_history.Add(new HistoryRecord(SentinelAction.Instance, _selection.start, _selection.length, _selectionEndsAtTheStart));
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
		_history.Add(new HistoryRecord(action, _selection.start, _selection.length, _selectionEndsAtTheStart));
		UpdateCanUndoRedo();
	}

	public void Undo()
	{
		if (!_isSkiaTextBox)
		{
			return;
		}

		TrySetCurrentlyTyping(false);
		if (_historyIndex == 0 || _isPressed)
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
				global::System.Diagnostics.Debug.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
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

		if (_historyIndex == _history.Count - 1 || _isPressed)
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
				global::System.Diagnostics.Debug.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
				break;
		}
		_clearHistoryOnTextChanged = true;
		UpdateCanUndoRedo();
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
}
