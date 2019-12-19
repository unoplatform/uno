package Uno.UI;

import android.graphics.Matrix;
import android.graphics.Rect;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewParent;

/* internal */ abstract class MotionTargetAdapter {
	private final Uno.UI.UnoMotionTarget _asMotionTarget;
	private final ViewGroup _asViewGroup;

	protected MotionTargetAdapter(Uno.UI.UnoMotionTarget target) {
		_asMotionTarget = target;
		_asViewGroup = (ViewGroup)target;
	}

	final ViewGroup asViewGroup() { return _asViewGroup; }
	final Uno.UI.UnoMotionTarget asMotionTarget() { return _asMotionTarget; }

	// State local to the target but managed by the adapter
	private boolean _isCustomDispatchIsActive;
	final boolean getCustomDispatchIsActive() { return _isCustomDispatchIsActive; }
	final void setCustomDispatchIsActive(boolean isCustomDispatchIsActive) { _isCustomDispatchIsActive = isCustomDispatchIsActive; }

	private View _customDispatchTarget;
	final View getCustomDispatchTouchTarget() { return _customDispatchTarget; }
	final void setCustomDispatchTouchTarget(View child) {
		if (getIsTargetCachingSupported()) {
			_customDispatchTarget = child;
		}
	}

	// specific to the raised event (generic vs touch)
	abstract boolean getIsTargetCachingSupported();
	abstract boolean dispatchToSuper(MotionEvent event);
	abstract boolean dispatchToChild(View child, MotionEvent event);
}

/* internal */ abstract class TouchMotionTarget extends Uno.UI.MotionTargetAdapter {

	protected TouchMotionTarget(Uno.UI.UnoMotionTarget target) {
		super(target);
	}

	@Override final boolean getIsTargetCachingSupported() { return true; }
	@Override final boolean dispatchToChild(View view, MotionEvent event) { return view.dispatchTouchEvent(event); }
}

/* internal */ abstract class GenericMotionTarget extends Uno.UI.MotionTargetAdapter {

	protected GenericMotionTarget(Uno.UI.UnoMotionTarget target) {
		super(target);
	}

	@Override final boolean getIsTargetCachingSupported() { return false; }
	@Override final boolean dispatchToChild(View view, MotionEvent event) { return view.dispatchGenericMotionEvent(event); }
}

