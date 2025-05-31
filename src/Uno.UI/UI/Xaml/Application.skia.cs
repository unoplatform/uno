﻿#nullable enable

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
		private static Application _current;

		[ThreadStatic]
		private static string? _argumentsOverride;

		internal static void SetArguments(string arguments)
			=> _argumentsOverride = arguments;

		partial void InitializePartial()
		{
			_current = this;
			SetCurrentLanguage();

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			CoreApplication.SetInvalidateRender(compositionTarget =>
			{
				Debug.Assert(compositionTarget is null or CompositionTarget);

				if (compositionTarget is CompositionTarget { Root: { } root })
				{
					foreach (var cRoot in CoreServices.Instance.ContentRootCoordinator.ContentRoots)
					{
						if (cRoot?.XamlRoot is { } xRoot && ReferenceEquals(xRoot.VisualTree.RootElement.Visual, root))
						{
							xRoot.QueueInvalidateRender();
							return;
						}
					}
				}
			});
		}

		internal ISkiaApplicationHost? Host { get; set; }

		internal static SynchronizationContext ApplicationSynchronizationContext { get; private set; }

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
					if (typeof(ApplicationLanguages).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(ApplicationLanguages).Log().Debug("InvariantCulture mode is enabled");
					}
				}
			}
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(
				ApplicationSynchronizationContext = new NativeDispatcherSynchronizationContext(NativeDispatcher.Main, NativeDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());

			// Force a schedule to let the dotnet exports be initialized properly
			DispatcherQueue.Main.TryEnqueue(_current.InvokeOnLaunched);
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

		internal void ForceSetRequestedTheme(ApplicationTheme theme) => _requestedTheme = theme;

		private static void PreloadFonts()
		{
			if (OperatingSystem.IsBrowser())
			{
				// WASM does the font preloading before removing the splash via PrefetchFonts.
				return;
			}

			var _ = Uno.UI.Xaml.FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.SymbolsFont), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
			_ = Uno.UI.Xaml.FontFamilyHelper.PreloadAllFontsInManifest(new Uri(FeatureConfiguration.Font.DefaultTextFontFamily));
		}
	}

	internal interface IApplicationEvents
	{
	}
}
