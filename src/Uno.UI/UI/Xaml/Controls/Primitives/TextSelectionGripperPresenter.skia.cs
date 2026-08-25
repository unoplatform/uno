using System;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.Foundation.Logging;
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

	// One frame-driven reposition pass for the pair: a per-gripper subscription runs Update - and the
	// ancestor-clip walk behind GripperClipBounds - once per showing gripper per frame.
	private CompositionTarget _frameLoop;
	private bool _repositionFailureLogged;

	public TextSelectionGripperPresenter(ITextSelectionGripperHost host)
	{
		_host = host;

		_startGripper = new CaretWithStemAndThumb();
		_endGripper = new CaretWithStemAndThumb();

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
		UnsubscribeFromFrameLoop();
		_startGripper.Hide();
		_endGripper.Hide();
	}

	// Subscribed for as long as the grippers are showing. Unsubscribing from inside the callback is safe:
	// FrameRendered is an Action event, so the in-flight invocation walks a snapshot of the handler list.
	private void SubscribeToFrameLoop()
	{
		if (_frameLoop is null && _host.GripperTextSurface.XamlRoot is { } xamlRoot)
		{
			_frameLoop = xamlRoot.VisualTree.ContentRoot.CompositionTarget;
			_frameLoop.FrameRendered += OnFrameRendered;
		}
	}

	private void UnsubscribeFromFrameLoop()
	{
		if (_frameLoop is { } frameLoop)
		{
			_frameLoop = null;
			frameLoop.FrameRendered -= OnFrameRendered;
		}
	}

	// The subscription outlives the grippers' visibility - a culled gripper still needs frames to notice its anchor
	// scrolling back in - so an exception escaping Update would take the window's render loop with it.
	private void OnFrameRendered()
	{
		try
		{
			Update();
		}
		catch (Exception e)
		{
			// First failure only: it would otherwise repeat every rendered frame.
			if (!_repositionFailureLogged)
			{
				_repositionFailureLogged = true;
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to reposition the text selection grippers.", e);
				}
			}
		}
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

		SubscribeToFrameLoop();

		var surface = _host.GripperTextSurface;
		// An axis-aligned bbox (GetGlobalBoundsWithOptions reduces the ancestor clip path to its Bounds), so a
		// rotated ancestor over-approximates its clip and culls less than it could.
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
			gripper.Height = rect.Height + CaretWithStemAndThumb.ThumbSize;
			var transform = surface.TransformToVisual(null);
			// Cull on the point the thumb hangs from - the bottom-center of the caret line - rather than on the
			// caret line as a whole. The grippers are drawn in an unclipped popup above the tree, so a line that is
			// only fractionally visible at the edge of the clip would still paint a whole thumb past it. This is
			// what native Android checks too (Editor.HandleView.isPositionVisible).
			// A 1px probe rather than a bare point: both sides of the test come from float matrices, and a caret
			// line that ends flush with the clip (a selectable TextBlock's last line) must not flicker on rounding.
			var anchor = new Rect(rect.GetMidX() - 0.5, rect.Bottom - 0.5, 1, 1);
			// Never cull the gripper the finger is holding: hiding it closes its popup, and unloading an element
			// releases its pointer captures (UIElement.Pointers.ClearPointersStateOnUnload), which would end the drag.
			// The cost is a dragged gripper painting outside the clip until the finger lifts; native instead
			// auto-scrolls the container when a handle reaches the edge, which we don't do.
			if (gripper.HasPointerCapture || transform.TransformBounds(anchor).IntersectWith(clip) is not null)
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
				// On the transition only. An empty clip here means GripperClipBounds computed nothing (host out of
				// the live tree, or zero-sized) rather than the anchor genuinely being scrolled away.
				if (gripper.IsShowing && this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Culling a text selection gripper: anchor {transform.TransformBounds(anchor)} is outside the clip {clip}.");
				}

				// Closed rather than collapsed: the reposition loop is driven by the presenter, not by the popups, so
				// nothing depends on a culled gripper's popup staying open - and an open one shows up in the public
				// VisualTreeHelper.GetOpenPopupsForXamlRoot.
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
