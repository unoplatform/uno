#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoStabilizationEffectEnabledChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.VideoStabilizationEffectEnabledChangedReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoStabilizationEffectEnabledChangedReason VideoStabilizationEffectEnabledChangedEventArgs.Reason is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.VideoStabilizationEffectEnabledChangedEventArgs.Reason.get
	}
}
