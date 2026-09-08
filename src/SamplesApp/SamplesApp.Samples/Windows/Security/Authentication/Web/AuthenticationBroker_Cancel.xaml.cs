using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.Security.Authentication.Web;

namespace SamplesApp.UITests.Windows_Security_Authentication_Web
{
	[Sample("Windows.Security", IsManualTest = true, Description = "Dismissing the authentication sheet must report UserCancel instead of crashing.")]
	public sealed partial class AuthenticationBroker_Cancel : Page
	{
		// Registered as a CFBundleURLSchemes entry of the iOS SamplesApp heads.
		private static readonly Uri _callbackUri = new("uno-samples-test://callback");

		public AuthenticationBroker_Cancel()
		{
			this.InitializeComponent();
		}

		private async void Authenticate_Clicked(object sender, RoutedEventArgs e)
		{
			resultTxt.Text = "Authenticating...";

			try
			{
				var result = await WebAuthenticationBroker.AuthenticateAsync(
					WebAuthenticationOptions.None,
					new Uri("https://example.com/"),
					_callbackUri);

				resultTxt.Text = $"{result.ResponseStatus} {result.ResponseData}";
			}
			catch (Exception ex)
			{
				resultTxt.Text = $"Error {ex}";
			}
		}
	}
}
