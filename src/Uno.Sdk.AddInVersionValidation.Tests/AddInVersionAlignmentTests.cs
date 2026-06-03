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
/// Validates that the assembly load graph rooted at each DevServer add-in's primary DLL
/// can be satisfied at runtime by the host's effective TPA without forcing the bridge in
/// <c>TryBridgeBySimpleName</c> into a downgrade, and without two add-ins disagreeing on
/// the version of an assembly the host doesn't carry.
/// </summary>
/// <remarks>
/// <para>
/// Rather than scanning every DLL in each add-in's NuGet payload (which produces noise
/// from client-side DLLs in <c>lib/net*</c> that the host never loads), this test simulates
/// the runtime resolution path:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       The host's effective TPA is computed from the .NET shared frameworks
///       (Microsoft.NETCore.App + Microsoft.AspNetCore.App) plus the bundled host deps
///       under <c>tools/rc/host/net10.0/</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       For each add-in, the BFS walk starts at the primary server DLL declared in the
///       package's <c>buildTransitive/*.targets</c> file (the same mechanism the DevServer
///       CLI uses to discover add-ins).
///     </description>
///   </item>
///   <item>
///     <description>
///       At each visited DLL, every <c>AssemblyRef</c> is resolved against either the host
///       TPA (no walk into host DLLs) or a same-directory bundled DLL (which is enqueued
///       and walked transitively). Unresolved refs are recorded for cross-add-in analysis.
///     </description>
///   </item>
/// </list>
/// <para>
/// Two failure categories drive the test's verdict:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Case 1 (guaranteed FNFE)</b>: an add-in needs a higher version than the host
///       carries AND it does not bundle that version in its loadable directory. The bridge
///       refuses to downgrade and the LoadFromResolveHandler finds nothing — runtime
///       <see cref="FileNotFoundException"/> is deterministic.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Case 2 (non-deterministic load order)</b>: an assembly is not in the host's TPA
///       AND two add-ins effectively contribute different versions of it (either via a
///       bundled DLL or an unresolved AssemblyRef). Whichever add-in loads first wins;
///       the other faces a downgrade-refusal at runtime. The failure manifests
///       inconsistently across runs.
///     </description>
///   </item>
/// </list>
/// <para>
/// Forward-roll cases (host carries a higher version than an add-in needs) are silently
/// healthy and do not appear in the report. So are intra-add-in refs to older versions of
/// an assembly the add-in itself bundles at a higher version — the walk follows the bundled
/// version and ignores the lower internal references the bridge would forward-roll.
/// </para>
/// </remarks>
[TestClass]
public class AddInVersionAlignmentTests
{
	private static readonly (string PackageId, string ManifestGroup)[] KnownAddIns =
	[
		("Uno.Settings.DevServer", "settings"),
		("Uno.UI.HotDesign", "hotdesign"),
		("Uno.UI.App.Mcp", "AppMcp"),
	];

