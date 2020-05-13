#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationUpdatedEventArgs 
	{
		// Skipping already declared property Cumulative
		// Skipping already declared property Delta
		// Skipping already declared property PointerDeviceType
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.ManipulationVelocities Velocities
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationVelocities ManipulationUpdatedEventArgs.Velocities is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.ManipulationUpdatedEventArgs.PointerDeviceType.get
		// Forced skipping of method Windows.UI.Input.ManipulationUpdatedEventArgs.Position.get
		// Forced skipping of method Windows.UI.Input.ManipulationUpdatedEventArgs.Delta.get
		// Forced skipping of method Windows.UI.Input.ManipulationUpdatedEventArgs.Cumulative.get
		// Forced skipping of method Windows.UI.Input.ManipulationUpdatedEventArgs.Velocities.get
	}
}
