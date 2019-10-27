#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AnalyticsInfo 
	{
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static string DeviceForm
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AnalyticsInfo.DeviceForm is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.System.Profile.AnalyticsVersionInfo VersionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AnalyticsVersionInfo AnalyticsInfo.VersionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
