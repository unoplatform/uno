#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayTaskResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong PresentId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong DisplayTaskResult.PresentId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20DisplayTaskResult.PresentId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayPresentStatus PresentStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayPresentStatus DisplayTaskResult.PresentStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayPresentStatus%20DisplayTaskResult.PresentStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplaySourceStatus SourceStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplaySourceStatus DisplayTaskResult.SourceStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplaySourceStatus%20DisplayTaskResult.SourceStatus");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTaskResult.PresentStatus.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTaskResult.PresentId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTaskResult.SourceStatus.get
	}
}
