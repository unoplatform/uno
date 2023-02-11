#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkOperatorTetheringOperationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AdditionalErrorMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NetworkOperatorTetheringOperationResult.AdditionalErrorMessage is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20NetworkOperatorTetheringOperationResult.AdditionalErrorMessage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.TetheringOperationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member TetheringOperationStatus NetworkOperatorTetheringOperationResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TetheringOperationStatus%20NetworkOperatorTetheringOperationResult.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringOperationResult.Status.get
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringOperationResult.AdditionalErrorMessage.get
	}
}
