#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioDeviceModulesManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AudioDeviceModulesManager( string deviceId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AudioDeviceModulesManager", "AudioDeviceModulesManager.AudioDeviceModulesManager(string deviceId)");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModulesManager.AudioDeviceModulesManager(string)
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModulesManager.ModuleNotificationReceived.add
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModulesManager.ModuleNotificationReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.AudioDeviceModule> FindAllById( string moduleId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<AudioDeviceModule> AudioDeviceModulesManager.FindAllById(string moduleId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.AudioDeviceModule> FindAll()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<AudioDeviceModule> AudioDeviceModulesManager.FindAll() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Devices.AudioDeviceModulesManager, global::Windows.Media.Devices.AudioDeviceModuleNotificationEventArgs> ModuleNotificationReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AudioDeviceModulesManager", "event TypedEventHandler<AudioDeviceModulesManager, AudioDeviceModuleNotificationEventArgs> AudioDeviceModulesManager.ModuleNotificationReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AudioDeviceModulesManager", "event TypedEventHandler<AudioDeviceModulesManager, AudioDeviceModuleNotificationEventArgs> AudioDeviceModulesManager.ModuleNotificationReceived");
			}
		}
		#endif
	}
}
