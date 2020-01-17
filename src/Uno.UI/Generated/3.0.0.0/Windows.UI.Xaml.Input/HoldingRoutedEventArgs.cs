#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial class HoldingRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HoldingRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.HoldingRoutedEventArgs", "bool HoldingRoutedEventArgs.Handled");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.HoldingState HoldingState
		{
			get
			{
				throw new global::System.NotImplementedException("The member HoldingState HoldingRoutedEventArgs.HoldingState is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType HoldingRoutedEventArgs.PointerDeviceType is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public HoldingRoutedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.HoldingRoutedEventArgs", "HoldingRoutedEventArgs.HoldingRoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.HoldingRoutedEventArgs.HoldingRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.HoldingRoutedEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Xaml.Input.HoldingRoutedEventArgs.HoldingState.get
		// Forced skipping of method Windows.UI.Xaml.Input.HoldingRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.HoldingRoutedEventArgs.Handled.set
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point GetPosition( global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member Point HoldingRoutedEventArgs.GetPosition(UIElement relativeTo) is not implemented in Uno.");
		}
		#endif
	}
}
