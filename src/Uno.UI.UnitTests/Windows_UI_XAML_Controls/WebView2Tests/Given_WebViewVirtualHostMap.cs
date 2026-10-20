#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_WebViewVirtualHostMap
{
	[TestMethod]
	[DataRow("https://0.0.0.1/", "uno-app://0.0.0.1/")]
	[DataRow("https://0.0.0.1/index.html", "uno-app://0.0.0.1/index.html")]
	[DataRow("https://0.0.0.1/_framework/blazor.webview.js?v=1&x=%20y#frag", "uno-app://0.0.0.1/_framework/blazor.webview.js?v=1&x=%20y#frag")]
	[DataRow("https://0.0.0.0/", "uno-app://0.0.0.0/")]
	[DataRow("https://0.255.255.255/a", "uno-app://0.255.255.255/a")]
	[DataRow("https://0.0.0.1:8443/a?b#c", "uno-app://0.0.0.1:8443/a?b#c")]
	[DataRow("https://0.0.0.1/a%2Fb/c%23d", "uno-app://0.0.0.1/a%2Fb/c%23d")]
	public void When_Virtual_Url_It_Round_Trips(string publicUrl, string nativeUrl)
	{
		var map = new WebViewVirtualHostMap();

		Assert.IsTrue(map.IsVirtual(new Uri(publicUrl)));
		Assert.AreEqual(nativeUrl, map.ToNative(publicUrl));
		Assert.AreEqual(publicUrl, map.ToPublic(nativeUrl));
		Assert.AreEqual(new Uri(nativeUrl), map.ToNative(new Uri(publicUrl)));
		Assert.AreEqual(new Uri(publicUrl), map.ToPublic(map.ToNative(new Uri(publicUrl))));
	}

	[TestMethod]
	public void When_Default_Port_It_Is_Dropped_Both_Ways()
	{
		var map = new WebViewVirtualHostMap();

		var native = map.ToNative(new Uri("https://0.0.0.1:443/a"));

		Assert.AreEqual("uno-app://0.0.0.1/a", native.AbsoluteUri);
		Assert.AreEqual("https://0.0.0.1/a", map.ToPublic(native).AbsoluteUri);
	}

	[TestMethod]
	[DataRow("https://example.com/")]
	[DataRow("https://1.0.0.1/")]
	[DataRow("https://10.0.0.1/")]
	[DataRow("https://127.0.0.1/")]
	[DataRow("https://00.example/")]
	[DataRow("https://0.0.0.1.example.com/")]
	[DataRow("http://0.0.0.1/")]
	[DataRow("file:///0.0.0.1/a")]
	[DataRow("about:blank")]
	[DataRow("data:text/html;base64,PGh0bWw+")]
	[DataRow("not a url")]
	[DataRow("")]
	public void When_Not_Virtual_It_Is_Unchanged(string url)
	{
		var map = new WebViewVirtualHostMap();

		Assert.AreSame(url, map.ToNative(url));
		Assert.AreSame(url, map.ToPublic(url));
		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			Assert.IsFalse(map.IsVirtual(uri));
			Assert.AreSame(uri, map.ToNative(uri));
		}
	}

	[TestMethod]
	public void When_Native_Url_Of_Unknown_Host_It_Is_Not_Made_Public()
	{
		var map = new WebViewVirtualHostMap();

		Assert.AreEqual("uno-app://example.com/a", map.ToPublic("uno-app://example.com/a"));
		Assert.AreEqual("https://example.com/a", map.ToPublic("https://example.com/a"));
	}

	[TestMethod]
	[DataRow("0.0.0.0", true)]
	[DataRow("0.0.0.1", true)]
	[DataRow("0.1.2.3", true)]
	[DataRow("0.255.255.255", true)]
	[DataRow("1.0.0.0", false)]
	[DataRow("0.0.0.256", false)]
	[DataRow("0.0.1", false)]
	[DataRow("0", false)]
	[DataRow("0.0.0.0.0", false)]
	[DataRow("0.0.0.a", false)]
	[DataRow("0.0.0.-1", false)]
	[DataRow("0.0.0.0001", false)]
	[DataRow("::1", false)]
	public void When_Checking_NonRoutable_Range(string host, bool expected)
		=> Assert.AreEqual(expected, WebViewVirtualHostMap.IsNonRoutableIPv4(host));

	[TestMethod]
	public void When_NonRoutable_Range_Disabled_Only_Registered_Hosts_Are_Virtual()
	{
		var map = new WebViewVirtualHostMap { IncludeNonRoutableHosts = false };
		map.SetHost("0.0.0.2");

		Assert.IsFalse(map.IsVirtual(new Uri("https://0.0.0.1/")));
		Assert.IsTrue(map.IsVirtual(new Uri("https://0.0.0.2/")));
	}

	[TestMethod]
	public void When_Host_Registered_It_Is_Virtual_Case_Insensitively_Until_Removed()
	{
		var map = new WebViewVirtualHostMap();
		map.SetHost("AppAssets.Example", "wwwroot");

		Assert.AreEqual("uno-app://appassets.example/index.html", map.ToNative("https://appassets.example/index.html"));
		Assert.AreEqual("https://appassets.example/index.html", map.ToPublic("uno-app://appassets.example/index.html"));
		Assert.IsTrue(map.TryGetFolder("appassets.example", out var folder));
		Assert.AreEqual("wwwroot", folder);
		Assert.IsFalse(map.TryGetFolder("0.0.0.1", out _));

		Assert.IsTrue(map.RemoveHost("APPASSETS.EXAMPLE"));

		Assert.AreEqual("https://appassets.example/index.html", map.ToNative("https://appassets.example/index.html"));
	}

	[TestMethod]
	public void When_String_Is_Translated_The_Rest_Keeps_Its_Form()
	{
		var map = new WebViewVirtualHostMap();

		// Uri.ToString() unescapes; the in-place swap must not re-escape it.
		Assert.AreEqual("https://0.0.0.1/a b", map.ToPublic("uno-app://0.0.0.1/a b"));
		Assert.AreEqual("uno-app://0.0.0.1/a b", map.ToNative("HTTPS://0.0.0.1/a b"));
	}

	[TestMethod]
	[DataRow("https://0.0.0.1/*", "uno-app://0.0.0.1/*")]
	[DataRow("https://0.0.0.1:8443/*", "uno-app://0.0.0.1:8443/*")]
	[DataRow("https://0.0.0.1", "uno-app://0.0.0.1")]
	[DataRow("https://*/*", "https://*/*")]
	[DataRow("https://0.0.0.?/*", "https://0.0.0.?/*")]
	[DataRow("https://example.com/*", "https://example.com/*")]
	[DataRow("*", "*")]
	[DataRow("http://0.0.0.1/*", "http://0.0.0.1/*")]
	public void When_Filter_Translated(string filter, string expected)
		=> Assert.AreEqual(expected, new WebViewVirtualHostMap().ToNativeFilter(filter));
}
