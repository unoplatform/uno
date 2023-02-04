#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialTappedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceKind InteractionSourceKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSourceKind SpatialTappedEventArgs.InteractionSourceKind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialInteractionSourceKind%20SpatialTappedEventArgs.InteractionSourceKind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint TapCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SpatialTappedEventArgs.TapCount is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20SpatialTappedEventArgs.TapCount");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialTappedEventArgs.InteractionSourceKind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialPointerPose TryGetPointerPose( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialPointerPose SpatialTappedEventArgs.TryGetPointerPose(SpatialCoordinateSystem coordinateSystem) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpatialPointerPose%20SpatialTappedEventArgs.TryGetPointerPose%28SpatialCoordinateSystem%20coordinateSystem%29");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialTappedEventArgs.TapCount.get
	}
}
