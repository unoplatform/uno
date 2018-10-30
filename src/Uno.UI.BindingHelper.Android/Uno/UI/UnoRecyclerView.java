package Uno.UI;

import android.content.Context;
import android.support.v7.widget.RecyclerView;
import android.view.*;
import android.graphics.PointF;
import android.graphics.Rect;
import android.util.Log;
import android.support.v4.view.*;
import java.lang.*;
import java.lang.reflect.*;

public abstract class UnoRecyclerView
		extends RecyclerView
		implements UnoViewParent{
	private boolean _childHandledTouchEvent;
	private boolean _childBlockedTouchEvent;
	private boolean _childIsUnoViewGroup;

	protected UnoRecyclerView(Context context) {
		super(context);
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

	public boolean dispatchTouchEvent(MotionEvent e)
	{
		// See UnoViewGroup for exegesis of Uno.Android's touch handling logic.
		_childIsUnoViewGroup = false;
		_childBlockedTouchEvent = false;
		_childHandledTouchEvent = false;

		// Always dispatch the touch events, otherwise system controls may not behave
		// properly, such as not displaying "material design" animation cues (e.g. the
		// growing circles in buttons when keeping pressed).
		boolean superDispatchTouchEvent = super.dispatchTouchEvent(e);

		if (!_childIsUnoViewGroup) // child is native
		{
			// Log.i(this.toString(), "!_childIsUnoViewGroup: " + !_childIsUnoViewGroup);
			// If no child is under the touch, or the UnoRecyclerView is scrolling, superDispatchTouchEvent is normally true.
			_childBlockedTouchEvent = _childHandledTouchEvent = superDispatchTouchEvent;
		}

		boolean isBlockingTouchEvent = _childBlockedTouchEvent;
		// Always return true if super returns true, otherwise scrolling might not be handled correctly.
		boolean isHandlingTouchEvent = _childHandledTouchEvent || superDispatchTouchEvent;

		// Log.i(this.toString(), "MotionEvent: " + e.toString());
		// Log.i(this.toString(), "superDispatchTouchEvent: " + superDispatchTouchEvent);
		// Log.i(this.toString(), "_childBlockedTouchEvent: " + _childBlockedTouchEvent);
		// Log.i(this.toString(), "_childHandledTouchEvent: " + _childHandledTouchEvent);
		// Log.i(this.toString(), "isBlockingTouchEvent: " + isBlockingTouchEvent);
		// Log.i(this.toString(), "isHandlingTouchEvent: " + isHandlingTouchEvent);

		UnoViewParent parentUnoViewGroup = getParentUnoViewGroup();
		boolean parentIsUnoViewGroup = parentUnoViewGroup != null;
		// Log.i(this.toString(), "parentIsUnoViewGroup: " + parentIsUnoViewGroup);

		if (parentIsUnoViewGroup)
		{
			parentUnoViewGroup.setChildIsUnoViewGroup(true);
		}

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

	@Override
	public void removeViewAt(int index) {
		View child = getChildAt(index);
		super.removeViewAt(index);
		notifyChildRemoved(child);
	}

	@Override
	public void removeView(View view) {
		super.removeView(view);
		notifyChildRemoved(view);
	}

	private void notifyChildRemoved(View child)
	{
		Uno.UI.UnoViewGroup childViewGroup = child instanceof Uno.UI.UnoViewGroup
				? (Uno.UI.UnoViewGroup)child
				: null;

		if(childViewGroup != null)
		{
			// This is required because the Parent property is set to null
			// after the onDetachedFromWindow is called.
			childViewGroup.onRemovedFromParent();
		}
	}

	private UnoViewParent getParentUnoViewGroup()
	{
		ViewParent parent = getParent();

		return parent instanceof UnoViewParent
				? (UnoViewParent)parent
				: null;
	}
}