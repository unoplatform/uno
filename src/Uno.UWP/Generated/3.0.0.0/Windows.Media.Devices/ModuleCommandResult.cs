#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ModuleCommandResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer ModuleCommandResult.Result is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.SendCommandStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member SendCommandStatus ModuleCommandResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ModuleCommandResult.Status.get
		// Forced skipping of method Windows.Media.Devices.ModuleCommandResult.Result.get
	}
}
