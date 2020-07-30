#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialAnchorRawCoordinateSystemAdjustedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Matrix4x4 OldRawCoordinateSystemToNewRawCoordinateSystemTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member Matrix4x4 SpatialAnchorRawCoordinateSystemAdjustedEventArgs.OldRawCoordinateSystemToNewRawCoordinateSystemTransform is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.SpatialAnchorRawCoordinateSystemAdjustedEventArgs.OldRawCoordinateSystemToNewRawCoordinateSystemTransform.get
	}
}
