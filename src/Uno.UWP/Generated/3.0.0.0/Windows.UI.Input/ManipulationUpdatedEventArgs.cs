#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManipulationUpdatedEventArgs 
	{
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.ManipulationDelta Cumulative
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationDelta ManipulationUpdatedEventArgs.Cumulative is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.ManipulationDelta Delta
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManipulationDelta ManipulationUpdatedEventArgs.Delta is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDeviceType PointerDeviceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDeviceType ManipulationUpdatedEventArgs.PointerDeviceType is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point ManipulationUpdatedEventArgs.Position is not implemented in Uno.");
			}
		}
		#endif
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
