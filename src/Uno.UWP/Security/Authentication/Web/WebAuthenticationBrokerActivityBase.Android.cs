using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace Uno.AuthenticationBroker
{
	public abstract class WebAuthenticationBrokerActivityBase : Activity
	{
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var data = Intent?.Data?.ToString();
			if (data != null)
			{
				// start the intermediate activity again with flags to close the custom tabs
				var intent = new Intent(this, typeof(WebAuthenticationBrokerRedirectActivity));
				intent.SetData(Intent?.Data);
				intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
				StartActivity(intent);

				// finish this activity
				Finish();
			}
		}
	}
}
