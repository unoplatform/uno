#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.WebView2Tests;

/// <remarks>
/// The unit test host registers no <see cref="ICoreWebView2EnvironmentStaticsExtension"/>, so these cover the
/// platforms that cannot probe for an installed browser.
/// </remarks>
[UITestClass]
public class Given_CoreWebView2Environment
{
	[TestMethod]
	public void When_Parsing_Headers()
	{
		var headers = ManagedHttpResponseHeaders.Parse(
			"Content-Type: text/html\r\n" +
			"  X-Padded  :   value  \r\n" +
			"\r\n" +
			"no separator\r\n" +
			": no name\r\n" +
			"Location: https://example.com:8080/path\r\n" +
			"X-Empty:\n" +
			"Set-Cookie: a=1\r\n" +
			"set-cookie: b=2");

		CollectionAssert.AreEqual(
			new[]
			{
				new KeyValuePair<string, string>("Content-Type", "text/html"),
				new KeyValuePair<string, string>("X-Padded", "value"),
				new KeyValuePair<string, string>("Location", "https://example.com:8080/path"),
				new KeyValuePair<string, string>("X-Empty", ""),
				new KeyValuePair<string, string>("Set-Cookie", "a=1"),
				new KeyValuePair<string, string>("set-cookie", "b=2"),
			},
			headers.ToArray());
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("\r\n\r\n")]
	public void When_Parsing_Empty_Headers(string? headers)
		=> Assert.IsFalse(ManagedHttpResponseHeaders.Parse(headers).Any());

	[TestMethod]
	public void When_Headers_Are_Queried_Case_Insensitively()
	{
		var headers = ManagedHttpResponseHeaders.Parse("Content-Type: text/plain\r\nSet-Cookie: a=1\r\nSet-Cookie: b=2");

		Assert.IsTrue(headers.Contains("content-type"));
		Assert.IsTrue(headers.Contains("SET-COOKIE"));
		Assert.IsFalse(headers.Contains("X-Missing"));
		Assert.AreEqual("text/plain", headers.GetHeader("CONTENT-TYPE"));
		Assert.AreEqual("a=1", headers.GetHeader("set-cookie"));
		Assert.ThrowsExactly<ArgumentException>(() => headers.GetHeader("X-Missing"));
	}

	[TestMethod]
	public void When_Header_Appended()
	{
		var headers = ManagedHttpResponseHeaders.Parse("A: 1");

		headers.AppendHeader("B", "2");
		headers.AppendHeader("a", "3");

		CollectionAssert.AreEqual(new[] { "A", "B", "a" }, headers.Select(h => h.Key).ToArray());
		CollectionAssert.AreEqual(new[] { "1", "2", "3" }, headers.Select(h => h.Value).ToArray());
	}

	[TestMethod]
	public async Task When_CreateWebResourceResponse()
	{
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();
		var content = CreateStream("hello");

		var response = environment.CreateWebResourceResponse(content, 404, "Not Found", "Content-Type: text/plain\r\nX-Value: 1\r\nx-value: 2");

		Assert.AreSame(content, response.Content);
		Assert.AreEqual(404, response.StatusCode);
		Assert.AreEqual("Not Found", response.ReasonPhrase);
		Assert.IsInstanceOfType<ManagedWebResourceResponse>(response.NativeResponseInterface);

		Assert.IsTrue(response.Headers.Contains("CONTENT-TYPE"));
		Assert.AreEqual("text/plain", response.Headers.GetHeader("content-type"));
		Assert.AreEqual(3, response.Headers.Count());

		response.StatusCode = 200;
		response.ReasonPhrase = "OK";
		response.Headers.AppendHeader("Cache-Control", "no-cache");

		Assert.AreEqual(200, response.StatusCode);
		Assert.AreEqual("OK", response.ReasonPhrase);
		Assert.AreEqual("no-cache", response.Headers.GetHeader("cache-control"));
	}