	[TestMethod]
	[Description("Walks the assembly load graph rooted at each add-in's primary server DLL and fails on real version-resolution conflicts " +
		"(guaranteed downgrade-refusal at runtime, or non-deterministic load-order disagreements between add-ins).")]
	public void AddInLoadGraphMustResolveAgainstHostTpaWithoutDowngrade()
	{
		var metadata = ReadAssemblyMetadata();

		// Each add-in PackageReference is injected at build time by the
		// _InjectAddInPackageReferencesFromManifest target in the csproj, reading the
		// version directly from src/Uno.Sdk/packages.json. There is no separate sync gate
		// to verify — by construction the versions match the manifest.
		var hostBinDir = metadata["UnoHostBinDir"];
		Directory.Exists(hostBinDir).Should().BeTrue(
			"the locally-built host bin directory must exist at '{0}' (ProjectReference to " +
			"Uno.UI.RemoteControl.Host.csproj should have built it)", hostBinDir);

		var hostTpa = BuildHostTpa(hostBinDir);
		var hostAssemblyGraph = BuildHostAssemblyGraph(hostBinDir);

		// BFS each add-in's load graph from its primary server DLL.
		var case1Fatal = new List<Case1>();
		var case1Latent = new List<Case1Latent>();
		var perAddInContributions = new Dictionary<string, Dictionary<string, AddInContribution>>(StringComparer.OrdinalIgnoreCase);
		var unresolvedReachable = new List<UnresolvedRef>();

		foreach (var (packageId, _) in KnownAddIns)
		{
			var packageRoot = metadata[ToMetadataKey(packageId)];
			var primary = FindPrimaryServerDll(packageRoot);
			if (primary is null)
			{
				Assert.Fail(
					$"Could not locate a primary server DLL for '{packageId}'. The convention is " +
					$"'tools/devserver/*Server.dll' or 'tools/*/server/*Server.dll'. Package root: {packageRoot}");
			}

			var loadableDir = Path.GetDirectoryName(primary)!;
			var contributions = new Dictionary<string, AddInContribution>(StringComparer.OrdinalIgnoreCase);
			perAddInContributions[packageId] = contributions;

			WalkLoadGraph(
				primary,
				loadableDir,
				hostTpa,
				packageId,
				contributions,
				case1Fatal,
				case1Latent,
				unresolvedReachable);
		}

		// Case 2: cross-add-in disagreement on assemblies not in host TPA.
		// Each add-in either "bundles" a version (loaded via LoadFromResolveHandler) or
		// has an "unresolved" ref to a version it can't satisfy on its own. Both count as
		// the add-in's effective demand for that simple name.
		var case2 = ClassifyCase2(perAddInContributions, hostTpa);

		if (case1Fatal.Count == 0 && case1Latent.Count == 0 && case2.Count == 0)
		{
			return;
		}

		var report = BuildReport(case1Fatal, case1Latent, case2, unresolvedReachable, hostAssemblyGraph, metadata["UnoSdkManifestPath"]);
		Assert.Fail(report);
	}

