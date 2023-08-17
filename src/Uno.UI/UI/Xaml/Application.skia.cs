#nullable enable

using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;
		private static string _arguments = "";

		partial void InitializePartial()
		{
			SetCurrentLanguage();

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);

			CoreApplication.SetInvalidateRender(() =>
			{
				var roots = CoreServices.Instance.ContentRootCoordinator.ContentRoots;
				for (int i = 0; i < roots.Count; i++)
				{
					roots[i].XamlRoot?.QueueInvalidateRender();
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

		internal static void StartWithArguments(global::Microsoft.UI.Xaml.ApplicationInitializationCallback callback)
		{
			_arguments = GetCommandLineArgsWithoutExecutable();
			Start(callback);
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(
				ApplicationSynchronizationContext = new NativeDispatcherSynchronizationContext(NativeDispatcher.Main, NativeDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());
		}

		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
#if !HAS_UNO_WINUI
				// Force init
				Window.SafeCurrent?.ToString();
#endif

				InitializationCompleted();

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, _arguments));
			}
		}

		internal void ForceSetRequestedTheme(ApplicationTheme theme) => _requestedTheme = theme;
	}

	internal interface IApplicationEvents
	{
	}
}
