#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverVideoStreamSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 MiracastReceiverVideoStreamSettings.Size is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverVideoStreamSettings", "SizeInt32 MiracastReceiverVideoStreamSettings.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Bitrate
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MiracastReceiverVideoStreamSettings.Bitrate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverVideoStreamSettings", "int MiracastReceiverVideoStreamSettings.Bitrate");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverVideoStreamSettings.Size.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverVideoStreamSettings.Size.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverVideoStreamSettings.Bitrate.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverVideoStreamSettings.Bitrate.set
	}
}
