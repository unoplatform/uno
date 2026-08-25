#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Mono.Cecil;

namespace Uno.UI.Tasks.RuntimeAssetsValidator;
/// <summary>
/// Validates that the assemblies flowing into the output are compatible with the Uno Platform runtime in use.
/// </summary>
public class RuntimeAssetsValidatorTask_v0 : Microsoft.Build.Utilities.Task
{
	private const string UnoUIRuntimeIdentifierKey = "UnoUIRuntimeIdentifier";
	private const string UnoUIAssemblyName = "Uno.UI";

	/// <summary>
	/// Maximum number of missing type names listed in a single UNOB0020 warning.
	/// </summary>
	private const int MaxReportedMissingTypes = 5;

	private const int MaxReportedTypeNameLength = 200;

	[Required]
	public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsInput { get; set; }

	public Microsoft.Build.Framework.ITaskItem[]? ResolvedCompileFileDefinitionsInput { get; set; }

	public string NuGetPackageRoot { get; set; } = "";

	public string UnoRuntimeIdentifier { get; set; } = "";

	public string UnoUIRuntimeIdentifier { get; set; } = "";

	public string UnoWinRTRuntimeIdentifier { get; set; } = "";

	/// <summary>
	/// The platform of the target framework being built, e.g. "android". Deliberately not the runtime identifiers:
	/// those are only set by the runtime packages, which class libraries do not always reference.
	/// </summary>
	public string TargetPlatformIdentifier { get; set; } = "";

	public bool DisablePlatformAssetValidation { get; set; }

	public override bool Execute()
	{
		bool succeeded = true;

		try
		{
			if (UnoRuntimeIdentifier == "reference")
			{
				return true;
			}

			// The assemblies of a project are all compiled against a single UI runtime. Two-layer heads name it
			// through UnoUIRuntimeIdentifier, single-layer heads through UnoRuntimeIdentifier.
			var expectedUIRuntimeIdentifier = string.IsNullOrEmpty(UnoUIRuntimeIdentifier)
				? UnoRuntimeIdentifier
				: UnoUIRuntimeIdentifier;

			var platformAssetValidator = CreatePlatformAssetValidator();

			foreach (var assembly in RuntimeCopyLocalItemsInput ?? [])
			{
				var assemblyPath = assembly.GetMetadata("FullPath");

				if (UnoUIAssemblyName.Equals(Path.GetFileNameWithoutExtension(assemblyPath), StringComparison.OrdinalIgnoreCase))
				{
					// Uno.UI is the oracle, not a candidate, and it is the largest file we would open.
					continue;
				}

				AssemblyDefinition originalAssembly;
				try
				{
					originalAssembly = AssemblyDefinition.ReadAssembly(assemblyPath);
				}
				catch (Exception e)
				{
					// A package may ship a native or malformed file; that is not this task's problem to report.
					this.Log.LogMessage(MessageImportance.Low, $"Skipping '{assemblyPath}', it could not be read ({e.Message}).");
					continue;
				}

				using var _ = originalAssembly;

				if (!originalAssembly.MainModule.AssemblyReferences.Any(m => m.Name == UnoUIAssemblyName))
				{
					// We only need to validate assemblies that reference Uno.UI, because this is the only layer
					// that is replaced for the Skia UI layer
					this.Log.LogMessage(MessageImportance.Low, $"Skipping {originalAssembly} validation");
					continue;
				}

				if (GetAssemblyMetadata(originalAssembly, UnoUIRuntimeIdentifierKey) is { Length: > 0 } identifier
					&& !expectedUIRuntimeIdentifier.Equals(identifier, StringComparison.OrdinalIgnoreCase))
				{
					succeeded = false;

					Log.LogError(
						$"The assembly {assembly.ItemSpec} has a different UnoUIRuntimeIdentifier than the one used to build the project. " +
						$"(Expected: {expectedUIRuntimeIdentifier}, Actual: {identifier})"
					);
				}

				platformAssetValidator?.Validate(originalAssembly, assemblyPath);
			}

			return succeeded;
		}
		catch (Exception e)
		{
			// Require because the task is running out of process
			// and can't marshal non-CLR known exceptions.
			throw new Exception(e.ToString());
		}
	}

