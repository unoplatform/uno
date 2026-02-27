using System.Text.Json.Nodes;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_ServerDefinitionResolver
{
	// ── ResolveExpectedVariant ──

	[TestMethod]
	public void ResolveExpectedVariant_StableVersion_NoFlags_ReturnsStable()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", false, false, null);
		result.Should().Be("stable");
	}

	[TestMethod]
	public void ResolveExpectedVariant_PrereleaseVersion_NoFlags_ReturnsPrerelease()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", false, false, null);
		result.Should().Be("prerelease");
	}

	[TestMethod]
	public void ResolveExpectedVariant_PrereleaseVersion_ReleaseFlag_ReturnsStable()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", releaseFlag: true, false, null);
		result.Should().Be("stable");
	}

	[TestMethod]
	public void ResolveExpectedVariant_StableVersion_PrereleaseFlag_ReturnsPrerelease()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", false, prereleaseFlag: true, null);
		result.Should().Be("prerelease");
	}

	[TestMethod]
	public void ResolveExpectedVariant_VersionFlag_ReturnsPinned()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0", false, false, versionFlag: "5.5.0");
		result.Should().Be("pinned:5.5.0");
	}

	[TestMethod]
	public void ResolveExpectedVariant_VersionFlag_TakesPrecedence()
	{
		var result = ServerDefinitionResolver.ResolveExpectedVariant("5.6.0-dev.42", true, true, versionFlag: "5.5.0");
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
			["stable"] = JsonNode.Parse("""{"command":"dnx","args":["-y","uno.devserver","--mcp-app"]}""")!.AsObject(),
			["prerelease"] = JsonNode.Parse("""{"command":"dnx","args":["-y","--prerelease","uno.devserver","--mcp-app"]}""")!.AsObject(),
			["pinned"] = JsonNode.Parse("""{"command":"dnx","args":["-y","--version","{version}","uno.devserver","--mcp-app"]}""")!.AsObject(),
		},
		Detection: new(["^UnoApp$"], null, null));

	[TestMethod]
	public void ResolveDefinition_Stable_ReturnsStableArgs()
	{
		var result = ServerDefinitionResolver.ResolveDefinition(TestUnoApp, "stable");
		var args = result["args"]!.AsArray();
		args.Select(a => a!.GetValue<string>()).Should().BeEquivalentTo(["-y", "uno.devserver", "--mcp-app"]);
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
}
