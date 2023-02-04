#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialManipulationUpdatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceKind InteractionSourceKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSourceKind SpatialManipulationUpdatedEventArgs.InteractionSourceKind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialInteractionSourceKind%20SpatialManipulationUpdatedEventArgs.InteractionSourceKind");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialManipulationUpdatedEventArgs.InteractionSourceKind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialManipulationDelta TryGetCumulativeDelta( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialManipulationDelta SpatialManipulationUpdatedEventArgs.TryGetCumulativeDelta(SpatialCoordinateSystem coordinateSystem) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialManipulationDelta%20SpatialManipulationUpdatedEventArgs.TryGetCumulativeDelta%28SpatialCoordinateSystem%20coordinateSystem%29");
		}
		#endif
	}
}