	private static void WalkLoadGraph(
		string primaryDll,
		string loadableDir,
		IReadOnlyDictionary<string, Version> hostTpa,
		string addInPackage,
		Dictionary<string, AddInContribution> contributions,
		List<Case1> case1Fatal,
		List<Case1Latent> case1Latent,
		List<UnresolvedRef> unresolvedReachable)
	{
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var queue = new Queue<string>();
		queue.Enqueue(primaryDll);

		// Record the primary DLL as the add-in's own contribution (lets cross-add-in
		// detect disagreements on the primary DLL itself, though unlikely in practice).
		if (TryReadAssemblyVersion(primaryDll, out var primaryVersion))
		{
			contributions[Path.GetFileNameWithoutExtension(primaryDll)] = new AddInContribution(
				Kind: ContributionKind.Bundled,
				Version: primaryVersion,
				SourceDll: primaryDll);
		}

		while (queue.TryDequeue(out var dll))
		{
			if (!visited.Add(dll))
			{
				continue;
			}

			foreach (var (refName, refVersion) in ReadAssemblyRefs(dll))
			{
				// Resolution priority: host TPA first (default ALC wins).
				if (hostTpa.TryGetValue(refName, out var hostVersion))
				{
					if (hostVersion >= refVersion)
					{
						// Forward-roll: the add-in's compiled AssemblyRef requests an older
						// AssemblyVersion than what the host loads. The default ALC binder
						// would strict-match-fail on OOB-style refs (Kiota → STEW v8 with
						// host v10 was the original #23287 crash), but the bridge handler
						// in HostAssemblyResolution.TryBridgeBySimpleName intercepts via
						// Default.Resolving and returns the host's higher instance. As long
						// as that handler stays in place, forward-rolls do not crash at
						// runtime, so they are deliberately not reported here — this test
						// gates on currently-crashable misalignments only.
						continue;
					}

					// Host has a lower version than the add-in needs. The bridge would
					// refuse to downgrade — check the LoadFromResolveHandler fallback.
					var bundlePath = Path.Combine(loadableDir, refName + ".dll");
					if (File.Exists(bundlePath)
						&& TryReadAssemblyVersion(bundlePath, out var bundleVersion)
						&& bundleVersion >= refVersion)
					{
						// The add-in ships its own copy at the required version. At
						// runtime, the LoadFromResolveHandler will load the bundled
						// version into Default ALC alongside the host's. The bug is
						// "latent" — it works, but Default ALC ends up carrying two
						// versions of the same simple name (a minor identity smell, not
						// a hard failure).
						case1Latent.Add(new Case1Latent(
							Reference: refName,
							HostVersion: hostVersion,
							RequiredVersion: refVersion,
							BundledVersion: bundleVersion,
							AddInPackage: addInPackage,
							ReferencingDll: dll,
							BundledDll: bundlePath));

						// Continue walking through the bundled DLL — its own refs may
						// expose further conflicts.
						queue.Enqueue(bundlePath);
					}
					else
					{
						// No bundled fallback. Bridge refuses the downgrade and the
						// LoadFromResolveHandler finds nothing → guaranteed FNFE the
						// moment runtime resolves this reference.
						case1Fatal.Add(new Case1(
							Reference: refName,
							HostVersion: hostVersion,
							RequiredVersion: refVersion,
							AddInPackage: addInPackage,
							ReferencingDll: dll));
					}

					continue;
				}

				// Not in host TPA. Look for the assembly in the loadable directory.
				var bundleCandidate = Path.Combine(loadableDir, refName + ".dll");
				if (File.Exists(bundleCandidate) && TryReadAssemblyVersion(bundleCandidate, out var bundledVersion))
				{
					// The add-in contributes this assembly to runtime via its own dir.
					// Record the bundled version (or the highest seen) for cross-add-in
					// comparison.
					if (!contributions.TryGetValue(refName, out var existing) || bundledVersion > existing.Version)
					{
						contributions[refName] = new AddInContribution(
							Kind: ContributionKind.Bundled,
							Version: bundledVersion,
							SourceDll: bundleCandidate);
					}

					queue.Enqueue(bundleCandidate);
				}
				else
				{
					// Reachable from the load graph but neither host TPA nor the add-in's
					// own bundle can satisfy it. At runtime this triggers FNFE unless
					// another add-in's LoadFromResolveHandler scope happens to carry it.
					// Record the add-in's max-needed version for cross-add-in analysis;
					// case 2 will pick up the disagreement if relevant.
					if (!contributions.TryGetValue(refName, out var existing) || refVersion > existing.Version)
					{
						contributions[refName] = new AddInContribution(
							Kind: ContributionKind.UnresolvedRequirement,
							Version: refVersion,
							SourceDll: dll);
					}

					unresolvedReachable.Add(new UnresolvedRef(
						Reference: refName,
						RequiredVersion: refVersion,
						AddInPackage: addInPackage,
						ReferencingDll: dll));
				}
			}
		}
	}

