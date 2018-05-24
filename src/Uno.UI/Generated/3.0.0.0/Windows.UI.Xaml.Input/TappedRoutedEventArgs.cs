#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TappedRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TappedRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.TappedRoutedEventArgs", "bool TappedRoutedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType TappedRoutedEventArgs.PointerDeviceType is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public TappedRoutedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.TappedRoutedEventArgs", "TappedRoutedEventArgs.TappedRoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.TappedRoutedEventArgs.TappedRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.TappedRoutedEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Xaml.Input.TappedRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.TappedRoutedEventArgs.Handled.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point GetPosition( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member Point TappedRoutedEventArgs.GetPosition(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
	}
}
