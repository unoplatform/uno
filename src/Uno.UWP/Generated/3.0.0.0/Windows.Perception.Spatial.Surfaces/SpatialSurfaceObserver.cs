#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial.Surfaces
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialSurfaceObserver 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SpatialSurfaceObserver() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver", "SpatialSurfaceObserver.SpatialSurfaceObserver()");
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver.SpatialSurfaceObserver()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, global::Windows.Perception.Spatial.Surfaces.SpatialSurfaceInfo> GetObservedSurfaces()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, SpatialSurfaceInfo> SpatialSurfaceObserver.GetObservedSurfaces() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBoundingVolume( global::Windows.Perception.Spatial.SpatialBoundingVolume bounds)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver", "void SpatialSurfaceObserver.SetBoundingVolume(SpatialBoundingVolume bounds)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBoundingVolumes( global::System.Collections.Generic.IEnumerable<global::Windows.Perception.Spatial.SpatialBoundingVolume> bounds)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver", "void SpatialSurfaceObserver.SetBoundingVolumes(IEnumerable<SpatialBoundingVolume> bounds)");
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver.ObservedSurfacesChanged.add
		// Forced skipping of method Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver.ObservedSurfacesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool SpatialSurfaceObserver.IsSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Perception.Spatial.SpatialPerceptionAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpatialPerceptionAccessStatus> SpatialSurfaceObserver.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver, object> ObservedSurfacesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver", "event TypedEventHandler<SpatialSurfaceObserver, object> SpatialSurfaceObserver.ObservedSurfacesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.Surfaces.SpatialSurfaceObserver", "event TypedEventHandler<SpatialSurfaceObserver, object> SpatialSurfaceObserver.ObservedSurfacesChanged");
			}
		}
		#endif
	}
}
