#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialLocatorAttachedFrameOfReference 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3 RelativePosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector3 SpatialLocatorAttachedFrameOfReference.RelativePosition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference", "Vector3 SpatialLocatorAttachedFrameOfReference.RelativePosition");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Quaternion RelativeOrientation
		{
			get
			{
				throw new global::System.NotImplementedException("The member Quaternion SpatialLocatorAttachedFrameOfReference.RelativeOrientation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference", "Quaternion SpatialLocatorAttachedFrameOfReference.RelativeOrientation");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference.RelativePosition.get
		// Forced skipping of method Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference.RelativePosition.set
		// Forced skipping of method Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference.RelativeOrientation.get
		// Forced skipping of method Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference.RelativeOrientation.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AdjustHeading( double headingOffsetInRadians)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Perception.Spatial.SpatialLocatorAttachedFrameOfReference", "void SpatialLocatorAttachedFrameOfReference.AdjustHeading(double headingOffsetInRadians)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem GetStationaryCoordinateSystemAtTimestamp( global::Windows.Perception.PerceptionTimestamp timestamp)
		{
			throw new global::System.NotImplementedException("The member SpatialCoordinateSystem SpatialLocatorAttachedFrameOfReference.GetStationaryCoordinateSystemAtTimestamp(PerceptionTimestamp timestamp) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? TryGetRelativeHeadingAtTimestamp( global::Windows.Perception.PerceptionTimestamp timestamp)
		{
			throw new global::System.NotImplementedException("The member double? SpatialLocatorAttachedFrameOfReference.TryGetRelativeHeadingAtTimestamp(PerceptionTimestamp timestamp) is not implemented in Uno.");
		}
		#endif
	}
}
