package Uno.UI;

import android.graphics.Matrix;
import android.view.*;

/**
 * A view that participates in Uno's expanded gesture-consumption logic, which distinguishes between
 * 'handling' versus merely 'blocking' a touch event.
 *
 * Note: This interface is for internal use (a.k.a. package-private) and is NOT VISIBLE to the C# binding
 */
interface UnoMotionTarget {
	/**
	 * Get value of the managed IsEnabled
	 */
	/* internal */ boolean getNativeIsEnabled();

	/**
	 * Gets the value of the managed IsHitTestVisible
	 */
	/* internal */ boolean getNativeIsHitTestVisible();
	/* internal OR protected */	boolean nativeHitCheck(); // TODO: This should be coerced into the IsHitTestVisible()

	/**
	 * Gets the number of children that have a RenderTransform
	 */
	/* internal */ int getChildrenRenderTransformCount();

	/**
	 * Gets the RenderTransform of the given child
	 */
	/* internal */ Matrix findChildRenderTransform(View child);

	/**
	 * Gets a boolean which indicates if a View is allowed to natively capture the pointer events
	 * (like the ScrollViewer). If 'true', we will receive a ACTION_CANCEL events when the native
	 * view is "stilling" the motion events.
	 * In UWP this is managed by the ManipulationMode.
	 * @return true to prevent the system to stole the motions
	 */
	/* protected */ boolean getIsNativeMotionEventsInterceptForbidden();

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

