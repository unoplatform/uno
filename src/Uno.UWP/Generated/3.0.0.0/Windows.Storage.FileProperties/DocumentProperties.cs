#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DocumentProperties : global::Windows.Storage.FileProperties.IStorageItemExtraProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DocumentProperties.Title is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.FileProperties.DocumentProperties", "string DocumentProperties.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Comment
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DocumentProperties.Comment is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.FileProperties.DocumentProperties", "string DocumentProperties.Comment");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> Author
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> DocumentProperties.Author is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> Keywords
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> DocumentProperties.Keywords is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Author.get
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Title.get
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Title.set
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Keywords.get
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Comment.get
		// Forced skipping of method Windows.Storage.FileProperties.DocumentProperties.Comment.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IDictionary<string, object>> RetrievePropertiesAsync( global::System.Collections.Generic.IEnumerable<string> propertiesToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IDictionary<string, object>> DocumentProperties.RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SavePropertiesAsync( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, object>> propertiesToSave)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DocumentProperties.SavePropertiesAsync(IEnumerable<KeyValuePair<string, object>> propertiesToSave) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SavePropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DocumentProperties.SavePropertiesAsync() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.FileProperties.IStorageItemExtraProperties
	}
}
