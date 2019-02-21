#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Resources
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CustomXamlResourceLoader 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Resources.CustomXamlResourceLoader Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member CustomXamlResourceLoader CustomXamlResourceLoader.Current is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Resources.CustomXamlResourceLoader", "CustomXamlResourceLoader CustomXamlResourceLoader.Current");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public CustomXamlResourceLoader() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Resources.CustomXamlResourceLoader", "CustomXamlResourceLoader.CustomXamlResourceLoader()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Resources.CustomXamlResourceLoader.CustomXamlResourceLoader()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected virtual object GetResource( string resourceId,  string objectType,  string propertyName,  string propertyType)
		{
			throw new global::System.NotImplementedException("The member object CustomXamlResourceLoader.GetResource(string resourceId, string objectType, string propertyName, string propertyType) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Resources.CustomXamlResourceLoader.Current.get
		// Forced skipping of method Windows.UI.Xaml.Resources.CustomXamlResourceLoader.Current.set
	}
}