	private static string? GetAssemblyMetadata(AssemblyDefinition assembly, string key)
	{
		if (!assembly.HasCustomAttributes)
		{
			return null;
		}

		// AssemblyMetadataAttribute allows multiple, and the SDK emits its own (RepositoryUrl, IsTrimmable, ...),
		// so the key has to be part of the match rather than checked on whichever attribute happens to come first.
		foreach (var attribute in assembly.CustomAttributes)
		{
			if (attribute.AttributeType.FullName == "System.Reflection.AssemblyMetadataAttribute"
				&& attribute.HasConstructorArguments
				&& attribute.ConstructorArguments.Count == 2
				&& string.Equals(attribute.ConstructorArguments[0].Value?.ToString(), key, StringComparison.OrdinalIgnoreCase))
			{
				return attribute.ConstructorArguments[1].Value?.ToString();
			}
		}

		return null;
	}

	private PlatformAssetValidator? CreatePlatformAssetValidator()
	{
		// Every reason to skip is logged: a silent no-op is indistinguishable from a clean run in a binlog.
		if (DisablePlatformAssetValidation)
		{
			this.Log.LogMessage(MessageImportance.Normal, "Skipping UNOB0020, disabled through UnoDisableUNOB0020Validation.");
			return null;
		}

		if (TargetPlatformIdentifier.ToLower(CultureInfo.InvariantCulture) is not ("android" or "ios" or "tvos"))
		{
			this.Log.LogMessage(MessageImportance.Low, $"Skipping UNOB0020, '{TargetPlatformIdentifier}' carries no platform-specific package assets.");
			return null;
		}

		var nugetPackageRoot = NormalizeNuGetPackageRoot();
		if (nugetPackageRoot.Length == 0)
		{
			this.Log.LogMessage(MessageImportance.Normal, "Skipping UNOB0020, NuGetPackageRoot is not set.");
			return null;
		}

		if (FindUnoUIAssemblyPath() is not { } unoUIPath)
		{
			// Uno.UI comes from a ProjectReference (in-repo heads), so there is nothing to compare against.
			this.Log.LogMessage(MessageImportance.Normal, "Skipping UNOB0020, Uno.UI could not be resolved from the compile references.");
			return null;
		}

		this.Log.LogMessage(MessageImportance.Normal, $"Validating platform-specific package assets against '{unoUIPath}'.");

		return new PlatformAssetValidator(Log, unoUIPath, nugetPackageRoot);
	}

	private string? FindUnoUIAssemblyPath()
		=> (ResolvedCompileFileDefinitionsInput ?? [])
			.Select(item => item.GetMetadata("FullPath"))
			.FirstOrDefault(path =>
				UnoUIAssemblyName.Equals(Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase));

	private string NormalizeNuGetPackageRoot()
	{
		var root = NuGetPackageRoot.Replace('\\', '/');
		return root.Length == 0 || root.EndsWith("/", StringComparison.Ordinal) ? root : root + "/";
	}

	/// <summary>
	/// Reports assemblies whose platform-specific asset was built against an Uno.UI that no longer provides the
	/// types they reference. Those assets used to be silently replaced by the package's platform-neutral asset.
	/// </summary>
	private sealed class PlatformAssetValidator
	{
		private readonly Microsoft.Build.Utilities.TaskLoggingHelper _log;
		private readonly string _unoUIPath;
		private readonly string _nugetCacheRoot;
		private HashSet<string>? _unoUITypes;

		public PlatformAssetValidator(Microsoft.Build.Utilities.TaskLoggingHelper log, string unoUIPath, string nugetCacheRoot)
		{
			_log = log;
			_unoUIPath = unoUIPath;
			_nugetCacheRoot = nugetCacheRoot;
		}

