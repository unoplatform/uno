using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls;

public partial class TextBox
{
	private const ulong MultiTapMaxDelayTicks = TimeSpan.TicksPerMillisecond / 20;

	private TextBoxView _textBoxView;
	private (int start, int length) _selection;
	private bool _selectionEndsAtTheStart;
	private bool _showCaret = true;
	private bool _resetSelectionOnChange = true;
	private (PointerPoint point, int repeatedPresses) _lastPointerDown; // point is null before first press
	private bool _isPressed;
	private (int start, int length)? _multiTapChunk;
	private (int hashCode, List<(int start, int length)> chunks) _cachedChunks = (-1, new());
	private readonly DispatcherTimer _timer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(0.5)
	};

	private MenuFlyout _contextMenu;
	private readonly Dictionary<string, MenuFlyoutItem> _flyoutItems = new();

	internal TextBoxView TextBoxView => _textBoxView;

	internal ContentControl ContentElement => _contentElement;

	partial void OnForegroundColorChangedPartial(Brush newValue) => TextBoxView?.OnForegroundChanged(newValue);

	partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush) => TextBoxView?.OnSelectionHighlightColorChanged(brush);

	partial void UpdateFontPartial() => TextBoxView?.OnFontFamilyChanged(FontFamily);

	partial void OnMaxLengthChangedPartial(int newValue) => TextBoxView?.UpdateMaxLength();

	partial void OnFlowDirectionChangedPartial()
	{
		TextBoxView?.SetFlowDirectionAndTextAlignment();
	}

	partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
	{
		TextBoxView?.SetFlowDirectionAndTextAlignment();
	}

	private void UpdateTextBoxView()
	{
		_textBoxView ??= new TextBoxView(this);
		if (ContentElement != null && ContentElement.Content != TextBoxView.DisplayBlock)
		{
			ContentElement.Content = TextBoxView.DisplayBlock;
			TextBoxView.SetTextNative(Text);
		}
	}

	partial void OnFocusStateChangedPartial(FocusState focusState)
	{
		if (FeatureConfiguration.TextBox.UseOverlayOnSkia)
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
				_showCaret = false;
				_timer.Stop();
			}
			UpdateDisplaySelection();
		}
	}

	partial void SelectPartial(int start, int length)
	{
		_selectionEndsAtTheStart = false;
		_selection = (start, length);
		if (FeatureConfiguration.TextBox.UseOverlayOnSkia)
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
		get => FeatureConfiguration.TextBox.UseOverlayOnSkia ? TextBoxView?.GetSelectionStart() ?? 0 : _selection.start;
		set => Select(start: value, length: SelectionLength);
	}

	public int SelectionLength
	{
		get => FeatureConfiguration.TextBox.UseOverlayOnSkia ? TextBoxView?.GetSelectionLength() ?? 0 : _selection.length;
		set => Select(SelectionStart, value);
	}

	internal void UpdateDisplaySelection()
	{
		if (!FeatureConfiguration.TextBox.UseOverlayOnSkia && TextBoxView?.DisplayBlock.Inlines is { } inlines)
		{
			inlines.Selection = (0, SelectionStart, 0, SelectionStart + SelectionLength);
			inlines.RenderSelectionAndCaret = FocusState != FocusState.Unfocused || (_contextMenu?.IsOpen ?? false);
			var showCaret = _showCaret && !FeatureConfiguration.TextBox.HideCaret && !IsReadOnly && _selection.length == 0;
			inlines.Caret = (!_selectionEndsAtTheStart, showCaret ? Colors.Black : Colors.Transparent);
			TextBoxView?.DisplayBlock.InvalidateInlines(true);
		}
	}

	private void UpdateScrolling()
	{
		if (!FeatureConfiguration.TextBox.UseOverlayOnSkia && _contentElement is ScrollViewer sv)
		{
			var selectionEnd = _selectionEndsAtTheStart ? _selection.start : _selection.start + _selection.length;

			var horizontalOffset = sv.HorizontalOffset;
			var verticalOffset = sv.VerticalOffset;

			var rect = TextBoxView.DisplayBlock.Inlines.GetRectForTextBlockIndex(selectionEnd);

			var newHorizontalOffset = horizontalOffset.AtMost(rect.Left).AtLeast(rect.Left - sv.ViewportWidth + rect.Height * InlineCollection.CaretThicknessAsRatioOfLineHeight);
			var newVerticalOffset = verticalOffset.AtMost(rect.Top).AtLeast(rect.Top - sv.ViewportWidth);

			sv.ChangeView(newHorizontalOffset, newVerticalOffset, null);
		}
	}

	private partial void OnKeyDownPartial(KeyRoutedEventArgs args)
	{
		if (FeatureConfiguration.TextBox.UseOverlayOnSkia)
		{
			OnKeyDownInternal(args);
			return;
		}

		base.OnKeyDown(args);

		// Note: On windows only keys that are "moving the cursor" are handled
		//		 AND ** only KeyDown ** is handled (not KeyUp)

		// move to possibly-negative selection length format
		var (selectionStart, selectionLength) = _selectionEndsAtTheStart ? (_selection.start + _selection.length, -_selection.length) : (_selection.start, _selection.length);

		var text = Text;
		var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
		var ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control);
		switch (args.Key)
		{
			case VirtualKey.Up:
				if (shift)
				{
					selectionLength = -selectionStart;
				}
				else
				{
					selectionStart = Math.Min(selectionStart, selectionStart + selectionLength);
					selectionLength = 0;
				}
				break;
			case VirtualKey.Down:
				if (selectionStart != text.Length || selectionLength != 0)
				{
					args.Handled = true;
					if (shift)
					{
						selectionLength = text.Length - selectionStart;
					}
					else
					{
						selectionStart = text.Length;
					}
				}
				break;
			case VirtualKey.Left:
				var moveOutLeft = !shift && selectionStart == 0 && selectionLength == 0 || shift && selectionStart + selectionLength == 0;
				if (!moveOutLeft)
				{
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
				break;
			case VirtualKey.Right:
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
				break;
			case VirtualKey.Home:
				args.Handled = selectionLength != 0 || selectionStart > 0;
				if (shift)
				{
					selectionLength = -selectionStart;
				}
				else
				{
					selectionStart = 0;
					selectionLength = 0;
				}
				break;
			case VirtualKey.End:
				args.Handled = selectionLength != 0 || selectionStart < text.Length;
				if (shift)
				{
					selectionLength = text.Length - selectionStart;
				}
				else
				{
					selectionStart = text.Length;
					selectionLength = 0;
				}
				break;
			case VirtualKey.Back when !IsReadOnly:
				if (selectionLength != 0)
				{
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);
					text = text[..start] + text[end..];
					selectionLength = 0;
					selectionStart = start;
				}
				else if (selectionStart != 0)
				{
					var index = ctrl ? FindChunkAt(selectionStart, false).start : selectionStart - 1;
					text = text[..index] + text[selectionStart..];
					selectionStart = index;
				}
				break;
			case VirtualKey.Delete when !IsReadOnly:
				args.Handled = true;
				if (selectionLength != 0)
				{
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);
					text = text[..start] + text[end..];
					selectionLength = 0;
					selectionStart = start;
				}
				else if (selectionStart != text.Length)
				{
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
				}
				break;
			case VirtualKey.A when ctrl:
				args.Handled = true;
				selectionStart = 0;
				selectionLength = text.Length;
				break;
			case VirtualKey.X when ctrl:
				args.Handled = true;
				CutSelectionToClipboard();
				selectionLength = 0;
				text = Text;
				break;
			case VirtualKey.V when ctrl:
				args.Handled = true;
				PasteFromClipboard(); // async so doesn't actually do anything right now
				break;
			case VirtualKey.C when ctrl:
				args.Handled = true;
				CopySelectionToClipboard();
				break;
			default:
				if (!IsReadOnly && args.UnicodeKey is { } c)
				{
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);

					text = text[..start] + c + text[end..];
					selectionStart = start + 1;
					selectionLength = 0;
				}
				break;
		}

		_resetSelectionOnChange = false;
		Text = text;
		_resetSelectionOnChange = true;

		selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart));
		selectionLength = Math.Max(-selectionStart, Math.Min(text.Length - selectionStart, selectionLength));
		SelectInternal(selectionStart, selectionLength);
	}

	/// <summary>
	/// Takes a possibly-negative selection length, indicating a selection that goes backwards.
	/// This makes the calculations a lot more natural.
	/// </summary>
	private void SelectInternal(int selectionStart, int selectionLength)
	{
		Select(Math.Min(selectionStart, selectionStart + selectionLength), Math.Abs(selectionLength));
		_selectionEndsAtTheStart = selectionLength < 0; // set here because Select clears it
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

		if (!FeatureConfiguration.TextBox.UseOverlayOnSkia && _isPressed)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var point = e.GetCurrentPoint(displayBlock);
			var index = displayBlock.Inlines.GetIndexForTextBlock(point.Position - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
			if (_multiTapChunk is { } tt)
			{
				var chunk = FindChunkAt(index, true);
				if (chunk.start < tt.start)
				{
					var start = tt.start + tt.length;
					var end = chunk.start;
					SelectInternal(start, end - start);
				}
				else if (chunk.start + chunk.length >= tt.start + tt.length)
				{
					var start = tt.start;
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

	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);
		e.Handled = true;

		if (!FeatureConfiguration.TextBox.UseOverlayOnSkia)
		{
			if (_contextMenu is null)
			{
				_contextMenu = new MenuFlyout();
				_contextMenu.Opened += (_, _) => UpdateDisplaySelection();

				_flyoutItems.Add("Cut", new MenuFlyoutItem { Text = "Cut", Command = new TextBoxCommand(CutSelectionToClipboard), Icon = new SymbolIcon(Symbol.Cut) });
				_flyoutItems.Add("Copy", new MenuFlyoutItem { Text = "Copy", Command = new TextBoxCommand(CopySelectionToClipboard), Icon = new SymbolIcon(Symbol.Copy) });
				_flyoutItems.Add("Paste", new MenuFlyoutItem { Text = "Paste", Command = new TextBoxCommand(PasteFromClipboard), Icon = new SymbolIcon(Symbol.Paste) });
				// undo/redo
				_flyoutItems.Add("Select All", new MenuFlyoutItem { Text = "Select All", Command = new TextBoxCommand(SelectAll), Icon = new SymbolIcon(Symbol.SelectAll) });
			}

			_contextMenu.Items.Clear();

			if (_selection.length == 0)
			{
				_contextMenu.Items.Add(_flyoutItems["Paste"]);
				// undo/redo
				_contextMenu.Items.Add(_flyoutItems["Select All"]);
			}
			else
			{
				_contextMenu.Items.Add(_flyoutItems["Cut"]);
				_contextMenu.Items.Add(_flyoutItems["Copy"]);
				_contextMenu.Items.Add(_flyoutItems["Paste"]);
				// undo/redo
				_contextMenu.Items.Add(_flyoutItems["Select All"]);
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
			&& currentTs - previousTap.ts <= MultiTapMaxDelayTicks
			&& !GestureRecognizer.Gesture.IsOutOfTapRange(previousTap.position, currentPosition);
	}

	partial void OnPointerPressedNative(PointerRoutedEventArgs args)
	{
		Console.WriteLine("Starting Pressed");
		if (!FeatureConfiguration.TextBox.UseOverlayOnSkia && args.GetCurrentPoint(null) is var currentPoint && (!currentPoint.Properties.IsRightButtonPressed || SelectionLength == 0))
		{
			if (currentPoint.Properties.IsLeftButtonPressed && _lastPointerDown.point is { } p && IsMultiTapGesture((p.PointerId, p.Timestamp, p.Position), currentPoint))
			{
				// multiple left presses
				if (_lastPointerDown.repeatedPresses == 1)
				{
					// triple tap
					SelectAll();
					_multiTapChunk = (SelectionStart, SelectionLength);
					_lastPointerDown = (currentPoint, 2);
				}
				else // _lastPointerDown.repeatedPresses == 2
				{
					// double tap
					var displayBlock = TextBoxView.DisplayBlock;
					var index = displayBlock.Inlines.GetIndexForTextBlock(args.GetCurrentPoint(displayBlock).Position - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
					var chunk = FindChunkAt(index, true);
					Select(chunk.start, chunk.length);
					_multiTapChunk = (chunk.start, chunk.length);
					_lastPointerDown = (currentPoint, 1);
				}
			}
			else
			{
				// single click
				var displayBlock = TextBoxView.DisplayBlock;
				var index = displayBlock.Inlines.GetIndexForTextBlock(args.GetCurrentPoint(displayBlock).Position - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
				Select(index, 0);
				_lastPointerDown = (currentPoint, 0);
			}

			_isPressed = currentPoint.Properties.IsLeftButtonPressed;
		}
	}

	partial void OnPointerReleasedNative(PointerRoutedEventArgs args)
	{
		Console.WriteLine("Starting Released");

		_isPressed = false;
		_multiTapChunk = null;
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;

		// var displayBlock = TextBoxView.DisplayBlock;
		// var index = displayBlock.Inlines.GetIndexForTextBlock(args.GetPosition(displayBlock) - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
		// var chunk = FindChunkAt(index, true);
		// Select(chunk.start, chunk.length);
		// _lastPointerDown.wasDoubleTap = true;
	}

	private (int start, int length) FindChunkAt(int index, bool right)
	{
		if (Text.GetHashCode() != _cachedChunks.hashCode)
		{
			GenerateChunks();
		}

		var i = 0;
		foreach (var chunk in _cachedChunks.chunks)
		{
			if (chunk.start < index && chunk.start + chunk.length > index || chunk.start == index && right || chunk.start + chunk.length == index && !right)
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

		// a chunk is possible (continuous letters/numbers or continuous non-letters/non-numbers) then possible whitespace
		var length = text.Length;
		for (var i = 0; i < length;)
		{
			var start = i;
			var c = text[i];
			if (char.IsWhiteSpace(c))
			{
				while (i < length && char.IsWhiteSpace(text[i]))
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
				while (i < length && char.IsWhiteSpace(text[i]))
				{
					i++;
				}
			}
			else
			{
				while (i < length && !char.IsLetterOrDigit(text[i]) && !char.IsWhiteSpace(text[i]))
				{
					i++;
				}
				while (i < length && char.IsWhiteSpace(text[i]))
				{
					i++;
				}
			}

			chunks.Add((start, i - start));
		}
	}

	private sealed class TextBoxCommand : ICommand
	{
		private readonly Action _action;

		public TextBoxCommand(Action action)
		{
			_action = action;
		}

		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter) => _action();

#pragma warning disable 67 // An event was declared but never used in the class in which it was declared.
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67 // An event was declared but never used in the class in which it was declared.
	}
}
