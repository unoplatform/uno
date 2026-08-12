using Uno.UI.RemoteControl.HotReload;

namespace Uno.UI.RemoteControl.DevServer.Tests;

/// <summary>
/// Tests for the framework-version composition of the client's runtime target-framework
/// descriptor (<see cref="ClientHotReloadProcessor.ResolveFrameworkVersion"/>) — the non-<c>#if</c>
/// part of <c>GetRuntimeTargetFramework</c>, including the malformed/missing fallback that the
/// rest of the suite (server-side parse/match/filter) does not cover.
/// </summary>
[TestClass]
public class Given_GetRuntimeTargetFramework
{
	[TestMethod]
	[DataRow(".NETCoreApp,Version=v10.0", 10, 0)]
	[DataRow(".NETCoreApp,Version=v9.0", 9, 0)]
	[DataRow(".NETCoreApp,Version=v8.0", 8, 0)]
	public void When_ValidNetCoreAppMoniker_Then_VersionIsParsed(string frameworkName, int major, int minor)
	{
		var version = ClientHotReloadProcessor.ResolveFrameworkVersion(frameworkName);

		version.Major.Should().Be(major);
		version.Minor.Should().Be(minor);
	}

	[TestMethod]
	[DataRow((string?)null)]              // no TargetFrameworkAttribute
	[DataRow("")]                          // empty FrameworkName
	[DataRow("not a framework name")]      // unparsable → ArgumentException
	[DataRow(".NETCoreApp")]               // identifier only, no version → ArgumentException
	[DataRow(".NETFramework,Version=v4.8")] // valid moniker, but not .NETCoreApp
	public void When_MissingMalformedOrNonNetCoreApp_Then_FallsBackToRuntimeVersion(string? frameworkName)
	{
		var version = ClientHotReloadProcessor.ResolveFrameworkVersion(frameworkName);

		version.Should().Be(ClientHotReloadProcessor.FallbackVersion);
	}
}
