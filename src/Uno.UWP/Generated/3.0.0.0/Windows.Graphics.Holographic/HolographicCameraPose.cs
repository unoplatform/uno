#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicCameraPose 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double FarPlaneDistance
		{
			get
			{
				throw new global::System.NotImplementedException("The member double HolographicCameraPose.FarPlaneDistance is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicCamera HolographicCamera
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicCamera HolographicCameraPose.HolographicCamera is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double NearPlaneDistance
		{
			get
			{
				throw new global::System.NotImplementedException("The member double HolographicCameraPose.NearPlaneDistance is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicStereoTransform ProjectionTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicStereoTransform HolographicCameraPose.ProjectionTransform is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect Viewport
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect HolographicCameraPose.Viewport is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraPose.HolographicCamera.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraPose.Viewport.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicStereoTransform? TryGetViewTransform( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member HolographicStereoTransform? HolographicCameraPose.TryGetViewTransform(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraPose.ProjectionTransform.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialBoundingFrustum? TryGetCullingFrustum( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingFrustum? HolographicCameraPose.TryGetCullingFrustum(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialBoundingFrustum? TryGetVisibleFrustum( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingFrustum? HolographicCameraPose.TryGetVisibleFrustum(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraPose.NearPlaneDistance.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraPose.FarPlaneDistance.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OverrideViewTransform( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::Windows.Graphics.Holographic.HolographicStereoTransform coordinateSystemToViewTransform)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraPose", "void HolographicCameraPose.OverrideViewTransform(SpatialCoordinateSystem coordinateSystem, HolographicStereoTransform coordinateSystemToViewTransform)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OverrideProjectionTransform( global::Windows.Graphics.Holographic.HolographicStereoTransform projectionTransform)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraPose", "void HolographicCameraPose.OverrideProjectionTransform(HolographicStereoTransform projectionTransform)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OverrideViewport( global::Windows.Foundation.Rect leftViewport,  global::Windows.Foundation.Rect rightViewport)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicCameraPose", "void HolographicCameraPose.OverrideViewport(Rect leftViewport, Rect rightViewport)");
		}
		#endif
	}
}
