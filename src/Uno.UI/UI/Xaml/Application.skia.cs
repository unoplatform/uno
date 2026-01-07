// #define REPORT_FPS

#nullable enable

using System;
using System.Diagnostics;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;
using Windows.Globalization;
using Uno.UI;
using Windows.UI.Text;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;


#if HAS_UNO_WINUI || WINAPPSDK
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
#else
using DispatcherQueue = Windows.System.DispatcherQueue;
#endif

namespace Microsoft.UI.Xaml;

public partial class Application : IApplicationEvents
{
	private static bool _startInvoked;

	[ThreadStatic]
	private static Application _current;

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
			DispatcherQueue.Main.TryEnqueue(_current.PrepareOnLaunched);
		}
		else
		{
			// Other platforms can be synchronous, except iOS that requires
			// the creation of the window to be synchronous to avoid a black screen.
			_current.PrepareOnLaunched();
		}
	}

	private void PrepareOnLaunched()
	{
		using var _ = WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop);
		InitializeSystemTheme();

		InitializationCompleted();
		PreloadFonts();

		InvokeOnLaunched(null);
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
