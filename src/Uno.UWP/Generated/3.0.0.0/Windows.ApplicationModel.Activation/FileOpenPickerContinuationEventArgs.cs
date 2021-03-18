#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileOpenPickerContinuationEventArgs : global::Windows.ApplicationModel.Activation.IFileOpenPickerContinuationEventArgs,global::Windows.ApplicationModel.Activation.IContinuationActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ActivationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivationKind FileOpenPickerContinuationEventArgs.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ApplicationExecutionState PreviousExecutionState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationExecutionState FileOpenPickerContinuationEventArgs.PreviousExecutionState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.SplashScreen SplashScreen
		{
			get
			{
				throw new global::System.NotImplementedException("The member SplashScreen FileOpenPickerContinuationEventArgs.SplashScreen is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User FileOpenPickerContinuationEventArgs.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet ContinuationData
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet FileOpenPickerContinuationEventArgs.ContinuationData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.StorageFile> Files
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<StorageFile> FileOpenPickerContinuationEventArgs.Files is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.Files.get
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.ContinuationData.get
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.Kind.get
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.PreviousExecutionState.get
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.SplashScreen.get
		// Forced skipping of method Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs.User.get
		// Processing: Windows.ApplicationModel.Activation.IFileOpenPickerContinuationEventArgs
		// Processing: Windows.ApplicationModel.Activation.IContinuationActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	}
}
