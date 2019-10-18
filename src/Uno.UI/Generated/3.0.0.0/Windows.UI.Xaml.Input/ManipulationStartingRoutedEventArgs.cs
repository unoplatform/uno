#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationStartingRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Input.ManipulationPivot Pivot
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationPivot ManipulationStartingRoutedEventArgs.Pivot is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", "ManipulationPivot ManipulationStartingRoutedEventArgs.Pivot");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Input.ManipulationModes Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationModes ManipulationStartingRoutedEventArgs.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", "ManipulationModes ManipulationStartingRoutedEventArgs.Mode");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ManipulationStartingRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", "bool ManipulationStartingRoutedEventArgs.Handled");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement Container
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElement ManipulationStartingRoutedEventArgs.Container is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", "UIElement ManipulationStartingRoutedEventArgs.Container");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public ManipulationStartingRoutedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", "ManipulationStartingRoutedEventArgs.ManipulationStartingRoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.ManipulationStartingRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Mode.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Mode.set
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Container.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Container.set
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Pivot.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Pivot.set
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs.Handled.set
	}
}
