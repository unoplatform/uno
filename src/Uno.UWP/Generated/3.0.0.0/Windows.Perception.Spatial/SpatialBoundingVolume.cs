#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialBoundingVolume 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialBoundingVolume FromBox( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::Windows.Perception.Spatial.SpatialBoundingBox box)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingVolume SpatialBoundingVolume.FromBox(SpatialCoordinateSystem coordinateSystem, SpatialBoundingBox box) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialBoundingVolume FromOrientedBox( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::Windows.Perception.Spatial.SpatialBoundingOrientedBox box)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingVolume SpatialBoundingVolume.FromOrientedBox(SpatialCoordinateSystem coordinateSystem, SpatialBoundingOrientedBox box) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialBoundingVolume FromSphere( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::Windows.Perception.Spatial.SpatialBoundingSphere sphere)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingVolume SpatialBoundingVolume.FromSphere(SpatialCoordinateSystem coordinateSystem, SpatialBoundingSphere sphere) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialBoundingVolume FromFrustum( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::Windows.Perception.Spatial.SpatialBoundingFrustum frustum)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingVolume SpatialBoundingVolume.FromFrustum(SpatialCoordinateSystem coordinateSystem, SpatialBoundingFrustum frustum) is not implemented in Uno.");
		}
		#endif
	}
}
