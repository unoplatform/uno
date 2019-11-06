package Uno.UI;

import android.graphics.Matrix;
import android.view.*;
import android.view.animation.Transformation;
import android.util.Log;

import java.lang.*;
import java.lang.reflect.*;
import java.util.ArrayList;
import java.util.Map;
import java.util.HashMap;

public abstract class UnoViewGroup
	extends android.view.ViewGroup
	implements Uno.UI.UnoMotionTarget {

	private static final String LOGTAG = "UnoViewGroup";
	private static boolean _isLayoutingFromMeasure = false;
	private static ArrayList<UnoViewGroup> callToRequestLayout = new ArrayList<UnoViewGroup>();

	private boolean _inLocalAddView, _inLocalRemoveView;
	private boolean _isEnabled;
	private boolean _isHitTestVisible;

	private boolean _isManagedLoaded;
	private boolean _needsLayoutOnAttachedToWindow;

	private Map<View, Matrix> _childrenTransformations = new HashMap<View, Matrix>();

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

		setClipChildren(false); // The actual clipping will be calculated in managed code
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

	private class TouchMotionTarget extends Uno.UI.TouchMotionTarget
	{
		TouchMotionTarget() { super(UnoViewGroup.this); }
		@Override public boolean dispatchToSuper(MotionEvent event) { return Uno.UI.UnoViewGroup.super.dispatchTouchEvent(event); }
	}

	private class GenericMotionTarget extends Uno.UI.GenericMotionTarget
	{
		GenericMotionTarget() { super(UnoViewGroup.this); }
		@Override public boolean dispatchToSuper(MotionEvent event) { return Uno.UI.UnoViewGroup.super.dispatchGenericMotionEvent(event); }
	}

	private final Uno.UI.MotionTargetAdapter _thisAsTouchTarget = new TouchMotionTarget();
	private final Uno.UI.MotionTargetAdapter _thisAsGenericMotionTarget = new GenericMotionTarget();

	@Override public /* TODO: final */ boolean dispatchTouchEvent(MotionEvent event) {
		return Uno.UI.UnoMotionHelper.Instance.dispatchMotionEvent(_thisAsTouchTarget, event);
	}
	@Override public final boolean dispatchGenericMotionEvent(MotionEvent event) {
		return Uno.UI.UnoMotionHelper.Instance.dispatchMotionEvent(_thisAsGenericMotionTarget, event);
	}

	private boolean _isNativeMotionEventsInterceptForbidden = false;
	@Override public /* protected in C# */ final boolean getIsNativeMotionEventsInterceptForbidden(){ return _isNativeMotionEventsInterceptForbidden; }
	public /* protected in C# */ final void setIsNativeMotionEventsInterceptForbidden(boolean isNativeMotionEventsInterceptForbidden){ _isNativeMotionEventsInterceptForbidden = isNativeMotionEventsInterceptForbidden; }

	private boolean _isNativeMotionEventsEnabled = true;
	@Override public /* protected in C# */ final boolean getIsNativeMotionEventsEnabled(){ return _isNativeMotionEventsEnabled; }
	public /* protected in C# */ final void setIsNativeMotionEventsEnabled(boolean isNativeMotionEventsEnabled){ _isNativeMotionEventsEnabled = isNativeMotionEventsEnabled; }

	@Override public /* protected in C# */ boolean onNativeMotionEvent(MotionEvent event, View originalSource, boolean isInView) {
		return false;
	}

	public final void setNativeIsHitTestVisible(boolean hitTestVisible) { _isHitTestVisible = hitTestVisible; }
	public /* hidden to C# */ final boolean getNativeIsHitTestVisible() { return _isHitTestVisible; }

	public final void setNativeIsEnabled(boolean isEnabled) { _isEnabled = isEnabled; }
	public /* hidden to C# */ final boolean getNativeIsEnabled() { return _isEnabled; }

	public /* protected in C# */ abstract boolean nativeHitCheck();

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
	 * Sets the static transform matrix to apply to the given child view.
	 * This will be used by the {@link #getChildStaticTransformation(View, Transformation)}
	 *
	 * @param child The view to which the matrix applies.
	 * @param transform The transformation matrix to apply.
	 */
	protected final void setChildRenderTransform(View child, Matrix transform) {
		_childrenTransformations.put(child, transform);
		if (_childrenTransformations.size() == 1) {
			setStaticTransformationsEnabled(true);
		}
	}

	/**
	 * Removes the static transform matrix applied to the given child view.
	 *
	 * @param child The view to which the matrix applies.
	 */
	protected final void removeChildRenderTransform(View child) {
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

	public /* hidden to C# */ int getChildrenRenderTransformCount() { return _childrenTransformations.size(); }
	public /* hidden to C# */ Matrix findChildRenderTransform(View child) { return _childrenTransformations.get(child); }


	@Override
	public void getLocationInWindow(int[] outLocation) {
		super.getLocationInWindow(outLocation);
		ViewParent currentParent = getParent();
		View currentChild = this;

		float[] points = null;
		while (currentParent instanceof View) {
			if (currentParent instanceof UnoViewGroup) {
				final UnoViewGroup currentUVGParent = (UnoViewGroup)currentParent;
				Matrix parentMatrix = currentUVGParent.findChildRenderTransform(currentChild);
				if (parentMatrix != null && !parentMatrix.isIdentity()) {
					if (points == null) {
						points = new float[2];
					}

					// Apply the offset from the ancestor's RenderTransform, because the base Android method doesn't take
					// StaticTransformation into account.
					Matrix inverse = new Matrix();
					parentMatrix.invert(inverse);
					inverse.mapPoints(points);
				}
			}

			currentChild = (View)currentParent;
			currentParent = currentParent.getParent();
		}

		if (points != null) {
			outLocation[0]-=(int)points[0];
			outLocation[1]-=(int)points[1];
		}
	}	
	
	// Allows UI automation operations to look for a single 'Text' property for both ViewGroup and TextView elements.
	// Is mapped to the UIAutomationText property
	public String getText() {
		return null;
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
