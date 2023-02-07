#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Pickers
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FolderPicker 
	{
		// Skipping already declared property ViewMode
		// Skipping already declared property SuggestedStartLocation
		// Skipping already declared property SettingsIdentifier
		// Skipping already declared property CommitButtonText
		// Skipping already declared property FileTypeFilter
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet ContinuationData
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet FolderPicker.ContinuationData is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ValueSet%20FolderPicker.ContinuationData");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User FolderPicker.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20FolderPicker.User");
			}
		}
		#endif
		// Skipping already declared method Windows.Storage.Pickers.FolderPicker.FolderPicker()
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.FolderPicker()
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.ContinuationData.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PickFolderAndContinue()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Pickers.FolderPicker", "void FolderPicker.PickFolderAndContinue()");
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.ViewMode.get
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.ViewMode.set
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.SettingsIdentifier.get
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.SettingsIdentifier.set
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.SuggestedStartLocation.get
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.SuggestedStartLocation.set
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.CommitButtonText.get
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.CommitButtonText.set
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.FileTypeFilter.get
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> PickSingleFolderAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> FolderPicker.PickSingleFolderAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageFolder%3E%20FolderPicker.PickSingleFolderAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Storage.Pickers.FolderPicker.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Pickers.FolderPicker CreateForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member FolderPicker FolderPicker.CreateForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=FolderPicker%20FolderPicker.CreateForUser%28User%20user%29");
		}
		#endif
	}
}
