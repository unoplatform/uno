#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Cortana
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CortanaActionableInsights 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User CortanaActionableInsights.User is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Cortana.CortanaActionableInsights.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> IsAvailableAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CortanaActionableInsights.IsAvailableAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsForImageAsync( global::Windows.Storage.Streams.IRandomAccessStreamReference imageStream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsForImageAsync(IRandomAccessStreamReference imageStream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsForImageAsync( global::Windows.Storage.Streams.IRandomAccessStreamReference imageStream,  global::Windows.Services.Cortana.CortanaActionableInsightsOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsForImageAsync(IRandomAccessStreamReference imageStream, CortanaActionableInsightsOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsForTextAsync( string text)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsForTextAsync(string text) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsForTextAsync( string text,  global::Windows.Services.Cortana.CortanaActionableInsightsOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsForTextAsync(string text, CortanaActionableInsightsOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsAsync( global::Windows.ApplicationModel.DataTransfer.DataPackage datapackage)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsAsync(DataPackage datapackage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowInsightsAsync( global::Windows.ApplicationModel.DataTransfer.DataPackage datapackage,  global::Windows.Services.Cortana.CortanaActionableInsightsOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CortanaActionableInsights.ShowInsightsAsync(DataPackage datapackage, CortanaActionableInsightsOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Cortana.CortanaActionableInsights GetDefault()
		{
			throw new global::System.NotImplementedException("The member CortanaActionableInsights CortanaActionableInsights.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Cortana.CortanaActionableInsights GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member CortanaActionableInsights CortanaActionableInsights.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
	}
}
