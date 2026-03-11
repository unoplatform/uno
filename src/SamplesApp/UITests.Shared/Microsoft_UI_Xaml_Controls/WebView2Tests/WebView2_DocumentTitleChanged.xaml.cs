#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", IgnoreInSnapshotTests = true, Name = "WebView2_DocumentTitleChanged", Description = "Shows how to listen for CoreWebView2.DocumentTitleChanged and read the updated title.")]
	public sealed partial class WebView2_DocumentTitleChanged : UserControl
	{
		private CoreWebView2? _coreWebView2;

		public WebView2_DocumentTitleChanged()
		{
			InitializeComponent();
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await InitializeCoreAsync();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (_coreWebView2 is not null)
			{
				_coreWebView2.DocumentTitleChanged -= OnDocumentTitleChanged;
				_coreWebView2 = null;
			}
		}

		private async Task InitializeCoreAsync()
		{
			if (_coreWebView2 is not null)
			{
				UpdateTitle(_coreWebView2.DocumentTitle);
				return;
			}

			try
			{
				if (SampleWebView.CoreWebView2 is null)
				{
					await SampleWebView.EnsureCoreWebView2Async();
				}
				var core = SampleWebView.CoreWebView2;
				if (core is null)
				{
					UpdateStatus("Failed to obtain CoreWebView2 instance after initialization.");
					return;
				}
				_coreWebView2 = core;
				core.DocumentTitleChanged += OnDocumentTitleChanged;
				UpdateStatus("Subscribed to DocumentTitleChanged.");
				UpdateTitle(core.DocumentTitle);
			}
			catch (Exception ex)
			{
				UpdateStatus($"Initialization failed: {ex.Message}");
			}
		}

		private void OnDocumentTitleChanged(CoreWebView2 sender, object args)
		{
			DispatcherQueue?.TryEnqueue(() =>
			{
				UpdateStatus("DocumentTitleChanged raised. Title pulled from CoreWebView2.DocumentTitle.");
				UpdateTitle(sender.DocumentTitle);
			});
		}

		private void OnNavigateClicked(object sender, RoutedEventArgs e)
		{
			if (Uri.TryCreate(AddressBar.Text, UriKind.Absolute, out var uri))
			{
				SampleWebView.Source = uri;
				UpdateStatus($"Navigating to {uri}...");
			}
			else
			{
				UpdateStatus("Enter a valid absolute URI before navigating.");
			}
		}

		private void OnReloadClicked(object sender, RoutedEventArgs e)
		{
			SampleWebView.Reload();
			UpdateStatus("Reload requested.");
		}

		private void UpdateTitle(string? title)
		{
			TitleText.Text = string.IsNullOrWhiteSpace(title) ? "(no title provided)" : title;
		}

		private void UpdateStatus(string message)
		{
			StatusText.Text = $"Status: {message}";
		}
	}
}
