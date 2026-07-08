using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class CsprojUserGeneratorTests
{
	private string _tempRoot = string.Empty;

	[TestInitialize]
	public void Setup()
	{
		_tempRoot = Path.Combine(Path.GetTempPath(), "uno-csprojuser-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempRoot);
	}

	[TestCleanup]
	public void Cleanup()
	{
		try
		{
			if (Directory.Exists(_tempRoot))
			{
				Directory.Delete(_tempRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup; a leaked temp dir must never fail a test.
		}
	}

	private string CreateProjectFile(string name = "App")
	{
		var projectDir = Path.Combine(_tempRoot, name);
		Directory.CreateDirectory(projectDir);
		var csproj = Path.Combine(projectDir, name + ".csproj");
		File.WriteAllText(csproj, "<Project Sdk=\"Uno.Sdk\" />");
		return csproj;
	}

	private static void WriteUserFileWithPort(string csproj, string rawPortValue)
		=> File.WriteAllText(csproj + ".user",
			$"""
			<?xml version="1.0" encoding="utf-8"?>
			<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
			  <PropertyGroup>
			    <UnoRemoteControlPort>{rawPortValue}</UnoRemoteControlPort>
			  </PropertyGroup>
			</Project>
			""");

	[TestMethod]
	public void SetCsprojUserPortForProject_WhenNoUserFile_CreatesItWithPort()
	{
		var csproj = CreateProjectFile();

		CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 62483);

		var userFile = csproj + ".user";
		File.Exists(userFile).Should().BeTrue();
		// The DevServer marks tooling-assigned ports with a trailing '#'.
		File.ReadAllText(userFile).Should().Contain("<UnoRemoteControlPort>62483#</UnoRemoteControlPort>");
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_WhenUserFileExists_UpdatesPort()
	{
		var csproj = CreateProjectFile();
		var userFile = csproj + ".user";
		File.WriteAllText(userFile,
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
			  <PropertyGroup>
			    <UnoRemoteControlPort>14300</UnoRemoteControlPort>
			  </PropertyGroup>
			</Project>
			""");

		CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 62483);

		var updated = File.ReadAllText(userFile);
		updated.Should().Contain("62483#");
		updated.Should().NotContain("14300");
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_AcceptsUserPathDirectly()
	{
		var csproj = CreateProjectFile();
		var userFile = csproj + ".user";

		CsprojUserGenerator.SetCsprojUserPortForProject(userFile, 55000);

		File.Exists(userFile).Should().BeTrue();
		File.ReadAllText(userFile).Should().Contain("55000#");
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_WithInvalidPort_Throws()
	{
		var csproj = CreateProjectFile();

		var act = () => CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 0);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_WithSolutionPath_DoesNothing()
	{
		var sln = Path.Combine(_tempRoot, "App.sln");

		CsprojUserGenerator.SetCsprojUserPortForProject(sln, 62483);

		File.Exists(sln + ".user").Should().BeFalse();
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_WithNonExistentCsproj_DoesNotCreateUserFile()
	{
		var csproj = Path.Combine(_tempRoot, "Ghost", "Ghost.csproj");

		CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 62483);

		File.Exists(csproj + ".user").Should().BeFalse();
	}

	[TestMethod]
	public void SetCsprojUserPortForProject_WithNonProjectUserPath_IsIgnored()
	{
		// The contract is .csproj/.csproj.user only; unrelated MSBuild user files must never be touched.
		// Not created when absent...
		var missing = Path.Combine(_tempRoot, "App.sln.user");

		CsprojUserGenerator.SetCsprojUserPortForProject(missing, 62483);

		File.Exists(missing).Should().BeFalse();

		// ...and not modified when it already exists.
		var existing = Path.Combine(_tempRoot, "Other.sln.user");
		const string original = "<Project />";
		File.WriteAllText(existing, original);

		CsprojUserGenerator.SetCsprojUserPortForProject(existing, 62483);

		File.ReadAllText(existing).Should().Be(original);
	}

	[TestMethod]
	public void TryGetConfiguredPort_WhenNoUserFile_ReturnsFalse()
	{
		var csproj = CreateProjectFile();

		var found = CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port);

		found.Should().BeFalse();
		port.Should().Be(0);
	}

	[TestMethod]
	public void TryGetConfiguredPort_StripsTrailingHashMarker()
	{
		var csproj = CreateProjectFile();
		CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 62483);

		var found = CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port);

		found.Should().BeTrue();
		port.Should().Be(62483);
	}

	[TestMethod]
	public void TryGetConfiguredPort_ReadsPlainPortWithoutMarker()
	{
		var csproj = CreateProjectFile();
		var userFile = csproj + ".user";
		File.WriteAllText(userFile,
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
			  <PropertyGroup>
			    <UnoRemoteControlPort>14300</UnoRemoteControlPort>
			  </PropertyGroup>
			</Project>
			""");

		var found = CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port);

		found.Should().BeTrue();
		port.Should().Be(14300);
	}

	[TestMethod]
	public async Task SetCsprojUserPortForPath_WithProjectFile_WritesProjectUser()
	{
		var csproj = CreateProjectFile();

		await CsprojUserGenerator.SetCsprojUserPortForPath(csproj, 62483);

		File.Exists(csproj + ".user").Should().BeTrue();
	}

	[TestMethod]
	public async Task SetCsprojUserPortForPath_WithDirectoryContainingProject_WritesProjectUser()
	{
		var csproj = CreateProjectFile();
		var projectDir = Path.GetDirectoryName(csproj)!;

		await CsprojUserGenerator.SetCsprojUserPortForPath(projectDir, 62483);

		File.Exists(csproj + ".user").Should().BeTrue();
		CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port).Should().BeTrue();
		port.Should().Be(62483);
	}

	[TestMethod]
	public void SetThenReadRoundTrips()
	{
		var csproj = CreateProjectFile("SomethingApp");

		CsprojUserGenerator.SetCsprojUserPortForProject(csproj, 51717);

		CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port).Should().BeTrue();
		port.Should().Be(51717);
	}

	[TestMethod]
	[DataRow("0")]
	[DataRow("-1")]
	[DataRow("99999")]
	[DataRow("not-a-number")]
	public void TryGetConfiguredPort_WithInvalidOrOutOfRangeValue_ReturnsFalse(string rawPortValue)
	{
		var csproj = CreateProjectFile();
		WriteUserFileWithPort(csproj, rawPortValue);

		var found = CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port);

		found.Should().BeFalse();
		port.Should().Be(0);
	}

	[TestMethod]
	public void TryGetConfiguredPort_TrimsWhitespaceAroundMarker()
	{
		var csproj = CreateProjectFile();
		WriteUserFileWithPort(csproj, "62483 #");

		var found = CsprojUserGenerator.TryGetConfiguredPort(csproj, out var port);

		found.Should().BeTrue();
		port.Should().Be(62483);
	}
}
