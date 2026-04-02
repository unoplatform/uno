#if __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Uno.UI.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Skia-specific rendering and editing implementation for RichEditBox.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native Windows RichEdit control (ITextServices2) for rendering and editing.
	/// Uno implements rendering using the same ParsedText/Skia text infrastructure as RichTextBlock,
	/// with editing, caret, and selection managed in managed code. Keyboard input is handled via OnKeyDown using args.UnicodeKey.
	/// The internal display uses a TextBlock for single-format text display (initial implementation),
	/// with plans to extend to full multi-format rendering.
	/// </remarks>
	public partial class RichEditBox
	{
		private RichEditTextDocument _document;
		private ScrollViewer _contentElement;
		private TextBlock _displayBlock;
		private FrameworkElement _placeholderElement;
		private FrameworkElement _borderElement;
		private bool _isFocused;
		private int _caretPosition;
		private int _selectionAnchor; // The fixed end of the selection
		private DispatcherTimer _caretTimer;
		private bool _caretVisible = true;

		partial void InitializeSkia()
		{
			_document = new RichEditTextDocument(this);
			_document.ContentChanged += OnDocumentContentChanged;

			_caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
			_caretTimer.Tick += (_, _) =>
			{
				_caretVisible = !_caretVisible;
				InvalidateDisplayBlock();
			};

			IsTabStop = true;
			AllowFocusOnInteraction = true;
		}

		private partial RichEditTextDocument GetDocument() => _document;

		private partial bool IsDocumentEmpty() => _document.TextLength == 0;

		partial void OnApplyTemplateSkia()
		{
			_contentElement = GetTemplateChild(ContentElementPartName) as ScrollViewer;
			_placeholderElement = GetTemplateChild(PlaceholderTextPartName) as FrameworkElement;
			_borderElement = GetTemplateChild(BorderElementPartName) as FrameworkElement;

			if (_contentElement != null)
			{
				_contentElement.SetProtectedCursor(
					Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.IBeam));

				// Create the display TextBlock for rendering
				_displayBlock = new TextBlock
				{
					MinWidth = TextBlock.CaretThickness,
					Style = null,
					TextWrapping = this.TextWrapping,
					TextAlignment = this.TextAlignment,
				};

				_contentElement.Content = _displayBlock;

				SyncDisplayFromDocument();
			}
		}

		partial void OnTextWrappingChangedPartial()
		{
			if (_displayBlock != null)
			{
				_displayBlock.TextWrapping = TextWrapping;
			}
		}

		partial void OnTextAlignmentChangedPartial()
		{
			if (_displayBlock != null)
			{
				_displayBlock.TextAlignment = TextAlignment;
			}
		}

		partial void OnGotFocusSkia()
		{
			_isFocused = true;
			_caretVisible = true;
			_caretTimer.Start();
			UpdateCaretAndSelection();
			UpdatePlaceholderVisibility();
		}

		partial void OnLostFocusSkia()
		{
			_isFocused = false;
			_caretTimer.Stop();
			_caretVisible = false;

			if (_displayBlock != null)
			{
				_displayBlock.RenderCaret = null;
				_displayBlock.RenderSelection = false;
			}

			UpdatePlaceholderVisibility();
		}

		// ===== Keyboard input =====

		// MUX Reference: RichEditBox_Partial.cpp - OnKeyDown
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			if (args.Handled)
			{
				return;
			}

			var ctrl = args.KeyboardModifiers.HasFlag(global::Windows.System.VirtualKeyModifiers.Control);
			var shift = args.KeyboardModifiers.HasFlag(global::Windows.System.VirtualKeyModifiers.Shift);

			// Command keys: always return from this switch, never break
			switch (args.Key)
			{
				case VirtualKey.Z when ctrl:
					_document.Undo();
					SyncDisplayFromDocument();
					args.Handled = true;
					return;

				case VirtualKey.Y when ctrl:
					_document.Redo();
					SyncDisplayFromDocument();
					args.Handled = true;
					return;

				case VirtualKey.X when ctrl:
					CutSelectionToClipboard();
					args.Handled = true;
					return;

				case VirtualKey.V when ctrl:
					PasteFromClipboard();
					args.Handled = true;
					return;

				case VirtualKey.C when ctrl:
					CopySelectionToClipboard();
					args.Handled = true;
					return;

				case VirtualKey.B when ctrl:
					if (!HasDisabledAccelerator(DisabledFormattingAccelerators.Bold))
					{
						ToggleBold();
						args.Handled = true;
					}
					return;

				case VirtualKey.I when ctrl:
					if (!HasDisabledAccelerator(DisabledFormattingAccelerators.Italic))
					{
						ToggleItalic();
						args.Handled = true;
					}
					return;

				case VirtualKey.U when ctrl:
					if (!HasDisabledAccelerator(DisabledFormattingAccelerators.Underline))
					{
						ToggleUnderline();
						args.Handled = true;
					}
					return;

				case VirtualKey.LeftShift:
				case VirtualKey.RightShift:
				case VirtualKey.Shift:
				case VirtualKey.Control:
				case VirtualKey.LeftControl:
				case VirtualKey.RightControl:
					// No-op for modifier keys.
					return;
			}

			// Navigation and text input
			switch (args.Key)
			{
				case VirtualKey.Left:
					HandleArrowKey(-1, shift, ctrl);
					args.Handled = true;
					break;

				case VirtualKey.Right:
					HandleArrowKey(1, shift, ctrl);
					args.Handled = true;
					break;

				case VirtualKey.Up:
					HandleVerticalNavigation(-1, shift);
					args.Handled = true;
					break;

				case VirtualKey.Down:
					HandleVerticalNavigation(1, shift);
					args.Handled = true;
					break;

				case VirtualKey.Home:
					HandleHomeEnd(isEnd: false, shift, ctrl);
					args.Handled = true;
					break;

				case VirtualKey.End:
					HandleHomeEnd(isEnd: true, shift, ctrl);
					args.Handled = true;
					break;

				case VirtualKey.Back:
					HandleBackspace(ctrl);
					args.Handled = true;
					break;

				case VirtualKey.Delete:
					HandleDelete(ctrl);
					args.Handled = true;
					break;

				case VirtualKey.A when ctrl:
					SelectAll();
					args.Handled = true;
					break;

				default:
					// Character input via UnicodeKey (OnCharacterReceived is NotImplemented on Skia)
					var isEnterKey = args.UnicodeKey is '\r' or '\n' || args.Key == VirtualKey.Enter;
					if (!IsReadOnly && args.UnicodeKey is { } key && (!isEnterKey || AcceptsReturn))
					{
						if (key is '\n')
						{
							key = '\r'; // TOM convention: paragraph separator is \r
						}

						var text = key.ToString();

						// Apply character casing
						text = CharacterCasing switch
						{
							CharacterCasing.Upper => text.ToUpperInvariant(),
							CharacterCasing.Lower => text.ToLowerInvariant(),
							_ => text
						};

						// Check max length
						if (MaxLength > 0 && _document.TextLength - GetSelectionLength() + text.Length > MaxLength)
						{
							return;
						}

						InsertTextAtCaret(text);
						args.Handled = true;
					}
					return;
			}
		}

		// ===== Pointer input =====

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (!e.Handled)
			{
				Focus(FocusState.Pointer);

				var position = e.GetCurrentPoint(this).Position;
				var index = GetCharIndexFromPoint(position);
				var shift = e.KeyModifiers.HasFlag(global::Windows.System.VirtualKeyModifiers.Shift);

				if (shift)
				{
					_caretPosition = index;
				}
				else
				{
					_caretPosition = index;
					_selectionAnchor = index;
				}

				UpdateCaretAndSelection();
				CapturePointer(e.Pointer);
				e.Handled = true;
			}
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs e)
		{
			base.OnPointerMoved(e);

			if (e.Pointer.IsInContact)
			{
				var position = e.GetCurrentPoint(this).Position;
				var index = GetCharIndexFromPoint(position);
				_caretPosition = index;
				UpdateCaretAndSelection();
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			base.OnPointerReleased(e);
			ReleasePointerCapture(e.Pointer);
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);

			// Select word at position
			var position = e.GetPosition(this);
			var index = GetCharIndexFromPoint(position);
			var text = _document.TextBuffer;

			if (text.Length == 0)
			{
				return;
			}

			index = Math.Min(index, text.Length - 1);

			// Find word boundaries
			var start = index;
			while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
			{
				start--;
			}

			var end = index;
			while (end < text.Length && !char.IsWhiteSpace(text[end]))
			{
				end++;
			}

			_selectionAnchor = start;
			_caretPosition = end;
			UpdateCaretAndSelection();
		}

		// ===== Document operations =====

		private void InsertTextAtCaret(string text)
		{
			if (IsReadOnly)
			{
				return;
			}

			RaiseTextChanging();

			var selStart = Math.Min(_caretPosition, _selectionAnchor);
			var selEnd = Math.Max(_caretPosition, _selectionAnchor);
			var selLength = selEnd - selStart;

			if (selLength > 0)
			{
				_document.ReplaceText(selStart, selLength, text);
			}
			else
			{
				_document.InsertText(selStart, text);
			}

			_caretPosition = selStart + text.Length;
			_selectionAnchor = _caretPosition;

			SyncDisplayFromDocument();
			RaiseTextChanged();
			ResetCaretBlink();
		}

		private void HandleBackspace(bool ctrl)
		{
			if (IsReadOnly)
			{
				return;
			}

			var selStart = Math.Min(_caretPosition, _selectionAnchor);
			var selEnd = Math.Max(_caretPosition, _selectionAnchor);

			if (selStart != selEnd)
			{
				// Delete selection
				RaiseTextChanging();
				_document.DeleteText(selStart, selEnd - selStart);
				_caretPosition = selStart;
				_selectionAnchor = selStart;
			}
			else if (selStart > 0)
			{
				RaiseTextChanging();
				if (ctrl)
				{
					// Delete word backwards
					var wordStart = GetWordStart(selStart);
					_document.DeleteText(wordStart, selStart - wordStart);
					_caretPosition = wordStart;
					_selectionAnchor = wordStart;
				}
				else
				{
					_document.DeleteText(selStart - 1, 1);
					_caretPosition = selStart - 1;
					_selectionAnchor = _caretPosition;
				}
			}

			SyncDisplayFromDocument();
			RaiseTextChanged();
			ResetCaretBlink();
		}

		private void HandleDelete(bool ctrl)
		{
			if (IsReadOnly)
			{
				return;
			}

			var selStart = Math.Min(_caretPosition, _selectionAnchor);
			var selEnd = Math.Max(_caretPosition, _selectionAnchor);

			if (selStart != selEnd)
			{
				RaiseTextChanging();
				_document.DeleteText(selStart, selEnd - selStart);
				_caretPosition = selStart;
				_selectionAnchor = selStart;
			}
			else if (selStart < _document.TextLength)
			{
				RaiseTextChanging();
				if (ctrl)
				{
					var wordEnd = GetWordEnd(selStart);
					_document.DeleteText(selStart, wordEnd - selStart);
				}
				else
				{
					_document.DeleteText(selStart, 1);
				}
			}

			SyncDisplayFromDocument();
			RaiseTextChanged();
			ResetCaretBlink();
		}

		private void HandleArrowKey(int direction, bool shift, bool ctrl)
		{
			var position = _caretPosition;

			if (ctrl)
			{
				// Move by word
				position = direction > 0 ? GetWordEnd(position) : GetWordStart(position);
			}
			else
			{
				// Move by character
				position = Math.Max(0, Math.Min(position + direction, _document.TextLength));
			}

			_caretPosition = position;
			if (!shift)
			{
				_selectionAnchor = _caretPosition;
			}

			UpdateCaretAndSelection();
			RaiseSelectionChanged();
			ResetCaretBlink();
		}

		private void HandleVerticalNavigation(int direction, bool shift)
		{
			// Simplified: move to adjacent paragraph
			var text = _document.TextBuffer;
			var position = _caretPosition;

			if (direction > 0)
			{
				// Move down - find next line break
				while (position < text.Length && text[position] != '\r')
				{
					position++;
				}

				if (position < text.Length)
				{
					position++; // Skip \r
				}
			}
			else
			{
				// Move up - find previous line break
				if (position > 0)
				{
					position--;
				}

				while (position > 0 && text[position - 1] != '\r')
				{
					position--;
				}

				// Try to go to previous line start
				if (position > 0)
				{
					position--;
					while (position > 0 && text[position - 1] != '\r')
					{
						position--;
					}
				}
			}

			_caretPosition = position;
			if (!shift)
			{
				_selectionAnchor = _caretPosition;
			}

			UpdateCaretAndSelection();
			RaiseSelectionChanged();
			ResetCaretBlink();
		}

		private void HandleHomeEnd(bool isEnd, bool shift, bool ctrl)
		{
			if (ctrl)
			{
				// Move to document start/end
				_caretPosition = isEnd ? _document.TextLength : 0;
			}
			else
			{
				// Move to line start/end
				var bounds = _document.GetParagraphBounds(_caretPosition);
				_caretPosition = isEnd ? bounds.end : bounds.start;
			}

			if (!shift)
			{
				_selectionAnchor = _caretPosition;
			}

			UpdateCaretAndSelection();
			RaiseSelectionChanged();
			ResetCaretBlink();
		}

		// ===== Formatting operations =====

		private void ToggleBold()
		{
			if (!HasSelection())
			{
				return;
			}

			var start = Math.Min(_caretPosition, _selectionAnchor);
			var end = Math.Max(_caretPosition, _selectionAnchor);
			var currentFormat = _document.GetFormatForRange(start, end);
			var newFormat = (TextCharacterFormat)currentFormat.GetClone();
			newFormat.Bold = currentFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
			_document.AddFormatSpan(start, end, newFormat);
			SyncDisplayFromDocument();
		}

		private void ToggleItalic()
		{
			if (!HasSelection())
			{
				return;
			}

			var start = Math.Min(_caretPosition, _selectionAnchor);
			var end = Math.Max(_caretPosition, _selectionAnchor);
			var currentFormat = _document.GetFormatForRange(start, end);
			var newFormat = (TextCharacterFormat)currentFormat.GetClone();
			newFormat.Italic = currentFormat.Italic == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
			_document.AddFormatSpan(start, end, newFormat);
			SyncDisplayFromDocument();
		}

		private void ToggleUnderline()
		{
			if (!HasSelection())
			{
				return;
			}

			var start = Math.Min(_caretPosition, _selectionAnchor);
			var end = Math.Max(_caretPosition, _selectionAnchor);
			var currentFormat = _document.GetFormatForRange(start, end);
			var newFormat = (TextCharacterFormat)currentFormat.GetClone();
			newFormat.Underline = currentFormat.Underline == UnderlineType.Single
				? UnderlineType.None : UnderlineType.Single;
			_document.AddFormatSpan(start, end, newFormat);
			SyncDisplayFromDocument();
		}

		private bool HasDisabledAccelerator(DisabledFormattingAccelerators accelerator)
		{
			return (DisabledFormattingAccelerators & accelerator) != 0;
		}

		// ===== Selection and clipboard =====

		private void SelectAll()
		{
			_selectionAnchor = 0;
			_caretPosition = _document.TextLength;
			UpdateCaretAndSelection();
			RaiseSelectionChanged();
		}

		private void CopySelectionToClipboard()
		{
			if (!HasSelection())
			{
				return;
			}

			var start = Math.Min(_caretPosition, _selectionAnchor);
			var end = Math.Max(_caretPosition, _selectionAnchor);
			var text = _document.GetTextInRange(start, end);

			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
		}

		private void CutSelectionToClipboard()
		{
			if (!HasSelection() || IsReadOnly)
			{
				return;
			}

			CopySelectionToClipboard();

			var start = Math.Min(_caretPosition, _selectionAnchor);
			var end = Math.Max(_caretPosition, _selectionAnchor);
			RaiseTextChanging();
			_document.DeleteText(start, end - start);
			_caretPosition = start;
			_selectionAnchor = start;
			SyncDisplayFromDocument();
			RaiseTextChanged();
		}

		private async void PasteFromClipboard()
		{
			if (IsReadOnly)
			{
				return;
			}

			try
			{
				var content = Clipboard.GetContent();
				if (content?.Contains(StandardDataFormats.Text) == true)
				{
					var clipboardText = await content.GetTextAsync();
					if (!string.IsNullOrEmpty(clipboardText))
					{
						// Normalize line endings
						clipboardText = clipboardText.Replace("\r\n", "\r").Replace("\n", "\r");

						if (MaxLength > 0)
						{
							var available = MaxLength - _document.TextLength + GetSelectionLength();
							if (clipboardText.Length > available)
							{
								clipboardText = clipboardText.Substring(0, Math.Max(0, available));
							}
						}

						InsertTextAtCaret(clipboardText);
					}
				}
			}
			catch
			{
				// Clipboard access may fail
			}
		}

		// ===== Display synchronization =====

		/// <summary>
		/// Synchronizes the visual TextBlock display from the document model.
		/// </summary>
		/// <remarks>
		/// Diverges from WinUI: WinUI renders through the native RichEdit control's ITextServices2::TxDraw.
		/// Uno rebuilds the TextBlock's Inlines collection from the document model's format spans.
		/// For the initial implementation, uses a simple TextBlock.Text approach.
		/// Rich formatting is applied via Inlines when format spans are present.
		/// </remarks>
		private void SyncDisplayFromDocument()
		{
			if (_displayBlock == null)
			{
				return;
			}

			var text = _document.TextBuffer;
			var spans = _document.FormatSpans;

			if (spans.Count == 0)
			{
				// Simple case: no formatting, use plain text
				// Replace \r with \n for TextBlock display
				_displayBlock.Text = text.Replace('\r', '\n');
			}
			else
			{
				// Build Inlines from format spans
				_displayBlock.Inlines.Clear();

				int currentPos = 0;
				foreach (var span in spans.OrderBy(s => s.Start))
				{
					// Add unformatted text before this span
					if (span.Start > currentPos)
					{
						var plainText = text.Substring(currentPos, span.Start - currentPos).Replace('\r', '\n');
						_displayBlock.Inlines.Add(new Run { Text = plainText });
					}

					// Add formatted run
					var formattedText = text.Substring(span.Start, Math.Min(span.End - span.Start, text.Length - span.Start)).Replace('\r', '\n');
					var run = new Run { Text = formattedText };
					ApplyFormatToRun(run, span.Format);
					_displayBlock.Inlines.Add(run);

					currentPos = span.End;
				}

				// Add remaining unformatted text
				if (currentPos < text.Length)
				{
					var remainingText = text.Substring(currentPos).Replace('\r', '\n');
					_displayBlock.Inlines.Add(new Run { Text = remainingText });
				}
			}

			UpdateCaretAndSelection();
			UpdatePlaceholderVisibility();

			// Sync selection state to the document model
			var selStart = Math.Min(_caretPosition, _selectionAnchor);
			var selEnd = Math.Max(_caretPosition, _selectionAnchor);
			_document.Selection.SetRange(selStart, selEnd);
		}

		private void ApplyFormatToRun(Run run, TextCharacterFormat format)
		{
			if (format.Bold == FormatEffect.On)
			{
				run.FontWeight = global::Windows.UI.Text.FontWeights.Bold;
			}

			if (format.Italic == FormatEffect.On)
			{
				run.FontStyle = global::Windows.UI.Text.FontStyle.Italic;
			}

			if (format.Underline == UnderlineType.Single)
			{
				run.TextDecorations = global::Windows.UI.Text.TextDecorations.Underline;
			}
			else if (format.Strikethrough == FormatEffect.On)
			{
				run.TextDecorations = global::Windows.UI.Text.TextDecorations.Strikethrough;
			}

			if (format.Size > 0)
			{
				run.FontSize = format.Size;
			}

			if (!string.IsNullOrEmpty(format.Name))
			{
				run.FontFamily = new FontFamily(format.Name);
			}

			if (format.ForegroundColor != default && format.ForegroundColor != Colors.Black)
			{
				run.Foreground = new SolidColorBrush(format.ForegroundColor);
			}

			if (format.Weight > 0)
			{
				run.FontWeight = new global::Windows.UI.Text.FontWeight((ushort)format.Weight);
			}
		}

		/// <summary>
		/// Updates caret and selection rendering on the display TextBlock.
		/// </summary>
		private void UpdateCaretAndSelection()
		{
			if (_displayBlock == null)
			{
				return;
			}

			// Use document length as the authoritative length.
			// When Inlines are used, TextBlock.Text may be null even though content exists.
			var docLength = _document.TextLength;
			var caretIndex = Math.Max(0, Math.Min(_caretPosition, docLength));

			if (_isFocused)
			{
				// Show caret
				if (_caretVisible)
				{
					var brush = (SelectionHighlightColor ?? new SolidColorBrush(Colors.Black))
						.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor());
					_displayBlock.RenderCaret = (caretIndex, brush);
				}
				else
				{
					_displayBlock.RenderCaret = null;
				}

				// Show selection
				if (HasSelection())
				{
					var selStart = Math.Min(_caretPosition, _selectionAnchor);
					var selEnd = Math.Max(_caretPosition, _selectionAnchor);
					selStart = Math.Max(0, Math.Min(selStart, docLength));
					selEnd = Math.Max(0, Math.Min(selEnd, docLength));
					_displayBlock.Selection = new TextBlock.Range(selStart, selEnd);
					_displayBlock.RenderSelection = true;
				}
				else
				{
					_displayBlock.RenderSelection = false;
				}
			}
			else
			{
				_displayBlock.RenderCaret = null;
				_displayBlock.RenderSelection = false;
			}
		}

		private void InvalidateDisplayBlock()
		{
			UpdateCaretAndSelection();
		}

		private void OnDocumentContentChanged()
		{
			SyncDisplayFromDocument();
		}

		// ===== Helpers =====

		private bool HasSelection()
		{
			return _caretPosition != _selectionAnchor;
		}

		private int GetSelectionLength()
		{
			return Math.Abs(_caretPosition - _selectionAnchor);
		}

		private void ResetCaretBlink()
		{
			_caretVisible = true;
			_caretTimer.Stop();
			_caretTimer.Start();
			UpdateCaretAndSelection();
		}

		private int GetCharIndexFromPoint(Point point)
		{
			if (_displayBlock != null)
			{
				var parsedText = _displayBlock.ParsedText;
				if (parsedText != null)
				{
					// Adjust for padding/margin
					var adjustedPoint = new Point(
						point.X - Padding.Left,
						point.Y - Padding.Top);

					var index = parsedText.GetIndexAt(adjustedPoint, false, true);
					return Math.Max(0, Math.Min(index, _document.TextLength));
				}
			}

			return 0;
		}

		private int GetWordStart(int position)
		{
			var text = _document.TextBuffer;
			if (position <= 0)
			{
				return 0;
			}

			var pos = Math.Min(position - 1, text.Length - 1);
			while (pos > 0 && char.IsWhiteSpace(text[pos]))
			{
				pos--;
			}

			while (pos > 0 && !char.IsWhiteSpace(text[pos - 1]))
			{
				pos--;
			}

			return pos;
		}

		private int GetWordEnd(int position)
		{
			var text = _document.TextBuffer;
			if (position >= text.Length)
			{
				return text.Length;
			}

			var pos = position;
			while (pos < text.Length && !char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			while (pos < text.Length && char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			return pos;
		}
	}
}
#endif
