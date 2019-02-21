#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AnalyticsVersionInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceFamily
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AnalyticsVersionInfo.DeviceFamily is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceFamilyVersion
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AnalyticsVersionInfo.DeviceFamilyVersion is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Profile.AnalyticsVersionInfo.DeviceFamily.get
		// Forced skipping of method Windows.System.Profile.AnalyticsVersionInfo.DeviceFamilyVersion.get
	}
}