	[TestMethod]
	public async Task When_CreateWebResourceResponse_Without_Content_Or_Headers()
	{
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();

		var response = environment.CreateWebResourceResponse(null, 204, null, null);

		Assert.IsNull(response.Content);
		Assert.AreEqual(204, response.StatusCode);
		Assert.AreEqual("", response.ReasonPhrase);
		Assert.IsFalse(response.Headers.Any());
	}

	[TestMethod]
	public async Task When_Response_Has_Multiple_Values()
	{
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();
		var response = environment.CreateWebResourceResponse(null, 200, "OK", "Set-Cookie: a=1\r\nContent-Type: text/html\r\nset-cookie: b=2");

		var iterator = response.Headers.GetHeaders("SET-COOKIE");
		var values = new List<string>();
		while (iterator.HasCurrent)
		{
			values.Add(iterator.Current.Value);
			iterator.MoveNext();
		}

		CollectionAssert.AreEqual(new[] { "a=1", "b=2" }, values);
		Assert.IsFalse(iterator.MoveNext());

		var items = new KeyValuePair<string, string>[4];
		Assert.AreEqual(2u, response.Headers.GetHeaders("set-cookie").GetMany(items));
		Assert.AreEqual("b=2", items[1].Value);

		Assert.IsFalse(response.Headers.GetHeaders("X-Missing").HasCurrent);
	}

	[TestMethod]
	public async Task When_CreateAsync()
	{
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();

		Assert.IsNotNull(environment);
		Assert.AreEqual("", environment.UserDataFolder);
		Assert.AreEqual("", environment.BrowserVersionString);
		Assert.IsNull(environment.BrowserExecutableFolder);
		Assert.IsNull(environment.Options);
	}

	[TestMethod]
	public async Task When_CreateWithOptionsAsync_Records_Inputs()
	{
		var options = new CoreWebView2EnvironmentOptions { Language = "fr-FR" };

		var environment = await CoreWebView2Environment.CreateWithOptionsAsync("browser", "user-data", options).AsTask();

		Assert.AreEqual("user-data", environment.UserDataFolder);
		Assert.AreEqual("browser", environment.BrowserExecutableFolder);
		Assert.AreSame(options, environment.Options);
		Assert.AreEqual("", environment.BrowserVersionString);
	}

	[TestMethod]
	public async Task When_CreateWithOptionsAsync_Without_Browser_Folder()
	{
		var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, "folder", new CoreWebView2EnvironmentOptions()).AsTask();

