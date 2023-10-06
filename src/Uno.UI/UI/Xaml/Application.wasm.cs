#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI;
using Uno.UI.Xaml;
using Uno;
using System.Web;
using System.Collections.Specialized;
using Uno.Helpers;
using Uno.UI.Xaml.Controls;
using Uno.UI.Dispatching;


#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

using NativeMethods = __Microsoft.UI.Xaml.Application.NativeMethods;

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		private static bool _startInvoked;

		partial void InitializePartial()
		{
			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			global::Uno.Foundation.Extensibility.ApiExtensibility.Register(
				typeof(global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension),
				o => global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.GetForCurrentView());

			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);

			ObserveApplicationVisibility();
		}

		[JSExport]
		internal static int DispatchVisibilityChange(bool isVisible)
		{
			var application = Windows.UI.Xaml.Application.Current;
			if (isVisible)
			{
				application?.RaiseLeavingBackground(() =>
				{
					NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(true);
					NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.CodeActivated);
				});
			}
			else
			{
				NativeWindowWrapper.Instance?.OnNativeActivated(CoreWindowActivationState.Deactivated);
				NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(false);
				application?.RaiseEnteredBackground(null);
			}

			return 0;
		}

		static async partial void StartPartial(ApplicationInitializationCallback callback)
		{
			try
			{
				_startInvoked = true;

				SynchronizationContext.SetSynchronizationContext(
					new NativeDispatcherSynchronizationContext(NativeDispatcher.Main, NativeDispatcherPriority.Normal)
				);

				var isLoadEventsEnabled = !FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded;

				await WindowManagerInterop.InitAsync(isLoadEventsEnabled);

				Windows.Storage.ApplicationData.Init();

				callback(new ApplicationInitializationCallbackParams());
			}
			catch (Exception exception)
			{
				if (typeof(Application).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Application).Log().LogError("Application initialization failed.", exception);
				}
			}
		}

		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				var arguments = WindowManagerInterop.BeforeLaunch();

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Launch arguments: " + arguments);
				}

				InitializationCompleted();

				if (!string.IsNullOrEmpty(arguments))
				{
					if (ProtocolActivation.TryParseActivationUri(arguments, out var activationUri))
					{
						OnActivated(new ProtocolActivatedEventArgs(activationUri, ApplicationExecutionState.NotRunning));
						return;
					}
				}

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, arguments));
			}
		}

		/// <summary>
		/// Dispatch method from Javascript
		/// </summary>
		[JSExport]
		internal static void DispatchSuspending()
		{
			Current?.RaiseSuspending();
		}

		private void ObserveApplicationVisibility()
		{
			NativeMethods.ObserveVisibility();
		}
	}
}
