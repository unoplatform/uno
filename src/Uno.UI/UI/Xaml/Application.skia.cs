// #define REPORT_FPS

#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
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

		private static HashSet<Visual> _continuousTargets = new();

		partial void InitializePartial()
		{
			_current = this;
			SetCurrentLanguage();

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			CoreApplication.SetInvalidateRender(OnInvalidateRender, OnSetContinuousRender);

			NativeDispatcher.Main.Rendered += OnRendered;
		}

#if REPORT_FPS
		static FrameRateLogger _renderFpsLogger = new FrameRateLogger(typeof(Application), "Render");
#endif
		private long _lastRender = Stopwatch.GetTimestamp();

		private void OnRendered()
		{
			if (CompositionTargetTimer.IsRunning
				&& Stopwatch.GetElapsedTime(_lastRender) < TimeSpan.FromSeconds(1 / FeatureConfiguration.CompositionTarget.FrameRate))
			{
				// Throttle rendering to the expected frame rate
				return;
			}

			_lastRender = Stopwatch.GetTimestamp();

#if REPORT_FPS
			_renderFpsLogger.ReportFrame();
#endif

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OnRendered");
			}

			foreach (var cRoot in CoreServices.Instance.ContentRootCoordinator.ContentRoots)
			{
				if (cRoot?.XamlRoot is { } xRoot)
				{
					xRoot.InvalidateRender();
				}
			}
		}

		private void OnSetContinuousRender(object? compositionTarget, bool enabled)
		{
			Debug.Assert(compositionTarget is null or CompositionTarget);

			if (compositionTarget is CompositionTarget { Root: { } root })
			{
				var originalCount = _continuousTargets.Count;

				if (_continuousTargets.Contains(root))
				{
					if (!enabled)
					{
						_continuousTargets.Remove(root);
					}
				}
				else
				{
					if (enabled)
					{
						_continuousTargets.Add(root);
					}
				}

				if (originalCount != _continuousTargets.Count)
				{
					if (_continuousTargets.Count == 0)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"OnSetContinuousRender({enabled}) detach (Count:{_continuousTargets.Count})");
						}

						// We have no targets anymore, unhook from the composition target
						CompositionTarget.Rendering -= OnContinuousRender;
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"OnSetContinuousRender({enabled}) attach (Count:{_continuousTargets.Count})");
						}

						// We have at least one target, we need to start the render loop
						CompositionTarget.Rendering += OnContinuousRender;
					}
				}
			}
		}

		private void OnContinuousRender(object? sender, object e)
		{
			// Intentionally empty to force enable continous mode.
		}

		private void OnInvalidateRender(object? compositionTarget)
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

			SynchronizationContext.SetSynchronizationContext(
				ApplicationSynchronizationContext = new NativeDispatcherSynchronizationContext(NativeDispatcher.Main, NativeDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());

			if (OperatingSystem.IsBrowser())
			{
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
				_ = FontFamilyHelper.PreloadAllFontsInManifest(uri);
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
