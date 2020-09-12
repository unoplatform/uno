#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioStateMonitor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SoundLevel SoundLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member SoundLevel AudioStateMonitor.SoundLevel is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioStateMonitor.SoundLevelChanged.add
		// Forced skipping of method Windows.Media.Audio.AudioStateMonitor.SoundLevelChanged.remove
		// Forced skipping of method Windows.Media.Audio.AudioStateMonitor.SoundLevel.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForRenderMonitoring()
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForRenderMonitoring() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForRenderMonitoring( global::Windows.Media.Render.AudioRenderCategory category)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForRenderMonitoring(AudioRenderCategory category) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForRenderMonitoring( global::Windows.Media.Render.AudioRenderCategory category,  global::Windows.Media.Devices.AudioDeviceRole role)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForRenderMonitoring(AudioRenderCategory category, AudioDeviceRole role) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForRenderMonitoringWithCategoryAndDeviceId( global::Windows.Media.Render.AudioRenderCategory category,  string deviceId)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForRenderMonitoringWithCategoryAndDeviceId(AudioRenderCategory category, string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForCaptureMonitoring()
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForCaptureMonitoring() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForCaptureMonitoring( global::Windows.Media.Capture.MediaCategory category)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForCaptureMonitoring(MediaCategory category) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForCaptureMonitoring( global::Windows.Media.Capture.MediaCategory category,  global::Windows.Media.Devices.AudioDeviceRole role)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForCaptureMonitoring(MediaCategory category, AudioDeviceRole role) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioStateMonitor CreateForCaptureMonitoringWithCategoryAndDeviceId( global::Windows.Media.Capture.MediaCategory category,  string deviceId)
		{
			throw new global::System.NotImplementedException("The member AudioStateMonitor AudioStateMonitor.CreateForCaptureMonitoringWithCategoryAndDeviceId(MediaCategory category, string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.AudioStateMonitor, object> SoundLevelChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioStateMonitor", "event TypedEventHandler<AudioStateMonitor, object> AudioStateMonitor.SoundLevelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.AudioStateMonitor", "event TypedEventHandler<AudioStateMonitor, object> AudioStateMonitor.SoundLevelChanged");
			}
		}
		#endif
	}
}
