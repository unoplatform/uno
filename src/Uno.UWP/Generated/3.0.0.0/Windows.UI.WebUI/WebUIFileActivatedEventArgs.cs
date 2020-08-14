#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebUIFileActivatedEventArgs : global::Windows.ApplicationModel.Activation.IFileActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs,global::Windows.ApplicationModel.Activation.IApplicationViewActivatedEventArgs,global::Windows.UI.WebUI.IActivatedEventArgsDeferral,global::Windows.ApplicationModel.Activation.IFileActivatedEventArgsWithNeighboringFiles,global::Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ActivationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivationKind WebUIFileActivatedEventArgs.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ApplicationExecutionState PreviousExecutionState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationExecutionState WebUIFileActivatedEventArgs.PreviousExecutionState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.SplashScreen SplashScreen
		{
			get
			{
				throw new global::System.NotImplementedException("The member SplashScreen WebUIFileActivatedEventArgs.SplashScreen is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User WebUIFileActivatedEventArgs.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int CurrentlyShownApplicationViewId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int WebUIFileActivatedEventArgs.CurrentlyShownApplicationViewId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem> Files
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<IStorageItem> WebUIFileActivatedEventArgs.Files is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Verb
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebUIFileActivatedEventArgs.Verb is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Search.StorageFileQueryResult NeighboringFilesQuery
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFileQueryResult WebUIFileActivatedEventArgs.NeighboringFilesQuery is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WebUI.ActivatedOperation ActivatedOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivatedOperation WebUIFileActivatedEventArgs.ActivatedOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.Files.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.Verb.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.Kind.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.PreviousExecutionState.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.SplashScreen.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.CurrentlyShownApplicationViewId.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.ActivatedOperation.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.NeighboringFilesQuery.get
		// Forced skipping of method Windows.UI.WebUI.WebUIFileActivatedEventArgs.User.get
		// Processing: Windows.ApplicationModel.Activation.IFileActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IApplicationViewActivatedEventArgs
		// Processing: Windows.UI.WebUI.IActivatedEventArgsDeferral
		// Processing: Windows.ApplicationModel.Activation.IFileActivatedEventArgsWithNeighboringFiles
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	}
}
