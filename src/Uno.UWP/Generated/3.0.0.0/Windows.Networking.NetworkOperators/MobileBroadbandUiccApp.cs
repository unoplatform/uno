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
				throw new global::System.NotImplementedException("The member IBuffer MobileBroadbandUiccApp.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20MobileBroadbandUiccApp.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.UiccAppKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UiccAppKind MobileBroadbandUiccApp.Kind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UiccAppKind%20MobileBroadbandUiccApp.Kind");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandUiccApp.Id.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandUiccApp.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandUiccAppRecordDetailsResult> GetRecordDetailsAsync( global::System.Collections.Generic.IEnumerable<uint> uiccFilePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandUiccAppRecordDetailsResult> MobileBroadbandUiccApp.GetRecordDetailsAsync(IEnumerable<uint> uiccFilePath) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CMobileBroadbandUiccAppRecordDetailsResult%3E%20MobileBroadbandUiccApp.GetRecordDetailsAsync%28IEnumerable%3Cuint%3E%20uiccFilePath%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandUiccAppReadRecordResult> ReadRecordAsync( global::System.Collections.Generic.IEnumerable<uint> uiccFilePath,  int recordIndex)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandUiccAppReadRecordResult> MobileBroadbandUiccApp.ReadRecordAsync(IEnumerable<uint> uiccFilePath, int recordIndex) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CMobileBroadbandUiccAppReadRecordResult%3E%20MobileBroadbandUiccApp.ReadRecordAsync%28IEnumerable%3Cuint%3E%20uiccFilePath%2C%20int%20recordIndex%29");
		}
		#endif
	}
}
