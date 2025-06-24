#if DEBUG // Workaround for https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2617
//#define DISABLE_GRAPH
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
#if !DISABLE_GRAPH
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
#endif

		public MsalLoginAndGraph()
		{
			this.InitializeComponent();

#if !DISABLE_GRAPH
			_app = PublicClientApplicationBuilder
				.Create(CLIENT_ID)
				.WithTenantId(TENANT_ID)
				.WithRedirectUri(RedirectUri)
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
