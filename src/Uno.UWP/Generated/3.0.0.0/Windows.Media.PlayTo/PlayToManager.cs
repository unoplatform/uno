#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayToManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool DefaultSourceSelection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlayToManager.DefaultSourceSelection is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "bool PlayToManager.DefaultSourceSelection");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.SourceRequested.add
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.SourceRequested.remove
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.SourceSelected.add
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.SourceSelected.remove
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.DefaultSourceSelection.set
		// Forced skipping of method Windows.Media.PlayTo.PlayToManager.DefaultSourceSelection.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.PlayTo.PlayToManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member PlayToManager PlayToManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowPlayToUI()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "void PlayToManager.ShowPlayToUI()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.PlayTo.PlayToManager, global::Windows.Media.PlayTo.PlayToSourceRequestedEventArgs> SourceRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "event TypedEventHandler<PlayToManager, PlayToSourceRequestedEventArgs> PlayToManager.SourceRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "event TypedEventHandler<PlayToManager, PlayToSourceRequestedEventArgs> PlayToManager.SourceRequested");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.PlayTo.PlayToManager, global::Windows.Media.PlayTo.PlayToSourceSelectedEventArgs> SourceSelected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "event TypedEventHandler<PlayToManager, PlayToSourceSelectedEventArgs> PlayToManager.SourceSelected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.PlayTo.PlayToManager", "event TypedEventHandler<PlayToManager, PlayToSourceSelectedEventArgs> PlayToManager.SourceSelected");
			}
		}
		#endif
	}
}
