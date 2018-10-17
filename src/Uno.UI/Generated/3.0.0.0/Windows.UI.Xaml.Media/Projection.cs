#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Projection : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || false || false || false || false
		[global::Uno.NotImplemented]
		protected Projection() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Projection", "Projection.Projection()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Projection.Projection()
	}
}
