#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_CustomSchemeResourceHandler
{
	private const string Page = "uno-app://0.0.0.1/index.html";

	[TestMethod]
	public async Task When_No_Filter_Matches_And_No_Folder_It_Is_404()
	{
		var raised = 0;
		var handler = new CustomSchemeResourceHandler((_, _) => false, _ => raised++);

		var response = await handler.HandleAsync(Request(Page), CancellationToken.None);

		Assert.AreEqual(0, raised);
		Assert.AreEqual(404, response.StatusCode);
		Assert.AreEqual("Not Found", Encoding.UTF8.GetString(response.Body));
	}

	[TestMethod]
	public async Task When_Response_Is_Set_After_Deferral_It_Is_Awaited()
	{
		var release = new TaskCompletionSource();
		var handler = new CustomSchemeResourceHandler((_, _) => true, args =>
		{
			var deferral = args.GetDeferral();
			_ = Task.Run(async () =>
			{
				await release.Task;
				args.Response = Response(201, "Content-Type: text/html\r\nX-Test: a", "<p>late</p>");
				deferral.Complete();
			});
		});

		var pending = handler.HandleAsync(Request(Page), CancellationToken.None);
		await Task.Delay(50);
		Assert.IsFalse(pending.IsCompleted);

		release.SetResult();
		var response = await pending;

		Assert.AreEqual(201, response.StatusCode);
		Assert.AreEqual("<p>late</p>", Encoding.UTF8.GetString(response.Body));
		Assert.AreEqual("text/html", Header(response, "Content-Type"));
		Assert.AreEqual("a", Header(response, "X-Test"));
		Assert.AreEqual("11", Header(response, "Content-Length"));
	}

	[TestMethod]
	public async Task When_Deferral_Is_Completed_Twice_The_Other_Deferral_Is_Still_Awaited()
	{
		Deferral? second = null;
		var handler = new CustomSchemeResourceHandler((_, _) => true, args =>
		{
			var first = args.GetDeferral();
			second = args.GetDeferral();
			first.Complete();
			first.Complete();
			args.Response = Response(200, "Content-Type: text/plain", "ok");
		});

		var pending = handler.HandleAsync(Request(Page), CancellationToken.None);
		await Task.Delay(50);
		Assert.IsFalse(pending.IsCompleted);

		second!.Complete();
		Assert.AreEqual(200, (await pending).StatusCode);
	}

	[TestMethod]
	public async Task When_Response_Lacks_Content_Type_It_Is_Inferred_And_Length_Is_Fixed()
	{
		var handler = new CustomSchemeResourceHandler((_, _) => true, args =>
			args.Response = Response(200, "Content-Length: 999", "body{}"));

		var response = await handler.HandleAsync(Request("uno-app://0.0.0.1/css/site.css?v=1"), CancellationToken.None);

		Assert.AreEqual("text/css", Header(response, "Content-Type"));
		Assert.AreEqual("6", Header(response, "Content-Length"));
		Assert.AreEqual(1, response.Headers.Count(h => h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)));
	}

	[TestMethod]
	public async Task When_Handler_Sets_No_Response_It_Is_404()
	{
		var raised = 0;
		var handler = new CustomSchemeResourceHandler((_, _) => true, _ => raised++);

		var response = await handler.HandleAsync(Request(Page), CancellationToken.None);

		Assert.AreEqual(1, raised);
		Assert.AreEqual(404, response.StatusCode);
	}

	[TestMethod]
	public async Task When_Handler_Throws_It_Is_500()
	{
		var handler = new CustomSchemeResourceHandler((_, _) => true, _ => throw new InvalidOperationException("boom"));

		var response = await handler.HandleAsync(Request(Page), CancellationToken.None);

		Assert.AreEqual(500, response.StatusCode);
	}

	[TestMethod]
	public async Task When_Status_Code_Is_Invalid_It_Is_500()
	{
		var handler = new CustomSchemeResourceHandler((_, _) => true, args =>
			args.Response = Response(0, "Content-Type: text/plain", "x"));

		var response = await handler.HandleAsync(Request(Page), CancellationToken.None);

		Assert.AreEqual(500, response.StatusCode);
	}

	[TestMethod]
	public async Task When_Cancelled_While_Deferred_It_Throws()
	{
		var handler = new CustomSchemeResourceHandler((_, _) => true, args => args.GetDeferral());
		using var cts = new CancellationTokenSource();

		var pending = handler.HandleAsync(Request(Page), cts.Token);
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
	}

	[TestMethod]
	public async Task When_Request_Is_Raised_Headers_Body_And_Context_Are_Exposed()
	{
		CoreWebView2WebResourceRequestedEventArgs? received = null;
		var handler = new CustomSchemeResourceHandler((_, _) => true, args => received = args);
		var request = new CustomSchemeRequest(
			"uno-app://0.0.0.1/api/save",
			"POST",
			new KeyValuePair<string, string>[] { new("Accept", "*/*"), new("X-Token", "t") },
			Encoding.UTF8.GetBytes("payload"),
			Page);

		await handler.HandleAsync(request, CancellationToken.None);

		Assert.IsNotNull(received);
		Assert.AreEqual("uno-app://0.0.0.1/api/save", received.Request.Uri);
		Assert.AreEqual("POST", received.Request.Method);
		Assert.AreEqual("t", received.Request.Headers.GetHeader("x-token"));
		Assert.AreEqual(CoreWebView2WebResourceContext.Other, received.ResourceContext);
		using var reader = new StreamReader(received.Request.Content.AsStream());
		Assert.AreEqual("payload", reader.ReadToEnd());
	}

	[TestMethod]
	public async Task When_Request_Has_No_Body_Content_Is_Null()
	{
		CoreWebView2WebResourceRequestedEventArgs? received = null;
		var handler = new CustomSchemeResourceHandler((_, _) => true, args => received = args);

		await handler.HandleAsync(Request(Page), CancellationToken.None);

		Assert.IsNull(received!.Request.Content);
	}

	[TestMethod]
	[DataRow(Page, Page, null, CoreWebView2WebResourceContext.Document)]
	[DataRow("uno-app://0.0.0.1/index.html#top", Page, null, CoreWebView2WebResourceContext.Document)]
	[DataRow("uno-app://0.0.0.1/frame", Page, "text/html,application/xhtml+xml", CoreWebView2WebResourceContext.Document)]
	[DataRow("uno-app://0.0.0.1/theme", Page, "text/css,*/*;q=0.1", CoreWebView2WebResourceContext.Stylesheet)]
	[DataRow("uno-app://0.0.0.1/logo", Page, "image/webp,*/*", CoreWebView2WebResourceContext.Image)]
	[DataRow("uno-app://0.0.0.1/app.js", Page, "*/*", CoreWebView2WebResourceContext.Script)]
	[DataRow("uno-app://0.0.0.1/api/data", Page, "*/*", CoreWebView2WebResourceContext.Other)]
	[DataRow("uno-app://0.0.0.1/", null, null, CoreWebView2WebResourceContext.Document)]
	public void When_Classifying_Requests(string uri, string? mainDocument, string? accept, CoreWebView2WebResourceContext expected)
	{
		var headers = accept is null ? Array.Empty<KeyValuePair<string, string>>() : new KeyValuePair<string, string>[] { new("Accept", accept) };

		var context = CustomSchemeResourceHandler.ClassifyResourceContext(new CustomSchemeRequest(uri, "GET", headers, null, mainDocument));

		Assert.AreEqual(expected, context);
	}

	[TestMethod]
	public async Task When_Host_Has_A_Folder_Files_Are_Served_Without_Escaping_It()
	{
		var root = Path.Combine(Path.GetTempPath(), "UnoCustomScheme", Guid.NewGuid().ToString("N"));
		var folder = Path.Combine(root, "site");
		Directory.CreateDirectory(Path.Combine(folder, "sub dir"));
		File.WriteAllText(Path.Combine(folder, "sub dir", "page.html"), "<p>file</p>");
		File.WriteAllText(Path.Combine(root, "secret.txt"), "secret");

		try
		{
			var map = new WebViewVirtualHostMap();
			map.SetHost("appassets.example", folder);
			var handler = new CustomSchemeResourceHandler((_, _) => false, _ => { }) { Map = map };

			var file = await handler.HandleAsync(Request("uno-app://appassets.example/sub%20dir/page.html"), CancellationToken.None);
			var escaped = await handler.HandleAsync(Request("uno-app://appassets.example/%2E%2E/secret.txt"), CancellationToken.None);
			var post = await handler.HandleAsync(Request("uno-app://appassets.example/sub%20dir/page.html") with { Method = "POST" }, CancellationToken.None);

			Assert.AreEqual(200, file.StatusCode);
			Assert.AreEqual("<p>file</p>", Encoding.UTF8.GetString(file.Body));
			Assert.AreEqual("text/html", Header(file, "Content-Type"));
			Assert.AreEqual(404, escaped.StatusCode);
			Assert.AreEqual(404, post.StatusCode);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	public async Task When_Handler_Responds_The_Folder_Is_Not_Used()
	{
		var folder = Path.Combine(Path.GetTempPath(), "UnoCustomScheme", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "index.html"), "from folder");

		try
		{
			var map = new WebViewVirtualHostMap();
			map.SetHost("appassets.example", folder);
			var handler = new CustomSchemeResourceHandler((_, _) => true, args =>
				args.Response = Response(200, "Content-Type: text/plain", "from handler")) { Map = map };

			var response = await handler.HandleAsync(Request("uno-app://appassets.example/index.html"), CancellationToken.None);

			Assert.AreEqual("from handler", Encoding.UTF8.GetString(response.Body));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[TestMethod]
	[DataRow("uno-app://0.0.0.1/_framework/blazor.webview.js", "text/javascript")]
	[DataRow("uno-app://0.0.0.1/app.wasm", "application/wasm")]
	[DataRow("uno-app://0.0.0.1/", "text/html")]
	[DataRow("uno-app://0.0.0.1/data.bin", "application/octet-stream")]
	public void When_Inferring_Content_Type(string uri, string expected)
		=> Assert.AreEqual(expected, CustomSchemeResourceHandler.GetContentType(uri));

	private static CustomSchemeRequest Request(string uri)
		=> new(uri, "GET", Array.Empty<KeyValuePair<string, string>>(), null, uri);

	private static CoreWebView2WebResourceResponse Response(int statusCode, string headers, string body)
		=> new(new ManagedWebResourceResponse(
			new MemoryStream(Encoding.UTF8.GetBytes(body)).AsRandomAccessStream(),
			statusCode,
			"Reason",
			ManagedHttpResponseHeaders.Parse(headers)));

	private static string? Header(WebResourceResponseSnapshot response, string name)
		=> response.Headers.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
}
