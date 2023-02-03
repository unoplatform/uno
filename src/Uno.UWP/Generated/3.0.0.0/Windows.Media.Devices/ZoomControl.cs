#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ZoomControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ZoomControl.Value is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=float%20ZoomControl.Value");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ZoomControl", "float ZoomControl.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Max
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ZoomControl.Max is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=float%20ZoomControl.Max");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Min
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ZoomControl.Min is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=float%20ZoomControl.Min");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Step
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ZoomControl.Step is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=float%20ZoomControl.Step");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ZoomControl.Supported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ZoomControl.Supported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.ZoomTransitionMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ZoomTransitionMode ZoomControl.Mode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ZoomTransitionMode%20ZoomControl.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.ZoomTransitionMode> SupportedModes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ZoomTransitionMode> ZoomControl.SupportedModes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CZoomTransitionMode%3E%20ZoomControl.SupportedModes");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Min.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Max.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Step.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Value.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Value.set
		// Forced skipping of method Windows.Media.Devices.ZoomControl.SupportedModes.get
		// Forced skipping of method Windows.Media.Devices.ZoomControl.Mode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Configure( global::Windows.Media.Devices.ZoomSettings settings)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ZoomControl", "void ZoomControl.Configure(ZoomSettings settings)");
		}
		#endif
	}
}
