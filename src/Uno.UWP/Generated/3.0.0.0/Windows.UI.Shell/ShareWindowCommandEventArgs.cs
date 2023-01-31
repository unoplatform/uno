#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Shell
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ShareWindowCommandEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Shell.ShareWindowCommand Command
		{
			get
			{
				throw new global::System.NotImplementedException("The member ShareWindowCommand ShareWindowCommandEventArgs.Command is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ShareWindowCommand%20ShareWindowCommandEventArgs.Command");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Shell.ShareWindowCommandEventArgs", "ShareWindowCommand ShareWindowCommandEventArgs.Command");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowId WindowId
		{
			get
			{
				throw new global::System.NotImplementedException("The member WindowId ShareWindowCommandEventArgs.WindowId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WindowId%20ShareWindowCommandEventArgs.WindowId");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Shell.ShareWindowCommandEventArgs.WindowId.get
		// Forced skipping of method Windows.UI.Shell.ShareWindowCommandEventArgs.Command.get
		// Forced skipping of method Windows.UI.Shell.ShareWindowCommandEventArgs.Command.set
	}
}
