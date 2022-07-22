#nullable enable
using System;
using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using Windows.Security.Authentication.Web;

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


	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, Exported = true)]
	internal class WebAuthenticationBrokerRedirectActivity : Activity
	{
		const string launchedExtra = "launched";
		const string actualIntentExtra = "actual_intent";

		bool launched;
		Intent? actualIntent;

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var extras = savedInstanceState ?? Intent?.Extras;

			// read the values
			launched = extras?.GetBoolean(launchedExtra, false)??false;
			actualIntent = extras?.GetParcelable(actualIntentExtra) as Intent;
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (!launched)
			{
				// if this is the first time, start the authentication flow
				StartActivity(actualIntent);

				launched = true;
			}
			else
			{
				// otherwise, resume the auth flow and finish this activity
				WebAuthenticationBroker.OnResume(Intent!);

				Finish();
			}
		}

		protected override void OnNewIntent(Intent? intent)
		{
			base.OnNewIntent(intent);

			Intent = intent;
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			// save the values
			outState.PutBoolean(launchedExtra, launched);
			outState.PutParcelable(actualIntentExtra, actualIntent);

			base.OnSaveInstanceState(outState);
		}

		public static void StartActivity(Activity activity, Intent intent)
		{
			var intermediateIntent = new Intent(activity, typeof(WebAuthenticationBrokerRedirectActivity));
			intermediateIntent.PutExtra(actualIntentExtra, intent);

			activity.StartActivity(intermediateIntent);
		}
	}
}
