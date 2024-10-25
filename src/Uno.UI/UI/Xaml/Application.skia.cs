#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Media;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml
{
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
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Windows.UI.Xaml.Application.Start(_ => new App());");
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
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(
				ApplicationSynchronizationContext = new NativeDispatcherSynchronizationContext(NativeDispatcher.Main, NativeDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());

			_current.InvokeOnLaunched();
		}

		private void InvokeOnLaunched()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				InitializationCompleted();

				// OnLaunched should execute only for full apps, not for individual islands.
				if (CoreApplication.IsFullFledgedApp)
				{
					OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, GetCommandLineArgsWithoutExecutable()));
				}
			}
		}

		internal void ForceSetRequestedTheme(ApplicationTheme theme) => _requestedTheme = theme;
	}

	internal interface IApplicationEvents
	{
	}
}
