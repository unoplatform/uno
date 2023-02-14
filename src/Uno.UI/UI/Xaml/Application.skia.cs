#nullable enable

using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.Foundation.Extensibility;
using System.Globalization;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked;
		private static string _arguments = "";

		private readonly IApplicationExtension? _applicationExtension;

		internal ISkiaHost? Host { get; set; }

		public Application()
		{
			Current = this;
			SetCurrentLanguage();

			Package.SetEntryAssembly(this.GetType().Assembly);

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			ApiExtensibility.CreateInstance(this, out _applicationExtension);

			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
		}

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

		partial void ObserveSystemThemeChanges()
		{
			if (_applicationExtension != null)
			{
				_applicationExtension.SystemThemeChanged += SystemThemeChanged;
			}

			_systemThemeChangesObserved = true;
		}

		private void SystemThemeChanged(object? sender, EventArgs e) => OnSystemThemeChanged();
	}

	internal interface IApplicationEvents
	{
	}

	internal interface ISkiaHost
	{
	}
}
