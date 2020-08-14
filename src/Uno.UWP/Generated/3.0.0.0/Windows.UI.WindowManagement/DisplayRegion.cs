#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayRegion 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayMonitorDeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DisplayRegion.DisplayMonitorDeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVisible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayRegion.IsVisible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.WindowingEnvironment WindowingEnvironment
		{
			get
			{
				throw new global::System.NotImplementedException("The member WindowingEnvironment DisplayRegion.WindowingEnvironment is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point WorkAreaOffset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point DisplayRegion.WorkAreaOffset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Size WorkAreaSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size DisplayRegion.WorkAreaSize is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.DisplayMonitorDeviceId.get
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.IsVisible.get
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.WorkAreaOffset.get
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.WorkAreaSize.get
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.WindowingEnvironment.get
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.Changed.add
		// Forced skipping of method Windows.UI.WindowManagement.DisplayRegion.Changed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.WindowManagement.DisplayRegion, object> Changed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.DisplayRegion", "event TypedEventHandler<DisplayRegion, object> DisplayRegion.Changed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.DisplayRegion", "event TypedEventHandler<DisplayRegion, object> DisplayRegion.Changed");
			}
		}
		#endif
	}
}
