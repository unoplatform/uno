namespace Uno.Sdk.Tasks;

public sealed class ImplicitPackagesResolver_v0 : ImplicitPackagesResolverBase
{
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
		AddPackage("Uno.WinUI", null);

		if (!IsPackable)
		{
			AddPackage("Uno.UI.Adapter.Microsoft.Extensions.Logging", null);
			AddPackage("Uno.Resizetizer", UnoResizetizerVersion);
			AddPackage("Microsoft.Extensions.Logging.Console", MicrosoftLoggingVersion);
		}

		AddPackageForFeature(UnoFeature.Maps, "Uno.WinUI.Maps", null);
		AddPackageForFeature(UnoFeature.Foldable, "Uno.WinUI.Foldable", null);

		if (TargetRuntime != UnoTarget.Windows)
		{
			AddPackageWhen(!IsPackable, "Uno.WinUI.Lottie", null);

			// Included for Debug Builds
			AddPackageWhen(!Optimize, "Uno.WinUI.DevServer", null);

			if (TargetRuntime != UnoTarget.Wasm && !IsLegacyWasmHead() && !IsPackable)
			{
				AddPackage("SkiaSharp.Skottie", SkiaSharpVersion);
				AddPackage("SkiaSharp.Views.Uno.WinUI", SkiaSharpVersion);
			}

			if (TargetRuntime == UnoTarget.Wasm || IsLegacyWasmHead())
			{
				AddPackage("Uno.WinUI.WebAssembly", null);
			}

			AddCorePackagesForExecutable();
		}
		else
		{
			AddPackage("Microsoft.WindowsAppSDK", WinAppSdkVersion);
			AddPackage("Microsoft.Windows.SDK.BuildTools", WinAppSdkBuildToolsVersion);
			AddPackageWhen(IsExecutable, "Uno.Core.Extensions.Logging.Singleton", UnoCoreLoggingSingletonVersion);
		}
	}

	private void AddCorePackagesForExecutable()
	{
		if (!IsExecutable)
		{
			Debug("Skipping Core packages for Library build.");
			return;
		}

		if (TargetRuntime == UnoTarget.Android)
		{
			if (!HasFeature(UnoFeature.MauiEmbedding))
			{
				AddPackage("Xamarin.Google.Android.Material", AndroidMaterialVersion);
			}

			AddPackage("Uno.UniversalImageLoader", UnoUniversalImageLoaderVersion);
			AddPackage("Xamarin.AndroidX.Legacy.Support.V4", AndroidXLegacySupportV4Version);
			AddPackage("Xamarin.AndroidX.AppCompat", AndroidXAppCompatVersion);
			AddPackage("Xamarin.AndroidX.RecyclerView", AndroidXRecyclerViewVersion);
			AddPackage("Xamarin.AndroidX.Activity", AndroidXActivityVersion);
			AddPackage("Xamarin.AndroidX.Browser", AndroidXBrowserVersion);
			AddPackage("Xamarin.AndroidX.SwipeRefreshLayout", AndroidXSwipeRefreshLayoutVersion);
		}
		else if (TargetRuntime == UnoTarget.iOS)
		{
			AddPackage("Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
		}
		else if (TargetRuntime == UnoTarget.MacCatalyst)
		{
			AddPackage("Uno.Extensions.Logging.OSLog", UnoLoggingVersion);
		}
		else if (TargetRuntime == UnoTarget.SkiaDesktop)
		{
			AddPackage("Uno.WinUI.Skia.Linux.FrameBuffer", null);
			AddPackage("Uno.WinUI.Skia.MacOS", null);
			AddPackage("Uno.WinUI.Skia.Wpf", null);
			AddPackage("Uno.WinUI.Skia.X11", null);
		}
		else if (TargetRuntime == UnoTarget.SkiaGtk)
		{
			AddPackage("Uno.WinUI.Skia.Gtk", null);
			AddPackage("SkiaSharp.NativeAssets.Linux", SkiaSharpVersion);
			AddPackage("SkiaSharp.NativeAssets.macOS", SkiaSharpVersion);
		}
		else if (TargetRuntime == UnoTarget.SkiaLinuxFramebuffer)
		{
			AddPackage("Uno.WinUI.Skia.Linux.FrameBuffer", null);
			AddPackage("SkiaSharp.NativeAssets.Linux", SkiaSharpVersion);
		}
		else if (TargetRuntime == UnoTarget.SkiaWpf)
		{
			AddPackage("Uno.WinUI.Skia.Wpf", null);
			AddPackage("SkiaSharp.NativeAssets.Win32", SkiaSharpVersion);
		}
		else if (TargetRuntime == UnoTarget.Wasm || IsLegacyWasmHead())
		{
			AddPackageForFeature(UnoFeature.MediaElement, "Uno.WinUI.MediaPlayer.WebAssembly", null);
			AddPackage("Microsoft.Windows.Compatibility", WindowsCompatibilityVersion);

			AddPackage("Uno.Extensions.Logging.WebAssembly.Console", UnoLoggingVersion);
			AddPackage("Uno.Wasm.Bootstrap", UnoWasmBootstrapVersion);
			AddPackage("Uno.Wasm.Bootstrap.DevServer", UnoWasmBootstrapVersion);
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
			AddPackage("Uno.WinUI.MSAL", null);
		}
		else if (HasFeature(UnoFeature.AuthenticationMsal))
		{
			AddPackage("Uno.Extensions.Authentication.MSAL.WinUI", UnoExtensionsVersion);
			AddPackage("Microsoft.Identity.Client", MicrosoftIdentityClientVersion);
			AddPackage("Uno.WinUI.MSAL", null);
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

			if (TargetRuntime == UnoTarget.Android)
			{
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
