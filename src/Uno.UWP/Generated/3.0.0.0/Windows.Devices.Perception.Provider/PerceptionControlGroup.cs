#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionControlGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> FrameProviderIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> PerceptionControlGroup.FrameProviderIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PerceptionControlGroup( global::System.Collections.Generic.IEnumerable<string> ids) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionControlGroup", "PerceptionControlGroup.PerceptionControlGroup(IEnumerable<string> ids)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionControlGroup.PerceptionControlGroup(System.Collections.Generic.IEnumerable<string>)
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionControlGroup.FrameProviderIds.get
	}
}
