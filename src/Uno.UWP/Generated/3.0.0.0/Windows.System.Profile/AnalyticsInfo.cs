#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public static partial class AnalyticsInfo 
	{
		// Skipping already declared property DeviceForm
		// Skipping already declared property VersionInfo
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyDictionary<string, string>> GetSystemPropertiesAsync( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyDictionary<string, string>> AnalyticsInfo.GetSystemPropertiesAsync(IEnumerable<string> attributeNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyDictionary%3Cstring%2C%20string%3E%3E%20AnalyticsInfo.GetSystemPropertiesAsync%28IEnumerable%3Cstring%3E%20attributeNames%29");
		}
		#endif
		// Forced skipping of method Windows.System.Profile.AnalyticsInfo.VersionInfo.get
		// Forced skipping of method Windows.System.Profile.AnalyticsInfo.DeviceForm.get
	}
}
