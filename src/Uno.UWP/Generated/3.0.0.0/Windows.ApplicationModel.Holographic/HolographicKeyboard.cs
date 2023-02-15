#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicKeyboard 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPlacementOverride( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 topCenterPosition,  global::System.Numerics.Quaternion orientation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Holographic.HolographicKeyboard", "void HolographicKeyboard.SetPlacementOverride(SpatialCoordinateSystem coordinateSystem, Vector3 topCenterPosition, Quaternion orientation)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPlacementOverride( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 topCenterPosition,  global::System.Numerics.Quaternion orientation,  global::System.Numerics.Vector2 maxSize)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Holographic.HolographicKeyboard", "void HolographicKeyboard.SetPlacementOverride(SpatialCoordinateSystem coordinateSystem, Vector3 topCenterPosition, Quaternion orientation, Vector2 maxSize)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ResetPlacementOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Holographic.HolographicKeyboard", "void HolographicKeyboard.ResetPlacementOverride()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Holographic.HolographicKeyboard GetDefault()
		{
			throw new global::System.NotImplementedException("The member HolographicKeyboard HolographicKeyboard.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HolographicKeyboard%20HolographicKeyboard.GetDefault%28%29");
		}
		#endif
	}
}
