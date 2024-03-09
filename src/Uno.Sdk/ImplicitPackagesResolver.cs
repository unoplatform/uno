using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk;

public sealed class ImplicitPackagesResolver : Task
{
	public bool SingleProject { get; set; }

	public bool Optimize { get; set; }

	public string UnoFeatures { get; set; }

	public string TargetFrameworkIdentifier { get; set; }

	public string ProjectName { get; set; }

	public ITaskItem[] PackageReferences { get; set; } = [];

	public ITaskItem[] PackageVersions { get; set; } = [];

	[Required]
	public string UnoVersion { get; set; }

	public string MauiVersion { get; set; }

	public string UnoExtensionsVersion { get; set; }

	public string UnoToolkitVersion { get; set; }

	public string UnoThemesVersion { get; set; }

	public string UnoCSharpMarkupVersion { get; set; }

	public string SkiaSharpVersion { get; set; }

	public string UnoLoggingVersion { get; set; }

	public string WindowsCompatibilityVersion { get; set; }

	public string UnoWasmBootstrapVersion { get; set; }

	public string UnoUniversalImageLoaderVersion { get; set; }

	public string AndroidMaterialVersion { get; set; }

	public string UnoResizetizerVersion { get; set; }

	public string MicrosoftLoggingVersion { get; set; }

	public string WinAppSdkVersion { get; set; }

	public string WinAppSdkBuildToolsVersion { get; set; }

	public string UnoCoreLoggingSingletonVersion { get; set; }

	public string UnoDspTasksVersion { get; set; }

	public string CommunityToolkitMvvmVersion { get; set; }

	public string PrismVersion { get; set; }

	public string AndroidXNavigationVersion { get; set; }

	public string AndroidXCollectionVersion { get; set; }

	public string MicrosoftIdentityClientVersion { get; set; }

	private readonly List<PackageReference> _implicitPackages = [];
	[Output]
	public ITaskItem[] ImplicitPackages => [.. _implicitPackages.Distinct()
		.Select(x => x.ToTaskItem())];

	private readonly List<ITaskItem> _removePackageVersions = [];
	[Output]
	public ITaskItem[] RemovePackageVersions => [.. _removePackageVersions];

	public override bool Execute()
	{
		try
		{
			var features = GetFeatures();

			AddUnoCorePackages(features);
			AddUnoCSharpMarkup(features);
			AddUnoExtensionsPackages(features);
			AddUnoToolkitPackages(features);
			AddUnoThemes(features);
			AddPrism(features);
			AddPackageForFeature(features, UnoFeature.Dsp, "Uno.Dsp.Tasks", UnoDspTasksVersion);
			AddPackageForFeature(features, UnoFeature.Mvvm, "CommunityToolkit.Mvvm", CommunityToolkitMvvmVersion);
		}
		catch (Exception ex)
		{
			//System.Diagnostics.Debugger.Launch();
			Log.LogErrorFromException(ex);
		}

		return !Log.HasLoggedErrors;
	}

	private UnoFeature[] GetFeatures()
	{
		if (string.IsNullOrEmpty(UnoFeatures))
		{
			return [];
		}

		var features = Regex.Replace(UnoFeatures, @"\s", string.Empty);
		if (string.IsNullOrEmpty(features))
		{
			return [];
		}

		return features.Split(';')
			.Select(x => x.Trim()) // sanity check
			.Where(x => !string.IsNullOrEmpty(x))
			.Distinct()
			.Select(x => Enum.TryParse<UnoFeature>(x, out var f) ? f : UnoFeature.Invalid)
			.Where(x => x != UnoFeature.Invalid)
			.ToArray();
	}

