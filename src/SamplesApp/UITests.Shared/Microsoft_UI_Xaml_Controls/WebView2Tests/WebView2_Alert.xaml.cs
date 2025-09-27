﻿using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;

namespace UITests.Shared.Windows_UI_Xaml
{
	[Sample("WebView", Name = "WebView2_Alert", ViewModelType = typeof(WebView2ViewModel))]
	public sealed partial class WebView2_Alert : Page
	{
		public WebView2_Alert()
		{
			this.InitializeComponent();
		}

		private async void Load(object sender, RoutedEventArgs e)
		{
			var html = ((WebView2ViewModel)DataContext).AlertHtml;
			await web.EnsureCoreWebView2Async();
			web.CoreWebView2.NavigateToString(html);
		}

		private async void StopScript(object sender, RoutedEventArgs e)
		{
			await web.CoreWebView2.ExecuteScriptAsync("count = -1;");
		}
	}
}
