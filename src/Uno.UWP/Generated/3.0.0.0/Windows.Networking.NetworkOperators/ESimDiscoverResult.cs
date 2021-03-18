#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ESimDiscoverResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.NetworkOperators.ESimDiscoverEvent> Events
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ESimDiscoverEvent> ESimDiscoverResult.Events is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimDiscoverResultKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimDiscoverResultKind ESimDiscoverResult.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimProfileMetadata ProfileMetadata
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimProfileMetadata ESimDiscoverResult.ProfileMetadata is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimOperationResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimOperationResult ESimDiscoverResult.Result is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDiscoverResult.Events.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDiscoverResult.Kind.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDiscoverResult.ProfileMetadata.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimDiscoverResult.Result.get
	}
}
