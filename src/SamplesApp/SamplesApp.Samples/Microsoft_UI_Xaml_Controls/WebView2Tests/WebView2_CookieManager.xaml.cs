using System;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Exercises CoreWebView2.CookieManager add/list/delete; visit /cookies to verify in the page.")]
	public sealed partial class WebView2_CookieManager : Page
	{
		public WebView2_CookieManager()
		{
			this.InitializeComponent();
			this.Loaded += async (_, _) => await WebView.EnsureCoreWebView2Async();
		}

		private void OnAddClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				var cookie = WebView.CoreWebView2.CookieManager.CreateCookie(
					NameInput.Text, ValueInput.Text, DomainInput.Text, PathInput.Text);
				WebView.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
				OutputText.Text = $"Added/updated {cookie.Name}={cookie.Value} (domain={cookie.Domain}, path={cookie.Path})";
			}
			catch (Exception ex)
			{
				OutputText.Text = $"Add failed: {ex.Message}";
			}
		}

		private async void OnListClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				var cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://httpbin.org");
				var sb = new StringBuilder();
				sb.Append("Cookies for httpbin.org (").Append(cookies.Count).Append("):\n");
				foreach (var c in cookies)
				{
					sb.Append("  ").Append(c.Name).Append('=').Append(c.Value)
						.Append("  domain=").Append(c.Domain)
						.Append("  path=").Append(c.Path)
						.Append("  secure=").Append(c.IsSecure)
						.Append("  httpOnly=").Append(c.IsHttpOnly)
						.Append('\n');
				}
				OutputText.Text = sb.ToString();
			}
			catch (Exception ex)
			{
				OutputText.Text = $"List failed: {ex.Message}";
			}
		}

		private void OnDeleteClick(object sender, RoutedEventArgs e)
		{
			try
			{
				WebView.CoreWebView2?.CookieManager.DeleteCookies(NameInput.Text, "https://httpbin.org");
				OutputText.Text = $"Deleted cookies with name '{NameInput.Text}' on httpbin.org";
			}
			catch (Exception ex)
			{
				OutputText.Text = $"Delete failed: {ex.Message}";
			}
		}

		private void OnDeleteAllClick(object sender, RoutedEventArgs e)
		{
			try
			{
				WebView.CoreWebView2?.CookieManager.DeleteAllCookies();
				OutputText.Text = "Deleted all cookies.";
			}
			catch (Exception ex)
			{
				OutputText.Text = $"Delete-all failed: {ex.Message}";
			}
		}

		private void OnVisitClick(object sender, RoutedEventArgs e)
		{
			WebView.CoreWebView2?.Navigate("https://httpbin.org/cookies");
		}
	}
}
