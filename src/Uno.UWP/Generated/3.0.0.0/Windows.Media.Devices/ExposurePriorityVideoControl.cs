#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExposurePriorityVideoControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ExposurePriorityVideoControl.Enabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ExposurePriorityVideoControl", "bool ExposurePriorityVideoControl.Enabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ExposurePriorityVideoControl.Supported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ExposurePriorityVideoControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.ExposurePriorityVideoControl.Enabled.get
		// Forced skipping of method Windows.Media.Devices.ExposurePriorityVideoControl.Enabled.set
	}
}
