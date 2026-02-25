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


using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Microsoft.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;

		[ThreadStatic]
		private static Application _current;

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
			_current = this;
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

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(NativeDispatcher.Main.SynchronizationContext);

			callback(new ApplicationInitializationCallbackParams());

			if (OperatingSystem.IsBrowser())
			{
				_ = ApplicationData.Current.EnablePersistenceAsync();

				// Force a schedule to let the dotnet exports be initialized properly
				DispatcherQueue.Main.TryEnqueue(_current.InvokeOnLaunched);
			}
			else
			{
				// Other platforms can be synchronous, except iOS that requires
				// the creation of the window to be synchronous to avoid a black screen.
				_current.InvokeOnLaunched();
			}
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
							typeof(Application).LogDebug()?.Debug($"Failed to load ${nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a font manifest");
						}
					}
					catch (Exception e)
					{
						typeof(Application).LogError()?.Error($"Failed to load ${nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a font manifest", e);
					}
				}
				if (!textFontManifestSuccess)
				{
					try
					{
						textFontManifestSuccess = await FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.DefaultTextFontFamily), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
						if (!textFontManifestSuccess)
						{
							typeof(Application).LogDebug()?.Debug($"Failed to load ${nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a non-manifest font");
						}
					}
					catch (Exception e)
					{
						typeof(Application).LogError()?.Error($"Failed to load ${nameof(FeatureConfiguration.Font.DefaultTextFontFamily)}=[{FeatureConfiguration.Font.DefaultTextFontFamily}] as a non-manifest font", e);
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
						typeof(Application).LogError()?.Error($"Failed to load ${nameof(FeatureConfiguration.Font.SymbolsFont)}=[{FeatureConfiguration.Font.SymbolsFont}]");
					}
				}
				catch (Exception e)
				{
					typeof(Application).LogError()?.Error($"Failed to load ${nameof(FeatureConfiguration.Font.SymbolsFont)}=[{FeatureConfiguration.Font.SymbolsFont}]", e);
				}
			}
			catch (Exception e)
			{
				typeof(Application).LogError()?.Error($"Unexpected error during font preloading", e);
			}
		}
	}

	internal interface IApplicationEvents
	{
	}
}
