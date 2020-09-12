#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial.Surfaces
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialSurfaceInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid SpatialSurfaceInfo.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset UpdateTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset SpatialSurfaceInfo.UpdateTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.Surfaces.SpatialSurfaceInfo.Id.get
		// Forced skipping of method Windows.Perception.Spatial.Surfaces.SpatialSurfaceInfo.UpdateTime.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialBoundingOrientedBox? TryGetBounds( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialBoundingOrientedBox? SpatialSurfaceInfo.TryGetBounds(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.Surfaces.SpatialSurfaceMesh> TryComputeLatestMeshAsync( double maxTrianglesPerCubicMeter)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialSurfaceMesh> SpatialSurfaceInfo.TryComputeLatestMeshAsync(double maxTrianglesPerCubicMeter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.Surfaces.SpatialSurfaceMesh> TryComputeLatestMeshAsync( double maxTrianglesPerCubicMeter,  global::Windows.Perception.Spatial.Surfaces.SpatialSurfaceMeshOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialSurfaceMesh> SpatialSurfaceInfo.TryComputeLatestMeshAsync(double maxTrianglesPerCubicMeter, SpatialSurfaceMeshOptions options) is not implemented in Uno.");
		}
		#endif
	}
}
