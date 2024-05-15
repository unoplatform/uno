package Uno.UI;

import android.graphics.*;
import android.text.*;

public final class TextPaintPoolNative {

	private TextPaintPoolNative() { 
	}

	public static TextPaint buildPaint(
		float density,
		float size,
		float letterSpacing,
		Typeface typeface,
		int color,
		boolean underline,
		boolean strikethrough,
		boolean superscript,
		Shader shader)
	{
		TextPaint paint = new TextPaint(Paint.ANTI_ALIAS_FLAG);
		paint.density = density;
		paint.setTextSize(size);
		paint.setUnderlineText(underline);
		paint.setStrikeThruText(strikethrough);

		if (shader != null) {
			paint.setShader(shader);
		}
		else {
			paint.setColor(color);
		}

		if (superscript) {
			paint.baselineShift += (int)(paint.ascent() / 2);
		}

		paint.setLetterSpacing(letterSpacing);
		paint.setTypeface(typeface);

		return paint;
	}
}
