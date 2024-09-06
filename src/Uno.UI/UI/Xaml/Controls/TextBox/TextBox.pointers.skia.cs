using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
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

using SelectionDetails = (int start, int length, bool selectionEndsAtTheStart);

public partial class TextBox
{
	private (PointerPoint point, int repeatedPresses) _lastPointerDown; // point is null before first press
	private (int start, int length, bool tripleTap)? _mouseMultiTapChunk;
	/// <summary>Determines whether the caret is currently being dragged through touch or mouse input. null if no pointer is pressed (also see remark).</summary>
	/// <remarks>can be null if the pointer is still pressed but Escape is pressed.</remarks>>
	private PointerDeviceType? _caretDraggingPointerType;
	private bool IsDraggingCaretWithPointer => _caretDraggingPointerType != null;

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		e.Handled = true;

		if (!_isSkiaTextBox || !IsDraggingCaretWithPointer)
		{
			return;
		}

		if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// TODO
		}
		else
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var point = e.GetCurrentPoint(displayBlock);
			var index = Math.Max(0, displayBlock.Inlines.GetIndexAt(point.Position, false, true));
			if (_mouseMultiTapChunk is { } mtc)
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
				var selectionInternalStart = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
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

		if (!_isSkiaTextBox)
		{
			return;
		}

		if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// TODO
		}
		else
		{
			if (args.GetCurrentPoint(null) is var currentPoint
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
						_mouseMultiTapChunk = (SelectionStart, SelectionLength, true);
						_lastPointerDown = (currentPoint, 2);
					}
					else // _lastPointerDown.repeatedPresses == 0 or 2
					{
						// double tap
						var chunk = FindChunkAt(index, true);
						Select(chunk.start, chunk.length);
						_mouseMultiTapChunk = (chunk.start, chunk.length, false);
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

				_caretDraggingPointerType = PointerDeviceType.Mouse;
			}
		}
	}

	partial void OnPointerReleasedPartial(PointerRoutedEventArgs args)
	{
		_caretDraggingPointerType = null;
		_mouseMultiTapChunk = null;
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;
	}
}
