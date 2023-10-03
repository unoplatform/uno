using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls;

public partial class TextBox
{
	private TextBoxView _textBoxView;
	private (int start, int length) _selection;
	private bool _selectionEndsAtTheStart;
	private bool _showCaret = true;
	private readonly DispatcherTimer _timer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(0.5)
	};

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
			UpdateLayout();
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
		if (TextBoxView?.DisplayBlock.Inlines is { } inlines)
		{
			inlines.Selection = (0, SelectionStart, 0, SelectionStart + SelectionLength);
			inlines.RenderSelectionAndCaret = FocusState != FocusState.Unfocused;
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
						selectionLength -= 1;
					}
					else
					{
						if (selectionLength != 0)
						{
							selectionStart = Math.Min(selectionStart, selectionStart + selectionLength);
						}
						else
						{
							selectionStart -= 1;
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
						selectionLength += 1;
					}
					else
					{
						if (selectionLength != 0)
						{
							selectionStart = Math.Max(selectionStart, selectionStart + selectionLength);
						}
						else
						{
							selectionStart += 1;
						}
						selectionLength = 0;
					}
				}
				break;
			case VirtualKey.Home:
				args.Handled = true;
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
				args.Handled = true;
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
				args.Handled = true;
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
					text = text[..(selectionStart - 1)] + text[selectionStart..];
					selectionStart -= 1;
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
					text = text[..selectionStart] + text[(selectionStart + 1)..];
					selectionStart += 1;
				}
				break;
			case VirtualKey.A when args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control):
				args.Handled = true;
				selectionStart = 0;
				selectionLength = text.Length;
				break;
			default:
				var key = (int)args.Key;
				if (!IsReadOnly && key is >= 'A' and <= 'Z' || args.Key == VirtualKey.Space)
				{
					args.Handled = true;
					var c = args.Key == VirtualKey.Space ? ' ' : shift ? (char)key : char.ToLower((char)key);


					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);

					text = text[..start] + c + text[end..];
					selectionStart = start + 1;
					selectionLength = 0;
				}
				break;
		}

		Text = text;

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
		var displayBlock = TextBoxView.DisplayBlock;
		var point = e.GetCurrentPoint(displayBlock);
		var index = displayBlock.Inlines.GetIndexForTextBlock(point.Position - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
		if (point.Properties.IsLeftButtonPressed)
		{
			var selectionInternalStart = _selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
			SelectInternal(selectionInternalStart, index - selectionInternalStart);
		}
	}

	partial void OnPointerPressedNative(PointerRoutedEventArgs e)
	{
		var displayBlock = TextBoxView.DisplayBlock;
		var index = displayBlock.Inlines.GetIndexForTextBlock(e.GetCurrentPoint(displayBlock).Position - new Point(displayBlock.Padding.Left, displayBlock.Padding.Top));
		Select(index, 0);
	}
}
