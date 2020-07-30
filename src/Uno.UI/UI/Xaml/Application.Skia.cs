using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Logging;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.Foundation.Extensibility;

namespace Windows.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked = false;
		private static string[] _args;
		private readonly IApplicationExtension _coreWindowExtension;

		internal ISkiaHost Host { get; set; }

		public Application()
		{
			Current = this;
			Package.SetEntryAssembly(this.GetType().Assembly);

			if (!ApiExtensibility.CreateInstance(this, out _coreWindowExtension))
			{
				throw new InvalidOperationException($"Unable to find IApplicationExtension extension");
			}

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Windows.UI.Xaml.Application.Start(_ => new App());");
			}

			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
		}

		internal static void Start(global::Windows.UI.Xaml.ApplicationInitializationCallback callback, string[] args)
		{
			_args = args;
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

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, string.Join(";", _args)));
			}
		}

		private ApplicationTheme GetDefaultSystemTheme() => _coreWindowExtension.GetDefaultSystemTheme();

		internal void ForceSetRequestedTheme(ApplicationTheme theme) => _requestedTheme = theme;
	}

	internal interface IApplicationExtension
	{
		ApplicationTheme GetDefaultSystemTheme();
	}

	internal interface IApplicationEvents
	{
	}

	internal interface ISkiaHost
	{
	}
}