		public void Validate(AssemblyDefinition assembly, string assemblyPath)
		{
			if (GetPlatformSpecificPackageAsset(assemblyPath) is not { } asset)
			{
				return;
			}

			var knownTypes = GetUnoUITypes();
			var missingTypes = new HashSet<string>(StringComparer.Ordinal);

			foreach (var typeReference in assembly.MainModule.GetTypeReferences())
			{
				if (typeReference.Scope is AssemblyNameReference { Name: UnoUIAssemblyName }
					&& !knownTypes.Contains(typeReference.FullName))
				{
					missingTypes.Add(typeReference.FullName);
				}
			}

			if (missingTypes.Count == 0)
			{
				return;
			}

			// The full list only reaches the log; the warning itself stays readable.
			_log.LogMessage(
				MessageImportance.Low,
				$"'{assemblyPath}' references {missingTypes.Count} missing Uno.UI type(s): {string.Join(", ", missingTypes.OrderBy(name => name, StringComparer.Ordinal))}");

			var displayed = missingTypes
				.OrderBy(name => name, StringComparer.Ordinal)
				.Take(MaxReportedMissingTypes)
				.Select(Sanitize);

			var reported = string.Join(", ", displayed);
			if (missingTypes.Count > MaxReportedMissingTypes)
			{
				reported += $" and {missingTypes.Count - MaxReportedMissingTypes} more";
			}

			_log.LogWarning(
				subcategory: null,
				warningCode: "UNOB0020",
				helpKeyword: null,
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: "The '{0}' asset of package '{1}' {2} references Uno Platform types that no longer exist: {3}. " +
					"It was built against an earlier version of Uno Platform and will fail at runtime. Update the package to a " +
					"version built for this release. https://aka.platform.uno/UNOB0020",
				messageArgs: [$"lib/{asset.TargetFramework}/{Path.GetFileName(assemblyPath)}", asset.PackageId, asset.PackageVersion, reported]);
		}

		/// <summary>
		/// Reading Uno.UI materializes its whole type table, so the result is shared across every project built by
		/// this MSBuild node rather than rebuilt per project.
		/// </summary>
		private static readonly ConcurrentDictionary<(string Path, DateTime Written), HashSet<string>> _unoUITypeCache = new();

		private HashSet<string> GetUnoUITypes()
			=> _unoUITypes ??= _unoUITypeCache.GetOrAdd((_unoUIPath, File.GetLastWriteTimeUtc(_unoUIPath)), static key => ReadTypeNames(key.Path));

		private static HashSet<string> ReadTypeNames(string path)
		{
			using var unoUI = AssemblyDefinition.ReadAssembly(path);
			var names = new HashSet<string>(StringComparer.Ordinal);

			foreach (var type in unoUI.MainModule.GetTypes())
			{
				names.Add(type.FullName);
			}

			// Type forwards are as good as declarations for a consumer's type references.
			foreach (var exportedType in unoUI.MainModule.ExportedTypes)
			{
				names.Add(exportedType.FullName);
			}

			return names;
		}

		/// <summary>
		/// Type names come from a foreign assembly's string heap, which allows control characters.
		/// </summary>
		private static string Sanitize(string name)
		{
			var builder = new StringBuilder(name.Length);

			foreach (var c in name.Length > MaxReportedTypeNameLength ? name.Substring(0, MaxReportedTypeNameLength) : name)
			{
				builder.Append(c < ' ' || c == '' ? '?' : c);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Matches &lt;NuGetPackageRoot&gt;/&lt;package&gt;/&lt;version&gt;/lib/&lt;tfm&gt;-[android|ios|tvos]/&lt;assembly&gt;.dll.
		/// </summary>
		private (string PackageId, string PackageVersion, string TargetFramework)? GetPlatformSpecificPackageAsset(string assemblyPath)
		{
			if (_nugetCacheRoot.Length == 0)
			{
				return null;
			}

			var normalized = Path.GetFullPath(assemblyPath).Replace('\\', '/');
			if (!normalized.StartsWith(_nugetCacheRoot, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var segments = normalized.Substring(_nugetCacheRoot.Length).Split('/');
			if (segments.Length != 5 || segments[2] != "lib")
			{
				return null;
			}

			var targetFramework = segments[3];
			var dashIndex = targetFramework.IndexOf('-');
			if (dashIndex < 0)
			{
				return null;
			}

			var platform = targetFramework.Substring(dashIndex + 1).ToLower(CultureInfo.InvariantCulture);
			if (!platform.StartsWith("android", StringComparison.Ordinal)
				&& !platform.StartsWith("ios", StringComparison.Ordinal)
				&& !platform.StartsWith("tvos", StringComparison.Ordinal))
			{
				return null;
			}

			return (segments[0], segments[1], targetFramework);
		}
	}
}
