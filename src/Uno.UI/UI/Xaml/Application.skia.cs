#nullable enable

using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;

namespace Windows.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;
		private static string _arguments = "";

		public Application()
		{
			Current = this;
			SetCurrentLanguage();
			InitializeSystemTheme();

			Package.SetEntryAssembly(this.GetType().Assembly);

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Windows.UI.Xaml.Application.Start(_ => new App());");
			}

			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
		}

		internal ISkiaApplicationHost? Host { get; set; }

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

		internal static void StartWithArguments(global::Windows.UI.Xaml.ApplicationInitializationCallback callback)
		{
			_arguments = GetCommandLineArgsWithoutExecutable();
			Start(callback);
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			SynchronizationContext.SetSynchronizationContext(
				new CoreDispatcherSynchronizationContext(CoreDispatcher.Main, CoreDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());
		}

		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				// Force init
				Window.Current.ToString();

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
