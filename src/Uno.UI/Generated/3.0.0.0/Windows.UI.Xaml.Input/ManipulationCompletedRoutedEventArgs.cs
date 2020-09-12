#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationCompletedRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		// Skipping already declared property Handled
		// Skipping already declared property Container
		// Skipping already declared property Cumulative
		// Skipping already declared property IsInertial
		// Skipping already declared property PointerDeviceType
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.ManipulationVelocities Velocities
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationVelocities ManipulationCompletedRoutedEventArgs.Velocities is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.ManipulationCompletedRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.ManipulationCompletedRoutedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Container.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Position.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.IsInertial.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Cumulative.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Velocities.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs.PointerDeviceType.get
	}
}
