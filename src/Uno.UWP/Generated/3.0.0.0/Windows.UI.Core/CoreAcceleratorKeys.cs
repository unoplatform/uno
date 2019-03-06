#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreAcceleratorKeys : global::Windows.UI.Core.ICoreAcceleratorKeys
	{
		// Forced skipping of method Windows.UI.Core.CoreAcceleratorKeys.AcceleratorKeyActivated.add
		// Forced skipping of method Windows.UI.Core.CoreAcceleratorKeys.AcceleratorKeyActivated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreDispatcher, global::Windows.UI.Core.AcceleratorKeyEventArgs> AcceleratorKeyActivated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreAcceleratorKeys", "event TypedEventHandler<CoreDispatcher, AcceleratorKeyEventArgs> CoreAcceleratorKeys.AcceleratorKeyActivated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreAcceleratorKeys", "event TypedEventHandler<CoreDispatcher, AcceleratorKeyEventArgs> CoreAcceleratorKeys.AcceleratorKeyActivated");
			}
		}
		#endif
		// Processing: Windows.UI.Core.ICoreAcceleratorKeys
	}
}
