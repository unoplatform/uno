namespace Uno.UI.DevServer.Cli.Helpers;

public sealed class DiscoveryInfo
{
	/// <summary>
	/// Gets the working directory used for discovery.
	/// </summary>
	public string? WorkingDirectory { get; init; }

	/// <summary>
	/// Gets the resolved path to global.json if found.
	/// </summary>
	public string? GlobalJsonPath { get; init; }

	/// <summary>
	/// Gets the source used to resolve the Uno SDK (e.g. global.json, project).
	/// </summary>
	public string? UnoSdkSource { get; init; }

	/// <summary>
	/// Gets the path associated with the Uno SDK source (e.g. global.json or project file).
	/// </summary>
	public string? UnoSdkSourcePath { get; init; }

	/// <summary>
	/// Gets the Uno SDK package id (Uno.Sdk or Uno.Sdk.Private).
	/// </summary>
	public string? UnoSdkPackage { get; init; }

	/// <summary>
	/// Gets the Uno SDK package version.
	/// </summary>
	public string? UnoSdkVersion { get; init; }

	/// <summary>
	/// Gets the resolved Uno SDK package path in the NuGet cache.
	/// </summary>
	public string? UnoSdkPath { get; init; }

	/// <summary>
	/// Gets the resolved packages.json path from the Uno SDK.
	/// </summary>
	public string? PackagesJsonPath { get; init; }

	/// <summary>
	/// Gets the Uno.WinUI.DevServer package version from packages.json.
	/// </summary>
	public string? DevServerPackageVersion { get; init; }

	/// <summary>
	/// Gets the resolved Uno.WinUI.DevServer package path in the NuGet cache.
	/// </summary>
	public string? DevServerPackagePath { get; init; }

	/// <summary>
	/// Gets the uno.settings.devserver package version from packages.json.
	/// </summary>
	public string? SettingsPackageVersion { get; init; }

	/// <summary>
	/// Gets the resolved uno.settings.devserver package path in the NuGet cache.
	/// </summary>
	public string? SettingsPackagePath { get; init; }

	/// <summary>
	/// Gets the raw dotnet --version output.
	/// </summary>
	public string? DotNetVersion { get; init; }

	/// <summary>
	/// Gets the computed target framework moniker (e.g. net9.0).
	/// </summary>
	public string? DotNetTfm { get; init; }

	/// <summary>
	/// Gets the resolved DevServer host executable or DLL path.
	/// </summary>
	public string? HostPath { get; init; }

	/// <summary>
	/// Gets the resolved Settings application (Studio / Licensing) path.
	/// </summary>
	public string? SettingsPath { get; init; }

	/// <summary>
	/// Gets the resolved add-ins discovered via convention-based parsing.
	/// </summary>
	public IReadOnlyList<ResolvedAddIn> AddIns { get; init; } = [];

	/// <summary>
	/// Gets the method used to discover add-ins (e.g. "targets").
	/// </summary>
	public string? AddInsDiscoveryMethod { get; init; }

	/// <summary>
	/// Gets the duration of add-in discovery in milliseconds.
	/// </summary>
	public long AddInsDiscoveryDurationMs { get; init; }

	/// <summary>
	/// Gets whether convention-based add-in discovery failed with an exception.
	/// </summary>
	public bool AddInDiscoveryFailed { get; init; }

	/// <summary>
	/// Gets the active DevServer instance information if one is running for this workspace.
	/// </summary>
	public ActiveServerInfo? ActiveServer { get; init; }

	/// <summary>
	/// Gets warnings encountered during discovery.
	/// </summary>
	public IReadOnlyList<string> Warnings { get; init; } = [];

	/// <summary>
	/// Gets errors encountered during discovery.
	/// </summary>
	public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Describes an active DevServer instance discovered via the ambient registry.
/// </summary>
public sealed class ActiveServerInfo
{
	public int ProcessId { get; init; }
	public int Port { get; init; }
	public string McpEndpoint { get; init; } = "";
	public int ParentProcessId { get; init; }
	public DateTime StartTime { get; init; }
	public string? IdeChannelId { get; init; }
}
