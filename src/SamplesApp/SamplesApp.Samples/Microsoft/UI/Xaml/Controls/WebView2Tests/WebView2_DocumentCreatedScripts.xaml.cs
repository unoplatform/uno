using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Registers JavaScript snippets via AddScriptToExecuteOnDocumentCreatedAsync; visible after Reload.")]
	public sealed partial class WebView2_DocumentCreatedScripts : Page
	{
		private readonly ObservableCollection<string> _ids = new();

		public WebView2_DocumentCreatedScripts()
		{
			this.InitializeComponent();
			ScriptList.ItemsSource = _ids;
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			try
			{
				await WebView.EnsureCoreWebView2Async();
			}
			catch (Exception ex)
			{
				_ids.Add($"FAILED: {ex.Message}");
			}
		}

		private async void OnAddClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				var id = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(ScriptInput.Text);
				_ids.Add(id);
			}
			catch (Exception ex)
			{
				// async void: an unobserved exception would tear the app down, so every failure is reported here.
				_ids.Add($"FAILED: {ex.Message}");
			}
		}

		private void OnRemoveClick(object sender, RoutedEventArgs e)
		{
			if (sender is Button { Tag: string id } && WebView.CoreWebView2 is not null)
			{
				if (id.StartsWith("FAILED:", StringComparison.Ordinal))
				{
					_ids.Remove(id);
					return;
				}

				WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(id);
				_ids.Remove(id);
			}
		}

		private void OnReloadClick(object sender, RoutedEventArgs e) => WebView.CoreWebView2?.Reload();
	}
}
