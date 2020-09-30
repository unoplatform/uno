#nullable enable
using System;
using Android.App;
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
				WebAuthenticationBrokerProvider.SetReturnData(new Uri(data));
				Finish();
			}
		}
	}
}
