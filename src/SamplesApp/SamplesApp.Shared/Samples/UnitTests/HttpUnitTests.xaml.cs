using System;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Samples.UnitTests
{
	[Sample("WebAssembly", Name = "Wasm Http Handler")]
	public sealed partial class HttpUnitTests : Page
	{
		public HttpUnitTests()
		{
			this.InitializeComponent();
		}

		private async void Go(object sender, RoutedEventArgs e)
		{
			var handler = new HttpClientHandler();

			try
			{
				using (handler)
				using (var client = new HttpClient(handler))
				{
					Log("Creating a request message");
					var request = new HttpRequestMessage(HttpMethod.Get, new Uri(address.Text));
					Log("Sending request message");
					var response = await client.SendAsync(request);
					Log("Reading from response");
					var s = await response.Content.ReadAsStringAsync();
					result.Text = s;
				}
			}
			catch (Exception ex)
			{
				while (ex != null)
				{
					Log($"Exception: {ex}\n\n");
					ex = ex.InnerException;
				}
			}

		}

		private void Log(string s)
		{
			log.Text += $"{s}\n";
		}
	}
}
