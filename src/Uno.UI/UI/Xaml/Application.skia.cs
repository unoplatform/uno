// #define REPORT_FPS

#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;
using Windows.Globalization;
using System.Threading.Tasks;
using Uno.UI;
using Windows.UI.Text;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Windows.Storage;
using System.Runtime.Loader;


using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Microsoft.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;

		[ThreadStatic]
		private static string? _argumentsOverride;

		[ThreadStatic]
		private static Uri? _activationUri;

		internal static void SetArguments(string arguments)
			=> _argumentsOverride = arguments;

		internal static void SetActivationUri(Uri uri)
			=> _activationUri = uri;

		partial void InitializePartial()
		{
			SetCurrentApplication(this);
			SetCurrentLanguage();

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			// WinUI sets DispatcherShutdownMode to OnLastWindowClose when Start is called (see
			// FrameworkApplication::StartDesktop, which sets it *before* invoking the init callback).
			// We mirror that here, in the base ctor that runs during `new App()`, so the default is
			// established before the derived App constructor body runs and can override it.
			// For XAML Islands (no Start call), the field default of OnExplicitShutdown remains.
			_dispatcherShutdownMode = DispatcherShutdownMode.OnLastWindowClose;
		}

#if REPORT_FPS
		static FrameRateLogger _renderFpsLogger = new FrameRateLogger(typeof(Application), "Render");
#endif
		private long _lastRender = Stopwatch.GetTimestamp();


		internal ISkiaApplicationHost? Host { get; set; }

		private void SetCurrentLanguage()
		{
			if (CultureInfo.CurrentUICulture.IetfLanguageTag == "" &&
				CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "iv")
			{
				if (!ApplicationLanguages.InvariantCulture)
				{

					try
					{
						// Fallback to English
						var cultureInfo = CultureInfo.CreateSpecificCulture("en");
						CultureInfo.CurrentUICulture = cultureInfo;
						CultureInfo.CurrentCulture = cultureInfo;
						Thread.CurrentThread.CurrentCulture = cultureInfo;
						Thread.CurrentThread.CurrentUICulture = cultureInfo;
					}
					catch (Exception ex)
					{
						this.Log().Error($"Failed to set default culture", ex);
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("InvariantCulture mode is enabled");
					}
				}
			}
		}

		private static partial Application? StartPartial(Func<ApplicationInitializationCallbackParams, Application?> callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(NativeDispatcher.Main.SynchronizationContext);

			var currentApp = callback(new ApplicationInitializationCallbackParams()) ?? _current;

			if (currentApp is null)
			{
				throw new InvalidOperationException("Application.Start callback must return a non-null Application instance. Ensure your callback returns a valid Application.");
			}

			if (OperatingSystem.IsBrowser())
			{
				if (AssemblyLoadContext.GetLoadContext(currentApp.GetType().Assembly) == AssemblyLoadContext.Default)
				{
					// Ensure the assembly containing Application is not unloaded while running in WASM
					_ = ApplicationData.Current.EnablePersistenceAsync();
				}
				else
				{
					// Secondary ALC uses the primary ALC's ApplicationData, so no need to initialize it again

					if (typeof(Application).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(Application).Log().Debug("Skipping secondary ALC persistence initialization");
					}
				}

				// Force a schedule to let the dotnet exports be initialized properly
				DispatcherQueue.Main.TryEnqueue(currentApp.InvokeOnLaunched);
			}
			else
			{
				// Other platforms can be synchronous, except iOS that requires
				// the creation of the window to be synchronous to avoid a black screen.
				currentApp.InvokeOnLaunched();
			}

			return currentApp;
		}

		internal Task FontPreloadTask { get; private set; }

		private void InvokeOnLaunched()
		{
			InitializeSystemTheme();

			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				InitializationCompleted();
				FontPreloadTask = PreloadFonts();

				// OnLaunched should execute only for full apps, not for individual islands.
				if (CoreApplication.IsFullFledgedApp)
				{
					OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, GetCommandLineArgsWithoutExecutable()));

					if (OperatingSystem.IsAndroid() && _activationUri is { } uri)
					{
						OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.NotRunning));
						_activationUri = null;
					}
				}
			}
		}

		private static async Task PreloadFonts()
		{
			try
			{
				var symbolsFontTask = FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.SymbolsFont), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);

				var textFontManifestSuccess = false;
				if (Uri.TryCreate(FeatureConfiguration.Font.DefaultTextFontFamily, UriKind.RelativeOrAbsolute, out var uri))
				{
					try
					{
						textFontManifestSuccess = await FontFamilyHelper.PreloadAllFontsInManifest(uri);
						if (!textFontManifestSuccess)
						{
							typeof(Application).LogDebug()?.Debug($"Failed to load {nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a font manifest");
						}
					}
					catch (Exception e)
					{
						typeof(Application).LogError()?.Error($"Failed to load {nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a font manifest", e);
					}
				}
				if (!textFontManifestSuccess)
				{
					try
					{
						textFontManifestSuccess = await FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.DefaultTextFontFamily), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
						if (!textFontManifestSuccess)
						{
							typeof(Application).LogDebug()?.Debug($"Failed to load {nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a non-manifest font");
						}
					}
					catch (Exception e)
					{
						typeof(Application).LogError()?.Error($"Failed to load {nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a non-manifest font", e);
					}
				}

				try
				{
					var symbolsFontSuccess = await symbolsFontTask;
					if (symbolsFontSuccess)
					{
						typeof(Application).LogInfo()?.Info($"Loaded ${nameof(FeatureConfiguration.Font.SymbolsFont)}=[{FeatureConfiguration.Font.SymbolsFont}] successfully");
					}
					else
					{
						typeof(Application).LogError()?.Error($"Failed to load {nameof(FeatureConfiguration.Font.SymbolsFont)}=[{FeatureConfiguration.Font.SymbolsFont}]");
					}
				}
				catch (Exception e)
				{
					typeof(Application).LogError()?.Error($"Failed to load {nameof(FeatureConfiguration.Font.SymbolsFont)}=[{FeatureConfiguration.Font.SymbolsFont}]", e);
				}
			}
			catch (Exception e)
			{
				typeof(Application).LogError()?.Error($"Unexpected error during font preloading", e);
			}
		}
		partial void InitializeTextScalingPlatform()
		{
			global::Windows.UI.ViewManagement.UISettings.ObserveTextScaleFactorChanges();
			global::Windows.UI.ViewManagement.UISettings.TextScaleFactorChangedInternal += (_, _) =>
			{
				CoreServices.Instance.UpdateFontScale(global::Windows.UI.ViewManagement.UISettings.GetTextScaleFactorValue());
			};
		}
	}

	internal interface IApplicationEvents
	{
	}
}
