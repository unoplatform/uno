#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Preview.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicKeyboardPlacementOverridePreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPlacementOverride( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 topCenterPosition,  global::System.Numerics.Vector3 normal)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Preview.Holographic.HolographicKeyboardPlacementOverridePreview", "void HolographicKeyboardPlacementOverridePreview.SetPlacementOverride(SpatialCoordinateSystem coordinateSystem, Vector3 topCenterPosition, Vector3 normal)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPlacementOverride( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 topCenterPosition,  global::System.Numerics.Vector3 normal,  global::System.Numerics.Vector2 maxSize)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Preview.Holographic.HolographicKeyboardPlacementOverridePreview", "void HolographicKeyboardPlacementOverridePreview.SetPlacementOverride(SpatialCoordinateSystem coordinateSystem, Vector3 topCenterPosition, Vector3 normal, Vector2 maxSize)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ResetPlacementOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Preview.Holographic.HolographicKeyboardPlacementOverridePreview", "void HolographicKeyboardPlacementOverridePreview.ResetPlacementOverride()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Preview.Holographic.HolographicKeyboardPlacementOverridePreview GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member HolographicKeyboardPlacementOverridePreview HolographicKeyboardPlacementOverridePreview.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
	}
}
