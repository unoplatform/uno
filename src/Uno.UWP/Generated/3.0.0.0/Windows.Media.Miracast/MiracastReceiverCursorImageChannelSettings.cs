#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverCursorImageChannelSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 MaxImageSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 MiracastReceiverCursorImageChannelSettings.MaxImageSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings", "SizeInt32 MiracastReceiverCursorImageChannelSettings.MaxImageSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverCursorImageChannelSettings.IsEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings", "bool MiracastReceiverCursorImageChannelSettings.IsEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings.IsEnabled.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings.IsEnabled.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings.MaxImageSize.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverCursorImageChannelSettings.MaxImageSize.set
	}
}
