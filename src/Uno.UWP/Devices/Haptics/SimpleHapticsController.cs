namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		internal SimpleHapticsController()
		{
			InitPlatform();
		}

		partial void InitPlatform();

#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || __SKIA__
		public bool IsIntensitySupported => false;

		public bool IsPlayCountSupported => false;

		public bool IsPlayDurationSupported => false;

		public bool IsReplayPauseIntervalSupported => false;
#endif
	}
}
