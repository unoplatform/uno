using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Logs the CoreWebView2 top-level document lifecycle events.")]
	public sealed partial class WebView2_LifecycleEvents : Page
	{
		private readonly ObservableCollection<string> _entries = new();

		public WebView2_LifecycleEvents()
		{
			this.InitializeComponent();
			EventLog.ItemsSource = _entries;
			this.Loaded += async (_, _) =>
			{
				await WebView.EnsureCoreWebView2Async();
				var core = WebView.CoreWebView2;
				if (core is null)
				{
					return;
				}
				core.NavigationStarting += (_, e) => Log($"NavigationStarting uri={e.Uri}");
				core.NavigationCompleted += (_, e) => Log($"NavigationCompleted success={e.IsSuccess} status={e.HttpStatusCode}");
				core.ContentLoading += (_, e) => Log($"ContentLoading navId={e.NavigationId} isErrorPage={e.IsErrorPage}");
				core.DOMContentLoaded += (_, e) => Log($"DOMContentLoaded navId={e.NavigationId}");
			};
		}

		private void Log(string entry)
		{
			_entries.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {entry}");
			while (_entries.Count > 200)
			{
				_entries.RemoveAt(_entries.Count - 1);
			}
		}

		private void OnVisitClick(object sender, RoutedEventArgs e) => WebView.CoreWebView2?.Navigate("https://platform.uno");

		private void OnVisitBadClick(object sender, RoutedEventArgs e) => WebView.CoreWebView2?.Navigate("https://expired.badssl.com");

		private void OnClearClick(object sender, RoutedEventArgs e) => _entries.Clear();
	}
}
