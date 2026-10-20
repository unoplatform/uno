using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Microsoft.Web.WebView2.Core;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml
{
	[Sample("WebView", Name = "WebView2_TargetBlank", ViewModelType = typeof(WebView2ViewModel))]
	public sealed partial class WebView2_TargetBlank : Page
	{
		public WebView2_TargetBlank()
		{
			this.InitializeComponent();
			this.Loaded += WebView2_TargetBlank_Loaded;
		}

		private void WebView2_TargetBlank_Loaded(object sender, RoutedEventArgs e)
		{
			const string htmlContent = @"
			<html>
			<head>
				<title>Repro</title>
				<style>
					body { font-family: sans-serif; padding: 20px; }
					h1 { color: #333; }
					p { line-height: 1.6; }
					a { color: #0078d4; text-decoration: none; }
					a:hover { text-decoration: underline; }
					code { background: #f4f4f4; padding: 2px 6px; border-radius: 4px; }
				</style>
			</head>
			<body>
				<h1>WebView2 New Window Test</h1>
				<p>This app demonstrates GitHub issue #382.</p>
				<hr>
				<p>This link should navigate inside the WebView:</p>
				<a href=""https://platform.uno/"">Uno (Normal Link)</a>
				<br><br>
				<p>This link will open a new browser window (the bug):</p>
				<a href=""https://platform.uno/studio/"" target=""_blank"">Uno Platform Studio (target=""_blank"")</a>
				<br><br>
				<hr>
				<p>
					The <code>target=""_blank""</code> link fires the <strong>NewWindowRequested</strong> event.
					If this event is not handled, the default behavior is to open a new browser window.
					This file demonstrates the un-handled case, which reproduces the bug.
				</p>
				<p>
					<strong>Expected behavior:</strong> The app should handle <code>NewWindowRequested</code>, 
					set <code>e.Handled = true</code>, and then navigate the <em>existing</em> WebView 
					to the target URI.
				</p>
			</body>
			</html>";

			MyWebView.NavigateToString(htmlContent);

			StatusText.Text = $"Page loaded. Navigated to string.";

			MyWebView.CoreWebView2.NewWindowRequested += WebView_NewWindowRequested;
		}

		private void WebView_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
		{
			args.Handled = true;

			StatusText.Text = $"NewWindowRequested for URI: {args.Uri}";

			sender.Navigate(args.Uri);
		}
	}
}
