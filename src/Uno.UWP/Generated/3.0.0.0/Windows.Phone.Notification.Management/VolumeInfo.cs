#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VolumeInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint CallVolume
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint VolumeInfo.CallVolume is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMuted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VolumeInfo.IsMuted is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.Notification.Management.VibrateState IsVibrateEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member VibrateState VolumeInfo.IsVibrateEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MediaVolume
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint VolumeInfo.MediaVolume is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SystemVolume
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint VolumeInfo.SystemVolume is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.Notification.Management.VolumeInfo.SystemVolume.get
		// Forced skipping of method Windows.Phone.Notification.Management.VolumeInfo.CallVolume.get
		// Forced skipping of method Windows.Phone.Notification.Management.VolumeInfo.MediaVolume.get
		// Forced skipping of method Windows.Phone.Notification.Management.VolumeInfo.IsMuted.get
		// Forced skipping of method Windows.Phone.Notification.Management.VolumeInfo.IsVibrateEnabled.get
	}
}
