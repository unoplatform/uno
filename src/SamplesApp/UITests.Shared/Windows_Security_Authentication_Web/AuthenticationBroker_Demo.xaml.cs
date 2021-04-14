using System;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

#if !NET6_0_OR_GREATER
using IdentityModel.OidcClient;
#endif

namespace SamplesApp.UITests.Windows_Security_Authentication_Web
{
	[Sample("Authentication", IsManualTest = true)]
	public sealed partial class AuthenticationBroker_Demo : Page
	{
#if !NET6_0_OR_GREATER
		public AuthenticationBroker_Demo()
		{
			this.InitializeComponent();
			PrepareClient();
		}

		private OidcClient _oidcClient;
		private AuthorizeState _loginState;
		private Uri _logoutUrl;

		private async void PrepareClient()
		{
			try
			{
				var redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;

				// Create options for endpoint discovery
				var options = new OidcClientOptions()
				{
					Authority = "https://demo.identityserver.io",
					ClientId = "interactive.confidential",
					ClientSecret = "secret",
					Scope = "openid profile email api offline_access",
					RedirectUri = redirectUri,
					PostLogoutRedirectUri = redirectUri,
					ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
					Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode
				};

				// Create the client. In production application, this is often created and stored
				// directly in the Application class.
				_oidcClient = new OidcClient(options);

				// Invoke Discovery and prepare a request state, containing the nonce.
				// This is done here to ensure the discovery mecanism is done before
				// the user clicks on the SignIn button. Since the opening of a web window
				// should be done during the handling of a user interaction (here it's the button click),
				// it will be too late to reach the discovery endpoint.
				// Not doing this could trigger popup blockers mechanisms in browsers.
				_loginState = await _oidcClient.PrepareLoginAsync();
				btnSignin.IsEnabled = true;

				resultTxt.Text = "Login URI correct";

				// Same for logout url.
				_logoutUrl = new Uri(await _oidcClient.PrepareLogoutAsync(new LogoutRequest()));
				btnSignout.IsEnabled = true;

				resultTxt.Text = $"Initialization completed.\nStart={_loginState.StartUrl}\nCallback={_loginState.RedirectUri}\nLogout={_logoutUrl}";
			}
			catch(Exception ex)
			{
				resultTxt.Text = $"Error {ex}";

			}
		}
#endif

		private async void SignIn_Clicked(object sender, RoutedEventArgs e)
		{
#if !NET6_0_OR_GREATER
			var startUri = new Uri(_loginState.StartUrl);

			// Important: there should be NO await before calling .AuthenticateAsync() - at least
			// on WebAssembly, in order to prevent trigering the popup blocker mechanisms.
			var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, startUri);

			if (userResult.ResponseStatus != WebAuthenticationStatus.Success)
			{
				resultTxt.Text = $"{userResult.ResponseStatus} {userResult.ResponseData}";
				// Error or user cancellation
				return;
			}

			// User authentication process completed successfully.
			// Now we need to get authorization tokens from the response
			var authenticationResult = await _oidcClient.ProcessResponseAsync(userResult.ResponseData, _loginState);

			if (authenticationResult.IsError)
			{
				var errorMessage = authenticationResult.Error;
				resultTxt.Text = $"ProcessResponseAsync() error - {errorMessage}";
				// TODO: do something with error message
				return;
			}

			// That's completed. Here you have to token, ready to do something
			var token = authenticationResult.AccessToken;
			var refreshToken = authenticationResult.RefreshToken;

			// TODO: make something useful with the tokens
			resultTxt.Text = $"Success - Token is {token}";
#endif
		}

		private async void SignOut_Clicked(object sender, RoutedEventArgs e)
		{
#if !NET6_0_OR_GREATER
			var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, _logoutUrl);
			resultTxt.Text = $"{userResult.ResponseStatus}";
#endif
		}
	}
}
