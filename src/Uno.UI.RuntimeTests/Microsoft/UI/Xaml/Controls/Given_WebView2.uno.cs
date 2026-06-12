#if HAS_UNO_WINUI
using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaFrameBuffer)]
public class Given_WebView2
{
	[TestMethod]
#if __SKIA__
	[Ignore("WebView2 is not yet supported on skia targets")]
#endif
	public async Task When_InvokeScriptAsync()
	{
		var border = new Border();
		var webView = new WebView2();
		webView.Width = 200;
		webView.Height = 200;
		border.Child = webView;
		TestServices.WindowHelper.WindowContent = border;
		bool navigated = false;
		await TestServices.WindowHelper.WaitForLoaded(border);
		webView.NavigationCompleted += (sender, e) => navigated = true;
		webView.NavigateToString("<html><body><div id='test' style='width: 100px; height: 100px; background-color: blue;' /></body></html>");
		await TestServices.WindowHelper.WaitFor(() => navigated, timeoutMS: 10000);

		var sw = Stopwatch.StartNew();
		string color = null;

		do
		{
			// We need to wait for the element to be available, navigated
			// may be set to true too early on wasm.
			color = await webView.ExecuteScriptAsync(
				"""
				(function () {
					let testElement = document.getElementById('test');
					if(testElement){
						return testElement.style.backgroundColor.toString();
					}
					return "";
				})()
				""");

		} while (sw.Elapsed < TimeSpan.FromSeconds(10) && string.IsNullOrEmpty(color.Replace("\"", "")));

		Assert.AreEqual("\"blue\"", color);

		// Change color to red
		await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red'");
		color = await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor.toString()");

		Assert.AreEqual("\"red\"", color);
	}
}
#endif
