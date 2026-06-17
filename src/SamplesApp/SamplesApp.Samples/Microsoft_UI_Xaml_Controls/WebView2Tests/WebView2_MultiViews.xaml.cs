using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests;

[Sample("WebView",
	IsManualTest = true,
	IgnoreInSnapshotTests = true,
	Description = "Add tabs, each hosting a WebView2 driven by its own URL TextBox.")]
public sealed partial class WebView2_MultiViews : Page
{
	public WebView2_MultiViews()
	{
		this.InitializeComponent();
		CreateNewTab("https://platform.uno");
	}

	private void AddTabButtonClick(TabView sender, object args)
	{
		CreateNewTab();
	}

	private void TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
	{
		sender.TabItems.Remove(args.Tab);
	}

	private void CreateNewTab(string url = "https://www.google.com/")
	{
		var addressBar = new TextBox
		{
			PlaceholderText = "Enter a URL and press Enter",
			Text = url,
		};

		var webView = new WebView2()
		{
			Source = new Uri(url),
		};

		Grid.SetRow(addressBar, 0);
		Grid.SetRow(webView, 1);

		var content = new Grid()
		{
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
			},
			Children = { addressBar, webView },
		};

		var item = new TabViewItem
		{
			Header = "New Tab",
			Content = content,
		};

		addressBar.KeyDown += (s, e) =>
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				if (Uri.TryCreate(addressBar.Text, UriKind.Absolute, out var uri))
				{
					webView.Source = uri;
				}
				else if (!string.IsNullOrWhiteSpace(addressBar.Text))
				{
					webView.Source = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(addressBar.Text));
				}
			}
		};
		webView.NavigationStarting += (s, e) =>
		{
			addressBar.Text = e.Uri;
		};
		webView.NavigationCompleted += (s, e) =>
		{
			if (e.IsSuccess)
			{
				item.Header = webView.CoreWebView2.DocumentTitle;
			}
		};

		TabHost.TabItems.Add(item);
		TabHost.SelectedItem = item;
	}
}
