#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadySecureStopIterable : global::System.Collections.Generic.IEnumerable<global::Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PlayReadySecureStopIterable( byte[] publisherCertBytes) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.PlayReadySecureStopIterable", "PlayReadySecureStopIterable.PlayReadySecureStopIterable(byte[] publisherCertBytes)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadySecureStopIterable.PlayReadySecureStopIterable(byte[])
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadySecureStopIterable.First()
		// Processing: System.Collections.Generic.IEnumerable<Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
