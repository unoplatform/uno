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
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml
{
	public partial class Application : IApplicationEvents
	{
		private static bool _startInvoked = false;
		private static string[] _args;
		private readonly IApplicationExtension _applicationExtension;

		internal ISkiaHost Host { get; set; }

		public Application()
		{
			Current = this;
			Package.SetEntryAssembly(this.GetType().Assembly);

			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Windows.UI.Xaml.Application.Start(_ => new App());");
			}

			ApiExtensibility.CreateInstance(this, out _applicationExtension);

			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
		}

		internal static void Start(global::Windows.UI.Xaml.ApplicationInitializationCallback callback, string[] args)
		{
			_args = args;
			Start(callback);
		}

		public void Exit()
		{
			if (_applicationExtension != null && _applicationExtension.CanExit)
			{
				_applicationExtension.Exit();
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("This platform does not support application exit.");
				}
			}
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

		internal void ForceSetRequestedTheme(ApplicationTheme theme) => _requestedTheme = theme;
	}

	internal interface IApplicationEvents
	{
	}

	internal interface ISkiaHost
	{
	}
}
