#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadySecureStopIterator : global::Windows.Foundation.Collections.IIterator<global::Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPlayReadySecureStopServiceRequest PlayReadySecureStopIterator.Current is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCurrent
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlayReadySecureStopIterator.HasCurrent is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadySecureStopIterator.Current.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.PlayReadySecureStopIterator.HasCurrent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MoveNext()
		{
			throw new global::System.NotImplementedException("The member bool PlayReadySecureStopIterator.MoveNext() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetMany( global::Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest[] items)
		{
			throw new global::System.NotImplementedException("The member uint PlayReadySecureStopIterator.GetMany(IPlayReadySecureStopServiceRequest[] items) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Foundation.Collections.IIterator<Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest>
	}
}
