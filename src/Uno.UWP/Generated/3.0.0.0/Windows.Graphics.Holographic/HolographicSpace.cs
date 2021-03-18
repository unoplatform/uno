#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HolographicSpace 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicAdapterId PrimaryAdapterId
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicAdapterId HolographicSpace.PrimaryAdapterId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicSpaceUserPresence UserPresence
		{
			get
			{
				throw new global::System.NotImplementedException("The member HolographicSpaceUserPresence HolographicSpace.UserPresence is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicSpace.IsAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicSpace.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsConfigured
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HolographicSpace.IsConfigured is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.PrimaryAdapterId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDirect3D11Device( global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "void HolographicSpace.SetDirect3D11Device(IDirect3DDevice value)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.CameraAdded.add
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.CameraAdded.remove
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.CameraRemoved.add
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.CameraRemoved.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFrame CreateNextFrame()
		{
			throw new global::System.NotImplementedException("The member HolographicFrame HolographicSpace.CreateNextFrame() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.UserPresence.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.UserPresenceChanged.add
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.UserPresenceChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void WaitForNextFrameReady()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "void HolographicSpace.WaitForNextFrameReady()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void WaitForNextFrameReadyWithHeadStart( global::System.TimeSpan requestedHeadStartDuration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "void HolographicSpace.WaitForNextFrameReadyWithHeadStart(TimeSpan requestedHeadStartDuration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFramePresentationMonitor CreateFramePresentationMonitor( uint maxQueuedReports)
		{
			throw new global::System.NotImplementedException("The member HolographicFramePresentationMonitor HolographicSpace.CreateFramePresentationMonitor(uint maxQueuedReports) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Holographic.HolographicFrameScanoutMonitor CreateFrameScanoutMonitor( uint maxQueuedReports)
		{
			throw new global::System.NotImplementedException("The member HolographicFrameScanoutMonitor HolographicSpace.CreateFrameScanoutMonitor(uint maxQueuedReports) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.IsConfigured.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.IsSupported.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.IsAvailable.get
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.IsAvailableChanged.add
		// Forced skipping of method Windows.Graphics.Holographic.HolographicSpace.IsAvailableChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Holographic.HolographicSpace CreateForCoreWindow( global::Windows.UI.Core.CoreWindow window)
		{
			throw new global::System.NotImplementedException("The member HolographicSpace HolographicSpace.CreateForCoreWindow(CoreWindow window) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Holographic.HolographicSpace, global::Windows.Graphics.Holographic.HolographicSpaceCameraAddedEventArgs> CameraAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, HolographicSpaceCameraAddedEventArgs> HolographicSpace.CameraAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, HolographicSpaceCameraAddedEventArgs> HolographicSpace.CameraAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Holographic.HolographicSpace, global::Windows.Graphics.Holographic.HolographicSpaceCameraRemovedEventArgs> CameraRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, HolographicSpaceCameraRemovedEventArgs> HolographicSpace.CameraRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, HolographicSpaceCameraRemovedEventArgs> HolographicSpace.CameraRemoved");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Holographic.HolographicSpace, object> UserPresenceChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, object> HolographicSpace.UserPresenceChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event TypedEventHandler<HolographicSpace, object> HolographicSpace.UserPresenceChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> IsAvailableChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event EventHandler<object> HolographicSpace.IsAvailableChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Holographic.HolographicSpace", "event EventHandler<object> HolographicSpace.IsAvailableChanged");
			}
		}
		#endif
	}
}