		Assert.AreEqual("folder", environment.UserDataFolder);
	}

	[TestMethod]
	public async Task When_Detached_Environment_Needs_WebView()
	{
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();

		Assert.ThrowsExactly<NotImplementedException>(() => _ = environment.FailureReportFolderPath);
		Assert.ThrowsExactly<NotImplementedException>(() => environment.GetProcessInfos());
	}

	[TestMethod]
	public void When_Statics_Unavailable()
	{
		Assert.ThrowsExactly<NotImplementedException>(() => CoreWebView2Environment.GetAvailableBrowserVersionString(null, new CoreWebView2EnvironmentOptions()));
	}

	[TestMethod]
	public void When_Options_Default()
	{
		var options = new CoreWebView2EnvironmentOptions();

		Assert.AreEqual("", options.AdditionalBrowserArguments);
		Assert.IsFalse(options.AllowSingleSignOnUsingOSPrimaryAccount);
		Assert.IsFalse(options.AreBrowserExtensionsEnabled);
		Assert.AreEqual(CoreWebView2ChannelSearchKind.MostStable, options.ChannelSearchKind);
		Assert.IsNotNull(options.CustomSchemeRegistrations);
		Assert.AreEqual(0, options.CustomSchemeRegistrations.Count);
		Assert.IsTrue(options.EnableTrackingPrevention);
		Assert.IsFalse(options.ExclusiveUserDataFolderAccess);
		Assert.IsFalse(options.IsCustomCrashReportingEnabled);
		Assert.AreEqual("", options.Language);
		Assert.AreEqual(
			CoreWebView2ReleaseChannels.Stable | CoreWebView2ReleaseChannels.Beta | CoreWebView2ReleaseChannels.Dev | CoreWebView2ReleaseChannels.Canary,
			options.ReleaseChannels);
		Assert.AreEqual(CoreWebView2ScrollbarStyle.Default, options.ScrollBarStyle);
		Assert.AreEqual("", options.TargetCompatibleBrowserVersion);
	}

	[TestMethod]
	public void When_Options_Round_Trip()
	{
		var schemes = new List<CoreWebView2CustomSchemeRegistration>();
		var options = new CoreWebView2EnvironmentOptions
		{
			AdditionalBrowserArguments = "--flag",
			AllowSingleSignOnUsingOSPrimaryAccount = true,
			AreBrowserExtensionsEnabled = true,
			ChannelSearchKind = CoreWebView2ChannelSearchKind.LeastStable,
			CustomSchemeRegistrations = schemes,
			EnableTrackingPrevention = false,
			ExclusiveUserDataFolderAccess = true,
			IsCustomCrashReportingEnabled = true,
			Language = "de-DE",
			ReleaseChannels = CoreWebView2ReleaseChannels.Beta,
			ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay,
			TargetCompatibleBrowserVersion = "120.0.0.0",
		};

		Assert.AreEqual("--flag", options.AdditionalBrowserArguments);
		Assert.IsTrue(options.AllowSingleSignOnUsingOSPrimaryAccount);
		Assert.IsTrue(options.AreBrowserExtensionsEnabled);
		Assert.AreEqual(CoreWebView2ChannelSearchKind.LeastStable, options.ChannelSearchKind);
		Assert.AreSame(schemes, options.CustomSchemeRegistrations);
		Assert.IsFalse(options.EnableTrackingPrevention);
		Assert.IsTrue(options.ExclusiveUserDataFolderAccess);
		Assert.IsTrue(options.IsCustomCrashReportingEnabled);
		Assert.AreEqual("de-DE", options.Language);
		Assert.AreEqual(CoreWebView2ReleaseChannels.Beta, options.ReleaseChannels);
		Assert.AreEqual(CoreWebView2ScrollbarStyle.FluentOverlay, options.ScrollBarStyle);
		Assert.AreEqual("120.0.0.0", options.TargetCompatibleBrowserVersion);

		options.Language = null!;
		Assert.AreEqual("", options.Language);
	}

	[TestMethod]
	public void When_Settings_Default_And_Round_Trip()
	{
		var settings = new CoreWebView2Settings();

		Assert.IsTrue(settings.AreDevToolsEnabled);
		Assert.IsTrue(settings.AreDefaultContextMenusEnabled);
		Assert.IsTrue(settings.IsStatusBarEnabled);

		settings.AreDevToolsEnabled = false;
		settings.AreDefaultContextMenusEnabled = false;
		settings.IsStatusBarEnabled = false;

		Assert.IsFalse(settings.AreDevToolsEnabled);
		Assert.IsFalse(settings.AreDefaultContextMenusEnabled);
		Assert.IsFalse(settings.IsStatusBarEnabled);
	}

	[TestMethod]
	public void When_EnsureCoreWebView2Async_With_Null_Environment()
	{
		var webView = new WebView2();

		Assert.ThrowsExactly<ArgumentNullException>(() => webView.EnsureCoreWebView2Async(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => webView.EnsureCoreWebView2Async(null!, null));
	}

	[TestMethod]
	public async Task When_EnsureCoreWebView2Async_Adopts_Environment()
	{
		var webView = new WebView2();
		var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, "folder", null).AsTask();
		var other = await CoreWebView2Environment.CreateAsync().AsTask();

		// The native WebView never initializes in the unit test host, so the returned action is not awaited.
		_ = webView.EnsureCoreWebView2Async(environment);
		_ = webView.EnsureCoreWebView2Async(other, null);

		Assert.AreSame(environment, webView.CoreWebView2.Environment);
		Assert.AreEqual("folder", webView.CoreWebView2.Environment.UserDataFolder);
	}

	private static IRandomAccessStream CreateStream(string text)
		=> new MemoryStream(Encoding.UTF8.GetBytes(text)).AsRandomAccessStream();
}
