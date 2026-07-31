using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	/// <summary>
	/// Resolves the short MSBuild TFM (e.g. <c>net10.0-browserwasm</c>, <c>net10.0-ios26.0</c>,
	/// plain <c>net10.0</c>) of a Roslyn <see cref="Project"/> from the data the project itself
	/// carries — runtime metadata references for the framework version and platform identifier
	/// (source #1), preprocessor symbols as a tiebreaker for the wasm / desktop / plain case
	/// where the base ref pack alone cannot disambiguate (source #2). Output paths
	/// (<see cref="Project.OutputFilePath"/> / <see cref="Project.CompilationOutputInfo"/>) are
	/// never consulted — they are MSBuild-customizable and do not represent the runtime the
	/// compilation actually links against.
	/// </summary>
	/// <param name="project">The project to resolve the TFM for.</param>
	/// <param name="targetFramework">When the method returns <see langword="true"/>, the resolved short MSBuild TFM; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when the resolution succeeded; <see langword="false"/> when no recognised .NET ref pack was found in <see cref="Project.MetadataReferences"/> or when distinct ref packs surfaced different framework-version segments.</returns>
	public static bool TryGetTargetFramework(
		this Project project,
		[NotNullWhen(true)] out string? targetFramework)
	{
		ArgumentNullException.ThrowIfNull(project);

		targetFramework = null;

		string? frameworkVersion = null;
		string? platformTfm = null;

		foreach (var reference in project.MetadataReferences)
		{
			if (reference is not PortableExecutableReference { FilePath: { Length: > 0 } path })
			{
				continue;
			}

			if (!TryParseRefPackPath(path, out var fwVer, out var packPlatform, out var packPlatformVersion))
			{
				continue;
			}

			if (frameworkVersion is null)
			{
				frameworkVersion = fwVer;
			}
			else if (!string.Equals(frameworkVersion, fwVer, StringComparison.OrdinalIgnoreCase))
			{
				// Diverging framework versions across recognised ref packs — invariant violation.
				return false;
			}

			if (platformTfm is null && packPlatform is not null)
			{
				platformTfm = packPlatformVersion is null
					? $"{fwVer}-{packPlatform}"
					: $"{fwVer}-{packPlatform}{packPlatformVersion}";
			}
		}

		if (frameworkVersion is null)
		{
			// No recognised ref pack at all — the project does not link any .NET runtime.
			return false;
		}

		// Source #1 surfaced a platform-specific ref pack; resolution complete.
		if (platformTfm is not null)
		{
			targetFramework = platformTfm;
			return true;
		}

		// Source #2 (tiebreaker): only base packs were found, the platform is ambiguous
		// (plain net10.0 vs. net10.0-desktop vs. net10.0-browserwasm — all link the same
		// Microsoft.NETCore.App.Ref). Disambiguate via Uno's compile-time markers.
		if (project.ParseOptions is CSharpParseOptions csOpts)
		{
			foreach (var symbol in csOpts.PreprocessorSymbolNames)
			{
				if (string.Equals(symbol, "__WASM__", StringComparison.Ordinal))
				{
					targetFramework = $"{frameworkVersion}-browserwasm";
					return true;
				}

				if (string.Equals(symbol, "__DESKTOP__", StringComparison.Ordinal))
				{
					targetFramework = $"{frameworkVersion}-desktop";
					return true;
				}
			}
		}

		// Plain net10.0 (no platform marker, no platform-specific ref pack).
		targetFramework = frameworkVersion;
		return true;
	}

	/// <summary>
	/// Determines whether the project links at least one recognised .NET ref pack (see
	/// <see cref="TryParseRefPackPath"/>), i.e. whether its design-time build resolved the
	/// framework references at all.
	/// </summary>
	public static bool HasFrameworkReferences(this Project project)
	{
		ArgumentNullException.ThrowIfNull(project);

		return project.MetadataReferences.Any(reference =>
			reference is PortableExecutableReference { FilePath: { Length: > 0 } path }
			&& TryParseRefPackPath(path, out _, out _, out _));
	}

	/// <summary>
	/// Detects the missing-targeting-pack signature (spec 049): the design-time build
	/// resolved package references (the project is not empty) but produced no .NET framework
	/// reference — typically because the targeting pack pinned by a workload manifest is not
	/// installed and is only satisfied through a restore-time <c>PackageDownload</c>, which
	/// Roslyn's <c>MSBuildWorkspace</c> never runs. The .NET SDK deliberately emits no error
	/// for this under <c>DesignTimeBuild=true</c>, making this snapshot inspection the only
	/// detection point.
	/// </summary>
	public static bool IsMissingFrameworkReferences(this Project project)
	{
		ArgumentNullException.ThrowIfNull(project);

		return project.MetadataReferences
				.OfType<PortableExecutableReference>()
				.Any(r => r.FilePath is { Length: > 0 })
			&& !project.HasFrameworkReferences();
	}

	/// <summary>
	/// The flavors of <paramref name="headProjectPath"/> in <paramref name="solution"/>
	/// exhibiting the missing-targeting-pack signature (see
	/// <see cref="IsMissingFrameworkReferences"/>). Non-head projects are not scanned: a
	/// library flavor without framework references follows its head's fate through the
	/// reachability-based filtering.
	/// </summary>
	public static IReadOnlyList<Project> GetHeadFlavorsMissingFrameworkReferences(this Solution solution, string headProjectPath)
	{
		ArgumentNullException.ThrowIfNull(solution);

		return solution.Projects
			.Where(p => PathComparer.PathEquals(p.FilePath, headProjectPath))
			.Where(IsMissingFrameworkReferences)
			.ToList();
	}

	/// <summary>
	/// Attempts to locate the dotnet root whose SDK-installed targeting packs the project
	/// links, from the ref-pack layout <c>&lt;root&gt;/packs/&lt;Pack&gt;/&lt;ver&gt;/ref/&lt;tfm&gt;/&lt;asm&gt;.dll</c>.
	/// Returns <see langword="false"/> when the project's ref packs come from the NuGet
	/// cache (which carries no SDK root) or when it has none.
	/// </summary>
	public static bool TryGetDotnetRootFromFrameworkReferences(this Project project, [NotNullWhen(true)] out string? dotnetRoot)
	{
		ArgumentNullException.ThrowIfNull(project);

		dotnetRoot = null;

		foreach (var reference in project.MetadataReferences)
		{
			if (reference is not PortableExecutableReference { FilePath: { Length: > 0 } path }
				|| !TryParseRefPackPath(path, out _, out _, out _))
			{
				continue;
			}

			// path = <root>/packs/<PackName>/<PackVersion>/ref/<tfm>/<asm>.dll
			// Five parents up is the "packs" folder; its own parent is the dotnet root.
			var packsDirectory = path;
			for (var i = 0; i < 5 && packsDirectory is not null; i++)
			{
				packsDirectory = Path.GetDirectoryName(packsDirectory);
			}

			if (packsDirectory is null
				|| !"packs".Equals(Path.GetFileName(packsDirectory), StringComparison.OrdinalIgnoreCase))
			{
				// NuGet-cache layout (…/.nuget/packages/…) or an unexpected shape — no SDK root.
				continue;
			}

			if (Path.GetDirectoryName(packsDirectory) is { Length: > 0 } root)
			{
				dotnetRoot = root;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Recognised platform-specific ref pack name prefixes mapped to the corresponding TFM
	/// platform identifier. The actual on-disk pack name may carry a TFM-encoding suffix
	/// (e.g. <c>Microsoft.iOS.Ref.net10.0_26.0</c>); the matcher accepts both shapes.
	/// </summary>
	private static readonly Dictionary<string, string> _platformPackPrefixes = new(StringComparer.OrdinalIgnoreCase)
	{
		["Microsoft.iOS.Ref"] = "ios",
		["Microsoft.tvOS.Ref"] = "tvos",
		["Microsoft.MacCatalyst.Ref"] = "maccatalyst",
		["Microsoft.macOS.Ref"] = "macos",
		["Microsoft.Android.Ref"] = "android",
		["Microsoft.Windows.SDK.NET.Ref"] = "windows",
	};

	/// <summary>
	/// Recognised base (non-platform-specific) ref pack names. References to these packs
	/// pin the framework version but leave the platform unresolved — source #2 takes over.
	/// </summary>
	private static readonly HashSet<string> _basePackNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Microsoft.NETCore.App.Ref",
		"Microsoft.AspNetCore.App.Ref",
		"Microsoft.WindowsDesktop.App.Ref",
	};

	/// <summary>
	/// Parses a metadata-reference file path matching the .NET ref-pack layout
	/// <c>…/&lt;PackName&gt;/&lt;PackVersion&gt;/ref/&lt;tfm&gt;/&lt;asm&gt;.dll</c>. Both
	/// SDK-installed (<c>&lt;dotnet-root&gt;/packs/…</c>) and NuGet-cached
	/// (<c>~/.nuget/packages/…</c>) layouts share that trailing pattern. Returns
	/// <see langword="false"/> when the path does not match a recognised ref pack.
	/// </summary>
	internal static bool TryParseRefPackPath(
		string path,
		[NotNullWhen(true)] out string? frameworkVersion,
		out string? platformIdentifier,
		out string? platformVersion)
	{
		frameworkVersion = null;
		platformIdentifier = null;
		platformVersion = null;

		var segments = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length < 5)
		{
			return false;
		}

		// segments[^1] = "<asm>.dll"
		// segments[^2] = "<tfm>"      e.g. "net10.0"
		// segments[^3] = "ref"        literal
		// segments[^4] = "<pack-version>"
		// segments[^5] = "<pack-name>"

		if (!"ref".Equals(segments[^3], StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var tfmSegment = segments[^2];
		if (!IsFrameworkVersionSegment(tfmSegment))
		{
			return false;
		}

		var packName = segments[^5];
		if (!TryClassifyPack(packName, out platformIdentifier, out platformVersion))
		{
			return false;
		}

		frameworkVersion = tfmSegment;
		return true;
	}

	private static bool IsFrameworkVersionSegment(string segment)
		=> segment.StartsWith("net", StringComparison.OrdinalIgnoreCase)
			&& segment.Length > 3
			&& char.IsAsciiDigit(segment[3]);

	/// <summary>
	/// Classifies a pack name as one of the known ref packs, optionally extracting platform
	/// information from the prefix / suffix. Returns <see langword="false"/> when the pack
	/// is not recognised; sets both out parameters to <see langword="null"/> for recognised
	/// base packs (no platform info).
	/// </summary>
	private static bool TryClassifyPack(
		string packName,
		out string? platformIdentifier,
		out string? platformVersion)
	{
		platformIdentifier = null;
		platformVersion = null;

		foreach (var (prefix, platform) in _platformPackPrefixes)
		{
			if (string.Equals(packName, prefix, StringComparison.OrdinalIgnoreCase))
			{
				platformIdentifier = platform;
				return true;
			}

			if (packName.Length > prefix.Length + 1
				&& packName[prefix.Length] == '.'
				&& packName.AsSpan(0, prefix.Length).Equals(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
			{
				platformIdentifier = platform;
				platformVersion = TryExtractPlatformVersion(packName[(prefix.Length + 1)..]);
				return true;
			}
		}

		return _basePackNames.Contains(packName);
	}

	/// <summary>
	/// Extracts the platform-version part from the encoded TFM suffix of a platform ref-pack
	/// name (e.g. <c>net10.0_26.0</c> → <c>26.0</c>). Returns <see langword="null"/> when
	/// the suffix has no underscore-separated platform-version (older pack-name format).
	/// </summary>
	private static string? TryExtractPlatformVersion(string suffix)
	{
		var underscoreIdx = suffix.IndexOf('_');
		return underscoreIdx > 0 && underscoreIdx < suffix.Length - 1
			? suffix[(underscoreIdx + 1)..]
			: null;
	}
}
