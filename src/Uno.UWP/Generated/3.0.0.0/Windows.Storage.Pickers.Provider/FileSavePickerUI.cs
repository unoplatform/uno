#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Pickers.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileSavePickerUI 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileSavePickerUI.Title is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20FileSavePickerUI.Title");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileSavePickerUI", "string FileSavePickerUI.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> AllowedFileTypes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> FileSavePickerUI.AllowedFileTypes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20FileSavePickerUI.AllowedFileTypes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string FileName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileSavePickerUI.FileName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20FileSavePickerUI.FileName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SettingsIdentifier
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileSavePickerUI.SettingsIdentifier is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20FileSavePickerUI.SettingsIdentifier");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.Title.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.Title.set
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.AllowedFileTypes.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.SettingsIdentifier.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.FileName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Pickers.Provider.SetFileNameResult TrySetFileName( string value)
		{
			throw new global::System.NotImplementedException("The member SetFileNameResult FileSavePickerUI.TrySetFileName(string value) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SetFileNameResult%20FileSavePickerUI.TrySetFileName%28string%20value%29");
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.FileNameChanged.add
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.FileNameChanged.remove
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.TargetFileRequested.add
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileSavePickerUI.TargetFileRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.Pickers.Provider.FileSavePickerUI, object> FileNameChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileSavePickerUI", "event TypedEventHandler<FileSavePickerUI, object> FileSavePickerUI.FileNameChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileSavePickerUI", "event TypedEventHandler<FileSavePickerUI, object> FileSavePickerUI.FileNameChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.Pickers.Provider.FileSavePickerUI, global::Windows.Storage.Pickers.Provider.TargetFileRequestedEventArgs> TargetFileRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileSavePickerUI", "event TypedEventHandler<FileSavePickerUI, TargetFileRequestedEventArgs> FileSavePickerUI.TargetFileRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileSavePickerUI", "event TypedEventHandler<FileSavePickerUI, TargetFileRequestedEventArgs> FileSavePickerUI.TargetFileRequested");
			}
		}
		#endif
	}
}
