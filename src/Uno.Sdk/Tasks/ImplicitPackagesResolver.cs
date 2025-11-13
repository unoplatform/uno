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

public sealed class ImplicitPackagesResolver_v0 : Task
{
	private readonly List<string> _existingReferences = [];
	private PackageManifest? _manifest;
	private NuGetVersion? _unoVersion;

	public bool SdkDebugging { get; set; }

	public bool SingleProject { get; set; }

	[Required]
	public string OutputType { get; set; } = null!;

	public bool Optimize { get; set; }

	[Required]
	public string IntermediateOutput { get; set; } = null!;

	public string? UnoFeatures { get; set; }

	[Required]
	public string TargetFramework { get; set; } = null!;

	private string TargetFrameworkVersion = null!;

	private string TargetRuntime = null!;

	[Required]
	public string ProjectName { get; set; } = null!;

	public string? UnoExtensionsVersion { get; set; }

	public string? UnoToolkitVersion { get; set; }

	public string? UnoThemesVersion { get; set; }

	public string? UnoCSharpMarkupVersion { get; set; }

	public string? MauiVersion { get; set; }

	public string? SkiaSharpVersion { get; set; }

	public string? SvgSkiaVersion { get; set; }

	public string? UnoLoggingVersion { get; set; }

	public string? VlcNativeWindowsAssetsVersion { get; set; }

	public string? MicrosoftWebView2Version { get; set; }

	public string? WindowsCompatibilityVersion { get; set; }

	public string? UnoWasmBootstrapVersion { get; set; }

	public string? UnoUniversalImageLoaderVersion { get; set; }

	public string? AndroidMaterialVersion { get; set; }

	public string? AndroidXLegacySupportV4Version { get; set; }

	public string? AndroidXSplashScreenVersion { get; set; }

	public string? AndroidXAppCompatVersion { get; set; }

	public string? AndroidXRecyclerViewVersion { get; set; }

	public string? AndroidXActivityVersion { get; set; }

	public string? AndroidXBrowserVersion { get; set; }

	public string? AndroidXSwipeRefreshLayoutVersion { get; set; }

	public string? UnoResizetizerVersion { get; set; }

	public string? UnoSdkExtrasVersion { get; set; }

	public string? UnoSettingsVersion { get; set; }

	public string? UnoHotDesignVersion { get; set; }

	public string? UnoAppMcpVersion { get; set; }

	public string? MicrosoftLoggingVersion { get; set; }

	public string? WinAppSdkVersion { get; set; }

	public string? WinAppSdkBuildToolsVersion { get; set; }

	public string? UnoCoreLoggingSingletonVersion { get; set; }

	public string? UnoDspTasksVersion { get; set; }

	public string? CommunityToolkitMvvmVersion { get; set; }

	public string? PrismVersion { get; set; }

	public string? UnoFontsVersion { get; set; }

	public string? AndroidXNavigationVersion { get; set; }

	public string? AndroidXCollectionVersion { get; set; }

	public string? MicrosoftIdentityClientVersion { get; set; }

	public ITaskItem[] ImplicitPackageReferences { get; set; } = [];

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

