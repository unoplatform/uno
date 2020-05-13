#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationCompletedEventArgs 
	{
		// Skipping already declared property Cumulative
		// Skipping already declared property PointerDeviceType
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.ManipulationVelocities Velocities
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationVelocities ManipulationCompletedEventArgs.Velocities is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.ManipulationCompletedEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Input.ManipulationCompletedEventArgs.Position.get
		// Forced skipping of method Windows.UI.Input.ManipulationCompletedEventArgs.Cumulative.get
		// Forced skipping of method Windows.UI.Input.ManipulationCompletedEventArgs.Velocities.get
	}
}
