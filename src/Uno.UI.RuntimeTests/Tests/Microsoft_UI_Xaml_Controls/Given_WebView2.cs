using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

#if !HAS_UNO || __ANDROID__ || __IOS__ || __MACOS__
[TestClass]
[RunsOnUIThread]
public class Given_WebView2
{
	[TestMethod]
	public async Task When_Navigate()
	{
		var webView = new WebView2();
		var uri = new Uri("https://www.bing.com/");
		await webView.EnsureCoreWebView2Async();
		bool navigationDone = false;
		webView.NavigationCompleted += (s, e) => navigationDone = true;
		webView.CoreWebView2.Navigate(uri.ToString());
		Assert.IsNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => navigationDone);
		Assert.IsNotNull(webView.Source);
		Assert.IsTrue(webView.Source.OriginalString.StartsWith("https://www.bing.com/", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public async Task When_NavigateToString()
	{
		var webView = new WebView2();
		var uri = new Uri("https://www.bing.com/");
		await webView.EnsureCoreWebView2Async();
		bool navigationDone = false;
		webView.NavigationCompleted += (s, e) => navigationDone = true;
		webView.Source = uri;
		Assert.IsNotNull(webView.Source);
		await TestServices.WindowHelper.WaitFor(() => navigationDone);
		Assert.IsNotNull(webView.Source);
		webView.NavigateToString("<html></html>");
		Assert.IsTrue(webView.Source.OriginalString.StartsWith("https://www.bing.com/", StringComparison.OrdinalIgnoreCase));
		Assert.IsTrue(webView.CoreWebView2.Source.StartsWith("https://www.bing.com/", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public async Task When_ExecuteScriptAsync()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated);

		var color = await webView.ExecuteScriptAsync("eval({ 'color' : document.getElementById('test').style.backgroundColor })");
		Assert.AreEqual("{\"color\":\"blue\"}", color);

		// Change color to red
		await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red';");
		color = await webView.ExecuteScriptAsync("eval({ 'color' : document.getElementById('test').style.backgroundColor })");

		Assert.AreEqual("{\"color\":\"red\"}", color);
	}


	[TestMethod]
	public async Task When_WebMessageReceived()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();

		string message = null;
		webView.WebMessageReceived += (s, e) =>
		{
			message = e.WebMessageAsJson;
		};

		webView.NavigateToString(
			"""
			<html>
				<head>
					<script type="text/javascript">
						function sendMessage(){
							try{
								const message = {"some": ['values',"in","json",1]};
										
								if (window.hasOwnProperty("chrome") && chrome?.webview?.postMessage){
									chrome.webview.postMessage(message);
								} else if (window.hasOwnProperty("unoWebMessageHandler")){
									unoWebMessageHandler?.postMessage(JSON.stringify(message));
								} else if (window.hasOwnProperty("webkit") && webkit?.messageHandlers?.unoWebView?.postMessage){
									webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
								}
							}
							catch (ex){
								alert("Error occurred: " + ex);
							}
						}
					</script>
				</head>
				<body onload="sendMessage();">
					<div id='test' style='width: 100px; height: 100px; background-color: blue;' />
				</body>
			</html>
			""");

		await TestServices.WindowHelper.WaitFor(() => message is not null);

		Assert.AreEqual(@"{""some"":[""values"",""in"",""json"",1]}", message);
	}
}
#endif
