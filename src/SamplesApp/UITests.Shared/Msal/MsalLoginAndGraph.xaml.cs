#if DEBUG && __IOS__ // Workaround for https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2617
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.MSAL;
using Uno.UI.Samples.Controls;
using Prompt = Microsoft.Identity.Client.Prompt;
#if !DISABLE_GRAPH
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Graph;
#endif

namespace UITests.Msal
{
	[Sample("MSAL", IgnoreInSnapshotTests = true)]
	public sealed partial class MsalLoginAndGraph : Page
#if !DISABLE_GRAPH
	, IAuthenticationProvider
#endif
	{
#if !DISABLE_GRAPH
		private const string CLIENT_ID = "a74f513b-2d8c-45c0-a15a-15e63f7a7862";
		private const string TENANT_ID = "6d53ef61-b6d1-4150-ae0b-43b90e75e0cd";

#if __WASM__
		private const string REDIRECT_URI = "http://localhost:55838/authentication/login-callback.htm";
#elif __IOS__
		private const string REDIRECT_URI = "msal" + CLIENT_ID + "://auth";
#elif __ANDROID__
		private const string REDIRECT_URI = "msauth://SamplesApp.Droid/BUWXtvbCbxw6rdZidSYhNH6gLvA%3D";
#else
		private const string REDIRECT_URI = "https://login.microsoftonline.com/common/oauth2/nativeclient";
#endif

		private readonly string[] SCOPES = new[] { "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/email", "https://graph.microsoft.com/profile" };

		private readonly IPublicClientApplication _app;
#endif

		public MsalLoginAndGraph()
		{
			this.InitializeComponent();

#if !DISABLE_GRAPH
			_app = PublicClientApplicationBuilder
				.Create(CLIENT_ID)
				.WithTenantId(TENANT_ID)
				.WithRedirectUri(REDIRECT_URI)
				.WithUnoHelpers()
				.Build();
#endif
		}

		private
#if !DISABLE_GRAPH
			async
#endif
			void SignIn(object sender, RoutedEventArgs e)
		{
#if !DISABLE_GRAPH
			var result = await _app.AcquireTokenInteractive(SCOPES)
				.WithPrompt(Prompt.SelectAccount)
				.WithUnoHelpers()
				.ExecuteAsync();

			tokenBox.Text = result.AccessToken;
#endif
		}

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
