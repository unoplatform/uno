using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_WebView2
{
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
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString(
			"""
			<html>
				<body>
					<script type="text/javascript">
						function sendMessage(){
							try{
								const message = {"some": ['values',"in","json",1]};
										
								if (typeof chrome !== 'undefined' &&
									chrome?.webview?.postMessage){
									chrome.webview.postMessage(message);
								} else if (unoWebMessageHandler){
									unoWebMessageHandler?.postMessage(JSON.stringify(message));
								}
							}
							catch (ex){
								alert("wtf" + ex);
							}
						}
					</script>
					<div id='test' style='width: 100px; height: 100px; background-color: blue;' />
				</body>
			</html>
			""");
		await TestServices.WindowHelper.WaitFor(() => navigated);
		string message = null;
		webView.WebMessageReceived += (s, e) =>
		{
			message = e.WebMessageAsJson;
		};
		await webView.ExecuteScriptAsync("sendMessage()");

		await TestServices.WindowHelper.WaitFor(() => message is not null);

		Assert.AreEqual(@"{""some"":[""values"",""in"",""json"",1]}", message);
	}
}
