using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Uno.UI.Tests.Windows_Web_Http;

[TestClass]
public class Given_HttpRequestHeaderCollection
{
	private static HttpRequestHeaderCollection GetNewHeaders(Uri uri)
		=> new HttpRequestMessage(new HttpMethod("GET"), uri).Headers;

	[TestMethod]
	public void TestRefererAbsolute()
	{
		var uri = new Uri("https://google.com");
		var headers = GetNewHeaders(uri);
		Assert.IsEmpty(headers);
		var refererUri = new Uri("https://google2.com");
		Assert.IsTrue(refererUri.IsAbsoluteUri);
		headers.Referer = refererUri;
		Assert.IsFalse(ReferenceEquals(refererUri, headers.Referer));
		Assert.IsFalse(ReferenceEquals(headers.Referer, headers.Referer));
		Assert.AreEqual("https://google2.com/", refererUri.ToString());
		Assert.AreEqual("https://google2.com", refererUri.OriginalString);
		Assert.AreEqual("https://google2.com/", headers.Referer.ToString());
		Assert.AreEqual("https://google2.com/", headers.Referer.OriginalString); // We have extra trailing "/" that doesn't show up in Windows.
		Assert.IsTrue(headers.TryGetValue("Referer", out var ref1));
		Assert.AreEqual("https://google2.com/", ref1);
		Assert.IsTrue(headers.TryGetValue("referer", out var ref2));
		Assert.AreEqual("https://google2.com/", ref2);
		Assert.AreEqual("https://google2.com/", headers["Referer"]);

		headers = GetNewHeaders(uri);
		Assert.IsEmpty(headers);
		headers["Referer"] = "https://google2.com";
		Assert.IsFalse(ReferenceEquals(refererUri, headers.Referer));
		Assert.IsFalse(ReferenceEquals(headers.Referer, headers.Referer));
		Assert.AreEqual("https://google2.com/", refererUri.ToString());
		Assert.AreEqual("https://google2.com", refererUri.OriginalString);
		Assert.AreEqual("https://google2.com/", headers.Referer.ToString());
		Assert.AreEqual("https://google2.com/", headers.Referer.OriginalString); // We have extra trailing "/" that doesn't show up in Windows.
		Assert.IsTrue(headers.TryGetValue("Referer", out var ref3));
		Assert.AreEqual("https://google2.com/", ref3);
		Assert.IsTrue(headers.TryGetValue("referer", out var ref4));
		Assert.AreEqual("https://google2.com/", ref4);
		Assert.AreEqual("https://google2.com/", headers["Referer"]);
	}

	[TestMethod]
	public void TestRefererRelative()
	{
		var uri = new Uri("https://google.com");
		var headers = GetNewHeaders(uri);
		Assert.IsEmpty(headers);
		var refererUri = new Uri("Relative With Spaces", UriKind.Relative);
		Assert.IsFalse(refererUri.IsAbsoluteUri);
		headers.Referer = refererUri; // This crashes in Windows.
		Assert.IsFalse(ReferenceEquals(refererUri, headers.Referer));
		Assert.IsFalse(ReferenceEquals(headers.Referer, headers.Referer));
		Assert.AreEqual("https://google.com/Relative With Spaces", headers.Referer.ToString());
		Assert.AreEqual("https://google.com/Relative%20With%20Spaces", headers.Referer.OriginalString);
		Assert.IsTrue(headers.TryGetValue("Referer", out var ref1));
		Assert.AreEqual("https://google.com/Relative%20With%20Spaces", ref1);
		Assert.IsTrue(headers.TryGetValue("referer", out var ref2));
		Assert.AreEqual("https://google.com/Relative%20With%20Spaces", ref2);

		headers = GetNewHeaders(uri);
		Assert.IsEmpty(headers);
		headers["Referer"] = "Rleative With Spaces";
		Assert.IsFalse(ReferenceEquals(headers.Referer, headers.Referer));
		Assert.AreEqual("https://google.com/Rleative With Spaces", headers.Referer.ToString());
		Assert.AreEqual("https://google.com/Rleative%20With%20Spaces", headers.Referer.OriginalString);
		Assert.IsTrue(headers.TryGetValue("Referer", out var ref3));
		Assert.AreEqual("https://google.com/Rleative%20With%20Spaces", ref3);
		Assert.IsTrue(headers.TryGetValue("referer", out var ref4));
		Assert.AreEqual("https://google.com/Rleative%20With%20Spaces", ref4);
		Assert.AreEqual("https://google.com/Rleative%20With%20Spaces", headers["Referer"]);
	}
}