	private void AddUnoCorePackages(IEnumerable<UnoFeature> features)
	{
		AddPackage("Uno.WinUI", UnoVersion);
		AddPackage("Uno.UI.Adapter.Microsoft.Extensions.Logging", UnoVersion);
		AddPackage("Uno.Resizetizer", UnoResizetizerVersion);
		AddPackage("Microsoft.Extensions.Logging.Console", MicrosoftLoggingVersion);

		AddPackageForFeature(features, UnoFeature.Maps, "Uno.WinUI.Maps", UnoVersion);
		AddPackageForFeature(features, UnoFeature.Foldable, "Uno.WinUI.Foldable", UnoVersion);

		if (TargetFrameworkIdentifier != UnoTarget.Windows)
		{
			AddPackage("Uno.WinUI.Lottie", UnoVersion);
			if (!Optimize)
			{
				// Included for Debug Builds
				AddPackage("Uno.WinUI.DevServer", UnoVersion);
			}

			if (TargetFrameworkIdentifier != UnoTarget.Wasm)
			{
				AddPackage("SkiaSharp.Skottie", SkiaSharpVersion);
				AddPackage("SkiaSharp.Views.Uno.WinUI", SkiaSharpVersion);
			}

			if (TargetFrameworkIdentifier == UnoTarget.Android)
			{
				AddPackage("Uno.UniversalImageLoader", UnoUniversalImageLoaderVersion);
				AddPackage("Xamarin.Google.Android.Material", AndroidMaterialVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.iOS)
			{
				AddPackage("Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.MacCatalyst)
			{
				AddPackage("Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.SkiaDesktop)
			{
				AddPackage("Uno.WinUI.Skia.Linux.FrameBuffer", UnoVersion);
				AddPackage("Uno.WinUI.Skia.MacOS", UnoVersion);
				AddPackage("Uno.WinUI.Skia.Wpf", UnoVersion);
				AddPackage("Uno.WinUI.Skia.X11", UnoVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.Wasm || IsLegacyWasmHead())
			{
				AddPackage("Uno.WinUI.WebAssembly", UnoVersion);
				AddPackage("Uno.Extensions.Logging.WebAssembly.Console", UnoLoggingVersion);
				AddPackage("Microsoft.Windows.Compatibility", WindowsCompatibilityVersion);

				if (SingleProject || IsLegacyWasmHead())
				{
					AddPackage("Uno.Wasm.Bootstrap", UnoWasmBootstrapVersion);
					AddPackage("Uno.Wasm.Bootstrap.DevServer", UnoWasmBootstrapVersion);
				}
			}
		}
		else
		{
			AddPackage("Microsoft.WindowsAppSDK", WinAppSdkVersion);
			AddPackage("Microsoft.Windows.SDK.BuildTools", WinAppSdkBuildToolsVersion);
			AddPackage("Uno.Core.Extensions.Logging.Singleton", UnoCoreLoggingSingletonVersion);
		}
	}

	private void AddUnoCSharpMarkup(IEnumerable<UnoFeature> features)
	{
		if (!features.Contains(UnoFeature.CSharpMarkup))
		{
			return;
		}

		AddPackage("Uno.WinUI.Markup", UnoCSharpMarkupVersion);
		AddPackage("Uno.Extensions.Markup.Generators", UnoCSharpMarkupVersion);
	}

	public void AddUnoExtensionsPackages(IEnumerable<UnoFeature> features)
	{
		var useExtensions = features.Contains(UnoFeature.Extensions);
		if (useExtensions || features.Contains(UnoFeature.Authentication))
		{
			AddPackage("Uno.Extensions.Authentication.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Authentication.MSAL.WinUI", UnoExtensionsVersion);
			AddPackage("Uno>Extensions.Authentication.Oidc.WinUI", UnoExtensionsVersion);
			AddPackage("Microsoft.Identity.Client", MicrosoftIdentityClientVersion);
		}
		else if (features.Contains(UnoFeature.AuthenticationMsal))
		{
			AddPackage("Uno.Extensions.Authentication.MSAL.WinUI", UnoExtensionsVersion);
			AddPackage("Microsoft.Identity.Client", MicrosoftIdentityClientVersion);
		}
		else if (features.Contains(UnoFeature.AuthenticationOidc))
		{
			AddPackage("Uno.Extensions.Authentication.Oidc.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Configuration))
		{
			AddPackage("Uno.Extensions.Configuration", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.ExtensionsCore))
		{
			AddPackage("Uno.Extensions.Core.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Hosting))
		{
			AddPackage("Uno.Extensions.Hosting.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Http))
		{
			AddPackage("Uno.Extensions.Http.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Http.Refit", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Localization))
		{
			AddPackage("Uno.Extensions.Localization.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Logging))
		{
			AddPackage("Uno.Extensions.Logging.WinUI", UnoExtensionsVersion);
		}

		if (features.Contains(UnoFeature.MauiEmbedding))
		{
			AddPackage("Uno.Extensions.Maui.WinUI", UnoExtensionsVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Extensions.Maui.WinUI.Markup", UnoExtensionsVersion);

			AddPackage("Microsoft.Maui.Controls", MauiVersion);
			AddPackage("Microsoft.Maui.Controls.Compatibility", MauiVersion);
			AddPackage("Microsoft.Maui.Graphics", MauiVersion);

			// TOOD: Add Android Packages
			if (TargetFrameworkIdentifier == UnoTarget.Android)
			{
				AddPackage("Xamarin.Google.Android.Material", AndroidMaterialVersion, true);
				AddPackage("Xamarin.AndroidX.Navigation.UI", AndroidXNavigationVersion, true);
				AddPackage("Xamarin.AndroidX.Navigation.Fragment", AndroidXNavigationVersion, true);
				AddPackage("Xamarin.AndroidX.Navigation.Runtime", AndroidXNavigationVersion, true);
				AddPackage("Xamarin.AndroidX.Navigation.Common", AndroidXNavigationVersion, true);
				AddPackage("Xamarin.AndroidX.Collection", AndroidXCollectionVersion, true);
				AddPackage("Xamarin.AndroidX.Collection.Ktx", AndroidXCollectionVersion, true);
			}
		}

		if ((useExtensions || features.Contains(UnoFeature.Navigation))
			&& !features.Contains(UnoFeature.Prism))
		{
			AddPackage("Uno.Extensions.Navigation.WinUI", UnoExtensionsVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Extensons.Navigation.WinUI.Markup", UnoExtensionsVersion);
			AddPackageForFeature(features, UnoFeature.Toolkit, "Uno.Extensions.Navigation.Toolkit.WinUI", UnoExtensionsVersion);
		}

		if ((useExtensions || features.Contains(UnoFeature.Mvux))
			&& !features.Contains(UnoFeature.Mvvm))
		{
			AddPackage("Uno.Extensions.Reactive.WinUI", UnoExtensionsVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Extensions.Reactive.WinUI.Markup", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Serialization))
		{
			AddPackage("Uno.Extensions.Serialization.Http", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Serialization.Refit", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Serilog))
		{
			AddPackage("Uno.Extensions.Logging.Serilog", UnoExtensionsVersion);
		}

		if (useExtensions || features.Contains(UnoFeature.Storage))
		{
			AddPackage("Uno.Extensions.Storage.WinUI", UnoExtensionsVersion);
		}
	}

	public void AddUnoToolkitPackages(IEnumerable<UnoFeature> features)
	{
		if (features.Contains(UnoFeature.Toolkit))
		{
			return;
		}

		AddPackage("Uno.Toolkit.WinUI", UnoToolkitVersion);
		AddPackageForFeature(features, UnoFeature.Cupertino, "Uno.Toolkit.WinUI.Cupertino", UnoToolkitVersion);
		if (features.Contains(UnoFeature.Material))
		{
			AddPackage("Uno.Toolkit.WinUI.Material", UnoToolkitVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Toolkit.WinUI.Material.Markup", UnoToolkitVersion);
		}

		AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Toolkit.WinUI.Markup", UnoToolkitVersion);
		AddPackageForFeature(features, UnoFeature.Skia, "Uno.Toolkit.Skia.WinUI", UnoToolkitVersion);
	}

	public void AddUnoThemes(IEnumerable<UnoFeature> features)
	{
		if (features.Contains(UnoFeature.Material))
		{
			AddPackage("Uno.Material.WinUI", UnoThemesVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Material.WinUI.Markup", UnoThemesVersion);
			AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Uno.Themes.WinUI.Markup", UnoThemesVersion);
		}
		else
		{
			AddPackageForFeature(features, UnoFeature.Cupertino, "Uno.Cupertino.WinUI", UnoThemesVersion);
		}
	}

	public void AddPrism(IEnumerable<UnoFeature> features)
	{
		if (!features.Contains(UnoFeature.Prism))
		{
			return;
		}

		AddPackage("Prism.DryIoc.Uno.WinUI", PrismVersion);
		AddPackageForFeature(features, UnoFeature.CSharpMarkup, "Prism.Uno.WinUI.Markup", PrismVersion);
	}
	private bool IsLegacyWasmHead()
	{
		if (string.IsNullOrWhiteSpace(TargetFrameworkIdentifier) || string.IsNullOrEmpty(ProjectName))
		{
			return false;
		}

		return ProjectName.EndsWith(".Wasm", StringComparison.InvariantCultureIgnoreCase)
			|| ProjectName.EndsWith(".WebAssembly", StringComparison.InvariantCultureIgnoreCase);
	}

	private void AddPackageForFeature(IEnumerable<UnoFeature> features, UnoFeature feature, string packageId, string packageVersion)
	{
		if (features.Contains(feature))
		{
			AddPackage(packageId, packageVersion);
		}
	}

	private void AddPackage(string packageId, string version, bool @override = false)
	{
		if (string.IsNullOrEmpty(version))
		{
			Log.LogWarning("The package '{0}' has no available version.", packageId);
			using var client = new NuGetClient();
			var preview = packageId.StartsWith("Uno.", StringComparison.InvariantCulture) && new NuGetVersion(UnoVersion).IsPreview;
			version = client.GetVersion(packageId, preview);
			Log.LogMessage(MessageImportance.High, "Retrived the latest package version '{0}' for the package '{1}'.", version, packageId);
		}

		if (PackageReferences.Any(x => x.ItemSpec == packageId))
		{
			Log.LogMessage(MessageImportance.High, "Uno Implicit PackageReferences are enabled, however you have an explicit reference to '{0}'. Please remove the PackageReference.", packageId);
			return;
		}

		var existingPackageVersion = PackageVersions.FirstOrDefault(x => x.ItemSpec == packageId);
		if (existingPackageVersion is not null)
		{
			_removePackageVersions.Add(existingPackageVersion);
		}

		var existing = _implicitPackages.SingleOrDefault(x => x.PackageId == packageId);
		if (existing is not null)
		{
			if (existing.Override == @override || !@override)
			{
				return;
			}

			_implicitPackages.Remove(existing);
		}

		_implicitPackages.Add(new PackageReference(packageId, version, @override));
	}

	private record PackageReference(string PackageId, string Version, bool Override)
	{
		public ITaskItem ToTaskItem()
		{
			var taskItem = new TaskItem
			{
				ItemSpec = PackageId,
			};
			var versionMetadta = Override ? "VersionOverride" : "Version";
			taskItem.SetMetadata(versionMetadta, Version);
			taskItem.SetMetadata("IsImplicitlyDefined", bool.TrueString);
			return taskItem;
		}
	}
}
