#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Activation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SplashScreen 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Rect ImageLocation
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect SplashScreen.ImageLocation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Activation.SplashScreen.ImageLocation.get
		// Forced skipping of method Windows.ApplicationModel.Activation.SplashScreen.Dismissed.add
		// Forced skipping of method Windows.ApplicationModel.Activation.SplashScreen.Dismissed.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Activation.SplashScreen, object> Dismissed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Activation.SplashScreen", "event TypedEventHandler<SplashScreen, object> SplashScreen.Dismissed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Activation.SplashScreen", "event TypedEventHandler<SplashScreen, object> SplashScreen.Dismissed");
			}
		}
		#endif
	}
}
