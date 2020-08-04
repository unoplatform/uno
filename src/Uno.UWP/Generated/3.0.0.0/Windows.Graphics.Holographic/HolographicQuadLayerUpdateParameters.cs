#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicQuadLayerUpdateParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanAcquireWithHardwareProtection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicQuadLayerUpdateParameters.CanAcquireWithHardwareProtection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface AcquireBufferToUpdateContent()
		{
			throw new global::System.NotImplementedException("The member IDirect3DSurface HolographicQuadLayerUpdateParameters.AcquireBufferToUpdateContent() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateViewport( global::Windows.Foundation.Rect value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters", "void HolographicQuadLayerUpdateParameters.UpdateViewport(Rect value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateContentProtectionEnabled( bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters", "void HolographicQuadLayerUpdateParameters.UpdateContentProtectionEnabled(bool value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateExtents( global::System.Numerics.Vector2 value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters", "void HolographicQuadLayerUpdateParameters.UpdateExtents(Vector2 value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateLocationWithStationaryMode( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position,  global::System.Numerics.Quaternion orientation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters", "void HolographicQuadLayerUpdateParameters.UpdateLocationWithStationaryMode(SpatialCoordinateSystem coordinateSystem, Vector3 position, Quaternion orientation)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UpdateLocationWithDisplayRelativeMode( global::System.Numerics.Vector3 position,  global::System.Numerics.Quaternion orientation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters", "void HolographicQuadLayerUpdateParameters.UpdateLocationWithDisplayRelativeMode(Vector3 position, Quaternion orientation)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicQuadLayerUpdateParameters.CanAcquireWithHardwareProtection.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface AcquireBufferToUpdateContentWithHardwareProtection()
		{
			throw new global::System.NotImplementedException("The member IDirect3DSurface HolographicQuadLayerUpdateParameters.AcquireBufferToUpdateContentWithHardwareProtection() is not implemented in Uno.");
		}
		#endif
	}
}
