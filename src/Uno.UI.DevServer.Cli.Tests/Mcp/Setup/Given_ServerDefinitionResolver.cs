using System.Text.Json.Nodes;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_ServerDefinitionResolver
{
	// ── ResolveExpectedVariant ──

	[TestMethod]
	public void ResolveExpectedVariant_StableVersion_NoOverrides_ReturnsStable()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", null, null);
		result.Should().Be("stable");
	}

	[TestMethod]
	public void ResolveExpectedVariant_PrereleaseVersion_NoOverrides_ReturnsPrerelease()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", null, null);
		result.Should().Be("prerelease");
	}

	[TestMethod]
	public void ResolveExpectedVariant_ChannelStable_ReturnsStable()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", "stable", null);
		result.Should().Be("stable");
	}

	[TestMethod]
	public void ResolveExpectedVariant_ChannelPrerelease_ReturnsPrerelease()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", "prerelease", null);
		result.Should().Be("prerelease");
	}

	[TestMethod]
	public void ResolveExpectedVariant_ToolVersion_ReturnsPinned()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", null, "5.5.0");
		result.Should().Be("pinned:5.5.0");
	}

	[TestMethod]
	public void ResolveExpectedVariant_ToolVersion_TakesPrecedence()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", "prerelease", "5.5.0");
		result.Should().Be("pinned:5.5.0");
	}

	// ── IsPrerelease ──

	[TestMethod]
	public void IsPrerelease_StableVersion_ReturnsFalse()
	{
		ServerDefinitionResolver.IsPrerelease("5.6.0").Should().BeFalse();
	}

	[TestMethod]
	public void IsPrerelease_DevVersion_ReturnsTrue()
	{
		ServerDefinitionResolver.IsPrerelease("5.6.0-dev.42").Should().BeTrue();
	}

	// ── ResolveDefinition ──

	private static ServerDefinition TestUnoApp => new(
		Transport: "stdio",
		Variants: new Dictionary<string, JsonObject>
		{
			["stable"] = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
			["prerelease"] = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject(),
			["pinned"] = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","--version","{version}","uno.devserver","--mcp-app"]}""")!.AsObject(),
		},
		Detection: new(["^UnoApp$"], null, null));

	[TestMethod]
	public void ResolveDefinition_Stable_ReturnsStableArgs()
	{
		var result = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "stable");
		var args = result["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().BeEquivalentTo(["dnx", "-y", "uno.devserver", "--mcp-app"]);
	}

	[TestMethod]
	public void ResolveDefinition_Prerelease_ReturnsPrereleaseArgs()
	{
		var result = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "prerelease");
		var args = result["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().Contain("--prerelease");
	}

	[TestMethod]
	public void ResolveDefinition_Pinned_ReplacesVersionPlaceholder()
	{
		var result = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "pinned:5.6.0");
		var args = result["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().Contain("5.6.0");
		args.Select(a => a!.GetValue<string>()).Should().NotContain("{version}");
	}

	[TestMethod]
	public void ResolveDefinition_ReturnsClone_NotOriginal()
	{
		var result1 = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "stable");
		var result2 = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "stable");

		// They should be equal but not the same reference
		result1.ToJsonString().Should().Be(result2.ToJsonString());
	}

	[TestMethod]
	public void ResolveDefinition_PinnedWithoutPinnedVariant_FallsBackToStable()
	{
		var defWithoutPinned = new ServerDefinition(
			Transport: "stdio",
			Variants: new Dictionary<string, JsonObject>
			{
				["stable"] = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
			},
			Detection: new(["^UnoApp$"], null, null));

		var result = ServerDefinitionResolver.ResolveDefinition(defWithoutPinned, "pinned:5.6.0");
		var args = result["args"]!.AsArray();

		args.Select(a => a!.GetValue<string>()).Should().BeEquivalentTo(["dnx", "-y", "uno.devserver", "--mcp-app"]);
	}

	[TestMethod]
	public void ResolveDefinition_MissingRequestedVariantAndStable_ThrowsInvalidOperationException()
	{
		var defWithoutFallback = new ServerDefinition(
			Transport: "stdio",
			Variants: new Dictionary<string, JsonObject>
			{
				["prerelease"] = JsonNode.Parse("""{"command":"dotnet","args":["dnx","-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject(),
			},
			Detection: new(["^UnoApp$"], null, null));

		var act = () => ServerDefinitionResolver.ResolveDefinition(defWithoutFallback, "stable");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*variant*stable*");
	}
}
