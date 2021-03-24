using Android.App;
using Android.Content.PM;
using Uno.AuthenticationBroker;

namespace SamplesApp.Droid
{
	[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter(
		new[] {Android.Content.Intent.ActionView},
		Categories = new[] {Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable},
		DataScheme = "uno-samples-auth")]
	public class WebAuthenticationBrokerActivity : WebAuthenticationBrokerActivityBase
	{
	}
}
