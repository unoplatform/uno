#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ElementSoundPlayer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static double Volume
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ElementSoundPlayer.Volume is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.ElementSoundPlayer", "double ElementSoundPlayer.Volume");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.ElementSoundPlayerState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ElementSoundPlayerState ElementSoundPlayer.State is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.ElementSoundPlayer", "ElementSoundPlayerState ElementSoundPlayer.State");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.ElementSpatialAudioMode SpatialAudioMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ElementSpatialAudioMode ElementSoundPlayer.SpatialAudioMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.ElementSoundPlayer", "ElementSpatialAudioMode ElementSoundPlayer.SpatialAudioMode");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.SpatialAudioMode.get
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.SpatialAudioMode.set
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.Volume.get
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.Volume.set
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.State.get
		// Forced skipping of method Windows.UI.Xaml.ElementSoundPlayer.State.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void Play( global::Windows.UI.Xaml.ElementSoundKind sound)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.ElementSoundPlayer", "void ElementSoundPlayer.Play(ElementSoundKind sound)");
		}
		#endif
	}
}
