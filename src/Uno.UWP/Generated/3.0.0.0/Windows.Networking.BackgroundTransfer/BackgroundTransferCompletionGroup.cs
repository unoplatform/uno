#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundTransferCompletionGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BackgroundTransferCompletionGroup.IsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.IBackgroundTrigger Trigger
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBackgroundTrigger BackgroundTransferCompletionGroup.Trigger is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BackgroundTransferCompletionGroup() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroup", "BackgroundTransferCompletionGroup.BackgroundTransferCompletionGroup()");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroup.BackgroundTransferCompletionGroup()
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroup.Trigger.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroup.IsEnabled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Enable()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroup", "void BackgroundTransferCompletionGroup.Enable()");
		}
		#endif
	}
}
