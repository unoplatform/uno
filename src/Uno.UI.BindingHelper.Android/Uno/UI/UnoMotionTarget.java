package Uno.UI;

import android.graphics.Matrix;
import android.view.*;
import android.graphics.PointF;
import android.graphics.Rect;
import android.util.Log;
import java.lang.*;
import java.lang.reflect.*;

/**
 * A view that participates in Uno's expanded gesture-consumption logic, which distinguishes between
 * 'handling' versus merely 'blocking' a touch event.
 *
 * Note: This interface is for internal use (a.k.a. package-private) and is NOT VISIBLE to the C# binding
 */
interface UnoMotionTarget {
//	/**
//	 * Called by a child to indicate to its parent that it is an Uno view. Normally called every time
//	 * the child receives a {@link android.view.View.dispatchTouchEvent(MotionEvent)}, and should be
//	 * reset by the owner at the beginning of its own dispatchTouchEvent().
//	 * @param value True if the child is an {@link UnoMotionTarget}
//	 */
//	public void setChildIsUnoViewGroup(boolean value);
//
//	/**
//	 * Called by a child from dispatchTouchEvent to indicate that it (or one of its descendants) blocked
//	 * the touch event, meaning that siblings should be prevented from consuming the event.
//	 * @param isBlockingTouchEvent True if touch event is blocked (eg because the child has a background)
//	 */
//	public void setChildBlockedTouchEvent(boolean isBlockingTouchEvent);
//
//	/**
//	 * Called by a child from dispatchTouchEvent to indicate that it (or one of its descendants) handled
//	 * the touch event.
//	 * @param isHandlingTouchEvent True if the touch event is handled (eg the child is a button)
//	 */
//	public void setChildHandledTouchEvent(boolean isHandlingTouchEvent);

	//void setChildMotionEventResult(View child, boolean isBlocking, boolean isHandling);

	/**
	 * Get the
	 * @return
	 */
	/* internal */ boolean getNativeIsEnabled();
	/* internal */ boolean getNativeIsHitTestVisible();
	/* internal OR protected */	boolean nativeHitCheck(); // TODO: This should be coerced into the IsHitTestVisible()

	/* internal */ int getChildrenRenderTransformCount();
	/* internal */ Matrix findChildRenderTransform(View child);

	/**
	 * Gets a boolean which indicates if the native motion events should be propagated to the managed
	 * code using the {{@link #onNativeMotionEvent(MotionEvent, View)}
	 * @return false means that {{@link #onNativeMotionEvent(MotionEvent, View)} won't be invoked.
	 */
	/* protected */ boolean getIsNativeMotionEventsEnabled();

	/**
	 * Propagates a native {{@Link MotionEvent}} to the managed code, if {{@Link getIsNativeMotionEventsEnabled}}.
	 * Remarks: this is the base method for Pointer events in UWP.
	 * @param event The native event that has to be interpreted and raised by the managed code
	 * @param originalSource The OriginalSource of the UWP RoutedEvent
	 * @return true means that the managed event has been handled (in UWP terminology), so the
	 * 		{@link #onNativeMotionEvent(MotionEvent, View)} won't be invoked by the parent Views
	 */
	/* protected */ boolean onNativeMotionEvent(MotionEvent event, View originalSource, boolean isInView);
}

