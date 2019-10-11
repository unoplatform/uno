#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false
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
		// Skipping already declared method Windows.UI.Xaml.Input.PointerRoutedEventArgs.GetCurrentPoint(Windows.UI.Xaml.UIElement)
#if false 
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerRoutedEventArgs.GetIntermediatePoints(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.IsGenerated.get
	}
}
