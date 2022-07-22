#nullable enable
using Android.Content;
using Uno.AuthenticationBroker;

namespace Windows.Security.Authentication.Web
{
	public static partial class WebAuthenticationBroker
	{
		public static bool OnResume(Intent? intent=null)
		{
			return (_authenticationBrokerProvider as WebAuthenticationBrokerProvider)?.OnResumeCallback(intent) ?? false;
		}
	}
}
