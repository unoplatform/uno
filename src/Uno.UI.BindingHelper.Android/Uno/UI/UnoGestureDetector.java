/*package Uno.UI;

import android.graphics.PointF;
import android.graphics.Rect;
import android.view.*;

public abstract class UnoGestureDetector extends GestureDetector {

	/**
	 * Handle touch event if appropriate.
	 * @param ev The motion event
	 * @param isPointInView Is the pointer within the view's bounds?
	 * @param isPointerCaptured Has the pointer been captured?
	 * @param isCurrentPointer If this is a pointer_up event, does it correspond to the first pointer (ie, finger) received on pointer_down by this view?
	 * @return Was the event handled.
	 *  /
	public boolean onTouchEvent(MotionEvent ev, boolean isPointInView, boolean wasPointInView, boolean isPointerCaptured, boolean isCurrentPointer)
	{
		boolean isEventHandled = false;

		if(shouldHandleCancel(ev, isPointInView, isPointerCaptured)) {
			boolean isCancelHandled = onCancel(ev);
			isEventHandled |= isCancelHandled;
		}

		if(shouldHandleUpMotion(ev, isPointInView, isPointerCaptured, isCurrentPointer)) {
			boolean isUpHandled = onUp(ev);
			isEventHandled |= isUpHandled;
		}

		if(shouldHandleDownMotion(ev, isPointInView, isPointerCaptured)) {
			boolean isDownHandled = onDown(ev);
			isEventHandled |= isDownHandled;
		}

		if(shouldHandleExitMotion(ev, isPointInView, isPointerCaptured)) {
			boolean isExitHandled = onExit(ev);
			isEventHandled |= isExitHandled;
		}

		if (shouldHandleEnterMotion(ev, isPointInView, wasPointInView, isPointerCaptured)) {
			boolean isEnterHandled = onEnter(ev);
			isEventHandled |= isEnterHandled;
		}

		if (shouldHandleMoveMotion(ev, isPointInView, isPointerCaptured)) {
			boolean isMoveHandled = onMove(ev);
			isEventHandled |= isMoveHandled;
		}

		return isEventHandled || super.onTouchEvent(ev);
	}

	@Override
	public boolean onTouchEvent(MotionEvent ev) {
		boolean wasPointInView = _target.getIsPointInView();
		boolean isPointInView = _target.isLocalTouchPointInView(_target.getTransformedTouchX(), _target.getTransformedTouchY());
		return onTouchEvent(ev, isPointInView, wasPointInView, _target.getIsPointerCaptured(), _target.isCurrentPointer(ev, isPointInView));
	}

	private boolean shouldHandleCancel(MotionEvent ev, boolean isPointInView, boolean isPointerCaptured) {
		return getShouldHandleCancel()
				&& ev.getAction() == MotionEvent.ACTION_CANCEL;
	}

	private boolean shouldHandleUpMotion(MotionEvent ev, boolean isPointInView, boolean isPointerCaptured, boolean isCurrentPointer) {
		return getShouldHandleUp()
				&& (ev.getActionMasked() == MotionEvent.ACTION_POINTER_UP || ev.getAction() == MotionEvent.ACTION_UP)
				&& isCurrentPointer
				&& (isPointInView || isPointerCaptured);
	}

	private boolean shouldHandleDownMotion(MotionEvent ev, boolean isPointInView, boolean isPointerCaptured) {
		return getShouldHandleDown()
				&& (ev.getAction() == MotionEvent.ACTION_POINTER_DOWN || ev.getAction() == MotionEvent.ACTION_DOWN)
				&& isPointInView;
	}

	private boolean shouldHandleMoveMotion(MotionEvent ev, boolean isPointInView, boolean isPointerCaptured) {
		return getShouldHandleMove()
				&& (ev.getAction() == MotionEvent.ACTION_MOVE)
				&& (isPointInView || isPointerCaptured);
	}

	private boolean shouldHandleExitMotion(MotionEvent ev, boolean isPointInView, boolean isPointerCaptured) {
		return getShouldHandleExit() && !isPointInView;
	}

	private boolean shouldHandleEnterMotion(MotionEvent ev, boolean isPointInView, boolean wasPointInView, boolean isPointerCaptured) {
		return getShouldHandleEnter() && isPointInView && !wasPointInView;
	}

	private boolean shouldHandleSingleTapMotion(MotionEvent ev, boolean isPointInView) {
		return getShouldHandleSingleTap() && isPointInView;
	}

	private boolean shouldHandleDoubleTapMotion(MotionEvent ev, boolean isPointInView) {
		return getShouldHandleDoubleTap() && isPointInView;
	}

	private boolean _shouldHandleSingleTap;
	private boolean _shouldHandleDoubleTap;
	private boolean _shouldHandleDown;
	private boolean _shouldHandleUp;
	private boolean _shouldHandleExit;
	private boolean _shouldHandleEnter;
	private boolean _shouldHandleCancel;
	private boolean _shouldHandleMove;

	private boolean getShouldHandleSingleTap() {
		return _shouldHandleSingleTap;
	}

	protected void setShouldHandleSingleTap(boolean handle) {
		_shouldHandleSingleTap = handle;
	}

	private boolean getShouldHandleDoubleTap() {
		return _shouldHandleDoubleTap;
	}

	protected void setShouldHandleDoubleTap(boolean handle) {
		_shouldHandleDoubleTap = handle;
	}

	private boolean getShouldHandleDown() {
		return _shouldHandleDown;
	}

	protected void setShouldHandleDown(boolean handle) {
		_shouldHandleDown = handle;
	}

	private boolean getShouldHandleUp() {
		return _shouldHandleUp;
	}

	protected void setShouldHandleUp(boolean handle) {
		_shouldHandleUp = handle;
	}

	private boolean getShouldHandleExit() {
		return _shouldHandleExit;
	}

	protected void setShouldHandleExit(boolean handle) {
		_shouldHandleExit = handle;
	}

	private boolean getShouldHandleEnter() {
		return _shouldHandleEnter;
	}

	protected void setShouldHandleEnter(boolean handle) {
		_shouldHandleEnter = handle;
	}

	private boolean getShouldHandleCancel() {
		return _shouldHandleCancel;
	}

	protected void setShouldHandleCancel(boolean handle) {
		_shouldHandleCancel = handle;
	}

	private boolean getShouldHandleMove() {
		return _shouldHandleMove;
	}

	protected void setShouldHandleMove(boolean handle) {
		_shouldHandleMove = handle;
	}

	private UnoViewGroup _target;

	protected UnoViewGroup getTarget() {
		return _target;
	}

	protected UnoGestureDetector(android.content.Context context, final UnoViewGroup target)
	{
		super(context, new OnGestureListener() {
			@Override
			public boolean onDown(MotionEvent e) {
				UnoGestureDetector detector = target.getGestureDetector();

				if(detector == null) {
					return false;
				}

				boolean onDown = false;

				boolean isPointInView = target.isLocalTouchPointInView(target.getTransformedTouchX(), target.getTransformedTouchY());

				//If we want to handle single tap then we need to return true here, or onSingleTapUp will never be called
				return onDown || detector.shouldHandleSingleTapMotion(e, isPointInView);
			}

			@Override
			public void onShowPress(MotionEvent e) {

			}

			@Override
			public boolean onSingleTapUp(MotionEvent e) {
				UnoGestureDetector detector = target.getGestureDetector();
				boolean isPointInView = target.isLocalTouchPointInView(target.getTransformedTouchX(), target.getTransformedTouchY());

				if(detector == null || !detector.shouldHandleSingleTapMotion(e, isPointInView)) {
					return  false;
				}
				else {
					return detector.onSingleTap(e);
				}
			}

			@Override
			public boolean onScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) {
				return false;
			}

			@Override
			public void onLongPress(MotionEvent e) {

			}

			@Override
			public boolean onFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY) {
				return false;
			}
		});

		setOnDoubleTapListener(new OnDoubleTapListener() {
			@Override
			public boolean onSingleTapConfirmed(MotionEvent e) {
				return false;
			}

			@Override
			public boolean onDoubleTap(MotionEvent e) {
				UnoGestureDetector detector = target.getGestureDetector();
				boolean isPointInView = target.isLocalTouchPointInView(target.getTransformedTouchX(), target.getTransformedTouchY());

				if (detector == null || !detector.shouldHandleDoubleTapMotion(e, isPointInView)) {
					return false;
				} else {
					return detector.onDoubleTap(e);
				}
			}

			@Override
			public boolean onDoubleTapEvent(MotionEvent e) {
				return false;
			}
		});

		_target = target;
	}

	protected void Configure(final UnoViewGroup target) {
		target.setGestureDetector(this);
	}

	protected abstract boolean onSingleTap(MotionEvent e);

	protected abstract boolean onDoubleTap(MotionEvent e);

	protected abstract boolean onDown(MotionEvent e);

	protected abstract boolean onUp(MotionEvent e);

	protected abstract boolean onExit(MotionEvent e);

	protected abstract boolean onEnter(MotionEvent e);

	protected abstract boolean onCancel(MotionEvent e);

	protected abstract boolean onMove(MotionEvent e);
}*/
