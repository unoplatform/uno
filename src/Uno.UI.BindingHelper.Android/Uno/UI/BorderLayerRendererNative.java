package Uno.UI;

import android.graphics.*;
import android.graphics.drawable.*;
import android.graphics.drawable.shapes.*;

public final class BorderLayerRendererNative
{
	private static Paint.Style paintStyleFillStroke = Paint.Style.FILL_AND_STROKE;
	
	private BorderLayerRendererNative() {
	}

	public static void buildBorderCornerRadius(
		PaintDrawable drawable,
		Path borderPath,
		Paint strokePaint,
		float width,
		float height)
	{
		drawable.setShape(new PathShape(borderPath, width, height));

		Paint paint = drawable.getPaint();
		paint.setAlpha(strokePaint.getAlpha());
		paint.setColor(strokePaint.getColor());
		paint.setShader(strokePaint.getShader());
		paint.setStyle(paintStyleFillStroke);
	}
}
