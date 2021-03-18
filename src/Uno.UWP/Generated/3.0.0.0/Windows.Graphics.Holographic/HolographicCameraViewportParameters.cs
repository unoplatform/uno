#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicCameraViewportParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2[] HiddenAreaMesh
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2[] HolographicCameraViewportParameters.HiddenAreaMesh is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector2[] VisibleAreaMesh
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2[] HolographicCameraViewportParameters.VisibleAreaMesh is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraViewportParameters.HiddenAreaMesh.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicCameraViewportParameters.VisibleAreaMesh.get
	}
}
