#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationCompletedEventArgs 
	{
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.ManipulationDelta Cumulative
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationDelta ManipulationCompletedEventArgs.Cumulative is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType ManipulationCompletedEventArgs.PointerDeviceType is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point ManipulationCompletedEventArgs.Position is not implemented in Uno.");
			}
		}
		#endif
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
