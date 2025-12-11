using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	/// <summary>
	/// Sample to reproduce WebView2 disappearing when switching between TabView tabs.
	/// Issue: WebViews begin to disappear the second time you navigate back to a tab.
	/// This matches the real-world scenario where multiple WebView2 instances exist
	/// in different tabs and become invisible/unloaded when switching tabs.
	/// 
	/// Repro steps:
	/// 1. App loads with Tab 1 visible - WebView1 appears
	/// 2. Switch to Tab 2 - WebView2 appears
	/// 3. Switch back to Tab 1 - WebView1 may disappear (the bug)
	/// 4. Observe the visit counts and WebView status to see what's happening
	/// </summary>
	[SampleControlInfo("WebView", "WebView2_NavigationReparent",
		description: "Repro for WebView2 disappearing when switching TabView tabs. Switch Tab1 → Tab2 → Tab1 and observe if WebView disappears on second Tab1 visit.")]
	public sealed partial class WebView2_NavigationReparent : Page
	{
		private int _tab1VisitCount = 0;
		private int _tab2VisitCount = 0;
		private int _tab3VisitCount = 0;

		private int _webView1LoadCount = 0;
		private int _webView2LoadCount = 0;
		private int _webView3LoadCount = 0;

		public WebView2_NavigationReparent()
		{
			this.InitializeComponent();
		}

		private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainTabView.SelectedIndex == 0)
			{
				_tab1VisitCount++;
				Tab1VisitCount.Text = $"Visit count: {_tab1VisitCount}";
				UpdateStatus($"Switched to Tab 1 (visit #{_tab1VisitCount})");
			}
			else if (MainTabView.SelectedIndex == 1)
			{
				_tab2VisitCount++;
				Tab2VisitCount.Text = $"Visit count: {_tab2VisitCount}";
				UpdateStatus($"Switched to Tab 2 (visit #{_tab2VisitCount})");
			}
			else if (MainTabView.SelectedIndex == 2)
			{
				_tab3VisitCount++;
				Tab3VisitCount.Text = $"Visit count: {_tab3VisitCount}";
				UpdateStatus($"Switched to Tab 3 (visit #{_tab3VisitCount})");
			}
		}

		// WebView 1 events
		private void OnWebView1Loaded(object sender, RoutedEventArgs e)
		{
			_webView1LoadCount++;
			Tab1WebViewStatus.Text = $"WebView: ✅ LOADED (#{_webView1LoadCount})";
			UpdateStatus($"WebView1 loaded (count: {_webView1LoadCount})");
		}

		private void OnWebView1Unloaded(object sender, RoutedEventArgs e)
		{
			Tab1WebViewStatus.Text = "WebView: ❌ UNLOADED from visual tree";
			UpdateStatus("WebView1 unloaded");
		}

		private void OnWebView1NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			Tab1WebViewStatus.Text = $"WebView: 🔄 Navigating...";
		}

		private void OnWebView1NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			var status = args.IsSuccess ? "✅ Navigation complete" : $"❌ Failed: {args.WebErrorStatus}";
			Tab1WebViewStatus.Text = $"WebView: {status}";
		}

		// WebView 2 events
		private void OnWebView2Loaded(object sender, RoutedEventArgs e)
		{
			_webView2LoadCount++;
			Tab2WebViewStatus.Text = $"WebView: ✅ LOADED (#{_webView2LoadCount})";
			UpdateStatus($"WebView2 loaded (count: {_webView2LoadCount})");
		}

		private void OnWebView2Unloaded(object sender, RoutedEventArgs e)
		{
			Tab2WebViewStatus.Text = "WebView: ❌ UNLOADED from visual tree";
			UpdateStatus("WebView2 unloaded");
		}

		private void OnWebView2NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			Tab2WebViewStatus.Text = $"WebView: 🔄 Navigating...";
		}

		private void OnWebView2NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			var status = args.IsSuccess ? "✅ Navigation complete" : $"❌ Failed: {args.WebErrorStatus}";
			Tab2WebViewStatus.Text = $"WebView: {status}";
		}

		// WebView 3 events
		private void OnWebView3Loaded(object sender, RoutedEventArgs e)
		{
			_webView3LoadCount++;
			Tab3WebViewStatus.Text = $"WebView: ✅ LOADED (#{_webView3LoadCount})";
			UpdateStatus($"WebView3 loaded (count: {_webView3LoadCount})");
		}

		private void OnWebView3Unloaded(object sender, RoutedEventArgs e)
		{
			Tab3WebViewStatus.Text = "WebView: ❌ UNLOADED from visual tree";
			UpdateStatus("WebView3 unloaded");
		}

		private void OnWebView3NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			Tab3WebViewStatus.Text = $"WebView: 🔄 Navigating...";
		}

		private void OnWebView3NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			var status = args.IsSuccess ? "✅ Navigation complete" : $"❌ Failed: {args.WebErrorStatus}";
			Tab3WebViewStatus.Text = $"WebView: {status}";
		}

		private void UpdateStatus(string message)
		{
			StatusText.Text = $"Status: {message}";
		}
	}
}