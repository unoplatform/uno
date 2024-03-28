using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Uno.Sdk.Models;
using Uno.Sdk.Services;

namespace Uno.Sdk.Tasks;

public abstract class ImplicitPackagesResolverBase : Task
{
	private static readonly string[] _legacyWasmProjectSuffix = [".Wasm", ".WebAssembly"];
	private readonly List<string> _existingReferences = [];
	private PackageManifest? _manifest;

	public bool SdkDebugging { get; set; }

	public bool SingleProject { get; set; }

	[Required]
	public string OutputType { get; set; } = null!;

	public bool IsPackable { get; set; }

	protected bool IsExecutable => !string.IsNullOrEmpty(OutputType) && OutputType.ToLowerInvariant().Contains("exe");

	public bool Optimize { get; set; }

	[Required]
	public string IntermediateOutput { get; set; } = null!;

	public string? UnoFeatures { get; set; }

	[Required]
	public string TargetFramework { get; set; } = null!;

	protected string TargetFrameworkVersion { get; private set; } = null!;

	protected string TargetRuntime { get; private set; } = null!;

	[Required]
	public string ProjectName { get; set; } = null!;

	public string? UnoExtensionsVersion { get; set; }

	public string? UnoToolkitVersion { get; set; }

	public string? UnoThemesVersion { get; set; }

	public string? UnoCSharpMarkupVersion { get; set; }

	public string? MauiVersion { get; set; }

	public string? SkiaSharpVersion { get; set; }

	public string? UnoLoggingVersion { get; set; }

	public string? WindowsCompatibilityVersion { get; set; }

	public string? UnoWasmBootstrapVersion { get; set; }

	public string? UnoUniversalImageLoaderVersion { get; set; }

	public string? AndroidMaterialVersion { get; set; }

	public string? AndroidXLegacySupportV4Version { get; set; }

	public string? AndroidXAppCompatVersion { get; set; }

	public string? AndroidXRecyclerViewVersion { get; set; }

	public string? AndroidXActivityVersion { get; set; }

	public string? AndroidXBrowserVersion { get; set; }

	public string? AndroidXSwipeRefreshLayoutVersion { get; set; }

	public string? UnoResizetizerVersion { get; set; }

	public string? MicrosoftLoggingVersion { get; set; }

	public string? WinAppSdkVersion { get; set; }

	public string? WinAppSdkBuildToolsVersion { get; set; }

	public string? UnoCoreLoggingSingletonVersion { get; set; }

	public string? UnoDspTasksVersion { get; set; }

	public string? CommunityToolkitMvvmVersion { get; set; }

	public string? PrismVersion { get; set; }

	public string? AndroidXNavigationVersion { get; set; }

	public string? AndroidXCollectionVersion { get; set; }

	public string? MicrosoftIdentityClientVersion { get; set; }

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
			_manifest = new PackageManifest(Log);

			if (TargetFramework.Contains('-'))
			{
				var frameworkParts = TargetFramework.Split('-');
				TargetFrameworkVersion = frameworkParts[0];
				TargetRuntime = frameworkParts[1].StartsWith(UnoTarget.Windows, StringComparison.InvariantCultureIgnoreCase) ? UnoTarget.Windows : frameworkParts[1].ToLower(CultureInfo.InvariantCulture);
			}
			else
			{
				TargetFrameworkVersion = TargetFramework;
				TargetRuntime = UnoTarget.Reference;
			}

			_unoFeatures = GetFeatures();
			if (Log.HasLoggedErrors)
			{
				return false;
			}

			// TODO: Add update from Manifest that can ship via nuget.org
			SetupRuntimePackageManifestUpdates(_manifest);
			var cachedReferences = CachedReferences.Load(IntermediateOutput);
			if (cachedReferences.NeedsUpdate(_unoFeatures, _manifest))
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

#if DEBUG
		var missingImplicitPackages = PackageReferences.Where(x => !string.IsNullOrEmpty(x.GetMetadata("ProjectSystem"))
			&& !_implicitPackages.Any(p => p.PackageId == x.ItemSpec))
			.ToArray();

