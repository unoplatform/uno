#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DoubleTappedRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DoubleTappedRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs", "bool DoubleTappedRoutedEventArgs.Handled");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType DoubleTappedRoutedEventArgs.PointerDeviceType is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DoubleTappedRoutedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs", "DoubleTappedRoutedEventArgs.DoubleTappedRoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs.DoubleTappedRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs.Handled.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point GetPosition( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member Point DoubleTappedRoutedEventArgs.GetPosition(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
	}
}
