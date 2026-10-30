#if DEBUG // Workaround for https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2617
#define DISABLE_GRAPH
#elif __WASM__ // Microsoft.Graph is not referenced on wasm, see SamplesApp.csproj
#define DISABLE_GRAPH
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.MSAL;
using Uno.UI.Samples.Controls;
using Prompt = Microsoft.Identity.Client.Prompt;
#if __WASM__
using Uno;
#endif
#if !DISABLE_GRAPH
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Graph;
using System.Runtime.InteropServices;
#endif

namespace UITests.Msal
{
	[Sample("MSAL", IgnoreInSnapshotTests = true)]
	public sealed partial class MsalLoginAndGraph : Page
#if !DISABLE_GRAPH
	, IAuthenticationProvider
#endif
	{
		private const string CLIENT_ID = "03c890e9-d868-4d90-9c0a-919eff5b4d27";
		private const string TENANT_ID = "a297d6c0-b635-41a3-b1e3-558efe71e413";

		public string RedirectUri
		{
			get
			{
				if (OperatingSystem.IsOSPlatform("android"))
				{
					return "msauth://SamplesApp.Droid/BUWXtvbCbxw6rdZidSYhNH6gLvA%3D";
				}
				else if (OperatingSystem.IsOSPlatform("ios"))
				{
					return "msal" + CLIENT_ID + "://auth";
				}
				else if (OperatingSystem.IsOSPlatform("browser"))
				{
					return "http://localhost:55838/authentication/login-callback.htm";
				}
				else
				{
					return "https://login.microsoftonline.com/common/oauth2/nativeclient";
				}
			}
		}

		private readonly string[] SCOPES = new[] { "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/email", "https://graph.microsoft.com/profile" };

		private readonly IPublicClientApplication _app;
		private CancellationTokenSource _signInCts;

		public MsalLoginAndGraph()
		{
			this.InitializeComponent();

			_app = PublicClientApplicationBuilder
				.Create(CLIENT_ID)
				.WithTenantId(TENANT_ID)
				.WithRedirectUri(RedirectUri)
				.WithUnoHelpers()
				.Build();
		}

		private async void SignIn(object sender, RoutedEventArgs e)
		{
			_signInCts?.Cancel();
			var cts = _signInCts = new CancellationTokenSource();
#if __WASM__
			var originalTimeout = WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout;
			if (shortTimeout.IsChecked == true)
			{
				WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = TimeSpan.FromSeconds(5);
			}
#endif

			try
			{
				status.Text = "Signing in...";

				var result = await _app.AcquireTokenInteractive(SCOPES)
					.WithPrompt(Prompt.SelectAccount)
					.WithUnoHelpers()
					.ExecuteAsync(cts.Token);

				tokenBox.Text = result.AccessToken;
				status.Text = "Signed in";
			}
			catch (MsalException ex)
			{
				status.Text = $"{ex.GetType().Name} ({ex.ErrorCode}): {ex.Message}";
			}
			catch (Exception ex)
			{
				status.Text = $"{ex.GetType().Name}: {ex.Message}";
			}
			finally
			{
#if __WASM__
				WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = originalTimeout;
#endif
			}
		}

		private void CancelSignIn(object sender, RoutedEventArgs e)
			=> _signInCts?.Cancel();

		private
#if !DISABLE_GRAPH
			async
#endif
			void LoadFromGraph(object sender, RoutedEventArgs e)
		{
#if !DISABLE_GRAPH
			var httpClient = new HttpClient();
			var client = new GraphServiceClient(httpClient, this);

			try
			{
				using (var stream = await client.Me.Photo.Content.GetAsync())
				{
					var bitmap = new BitmapImage();
					await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
					thumbnail.Source = bitmap;
				}
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(exception);
			}

			try
			{
				var me = await client.Me.GetAsync();

				name.Text = me.DisplayName;
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(exception);
			}
#endif
		}

#if !DISABLE_GRAPH
		Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext, CancellationToken cancellationToken)
		{
			request.Headers.Add("Authorization", $"Bearer {tokenBox.Text}");
			return Task.CompletedTask;
		}
#endif
	}
}
