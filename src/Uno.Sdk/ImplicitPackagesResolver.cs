using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace Uno.Sdk;

public sealed class ImplicitPackagesResolver : ImplicitPackagesResolverBase
{
	public string MauiVersion { get; set; }

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

	protected override void ExecuteInternal()
	{
		AddUnoCorePackages();
		AddUnoCSharpMarkup();
		AddUnoExtensionsPackages();
		AddUnoToolkitPackages();
		AddUnoThemes();
		AddPrism();
		AddPackageForFeature(UnoFeature.Dsp, "Uno.Dsp.Tasks", UnoDspTasksVersion);
		AddPackageForFeature(UnoFeature.Mvvm, "CommunityToolkit.Mvvm", CommunityToolkitMvvmVersion);
	}

	private void AddUnoCorePackages()
	{
		AddPackage("Uno.WinUI", UnoVersion);

		if (!IsPackable)
		{
			AddPackage("Uno.UI.Adapter.Microsoft.Extensions.Logging", UnoVersion);
			AddPackage("Uno.Resizetizer", UnoResizetizerVersion);
			AddPackage("Microsoft.Extensions.Logging.Console", MicrosoftLoggingVersion);
		}

		AddPackageForFeature(UnoFeature.Maps, "Uno.WinUI.Maps", UnoVersion);
		AddPackageForFeature(UnoFeature.Foldable, "Uno.WinUI.Foldable", UnoVersion);

		if (TargetFrameworkIdentifier != UnoTarget.Windows)
		{
			AddPackageWhen(!IsPackable, "Uno.WinUI.Lottie", UnoVersion);
			if (!Optimize)
			{
				// Included for Debug Builds
				AddPackage("Uno.WinUI.DevServer", UnoVersion);
			}

			if (TargetFrameworkIdentifier != UnoTarget.Wasm && !IsLegacyWasmHead())
			{
				AddPackageWhen(IsExecutable, "SkiaSharp.Skottie", SkiaSharpVersion);
				AddPackageWhen(IsExecutable, "SkiaSharp.Views.Uno.WinUI", SkiaSharpVersion);
			}

			if (TargetFrameworkIdentifier == UnoTarget.Android)
			{
				AddPackageWhen(IsExecutable, "Uno.UniversalImageLoader", UnoUniversalImageLoaderVersion);
				AddPackageWhen(IsExecutable, "Xamarin.Google.Android.Material", AndroidMaterialVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.iOS)
			{
				AddPackageWhen(IsExecutable, "Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.MacCatalyst)
			{
				AddPackageWhen(IsExecutable, "Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
			}
			else if (TargetFrameworkIdentifier == UnoTarget.SkiaDesktop)
			{
				AddPackageWhen(IsExecutable, "Uno.WinUI.Skia.Linux.FrameBuffer", UnoVersion);
				AddPackageWhen(IsExecutable, "Uno.WinUI.Skia.MacOS", UnoVersion);
				AddPackageWhen(IsExecutable, "Uno.WinUI.Skia.Wpf", UnoVersion);
				AddPackageWhen(IsExecutable, "Uno.WinUI.Skia.X11", UnoVersion);
			}
			else if (IsExecutable && (TargetFrameworkIdentifier == UnoTarget.Wasm || IsLegacyWasmHead()))
			{
				AddPackage("Uno.WinUI.WebAssembly", UnoVersion);
				AddPackageForFeature(UnoFeature.MediaElement, "Uno.WinUI.MediaPlayer.WebAssembly", UnoVersion);
				AddPackage("Microsoft.Windows.Compatibility", WindowsCompatibilityVersion);

				AddPackage("Uno.Extensions.Logging.WebAssembly.Console", UnoLoggingVersion);
				AddPackage("Uno.Wasm.Bootstrap", UnoWasmBootstrapVersion);
				AddPackage("Uno.Wasm.Bootstrap.DevServer", UnoWasmBootstrapVersion);
			}
		}
		else
		{
			AddPackage("Microsoft.WindowsAppSDK", WinAppSdkVersion);
			AddPackage("Microsoft.Windows.SDK.BuildTools", WinAppSdkBuildToolsVersion);
			AddPackageWhen(IsExecutable, "Uno.Core.Extensions.Logging.Singleton", UnoCoreLoggingSingletonVersion);
		}
	}

	private void AddUnoCSharpMarkup()
	{
		if (!HasFeature(UnoFeature.CSharpMarkup))
		{
			return;
		}

		AddPackage("Uno.WinUI.Markup", UnoCSharpMarkupVersion);
		AddPackage("Uno.Extensions.Markup.Generators", UnoCSharpMarkupVersion);
	}

	public void AddUnoExtensionsPackages()
	{
		var useExtensions = HasFeature(UnoFeature.Extensions);
		if (useExtensions || HasFeature(UnoFeature.Authentication))
		{
			AddPackage("Uno.Extensions.Authentication.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Authentication.MSAL.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Authentication.Oidc.WinUI", UnoExtensionsVersion);
			AddPackage("Microsoft.Identity.Client", MicrosoftIdentityClientVersion);
		}
		else if (HasFeature(UnoFeature.AuthenticationMsal))
		{
			AddPackage("Uno.Extensions.Authentication.MSAL.WinUI", UnoExtensionsVersion);
			AddPackage("Microsoft.Identity.Client", MicrosoftIdentityClientVersion);
		}
		else if (HasFeature(UnoFeature.AuthenticationOidc))
		{
			AddPackage("Uno.Extensions.Authentication.Oidc.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Configuration))
		{
			AddPackage("Uno.Extensions.Configuration", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.ExtensionsCore))
		{
			AddPackage("Uno.Extensions.Core.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Hosting))
		{
			AddPackage("Uno.Extensions.Hosting.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Http))
		{
			AddPackage("Uno.Extensions.Http.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Http.Refit", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Localization))
		{
			AddPackage("Uno.Extensions.Localization.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Logging))
		{
			AddPackage("Uno.Extensions.Logging.WinUI", UnoExtensionsVersion);
		}

		if (HasFeature(UnoFeature.MauiEmbedding))
		{
			AddPackage("Uno.Extensions.Maui.WinUI", UnoExtensionsVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Extensions.Maui.WinUI.Markup", UnoExtensionsVersion);

			AddPackage("Microsoft.Maui.Controls", MauiVersion);
			AddPackage("Microsoft.Maui.Controls.Compatibility", MauiVersion);
			AddPackage("Microsoft.Maui.Graphics", MauiVersion);

			if (SingleProject)
			{
				AddPackage("Microsoft.Maui.Controls.Build.Tasks", MauiVersion, "all");
			}

			if (TargetFrameworkIdentifier == UnoTarget.Android)
			{
				AddPackage("Xamarin.Google.Android.Material", AndroidMaterialVersion);
				AddPackage("Xamarin.AndroidX.Navigation.UI", AndroidXNavigationVersion);
				AddPackage("Xamarin.AndroidX.Navigation.Fragment", AndroidXNavigationVersion);
				AddPackage("Xamarin.AndroidX.Navigation.Runtime", AndroidXNavigationVersion);
				AddPackage("Xamarin.AndroidX.Navigation.Common", AndroidXNavigationVersion);
				AddPackage("Xamarin.AndroidX.Collection", AndroidXCollectionVersion);
				AddPackage("Xamarin.AndroidX.Collection.Ktx", AndroidXCollectionVersion);
			}
		}

		if ((useExtensions || HasFeature(UnoFeature.Navigation))
			&& !HasFeature(UnoFeature.Prism))
		{
			AddPackage("Uno.Extensions.Navigation.WinUI", UnoExtensionsVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Extensions.Navigation.WinUI.Markup", UnoExtensionsVersion);
			AddPackageForFeature(UnoFeature.Toolkit, "Uno.Extensions.Navigation.Toolkit.WinUI", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Mvux))
		{
			AddPackage("Uno.Extensions.Reactive.WinUI", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Reactive.Messaging", UnoExtensionsVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Extensions.Reactive.WinUI.Markup", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Serialization))
		{
			AddPackage("Uno.Extensions.Serialization.Http", UnoExtensionsVersion);
			AddPackage("Uno.Extensions.Serialization.Refit", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Serilog))
		{
			AddPackage("Uno.Extensions.Logging.Serilog", UnoExtensionsVersion);
		}

		if (useExtensions || HasFeature(UnoFeature.Storage))
		{
			AddPackage("Uno.Extensions.Storage.WinUI", UnoExtensionsVersion);
		}
	}

	public void AddUnoToolkitPackages()
	{
		if (!HasFeature(UnoFeature.Toolkit))
		{
			return;
		}

		AddPackage("Uno.Toolkit.WinUI", UnoToolkitVersion);
		AddPackageForFeature(UnoFeature.Cupertino, "Uno.Toolkit.WinUI.Cupertino", UnoToolkitVersion);
		if (HasFeature(UnoFeature.Material))
		{
			AddPackage("Uno.Toolkit.WinUI.Material", UnoToolkitVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Toolkit.WinUI.Material.Markup", UnoToolkitVersion);
		}

		AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Toolkit.WinUI.Markup", UnoToolkitVersion);
		AddPackageForFeature(UnoFeature.Skia, "Uno.Toolkit.Skia.WinUI", UnoToolkitVersion);
	}

	public void AddUnoThemes()
	{
		if (HasFeature(UnoFeature.Material))
		{
			AddPackage("Uno.Material.WinUI", UnoThemesVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Material.WinUI.Markup", UnoThemesVersion);
			AddPackageForFeature(UnoFeature.CSharpMarkup, "Uno.Themes.WinUI.Markup", UnoThemesVersion);
		}
		else
		{
			AddPackageForFeature(UnoFeature.Cupertino, "Uno.Cupertino.WinUI", UnoThemesVersion);
		}
	}

	public void AddPrism()
	{
		if (!HasFeature(UnoFeature.Prism))
		{
			return;
		}

		AddPackageWhen(!IsExecutable, "Prism.Uno.WinUI", PrismVersion);
		AddPackageWhen(IsExecutable, "Prism.DryIoc.Uno.WinUI", PrismVersion);
		AddPackageForFeature(UnoFeature.CSharpMarkup, "Prism.Uno.WinUI.Markup", PrismVersion);
	}
}
