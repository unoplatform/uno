using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace Uno.Sdk.AddInVersionValidation.Tests;

/// <summary>
/// Validates that every DevServer add-in shipped together in a single Uno.Sdk release
/// (per <c>src/Uno.Sdk/packages.json</c>) agrees on the version of every <c>AssemblyRef</c>
/// it shares with the other add-ins of the same release. Misalignments produce runtime
/// failures in the DevServer host that are otherwise only discoverable when an end user
/// starts an Uno app from Visual Studio (issue #23304).
/// </summary>
/// <remarks>
/// The test is a regression gate. It is intentionally <em>expected to be RED</em> whenever
/// the add-in maintainers ship a Uno.Sdk version with misaligned dependencies, and turns
/// GREEN once they align them. The dedicated Linux CI step (<c>devserver_tests_linux</c>)
/// runs it on every PR, surfacing misalignments before they reach end users.
/// </remarks>
[TestClass]
public class AddInVersionAlignmentTests
{
	/// <summary>
	/// The DevServer add-in packages this validation knows about, paired with the
	/// <c>packages.json</c> group name that carries their shared version.
	/// </summary>
	private static readonly (string PackageId, string ManifestGroup)[] KnownAddIns =
	[
		("Uno.Settings.DevServer", "settings"),
		("Uno.UI.HotDesign", "hotdesign"),
		("Uno.UI.App.Mcp", "AppMcp"),
	];

	[TestMethod]
	[Description("All DevServer add-ins in a single Uno.Sdk release must reference the same version of every cross-add-in dependency. " +
		"This test is the regression gate that catches misalignments at PR time, before they manifest at runtime.")]
	public void AllAddInsAgreeOnCrossPackageAssemblyRefVersions()
	{
		var metadata = ReadAssemblyMetadata();
		var manifestPath = metadata["UnoSdkManifestPath"];
		File.Exists(manifestPath).Should().BeTrue(
			"the Uno.Sdk packages.json manifest must be reachable from the test for the alignment check to run");

		var manifest = ParseManifest(manifestPath);

		// Step 1 — sync gate: the csproj's PackageReference versions must match the manifest.
		// If they don't, restoring different versions than the manifest declares makes the rest
		// of the test meaningless.
		foreach (var (packageId, groupName) in KnownAddIns)
		{
			var manifestVersion = manifest
				.FirstOrDefault(g => string.Equals(g.Group, groupName, StringComparison.OrdinalIgnoreCase))
				?.Version;
			manifestVersion.Should().NotBeNull(
				"the manifest must declare a '{0}' group carrying the version for '{1}'", groupName, packageId);

			var metadataKey = ToMetadataKey(packageId);
			metadata.Should().ContainKey(metadataKey,
				"the test csproj must reference '{0}' with GeneratePathProperty so the restored package path is exposed", packageId);

			var restoredPath = metadata[metadataKey];
			var actualVersion = Path.GetFileName(restoredPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			actualVersion.Should().Be(manifestVersion,
				"the csproj's '{0}' PackageReference (Version={1}) must match the manifest (Version={2}). " +
				"Update the csproj to keep them in sync — packages.json is the source of truth.",
				packageId, actualVersion, manifestVersion);
		}

		// Step 2 — collect AssemblyRefs from the runtime payload of each add-in. The index
		// maps (refSimpleName) -> [(refVersion, sourceAddinPackageId, sourceDllPath)].
		var refIndex = new Dictionary<string, List<(string Version, string SourcePackage, string SourceDll)>>(StringComparer.OrdinalIgnoreCase);
		foreach (var (packageId, _) in KnownAddIns)
		{
			var packageRoot = metadata[ToMetadataKey(packageId)];
			foreach (var dllPath in EnumerateRuntimeDlls(packageRoot))
			{
				foreach (var (refName, refVersion) in ReadAssemblyRefs(dllPath))
				{
					if (!refIndex.TryGetValue(refName, out var occurrences))
					{
						occurrences = [];
						refIndex[refName] = occurrences;
					}

					// One entry per (version, source-package) pair — we don't care if the
					// same package references the same name twice from different DLLs.
					if (!occurrences.Any(o => o.Version == refVersion && o.SourcePackage == packageId))
					{
						occurrences.Add((refVersion, packageId, dllPath));
					}
				}
			}
		}

		// Step 3 — detect conflicts: a single simple name referenced by at least two distinct
		// add-in packages with at least two distinct versions. Same package referencing two
		// versions of the same name from its own internal DLLs is its own problem and not
		// what this cross-add-in test is about.
		var conflicts = refIndex
			.Where(kvp => kvp.Value.Select(o => o.Version).Distinct().Count() > 1)
			.Where(kvp => kvp.Value.Select(o => o.SourcePackage).Distinct().Count() > 1)
			.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (conflicts.Count > 0)
		{
			var report = new StringBuilder();
			report.AppendLine($"Detected {conflicts.Count} cross-add-in AssemblyRef version conflict(s) in the current Uno.Sdk manifest:");
			report.AppendLine();
			foreach (var conflict in conflicts)
			{
				report.AppendLine($"  • {conflict.Key}");
				foreach (var (version, source, _) in conflict.Value.OrderBy(o => o.SourcePackage, StringComparer.OrdinalIgnoreCase).ThenBy(o => o.Version, StringComparer.Ordinal))
				{
					report.AppendLine($"      Version={version}  ← referenced by {source}");
				}
				report.AppendLine();
			}
			report.AppendLine("These misalignments cause runtime FileNotFoundException, TypeLoadException or 'Unable to resolve service' failures in the DevServer host because");
			report.AppendLine("multiple add-ins load into a single AssemblyLoadContext and only one version of each simple name can win at runtime.");
			report.AppendLine();
			report.AppendLine("Resolution (pick one per conflicting reference):");
			report.AppendLine("  (1) Align the package version across all add-ins that ship in the same Uno.Sdk release. This is the preferred fix because it avoids");
			report.AppendLine("      cross-add-in Type-identity issues entirely. Each add-in's maintainer publishes a new version pinned to the agreed dependency.");
			report.AppendLine("  (2) Have one of the add-ins load the conflicting dependency inside a private AssemblyLoadContext it owns, and expose the contract");
			report.AppendLine("      surface through interfaces the host already loads. Use this when option (1) is impossible (e.g. true major-version break).");
			report.AppendLine();
			report.AppendLine($"Manifest source of truth: {manifestPath}");

			Assert.Fail(report.ToString());
		}
	}

	private static IReadOnlyDictionary<string, string> ReadAssemblyMetadata()
	{
		// AssemblyMetadataAttribute is emitted from the <AssemblyMetadata Include="..." Value="..." />
		// items in the csproj. Use it to pull the restored package paths and the manifest path
		// into the test process without depending on environment variables or hardcoded paths.
		var attributes = typeof(AddInVersionAlignmentTests).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>();
		var dict = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var attribute in attributes)
		{
			if (attribute.Key is not null && attribute.Value is not null)
			{
				dict[attribute.Key] = attribute.Value;
			}
		}
		return dict;
	}

