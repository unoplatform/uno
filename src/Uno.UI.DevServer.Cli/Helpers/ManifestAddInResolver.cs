using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Resolves add-ins from a <c>devserver-addin.json</c> manifest file at the root of a NuGet package.
/// This is priority 1 in the discovery chain (before <c>.targets</c> parsing).
/// </summary>
/// <remarks>
/// Return semantics:
/// <list type="bullet">
///   <item><c>null</c> — no manifest found or version &gt; 1 (fall through to .targets)</item>
///   <item>Empty result — manifest found but parse error (stop chain for this package)</item>
///   <item>Populated result — success</item>
/// </list>
/// </remarks>
/// <seealso href="../addin-discovery.md"/>
internal class ManifestAddInResolver(ILogger<ManifestAddInResolver> logger, string? hostVersion = null)
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	private readonly ILogger<ManifestAddInResolver> _logger = logger;
	private readonly Version? _hostVersion = hostVersion is not null && Version.TryParse(hostVersion.Split('-')[0], out var v) ? v : null;

	public ManifestResult? TryResolveFromManifest(string packageRoot, string packageName, string packageVersion)
	{
		var manifestPath = Path.Combine(packageRoot, "devserver-addin.json");
		if (!File.Exists(manifestPath))
		{
			return null;
		}

		AddInManifest? manifest;
		try
		{
			var json = File.ReadAllText(manifestPath);
			manifest = JsonSerializer.Deserialize<AddInManifest>(json, _jsonOptions);
		}
		catch (JsonException ex)
		{
			_logger.LogWarning(ex, "Failed to parse devserver-addin.json in {Package}", packageName);
			return new ManifestResult();
		}
		catch (IOException ex)
		{
			_logger.LogWarning(ex, "Failed to read devserver-addin.json in {Package}", packageName);
			return new ManifestResult();
		}

		if (manifest is null)
		{
			_logger.LogWarning("devserver-addin.json in {Package} deserialized to null", packageName);
			return new ManifestResult();
		}

		if (manifest.Version > 1)
		{
			_logger.LogWarning(
				"devserver-addin.json in {Package} has version {Version}, falling through to .targets discovery",
				packageName, manifest.Version);
			return null;
		}

		var result = new ManifestResult();

		if (manifest.Addins is null or { Count: 0 })
		{
			return result;
		}

		foreach (var entry in manifest.Addins)
		{
			if (string.IsNullOrWhiteSpace(entry.EntryPoint))
			{
				_logger.LogWarning("devserver-addin.json in {Package} has an entry with missing entryPoint, skipping", packageName);
				continue;
			}

			// Normalize forward slashes to platform-specific separator
			var normalizedPath = entry.EntryPoint.Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(packageRoot, normalizedPath));

			if (!File.Exists(fullPath))
			{
				_logger.LogWarning(
					"devserver-addin.json in {Package} references {EntryPoint} but file not found at {FullPath}",
					packageName, entry.EntryPoint, fullPath);
				continue;
			}

			if (entry.MinHostVersion is not null && _hostVersion is not null)
			{
				if (Version.TryParse(entry.MinHostVersion.Split('-')[0], out var minVersion) && _hostVersion < minVersion)
				{
					_logger.LogWarning(
						"devserver-addin.json in {Package}: entry {EntryPoint} requires host >= {MinVersion} but current is {Current}, skipping",
						packageName, entry.EntryPoint, entry.MinHostVersion, hostVersion);
					continue;
				}
			}

			result.AddIns.Add(new ResolvedAddIn
			{
				PackageName = packageName,
				PackageVersion = packageVersion,
				EntryPointDll = fullPath,
				DiscoverySource = "manifest",
			});
		}

		return result;
	}
}

internal sealed class ManifestResult
{
	public List<ResolvedAddIn> AddIns { get; } = [];
}