		if (missingImplicitPackages.Length > 0)
		{
			System.Diagnostics.Debugger.Launch();
		}
#endif

		return !Log.HasLoggedErrors;
	}

	private void SetupRuntimePackageManifestUpdates(PackageManifest manifest)
	{
		// Checks any MSBuild parameters passed to the task to override the default versions from the bundled packages.json
		manifest.UpdateManifest(PackageManifest.Group.WasmBootstrap, UnoWasmBootstrapVersion)
			.UpdateManifest(PackageManifest.Group.OSLogging, UnoLoggingVersion)
			.UpdateManifest(PackageManifest.Group.CoreLogging, UnoCoreLoggingSingletonVersion)
			.UpdateManifest(PackageManifest.Group.UniversalImageLoading, UnoUniversalImageLoaderVersion)
			.UpdateManifest(PackageManifest.Group.Dsp, UnoDspTasksVersion)
			.UpdateManifest(PackageManifest.Group.Resizetizer, UnoResizetizerVersion)
			.UpdateManifest(PackageManifest.Group.SkiaSharp, SkiaSharpVersion)
			.UpdateManifest(PackageManifest.Group.WinAppSdk, WinAppSdkVersion)
			.UpdateManifest(PackageManifest.Group.WinAppSdkBuildTools, WinAppSdkBuildToolsVersion)
			.UpdateManifest(PackageManifest.Group.MicrosoftLoggingConsole, MicrosoftLoggingVersion)
			.UpdateManifest(PackageManifest.Group.WindowsCompatibility, WindowsCompatibilityVersion)
			.UpdateManifest(PackageManifest.Group.MsalClient, MicrosoftIdentityClientVersion)
			.UpdateManifest(PackageManifest.Group.Mvvm, CommunityToolkitMvvmVersion)
			.UpdateManifest(PackageManifest.Group.Prism, PrismVersion)
			.UpdateManifest(PackageManifest.Group.AndroidMaterial, AndroidMaterialVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXLegacySupportV4, AndroidXLegacySupportV4Version)
			.UpdateManifest(PackageManifest.Group.AndroidXAppCompat, AndroidXAppCompatVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXRecyclerView, AndroidXRecyclerViewVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXActivity, AndroidXActivityVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXBrowser, AndroidXBrowserVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXSwipeRefreshLayout, AndroidXSwipeRefreshLayoutVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXNavigation, AndroidXNavigationVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXCollection, AndroidXCollectionVersion);

		if (HasFeature(UnoFeature.MauiEmbedding))
		{
			manifest.AddManifestGroup(PackageManifest.Group.Maui, MauiVersion,
				"Microsoft.Maui.Controls",
				"Microsoft.Maui.Controls.Compatibility",
				"Microsoft.Maui.Graphics");
		}

		manifest.AddManifestGroup(PackageManifest.Group.CSharpMarkup, UnoCSharpMarkupVersion,
				"Uno.WinUI.Markup",
				"Uno.Extensions.Markup.Generators")
			.AddManifestGroup(PackageManifest.Group.Extensions, UnoExtensionsVersion,
				"Uno.Extensions.Authentication.WinUI",
				"Uno.Extensions.Authentication.MSAL.WinUI",
				"Uno.Extensions.Authentication.Oidc.WinUI",
				"Uno.Extensions.Configuration",
				"Uno.Extensions.Core.WinUI",
				"Uno.Extensions.Hosting.WinUI",
				"Uno.Extensions.Http.WinUI",
				"Uno.Extensions.Http.Refit",
				"Uno.Extensions.Localization.WinUI",
				"Uno.Extensions.Logging.WinUI",
				"Uno.Extensions.Maui.WinUI",
				"Uno.Extensions.Maui.WinUI.Markup",
				"Uno.Extensions.Navigation.WinUI",
				"Uno.Extensions.Navigation.WinUI.Markup",
				"Uno.Extensions.Navigation.Toolkit.WinUI",
				"Uno.Extensions.Reactive.WinUI",
				"Uno.Extensions.Reactive.Messaging",
				"Uno.Extensions.Reactive.WinUI.Markup",
				"Uno.Extensions.Serialization.Http",
				"Uno.Extensions.Serialization.Refit",
				"Uno.Extensions.Logging.Serilog",
				"Uno.Extensions.Storage.WinUI")
			.AddManifestGroup(PackageManifest.Group.Toolkit, UnoToolkitVersion,
				"Uno.Toolkit.WinUI",
				"Uno.Toolkit.WinUI.Cupertino",
				"Uno.Toolkit.WinUI.Material",
				"Uno.Toolkit.WinUI.Material.Markup",
				"Uno.Toolkit.WinUI.Markup",
				"Uno.Toolkit.Skia.WinUI")
			.AddManifestGroup(PackageManifest.Group.Themes, UnoThemesVersion,
				"Uno.Material.WinUI",
				"Uno.Material.WinUI.Markup",
				"Uno.Themes.WinUI.Markup",
				"Uno.Cupertino.WinUI");
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

		Debug("Found {0} UnoFeatures for platform {1}.", unoFeatures.Length, TargetFramework ?? "Default");
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
				VerifyFeature(feature, _manifest!.UnoVersion);
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

	private void VerifyFeature(UnoFeature feature, string? version, [CallerArgumentExpression(nameof(version))] string? versionName = null)
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
		if (string.IsNullOrEmpty(ProjectName))
		{
			Debug("The ProjectName has no value.");
			return false;
		}

		var isLegacyProject = !SingleProject && TargetRuntime == UnoTarget.Reference
			&& _legacyWasmProjectSuffix.Any(x => ProjectName.EndsWith(x, StringComparison.InvariantCulture));

		if (isLegacyProject)
		{
			Debug("Building a Legacy WASM project.");
		}

		return isLegacyProject;
	}

	protected void AddPackageForFeature(UnoFeature feature, string packageId, string? version)
	{
		if (HasFeature(feature))
		{
			Debug("Adding '{0}' for the feature: {1}", packageId, feature);
			AddPackage(packageId, version);
		}
	}

	protected void AddPackageForFeatureWhen(bool condition, UnoFeature feature, string packageId, string? version)
	{
		if (condition)
		{
			AddPackageForFeature(feature, packageId, version);
		}
	}

	protected void AddPackageWhen(bool condition, string packageId, string? version, string? excludeAssets = null)
	{
		if (condition)
		{
			AddPackage(packageId, version, excludeAssets);
		}
	}

	protected void AddPackage(string packageId, string? version, string? excludeAssets = null)
	{
		version = _manifest!.GetPackageVersion(packageId, TargetFrameworkVersion, version);

		if (string.IsNullOrEmpty(version))
		{
			Log.LogWarning("The package '{0}' has no available version.", packageId);
			using var client = new NuGetApiClient();
			var preview = packageId.StartsWith("Uno.", StringComparison.InvariantCulture) && new NuGetVersion(_manifest.UnoVersion).IsPreview;
			version = client.GetVersion(packageId, preview);
			Log.LogMessage(MessageImportance.High, "Retrieved the latest package version '{0}' for the package '{1}'.", version, packageId);
		}

		if (version is null || string.IsNullOrEmpty(version))
		{
			Debug("Unable to locate package version for '{0}'.", packageId);
			return;
		}

		Debug("Attempting to add package '{0}' with version '{1}' for platform ({2}).", packageId, version, TargetFramework);

		if (PackageReferences.Any(x => x.ItemSpec == packageId && string.IsNullOrEmpty(x.GetMetadata("ProjectSystem"))))
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

	protected void Debug(string message, params object[] args)
	{
		var importantance = SdkDebugging ? MessageImportance.High : MessageImportance.Low;

		Log.LogMessage(importantance, message, args);
	}
}
