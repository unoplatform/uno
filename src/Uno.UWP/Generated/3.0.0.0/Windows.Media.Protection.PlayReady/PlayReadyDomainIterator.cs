#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadyDomainIterator : global::Windows.Foundation.Collections.IIterator<global::Windows.Media.Protection.PlayReady.IPlayReadyDomain>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.IPlayReadyDomain Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPlayReadyDomain PlayReadyDomainIterator.Current is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCurrent
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlayReadyDomainIterator.HasCurrent is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadyDomainIterator.Current.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadyDomainIterator.HasCurrent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MoveNext()
		{
			throw new global::System.NotImplementedException("The member bool PlayReadyDomainIterator.MoveNext() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetMany( global::Windows.Media.Protection.PlayReady.IPlayReadyDomain[] items)
		{
			throw new global::System.NotImplementedException("The member uint PlayReadyDomainIterator.GetMany(IPlayReadyDomain[] items) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Foundation.Collections.IIterator<Windows.Media.Protection.PlayReady.IPlayReadyDomain>
	}
}
