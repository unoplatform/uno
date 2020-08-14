#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProjectionManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool ProjectionDisplayAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ProjectionManager.ProjectionDisplayAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction StartProjectingAsync( int projectionViewId,  int anchorViewId,  global::Windows.Devices.Enumeration.DeviceInformation displayDeviceInfo)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ProjectionManager.StartProjectingAsync(int projectionViewId, int anchorViewId, DeviceInformation displayDeviceInfo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> RequestStartProjectingAsync( int projectionViewId,  int anchorViewId,  global::Windows.Foundation.Rect selection)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ProjectionManager.RequestStartProjectingAsync(int projectionViewId, int anchorViewId, Rect selection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> RequestStartProjectingAsync( int projectionViewId,  int anchorViewId,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement prefferedPlacement)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ProjectionManager.RequestStartProjectingAsync(int projectionViewId, int anchorViewId, Rect selection, Placement prefferedPlacement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string ProjectionManager.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction StartProjectingAsync( int projectionViewId,  int anchorViewId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ProjectionManager.StartProjectingAsync(int projectionViewId, int anchorViewId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SwapDisplaysForViewsAsync( int projectionViewId,  int anchorViewId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ProjectionManager.SwapDisplaysForViewsAsync(int projectionViewId, int anchorViewId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction StopProjectingAsync( int projectionViewId,  int anchorViewId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ProjectionManager.StopProjectingAsync(int projectionViewId, int anchorViewId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.ProjectionManager.ProjectionDisplayAvailable.get
		// Forced skipping of method Windows.UI.ViewManagement.ProjectionManager.ProjectionDisplayAvailableChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.ProjectionManager.ProjectionDisplayAvailableChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> ProjectionDisplayAvailableChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.ProjectionManager", "event EventHandler<object> ProjectionManager.ProjectionDisplayAvailableChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.ProjectionManager", "event EventHandler<object> ProjectionManager.ProjectionDisplayAvailableChanged");
			}
		}
		#endif
	}
}
