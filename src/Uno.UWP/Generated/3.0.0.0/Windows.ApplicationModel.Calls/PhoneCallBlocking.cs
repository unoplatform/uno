#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class PhoneCallBlocking 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool BlockUnknownNumbers
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhoneCallBlocking.BlockUnknownNumbers is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneCallBlocking.BlockUnknownNumbers");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneCallBlocking", "bool PhoneCallBlocking.BlockUnknownNumbers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool BlockPrivateNumbers
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhoneCallBlocking.BlockPrivateNumbers is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneCallBlocking.BlockPrivateNumbers");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneCallBlocking", "bool PhoneCallBlocking.BlockPrivateNumbers");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallBlocking.BlockUnknownNumbers.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallBlocking.BlockUnknownNumbers.set
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallBlocking.BlockPrivateNumbers.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallBlocking.BlockPrivateNumbers.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> SetCallBlockingListAsync( global::System.Collections.Generic.IEnumerable<string> phoneNumberList)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> PhoneCallBlocking.SetCallBlockingListAsync(IEnumerable<string> phoneNumberList) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20PhoneCallBlocking.SetCallBlockingListAsync%28IEnumerable%3Cstring%3E%20phoneNumberList%29");
		}
		#endif
	}
}
