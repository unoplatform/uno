package Uno.UI;

import android.view.*;
import android.content.Context;
import android.util.Log;

public class UnoScrollViewScaleGestureDetector extends ScaleGestureDetector {
	private UnoScaleGestureListener _listener;

	public UnoScrollViewScaleGestureDetector(Context context, UnoScaleGestureListener listener) {
		super(context, listener);
		_listener = listener;
	}

	@Override
	public boolean onTouchEvent(MotionEvent ev) {
		 // Log.w(this.toString(),"onTouchEvent: "+String.valueOf(_listener._isScaling));
		super.onTouchEvent(ev);
		return _listener._isScaling;
	}

	public static class UnoScaleGestureListener extends ScaleGestureDetector.SimpleOnScaleGestureListener {
			private UnoTwoDScrollView _targetScrollView;
			private float _initialScale;
			private float _initialOffsetX;
			private float _initialOffsetY;
			private boolean _isScaling;

			public UnoScaleGestureListener(UnoTwoDScrollView targetScrollView) {
				_targetScrollView = targetScrollView;
			}

			@Override
			public boolean onScale(ScaleGestureDetector detector) {
				 // Log.w(this.toString(),"onScale");

				float relativeZoom = detector.getScaleFactor();
				final float newZoom = _initialScale * relativeZoom;
				final float maxZoom = _targetScrollView.getMaximumZoomScale();
				final float minZoom = _targetScrollView.getMinimumZoomScale();
				if (newZoom > maxZoom) {
					relativeZoom *= maxZoom / newZoom;
				}
				else if (newZoom < minZoom) {
					relativeZoom *= minZoom / newZoom;
				}
				final float newOffsetX = calculateNewScrollOffset(relativeZoom, detector.getFocusX(), _initialOffsetX);
				final float newOffsetY = calculateNewScrollOffset(relativeZoom, detector.getFocusY(), _initialOffsetY);

				_targetScrollView.setZoomScale(newZoom);
				_targetScrollView.scrollTo((int)newOffsetX, (int)newOffsetY);

				return super.onScale(detector);
			}

			@Override
			public boolean onScaleBegin(ScaleGestureDetector detector) {
			 // Log.w(this.toString(),"onScaleBegin");

				final View content = _targetScrollView.getChildAt(0);
				if (content != null) {
					_initialScale = content.getScaleX();
				}
				_initialOffsetX = _targetScrollView.getScrollX();
				_initialOffsetY = _targetScrollView.getScrollY();

				_isScaling = true;

				return super.onScaleBegin(detector);
			}

			@Override
			public void onScaleEnd(ScaleGestureDetector detector) {
			 // Log.w(this.toString(),"onScaleEnd");
				_isScaling = false;
				super.onScaleEnd(detector);
			}

			private float clamp(float toClamp, float minValue, float maxValue) {
				return Math.max(Math.min(toClamp, maxValue), minValue);
			}

			private float calculateNewScrollOffset(float relativeZoom, float pinchCenter, float initialOffset) {
				return relativeZoom*(pinchCenter + initialOffset) - pinchCenter;
			}
	}
}