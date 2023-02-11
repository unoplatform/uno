#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialInteractionSourceProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double SourceLossRisk
		{
			get
			{
				throw new global::System.NotImplementedException("The member double SpatialInteractionSourceProperties.SourceLossRisk is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20SpatialInteractionSourceProperties.SourceLossRisk");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3? TryGetSourceLossMitigationDirection( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member Vector3? SpatialInteractionSourceProperties.TryGetSourceLossMitigationDirection(SpatialCoordinateSystem coordinateSystem) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Vector3%3F%20SpatialInteractionSourceProperties.TryGetSourceLossMitigationDirection%28SpatialCoordinateSystem%20coordinateSystem%29");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionSourceProperties.SourceLossRisk.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceLocation TryGetLocation( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialInteractionSourceLocation SpatialInteractionSourceProperties.TryGetLocation(SpatialCoordinateSystem coordinateSystem) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialInteractionSourceLocation%20SpatialInteractionSourceProperties.TryGetLocation%28SpatialCoordinateSystem%20coordinateSystem%29");
		}
		#endif
	}
}
