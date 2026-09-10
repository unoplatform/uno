#nullable enable
using Android.Content;
using Uno.AuthenticationBroker;

namespace Windows.Security.Authentication.Web
{
	public static partial class WebAuthenticationBroker
	{
		internal static bool OnResume(Intent? intent = null) => (_authenticationBrokerProvider as WebAuthenticationBrokerProvider)?.OnResumeCallback(intent) ?? false;
	}
}
