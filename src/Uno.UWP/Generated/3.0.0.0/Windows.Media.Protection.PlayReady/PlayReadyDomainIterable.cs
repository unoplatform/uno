#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadyDomainIterable : global::System.Collections.Generic.IEnumerable<global::Windows.Media.Protection.PlayReady.IPlayReadyDomain>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PlayReadyDomainIterable( global::System.Guid domainAccountId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.PlayReadyDomainIterable", "PlayReadyDomainIterable.PlayReadyDomainIterable(Guid domainAccountId)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadyDomainIterable.PlayReadyDomainIterable(System.Guid)
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadyDomainIterable.First()
		// Processing: System.Collections.Generic.IEnumerable<Windows.Media.Protection.PlayReady.IPlayReadyDomain>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Media.Protection.PlayReady.IPlayReadyDomain>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Media.Protection.PlayReady.IPlayReadyDomain> GetEnumerator()
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
