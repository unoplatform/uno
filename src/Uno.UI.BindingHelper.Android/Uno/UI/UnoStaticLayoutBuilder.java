package Uno.UI;

import android.view.*;
import android.text.*;
import android.graphics.Bitmap;
import android.graphics.Paint;
import android.text.style.LeadingMarginSpan;
import android.text.style.LeadingMarginSpan.LeadingMarginSpan2;
import android.text.style.LineHeightSpan;
import android.text.style.MetricAffectingSpan;
import android.text.style.TabStopSpan;
import android.util.Log;
import java.lang.*;
import java.lang.reflect.*;

public class UnoStaticLayoutBuilder {

	static Constructor staticLayoutConstructor;
	static TextDirectionHeuristic textDirectionHeuristic;

	static {
		try {
			buildStaticLayoutReflection();
			buildTextDirection();
		}
		catch(Exception e) { 
			Log.e("UmbrellaStaticLayoutBuilder", "Failed to initialize StaticLayout builder. " + e.toString());
		}
	}

	private static void buildStaticLayoutReflection() throws ClassNotFoundException, InstantiationException, Exception
	{
		Class staticLayoutClass = Class.forName("android.text.StaticLayout");
		Constructor[] ctors = staticLayoutClass.getDeclaredConstructors();

		for(int i=0; i<ctors.length; i++) {
			if(ctors[i].getParameterTypes().length == 13){
				staticLayoutConstructor = ctors[i];
				staticLayoutConstructor.setAccessible(true);
			}
		}

		if(staticLayoutConstructor == null) {
			throw new Exception("Unable to find StaticLayout constructor.");
		}
	}

	private static void buildTextDirection() throws ClassNotFoundException, NoSuchFieldException, IllegalAccessException {
		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.KITKAT) {
			textDirectionHeuristic = TextDirectionHeuristics.FIRSTSTRONG_LTR;
		}
		else {
			// This is required because this class was not exposed until API 18 but available before.
			Class textDirectionHeuristicsClass = java.lang.Class.forName("android.text.TextDirectionHeuristics");
			Field firstStrongField = textDirectionHeuristicsClass.getField("FIRSTSTRONG_LTR");

			textDirectionHeuristic = (TextDirectionHeuristic)firstStrongField.get(null);
		}
	}

	public static StaticLayout Build(
		CharSequence source,
		TextPaint paint,
		int outerwidth,
		Layout.Alignment align,
		float spacingmult,
		float spacingadd,
		boolean includepad,
		TextUtils.TruncateAt ellipsize,
		int ellipsizedWidth,
		int maxLines
	) throws InstantiationException, IllegalAccessException, InvocationTargetException
	{
		// This method is used to avoid paying a very high cost of reflection interop 
		// in Xamarin.Android bindings.

		return (StaticLayout)staticLayoutConstructor.newInstance(
			/*source:*/ source,
			/*bufstart: */ 0,
			/*bufend: */ source.length(),
			/*paint: */ paint,
			/*outerwidth: */ outerwidth,
			/*align: */ align,
			/*textDir: */ textDirectionHeuristic,
			/*spacingmult:*/  spacingmult,
			/*spacingadd: */ spacingadd,
			/*includepad:*/  includepad,
			/*ellipsize: */ ellipsize,
			/*ellipsizedWidth: */ ellipsizedWidth,
			/*maxLines: */ maxLines
		);
	}
	
}