using System;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal enum GripperMode
{
	Hidden,
	/// <summary>Only the "end" gripper is shown, tracking a collapsed caret (TextBox only).</summary>
	EndOnly,
	/// <summary>Both grippers are shown, one at each end of a non-empty selection.</summary>
	Both,
}

internal interface ITextSelectionGripperHost
{
	/// <summary>
	/// The <see cref="TextBlock"/> that actually renders the text. Used for hit-testing
	/// (<c>ParsedText</c>), coordinate transforms, padding and the popup's <c>XamlRoot</c>.
	/// For a TextBox this is the internal DisplayBlock; for a selectable TextBlock it is itself.
	/// </summary>
	TextBlock GripperTextSurface { get; }

	/// <summary>
	/// The absolute (root-relative) bounds the grippers are culled against. This is the visible
	/// region of the control, which for a TextBox differs from the (potentially scrolled) text surface.
	/// </summary>
	Rect GripperClipBounds { get; }

	GripperMode GripperMode { get; }

	/// <summary>The lower (smaller) character index of the current selection.</summary>
	int SelectionLowerIndex { get; }

	/// <summary>The upper (larger) character index of the current selection.</summary>
	int SelectionUpperIndex { get; }

	/// <summary>
	/// Apply a selection spanning <paramref name="start"/>..<paramref name="end"/> while dragging a
	/// gripper in <see cref="GripperMode.Both"/>. <paramref name="end"/> may be smaller than
	/// <paramref name="start"/>; the host decides how (or whether) to track that direction.
	/// </summary>
	void SetGripperSelection(int start, int end);

	/// <summary>Move a collapsed caret to <paramref name="index"/> (only used in <see cref="GripperMode.EndOnly"/>).</summary>
	void MoveGripperCaret(int index);

	/// <summary>Bring the dragged gripper's position into view (no-op for controls that don't scroll their content).</summary>
	void ScrollForGripper(bool isEndGripper);

	/// <summary>
	/// A gripper was pressed down. Dismiss any open transient selection UI (the selection flyout) so a
	/// fresh interaction starts clean; the gesture result (hold -> context menu, release -> flyout) re-shows it.
	/// </summary>
	void OnGripperPressed();

	/// <summary>The gripper was long-pressed: open the context menu.</summary>
	void RequestGripperContextMenu(PointerRoutedEventArgs args);

	/// <summary>A gripper interaction ended: queue a selection-flyout visibility update.</summary>
	void QueueGripperSelectionFlyout(PointerRoutedEventArgs args);

	/// <summary>The gripper was tapped (not dragged or held): treat it like a tap on the text.</summary>
	void OnGripperTapped(PointerRoutedEventArgs args);
}

/// <summary>
/// Owns and drives the pair of <see cref="CaretWithStemAndThumb"/> touch-selection grippers shared by
/// TextBox and selectable TextBlock. All the fiddly geometry (popup placement, the stem offset, the
/// thumb-swap when the dragged gripper crosses the anchor) lives here so the two controls stay in lockstep.
/// </summary>
internal sealed class TextSelectionGripperPresenter
{
	private readonly ITextSelectionGripperHost _host;

	// _startGripper is rendered at the lower index, _endGripper at the upper index.
	private CaretWithStemAndThumb _startGripper;
	private CaretWithStemAndThumb _endGripper;

	public TextSelectionGripperPresenter(ITextSelectionGripperHost host)
	{
		_host = host;

		_startGripper = new CaretWithStemAndThumb(Update);
		_endGripper = new CaretWithStemAndThumb(Update);

		foreach (var gripper in (ReadOnlySpan<CaretWithStemAndThumb>)[_startGripper, _endGripper])
		{
			gripper.PointerPressed += OnGripperPointerPressed;
			gripper.PointerReleased += OnGripperPointerReleased;
			gripper.PointerMoved += OnGripperPointerMoved;
			gripper.PointerCanceled += ClearGripperPointerState;
			gripper.PointerCaptureLost += ClearGripperPointerState;
		}

		// Keep the grippers glued to the selection ends as the text surface is (re)drawn.
		_host.GripperTextSurface.DrawingFinished += () =>
		{
			// Only invalidate the grippers after drawing is complete to avoid modifying the children
			// visuals during the render cycle.
			NativeDispatcher.Main.Enqueue(Update);
		};
	}

	// Test hook: the pair of grippers when they are currently showing, otherwise null.
	internal (CaretWithStemAndThumb start, CaretWithStemAndThumb end)? VisibleGrippersForTesting
		=> _host.GripperMode != GripperMode.Hidden ? (_startGripper, _endGripper) : null;

	public void Hide()
	{
		_startGripper.Hide();
		_endGripper.Hide();
	}

