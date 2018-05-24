#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.PointerRoutedEventArgs", "bool PointerRoutedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.System.VirtualKeyModifiers KeyModifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKeyModifiers PointerRoutedEventArgs.KeyModifiers is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Input.Pointer Pointer
		{
			get
			{
				throw new global::System.NotImplementedException("The member Pointer PointerRoutedEventArgs.Pointer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsGenerated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerRoutedEventArgs.IsGenerated is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Pointer.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.KeyModifiers.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.Handled.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.PointerPoint GetCurrentPoint( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerRoutedEventArgs.GetCurrentPoint(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerRoutedEventArgs.GetIntermediatePoints(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.PointerRoutedEventArgs.IsGenerated.get
	}
}
