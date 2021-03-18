#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionDepthCorrelatedCoordinateMapper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point MapPixelToTarget( global::Windows.Foundation.Point sourcePixelCoordinate,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame)
		{
			throw new global::System.NotImplementedException("The member Point PerceptionDepthCorrelatedCoordinateMapper.MapPixelToTarget(Point sourcePixelCoordinate, PerceptionDepthFrame depthFrame) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void MapPixelsToTarget( global::Windows.Foundation.Point[] sourceCoordinates,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::Windows.Foundation.Point[] results)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthCorrelatedCoordinateMapper", "void PerceptionDepthCorrelatedCoordinateMapper.MapPixelsToTarget(Point[] sourceCoordinates, PerceptionDepthFrame depthFrame, Point[] results)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MapRegionOfPixelsToTargetAsync( global::Windows.Foundation.Rect region,  global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::Windows.Foundation.Point[] targetCoordinates)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PerceptionDepthCorrelatedCoordinateMapper.MapRegionOfPixelsToTargetAsync(Rect region, PerceptionDepthFrame depthFrame, Point[] targetCoordinates) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MapAllPixelsToTargetAsync( global::Windows.Devices.Perception.PerceptionDepthFrame depthFrame,  global::Windows.Foundation.Point[] targetCoordinates)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PerceptionDepthCorrelatedCoordinateMapper.MapAllPixelsToTargetAsync(PerceptionDepthFrame depthFrame, Point[] targetCoordinates) is not implemented in Uno.");
		}
		#endif
	}
}
