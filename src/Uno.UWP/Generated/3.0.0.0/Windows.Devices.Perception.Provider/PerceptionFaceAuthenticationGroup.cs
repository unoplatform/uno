#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionFaceAuthenticationGroup 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> FrameProviderIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> PerceptionFaceAuthenticationGroup.FrameProviderIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PerceptionFaceAuthenticationGroup( global::System.Collections.Generic.IEnumerable<string> ids,  global::Windows.Devices.Perception.Provider.PerceptionStartFaceAuthenticationHandler startHandler,  global::Windows.Devices.Perception.Provider.PerceptionStopFaceAuthenticationHandler stopHandler) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.Provider.PerceptionFaceAuthenticationGroup", "PerceptionFaceAuthenticationGroup.PerceptionFaceAuthenticationGroup(IEnumerable<string> ids, PerceptionStartFaceAuthenticationHandler startHandler, PerceptionStopFaceAuthenticationHandler stopHandler)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFaceAuthenticationGroup.PerceptionFaceAuthenticationGroup(System.Collections.Generic.IEnumerable<string>, Windows.Devices.Perception.Provider.PerceptionStartFaceAuthenticationHandler, Windows.Devices.Perception.Provider.PerceptionStopFaceAuthenticationHandler)
		// Forced skipping of method Windows.Devices.Perception.Provider.PerceptionFaceAuthenticationGroup.FrameProviderIds.get
	}
}
