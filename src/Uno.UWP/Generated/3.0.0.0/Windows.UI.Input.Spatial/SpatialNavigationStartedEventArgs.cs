#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialNavigationStartedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialInteractionSourceKind InteractionSourceKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialInteractionSourceKind SpatialNavigationStartedEventArgs.InteractionSourceKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsNavigatingX
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialNavigationStartedEventArgs.IsNavigatingX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsNavigatingY
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialNavigationStartedEventArgs.IsNavigatingY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsNavigatingZ
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialNavigationStartedEventArgs.IsNavigatingZ is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialNavigationStartedEventArgs.InteractionSourceKind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Spatial.SpatialPointerPose TryGetPointerPose( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialPointerPose SpatialNavigationStartedEventArgs.TryGetPointerPose(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialNavigationStartedEventArgs.IsNavigatingX.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialNavigationStartedEventArgs.IsNavigatingY.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialNavigationStartedEventArgs.IsNavigatingZ.get
	}
}