	private UnoFeature[] _unoFeatures = [];
	public sealed override bool Execute()
	{
		try
		{
			if (TargetFramework.Contains('-'))
			{
				var frameworkParts = TargetFramework.Split('-');
				TargetFrameworkVersion = frameworkParts[0];
				var runtime = frameworkParts[1].ToLowerInvariant();
				if (runtime.Contains("windows"))
				{
					TargetRuntime = runtime.StartsWith(UnoTarget.Windows, StringComparison.InvariantCultureIgnoreCase) ? UnoTarget.Windows : UnoTarget.SkiaWpf;
				}
				else
				{
					TargetRuntime = runtime;
				}
			}
			else
			{
				TargetFrameworkVersion = TargetFramework;
				TargetRuntime = UnoTarget.Reference;
				if (ProjectName.EndsWith("Skia.WPF", StringComparison.InvariantCultureIgnoreCase))
				{
					TargetRuntime = UnoTarget.SkiaWpf;
				}
				else if (ProjectName.EndsWith("Skia.Linux.FrameBuffer", StringComparison.InvariantCultureIgnoreCase))
				{
					TargetRuntime = UnoTarget.SkiaLinuxFramebuffer;
				}
			}

			_manifest = new PackageManifest(Log, TargetFrameworkVersion);
			if (NuGetVersion.TryParse(_manifest.UnoVersion, out var unoVersion))
			{
				_unoVersion = unoVersion;
			}
			else
			{
				throw new InvalidOperationException("Unable to parse UnoVersion from the Package Manifest.");
			}

			// This needs to run before we get and Validate the Uno Features
			SetupRuntimePackageManifestUpdates(_manifest);

			_unoFeatures = GetFeatures();
			if (Log.HasLoggedErrors)
			{
				return false;
			}

			foreach (var reference in ImplicitPackageReferences)
			{
				var packageId = reference.ItemSpec;
				var metadata = reference.CloneCustomMetadata()
					.Keys
					.Cast<string>()
					.ToDictionary(x => x, x => reference.GetMetadata(x));
				AddPackage(packageId, metadata);
			}
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);

			if (SdkDebugging)
			{
				Log.LogMessage(MessageImportance.High, ex.ToString());
			}
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
		// This set of updates must not be conditional to features, as those may not be defined yet when the task
		// is invoked early.
		manifest.UpdateManifest(PackageManifest.Group.WasmBootstrap, UnoWasmBootstrapVersion)
			.UpdateManifest(PackageManifest.Group.OSLogging, UnoLoggingVersion)
			.UpdateManifest(PackageManifest.Group.VlcNativeWindowsAssets, VlcNativeWindowsAssetsVersion)
			.UpdateManifest(PackageManifest.Group.MicrosoftWebView2, MicrosoftWebView2Version)
			.UpdateManifest(PackageManifest.Group.CoreLogging, UnoCoreLoggingSingletonVersion)
			.UpdateManifest(PackageManifest.Group.UniversalImageLoading, UnoUniversalImageLoaderVersion)
			.UpdateManifest(PackageManifest.Group.Dsp, UnoDspTasksVersion)
			.UpdateManifest(PackageManifest.Group.Resizetizer, UnoResizetizerVersion)
			.UpdateManifest(PackageManifest.Group.SdkExtras, UnoSdkExtrasVersion)
			.UpdateManifest(PackageManifest.Group.Settings, UnoSettingsVersion)
			.UpdateManifest(PackageManifest.Group.HotDesign, UnoHotDesignVersion)
			.UpdateManifest(PackageManifest.Group.AppMcp, UnoAppMcpVersion)
			.UpdateManifest(PackageManifest.Group.SkiaSharp, SkiaSharpVersion)
			.UpdateManifest(PackageManifest.Group.SvgSkia, SvgSkiaVersion)
			.UpdateManifest(PackageManifest.Group.WinAppSdk, WinAppSdkVersion)
			.UpdateManifest(PackageManifest.Group.WinAppSdkBuildTools, WinAppSdkBuildToolsVersion)
			.UpdateManifest(PackageManifest.Group.MicrosoftLoggingConsole, MicrosoftLoggingVersion)
			.UpdateManifest(PackageManifest.Group.WindowsCompatibility, WindowsCompatibilityVersion)
			.UpdateManifest(PackageManifest.Group.MsalClient, MicrosoftIdentityClientVersion)
			.UpdateManifest(PackageManifest.Group.Mvvm, CommunityToolkitMvvmVersion)
			.UpdateManifest(PackageManifest.Group.Prism, PrismVersion)
			.UpdateManifest(PackageManifest.Group.UnoFonts, UnoFontsVersion)
			.UpdateManifest(PackageManifest.Group.AndroidMaterial, AndroidMaterialVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXLegacySupportV4, AndroidXLegacySupportV4Version)
			.UpdateManifest(PackageManifest.Group.AndroidXSplashScreen, AndroidXSplashScreenVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXAppCompat, AndroidXAppCompatVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXRecyclerView, AndroidXRecyclerViewVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXActivity, AndroidXActivityVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXBrowser, AndroidXBrowserVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXSwipeRefreshLayout, AndroidXSwipeRefreshLayoutVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXNavigation, AndroidXNavigationVersion)
			.UpdateManifest(PackageManifest.Group.AndroidXCollection, AndroidXCollectionVersion)
			.UpdateManifest(PackageManifest.Group.CSharpMarkup, UnoCSharpMarkupVersion)
			.UpdateManifest(PackageManifest.Group.Extensions, UnoExtensionsVersion)
			.UpdateManifest(PackageManifest.Group.Toolkit, UnoToolkitVersion)
			.UpdateManifest(PackageManifest.Group.Themes, UnoThemesVersion)
			.UpdateManifest(PackageManifest.Group.Maui, MauiVersion);
	}

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

