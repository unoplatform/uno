#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioDeviceModule 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ClassId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioDeviceModule.ClassId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioDeviceModule.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint InstanceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint AudioDeviceModule.InstanceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MajorVersion
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint AudioDeviceModule.MajorVersion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MinorVersion
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint AudioDeviceModule.MinorVersion is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModule.ClassId.get
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModule.DisplayName.get
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModule.InstanceId.get
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModule.MajorVersion.get
		// Forced skipping of method Windows.Media.Devices.AudioDeviceModule.MinorVersion.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Devices.ModuleCommandResult> SendCommandAsync( global::Windows.Storage.Streams.IBuffer Command)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ModuleCommandResult> AudioDeviceModule.SendCommandAsync(IBuffer Command) is not implemented in Uno.");
		}
		#endif
	}
}
