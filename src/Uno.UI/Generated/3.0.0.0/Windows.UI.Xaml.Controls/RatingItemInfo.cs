#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RatingItemInfo : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || false || false || false || false
		[global::Uno.NotImplemented]
		public RatingItemInfo() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.RatingItemInfo", "RatingItemInfo.RatingItemInfo()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemInfo.RatingItemInfo()
	}
}
