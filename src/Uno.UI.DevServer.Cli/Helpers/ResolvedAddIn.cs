namespace Uno.UI.DevServer.Cli.Helpers;

public sealed record ResolvedAddIn
{
	public required string PackageName { get; init; }
	public required string PackageVersion { get; init; }
	public required string EntryPointDll { get; init; }
	public required string DiscoverySource { get; init; }
}
