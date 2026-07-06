#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Shared, control-agnostic keyboard navigation and edit logic for the Skia text-editing engine.
	/// It operates purely on a working (text, selection) tuple plus the hosting control's
	/// <see cref="ITextViewEditorHost"/>, so it can be driven by both <see cref="TextBox"/> and
	/// RichEditBox without duplicating the caret/word/line navigation and delete logic.
	/// </summary>
	internal sealed class TextViewEditor
	{
		private readonly ITextViewEditorHost _host;

		public TextViewEditor(ITextViewEditorHost host) => _host = host;

		// Thin shims so the moved method bodies keep referencing the same member names as before.
		private TextBoxView TextBoxView => _host.TextBoxView;
		private string Text => _host.Text;
		private bool HasPointerCapture => _host.HasPointerCapture;
		private float _caretXOffset => _host.CaretXOffset;
		private void TrySetCurrentlyTyping(bool newValue) => _host.TrySetCurrentlyTyping(newValue);

		internal void KeyDownBack(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
		{
			// on Apple platforms it is `option` + `delete` (same location as backspace on PC keyboards) that removes the previous word
			if (DeviceTargetHelper.UsesAppleKeyboardLayout)
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
				var index = ctrl ? TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, false).start : selectionStart - 1;
				text = text[..index] + text[selectionStart..];
				selectionStart = index;

				if (ctrl)
				{
					// typing after ctrl starts a new run, and not a part of the ctrl-backspace run
					_host.CommitReplace(oldText, text, selectionStart);
				}
			}
		}

		internal void KeyDownUpArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
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

		internal void KeyDownDownArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
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

		internal void KeyDownLeftArrow(KeyRoutedEventArgs args, string text, bool shift, bool ctrl, ref int selectionStart, ref int selectionLength)
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
					end = TextBoxView.DisplayBlock.ParsedText.GetWordAt(end, false).start;
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
					selectionStart = ctrl ? TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, false).start : selectionStart - 1;
				}
				else
				{
					selectionStart = Math.Min(selectionStart, selectionStart + selectionLength);
				}
				selectionLength = 0;
			}
		}

		internal void KeyDownRightArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
		{
			// on Apple platforms it is:
			// * `option` + `right` that moves to the next word
			// * `shift` + `option` + `right` that select the next word
			if (DeviceTargetHelper.UsesAppleKeyboardLayout)
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
						var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(end, true);
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
							var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, true);
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

		internal void KeyDownHome(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
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
				selectionLength = ctrl ? -selectionStart : TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength).start - selectionStart;
			}
			else
			{
				selectionStart = ctrl ? 0 : TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength).start;
				selectionLength = 0;
			}
			args.Handled = selectionStart != start || selectionLength != end - start;
		}

		internal void KeyDownEnd(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
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
					var line = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);
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
					var line = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);
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

		internal void KeyDownDelete(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
		{
			// on Apple platforms it is `option` + `delete>` that removes the next word
			if (DeviceTargetHelper.UsesAppleKeyboardLayout)
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
				_host.CommitDelete(oldText, text, selectionStart, selectionLength);
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
					var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, true);
					index = chunk.start + chunk.length;
				}
				else
				{
					index = selectionStart + 1;
				}
				text = text[..selectionStart] + text[index..];
				// On WinUI, when ctrl-delete is Undone, the deleted text actually gets selected even though initially, nothing was selected
				_host.CommitDelete(oldText, text, selectionStart, ctrl ? index - selectionStart : 0);
			}
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

			var (startLineStart, startLineLength, startLineFirst, startLineLast, startLineIndex) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart);
			var (endLineStart, endLineLength, endLineFirst, endLineLast, endLineIndex) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);

			if (up && shift && endLineFirst)
			{
				return 0; // first line, goes to the beginning
			}
			else if (!up && shift && endLineLast)
			{
				return text.Length; // last line, goes to the end
			}
			else if (!up && !shift && (startLineLast || endLineLast))
			{
				return text.Length; // last line, goes to the end
			}

			int newLineIndex;
			if (up)
			{
				if (selectionLength < 0 || shift)
				{
					newLineIndex = !endLineFirst ? endLineIndex - 1 : endLineIndex;
				}
				else
				{
					newLineIndex = !startLineFirst ? startLineIndex - 1 : startLineIndex;
				}
			}
			else
			{
				if (selectionLength > 0 || shift)
				{
					newLineIndex = !endLineLast ? endLineIndex + 1 : endLineIndex;
				}
				else
				{
					newLineIndex = !startLineLast ? startLineIndex + 1 : startLineIndex;
				}
			}

			var rect = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(selectionStart + selectionLength);
			var x = _caretXOffset;
			var y = (newLineIndex + 0.5) * rect.Height; // 0.5 is to get the center of the line, rect.Height is line height
			var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(new Point(x, y), true, true));
			var (newLineStart, newLineLength, newLineFirst, newLineLast, _) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(index);
			if (text.Length > index - 1
				&& newLineLength > 1 // this check is for cases where the line has nothing but \r (i.e. is empty)
				&& index - 1 >= 0
				&& index == newLineStart + newLineLength
				&& (text[index - 1] == '\r' || text[index - 1] == ' '))
			{
				// if we're past \r or space, we will actually be at the beginning of the next line, so we take a step back
				index--;
			}

			return index;
		}
	}
}
