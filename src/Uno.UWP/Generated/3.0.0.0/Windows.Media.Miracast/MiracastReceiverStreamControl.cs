#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverStreamControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MuteAudio
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverStreamControl.MuteAudio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverStreamControl", "bool MiracastReceiverStreamControl.MuteAudio");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverVideoStreamSettings GetVideoStreamSettings()
		{
			throw new global::System.NotImplementedException("The member MiracastReceiverVideoStreamSettings MiracastReceiverStreamControl.GetVideoStreamSettings() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Miracast.MiracastReceiverVideoStreamSettings> GetVideoStreamSettingsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MiracastReceiverVideoStreamSettings> MiracastReceiverStreamControl.GetVideoStreamSettingsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SuggestVideoStreamSettings( global::Windows.Media.Miracast.MiracastReceiverVideoStreamSettings settings)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverStreamControl", "void MiracastReceiverStreamControl.SuggestVideoStreamSettings(MiracastReceiverVideoStreamSettings settings)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SuggestVideoStreamSettingsAsync( global::Windows.Media.Miracast.MiracastReceiverVideoStreamSettings settings)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MiracastReceiverStreamControl.SuggestVideoStreamSettingsAsync(MiracastReceiverVideoStreamSettings settings) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStreamControl.MuteAudio.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStreamControl.MuteAudio.set
	}
}
