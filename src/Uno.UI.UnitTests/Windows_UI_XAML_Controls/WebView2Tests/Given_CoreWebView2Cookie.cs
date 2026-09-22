#nullable enable

using System;
using System.Net;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_CoreWebView2Cookie
{
	[TestMethod]
	[DataRow("login.example.com", false)]
	[DataRow(".login.example.com", true)]
	public void When_The_Header_Is_Parsed_Only_Domain_Cookies_Reach_Subdomains(string domain, bool includesSubdomains)
	{
		var origin = new Uri("https://login.example.com/scope/page");
		var subdomain = new Uri("https://child.login.example.com/scope/page");
		var cookie = new CoreWebView2Cookie("uno_scope", "initial", domain, "/scope");
		var store = new CookieContainer();

		store.SetCookies(origin, cookie.ToSetCookieHeader());
		Assert.AreEqual("initial", store.GetCookies(origin)["uno_scope"]?.Value);
		Assert.AreEqual(includesSubdomains, store.GetCookies(subdomain)["uno_scope"] is not null);
		Assert.IsNull(store.GetCookies(new Uri("https://otherlogin.example.com/scope/page"))["uno_scope"]);

		cookie.Value = "updated";
		store.SetCookies(origin, cookie.ToSetCookieHeader());
		Assert.AreEqual("updated", store.GetCookies(origin)["uno_scope"]?.Value);
		Assert.AreEqual(includesSubdomains, store.GetCookies(subdomain)["uno_scope"] is not null);

		var expired = new CoreWebView2Cookie(cookie.Name, string.Empty, cookie.Domain, cookie.Path) { Expires = 0 };
		store.SetCookies(origin, expired.ToSetCookieHeader());
		Assert.IsNull(store.GetCookies(origin)["uno_scope"]);
		Assert.IsNull(store.GetCookies(subdomain)["uno_scope"]);
		Assert.AreEqual(domain, cookie.Domain);
	}

	[TestMethod]
	[DataRow("value; Secure")]
	[DataRow("value\0")]
	[DataRow("value\t")]
	[DataRow("value\r\n")]
	[DataRow("value\u001f")]
	[DataRow("value\u007f")]
	public void When_A_Value_Contains_A_Delimiter_Or_Control_It_Is_Rejected(string value)
	{
		var owner = new WebView2();
		var core = new CoreWebView2((IWebView)owner);
		try
		{
			var manager = core.CookieManager;
			Assert.ThrowsExactly<ArgumentException>(() => manager.CreateCookie("uno", value, "example.com", "/"));

			var cookie = manager.CreateCookie("uno", "original", "example.com", "/");
			cookie.Value = value;
			Assert.ThrowsExactly<ArgumentException>(() => manager.AddOrUpdateCookie(cookie));
		}
		finally
		{
			core.Close();
			owner.Close();
		}
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("has spaces")]
	[DataRow("\"quoted\",\\value")]
	[DataRow("caf\u00e9")]
	public void When_A_Value_Is_Allowed_By_Chromium_It_Is_Not_Overvalidated(string value)
	{
		var owner = new WebView2();
		var core = new CoreWebView2((IWebView)owner);
		try
		{
			var manager = core.CookieManager;
			var cookie = manager.CreateCookie("uno", value, "example.com", "/");
			Assert.AreEqual(value, cookie.Value);
			Assert.ThrowsExactly<NotSupportedException>(() => manager.AddOrUpdateCookie(cookie));
		}
		finally
		{
			core.Close();
			owner.Close();
		}
	}

	[TestMethod]
	[DataRow(CoreWebView2CookieSameSiteKind.None, "None")]
	[DataRow(CoreWebView2CookieSameSiteKind.Lax, "Lax")]
	[DataRow(CoreWebView2CookieSameSiteKind.Strict, "Strict")]
	public void When_A_Cookie_Is_Serialized_All_Attributes_Are_Included(CoreWebView2CookieSameSiteKind sameSite, string expected)
	{
		var cookie = new CoreWebView2Cookie("uno", "value", ".example.com", "/scope")
		{
			IsSecure = true,
			IsHttpOnly = true,
			SameSite = sameSite,
			Expires = 0,
		};

		Assert.AreEqual(
			$"uno=value; Domain=.example.com; Path=/scope; Secure; HttpOnly; SameSite={expected}; Expires=Thu, 01 Jan 1970 00:00:00 GMT",
			cookie.ToSetCookieHeader());
	}

	[TestMethod]
	public void When_A_Cookie_Is_A_Session_Cookie_No_Expiry_Is_Added()
	{
		var cookie = new CoreWebView2Cookie("uno", "value", "example.com", "/");
		Assert.AreEqual("uno=value; Path=/; SameSite=Lax", cookie.ToSetCookieHeader());
	}

	[TestMethod]
	[DataRow("login.example.com", "https://login.example.com/scope", true, true)]
	[DataRow("login.example.com", "https://LOGIN.EXAMPLE.COM/scope/page", true, true)]
	[DataRow("login.example.com", "https://child.login.example.com/scope/page", true, false)]
	[DataRow(".login.example.com", "https://login.example.com/scope/page", true, true)]
	[DataRow(".login.example.com", "https://child.login.example.com/scope/page", true, true)]
	[DataRow(".login.example.com", "https://otherlogin.example.com/scope/page", true, false)]
	[DataRow(".login.example.com", "https://example.com/scope/page", true, false)]
	[DataRow(".login.example.com", "https://login.example.com/scope-other", true, false)]
	[DataRow(".login.example.com", "http://child.login.example.com/scope/page", true, false)]
	[DataRow(".login.example.com", "http://child.login.example.com/scope/page", false, true)]
	public void When_Matching_A_Uri_Host_Only_Scope_Is_Not_Widened(string domain, string uri, bool secure, bool expected) =>
		Assert.AreEqual(expected, CoreWebView2Cookie.MatchesUri(domain, "/scope", secure, new Uri(uri)));

	[TestMethod]
	[DataRow("__Host-uno")]
	[DataRow("__Secure-uno")]
	public void When_Deleting_A_Secure_Prefixed_Cookie_Secure_Is_Preserved(string name)
	{
		var cookie = new CoreWebView2Cookie(name, "value", "login.example.com", "/") { IsSecure = true };
		Assert.AreEqual($"{name}=; Path=/; Max-Age=0; Secure", cookie.ToSetCookieDeletionHeader());
	}

	[TestMethod]
	[DataRow("login.example.com", "uno_scope=; Path=/scope; Max-Age=0")]
	[DataRow(".login.example.com", "uno_scope=; Domain=.login.example.com; Path=/scope; Max-Age=0")]
	public void When_Deleting_An_Original_Cookie_The_Header_Preserves_Its_Scope(string domain, string expectedHeader)
	{
		var origin = new Uri("https://login.example.com/scope/page");
		var original = new CoreWebView2Cookie("uno_scope", "original", domain, "/scope");
		var store = new CookieContainer();
		store.SetCookies(origin, original.ToSetCookieHeader());
		Assert.AreEqual(1, store.GetCookies(origin).Count);
		Assert.AreEqual(expectedHeader, original.ToSetCookieDeletionHeader());

		store.SetCookies(origin, original.ToSetCookieDeletionHeader());

		Assert.AreEqual(0, store.GetCookies(origin).Count);
		Assert.IsNull(store.GetCookies(new Uri("https://child.login.example.com/scope/page"))["uno_scope"]);
		Assert.AreEqual(domain, original.Domain);
	}
}
