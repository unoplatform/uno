namespace Windows.System.Profile
{
	#if NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AnalyticsInfo 
	{
#if NET46 || __WASM__
		[global::Uno.NotImplemented]
#endif
		public static string DeviceForm
		{
			get
			{
				return VersionInfo.DeviceFamily;
			}
		}
#if NET46 || __WASM__
		[global::Uno.NotImplemented]
#endif
		public static global::Windows.System.Profile.AnalyticsVersionInfo VersionInfo
		{
			get; 
		} = new AnalyticsVersionInfo();

#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyDictionary<string, string>> GetSystemPropertiesAsync( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyDictionary<string, string>> AnalyticsInfo.GetSystemPropertiesAsync(IEnumerable<string> attributeNames) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.Profile.AnalyticsInfo.VersionInfo.get
		// Forced skipping of method Windows.System.Profile.AnalyticsInfo.DeviceForm.get
	}
}
