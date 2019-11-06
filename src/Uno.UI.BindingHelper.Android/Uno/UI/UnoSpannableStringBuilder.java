package Uno.UI;

import android.view.*;

public class UnoSpannableStringBuilder
	extends android.text.SpannableStringBuilder {
	
	public UnoSpannableStringBuilder(String text)
	{
		super(text);
	}

	public final int length() {
		return super.length();
	}

	public final void setSpan(Object what, int start, int end, int flags){
		super.setSpan(what, start, end, flags);
	}
}