	private static string ToMetadataKey(string packageId)
		=> packageId switch
		{
			"Uno.Settings.DevServer" => "UnoSettingsDevServerPath",
			"Uno.UI.HotDesign" => "UnoUIHotDesignPath",
			"Uno.UI.App.Mcp" => "UnoUIAppMcpPath",
			_ => throw new InvalidOperationException(
				$"No assembly-metadata key declared for package '{packageId}'. Add a matching " +
				"<AssemblyMetadata> item in the test csproj and extend this switch.")
		};

	private record ManifestGroup(string Group, string Version);

	private static List<ManifestGroup> ParseManifest(string manifestPath)
	{
		// The manifest schema in src/Uno.Sdk/packages.json is an array of { group, version, packages }.
		// We only need group + version here — the packages array is implicitly validated through the
		// KnownAddIns mapping.
		using var stream = File.OpenRead(manifestPath);
		using var document = JsonDocument.Parse(stream);
		var groups = new List<ManifestGroup>();
		foreach (var element in document.RootElement.EnumerateArray())
		{
			if (element.TryGetProperty("group", out var groupProperty)
				&& element.TryGetProperty("version", out var versionProperty)
				&& groupProperty.GetString() is { } groupName
				&& versionProperty.GetString() is { } version)
			{
				groups.Add(new ManifestGroup(groupName, version));
			}
		}
		return groups;
	}

	private static IEnumerable<string> EnumerateRuntimeDlls(string packageRoot)
	{
		// Collect DLLs from every directory tree where an add-in typically drops its runtime
		// payload — tools/devserver/ and tools/*/server/ for the DevServer host, plus lib/netN/
		// for the standard library layout. Skip ref/ and runtimes/<rid>/native/ which are not
		// runtime managed assemblies for the host process.
		string[] subRoots = ["tools", "lib", "buildTransitive"];
		foreach (var sub in subRoots)
		{
			var path = Path.Combine(packageRoot, sub);
			if (!Directory.Exists(path))
			{
				continue;
			}
			foreach (var dll in Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories))
			{
				if (dll.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
					|| dll.Contains($"{Path.DirectorySeparatorChar}native{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				yield return dll;
			}
		}
	}

	private static IEnumerable<(string Name, string Version)> ReadAssemblyRefs(string dllPath)
	{
		// Use System.Reflection.Metadata to walk the AssemblyRef table without loading the
		// assembly. This is faster than Assembly.LoadFrom and avoids polluting the test's
		// AppDomain — important because the test process itself does NOT need to execute
		// any add-in code; we only need to read static metadata.
		List<(string, string)>? collected = null;
		try
		{
			using var stream = File.OpenRead(dllPath);
			using var peReader = new PEReader(stream);
			if (!peReader.HasMetadata)
			{
				yield break;
			}
			var reader = peReader.GetMetadataReader();
			collected = [];
			foreach (var handle in reader.AssemblyReferences)
			{
				var reference = reader.GetAssemblyReference(handle);
				var name = reader.GetString(reference.Name);
				var version = reference.Version.ToString();
				collected.Add((name, version));
			}
		}
		catch (BadImageFormatException)
		{
			// Not a managed assembly (mixed-mode native DLL, COM stub, etc.). Skip.
		}

		if (collected is null)
		{
			yield break;
		}
		foreach (var entry in collected)
		{
			yield return entry;
		}
	}
}
