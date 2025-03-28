using Uno.UI.Controls;

namespace Windows.UI.Xaml.Controls;

partial class NativeProgressRing : BindableProgressBar
{
	public NativeProgressRing()
	{
		// This is required to have multiple ProgressBar with different colors. 
		// Without this, changing one drawable would change all drawables (because they all have the same constant state).
		// http://stackoverflow.com/questions/7979440/android-cloning-a-drawable-in-order-to-make-a-statelistdrawable-with-filters
		IndeterminateDrawable.Mutate();
	}
}
