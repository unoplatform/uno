#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Pickers.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileOpenPickerUI 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileOpenPickerUI.Title is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "string FileOpenPickerUI.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> AllowedFileTypes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> FileOpenPickerUI.AllowedFileTypes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Pickers.Provider.FileSelectionMode SelectionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member FileSelectionMode FileOpenPickerUI.SelectionMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SettingsIdentifier
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileOpenPickerUI.SettingsIdentifier is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Pickers.Provider.AddFileResult AddFile( string id,  global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member AddFileResult FileOpenPickerUI.AddFile(string id, IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveFile( string id)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "void FileOpenPickerUI.RemoveFile(string id)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ContainsFile( string id)
		{
			throw new global::System.NotImplementedException("The member bool FileOpenPickerUI.ContainsFile(string id) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanAddFile( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member bool FileOpenPickerUI.CanAddFile(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.AllowedFileTypes.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.SelectionMode.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.SettingsIdentifier.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.Title.get
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.Title.set
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.FileRemoved.add
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.FileRemoved.remove
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.Closing.add
		// Forced skipping of method Windows.Storage.Pickers.Provider.FileOpenPickerUI.Closing.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.Pickers.Provider.FileOpenPickerUI, global::Windows.Storage.Pickers.Provider.PickerClosingEventArgs> Closing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "event TypedEventHandler<FileOpenPickerUI, PickerClosingEventArgs> FileOpenPickerUI.Closing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "event TypedEventHandler<FileOpenPickerUI, PickerClosingEventArgs> FileOpenPickerUI.Closing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.Pickers.Provider.FileOpenPickerUI, global::Windows.Storage.Pickers.Provider.FileRemovedEventArgs> FileRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "event TypedEventHandler<FileOpenPickerUI, FileRemovedEventArgs> FileOpenPickerUI.FileRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.Provider.FileOpenPickerUI", "event TypedEventHandler<FileOpenPickerUI, FileRemovedEventArgs> FileOpenPickerUI.FileRemoved");
			}
		}
		#endif
	}
}
