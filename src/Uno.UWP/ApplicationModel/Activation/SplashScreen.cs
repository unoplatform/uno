using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation
{
	public sealed partial class SplashScreen
	{
		public event TypedEventHandler<SplashScreen, object> Dismissed;

		public Rect ImageLocation
		{
			get;
		}

		private void OnDismissed()
		{
			Dismissed?.Invoke(this, null);
		}
	}
}
