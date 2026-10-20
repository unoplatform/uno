#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_WebResourceContextHelper
{
	[TestMethod]
	[DataRow("https://0.0.0.1/_framework/blazor.webview.js")]
	[DataRow("https://0.0.0.1/")]
	[DataRow("https://example.com/api/data")]
	public void When_Request_Is_For_Main_Frame_It_Is_A_Document(string url)
		=> Assert.AreEqual(CoreWebView2WebResourceContext.Document, WebResourceContextHelper.DetermineResourceContext(url, isForMainFrame: true, headers: null));

	[TestMethod]
	[DataRow("iframe", CoreWebView2WebResourceContext.Document)]
	[DataRow("style", CoreWebView2WebResourceContext.Stylesheet)]
	[DataRow("script", CoreWebView2WebResourceContext.Script)]
	[DataRow("image", CoreWebView2WebResourceContext.Image)]
	[DataRow("font", CoreWebView2WebResourceContext.Font)]
	[DataRow("video", CoreWebView2WebResourceContext.Media)]
	[DataRow("track", CoreWebView2WebResourceContext.TextTrack)]
	[DataRow("manifest", CoreWebView2WebResourceContext.Manifest)]
	[DataRow("empty", CoreWebView2WebResourceContext.Fetch)]
	public void When_Sec_Fetch_Dest_Is_Present_It_Wins(string destination, CoreWebView2WebResourceContext expected)
	{
		var headers = new Dictionary<string, string> { ["sec-fetch-dest"] = destination, ["Accept"] = "text/html" };

		Assert.AreEqual(expected, WebResourceContextHelper.DetermineResourceContext("https://0.0.0.1/file.png", isForMainFrame: false, headers));
	}

	[TestMethod]
	[DataRow("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", CoreWebView2WebResourceContext.Document)]
	[DataRow("text/css,*/*;q=0.1", CoreWebView2WebResourceContext.Stylesheet)]
	[DataRow("image/avif,image/webp,image/apng,image/*,*/*;q=0.8", CoreWebView2WebResourceContext.Image)]
	public void When_Accept_Identifies_The_Destination_It_Is_Used(string accept, CoreWebView2WebResourceContext expected)
	{
		var headers = new Dictionary<string, string> { ["Accept"] = accept };

		Assert.AreEqual(expected, WebResourceContextHelper.DetermineResourceContext("https://0.0.0.1/resource", isForMainFrame: false, headers));
	}

	[TestMethod]
	[DataRow("https://0.0.0.1/app.js", CoreWebView2WebResourceContext.Script)]
	[DataRow("https://0.0.0.1/app.css?v=1", CoreWebView2WebResourceContext.Stylesheet)]
	[DataRow("https://0.0.0.1/api/values", CoreWebView2WebResourceContext.Other)]
	public void When_Headers_Are_Inconclusive_The_Extension_Is_Used(string url, CoreWebView2WebResourceContext expected)
	{
		var headers = new Dictionary<string, string> { ["Accept"] = "*/*" };

		Assert.AreEqual(expected, WebResourceContextHelper.DetermineResourceContext(url, isForMainFrame: false, headers));
	}
}
