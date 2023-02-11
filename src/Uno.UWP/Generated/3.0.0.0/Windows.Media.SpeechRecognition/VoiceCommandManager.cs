#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class VoiceCommandManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Media.SpeechRecognition.VoiceCommandSet> InstalledCommandSets
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, VoiceCommandSet> VoiceCommandManager.InstalledCommandSets is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20VoiceCommandSet%3E%20VoiceCommandManager.InstalledCommandSets");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction InstallCommandSetsFromStorageFileAsync( global::Windows.Storage.StorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(StorageFile file) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20VoiceCommandManager.InstallCommandSetsFromStorageFileAsync%28StorageFile%20file%29");
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.VoiceCommandManager.InstalledCommandSets.get
	}
}
