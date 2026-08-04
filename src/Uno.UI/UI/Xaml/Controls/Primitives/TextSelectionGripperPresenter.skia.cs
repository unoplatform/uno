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

	/// <summary>
	/// A gripper interaction ended: queue a selection-flyout visibility update. <paramref name="allowEmptySelection"/>
	/// is set when the gripper was tapped (not dragged), so the flyout re-opens even over a collapsed caret (the single
	/// insertion handle) — mirroring the native iOS/Android insertion-handle popup. The host still restricts this to its
	/// mobile touch conventions.
	/// </summary>
	void QueueGripperSelectionFlyout(PointerRoutedEventArgs args, bool allowEmptySelection);

	/// <summary>
	/// The gripper was tapped (not dragged or held): treat it like a tap on the text at the character the gripper
	/// points at. <paramref name="anchorIndex"/> is that character index (the gripper's own selection edge / caret),
	/// so the tap pins there instead of re-sampling the finger — which sits on the thumb below the caret line and
	/// would spill onto the line below (on a single-line box, the end of the text). <paramref name="press"/> is the
	/// tap's <em>press</em> point, so the host can fold it into its multi-tap counter (a tap landing on the insertion
	/// handle is still the second tap of a double-tap-to-select-word).
	/// </summary>
	void OnGripperTapped(PointerPoint press, int anchorIndex);
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

		// The finger grabs the thumb, which hangs below the caret line the gripper points at. Remember how far
		// below that line's center the finger landed so the drag can sample the text on the caret's own line
		// (see OnGripperPointerMoved). Without this the sample spills onto the line below and GetIndexAt jumps
		// to the end of that line — on a single-line box, the end of the whole text.
		var surface = _host.GripperTextSurface;
		var gripperIndex = gripper == _startGripper ? _host.SelectionLowerIndex : _host.SelectionUpperIndex;
		var lineRect = surface.ParsedText.GetRectForIndex(gripperIndex);
		var lineCenterSurfaceY = surface.Padding.Top + lineRect.Y + lineRect.Height / 2;
		gripper.GrabOffsetY = args.GetCurrentPoint(surface).Position.Y - lineCenterSurfaceY;
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
		// Subtract the grab offset captured on press so the drag samples the caret's own line (where the finger
		// started relative to it), not the thumb's position a line below.
		var moveSurface = args.GetCurrentPoint(surface).Position;
		var sampleY = moveSurface.Y - gripper.GrabOffsetY - surface.Padding.Top;

		// Clamp the sampled Y into the text's vertical span (first line's centre .. last line's centre) so a finger
		// that drifts above or below the text still adjusts the caret horizontally, instead of GetIndexAt clamping
		// the out-of-range Y and snapping the caret to a line's start/end. GetRectForIndex clamps its index, so
		// int.MaxValue yields the last line's rect.
		var firstLine = surface.ParsedText.GetRectForIndex(0);
		var lastLine = surface.ParsedText.GetRectForIndex(int.MaxValue);
		sampleY = Math.Clamp(sampleY, firstLine.GetMidY(), lastLine.GetMidY());

		var point = new Point(moveSurface.X - surface.Padding.Left, sampleY);
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
		var stayedInPlace = !GestureRecognizer.IsOutOfTapRange(previous.Position, current.Position);
		if (stayedInPlace && holdDuration >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// The gripper was held in place (not dragged): open the context menu (mirrors WinUI OnGripperHeld).
			args.Handled = true;
			_host.RequestGripperContextMenu(args);
		}
		else if (IsMultiTapGesture((previous.PointerId, previous.Timestamp, previous.Position), current))
		{
			args.Handled = true;
			// Pin the tap to the character this gripper points at (its selection edge / caret). The finger grabbed
			// the thumb below the caret line, so re-sampling the release point would spill onto the line below and
			// jump to the end of the text — the same hazard the drag path avoids with GrabOffsetY.
			var anchorIndex = gripper == _startGripper ? _host.SelectionLowerIndex : _host.SelectionUpperIndex;
			_host.OnGripperTapped(previous, anchorIndex);
			// A tap on the (single) insertion handle re-opens the flyout even over a collapsed caret.
			_host.QueueGripperSelectionFlyout(args, allowEmptySelection: true);
		}
		else
		{
			// The gripper was dragged to adjust the selection: keep the thumbs and re-show the selection toolbar.
			_host.QueueGripperSelectionFlyout(args, allowEmptySelection: false);
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
