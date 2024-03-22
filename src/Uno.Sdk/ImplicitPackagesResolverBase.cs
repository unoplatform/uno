using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk;

public abstract class ImplicitPackagesResolverBase : Task
{
	private static readonly string[] _legacyWasmProjectSuffix = [".Wasm", ".WebAssembly"];
	private List<string> _existingReferences = [];

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

	public string UnoExtensionsVersion { get; set; }

	public string UnoToolkitVersion { get; set; }

	public string UnoThemesVersion { get; set; }

	public string UnoCSharpMarkupVersion { get; set; }

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
			if (Log.HasLoggedErrors)
			{
				return false;
			}

			var cachedReferences = CachedReferences.Load(IntermediateOutput);
			if (cachedReferences.NeedsUpdate(_unoFeatures, UnoVersion))
			{
				ExecuteInternal();
				cachedReferences = new CachedReferences(DateTimeOffset.Now, _unoFeatures, [.. _implicitPackages]);
				cachedReferences.SaveCache(IntermediateOutput);
			}
			else
			{
				Debug("Adding ({0}) Packages from cache file.", cachedReferences.References.Length);
				_implicitPackages.AddRange(cachedReferences.References);
			}
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);
		}

		if (_existingReferences.Count > 0)
		{
			var builder = new StringBuilder();
			builder.AppendLine("Uno Platform Implicit Package references are enabled, you should remove these references from your csproj:");
			_existingReferences.Select(x => $"\t<PackageReference Include=\"{x}\" />")
				.ToList()
				.ForEach(x => builder.AppendLine(x));
			builder.AppendLine("See https://aka.platform.uno/UNOB0009 for more information.");
			Log.LogMessage(subcategory: null,
				code: "UNOB0009",
				helpKeyword: null,
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				MessageImportance.Normal,
				message: builder.ToString());
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
			ValidateFeature(unoFeature);
			return unoFeature;
		}

		Log.LogWarning($"Unable to parse '{feature}' to a known Uno Feature.");
		return UnoFeature.Invalid;
	}

	public void ValidateFeature(UnoFeature feature)
	{
		var area = typeof(UnoFeature).GetMember(feature.ToString())
			.Single(x => x.DeclaringType == typeof(UnoFeature))
			.GetCustomAttribute<UnoAreaAttribute>()?.Area;

		switch (area)
		{
			case UnoArea.Core:
				VerifyFeature(feature, UnoVersion);
				break;
			case UnoArea.CSharpMarkup:
				VerifyFeature(feature, UnoCSharpMarkupVersion);
				break;
			case UnoArea.Extensions:
				VerifyFeature(feature, UnoExtensionsVersion);
				break;
			case UnoArea.Theme:
				VerifyFeature(feature, UnoThemesVersion);
				break;
			case UnoArea.Toolkit:
				VerifyFeature(feature, UnoToolkitVersion);
				break;
		}
	}

	private void VerifyFeature(UnoFeature feature, string version, [CallerArgumentExpression(nameof(version))] string versionName = null)
	{
		if (string.IsNullOrEmpty(version))
		{
			Log.LogError(subcategory: "",
				errorCode: "UNOB0006",
				helpKeyword: null,
				helpLink: "https://aka.platform.uno/UNOB0006",
				file: null,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: $"The UnoFeature '{feature}' was selected, but the property {versionName} was not set.");
		}
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
			_existingReferences.Add(packageId);
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
