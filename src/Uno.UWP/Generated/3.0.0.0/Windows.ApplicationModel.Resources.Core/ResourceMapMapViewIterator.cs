#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceMapMapViewIterator : global::Windows.Foundation.Collections.IIterator<global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.ResourceMap>>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.ResourceMap> Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member KeyValuePair<string, ResourceMap> ResourceMapMapViewIterator.Current is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=KeyValuePair%3Cstring%2C%20ResourceMap%3E%20ResourceMapMapViewIterator.Current");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCurrent
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResourceMapMapViewIterator.HasCurrent is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ResourceMapMapViewIterator.HasCurrent");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceMapMapViewIterator.Current.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceMapMapViewIterator.HasCurrent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MoveNext()
		{
			throw new global::System.NotImplementedException("The member bool ResourceMapMapViewIterator.MoveNext() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ResourceMapMapViewIterator.MoveNext%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetMany( global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.ResourceMap>[] items)
		{
			throw new global::System.NotImplementedException("The member uint ResourceMapMapViewIterator.GetMany(KeyValuePair<string, ResourceMap>[] items) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20ResourceMapMapViewIterator.GetMany%28KeyValuePair%3Cstring%2C%20ResourceMap%3E%5B%5D%20items%29");
		}
		#endif
		// Processing: Windows.Foundation.Collections.IIterator<System.Collections.Generic.KeyValuePair<string, Windows.ApplicationModel.Resources.Core.ResourceMap>>
	}
}
