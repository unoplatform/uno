package Uno.UI;

import android.content.Context;
import android.graphics.Matrix;
import android.support.v7.widget.RecyclerView;
import android.view.*;

import java.lang.*;

public abstract class UnoRecyclerView
	extends RecyclerView
	implements Uno.UI.UnoMotionTarget {

	protected UnoRecyclerView(Context context) {
		super(context);
	}

	// NativeIsEnabled property
	private boolean _isEnabled = true;
	@Override public /* hidden to C# */ final boolean getNativeIsEnabled() { return _isEnabled; }
	public /* protected to C# */ final void setNativeIsEnabled(boolean isEnabled) { _isEnabled = isEnabled; }

	@Override public /* hidden to C# */ final boolean getNativeIsHitTestVisible() { return true; }
	@Override public /* hidden to C# */ final boolean nativeHitCheck() { return true; }

	@Override public /* hidden to C# */ final int getChildrenRenderTransformCount() { return 0; }
	@Override public /* hidden to C# */ final Matrix findChildRenderTransform(View child) { return null; }

	@Override public /* protected in C# */ final boolean getIsNativeMotionEventsEnabled() { return false; }
	// public /* protected in C# */ final void setIsNativeMotionEventsEnabled(boolean value) { }
	@Override public /* protected in C# */ boolean onNativeMotionEvent(MotionEvent event, View originalSource, boolean isInView) { return false; }

	private class TouchMotionTarget extends Uno.UI.TouchMotionTarget
	{
		protected TouchMotionTarget() { super(UnoRecyclerView.this); }
		@Override public boolean dispatchToSuper(MotionEvent event) { return Uno.UI.UnoRecyclerView.super.dispatchTouchEvent(event); }
	}

	private class GenericMotionTarget extends Uno.UI.GenericMotionTarget
	{
		protected GenericMotionTarget() { super(UnoRecyclerView.this); }
		@Override public boolean dispatchToSuper(MotionEvent event) { return Uno.UI.UnoRecyclerView.super.dispatchGenericMotionEvent(event); }
	}

	private final Uno.UI.MotionTargetAdapter _thisAsTouchTarget = new TouchMotionTarget();
	private final Uno.UI.MotionTargetAdapter _thisAsGenericMotionTarget = new GenericMotionTarget();

	@Override public final boolean dispatchTouchEvent(MotionEvent event) {
		return Uno.UI.UnoMotionHelper.Instance.dispatchMotionEvent(_thisAsTouchTarget, event);
	}
	@Override public final boolean dispatchGenericMotionEvent(MotionEvent event) {
		return Uno.UI.UnoMotionHelper.Instance.dispatchMotionEvent(_thisAsGenericMotionTarget, event);
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
}
