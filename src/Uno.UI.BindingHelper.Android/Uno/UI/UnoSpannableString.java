package Uno.UI;

import android.view.*;
import android.text.style.*;
import android.text.*;
import android.graphics.*;

public class UnoSpannableString
	extends android.text.SpannableString {

	public static UnoSpannableString BuildFromString(String text){
		return new UnoSpannableString(text);
	}
	
	public UnoSpannableString(String text)
	{
		super(text);
	}

	public final void setPaintSpan(TextPaint paint, int start, int end)
	{
		super.setSpan(new TextPaintSpan(paint), start, end, Spanned.SPAN_MARK_MARK);
	}

	public final void setSpan(Object what, int start, int end, int flags) {

		super.setSpan(what, start, end, flags);
	}
}