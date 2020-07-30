#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SmartCards
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SmartCardProvisioning 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.SmartCards.SmartCard SmartCard
		{
			get
			{
				throw new global::System.NotImplementedException("The member SmartCard SmartCardProvisioning.SmartCard is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.SmartCards.SmartCardProvisioning.SmartCard.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Guid> GetIdAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Guid> SmartCardProvisioning.GetIdAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetNameAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> SmartCardProvisioning.GetNameAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardChallengeContext> GetChallengeContextAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardChallengeContext> SmartCardProvisioning.GetChallengeContextAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestPinChangeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SmartCardProvisioning.RequestPinChangeAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestPinResetAsync( global::Windows.Devices.SmartCards.SmartCardPinResetHandler handler)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SmartCardProvisioning.RequestPinResetAsync(SmartCardPinResetHandler handler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetAuthorityKeyContainerNameAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> SmartCardProvisioning.GetAuthorityKeyContainerNameAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardProvisioning> RequestAttestedVirtualSmartCardCreationAsync( string friendlyName,  global::Windows.Storage.Streams.IBuffer administrativeKey,  global::Windows.Devices.SmartCards.SmartCardPinPolicy pinPolicy)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardProvisioning> SmartCardProvisioning.RequestAttestedVirtualSmartCardCreationAsync(string friendlyName, IBuffer administrativeKey, SmartCardPinPolicy pinPolicy) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardProvisioning> RequestAttestedVirtualSmartCardCreationAsync( string friendlyName,  global::Windows.Storage.Streams.IBuffer administrativeKey,  global::Windows.Devices.SmartCards.SmartCardPinPolicy pinPolicy,  global::System.Guid cardId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardProvisioning> SmartCardProvisioning.RequestAttestedVirtualSmartCardCreationAsync(string friendlyName, IBuffer administrativeKey, SmartCardPinPolicy pinPolicy, Guid cardId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardProvisioning> FromSmartCardAsync( global::Windows.Devices.SmartCards.SmartCard card)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardProvisioning> SmartCardProvisioning.FromSmartCardAsync(SmartCard card) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardProvisioning> RequestVirtualSmartCardCreationAsync( string friendlyName,  global::Windows.Storage.Streams.IBuffer administrativeKey,  global::Windows.Devices.SmartCards.SmartCardPinPolicy pinPolicy)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardProvisioning> SmartCardProvisioning.RequestVirtualSmartCardCreationAsync(string friendlyName, IBuffer administrativeKey, SmartCardPinPolicy pinPolicy) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.SmartCards.SmartCardProvisioning> RequestVirtualSmartCardCreationAsync( string friendlyName,  global::Windows.Storage.Streams.IBuffer administrativeKey,  global::Windows.Devices.SmartCards.SmartCardPinPolicy pinPolicy,  global::System.Guid cardId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SmartCardProvisioning> SmartCardProvisioning.RequestVirtualSmartCardCreationAsync(string friendlyName, IBuffer administrativeKey, SmartCardPinPolicy pinPolicy, Guid cardId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> RequestVirtualSmartCardDeletionAsync( global::Windows.Devices.SmartCards.SmartCard card)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SmartCardProvisioning.RequestVirtualSmartCardDeletionAsync(SmartCard card) is not implemented in Uno.");
		}
		#endif
	}
}