		if (_manifest is null)
			throw new ArgumentNullException(nameof(_manifest));

		switch (area)
		{
			case UnoArea.Core:
				VerifyFeature(feature, _manifest.UnoVersion);
				break;
			case UnoArea.CSharpMarkup:
				VerifyFeature(feature, _manifest.GetGroupVersion(PackageManifest.Group.CSharpMarkup));
				break;
			case UnoArea.Extensions:
				VerifyFeature(feature, _manifest.GetGroupVersion(PackageManifest.Group.Extensions));
				break;
			case UnoArea.Theme:
				VerifyFeature(feature, _manifest.GetGroupVersion(PackageManifest.Group.Themes));
				break;
			case UnoArea.Toolkit:
				VerifyFeature(feature, _manifest.GetGroupVersion(PackageManifest.Group.Toolkit));
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

	private void AddPackage(string packageId, IDictionary<string, string> metadata)
	{
		// 1) Check for Existing References
		var existingReference = PackageReferences.SingleOrDefault(x => x.ItemSpec == packageId);
		if (existingReference is not null)
		{
			// 1.1) Validate it has a version available
			if (PackageVersions.Any(x => x.ItemSpec == existingReference.ItemSpec) || !string.IsNullOrEmpty(existingReference.GetMetadata("Version"))
				|| !string.IsNullOrEmpty(existingReference.GetMetadata("VersionOverride")))
			{
				// 1.2) Add the PackageId to the ExistingReferences so that we can log a warning at the end.
				_existingReferences.Add(packageId);
				return;
			}

			Log.LogWarning("The Package '{0}' has an existing PackageReference with no Version attribute or associated PackageVersion. The Uno.Sdk is removing this and adding an implicit reference.", packageId);
			return;
		}

		// 2) Load the Version from the PackageManifest. This will get the version whether it was set through MSBuild or the bundled packages.json
		var version = _manifest!.GetPackageVersion(packageId);

		// 3) Validate the version has a value. If not attempt to get the latest version from NuGet.org
		if (string.IsNullOrEmpty(version))
		{
			Log.LogWarning("The package '{0}' has no available version.", packageId);
			using var client = new NuGetApiClient();
			var isUnoPreview = _unoVersion?.IsPreview ?? false;
			var preview = packageId.StartsWith("Uno.", StringComparison.InvariantCulture) && isUnoPreview;
			version = client.GetVersion(packageId, preview);
			Log.LogMessage(MessageImportance.High, "Retrieved the latest package version '{0}' for the package '{1}'.", version, packageId);
		}

		if (version is null || string.IsNullOrEmpty(version))
		{
			Debug("Unable to locate package version for '{0}'.", packageId);
			return;
		}

		// 4) Ensure there is not already an existing Implicit Reference that was added (this shouldn't happen)
		var existing = _implicitPackages.SingleOrDefault(x => x.PackageId == packageId);
		if (existing is not null)
		{
			Debug("An existing Implicit Package reference has already been added for '{0}'.", packageId);
			return;
		}

		// 5) Add the Implicit Package Reference
		Debug("Adding Implicit Reference for '{0}' with version: '{1}'.", packageId, version);
		_implicitPackages.Add(new PackageReference(packageId, version, metadata));
	}

	private void Debug(string message, params object[] args)
	{
		var importantance = SdkDebugging ? MessageImportance.High : MessageImportance.Low;

		Log.LogMessage(importantance, message, args);
	}
}
