#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemIndexRange 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int FirstIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemIndexRange.FirstIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int LastIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemIndexRange.LastIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Length
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ItemIndexRange.Length is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ItemIndexRange( int firstIndex,  uint length) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Data.ItemIndexRange", "ItemIndexRange.ItemIndexRange(int firstIndex, uint length)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.ItemIndexRange.ItemIndexRange(int, uint)
		// Forced skipping of method Windows.UI.Xaml.Data.ItemIndexRange.FirstIndex.get
		// Forced skipping of method Windows.UI.Xaml.Data.ItemIndexRange.Length.get
		// Forced skipping of method Windows.UI.Xaml.Data.ItemIndexRange.LastIndex.get
	}
}
