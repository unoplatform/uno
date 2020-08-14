#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialAnchor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem CoordinateSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialCoordinateSystem SpatialAnchor.CoordinateSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem RawCoordinateSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialCoordinateSystem SpatialAnchor.RawCoordinateSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool RemovedByUser
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialAnchor.RemovedByUser is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchor.CoordinateSystem.get
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchor.RawCoordinateSystem.get
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchor.RawCoordinateSystemAdjusted.add
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchor.RawCoordinateSystemAdjusted.remove
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchor.RemovedByUser.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialAnchor TryCreateRelativeTo( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem)
		{
			throw new global::System.NotImplementedException("The member SpatialAnchor SpatialAnchor.TryCreateRelativeTo(SpatialCoordinateSystem coordinateSystem) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialAnchor TryCreateRelativeTo( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position)
		{
			throw new global::System.NotImplementedException("The member SpatialAnchor SpatialAnchor.TryCreateRelativeTo(SpatialCoordinateSystem coordinateSystem, Vector3 position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Perception.Spatial.SpatialAnchor TryCreateRelativeTo( global::Windows.Perception.Spatial.SpatialCoordinateSystem coordinateSystem,  global::System.Numerics.Vector3 position,  global::System.Numerics.Quaternion orientation)
		{
			throw new global::System.NotImplementedException("The member SpatialAnchor SpatialAnchor.TryCreateRelativeTo(SpatialCoordinateSystem coordinateSystem, Vector3 position, Quaternion orientation) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Perception.Spatial.SpatialAnchor, global::Windows.Perception.Spatial.SpatialAnchorRawCoordinateSystemAdjustedEventArgs> RawCoordinateSystemAdjusted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.SpatialAnchor", "event TypedEventHandler<SpatialAnchor, SpatialAnchorRawCoordinateSystemAdjustedEventArgs> SpatialAnchor.RawCoordinateSystemAdjusted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.SpatialAnchor", "event TypedEventHandler<SpatialAnchor, SpatialAnchorRawCoordinateSystemAdjustedEventArgs> SpatialAnchor.RawCoordinateSystemAdjusted");
			}
		}
		#endif
	}
}
