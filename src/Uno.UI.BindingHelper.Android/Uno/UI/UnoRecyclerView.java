package Uno.UI;

import android.content.Context;
import android.graphics.Matrix;
import androidx.recyclerview.widget.RecyclerView;
import android.view.*;

import java.lang.*;

public abstract class UnoRecyclerView
	extends RecyclerView {

	protected UnoRecyclerView(Context context) {
		super(context);
	}

	@Override
	public void removeViewAt(int index) {
		View child = getChildAt(index);
		super.removeViewAt(index);
		notifyChildRemoved(child);
	}

	@Override
	public void removeView(View view) {
		super.removeView(view);
		notifyChildRemoved(view);
	}

	private void notifyChildRemoved(View child)
	{
		if(child instanceof Uno.UI.UnoViewGroup)
		{
			// This is required because the Parent property is set to null
			// after the onDetachedFromWindow is called.
			((Uno.UI.UnoViewGroup)child).onRemovedFromParent();
		}
	}
}
