#nullable enable

using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_HtmlDocumentScriptInjector
{
	private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

	private static string Inject(string html) => html.Replace("<head>", "<head><script>injected()</script>", StringComparison.Ordinal);

	[TestMethod]
	public void When_Utf8_Document_Is_Injected_Output_Is_Utf8_With_Bom()
	{
		var document = Encoding.UTF8.GetBytes("<html><head><title>café</title></head></html>");

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, "utf-8", Inject, out var result));

		CollectionAssert.AreEqual(Utf8Bom, result.Take(3).ToArray());
		Assert.AreEqual("<html><head><script>injected()</script><title>café</title></head></html>", Encoding.UTF8.GetString(result, 3, result.Length - 3));
	}

	[TestMethod]
	public void When_Document_Has_Utf8_Bom_It_Is_Not_Duplicated()
	{
		var document = Utf8Bom.Concat(Encoding.UTF8.GetBytes("<head>é")).ToArray();

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, "iso-8859-1", Inject, out var result));

		// The byte order mark outranks the transport charset, as in browsers.
		Assert.AreEqual("﻿<head><script>injected()</script>é", Encoding.UTF8.GetString(result));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_Document_Has_Utf16_Bom_It_Is_Decoded(bool bigEndian)
	{
		var encoding = new UnicodeEncoding(bigEndian, byteOrderMark: true);
		var document = encoding.GetPreamble().Concat(encoding.GetBytes("<head>€")).ToArray();

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, null, Inject, out var result));

		Assert.AreEqual("<head><script>injected()</script>€", Encoding.UTF8.GetString(result, 3, result.Length - 3));
	}

	[TestMethod]
	[DataRow("iso-8859-1")]
	[DataRow("windows-1252")]
	[DataRow("\"ISO-8859-1\"")]
	public void When_Transport_Charset_Is_Latin_It_Is_Transcoded(string charset)
	{
		byte[] document = [.. Encoding.ASCII.GetBytes("<head>caf"), 0xE9];

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, charset, Inject, out var result));

		Assert.AreEqual("<head><script>injected()</script>café", Encoding.UTF8.GetString(result, 3, result.Length - 3));
	}

	[TestMethod]
	[DataRow("<meta charset=\"iso-8859-1\">")]
	[DataRow("<META http-equiv=\"Content-Type\" content=\"text/html; charset=ISO-8859-1\">")]
	[DataRow("<meta charset=windows-1252 />")]
	public void When_Meta_Declares_Charset_It_Is_Used(string meta)
	{
		byte[] document = [.. Encoding.ASCII.GetBytes("<html><head>" + meta + "</head><body>"), 0xE9];

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, null, Inject, out var result));

		StringAssert.EndsWith(Encoding.UTF8.GetString(result, 3, result.Length - 3), "<body>é");
	}

	[TestMethod]
	public void When_Meta_Charset_Is_Past_The_Prescan_It_Is_Ignored()
	{
		var padding = new string(' ', 1100);
		var document = Encoding.UTF8.GetBytes("<head>" + padding + "<meta charset=\"iso-8859-1\">é");

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, null, Inject, out var result));

		// Valid UTF-8 without a declaration in range is decoded as UTF-8.
		StringAssert.EndsWith(Encoding.UTF8.GetString(result, 3, result.Length - 3), "é");
	}

	[TestMethod]
	public void When_Undeclared_Bytes_Are_Not_Utf8_They_Are_Read_As_Windows1252()
	{
		byte[] document = [.. Encoding.ASCII.GetBytes("<head>caf"), 0xE9];

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, null, Inject, out var result));

		Assert.AreEqual("<head><script>injected()</script>café", Encoding.UTF8.GetString(result, 3, result.Length - 3));
	}

	[TestMethod]
	public void When_Meta_Declares_Utf16_It_Means_Utf8()
	{
		var document = Encoding.UTF8.GetBytes("<head><meta charset=\"utf-16\">é");

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, null, Inject, out var result));

		StringAssert.EndsWith(Encoding.UTF8.GetString(result, 3, result.Length - 3), "é");
	}

	[TestMethod]
	public void When_Charset_Is_Unknown_Injection_Is_Declined()
	{
		var document = Encoding.ASCII.GetBytes("<head></head>");

		Assert.IsFalse(HtmlDocumentScriptInjector.TryInject(document, "x-no-such-charset", Inject, out var result));
		Assert.AreEqual(0, result.Length);
	}

	[TestMethod]
	public void When_Document_Is_Empty_The_Scripts_Are_Still_Written()
	{
		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject([], null, html => "<script>injected()</script>" + html, out var result));

		Assert.AreEqual("<script>injected()</script>", Encoding.UTF8.GetString(result, 3, result.Length - 3));
	}

	[TestMethod]
	public void When_Bridge_Injects_A_Document_Its_Scripts_Precede_The_Page_Scripts()
	{
		var bridge = new TestBridge();
		var document = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><head><script>page()</script></head></html>");

		Assert.IsTrue(HtmlDocumentScriptInjector.TryInject(document, "utf-8", bridge.InjectIntoHtml, out var result));

		var text = Encoding.UTF8.GetString(result, 3, result.Length - 3);
		var bridgeIndex = text.IndexOf("chrome", StringComparison.Ordinal);
		var pageIndex = text.IndexOf("page()", StringComparison.Ordinal);
		Assert.IsTrue(bridgeIndex > 0 && bridgeIndex < pageIndex, text);
		StringAssert.StartsWith(text, "<!DOCTYPE html><html><head><script>");
	}

	private sealed class TestBridge() : ScriptBridgeWebViewCapabilities("unoWebView.postMessage", WebViewScriptInjectionMode.BeforeParse)
	{
		protected override System.Threading.Tasks.Task<string?> EvaluateScriptAsync(string script, System.Threading.CancellationToken ct)
			=> System.Threading.Tasks.Task.FromResult<string?>(null);
	}
}
