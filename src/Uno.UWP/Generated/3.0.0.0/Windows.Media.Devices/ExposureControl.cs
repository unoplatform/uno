#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExposureControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Auto
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ExposureControl.Auto is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ExposureControl.Auto");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Max
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ExposureControl.Max is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20ExposureControl.Max");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Min
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ExposureControl.Min is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20ExposureControl.Min");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Step
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ExposureControl.Step is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20ExposureControl.Step");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ExposureControl.Supported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ExposureControl.Supported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ExposureControl.Value is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20ExposureControl.Value");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Auto.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetAutoAsync( bool value)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ExposureControl.SetAutoAsync(bool value) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ExposureControl.SetAutoAsync%28bool%20value%29");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Min.get
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Max.get
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Step.get
		// Forced skipping of method Windows.Media.Devices.ExposureControl.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetValueAsync( global::System.TimeSpan shutterDuration)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ExposureControl.SetValueAsync(TimeSpan shutterDuration) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ExposureControl.SetValueAsync%28TimeSpan%20shutterDuration%29");
		}
		#endif
	}
}
