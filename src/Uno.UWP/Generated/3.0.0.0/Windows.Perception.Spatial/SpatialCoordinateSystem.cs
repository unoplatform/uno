#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialCoordinateSystem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Matrix4x4? TryGetTransformTo( global::Windows.Perception.Spatial.SpatialCoordinateSystem target)
		{
			throw new global::System.NotImplementedException("The member Matrix4x4? SpatialCoordinateSystem.TryGetTransformTo(SpatialCoordinateSystem target) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Matrix4x4%3F%20SpatialCoordinateSystem.TryGetTransformTo%28SpatialCoordinateSystem%20target%29");
		}
		#endif
	}
}
