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

	// The "current target" is used by the custom dispatch logic to track the last child to which an event was sent
	// This is required in order to make sure to follow the full events sequences: down->move->up OR enter->move->exit
	// (For instance on a HOVER_MOVE, if the pointer goes out of this "current target" we still make sure to
	// send it to this "current target" the event in order to raise the HOVER_EXIT)
	private View _currentTarget;
	final View getCurrentTarget() { return _currentTarget; }
	final void setCurrentTarget(View child) { _currentTarget = child; }

	// specific to the raised event (generic vs touch)

	// The "strong sequence" means that a full event sequence (down -> move -> up) MUST be raised
	// on the same element. This behavior a.k.a. "implicit capture" is not the UWP one, but the Android behavior.
	// It will be "patched" to follow the UWP behavior in the managed code (cf. UIElement.CapturePointer).
	abstract boolean getIsStrongSequence();
	abstract boolean dispatchToSuper(MotionEvent event);
	abstract boolean dispatchToChild(View child, MotionEvent event);
}

/* internal */ abstract class TouchMotionTarget extends Uno.UI.MotionTargetAdapter {

	protected TouchMotionTarget(Uno.UI.UnoMotionTarget target) {
		super(target);
	}

	@Override final boolean getIsStrongSequence() { return true; }
	@Override final boolean dispatchToChild(View view, MotionEvent event) { return view.dispatchTouchEvent(event); }
}

/* internal */ abstract class GenericMotionTarget extends Uno.UI.MotionTargetAdapter {

	protected GenericMotionTarget(Uno.UI.UnoMotionTarget target) {
		super(target);
	}

	@Override final boolean getIsStrongSequence() { return false; }
	@Override final boolean dispatchToChild(View view, MotionEvent event) { return view.dispatchGenericMotionEvent(event); }
}

/* internal */ final class UnoMotionHelper {
	private static final String LOGTAG = "UnoViewGroup";

	// Stylus when barrel is pressed when touching the screen
	private static final int STYLUS_WITH_BARREL_DOWN = 211;
	private static final int STYLUS_WITH_BARREL_MOVE = 213;
	private static final int STYLUS_WITH_BARREL_UP = 212;

	/**
	 * The singleton instance of the helper
	 */
	public static UnoMotionHelper Instance = new UnoMotionHelper();
	private UnoMotionHelper() {}

	public boolean dispatchMotionEvent(Uno.UI.MotionTargetAdapter adapter, MotionEvent event)
	{

		final ViewGroup view = adapter.asViewGroup();
		final Uno.UI.UnoMotionTarget target = adapter.asMotionTarget();

		if (isMotionSupportedByManaged(event)) {
			target.onNativeMotionEvent(event);
		}

		return true;
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
			case STYLUS_WITH_BARREL_DOWN:
			case STYLUS_WITH_BARREL_MOVE:
			case STYLUS_WITH_BARREL_UP:
				return true;
			default:
				return false;
		}
	}
}
