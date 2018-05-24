#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SocketErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationAborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HttpInvalidServerResponse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionTimedOut,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressFamilyNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SocketTypeNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HostNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoDataRecordOfRequestedType,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NonAuthoritativeHostNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClassTypeNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressAlreadyInUse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CannotAssignRequestedAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionRefused,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkIsUnreachable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnreachableHost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkIsDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkDroppedConnectionOnReset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SoftwareCausedConnectionAbort,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionResetByPeer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HostIsDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoAddressesFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooManyOpenFiles,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageTooLong,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateExpired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateUntrustedRoot,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateCommonNameIsIncorrect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateWrongUsage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateNoRevocationCheck,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateRevocationServerOffline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CertificateIsInvalid,
		#endif
	}
	#endif
}
