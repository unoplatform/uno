#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationDeltaRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		// Skipping already declared property Handled
		// Skipping already declared property Container
		// Skipping already declared property Cumulative
		// Skipping already declared property Delta
		// Skipping already declared property IsInertial
		// Skipping already declared property PointerDeviceType
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.ManipulationVelocities Velocities
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationVelocities ManipulationDeltaRoutedEventArgs.Velocities is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.ManipulationDeltaRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.ManipulationDeltaRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Container.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Position.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.IsInertial.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Delta.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Cumulative.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Velocities.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.PointerDeviceType.get
		// Skipping already declared method Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs.Complete()
	}
}
