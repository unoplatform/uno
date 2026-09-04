#nullable enable

using System;
using System.Reflection;
using Microsoft.Windows.AppLifecycle;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

/// <remarks>
/// <c>Uno.Helpers.ProtocolActivation</c> lives in a <c>.wasm.cs</c> file, so it only exists in the
/// WebAssembly flavour of <c>Uno.WinRT</c> — never in the reference assembly this test project compiles
/// against. The helper below therefore reaches it through the assembly loaded at runtime.
/// </remarks>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Wasm)]
public class Given_ProtocolActivation
{
	private const string QueryKey = "unoprotocolactivation";

	[TestMethod]
	public void When_Query_Contains_Activation_Uri()
	{
		const string ActivationUri = "web+unotest://open/document";

		var result = TryParseActivationUri(
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

		var result = TryParseActivationUri(
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

		var result = TryParseActivationUri(Query, out var uri, out var remainingArguments);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.AreEqual(Query, remainingArguments);
	}

	private static bool TryParseActivationUri(string queryArguments, out Uri? uri, out string? remainingArguments)
	{
		var type = typeof(AppInstance).Assembly.GetType("Uno.Helpers.ProtocolActivation", throwOnError: false);
		Assert.IsNotNull(type, "Uno.Helpers.ProtocolActivation is missing from the Uno.WinRT assembly loaded for this target.");

		var method = type!.GetMethod(
			"TryParseActivationUri",
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
			binder: null,
			[typeof(string), typeof(Uri).MakeByRefType(), typeof(string).MakeByRefType()],
			modifiers: null);
		Assert.IsNotNull(method, "ProtocolActivation.TryParseActivationUri(string, out Uri, out string) is missing.");

		var parameters = new object?[] { queryArguments, null, null };
		var result = (bool)method!.Invoke(null, parameters)!;

		uri = (Uri?)parameters[1];
		remainingArguments = (string?)parameters[2];

		return result;
	}
}
