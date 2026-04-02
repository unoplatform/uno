#if __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	/// <summary>
	/// Provides the Skia implementation of the <see cref="RichEditTextDocument"/> document model.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native Windows RichEdit control (ITextServices2/ITextDocument2 COM interface)
	/// as the backing store. Uno implements a managed document model with a flat text buffer and overlaid format spans.
	/// The paragraph separator is '\r' (carriage return), consistent with WinUI's TOM behavior.
	/// </remarks>
	public partial class RichEditTextDocument
	{
		private readonly StringBuilder _textBuffer = new();
		private readonly List<FormatSpan> _formatSpans = new();
		private readonly List<UndoRecord> _undoStack = new();
		private readonly List<UndoRecord> _redoStack = new();
		private RichEditTextSelection _selection;
		private TextCharacterFormat _defaultCharacterFormat = new();
		private TextParagraphFormat _defaultParagraphFormat = new();
		private int _batchUpdateCount;
		private bool _isInUndoGroup;
		private List<UndoRecord> _undoGroupRecords;

		internal event Action ContentChanged;

		internal RichEditBox Owner { get; set; }

		internal RichEditTextDocument(RichEditBox owner)
		{
			Owner = owner;
			_selection = new RichEditTextSelection(this);
		}

		/// <summary>
		/// Gets the full plain text of the document.
		/// </summary>
		internal string TextBuffer => _textBuffer.ToString();

		/// <summary>
		/// Gets the total length of text in the document.
		/// </summary>
		internal int TextLength => _textBuffer.Length;

		/// <summary>
		/// Gets the list of format spans overlaid on the text buffer.
		/// </summary>
		internal IReadOnlyList<FormatSpan> FormatSpans => _formatSpans;

		// ===== ITextDocument properties =====

		public ITextSelection Selection => _selection;

		public CaretType CaretType { get; set; } = CaretType.Normal;

		public float DefaultTabStop { get; set; } = 36f; // 0.5 inch default

		public uint UndoLimit { get; set; } = 100;

		// ===== ITextDocument methods =====

		public void GetText(TextGetOptions options, out string value)
		{
			if ((options & TextGetOptions.UseCrlf) != 0)
			{
				value = _textBuffer.ToString().Replace("\r", "\r\n");
			}
			else
			{
				value = _textBuffer.ToString();
			}
		}

		public void SetText(TextSetOptions options, string value)
		{
			var oldText = _textBuffer.ToString();
			_textBuffer.Clear();
			_formatSpans.Clear();

			if (value != null)
			{
				// Normalize line endings to \r (WinUI TOM convention)
				var normalizedText = value.Replace("\r\n", "\r").Replace("\n", "\r");
				_textBuffer.Append(normalizedText);
			}

			// Record undo
			RecordUndo(new UndoRecord(0, oldText, _textBuffer.ToString()));

			_selection.SetRange(0, 0);
			NotifyContentChanged();
		}

		public ITextRange GetRange(int startPosition, int endPosition)
		{
			startPosition = Math.Max(0, Math.Min(startPosition, _textBuffer.Length));
			endPosition = Math.Max(0, Math.Min(endPosition, _textBuffer.Length));
			return new RichEditTextRange(this, startPosition, endPosition);
		}

		public ITextRange GetRangeFromPoint(global::Windows.Foundation.Point point, PointOptions options)
		{
			// TODO: Uno - Implement hit testing from point coordinates
			return GetRange(0, 0);
		}

		public ITextCharacterFormat GetDefaultCharacterFormat()
		{
			return _defaultCharacterFormat.GetClone();
		}

		public void SetDefaultCharacterFormat(ITextCharacterFormat value)
		{
			_defaultCharacterFormat.SetClone(value);
			NotifyContentChanged();
		}

		public ITextParagraphFormat GetDefaultParagraphFormat()
		{
			return _defaultParagraphFormat.GetClone();
		}

		public void SetDefaultParagraphFormat(ITextParagraphFormat value)
		{
			_defaultParagraphFormat.SetClone(value);
			NotifyContentChanged();
		}

		public bool CanUndo() => _undoStack.Count > 0;

		public bool CanRedo() => _redoStack.Count > 0;

		public bool CanCopy() => _selection.Length > 0;

		public bool CanPaste() => true; // Simplified - always allow paste attempt

		public void Undo()
		{
			if (_undoStack.Count == 0)
			{
				return;
			}

			var record = _undoStack[_undoStack.Count - 1];
			_undoStack.RemoveAt(_undoStack.Count - 1);

			// Apply reverse
			_textBuffer.Clear();
			_textBuffer.Append(record.OldText);
			RebuildFormatSpansAfterUndo(record);

			_redoStack.Add(record);
			NotifyContentChanged();
		}

		public void Redo()
		{
			if (_redoStack.Count == 0)
			{
				return;
			}

			var record = _redoStack[_redoStack.Count - 1];
			_redoStack.RemoveAt(_redoStack.Count - 1);

			// Apply forward
			_textBuffer.Clear();
			_textBuffer.Append(record.NewText);

			_undoStack.Add(record);
			NotifyContentChanged();
		}

		public void ClearUndoRedoHistory()
		{
			_undoStack.Clear();
			_redoStack.Clear();
		}

		public void BeginUndoGroup()
		{
			_isInUndoGroup = true;
			_undoGroupRecords = new List<UndoRecord>();
		}

		public void EndUndoGroup()
		{
			if (_isInUndoGroup && _undoGroupRecords?.Count > 0)
			{
				// Combine into a single undo record
				var first = _undoGroupRecords[0];
				var last = _undoGroupRecords[_undoGroupRecords.Count - 1];
				var combined = new UndoRecord(first.Position, first.OldText, last.NewText);
				_undoStack.Add(combined);
				TrimUndoStack();
			}

			_isInUndoGroup = false;
			_undoGroupRecords = null;
		}

		public int BatchDisplayUpdates()
		{
			_batchUpdateCount++;
			return _batchUpdateCount;
		}

		public int ApplyDisplayUpdates()
		{
			if (_batchUpdateCount > 0)
			{
				_batchUpdateCount--;
			}

			if (_batchUpdateCount == 0)
			{
				NotifyContentChanged();
			}

			return _batchUpdateCount;
		}

		// ===== Internal document operations =====

		/// <summary>
		/// Inserts text at the specified position, shifting format spans accordingly.
		/// </summary>
		internal void InsertText(int position, string text, ITextCharacterFormat format = null)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			position = Math.Max(0, Math.Min(position, _textBuffer.Length));

			var oldText = _textBuffer.ToString();
			_textBuffer.Insert(position, text);

			// Shift existing spans
			for (int i = 0; i < _formatSpans.Count; i++)
			{
				var span = _formatSpans[i];
				if (span.Start >= position)
				{
					_formatSpans[i] = span with { Start = span.Start + text.Length, End = span.End + text.Length };
				}
				else if (span.End > position)
				{
					_formatSpans[i] = span with { End = span.End + text.Length };
				}
			}

			// Add format span for the inserted text if format is specified
			if (format != null)
			{
				AddFormatSpan(position, position + text.Length, format);
			}

			RecordUndo(new UndoRecord(position, oldText, _textBuffer.ToString()));

			if (_batchUpdateCount == 0)
			{
				NotifyContentChanged();
			}
		}

		/// <summary>
		/// Deletes text in the specified range.
		/// </summary>
		internal void DeleteText(int start, int length)
		{
			if (length <= 0 || start < 0 || start >= _textBuffer.Length)
			{
				return;
			}

			length = Math.Min(length, _textBuffer.Length - start);
			var end = start + length;
			var oldText = _textBuffer.ToString();

			_textBuffer.Remove(start, length);

			// Adjust format spans
			for (int i = _formatSpans.Count - 1; i >= 0; i--)
			{
				var span = _formatSpans[i];

				if (span.End <= start)
				{
					// Span is entirely before the deletion - no change
					continue;
				}
				else if (span.Start >= end)
				{
					// Span is entirely after the deletion - shift back
					_formatSpans[i] = span with { Start = span.Start - length, End = span.End - length };
				}
				else if (span.Start >= start && span.End <= end)
				{
					// Span is entirely within the deletion - remove
					_formatSpans.RemoveAt(i);
				}
				else if (span.Start < start && span.End > end)
				{
					// Span straddles the deletion - shrink
					_formatSpans[i] = span with { End = span.End - length };
				}
				else if (span.Start < start)
				{
					// Span starts before and ends within - trim end
					_formatSpans[i] = span with { End = start };
				}
				else
				{
					// Span starts within and ends after - trim start and shift
					_formatSpans[i] = span with { Start = start, End = span.End - length };
				}
			}

			// Remove zero-length spans
			_formatSpans.RemoveAll(s => s.Start >= s.End);

			RecordUndo(new UndoRecord(start, oldText, _textBuffer.ToString()));

			if (_batchUpdateCount == 0)
			{
				NotifyContentChanged();
			}
		}

		/// <summary>
		/// Replaces text in the specified range with new text.
		/// </summary>
		internal void ReplaceText(int start, int length, string newText, ITextCharacterFormat format = null)
		{
			if (start < 0)
			{
				return;
			}

			var batchWasZero = _batchUpdateCount == 0;
			if (batchWasZero)
			{
				_batchUpdateCount++;
			}

			DeleteText(start, length);
			InsertText(start, newText, format);

			if (batchWasZero)
			{
				_batchUpdateCount--;
				NotifyContentChanged();
			}
		}

		/// <summary>
		/// Gets the text in the specified range.
		/// </summary>
		internal string GetTextInRange(int start, int end)
		{
			start = Math.Max(0, Math.Min(start, _textBuffer.Length));
			end = Math.Max(start, Math.Min(end, _textBuffer.Length));
			return _textBuffer.ToString(start, end - start);
		}

		/// <summary>
		/// Adds a format span for the specified range.
		/// </summary>
		internal void AddFormatSpan(int start, int end, ITextCharacterFormat format)
		{
			if (start >= end)
			{
				return;
			}

			// Remove overlapping portions of existing spans in the same range
			SplitAndRemoveOverlapping(start, end);

			_formatSpans.Add(new FormatSpan(start, end, (TextCharacterFormat)((TextCharacterFormat)format ?? new TextCharacterFormat()).GetClone()));
			_formatSpans.Sort((a, b) => a.Start.CompareTo(b.Start));
		}

		/// <summary>
		/// Gets the character format at the specified position.
		/// </summary>
		internal TextCharacterFormat GetFormatAt(int position)
		{
			for (int i = 0; i < _formatSpans.Count; i++)
			{
				var span = _formatSpans[i];
				if (position >= span.Start && position < span.End)
				{
					return (TextCharacterFormat)span.Format.GetClone();
				}
			}

			return (TextCharacterFormat)_defaultCharacterFormat.GetClone();
		}

		/// <summary>
		/// Gets the merged character format for the specified range.
		/// If all characters share the same format, returns that format.
		/// If they differ, returns Undefined for differing properties.
		/// </summary>
		internal TextCharacterFormat GetFormatForRange(int start, int end)
		{
			if (start >= end || _textBuffer.Length == 0)
			{
				return (TextCharacterFormat)_defaultCharacterFormat.GetClone();
			}

			var result = GetFormatAt(start);

			for (int pos = start + 1; pos < end && pos < _textBuffer.Length; pos++)
			{
				var fmt = GetFormatAt(pos);
				result.MergeWith(fmt);
			}

			return result;
		}

		/// <summary>
		/// Gets the paragraph boundaries (start and end indices) for the paragraph containing the given position.
		/// </summary>
		internal (int start, int end) GetParagraphBounds(int position)
		{
			var text = _textBuffer.ToString();
			position = Math.Max(0, Math.Min(position, text.Length));

			var start = position;
			while (start > 0 && text[start - 1] != '\r')
			{
				start--;
			}

			var end = position;
			while (end < text.Length && text[end] != '\r')
			{
				end++;
			}

			return (start, end);
		}

		/// <summary>
		/// Gets all paragraph break positions in the text.
		/// </summary>
		internal List<int> GetParagraphBreaks()
		{
			var breaks = new List<int>();
			var text = _textBuffer.ToString();
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\r')
				{
					breaks.Add(i);
				}
			}

			return breaks;
		}

		private void SplitAndRemoveOverlapping(int start, int end)
		{
			var toAdd = new List<FormatSpan>();
			for (int i = _formatSpans.Count - 1; i >= 0; i--)
			{
				var span = _formatSpans[i];

				if (span.Start >= start && span.End <= end)
				{
					// Entirely within the new range - remove
					_formatSpans.RemoveAt(i);
				}
				else if (span.Start < start && span.End > end)
				{
					// Straddles the new range - split into two
					_formatSpans.RemoveAt(i);
					toAdd.Add(new FormatSpan(span.Start, start, span.Format));
					toAdd.Add(new FormatSpan(end, span.End, span.Format));
				}
				else if (span.Start < start && span.End > start)
				{
					// Overlaps start - trim end
					_formatSpans[i] = span with { End = start };
				}
				else if (span.Start < end && span.End > end)
				{
					// Overlaps end - trim start
					_formatSpans[i] = span with { Start = end };
				}
			}

			_formatSpans.AddRange(toAdd);
		}

		private void RecordUndo(UndoRecord record)
		{
			if (_isInUndoGroup)
			{
				_undoGroupRecords?.Add(record);
			}
			else
			{
				_undoStack.Add(record);
				_redoStack.Clear();
				TrimUndoStack();
			}
		}

		private void TrimUndoStack()
		{
			while (_undoStack.Count > (int)UndoLimit && _undoStack.Count > 0)
			{
				_undoStack.RemoveAt(0);
			}
		}

		private void RebuildFormatSpansAfterUndo(UndoRecord record)
		{
			// Simple approach: clear format spans on undo
			// A more sophisticated implementation would save/restore format state
			_formatSpans.Clear();
		}

		private void NotifyContentChanged()
		{
			ContentChanged?.Invoke();
		}

		// ===== Internal types =====

		internal record struct FormatSpan(int Start, int End, TextCharacterFormat Format);

		private record UndoRecord(int Position, string OldText, string NewText);
	}
}
#endif
