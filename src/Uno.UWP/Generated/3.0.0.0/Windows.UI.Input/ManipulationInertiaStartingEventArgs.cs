#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationInertiaStartingEventArgs 
	{
		// Skipping already declared property Cumulative
		// Skipping already declared property Delta
		// Skipping already declared property PointerDeviceType
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.ManipulationVelocities Velocities
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationVelocities ManipulationInertiaStartingEventArgs.Velocities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ContactCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ManipulationInertiaStartingEventArgs.ContactCount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.Position.get
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.Delta.get
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.Cumulative.get
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.Velocities.get
		// Forced skipping of method Windows.UI.Input.ManipulationInertiaStartingEventArgs.ContactCount.get
	}
}
