#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CastingDevicePickerFilter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SupportsVideo
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CastingDevicePickerFilter.SupportsVideo is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingDevicePickerFilter", "bool CastingDevicePickerFilter.SupportsVideo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SupportsPictures
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CastingDevicePickerFilter.SupportsPictures is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingDevicePickerFilter", "bool CastingDevicePickerFilter.SupportsPictures");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SupportsAudio
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CastingDevicePickerFilter.SupportsAudio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Casting.CastingDevicePickerFilter", "bool CastingDevicePickerFilter.SupportsAudio");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Casting.CastingSource> SupportedCastingSources
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<CastingSource> CastingDevicePickerFilter.SupportedCastingSources is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsAudio.get
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsAudio.set
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsVideo.get
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsVideo.set
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsPictures.get
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportsPictures.set
		// Forced skipping of method Windows.Media.Casting.CastingDevicePickerFilter.SupportedCastingSources.get
	}
}
