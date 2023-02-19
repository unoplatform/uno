using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation
{
	public sealed partial class SplashScreen
	{
#pragma warning disable CS0067 // The event 'SplashScreen.Dismissed' is never used
		public event TypedEventHandler<SplashScreen, object> Dismissed;
#pragma warning restore CS0067 // The event 'SplashScreen.Dismissed' is never used

		public Rect ImageLocation
		{
			get;
		}
	}
}
