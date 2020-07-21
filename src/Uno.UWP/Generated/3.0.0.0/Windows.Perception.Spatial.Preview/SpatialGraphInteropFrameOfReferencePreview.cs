#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialGraphInteropFrameOfReferencePreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Perception.Spatial.SpatialCoordinateSystem CoordinateSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpatialCoordinateSystem SpatialGraphInteropFrameOfReferencePreview.CoordinateSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Matrix4x4 CoordinateSystemToNodeTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member Matrix4x4 SpatialGraphInteropFrameOfReferencePreview.CoordinateSystemToNodeTransform is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid NodeId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid SpatialGraphInteropFrameOfReferencePreview.NodeId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Perception.Spatial.Preview.SpatialGraphInteropFrameOfReferencePreview.CoordinateSystem.get
		// Forced skipping of method Windows.Perception.Spatial.Preview.SpatialGraphInteropFrameOfReferencePreview.NodeId.get
		// Forced skipping of method Windows.Perception.Spatial.Preview.SpatialGraphInteropFrameOfReferencePreview.CoordinateSystemToNodeTransform.get
	}
}
