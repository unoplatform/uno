using Windows.Foundation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerRoutedEventArgs.GetCurrentPoint(UIElement relativeTo) is not implemented in Uno.");
		}
	}
}
