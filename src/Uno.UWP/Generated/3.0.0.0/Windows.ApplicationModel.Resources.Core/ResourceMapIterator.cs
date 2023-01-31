#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceMapIterator : global::Windows.Foundation.Collections.IIterator<global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.NamedResource>>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.NamedResource> Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member KeyValuePair<string, NamedResource> ResourceMapIterator.Current is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=KeyValuePair%3Cstring%2C%20NamedResource%3E%20ResourceMapIterator.Current");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasCurrent
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResourceMapIterator.HasCurrent is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ResourceMapIterator.HasCurrent");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceMapIterator.Current.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceMapIterator.HasCurrent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MoveNext()
		{
			throw new global::System.NotImplementedException("The member bool ResourceMapIterator.MoveNext() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ResourceMapIterator.MoveNext%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetMany( global::System.Collections.Generic.KeyValuePair<string, global::Windows.ApplicationModel.Resources.Core.NamedResource>[] items)
		{
			throw new global::System.NotImplementedException("The member uint ResourceMapIterator.GetMany(KeyValuePair<string, NamedResource>[] items) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20ResourceMapIterator.GetMany%28KeyValuePair%3Cstring%2C%20NamedResource%3E%5B%5D%20items%29");
		}
		#endif
		// Processing: Windows.Foundation.Collections.IIterator<System.Collections.Generic.KeyValuePair<string, Windows.ApplicationModel.Resources.Core.NamedResource>>
	}
}
