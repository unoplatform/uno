#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayToSource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToSource Next
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToSource PlayToSource.Next is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToSource", "PlayToSource PlayToSource.Next");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToConnection Connection
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToConnection PlayToSource.Connection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri PreferredSourceUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri PlayToSource.PreferredSourceUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToSource", "Uri PlayToSource.PreferredSourceUri");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToSource.Connection.get
		// Forced skipping of method Windows.Media.PlayTo.PlayToSource.Next.get
		// Forced skipping of method Windows.Media.PlayTo.PlayToSource.Next.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PlayNext()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToSource", "void PlayToSource.PlayNext()");
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToSource.PreferredSourceUri.get
		// Forced skipping of method Windows.Media.PlayTo.PlayToSource.PreferredSourceUri.set
	}
}
