#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Haptics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SimpleHapticsController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SimpleHapticsController.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsIntensitySupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SimpleHapticsController.IsIntensitySupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPlayCountSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SimpleHapticsController.IsPlayCountSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPlayDurationSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SimpleHapticsController.IsPlayDurationSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsReplayPauseIntervalSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SimpleHapticsController.IsReplayPauseIntervalSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Haptics.SimpleHapticsControllerFeedback> SupportedFeedback
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<SimpleHapticsControllerFeedback> SimpleHapticsController.SupportedFeedback is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.Id.get
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.SupportedFeedback.get
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.IsIntensitySupported.get
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.IsPlayCountSupported.get
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.IsPlayDurationSupported.get
		// Forced skipping of method Windows.Devices.Haptics.SimpleHapticsController.IsReplayPauseIntervalSupported.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopFeedback()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Haptics.SimpleHapticsController", "void SimpleHapticsController.StopFeedback()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendHapticFeedback( global::Windows.Devices.Haptics.SimpleHapticsControllerFeedback feedback)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Haptics.SimpleHapticsController", "void SimpleHapticsController.SendHapticFeedback(SimpleHapticsControllerFeedback feedback)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendHapticFeedback( global::Windows.Devices.Haptics.SimpleHapticsControllerFeedback feedback,  double intensity)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Haptics.SimpleHapticsController", "void SimpleHapticsController.SendHapticFeedback(SimpleHapticsControllerFeedback feedback, double intensity)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendHapticFeedbackForDuration( global::Windows.Devices.Haptics.SimpleHapticsControllerFeedback feedback,  double intensity,  global::System.TimeSpan playDuration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Haptics.SimpleHapticsController", "void SimpleHapticsController.SendHapticFeedbackForDuration(SimpleHapticsControllerFeedback feedback, double intensity, TimeSpan playDuration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendHapticFeedbackForPlayCount( global::Windows.Devices.Haptics.SimpleHapticsControllerFeedback feedback,  double intensity,  int playCount,  global::System.TimeSpan replayPauseInterval)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Haptics.SimpleHapticsController", "void SimpleHapticsController.SendHapticFeedbackForPlayCount(SimpleHapticsControllerFeedback feedback, double intensity, int playCount, TimeSpan replayPauseInterval)");
		}
		#endif
	}
}