	/// <summary>
	/// Reposition (or hide) the grippers based on the host's current selection. Idempotent and safe to
	/// call every frame.
	/// </summary>
	public void Update()
	{
		var mode = _host.GripperMode;
		if (mode == GripperMode.Hidden)
		{
			Hide();
			return;
		}

		var surface = _host.GripperTextSurface;
		var clip = _host.GripperClipBounds;
		var lower = _host.SelectionLowerIndex;
		var upper = _host.SelectionUpperIndex;

		foreach (var (index, gripper) in (ReadOnlySpan<(int, CaretWithStemAndThumb)>)[(lower, _startGripper), (upper, _endGripper)])
		{
			if (mode == GripperMode.EndOnly)
			{
				if (gripper == _startGripper)
				{
					gripper.Hide();
					continue;
				}
				else
				{
					gripper.SetStemVisible(lower == upper);
				}
			}

			var rect = surface.ParsedText.GetRectForIndex(index);
			rect.Width = TextBlock.CaretThickness;
			// ParsedText rects are relative to the text origin; the surface draws translated by its Padding.
			rect.X += surface.Padding.Left;
			rect.Y += surface.Padding.Top;
			gripper.Height = rect.Height + 16;
			var transform = surface.TransformToVisual(null);
			if (transform.TransformBounds(rect).IntersectWith(clip) is not null)
			{
				var matrixTransform = (MatrixTransform)transform;
				var surfaceMatrix = matrixTransform.Matrix.ToMatrix3x2();

				// Center the gripper horizontally on the caret position.
				var localCenterX = rect.GetMidX() - gripper.Width / 2;
				var localPoint = new Point(localCenterX, rect.Top);

				var translationMatrix = Matrix3x2.CreateTranslation((float)localPoint.X, (float)localPoint.Y);
				var totalMatrix = Matrix3x2.Multiply(translationMatrix, surfaceMatrix);
				gripper.ShowAt(surface.XamlRoot, totalMatrix);
			}
			else
			{
				gripper.Hide();
			}
		}
	}

	private void OnGripperPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;

		// Dismiss the selection flyout on press; it re-appears on release (or yields to the context menu on hold).
		_host.OnGripperPressed();

		var gripper = (CaretWithStemAndThumb)sender;
		if (gripper.CapturePointer(args.Pointer))
		{
			gripper.SetStemVisible(true);
		}

		gripper.LastPointerDown = args.GetCurrentPoint(null);
	}

	private void OnGripperPointerMoved(object sender, PointerRoutedEventArgs args)
	{
		var gripper = (CaretWithStemAndThumb)sender;
		if (!gripper.HasPointerCapture)
		{
			return;
		}
		args.Handled = true;

		var surface = _host.GripperTextSurface;
		var point = args.GetCurrentPoint(surface).Position
			- new Point(surface.Padding.Left, surface.Padding.Top)
			- new Point(0, (gripper.Height - 16) / 2);
		var index = Math.Max(0, surface.ParsedText.GetIndexAt(point, false, true));

		if (_host.GripperMode == GripperMode.EndOnly)
		{
			Debug.Assert(gripper == _endGripper);
			_host.MoveGripperCaret(index);
		}
		else
		{
			var start = _host.SelectionLowerIndex;
			var end = _host.SelectionUpperIndex;
			if (gripper == _startGripper)
			{
				start = index;
			}
			else
			{
				end = index;
			}

			if (start != end) // if start == end we do nothing, so the 2 grippers won't end up on top of one another
			{
				_host.SetGripperSelection(start, end);

				if (end < start)
				{
					// The dragged gripper crossed the anchor gripper. Swap which one is the "start" (lower)
					// vs "end" (upper) gripper so the captured gripper keeps tracking the finger.
					(_startGripper, _endGripper) = (_endGripper, _startGripper);
				}
			}
		}

		_host.ScrollForGripper(gripper == _endGripper);
	}

	private void OnGripperPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		ClearGripperPointerState(sender, args);

		var gripper = (CaretWithStemAndThumb)sender;
		var previous = gripper.LastPointerDown;
		var current = args.GetCurrentPoint(null);

		var holdDuration = current.Timestamp - previous.Timestamp;
		if (holdDuration >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// Gripper was held: open the context menu (mirrors WinUI OnGripperHeld).
			args.Handled = true;
			_host.RequestGripperContextMenu(args);
		}
		else if (IsMultiTapGesture((previous.PointerId, previous.Timestamp, previous.Position), current))
		{
			args.Handled = true;
			_host.OnGripperTapped(args);
			_host.QueueGripperSelectionFlyout(args);
		}
		else
		{
			_host.QueueGripperSelectionFlyout(args);
		}
	}

	private void ClearGripperPointerState(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;
		var gripper = (CaretWithStemAndThumb)sender;
		gripper.SetStemVisible(false);
		gripper.ReleasePointerCaptures();
	}

	private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
	{
		var currentId = down.PointerId;
		var currentTs = down.Timestamp;
		var currentPosition = down.Position;

		return previousTap.id == currentId
			&& currentTs - previousTap.ts <= GestureRecognizer.MultiTapMaxDelayMicroseconds
			&& !GestureRecognizer.IsOutOfTapRange(previousTap.position, currentPosition);
	}
}
