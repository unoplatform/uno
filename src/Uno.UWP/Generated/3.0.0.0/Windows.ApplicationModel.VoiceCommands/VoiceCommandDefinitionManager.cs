#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.VoiceCommands
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class VoiceCommandDefinitionManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinition> InstalledCommandDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, VoiceCommandDefinition> VoiceCommandDefinitionManager.InstalledCommandDefinitions is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20VoiceCommandDefinition%3E%20VoiceCommandDefinitionManager.InstalledCommandDefinitions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction InstallCommandDefinitionsFromStorageFileAsync( global::Windows.Storage.StorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(StorageFile file) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync%28StorageFile%20file%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstalledCommandDefinitions.get
	}
}
