package Uno.UI;

import android.view.*;
import android.graphics.*;
import android.text.*;
import android.util.*;
import android.content.res.*;

public class TextPaintSpan
	extends android.text.style.MetricAffectingSpan {

	private TextPaint _paint;

	public TextPaintSpan(TextPaint paint)
	{ 
		_paint = paint;
	}

	public final void updateDrawState(TextPaint tp)
	{
		apply(tp);
	}

	public final void updateMeasureState(TextPaint p)
	{
		apply(p);
	}

	private void apply(TextPaint paint)
	{
		paint.setAntiAlias(true);
		paint.setTypeface(_paint.getTypeface());
		paint.setTextSize(_paint.getTextSize());
		paint.density = _paint.density;		
		paint.setUnderlineText(_paint.isUnderlineText());
		paint.setStrikeThruText(_paint.isStrikeThruText());
		paint.setColor(_paint.getColor());
		paint.baselineShift = _paint.baselineShift;

		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.LOLLIPOP) {
			// CharacterSpacing is only supported on Android API Level 21+
			paint.setLetterSpacing(_paint.getLetterSpacing());
		}
	}
}