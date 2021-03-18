#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BasicProperties : global::Windows.Storage.FileProperties.IStorageItemExtraProperties
	{
		// Skipping already declared property DateModified
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset ItemDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset BasicProperties.ItemDate is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Size
		// Forced skipping of method Windows.Storage.FileProperties.BasicProperties.Size.get
		// Forced skipping of method Windows.Storage.FileProperties.BasicProperties.DateModified.get
		// Forced skipping of method Windows.Storage.FileProperties.BasicProperties.ItemDate.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IDictionary<string, object>> RetrievePropertiesAsync( global::System.Collections.Generic.IEnumerable<string> propertiesToRetrieve)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IDictionary<string, object>> BasicProperties.RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SavePropertiesAsync( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, object>> propertiesToSave)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BasicProperties.SavePropertiesAsync(IEnumerable<KeyValuePair<string, object>> propertiesToSave) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SavePropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BasicProperties.SavePropertiesAsync() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.FileProperties.IStorageItemExtraProperties
	}
}
