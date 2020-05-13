#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DefaultPresentationConfiguration : global::Windows.UI.WindowManagement.AppWindowPresentationConfiguration
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public DefaultPresentationConfiguration() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.DefaultPresentationConfiguration", "DefaultPresentationConfiguration.DefaultPresentationConfiguration()");
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.DefaultPresentationConfiguration.DefaultPresentationConfiguration()
	}
}
