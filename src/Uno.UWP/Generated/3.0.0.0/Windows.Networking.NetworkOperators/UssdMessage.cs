#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UssdMessage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PayloadAsText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UssdMessage.PayloadAsText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.UssdMessage", "string UssdMessage.PayloadAsText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte DataCodingScheme
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte UssdMessage.DataCodingScheme is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.UssdMessage", "byte UssdMessage.DataCodingScheme");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UssdMessage( string messageText) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.UssdMessage", "UssdMessage.UssdMessage(string messageText)");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.UssdMessage.UssdMessage(string)
		// Forced skipping of method Windows.Networking.NetworkOperators.UssdMessage.DataCodingScheme.get
		// Forced skipping of method Windows.Networking.NetworkOperators.UssdMessage.DataCodingScheme.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] GetPayload()
		{
			throw new global::System.NotImplementedException("The member byte[] UssdMessage.GetPayload() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPayload( byte[] value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.UssdMessage", "void UssdMessage.SetPayload(byte[] value)");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.UssdMessage.PayloadAsText.get
		// Forced skipping of method Windows.Networking.NetworkOperators.UssdMessage.PayloadAsText.set
	}
}
