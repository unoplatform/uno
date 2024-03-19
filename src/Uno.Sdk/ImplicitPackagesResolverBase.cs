using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk;

public abstract class ImplicitPackagesResolverBase : Task
{
	private static readonly string[] _legacyWasmProjectSuffix = [".Wasm", ".WebAssembly"];
	public bool SdkDebugging { get; set; }

	public bool SingleProject { get; set; }

	public string OutputType { get; set; }

	protected bool IsExecutable => !string.IsNullOrEmpty(OutputType) && OutputType.ToLowerInvariant().Contains("exe");

	public bool Optimize { get; set; }

	[Required]
	public string IntermediateOutput { get; set; }

	public string UnoFeatures { get; set; }

	public string TargetFrameworkIdentifier { get; set; }

	public string ProjectName { get; set; }

	[Required]
	public string UnoVersion { get; set; }

	public ITaskItem[] PackageReferences { get; set; } = [];

	public ITaskItem[] PackageVersions { get; set; } = [];

	private readonly List<PackageReference> _implicitPackages = [];
	[Output]
	public ITaskItem[] ImplicitPackages => [.. _implicitPackages.Distinct()
		.Select(x => x.ToTaskItem())];

	[Output]
	public ITaskItem[] RemovePackageVersions =>
		PackageVersions.Where(x =>
			_implicitPackages.Any(ip => ip.PackageId == x.ItemSpec)).ToArray();

	protected abstract void ExecuteInternal();

	private UnoFeature[] _unoFeatures = [];
	public sealed override bool Execute()
	{
		try
		{
			_unoFeatures = GetFeatures();
			var references = CachedReferences.Load(IntermediateOutput);
			if (references.NeedsUpdate(_unoFeatures))
			{
				ExecuteInternal();
				references = new CachedReferences(DateTimeOffset.Now, _unoFeatures, [.. _implicitPackages]);
				references.SaveCache(IntermediateOutput);
			}
			else
			{
				Debug("Adding ({0}) Packages from cache file.", references.References.Length);
				_implicitPackages.AddRange(references.References);
			}
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);
		}

		return !Log.HasLoggedErrors;
	}

	protected bool HasFeature(UnoFeature feature) =>
		_unoFeatures.Any(x => x == feature);

	protected bool HasFeatures(params UnoFeature[] features) =>
		features.All(f => _unoFeatures.Any(x => x == f));

	private UnoFeature[] GetFeatures()
	{
		if (string.IsNullOrEmpty(UnoFeatures))
		{
			Debug("UnoFeatures was provided as an empty or null value.");
			return [];
		}

		var features = Regex.Replace(UnoFeatures, @"\s", string.Empty)
			.Replace(',', ';');
		if (string.IsNullOrEmpty(features))
		{
			Debug("No UnoFeatures were provided.");
			return [];
		}

		var unoFeatures = features.Split(';')
			.Select(x => x.Trim()) // sanity check
			.Where(x => !string.IsNullOrEmpty(x))
			.Distinct()
			.Select(ParseFeature)
			.Where(x => x != UnoFeature.Invalid)
			.ToArray();

		Debug("Found {0} UnoFeatures for platform {1}.", unoFeatures.Length, TargetFrameworkIdentifier ?? "Default");
		return unoFeatures;
	}

	private UnoFeature ParseFeature(string feature)
	{
		if (Enum.TryParse<UnoFeature>(feature, true, out var unoFeature))
		{
			Debug("Parsed UnoFeature: '{0}'.", feature);
			return unoFeature;
		}

		Log.LogWarning($"Unable to parse '{feature}' to a known Uno Feature.");
		return UnoFeature.Invalid;
	}

	protected bool IsLegacyWasmHead()
	{
		// Neither of these should ever actually happen...
		if (string.IsNullOrEmpty(TargetFrameworkIdentifier))
		{
			Debug("The TargetFrameworkIdentifier has no value.");
			return false;
		}
		else if (string.IsNullOrEmpty(ProjectName))
		{
			Debug("The ProjectName has no value.");
			return false;
		}

		var isLegacyProject = !SingleProject && TargetFrameworkIdentifier == UnoTarget.Reference
			&& _legacyWasmProjectSuffix.Any(x => ProjectName.EndsWith(x, StringComparison.InvariantCulture));

		if (isLegacyProject)
		{
			Debug("Building a Legacy WASM project.");
		}

		return isLegacyProject;
	}

	protected void AddPackageForFeature(UnoFeature feature, string packageId, string packageVersion)
	{
		if (HasFeature(feature))
		{
			Debug("Adding '{0}' for the feature: {1}", packageId, feature);
			AddPackage(packageId, packageVersion);
		}
	}

	protected void AddPackage(string packageId, string version, string excludeAssets = null)
	{
		Debug("Attempting to add package '{0}' with version '{1}' for platform ({2}).", packageId, version, TargetFrameworkIdentifier);
		if (string.IsNullOrEmpty(version))
		{
			Log.LogWarning("The package '{0}' has no available version.", packageId);
			using var client = new NuGetClient();
			var preview = packageId.StartsWith("Uno.", StringComparison.InvariantCulture) && new NuGetVersion(UnoVersion).IsPreview;
			version = client.GetVersion(packageId, preview);
			Log.LogMessage(MessageImportance.High, "Retrieved the latest package version '{0}' for the package '{1}'.", version, packageId);
		}

		if (PackageReferences.Any(x => x.ItemSpec == packageId))
		{
			Log.LogMessage(MessageImportance.High, "Uno Implicit PackageReferences are enabled, however you have an explicit reference to '{0}'. Please remove the PackageReference.", packageId);
			return;
		}

		var existing = _implicitPackages.SingleOrDefault(x => x.PackageId == packageId);
		if (existing is not null)
		{
			Debug("An existing Implicit Package reference has already been added for '{0}'.", packageId);
			return;
		}

		Debug("Adding Implicit Reference for '{0}' with version: '{1}'.", packageId, version);
		_implicitPackages.Add(new PackageReference(packageId, version, excludeAssets));
	}

	private void Debug(string message, params object[] args)
	{
		if (!SdkDebugging)
		{
			return;
		}

		Log.LogMessage(MessageImportance.High, message, args);
	}
}
