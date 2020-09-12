#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicCameraRenderingParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface Direct3D11BackBuffer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDirect3DSurface HolographicCameraRenderingParameters.Direct3D11BackBuffer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice Direct3D11Device
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDirect3DDevice HolographicCameraRenderingParameters.Direct3D11Device is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicReprojectionMode ReprojectionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicReprojectionMode HolographicCameraRenderingParameters.ReprojectionMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "HolographicReprojectionMode HolographicCameraRenderingParameters.ReprojectionMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsContentProtectionEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicCameraRenderingParameters.IsContentProtectionEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "bool HolographicCameraRenderingParameters.IsContentProtectionEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetFocusPoint( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "void HolographicCameraRenderingParameters.SetFocusPoint(SpatialCoordinateSystem coordinateSystem, Vector3 position)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetFocusPoint( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position,  global::System.Numerics.Vector3 normal)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "void HolographicCameraRenderingParameters.SetFocusPoint(SpatialCoordinateSystem coordinateSystem, Vector3 position, Vector3 normal)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetFocusPoint( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position,  global::System.Numerics.Vector3 normal,  global::System.Numerics.Vector3 linearVelocity)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "void HolographicCameraRenderingParameters.SetFocusPoint(SpatialCoordinateSystem coordinateSystem, Vector3 position, Vector3 normal, Vector3 linearVelocity)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.Direct3D11Device.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.Direct3D11BackBuffer.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.ReprojectionMode.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.ReprojectionMode.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CommitDirect3D11DepthBuffer( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraRenderingParameters", "void HolographicCameraRenderingParameters.CommitDirect3D11DepthBuffer(IDirect3DSurface value)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.IsContentProtectionEnabled.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraRenderingParameters.IsContentProtectionEnabled.set
	}
}
