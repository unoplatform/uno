#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ShareProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Tag
		{
			get
			{
				throw new global::System.NotImplementedException("The member object ShareProvider.Tag is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ShareProvider", "object ShareProvider.Tag");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color BackgroundColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color ShareProvider.BackgroundColor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.RandomAccessStreamReference DisplayIcon
		{
			get
			{
				throw new global::System.NotImplementedException("The member RandomAccessStreamReference ShareProvider.DisplayIcon is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ShareProvider.Title is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ShareProvider( string title,  global::Windows.Storage.Streams.RandomAccessStreamReference displayIcon,  global::Windows.UI.Color backgroundColor,  global::Windows.ApplicationModel.DataTransfer.ShareProviderHandler handler) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ShareProvider", "ShareProvider.ShareProvider(string title, RandomAccessStreamReference displayIcon, Color backgroundColor, ShareProviderHandler handler)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.ShareProvider(string, Windows.Storage.Streams.RandomAccessStreamReference, Windows.UI.Color, Windows.ApplicationModel.DataTransfer.ShareProviderHandler)
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.Title.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.DisplayIcon.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.BackgroundColor.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.Tag.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareProvider.Tag.set
	}
}
