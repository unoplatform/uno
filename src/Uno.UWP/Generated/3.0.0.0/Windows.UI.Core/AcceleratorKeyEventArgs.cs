#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AcceleratorKeyEventArgs : global::Windows.UI.Core.ICoreWindowEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreAcceleratorKeyEventType EventType
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreAcceleratorKeyEventType AcceleratorKeyEventArgs.EventType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreAcceleratorKeyEventType%20AcceleratorKeyEventArgs.EventType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CorePhysicalKeyStatus AcceleratorKeyEventArgs.KeyStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CorePhysicalKeyStatus%20AcceleratorKeyEventArgs.KeyStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKey VirtualKey
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKey AcceleratorKeyEventArgs.VirtualKey is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=VirtualKey%20AcceleratorKeyEventArgs.VirtualKey");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AcceleratorKeyEventArgs.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AcceleratorKeyEventArgs.DeviceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AcceleratorKeyEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AcceleratorKeyEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.AcceleratorKeyEventArgs", "bool AcceleratorKeyEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.EventType.get
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.VirtualKey.get
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.KeyStatus.get
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Core.AcceleratorKeyEventArgs.DeviceId.get
		// Processing: Windows.UI.Core.ICoreWindowEventArgs
	}
}
