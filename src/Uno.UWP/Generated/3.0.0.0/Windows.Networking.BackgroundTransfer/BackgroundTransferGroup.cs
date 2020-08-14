#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundTransferGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferBehavior TransferBehavior
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferBehavior BackgroundTransferGroup.TransferBehavior is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.BackgroundTransferGroup", "BackgroundTransferBehavior BackgroundTransferGroup.TransferBehavior");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BackgroundTransferGroup.Name is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferGroup.Name.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferGroup.TransferBehavior.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferGroup.TransferBehavior.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.BackgroundTransfer.BackgroundTransferGroup CreateGroup( string name)
		{
			throw new global::System.NotImplementedException("The member BackgroundTransferGroup BackgroundTransferGroup.CreateGroup(string name) is not implemented in Uno.");
		}
		#endif
	}
}