	private static List<Case2> ClassifyCase2(
		IReadOnlyDictionary<string, Dictionary<string, AddInContribution>> perAddInContributions,
		IReadOnlyDictionary<string, Version> hostTpa)
	{
		// Flip the per-add-in map into a per-simpleName map.
		var bySimpleName = new Dictionary<string, List<Case2Entry>>(StringComparer.OrdinalIgnoreCase);
		foreach (var (packageId, contributions) in perAddInContributions)
		{
			foreach (var (simpleName, contribution) in contributions)
			{
				if (hostTpa.ContainsKey(simpleName))
				{
					// Host carries it — case 1 territory, not case 2.
					continue;
				}

				if (!bySimpleName.TryGetValue(simpleName, out var entries))
				{
					entries = [];
					bySimpleName[simpleName] = entries;
				}
				entries.Add(new Case2Entry(packageId, contribution));
			}
		}

		return bySimpleName
			.Where(kvp => kvp.Value.Select(e => e.AddInPackage).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
			.Where(kvp => kvp.Value.Select(e => e.Contribution.Version).Distinct().Count() > 1)
			.Select(kvp => new Case2(kvp.Key, kvp.Value))
			.OrderBy(c => c.Reference, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static string BuildReport(
		List<Case1> case1Fatal,
		List<Case1Latent> case1Latent,
		List<Case2> case2,
		List<UnresolvedRef> unresolvedReachable,
		IReadOnlyDictionary<string, IReadOnlyList<string>> hostAssemblyGraph,
		string manifestPath)
	{
		var report = new StringBuilder();
		var fatalDistinct = case1Fatal
			.GroupBy(c => (c.Reference, c.AddInPackage, c.RequiredVersion, c.HostVersion))
			.Count();
		var latentDistinct = case1Latent
			.GroupBy(c => (c.Reference, c.AddInPackage, c.RequiredVersion, c.HostVersion, c.BundledVersion))
			.Count();

		report.AppendLine($"Detected {fatalDistinct} guaranteed and {latentDistinct} latent runtime-blocking conflicts, plus {case2.Count} non-deterministic load-order conflicts.");
		report.AppendLine();

		if (case1Fatal.Count > 0)
		{
			report.AppendLine($"== Case 1 fatal ({fatalDistinct}): add-in needs a HIGHER version than the host, no bundled fallback → guaranteed FileNotFoundException ==");
			report.AppendLine();
			foreach (var group in case1Fatal
				.GroupBy(c => (c.Reference, c.AddInPackage, c.RequiredVersion, c.HostVersion))
				.OrderBy(g => g.Key.Reference, StringComparer.OrdinalIgnoreCase)
				.ThenBy(g => g.Key.AddInPackage, StringComparer.OrdinalIgnoreCase))
			{
				var dlls = group.Select(c => Path.GetFileName(c.ReferencingDll))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
					.ToList();
				var dllList = dlls.Count <= 3 ? string.Join(", ", dlls) : $"{string.Join(", ", dlls.Take(3))}, and {dlls.Count - 3} more";

				report.AppendLine($"  • {group.Key.Reference}");
				report.AppendLine($"      host loaded   v{group.Key.HostVersion}{FormatHostReferrers(group.Key.Reference, hostAssemblyGraph)}");
				report.AppendLine($"      add-in needs  v{group.Key.RequiredVersion}  ({group.Key.AddInPackage})");
				report.AppendLine($"      reached via   {dllList}");
				report.AppendLine();
			}
			report.AppendLine("Resolution: bump the host's reference to at least the version the add-in needs, OR pin the add-in down to the version the host carries, OR ship the required version inside the add-in's loadable directory so the LoadFromResolveHandler fallback can satisfy it (latent — see next section).");
			report.AppendLine();
		}

		if (case1Latent.Count > 0)
		{
			report.AppendLine($"== Case 1 latent ({latentDistinct}): add-in needs higher than host AND bundles a copy in its loadable dir — works via LoadFromResolveHandler but pollutes Default ALC with multiple versions ==");
			report.AppendLine();
			foreach (var group in case1Latent
				.GroupBy(c => (c.Reference, c.AddInPackage, c.RequiredVersion, c.HostVersion, c.BundledVersion))
				.OrderBy(g => g.Key.Reference, StringComparer.OrdinalIgnoreCase)
				.ThenBy(g => g.Key.AddInPackage, StringComparer.OrdinalIgnoreCase))
			{
				report.AppendLine($"  • {group.Key.Reference}");
				report.AppendLine($"      host loaded     v{group.Key.HostVersion}{FormatHostReferrers(group.Key.Reference, hostAssemblyGraph)}");
				report.AppendLine($"      add-in needs    v{group.Key.RequiredVersion}");
				report.AppendLine($"      add-in bundles  v{group.Key.BundledVersion}  ({group.Key.AddInPackage})");
				report.AppendLine();
			}
			report.AppendLine("These are not hard failures today — the LoadFromResolveHandler in Assembly.LoadFrom loads the add-in's bundled copy into Default ALC alongside the host's. The cost is two versions of the same simple name coexisting; cross-component code that tries to mix them gets TypeLoadException. Aligning the host's version with what the add-in ships removes the duplication.");
			report.AppendLine();
		}

		if (case2.Count > 0)
		{
			report.AppendLine($"== Case 2 ({case2.Count}): assembly not in host TPA AND add-ins disagree — non-deterministic load-order bug ==");
			report.AppendLine();
			foreach (var conflict in case2)
			{
				report.AppendLine($"  • {conflict.Reference}");
				foreach (var entry in conflict.Entries
					.OrderBy(e => e.AddInPackage, StringComparer.OrdinalIgnoreCase)
					.ThenBy(e => e.Contribution.Version))
				{
					var origin = entry.Contribution.Kind == ContributionKind.Bundled ? "bundles" : "needs (unresolved)";
					report.AppendLine($"      {entry.AddInPackage,-30} {origin,-22} v{entry.Contribution.Version}");
				}
				report.AppendLine();
			}
			report.AppendLine("The first add-in to resolve the assembly wins at runtime. Other add-ins requesting a higher version then face the bridge's downgrade-refusal. Pick a single version across all add-ins, OR have the host take ownership by bringing the dependency into its TPA.");
			report.AppendLine();
		}

		// Unresolved-reachable is informational: it lists refs that the walk reached but
		// neither the host nor the add-in could satisfy. If another add-in happens to
		// provide them at runtime, case 2 already flagged any disagreement. The list is
		// useful for diagnosing surprise FNFEs not captured by case 1 or case 2.
		if (unresolvedReachable.Count > 0)
		{
			var unresolvedDistinct = unresolvedReachable
				.GroupBy(u => (u.Reference, u.AddInPackage, u.RequiredVersion))
				.Count();
			report.AppendLine($"-- Informational: {unresolvedDistinct} reachable AssemblyRef(s) the add-in's own scope cannot satisfy (will rely on cross-add-in LoadFromResolveHandler at runtime) --");
		}

		report.AppendLine();
		report.AppendLine($"Manifest source of truth: {manifestPath}");
		return report.ToString();
	}

	private record Case1(string Reference, Version HostVersion, Version RequiredVersion, string AddInPackage, string ReferencingDll);
	private record Case1Latent(string Reference, Version HostVersion, Version RequiredVersion, Version BundledVersion, string AddInPackage, string ReferencingDll, string BundledDll);
	private record UnresolvedRef(string Reference, Version RequiredVersion, string AddInPackage, string ReferencingDll);
	private enum ContributionKind { Bundled, UnresolvedRequirement }
	private record AddInContribution(ContributionKind Kind, Version Version, string SourceDll);
	private record Case2Entry(string AddInPackage, AddInContribution Contribution);
	private record Case2(string Reference, List<Case2Entry> Entries);
	private record ManifestGroup(string Group, string Version);

	private static IReadOnlyDictionary<string, string> ReadAssemblyMetadata()
	{
		var dict = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var attribute in typeof(AddInVersionAlignmentTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
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
				$"No assembly-metadata key declared for package '{packageId}'.")
		};

	private static List<ManifestGroup> ParseManifest(string manifestPath)
	{
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

	/// <summary>
	/// Heuristic discovery of an add-in's primary server DLL. The DevServer convention is
	/// either <c>tools/devserver/&lt;PackageName&gt;.dll</c> (or any <c>*Server.dll</c>)
	/// for net-agnostic add-ins, or <c>tools/&lt;tfm&gt;/server/&lt;PackageName&gt;.Server.dll</c>
	/// for TFM-specific add-ins (e.g. HotDesign). Returns the first match.
	/// </summary>
	private static string? FindPrimaryServerDll(string packageRoot)
	{
		var devServer = Path.Combine(packageRoot, "tools", "devserver");
		if (Directory.Exists(devServer))
		{
			var dll = Directory.EnumerateFiles(devServer, "*Server.dll").FirstOrDefault()
				?? Directory.EnumerateFiles(devServer, "*.DevServer.dll").FirstOrDefault();
			if (dll is not null)
			{
				return dll;
			}
		}

		var toolsRoot = Path.Combine(packageRoot, "tools");
		if (Directory.Exists(toolsRoot))
		{
			foreach (var tfmDir in Directory.EnumerateDirectories(toolsRoot))
			{
				var server = Path.Combine(tfmDir, "server");
				if (!Directory.Exists(server))
				{
					continue;
				}
				var dll = Directory.EnumerateFiles(server, "*Server.dll").FirstOrDefault();
				if (dll is not null)
				{
					return dll;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Builds the host's effective TPA: every assembly the runtime resolves to before any
	/// add-in is consulted. Sources: the .NET shared frameworks
	/// (Microsoft.NETCore.App + Microsoft.AspNetCore.App per the host's runtimeconfig.json)
	/// and the host's bundled deps. <paramref name="hostBinDir"/> is the directory containing
	/// the host DLLs — typically the locally-built <c>bin/&lt;config&gt;/net10.0/</c> output
	/// when the test references the host project directly.
	/// </summary>
	private static Dictionary<string, Version> BuildHostTpa(string hostBinDir)
	{
		var tpa = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

		var bclDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
		foreach (var sharedFrameworkDir in EnumerateSharedFrameworkDirs(bclDir))
		{
			foreach (var dll in Directory.EnumerateFiles(sharedFrameworkDir, "*.dll"))
			{
				var name = Path.GetFileNameWithoutExtension(dll);
				if (TryReadAssemblyVersion(dll, out var version) && !tpa.ContainsKey(name))
				{
					tpa[name] = version;
				}
			}
		}

		if (Directory.Exists(hostBinDir))
		{
			foreach (var dll in Directory.EnumerateFiles(hostBinDir, "*.dll"))
			{
				var name = Path.GetFileNameWithoutExtension(dll);
				if (TryReadAssemblyVersion(dll, out var version))
				{
					// Bundled host deps win over BCL if both exist — the host's runtime
					// loads them from its own bin directory ahead of any framework fallback.
					tpa[name] = version;
				}
			}
		}

		return tpa;
	}

	/// <summary>
	/// Walks the host's transitive AssemblyRef graph starting from the host's primary
	/// executable assembly. For each referenced assembly simple name, records the host DLLs
	/// that reference it. This is the source of truth for two report features:
	/// <list type="bullet">
	///   <item><description>Attribution: when an add-in clashes on an assembly the host carries, name the host DLL(s) that ref the same assembly.</description></item>
	///   <item><description>Cross-major noise filter: assemblies the host's code does not actually reference (System.Collections, System.Threading — type-forwards) are absent from the graph and therefore excluded from the cross-major report.</description></item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// The walk is bounded to DLLs physically present in <paramref name="hostBinDir"/>: when
	/// an <c>AssemblyRef</c> does not resolve to a DLL in that directory (typical for refs
	/// satisfied by the .NET shared framework), the ref is still recorded but the walk does
	/// not recurse into it. This is enough to capture every assembly the host's compiled code
	/// actually mentions, which is the relevant set for cross-major analysis.
	/// </remarks>
	private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildHostAssemblyGraph(string hostBinDir)
	{
		// The primary DLL is the one matching the bin directory's project name. For the
		// DevServer host project that is `Uno.UI.RemoteControl.Host.dll`. Resolve generically
		// off the .runtimeconfig.json or .deps.json (only one per project), since both follow
		// the `<ProjectName>.runtimeconfig.json` / `<ProjectName>.deps.json` convention.
		var depsFile = Directory.EnumerateFiles(hostBinDir, "*.deps.json").FirstOrDefault();
		if (depsFile is null)
		{
			return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		}

		var primaryDll = Path.Combine(hostBinDir, Path.GetFileNameWithoutExtension(depsFile)[..^Path.GetExtension(Path.GetFileNameWithoutExtension(depsFile)).Length] + ".dll");
		// Path.GetFileNameWithoutExtension strips ".json" from "X.deps.json" leaving "X.deps";
		// we want just "X" — drop the trailing ".deps".
		var primarySimple = Path.GetFileNameWithoutExtension(depsFile);
		if (primarySimple.EndsWith(".deps", StringComparison.OrdinalIgnoreCase))
		{
			primarySimple = primarySimple[..^5];
		}
		primaryDll = Path.Combine(hostBinDir, primarySimple + ".dll");

		if (!File.Exists(primaryDll))
		{
			return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		}

		var inverse = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var queue = new Queue<string>();
		queue.Enqueue(primaryDll);

		while (queue.TryDequeue(out var dll))
		{
			if (!visited.Add(dll))
			{
				continue;
			}

			var dllSimpleName = Path.GetFileNameWithoutExtension(dll);
			foreach (var (refName, _) in ReadAssemblyRefs(dll))
			{
				if (!inverse.TryGetValue(refName, out var refList))
				{
					refList = [];
					inverse[refName] = refList;
				}
				if (!refList.Contains(dllSimpleName, StringComparer.OrdinalIgnoreCase))
				{
					refList.Add(dllSimpleName);
				}

				var candidate = Path.Combine(hostBinDir, refName + ".dll");
				if (File.Exists(candidate))
				{
					queue.Enqueue(candidate);
				}
			}
		}

		return inverse.ToDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<string>)kvp.Value.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(),
			StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Formats the "referenced by ..." suffix listing the host DLLs whose AssemblyRef table
	/// mentions <paramref name="simpleName"/>. Empty when the host's transitive graph does
	/// not reference the assembly (i.e. it would be loaded via framework type-forward).
	/// </summary>
	private static string FormatHostReferrers(string simpleName, IReadOnlyDictionary<string, IReadOnlyList<string>> hostAssemblyGraph)
	{
		if (!hostAssemblyGraph.TryGetValue(simpleName, out var referrers) || referrers.Count == 0)
		{
			return string.Empty;
		}
		var capped = referrers.Count <= 3 ? string.Join(", ", referrers) : $"{string.Join(", ", referrers.Take(3))}, and {referrers.Count - 3} more";
		return $"  referenced by {capped}";
	}

	private static IEnumerable<string> EnumerateSharedFrameworkDirs(string? bclDir)
	{
		if (string.IsNullOrEmpty(bclDir) || !Directory.Exists(bclDir))
		{
			yield break;
		}

		yield return bclDir;

		var versionDir = Path.GetFileName(bclDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		var netCoreAppParent = Path.GetDirectoryName(bclDir);
		var sharedRoot = Path.GetDirectoryName(netCoreAppParent);
		if (string.IsNullOrEmpty(sharedRoot))
		{
			yield break;
		}

		var aspNetCoreAppRoot = Path.Combine(sharedRoot, "Microsoft.AspNetCore.App");
		if (!Directory.Exists(aspNetCoreAppRoot))
		{
			yield break;
		}

		var exactPatch = Path.Combine(aspNetCoreAppRoot, versionDir);
		if (Directory.Exists(exactPatch))
		{
			yield return exactPatch;
			yield break;
		}

		if (Version.TryParse(versionDir, out var bclVersion))
		{
			var sameMajorMinor = Directory.EnumerateDirectories(aspNetCoreAppRoot)
				.Where(d => Version.TryParse(Path.GetFileName(d), out var v)
					&& v.Major == bclVersion.Major
					&& v.Minor == bclVersion.Minor)
				.OrderByDescending(d => Version.Parse(Path.GetFileName(d)!))
				.FirstOrDefault();
			if (sameMajorMinor is not null)
			{
				yield return sameMajorMinor;
				yield break;
			}
		}

		var anySibling = Directory.EnumerateDirectories(aspNetCoreAppRoot).FirstOrDefault();
		if (anySibling is not null)
		{
			yield return anySibling;
		}
	}

	private static bool TryReadAssemblyVersion(string dllPath, out Version version)
	{
		try
		{
			using var stream = File.OpenRead(dllPath);
			using var peReader = new PEReader(stream);
			if (peReader.HasMetadata)
			{
				var reader = peReader.GetMetadataReader();
				version = reader.GetAssemblyDefinition().Version;
				return true;
			}
		}
		catch (BadImageFormatException) { /* not a managed assembly */ }
		catch (IOException) { /* transient file system issue */ }

		version = new Version();
		return false;
	}

	private static IEnumerable<(string Name, Version Version)> ReadAssemblyRefs(string dllPath)
	{
		List<(string, Version)>? collected = null;
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
				collected.Add((name, reference.Version));
			}
		}
		catch (BadImageFormatException) { /* not a managed assembly */ }

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
