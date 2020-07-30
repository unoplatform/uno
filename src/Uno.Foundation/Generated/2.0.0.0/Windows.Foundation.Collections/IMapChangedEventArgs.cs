#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Collections
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMapChangedEventArgs<K> 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Collections.CollectionChange CollectionChange
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		K Key
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.Collections.IMapChangedEventArgs<K>.CollectionChange.get
		// Forced skipping of method Windows.Foundation.Collections.IMapChangedEventArgs<K>.Key.get
	}
}
