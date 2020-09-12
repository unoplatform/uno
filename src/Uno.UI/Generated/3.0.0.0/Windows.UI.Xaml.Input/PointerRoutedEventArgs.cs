#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		// Skipping already declared property Handled
		// Skipping already declared property KeyModifiers
		// Skipping already declared property Pointer
		// Skipping already declared property IsGenerated
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Pointer.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.KeyModifiers.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Handled.set
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public  global::Windows.UI.Input.PointerPoint GetCurrentPoint( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerRoutedEventArgs.GetCurrentPoint(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Input.PointerRoutedEventArgs.GetIntermediatePoints(Windows.UI.Xaml.UIElement)
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.IsGenerated.get
	}
}
