#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SmartCards
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmartCardChallengeContext : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Challenge
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer SmartCardChallengeContext.Challenge is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20SmartCardChallengeContext.Challenge");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.SmartCards.SmartCardChallengeContext.Challenge.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> VerifyResponseAsync( global::Windows.Storage.Streams.IBuffer response)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SmartCardChallengeContext.VerifyResponseAsync(IBuffer response) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20SmartCardChallengeContext.VerifyResponseAsync%28IBuffer%20response%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ProvisionAsync( global::Windows.Storage.Streams.IBuffer response,  bool formatCard)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SmartCardChallengeContext.ProvisionAsync(IBuffer response, bool formatCard) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20SmartCardChallengeContext.ProvisionAsync%28IBuffer%20response%2C%20bool%20formatCard%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ProvisionAsync( global::Windows.Storage.Streams.IBuffer response,  bool formatCard,  global::System.Guid newCardId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SmartCardChallengeContext.ProvisionAsync(IBuffer response, bool formatCard, Guid newCardId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20SmartCardChallengeContext.ProvisionAsync%28IBuffer%20response%2C%20bool%20formatCard%2C%20Guid%20newCardId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ChangeAdministrativeKeyAsync( global::Windows.Storage.Streams.IBuffer response,  global::Windows.Storage.Streams.IBuffer newAdministrativeKey)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SmartCardChallengeContext.ChangeAdministrativeKeyAsync(IBuffer response, IBuffer newAdministrativeKey) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20SmartCardChallengeContext.ChangeAdministrativeKeyAsync%28IBuffer%20response%2C%20IBuffer%20newAdministrativeKey%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.SmartCards.SmartCardChallengeContext", "void SmartCardChallengeContext.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
