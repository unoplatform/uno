using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli.Tests.Mcp.Setup;

[TestClass]
public class Given_FileSystem
{
	[TestMethod]
	public void ResolveAppDataPath_MacOS_UsesApplicationSupport()
	{
		var result = FileSystem.ResolveAppDataPath(
			static _ => null,
			static () => "/Users/testuser",
			static _ => "/ignored",
			isMacOS: true,
			isLinux: false);

		result.Should().Be(Path.Combine("/Users/testuser", "Library", "Application Support"));
	}

	[TestMethod]
	public void ResolveAppDataPath_LinuxNative_UsesLinuxAppData()
	{
		var result = FileSystem.ResolveAppDataPath(
			static _ => null,
			static () => "/home/testuser",
			static _ => "/home/testuser/.config",
			isMacOS: false,
			isLinux: true);

		result.Should().Be("/home/testuser/.config");
	}

	[TestMethod]
	public void ResolveAppDataPath_Wsl_UsesWindowsRoamingPath()
	{
		var result = FileSystem.ResolveAppDataPath(
			name => name switch
			{
				"WSL_DISTRO_NAME" => "Ubuntu",
				"APPDATA" => @"C:\Users\TestUser\AppData\Roaming",
				_ => null,
			},
			static () => "/home/testuser",
			static _ => "/home/testuser/.config",
			isMacOS: false,
			isLinux: true);

		result.Should().Be("/mnt/c/Users/TestUser/AppData/Roaming");
	}

	[TestMethod]
	public void ResolveAppDataPath_WslWithoutAppData_FallsBackToLinuxAppData()
	{
		var result = FileSystem.ResolveAppDataPath(
			name => name switch
			{
				"WSL_INTEROP" => "/run/WSL/123_interop",
				_ => null,
			},
			static () => "/home/testuser",
			static _ => "/home/testuser/.config",
			isMacOS: false,
			isLinux: true);

		result.Should().Be("/home/testuser/.config");
	}

	[TestMethod]
	public void ConvertWindowsPathToWslPath_ConvertsDrivePath()
	{
		var result = FileSystem.ConvertWindowsPathToWslPath(@"C:\Users\TestUser\AppData\Roaming");

		result.Should().Be("/mnt/c/Users/TestUser/AppData/Roaming");
	}

	[TestMethod]
	public void GetPathComparer_Windows_IsCaseInsensitive()
	{
		var comparer = FileSystem.GetPathComparer(isWindows: true);

		comparer.Equals("/project/.cursor/mcp.json", "/project/.CURSOR/mcp.json").Should().BeTrue();
	}

	[TestMethod]
	public void GetPathComparer_NonWindows_IsCaseSensitive()
	{
		var comparer = FileSystem.GetPathComparer(isWindows: false);

		comparer.Equals("/project/.cursor/mcp.json", "/project/.CURSOR/mcp.json").Should().BeFalse();
	}
}
