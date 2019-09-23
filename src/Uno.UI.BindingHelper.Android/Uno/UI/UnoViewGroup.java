package Uno.UI;

import android.graphics.Matrix;
import android.view.*;
import android.view.animation.Transformation;
import android.graphics.Rect;
import android.util.Log;

import java.lang.*;
import java.lang.reflect.*;
import java.util.ArrayList;
import java.util.Map;
import java.util.HashMap;

public abstract class UnoViewGroup
	extends android.view.ViewGroup
	implements UnoViewParent{

	private static final String LOGTAG = "UnoViewGroup";
	private static boolean _isLayoutingFromMeasure = false;
	private static ArrayList<UnoViewGroup> callToRequestLayout = new ArrayList<UnoViewGroup>();

	private boolean _inLocalAddView, _inLocalRemoveView;
	private boolean _isEnabled;
	private boolean _isHitTestVisible;
	private UnoGestureDetector _gestureDetector;

	private boolean _childHandledTouchEvent;
	private boolean _childBlockedTouchEvent;
	private boolean _childIsUnoViewGroup;

	private boolean _isManagedLoaded;
	private boolean _needsLayoutOnAttachedToWindow;

	private boolean _isPointInView;

	private boolean _shouldBlockRequestFocus;

	private int _currentPointerId = -1;

	private Map<View, Matrix> _childrenTransformations = new HashMap<View, Matrix>();
	private float _transformedTouchX;
	private float _transformedTouchY;

	private static Method _setFrameMethod;

	static {
		try {
			buildSetFrameReflection();
		}
		catch(Exception e) {
			Log.e(LOGTAG, "Failed to initialize NativeSetFrame method. " + e.toString());
		}
	}

	private static void buildSetFrameReflection() throws ClassNotFoundException, InstantiationException, Exception
	{
		// This is required because we need to set the view bounds before calling onLayout()
		Class viewClass = java.lang.Class.forName("android.view.View");

		Method[] methods = viewClass.getDeclaredMethods();

		for(int i=0; i < methods.length; i++) {
			if(methods[i].getName() == "setFrame" && methods[i].getParameterTypes().length == 4) {
				_setFrameMethod = methods[i];
				_setFrameMethod.setAccessible(true);
			}
		}

		// This block is commented for easier logging.
		//
		// if(_setFrameMethod != null) {
		// 	Log.i(LOGTAG, "Found android.view.View.setFrame(), arrange fast-path is ENABLED.");
		// }
		// else {
		// 	Log.i(LOGTAG, "Unable to find android.view.View.setFrame(), arrange fast-path is DISABLED.");
		// }
	}


	public UnoViewGroup(android.content.Context ctx)
	{
		super(ctx);

		_isEnabled = true;
		_isHitTestVisible = true;

		setOnHierarchyChangeListener(
			new OnHierarchyChangeListener()
			{
				@Override
				public void onChildViewAdded(View parent, View child)
				{
					if(!_inLocalAddView)
					{
						onLocalViewAdded(child, indexOfChild(child));
					}
				}

				@Override
				public void onChildViewRemoved(View parent, View child)
				{
					if(!_inLocalRemoveView)
					{
						onLocalViewRemoved(child);
					}
				}
			}
		);

		setClipChildren(false); // This is required for animations not to be cut off by transformed ancestor views. (#1333)
	}

	public final void enableAndroidClipping()
	{
		setClipChildren(true); // called by controls requiring it (ScrollViewer)
	}

	private boolean _unoLayoutOverride;

	public final void nativeStartLayoutOverride(int left, int top, int right, int bottom)  throws IllegalAccessException, InvocationTargetException {
		_unoLayoutOverride = true;

		if(_setFrameMethod != null) {
			// When Uno overrides the layout pass, the setFrame method must be called to
			// set the bounds of the frame, while not calling layout().

			_setFrameMethod.invoke(this, new Object[]{ left, top, right, bottom });
		}
		else {
			// This method is present as a fallback in case google would remove the
			// setFrame method.
			layout(left, top, right, bottom);

			// Force layout is required to ensure that the children added during the layout phase
			// are properly layouted. Failing to call this method will make controls
			// like the ContentPresenter not display their content, if added late.
			forceLayout();
		}
	}

	public final void nativeFinishLayoutOverride(){
		// Call the actual layout method, so the FORCE_LAYOUT flag is cleared.
		// This must be called before setting _unoLayoutOverride to false, to avoid onLayout calling
		// onLayoutCore.
		layout(getLeft(), getTop(), getRight(), getBottom());

		_unoLayoutOverride = false;
	}

	protected abstract void onLayoutCore(boolean changed, int left, int top, int right, int bottom);

	protected final void onLayout(boolean changed, int left, int top, int right, int bottom)
	{
		if(!_unoLayoutOverride)
		{
			onLayoutCore(changed, left, top, right, bottom);
		}
	}

	protected abstract void onLocalViewAdded(View view, int index);

	protected abstract void onLocalViewRemoved(View view);

	public static long getMeasuredDimensions(View view) {
		// This method is called often enough that returning one long
		// instead of two calls returning two integers improves
		// the layouting performance.
		return view.getMeasuredWidth() | (((long)view.getMeasuredHeight()) << 32);
	}

	protected final void addViewFast(View view)
	{
		try
		{
			_inLocalAddView = true;

			addView(view, -1, generateDefaultLayoutParams());
		}
		finally
		{
			_inLocalAddView = false;
		}
	}

	protected final void addViewFast(View view, int position)
	{
		try
		{
			_inLocalAddView = true;

			addView(view, position, generateDefaultLayoutParams());
		}
		finally
		{
			_inLocalAddView = false;
		}
	}

	protected final void removeViewFast(View view)
	{
		try
		{
			_inLocalRemoveView = true;

			removeView(view);

			notifyChildRemoved(view);
		}
		finally
		{
			_inLocalRemoveView = false;
		}
	}

	protected final void removeViewAtFast(int position)
	{
		try
		{
			_inLocalRemoveView = true;

			View child = getChildAt(position);

			removeViewAt(position);

			notifyChildRemoved(child);
		}
		finally
		{
			_inLocalRemoveView = false;
		}
	}

	private android.text.Layout _textBlockLayout;
	private int _leftTextBlockPadding, _topTextBlockPadding;

	// Provides a fast path for textblock text drawing, to avoid overriding
	// it in C#, for improved performance.
	public final void setNativeTextBlockLayout(android.text.Layout layout, int leftPadding, int topPadding) {
		_textBlockLayout = layout;
		_leftTextBlockPadding = leftPadding;
		_topTextBlockPadding = topPadding;
	}

	@Override
	protected void onDraw(android.graphics.Canvas canvas)
	{
		if(_textBlockLayout != null) {
			canvas.translate(_leftTextBlockPadding, _topTextBlockPadding);
			_textBlockLayout.draw(canvas);
		}
	}

	private void notifyChildRemoved(View child)
	{
		UnoViewGroup childViewGroup = child instanceof UnoViewGroup
			? (UnoViewGroup)child
			: null;

		if(childViewGroup != null)
		{
			// This is required because the Parent property is set to null
			// after the onDetachedFromWindow is called.
			childViewGroup.onRemovedFromParent();
		}
	}

	protected abstract void onRemovedFromParent();

	protected final void measureChild(View view, int widthSpec, int heightSpec)
	{
		super.measureChild(view, widthSpec, heightSpec);
	}

	public final boolean getIsNativeLoaded() {
		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.JELLY_BEAN_MR2) {
			return super.getWindowId() != null;
		}
		else {
			return super.getWindowToken() != null;
		}
	}

	public final void requestLayout()
	{
		if(nativeRequestLayout())
		{
			if (getIsManagedLoaded() && !getIsNativeLoaded()) {
				// If we're here, managed load is enabled (AndroidUseManagedLoadedUnloaded = true) and requestLayout() has been called from OnLoaded
				// prior to dispatchAttachedToWindow() being called. This can cause the request to fall through the cracks, because mAttachInfo
				// isn't set yet. (See ViewRootImpl.requestLayoutDuringLayout()). If we're in a layout pass already, we have to ensure that requestLayout()
				// is called again once the view is fully natively initialized.
				_needsLayoutOnAttachedToWindow = true;
			}

			if(_isLayoutingFromMeasure){
				callToRequestLayout.add(this);
				return;
			}

			super.requestLayout();
		}
	}

	public final void invalidate()
	{
		super.invalidate();
	}

	protected abstract boolean nativeRequestLayout();

	public final boolean isLayoutRequested()
	{
		return super.isLayoutRequested();
	}

	public final float getAlpha()
	{
		return super.getAlpha();
	}

	public final void setAlpha(float opacity)
	{
		super.setAlpha(opacity);
	}

	/*
	// To trace the 'dispatchTouchEvent', uncomment this and then uncomment logs in the method itself
	private static String _indent = "";
	public boolean dispatchTouchEvent(MotionEvent e)
	{
		String originalIndent = _indent;
		Log.i(LOGTAG, _indent + "    + " + this.toString());
		_indent += "    | ";

		boolean dispatched = dispatchTouchEventCore(e);

		_indent = originalIndent;

		return dispatched;
	}

	public boolean dispatchTouchEventCore(MotionEvent e)*/
	public boolean dispatchTouchEvent(MotionEvent e)
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

		// Reset possibly invalid states (set by children in previous calls)
		_childIsUnoViewGroup = false;
		_childBlockedTouchEvent = false;
		_childHandledTouchEvent = false;

		if (!_isHitTestVisible || !_isEnabled)
		{
			// Log.i(LOGTAG, _indent + "!_isHitTestVisible: " + !_isHitTestVisible);
			// Log.i(LOGTAG, _indent + "!_isEnabled: " + !_isEnabled);

			// ignore all touches
			clearCaptures();
			return false;
		}

		updateTransformedTouchCoordinate(e);

		final boolean wasPointInView = _isPointInView;

		// It's possible that for visual constraints (e.g. clipping),
		// the view must not handle the touch. If that's the case,
		// the touch event must be dispatched to other controls.
		// This check must be done independent of whether the gestureDetector is tested
		// because some controls may want to react to the gesture (e.g. action_cancel, action_up)
		// even if the point is outside the view bounds.
		_isPointInView = isLocalTouchPointInView(_transformedTouchX, _transformedTouchY); // takes clipping into account

		//Log.i(LOGTAG, _indent + "MotionEvent: " + e.toString());
		//Log.i(LOGTAG, _indent + "_isPointInView: " + _isPointInView);

		// Note: Always dispatch the touch events, otherwise system controls may not behave
		//		 properly, such as not displaying "material design" animation cues
		//		 (e.g. the growing circles in buttons when keeping pressed (RippleEffect)).

		boolean superDispatchTouchEvent = false;
		if (_childrenTransformations.size() == 0) {
			// We don't have any child which has a static transform, so propagate the event through the 'super' logic

			// _shouldBlockRequestFocus: This is a hacky way to prevent requestFocus() being incorrectly called for views whose parent is
			// using the 'static transformation path.' The better fix would be for the static transformation code path to import more of
			// ViewGroup's logic to only propagate dispatchTouchEvent() to children selectively.
			_shouldBlockRequestFocus = !_isPointInView && e.getAction() == MotionEvent.ACTION_UP && hasTransformedSiblings();
			superDispatchTouchEvent = super.dispatchTouchEvent(e);
			_shouldBlockRequestFocus = false;
		} else {

			// As super ViewGroup won't apply the "StaticTransform" on the event (cf. https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2992)
			// when it determines if the `MotionEvent` is "in the view" of the child (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2975)
			// the event will be filtered out and won't be propagated properly to all children (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2665)
			// As a result a UIElement which has a `RenderTransform` won't be able to handle tap properly.
			// To workaround this, if we have some child transformation, we propagate the event by ourseleves.
			// Doing this we bypass a lot of logic done by the super ViewGroup, (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2557)
			// especially optimization of the TouchTarget resolving / tracking. (https://android.googlesource.com/platform/frameworks/base/+/0e71b4f19ba602c8c646744e690ab01c69808b42/core/java/android/view/ViewGroup.java#2654)
			// We assume that events that are wronlgy dispatched to children are going to be filteerd by children themselves
			// and thios support is sufficent enough for our current cases.
			// Note: this is not fully complient with the UWP contract (cf. https://github.com/nventive/Uno/issues/649)

			// Note: If this logic is called once, it has to be called for all MotionEvents in the same touch cycle, including Cancel, because if
			// ViewGroup.dispatchTouchEvent() isn't called for Down then all subsequent events won't be handled correctly
			// (because mFirstTouchTarget won't be set)
			Matrix inverse = new Matrix();

			for (int i = getChildCount() - 1; i >= 0; i--) { // Inverse enumeration in order to prioritize controls that are on top
				View child = getChildAt(i);

				if (child.getVisibility() != View.VISIBLE)
				{
					continue;
				}

				final Matrix transform = _childrenTransformations.get(child);
				final float offsetX = getScrollX() - child.getLeft();
				final float offsetY = getScrollY() - child.getTop();

				if (transform == null || transform.isIdentity()) {
					// No meaningful transformation on this child, instead of cloning the MotionEvent,
					// we only offset the current one, propagate it to the child and then offset it back to its original values.

					e.offsetLocation(offsetX, offsetY);

					superDispatchTouchEvent = child.dispatchTouchEvent(e);

					e.offsetLocation(-offsetX, -offsetY);
				} else {
					// We have a valid static transform on this child, we have to transform the MotionEvent
					// into the child coordinates.

					final MotionEvent transformedEvent = MotionEvent.obtain(e);

					transformedEvent.offsetLocation(offsetX, offsetY);
					transform.invert(inverse);
					transformedEvent.transform(inverse);

					superDispatchTouchEvent = child.dispatchTouchEvent(transformedEvent);

					transformedEvent.recycle();
				}

				// Stop at the first child which is able to handle the event
				if (superDispatchTouchEvent) {
					break;
				}
			}
		}

		final boolean didPointerExit = wasPointInView &&
			!_isPointInView &&
			(e.getActionMasked() == MotionEvent.ACTION_MOVE || e.getActionMasked() == MotionEvent.ACTION_CANCEL);

		final boolean isCurrentPointer = isCurrentPointer(e, _isPointInView);

		if (!_childIsUnoViewGroup) // child is native
		{
			// Log.i(LOGTAG, _indent + "!_childIsUnoViewGroup: " + !_childIsUnoViewGroup);
			_childBlockedTouchEvent = _childHandledTouchEvent = superDispatchTouchEvent;
		}

		// Note: There is a bug (#14712) where the UnoViewGroup receives the MOTION_DOWN,
		// is collapsed (or the child who received the MOTION_DOWN) (e.g. VisualState concurrency issue)
		// and doesn't receive the MOTION_UP. This is because the control is removed
		// from the visual tree when it's collapsed and won't get the dispatchTouchEvent.
		// To workaround this, simply put a transparent background on the clickable control
		// so that it receives the touch (tryHandleTouchEvent) instead of its children.

		// only executed if left-hand side is false (prevents event from being handled twice)
		// gives the current view a chance to block/handle the touch event if none of its children have
		boolean isBlockingTouchEvent = _childBlockedTouchEvent || nativeHitCheck();
		boolean isHandlingTouchEvent = _childHandledTouchEvent ||
			((isBlockingTouchEvent || didPointerExit) && tryHandleTouchEvent(e, _isPointInView, wasPointInView, isCurrentPointer));

		//Log.i(LOGTAG, _indent + "superDispatchTouchEvent: " + superDispatchTouchEvent);
		//Log.i(LOGTAG, _indent + "_childBlockedTouchEvent: " + _childBlockedTouchEvent);
		//Log.i(LOGTAG, _indent + "_childHandledTouchEvent: " + _childHandledTouchEvent);
		//Log.i(LOGTAG, _indent + "isBlockingTouchEvent: " + isBlockingTouchEvent);
		//Log.i(LOGTAG, _indent + "isHandlingTouchEvent: " + isHandlingTouchEvent);

		UnoViewParent parentUnoViewGroup = getParentUnoViewGroup();
		boolean parentIsUnoViewGroup = parentUnoViewGroup != null;
		// Log.i(LOGTAG, _indent + "parentIsUnoViewGroup: " + parentIsUnoViewGroup);

		if (parentIsUnoViewGroup)
		{
			parentUnoViewGroup.setChildIsUnoViewGroup(true);
		}

		if (!_isPointInView && !getIsPointerCaptured())
		{
			// Log.i(LOGTAG, _indent + "!_isPointInView: " + !_isPointInView);
			return false;
		}

		tryClearCapture(e);

		if (parentIsUnoViewGroup)
		{
			parentUnoViewGroup.setChildBlockedTouchEvent(isBlockingTouchEvent);
			parentUnoViewGroup.setChildHandledTouchEvent(isHandlingTouchEvent);

			// Prevents siblings from receiving the touch event.
			// Won't actually be read by parent (which will prefer _childBlockedTouchEvent and _childHandledTouchEvent).
			return isBlockingTouchEvent;
		}
		else // parent is native
		{
			// Native views don't understand the difference between 'blocked' and 'handled',
			// and will assume true to mean that the touch event was handled (which can cause problems when nested inside native controls like ListViews).
			return isHandlingTouchEvent;
		}
	}

	/**
	 * Check if event corresponds to the 'current' pointer, and update current pointer if needed.
	 * @param e The MotionEvent.
	 * @param isPointInView Is the point within the bounds of this view.
	 * @return Does this event correspond to the current pointer, ie the first pointer (finger) to touch this view during the current interaction.
	 */
	public boolean isCurrentPointer(MotionEvent e, boolean isPointInView) {
		final int action = e.getActionMasked();

		final int pointerId = e.getPointerId(e.getActionIndex());

		switch (action) {
			case MotionEvent.ACTION_CANCEL: {
				// Unset currrent pointer
				_currentPointerId = -1;
				return true;
			}
			case MotionEvent.ACTION_DOWN:
			case MotionEvent.ACTION_POINTER_DOWN: {
				if (isPointInView && _currentPointerId == -1) {
					// The first pointer we encounter during an interaction becomes the current pointer.
					_currentPointerId = pointerId;
					return true;
				}
				return false;
			}
			case MotionEvent.ACTION_UP:
			case MotionEvent.ACTION_POINTER_UP: {
				// True if this matches the pointer received during the down event.
				final boolean isCurrentPointer = pointerId == _currentPointerId;
				if (isCurrentPointer || action == MotionEvent.ACTION_UP) {
					_currentPointerId = -1;
				}
				return isCurrentPointer;
			}
			default:
				// Since ActionIndex isn't supplied for events other than up/down, we don't actually
				// know which pointer this is coming from.
				return true;
		}
	}

	private boolean tryHandleTouchEvent(MotionEvent e, boolean isPointInView, boolean wasPointInView, boolean isCurrentPointer)
	{
		return _gestureDetector != null && _gestureDetector.onTouchEvent(e, isPointInView, wasPointInView, getIsPointerCaptured(), isCurrentPointer);
	}

	private void tryClearCapture(MotionEvent e) {
		int action = e.getAction();
		if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP || action == MotionEvent.ACTION_CANCEL) {
			clearCaptures();
		}
	}

	protected void clearCaptures(){
	}

	/**
	 * Call this method if a view is to be laid out outside of a framework layout pass, to ensure that requestLayout() requests are captured
	 * and propagated later. (Equivalent of ViewRootImpl.requestLayoutDuringLayout())
	 */
	public static void startLayoutingFromMeasure() {
		_isLayoutingFromMeasure = true;
	}

	/**
	 * This should always be called immediately after {{@link #startLayoutingFromMeasure()}} has been called.
	 */
	public static void endLayoutingFromMeasure() {
			_isLayoutingFromMeasure = false;
	}

	/**
	 * This should be called subsequently to {{@link #endLayoutingFromMeasure()}}, typically during a true layout pass, to flush any captured layout requests.
	 */
	public static void measureBeforeLayout() {
		if (_isLayoutingFromMeasure)
		{
			// This can happen when nested controls call startLayoutingFromMeasure()/measureBeforeLayout()
			return;
		}

		try {
			for (int i = 0; i < callToRequestLayout.size(); i++) {
				UnoViewGroup view = callToRequestLayout.get(i);
				if (view.isAttachedToWindow()) {
					view.requestLayout();
				}
			}
		}
		finally {
			callToRequestLayout.clear();
		}
	}

	protected boolean getIsPointerCaptured() {
		return false;
	}

	private UnoViewParent getParentUnoViewGroup()
	{
		ViewParent parent = getParent();

		return parent instanceof UnoViewParent
			? (UnoViewParent)parent
			: null;
	}

	public final void setChildHandledTouchEvent(boolean childHandledTouchEvent)
	{
		_childHandledTouchEvent |= childHandledTouchEvent;
	}

	public final void setChildBlockedTouchEvent(boolean childBlockedTouchEvent)
	{
		_childBlockedTouchEvent |= childBlockedTouchEvent;
	}

	public final void setChildIsUnoViewGroup(boolean childIsUnoViewGroup)
	{
		_childIsUnoViewGroup |= childIsUnoViewGroup;
	}

	public final void setNativeHitTestVisible(boolean hitTestVisible)
	{
		_isHitTestVisible = hitTestVisible;
	}

	public final void setNativeIsEnabled(boolean isEnabled)
	{
		_isEnabled = isEnabled;
	}

	/**
	 * The x-coordinate of the current motion event in the view's local coordinate space (ie its top left corner is at (0,0).
	 * @return
	 */
	public final float getTransformedTouchX() {
		return _transformedTouchX;
	}

	/**
	 * The y-coordinate of the current motion event in the view's local coordinate space (ie its top left corner is at (0,0).
	 */
	public final float getTransformedTouchY() {
		return _transformedTouchY;
	}

	protected abstract boolean nativeHitCheck();

	protected final void onAttachedToWindow()
	{
		super.onAttachedToWindow();

		if(!_isManagedLoaded) {
			onNativeLoaded();
			_isManagedLoaded = true;
		}
		else if (_needsLayoutOnAttachedToWindow && isInLayout()) {
			requestLayout();
		}

		_needsLayoutOnAttachedToWindow = false;
	}

	protected abstract void onNativeLoaded();

	protected final void onDetachedFromWindow()
	{
		super.onDetachedFromWindow();

		if(_isManagedLoaded) {
			onNativeUnloaded();
			_isManagedLoaded = false;
		}
	}

	protected abstract void onNativeUnloaded();

	/**
	 * Marks this view as loaded from the managed side, so onAttachedToWindow can skip
	 * calling onNativeLoaded.
	 */
	public final void setIsManagedLoaded(boolean value)
	{
		_isManagedLoaded = value;
	}

	/**
	 * Gets if this view is loaded from the managed side.
	 */
	public final boolean getIsManagedLoaded()
	{
		return _isManagedLoaded;
	}

	public final void setVisibility(int visibility)
	{
		super.setVisibility(visibility);
	}

	public final int getVisibility()
	{
		return super.getVisibility();
	}

	public final void setBackgroundColor(int color)
	{
		super.setBackgroundColor(color);
	}

	public final void setEnabled(boolean enabled)
	{
		super.setEnabled(enabled);
	}

	public final boolean isEnabled()
	{
		return super.isEnabled();
	}

	public UnoGestureDetector getGestureDetector() {
		return _gestureDetector;
	}

	public void setGestureDetector(UnoGestureDetector gestureDetector) {
		_gestureDetector = gestureDetector;
	}

	@Override
	public boolean requestFocus(int direction, Rect previouslyFocusedRect) {
		if (_shouldBlockRequestFocus) {
			// We return 'true' because otherwise performClick() gets called
			return true;
		}

		return super.requestFocus(direction, previouslyFocusedRect);
	}

    /*
    // Not supported because set is no virtual
    public final void setFocusable(boolean focusable)
    {
        super.setFocusable(focusable);
    }

    public final boolean getFocusable()
    {
        return super.isFocusable();
    }
    */

	/**
	 * Checks if the given point in the view's local coordinate space is within its bounds, taking any clipping and ancestral clipping into account.
	 * This will *only* return a valid value if the method has also been called for all visual ancestors for the same absolute (screen-space) point,
	 * as occurs for UnoViewGroup.dispatchTouchEvent().
	 * @param x X-coordinate in the view's local coordinate space.
	 * @param y Y-coordinate in the view's local coordinate space.
	 * @return True if the point is within the view's bounds, false otherwise.
	 */
	public boolean isLocalTouchPointInView(float x, float y) {
		return x >= 0 && x < getWidth()
			&& y >= 0 && y < getHeight()
			&& isWithinClipBounds(x, y)
			&& getIsParentPointInView(getParent()); // Ensures parent clipping (if any) is taken into account.
	}

	private boolean isWithinClipBounds(float x, float y) {
		Rect clipBounds = android.support.v4.view.ViewCompat.getClipBounds(this);
		if (clipBounds != null) {
			return x >= clipBounds.left && x < clipBounds.right && y >= clipBounds.top && y < clipBounds.bottom;
		}
		return true;
	}

	private static boolean getIsParentPointInView(ViewParent vp) {
		if (vp instanceof UnoViewGroup) {
			return ((UnoViewGroup) vp).getIsPointInView();
		}

		if (vp instanceof View) {
			return getIsParentPointInView(vp.getParent());
		}

		// Reached Window
		return true;
	}

	private void updateTransformedTouchCoordinate(MotionEvent e) {
		float[] coord = getTransformedTouchCoordinate(getParent(), e);
		calculateTransformedPoint(this, coord);
		_transformedTouchX = coord[0];
		_transformedTouchY = coord[1];
	}

	/**
	 * Transforms a point from the coordinate space of a view's parent to the view's coordinate space.
	 *
	 * The logic here is essentially the inverse of {@link #android.view.View.transformFromViewToWindowSpace(int[])} (which walks up the tree instead of down).
	 * @param view The view to put the point in the coordinate space of.
	 * @param point The point to be transformed as [x, y]
	 */
	private static void calculateTransformedPoint(View view, float[] point) {
		ViewParent viewParent = view.getParent();
		if (viewParent instanceof View) {
			final View parent = (View) viewParent;
			point[0] += parent.getScrollX();
			point[1] += parent.getScrollY();
		}

		point[0] -= view.getLeft();
		point[1] -= view.getTop();

		// Check the render transform applied by the parent UnoViewGroup
		Matrix inverse = new Matrix();
		if (viewParent instanceof UnoViewGroup) {
			Matrix parentMatrix = ((UnoViewGroup) viewParent).getChildStaticMatrix(view);
			if (!parentMatrix.isIdentity()) {
				parentMatrix.invert(inverse);
				inverse.mapPoints(point);
			}
		}

		// Then apply the transform defined directly on this view
		Matrix localMatrix = view.getMatrix();
		if (!localMatrix.isIdentity()) {
			localMatrix.invert(inverse);
			inverse.mapPoints(point);
		}
	}

	private Matrix getChildStaticMatrix(View view) {
		Matrix transform = _childrenTransformations.get(view);
		if (transform == null) {
			transform = new Matrix();
		}

		return transform;
	}

	/**
	 * Sets the static transform matrix to apply to the given child view.
	 * This will be used by the {@link #android.view.ViewGroup.getChildStaticTransformation()}
	 *
	 * @param child The view to which the matrix applies.
	 * @param transform The transformation matrix to apply.
	 */
	protected void setChildRenderTransform(View child, Matrix transform) {
		_childrenTransformations.put(child, transform);
		if (_childrenTransformations.size() == 1) {
			setStaticTransformationsEnabled(true);
		}
	}

	/**
	 * Removes the static transform matrix applied to the given child view.
	 *
	 * @param child The view to which the matrix applies.
	 * @param transform The transformation matrix to apply.
	 */
	protected void removeChildRenderTransform(View child) {
		_childrenTransformations.remove(child);
		if (_childrenTransformations.size() == 0) {
			setStaticTransformationsEnabled(false);
		}
	}

	@Override
	protected final boolean getChildStaticTransformation(View child, Transformation outTransform) {
		Matrix renderTransform = _childrenTransformations.get(child);
		if (renderTransform == null || renderTransform.isIdentity()) {
			outTransform.clear();
		} else {
			outTransform.getMatrix().set(renderTransform);
		}

		return true;
	}

	private boolean hasTransformedChildren() {
		return  _childrenTransformations.size() != 0;
	}

	private boolean hasTransformedSiblings() {
		ViewParent parent = getParent();
		if (parent instanceof UnoViewGroup) {
			return ((UnoViewGroup)parent).hasTransformedChildren();
		}

		return false;
	}

	/**
	 * Get touch coordinate transformed to a view's local space. If view is a UnoViewGroup, use already-calculated value;
	 * interpolate offsets for any non-UnoViewGroups in the visual hierarchy, and use the raw absolute position at the
	 * very top of the tree.
	 */
	private static float[] getTransformedTouchCoordinate(ViewParent view, MotionEvent e) {
		// First try to get the transformed touches from the parent if it's a UIElement (UnoViewGroup)
		if (view instanceof UnoViewGroup) {
			// Just use cached value
			float[] point = new float[2];
			point[0] = ((UnoViewGroup) view).getTransformedTouchX();
			point[1] = ((UnoViewGroup) view).getTransformedTouchY();
			return point;
		}

		if(view != null) {
			// The parent view may be null if the touched item is being removed
			// from the tree while being touched.

			// Non-UIElement view, walk the tree up to the next UIElement
			// (and adjust coordinate for each layer to include its location, i.e. Top, Left, etc.)
			final ViewParent parent = view.getParent();
			if (parent instanceof View) {
				// Not at root, walk upward
				float[] coords = getTransformedTouchCoordinate(parent, e);
				calculateTransformedPoint((View) view, coords);
				return coords;
			}
		}

		// We reached the top of the tree
		float[] point = new float[2];
		point[0] = e.getRawX();
		point[1] = e.getRawY();
		if (view instanceof View) {
			// Call getLocationOnScreen() to get window offsets which may be non-zero, eg in the case of a popup
			int[] screenLocation = new int[2];
			((View) view).getLocationOnScreen(screenLocation);
			point[0] -= screenLocation[0];
			point[1] -= screenLocation[1];
		}
		return point;
	}

	// Allows UI automation operations to look for a single 'Text' property for both ViewGroup and TextView elements.
	// Is mapped to the UIAutomationText property
	public String getText() {
		return null;
	}

	boolean getIsPointInView() {
		return _isPointInView;
	}

	/**
	 * Get the depth of this view in the visual tree. For debugging use only.
	 *
	 * @return Depth
	 */
	public int getViewDepth() {
		int viewDepth = 0;
		ViewParent parent = getParent();
		while (parent != null) {
			viewDepth++;
			parent = parent.getParent();
		}
		return viewDepth;
	}
}