/* internal */ final class UnoMotionHelper {
	private static final String LOGTAG = "UnoViewGroup";

	/**
	 * The singleton instance of the helper
	 */
	public static UnoMotionHelper Instance = new UnoMotionHelper();
	private UnoMotionHelper() {}

	private boolean _currentMotionIsHandled;
	private View _currentMotionOriginalSource;

	// To trace the pointer events (dispatchTouchEvent and dispatchGenericMotionEvent),
	// uncomment this and then uncomment logs in the method itself.
	/*
	private static String _indent = "";
	public boolean dispatchMotionEvent(Uno.UI.MotionTargetAdapter adapter, MotionEvent event)
	{
		final ViewGroup view = adapter.asViewGroup();
		final String originalIndent = _indent;
		Log.i(LOGTAG, _indent + "    + " + view.toString() + "(" + System.identityHashCode(this) + ") " +
			"[size: " + view.getWidth() + "x" + view.getHeight() + " | scroll: x="+ view.getScrollX() + " y=" + view.getScrollY() + "]");
		_indent += "    | ";
		Log.i(LOGTAG, _indent + event.toString());

		final boolean dispatched = dispatchMotionEventCore(adapter, event);

		_indent = originalIndent;

		return dispatched;
	}

	private boolean dispatchMotionEventCore(Uno.UI.MotionTargetAdapter adapter, MotionEvent event)
	*/ public boolean dispatchMotionEvent(Uno.UI.MotionTargetAdapter adapter, MotionEvent event)
	{
		// The purpose of dispatchTouchEvent is to find the target (view) of a touch event.
		// When the user touches the screen, dispatchTouchEvent is called on the top-most view, and recursively passed down to all children.
		// Once a view (that we will call target) decides to handle the touch event (generally when it has gesture detectors and none of its children handled the event), it returns true.
		// The parent then gets notified that the event was handled by one of its child, stops passing down the touch event to the siblings of the target,
		// ignores the event (i.e., doesn't handle it itself even if it could), and returns true to its own parent (which bubbles all the way to the top-most view).
		//
		// Essentially, returning true means that you 'handled' (and implicitly also 'blocked') a touch event, where:
		// - blocked -> prevent siblings from handling the event
		// - handled -> prevent parents from handling the event
		// Natively, you can't 'block' a touch event without also 'handling' it.
		//
		// In XAML, the distinction between 'blocking' and 'handling' is important, and a view must be able to 'block' a touch event without necessarily 'handling' it.
		// Therefore, the single boolean offered by dispatchTouchEvent isn't enough, and we must allow UnoViewGroups to communicate these 'blocked' vs 'handled' nuances through some other channel.
		// To do this, we introduce 3 boolean fields in UnoViewGroups: _childIsUnoViewGroup, _childBlockedTouchEvent, _childHandledTouchEvent (each with their own public setter).
		// Because UnoViewGroup must be compatible with native (non-UnoViewGroup) views, we need to process the input (super.dispatchTouchEvent) and output (return) differently based on the nature of the parent and children.
		//
		// Input:
		// - Child is UnoViewGroup (_childIsUnoViewGroup = true) ->
		//     - read the values of _childBlockedTouchEvent and _childHandledTouchEvent (set by the child in its own dispatchTouchEvent)
		// - Child is native (_childIsUnoViewGroup = false) ->
		//     - set both the values of _childBlockedTouchEvent and _childHandledTouchEvent to the value of super.dispatchTouchEvent
		//
		// Output:
		// - Parent is UnoViewGroup ->
		//     - set _childIsUnoViewGroup, _childBlockedTouchEvent and _childHandledTouchEvent on the parent
		//     - return true if isBlockingTouchEvent to prevent siblings from receiving the touch event
		//       (returned value won't actually be read by parent, as it will prefer _childBlockedTouchEvent and _childHandledTouchEvent instead)
		// - Parent is native ->
		//	   - return true if isHandlingTouchEvent
		//       (because native views can't read _childBlockedTouchEvent and _childHandledTouchEvent, and will assume true to mean the event was handled)

		final ViewGroup view = adapter.asViewGroup();
		final Uno.UI.UnoMotionTarget target = adapter.asMotionTarget();

		// Reset possibly invalid states (set by children in previous calls)
		_currentMotionIsHandled = false;
		_currentMotionOriginalSource = null;

		final boolean isDown = event.getAction() == MotionEvent.ACTION_DOWN;
		if (isDown) {
			// When we receive a pointer DOWN, we have to reset the down -> move -> up sequence,
			// so the dispatch will re-evaluate the _customDispatchTouchTarget;
			// Note: we do not support the MotionEvent splitting for the custom touch target,
			//		 we expect that the children will properly split the events

			adapter.setCustomDispatchIsActive(target.getChildrenRenderTransformCount() > 0);
			adapter.setCustomDispatchTouchTarget(null);

			// Make sure that the system won't stole the motion events if the ManipulationMode is not 'System'.
			// Note: We have to do this in native as we might not forward the ACTION_DOWN if !target.getIsNativeMotionEventsEnabled()
			// Note: This is automatically cleared on each ACTION_UP
			if (target.getIsNativeMotionEventsInterceptForbidden()) {
				// Log.i(LOGTAG, _indent + "INTERCEPT disable (i.e. scrolling disabled on parents)");

				view.requestDisallowInterceptTouchEvent(true);
			}
		}

		if (!target.getNativeIsHitTestVisible() || !target.getNativeIsEnabled()) {
			// The View is not TestVisible or disabled, there is nothing to do here!
			// Log.i(LOGTAG, _indent + "BLOCKED [isHitTestVisible: " + target.getNativeIsHitTestVisible() + " | isEnabled: " + target.getNativeIsEnabled() + "]");

			return false;
		}

		if (isDown && !isMotionInView(event, view)) {
			// When using the "super" dispatch path, it's possible that for visual constraints (i.e. clipping),
			// the view must not handle the touch. If that's the case, the touch event must be dispatched
			// to other controls.
			// Note: We do this check only when starting a new manipulation (isDown), so the whole down->move->up sequence is ignored;
			// Warning: We should also do this check for events that does not have strong sequence concept (e.g. Hover)
			// 			(!dispatch.getIsTargetCachingSupported() || isDown), BUT if we do this, we may miss some HOVER_EXIT
			//			So we prefer to not follow the UWP behavior (PointerEnter/Exit are raised only when entering/leaving
			//			non clipped content) and get all the events.
			// Log.i(LOGTAG, _indent + "BLOCKED (not in view due to clipping)");

			return false;
		}

		// Note: Try to always dispatch the touch events, otherwise system controls may not behave
		//		 properly, such as not displaying "material design" animation cues
		//		 (e.g. the growing circles in buttons when keeping pressed (RippleEffect)).

		boolean childIsTouchTarget; // a.k.a. blocking
		if (adapter.getCustomDispatchIsActive()) {
			// Log.i(LOGTAG, _indent + "CUSTOM dispatch (" + target.getChildrenRenderTransformCount() + " of " + view.getChildCount() + " children are transformed )");

			// Because we do not call dispatchToSuper(), we must manually call requestDisallowInterceptTouchEvent in the same circumstances
			// that dispatchToSuper would, otherwise we get inconsistencies (children have the flag but parents do not).
			final boolean isCancel = event.getAction() == MotionEvent.ACTION_CANCEL;
			final boolean isHover = event.getAction() == MotionEvent.ACTION_HOVER_MOVE;
			if (isDown || isCancel || isHover) {
				view.requestDisallowInterceptTouchEvent(false);
			}

			childIsTouchTarget = dispatchStaticTransformedMotionEvent(adapter, event);
		} else {
			// Log.i(LOGTAG, _indent + "SUPER dispatch (none of the " + view.getChildCount() + " children is transformed)");

			childIsTouchTarget = adapter.dispatchToSuper(event);
		}

		// Note: There is a bug (#14712) where the UnoViewGroup receives the MOTION_DOWN,
		// is collapsed (or the child who received the MOTION_DOWN) (e.g. VisualState concurrency issue)
		// and doesn't receive the MOTION_UP. This is because the control is removed
		// from the visual tree when it's collapsed and won't get the dispatchTouchEvent.
		// To workaround this, simply put a transparent background on the clickable control
		// so that it receives the touch (tryHandleTouchEvent) instead of its children.

		// Walk the tree up to the first UnoMotionTarget, if any.
		ViewParent parent = view.getParent();
		Uno.UI.UnoMotionTarget parentTarget = null;
		while(parent != null)
		{
			if (parent instanceof Uno.UI.UnoMotionTarget) {
				parentTarget = (Uno.UI.UnoMotionTarget)parent;
				break;
			}
			parent = parent.getParent();
		}

		final boolean isTouchTarget = childIsTouchTarget || target.nativeHitCheck();
		if (isTouchTarget && _currentMotionOriginalSource == null) {
			// If the ** static ** _currentMotionOriginalSource is null, it means we are the are the first managed child that
			// completes the dispatch, so we are the "OriginalSource" of the event (a.k.a. the leaf of the visual tree)

			// The original source must be convertible to a UIElement. As we don't have access to the UIElement class,
			// here we only make sure to (continue to) walk tree up to the first UnoViewGroup (which is the base class for UIElement).
			ViewParent originalSource = view;
			if (!(originalSource instanceof Uno.UI.UnoViewGroup))
			{
				originalSource = parent;
				while(originalSource != null && !(originalSource instanceof Uno.UI.UnoViewGroup)) {
					originalSource = originalSource.getParent();
				}
			}

			// Strong cast is legit here: either the originalSource will be a UnoViewGroup or null.
			_currentMotionOriginalSource = (View)originalSource;

			// Log.i(LOGTAG, _indent + "This control is the leaf, set OriginalSource= " + _currentMotionOriginalSource);
		}

		if (!_currentMotionIsHandled && isTouchTarget && target.getIsNativeMotionEventsEnabled() && isMotionSupportedByManaged(event)) {
			// If the event was not "handled" (in the UWP terminology) by the managed code yet,
			// try to handle it here for the current target. (i.e. we are bubbling the managed event here !)

			// As on Android there is an implicit capture of motion event (down -> move -> up) are raised on the same
			// target until pointer is released, the managed code will have to filter/re-route the event in order to follow
			// the UWP behavior. We prefer to compute the 'isInView' in native as it's less expensive.
			final boolean isInView = isMotionInView(event, view);

			_currentMotionIsHandled = target.onNativeMotionEvent(event, _currentMotionOriginalSource, isInView);

			// Log.i(LOGTAG, _indent + "Managed event not handled yet, tried to raise it, result: " + _currentMotionIsHandled);
		}

		if (parentTarget == null) {
			// The top element of the visual tree must always reply 'true' in order to receive all pointers events.
			// (If we reply 'false' to an ACTION_DOWN, we won't receive the subsequent ACTION_MOVE nor ACTION_UP.)

			// Log.i(LOGTAG, _indent + "ROOT true [isTarget: " + isTouchTarget + " | isHandled: " + _currentMotionIsHandled + "]");

			// When we reach to top of the visual tree, we clear the original source to prevent leak!
			_currentMotionOriginalSource = null;
			return true;
		}
		else if (isTouchTarget)
		{
			// This View (or one of its children) is opaque to the touch (a.k.a. blocking),
			// we reply "true" in order to prevent siblings View from receiving the motion event.

			// Log.i(LOGTAG, _indent + "TARGET true [isTarget: " + isTouchTarget + " | isHandled: " + _currentMotionIsHandled + "]");

			return true;
		}
		else
		{
			// This View and all its children are "transparent" to the touch (**null** background / fill / ...).
			// We have to reply "false", so the parent View will try to dispatch this motion event to the siblings of this target.

			// if (!_currentMotionIsHandled) {
			// 	Log.e(LOGTAG, _indent + "ERROR Invalid state: This View is being considered as 'transparent', " +
			// 		"however the managed event is already 'handled'. This means either that the motion event should have " +
			// 		"been dispatched to this target at all, or a child control handed the event but did not 'blocked' the touch.");
			// }

			// Log.i(LOGTAG, _indent + "OUT false [isTarget: " + isTouchTarget + " | isHandled: " + _currentMotionIsHandled + "]");

			return false;
		}
	}

	private boolean dispatchStaticTransformedMotionEvent(Uno.UI.MotionTargetAdapter target, MotionEvent event) {
		// As super ViewGroup won't apply the "StaticTransform" on the event (cf. https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2992)
		// when it determines if the `MotionEvent` is "in the view" of the child (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2975)
		// the event will be filtered out and won't be propagated properly to all children (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2665)
		// As a result a UIElement which has a `RenderTransform` won't be able to handle tap properly.
		// To workaround this, if we have some child transformation, we propagate the event by ourselves.
		// Doing this we bypass a lot of logic done by the super ViewGroup, (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2557)
		// especially optimization of the TouchTarget resolving / tracking. (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2654)
		// We assume that events that are wrongly dispatched to children are going to be filtered by children themselves
		// and this support is sufficient enough for our current cases.
		// Note: this is not fully compliant with the UWP contract (cf. https://github.com/unoplatform/uno/issues/649)

		// Note: If this logic is called once, it has to be called for all MotionEvents in the same touch cycle, including Cancel, because if
		// ViewGroup.dispatchTouchEvent() isn't called for Down then all subsequent events won't be handled correctly
		// (because mFirstTouchTarget won't be set)

		final View childTarget = target.getCustomDispatchTouchTarget();
		if (childTarget != null) {
			// We already have a target for the events, just apply the static transform and dispatch the event.
			// Note: We "assumeInView" as all the pointers of the same sequence (down -> move -> up) should be raised
			//		 on the same element. This behavior a.k.a. "implicit capture" is not the UWP one, but the Android behavior.
			//		 It will be "patched" to follow the UWP behavior in the managed code (cf. UIElement.CapturePointer).
			return dispatchStaticTransformedMotionEvent(target, childTarget, event, true);

		} else {
			final ViewGroup view = target.asViewGroup();

			// The target was not selected yet
			for (int i = view.getChildCount() - 1; i >= 0; i--) { // Inverse enumeration in order to prioritize controls that are on top
				View child = view.getChildAt(i);

				// Same check as native "canViewReceivePointerEvents"
				if (child.getVisibility() != View.VISIBLE || child.getAnimation() != null) {
					continue;
				}

				if (dispatchStaticTransformedMotionEvent(target, child, event, false)) {
					target.setCustomDispatchTouchTarget(child); // (try to) cache the child for future events
					return true; // Stop at the first child which is able to handle the event
				}
			}

			return false;
		}
	}

	private boolean dispatchStaticTransformedMotionEvent(Uno.UI.MotionTargetAdapter adapter, View child, MotionEvent event, boolean assumeInView){
		// For the ACTION_CANCEL the coordinates are not set properly:
		// "Canceling motions is a special case.  We don't need to perform any transformations
		// or filtering.  The important part is the action, not the contents."
		// https://android.googlesource.com/platform/frameworks/base/+/master/core/java/android/view/ViewGroup.java#3010
		if (event.getAction() == MotionEvent.ACTION_CANCEL) {
			return adapter.dispatchToChild(child, event);
		}

		final ViewGroup view = adapter.asViewGroup();
		final Matrix transform = adapter.asMotionTarget().findChildRenderTransform(child);
		final float offsetX = view.getScrollX() - child.getLeft();
		final float offsetY = view.getScrollY() - child.getTop();

		boolean handled = false;
		if (transform == null || transform.isIdentity()) {
			// No meaningful transformation on this child, instead of cloning the MotionEvent,
			// we only offset the current one, propagate it to the child and then offset it back to its original values.

			event.offsetLocation(offsetX, offsetY);

			// When we are searching for the target (i.e. !assumeInView),
			// we make sure that the view is under the touch event.
			if (assumeInView || isMotionInView(event, child)) {
				handled = adapter.dispatchToChild(child, event);
			}

			event.offsetLocation(-offsetX, -offsetY);
		} else {
			// We have a valid static transform on this child, we have to transform the MotionEvent
			// into the child coordinates.

			final Matrix inverse = new Matrix();
			transform.invert(inverse);

			final MotionEvent transformedEvent = MotionEvent.obtain(event);
			transformedEvent.offsetLocation(offsetX, offsetY);
			transformedEvent.transform(inverse);

			// When we are searching for the target (i.e. !assumeInView),
			// we make sure that the view is under the touch event.
			if (assumeInView || isMotionInView(transformedEvent, child)) {
				handled = adapter.dispatchToChild(child, transformedEvent);
			}

			transformedEvent.recycle();
		}

		return handled;
	}

	/**
	 * Checks if the given motion in the view's local coordinate space is within its bounds, taking any clipping into account.
	 * @param e The event to check
	 * @return True if the point is within the view's bounds, false otherwise.
	 */
	private static final boolean isMotionInView(MotionEvent e, View view) {
		final float x = e.getX();
		final float y = e.getY();

		if (x < 0 || x >= view.getWidth()
			|| y < 0 || y >= view.getHeight()) {
			return false;
		}

		final Rect clipBounds = android.support.v4.view.ViewCompat.getClipBounds(view);
		if (clipBounds == null) {
			return true;
		} else{
			return x >= clipBounds.left && x < clipBounds.right && y >= clipBounds.top && y < clipBounds.bottom;
		}
	}

	private static final boolean isMotionSupportedByManaged(MotionEvent event) {
		switch (event.getActionMasked())
		{
			case MotionEvent.ACTION_DOWN:
			case MotionEvent.ACTION_POINTER_DOWN:
			case MotionEvent.ACTION_MOVE:
			case MotionEvent.ACTION_UP:
			case MotionEvent.ACTION_CANCEL:
			case MotionEvent.ACTION_POINTER_UP:
			case MotionEvent.ACTION_HOVER_ENTER:
			case MotionEvent.ACTION_HOVER_MOVE:
			case MotionEvent.ACTION_HOVER_EXIT:
				return true;
			default:
				return false;
		}
	}
}
