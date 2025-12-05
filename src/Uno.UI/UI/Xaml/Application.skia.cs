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


#if HAS_UNO_WINUI || WINAPPSDK
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
#else
using DispatcherQueue = Windows.System.DispatcherQueue;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;

		[ThreadStatic]
		private static string? _argumentsOverride;

		internal static void SetArguments(string arguments)
			=> _argumentsOverride = arguments;

		partial void InitializePartial()
		{
			SetCurrentApplication(this);
			SetCurrentLanguage();

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}
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

		private void InvokeOnLaunched()
		{
			InitializeSystemTheme();

			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				InitializationCompleted();
				PreloadFonts();

				// OnLaunched should execute only for full apps, not for individual islands.
				if (CoreApplication.IsFullFledgedApp)
				{
					OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, GetCommandLineArgsWithoutExecutable()));
				}
			}
		}

		private static void PreloadFonts()
		{
			if (OperatingSystem.IsBrowser())
			{
				// WASM does the font preloading before removing the splash via PrefetchFonts.
				return;
			}

			_ = FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.SymbolsFont), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
			if (Uri.TryCreate(FeatureConfiguration.Font.DefaultTextFontFamily, UriKind.RelativeOrAbsolute, out var uri))
			{
				_ = FontFamilyHelper.PreloadAllFontsInManifest(uri).ContinueWith(t =>
				{
					if (!t.IsCompletedSuccessfully)
					{
						_ = FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.DefaultTextFontFamily), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
					}
				});
			}
			else
			{
				_ = FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.DefaultTextFontFamily), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
			}
		}
	}

	internal interface IApplicationEvents
	{
	}
}
