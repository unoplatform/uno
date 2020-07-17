#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandUiccApp 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MobileBroadbandUiccApp.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.UiccAppKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UiccAppKind MobileBroadbandUiccApp.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandUiccApp.Id.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandUiccApp.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandUiccAppRecordDetailsResult> GetRecordDetailsAsync( global::System.Collections.Generic.IEnumerable<uint> uiccFilePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandUiccAppRecordDetailsResult> MobileBroadbandUiccApp.GetRecordDetailsAsync(IEnumerable<uint> uiccFilePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandUiccAppReadRecordResult> ReadRecordAsync( global::System.Collections.Generic.IEnumerable<uint> uiccFilePath,  int recordIndex)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandUiccAppReadRecordResult> MobileBroadbandUiccApp.ReadRecordAsync(IEnumerable<uint> uiccFilePath, int recordIndex) is not implemented in Uno.");
		}
		#endif
	}
}
