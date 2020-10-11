namespace Windows.Devices.Haptics
{
	public partial class SimpleHapticsController
	{
		internal SimpleHapticsController()
		{
		}

#if __ANDROID__ || __IOS__ || __MACOS__
		public bool IsIntensitySupported => false;

		public bool IsPlayCountSupported => false;

		public bool IsPlayDurationSupported => false;

		public bool IsReplayPauseIntervalSupported => false;
#endif
	}
}
