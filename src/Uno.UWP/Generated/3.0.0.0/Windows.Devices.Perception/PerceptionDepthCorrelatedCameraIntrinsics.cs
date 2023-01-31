#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionDepthCorrelatedCameraIntrinsics 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3 UnprojectPixelAtCorrelatedDepth( global::Windows.Foundation.Point pixelCoordinate,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame)
		{
			throw new global::System.NotImplementedException("The member Vector3 PerceptionDepthCorrelatedCameraIntrinsics.UnprojectPixelAtCorrelatedDepth(Point pixelCoordinate, PerceptionDepthFrame depthFrame) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Vector3%20PerceptionDepthCorrelatedCameraIntrinsics.UnprojectPixelAtCorrelatedDepth%28Point%20pixelCoordinate%2C%20PerceptionDepthFrame%20depthFrame%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnprojectPixelsAtCorrelatedDepth( global::Windows.Foundation.Point[] sourceCoordinates,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::System.Numerics.Vector3[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthCorrelatedCameraIntrinsics", "void PerceptionDepthCorrelatedCameraIntrinsics.UnprojectPixelsAtCorrelatedDepth(Point[] sourceCoordinates, PerceptionDepthFrame depthFrame, Vector3[] results)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction UnprojectRegionPixelsAtCorrelatedDepthAsync( global::Windows.Foundation.Rect region,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::System.Numerics.Vector3[] results)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PerceptionDepthCorrelatedCameraIntrinsics.UnprojectRegionPixelsAtCorrelatedDepthAsync(Rect region, PerceptionDepthFrame depthFrame, Vector3[] results) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20PerceptionDepthCorrelatedCameraIntrinsics.UnprojectRegionPixelsAtCorrelatedDepthAsync%28Rect%20region%2C%20PerceptionDepthFrame%20depthFrame%2C%20Vector3%5B%5D%20results%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction UnprojectAllPixelsAtCorrelatedDepthAsync( global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::System.Numerics.Vector3[] results)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PerceptionDepthCorrelatedCameraIntrinsics.UnprojectAllPixelsAtCorrelatedDepthAsync(PerceptionDepthFrame depthFrame, Vector3[] results) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20PerceptionDepthCorrelatedCameraIntrinsics.UnprojectAllPixelsAtCorrelatedDepthAsync%28PerceptionDepthFrame%20depthFrame%2C%20Vector3%5B%5D%20results%29");
		}
		#endif
	}
}
