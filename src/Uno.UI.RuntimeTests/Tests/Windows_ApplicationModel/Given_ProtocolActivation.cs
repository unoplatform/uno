#nullable enable

using System;
using Uno.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

/// <summary>
/// Covers how a protocol activation is carried across the browser navigation that
/// <c>navigator.registerProtocolHandler</c> performs. The parsing itself is platform-neutral, so it
/// runs on every target rather than only on WebAssembly.
/// </summary>
[TestClass]
public class Given_ProtocolActivation
{
	private const string QueryKey = "unoprotocolactivation";

	[TestMethod]
	public void When_Query_Contains_Activation_Uri()
	{
		const string ActivationUri = "web+unotest://open/document";

		var result = ProtocolActivation.TryParseActivationUri(
			$"foo=bar&{QueryKey}={Uri.EscapeDataString(ActivationUri)}&baz=1",
			out var uri,
			out var remainingArguments);

		Assert.IsTrue(result);
		Assert.IsNotNull(uri);
		Assert.AreEqual(ActivationUri, uri!.OriginalString);

		Assert.IsNotNull(remainingArguments);
		Assert.IsFalse(remainingArguments!.Contains(QueryKey, StringComparison.Ordinal));
		Assert.AreEqual("foo=bar&baz=1", remainingArguments);
	}

	[TestMethod]
	public void When_Activation_Uri_Carries_Escaped_Characters()
	{
		// Regression guard: the value is already decoded by the query parser, so unescaping it a second
		// time would turn %2F into '/' and %25 into '%' inside the app's own activation URI.
		const string ActivationUri = "web+unotest://open?path=a%2Fb&pct=100%25";

		var result = ProtocolActivation.TryParseActivationUri(
			$"{QueryKey}={Uri.EscapeDataString(ActivationUri)}",
			out var uri,
			out _);

		Assert.IsTrue(result);
		Assert.IsNotNull(uri);
		Assert.AreEqual(ActivationUri, uri!.OriginalString);
		Assert.AreEqual("?path=a%2Fb&pct=100%25", uri.Query);
	}

	[TestMethod]
	public void When_Query_Has_No_Activation_Uri()
	{
		const string Query = "foo=bar&baz=1";

		var result = ProtocolActivation.TryParseActivationUri(Query, out var uri, out var remainingArguments);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.AreEqual(Query, remainingArguments);
	}
}
