package Uno.UI;

import android.graphics.*;

public final class UIElementNative
{
	private UIElementNative() {
	}

	public static void adjustCornerRadius(Canvas canvas, float[] radii)
	{
		Path clipPath = new Path();

		clipPath.addRoundRect(new RectF(canvas.getClipBounds()), radii, Path.Direction.CW);

		canvas.clipPath(clipPath);
	}
}
