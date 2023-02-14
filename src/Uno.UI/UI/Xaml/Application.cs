using System;
using Uno;
using Uno.UI;
using Uno.Diagnostics.Eventing;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Uno.Helpers.Theming;
using Windows.UI.ViewManagement;
using Uno.Extensions;
using System.Collections.Generic;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Data;
using Uno.Foundation.Extensibility;
using Windows.UI.Popups.Internal;
using Windows.UI.Popups;
using Uno.UI.WinRT.Extensions.UI.Popups;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using DependencyObject = System.Object;
#elif XAMARIN_IOS
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using AppKit;
#else
using View = Microsoft.UI.Xaml.UIElement;
using ViewGroup = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		private bool _initializationComplete;
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private ApplicationTheme? _requestedTheme;
#pragma warning disable CA1805 // Do not initialize unnecessarily
		// TODO: This field is ALWAYS false. Either remove it or assign when appropriate.
		private bool _systemThemeChangesObserved = false;
#pragma warning restore CA1805 // Do not initialize unnecessarily
		private SpecializedResourceDictionary.ResourceKey _requestedThemeForResources;
		private bool _isInBackground;

		static Application()
		{
			ApiInformation.RegisterAssembly(typeof(Application).Assembly);
			ApiInformation.RegisterAssembly(typeof(Windows.Storage.ApplicationData).Assembly);
			ApiInformation.RegisterAssembly(typeof(Microsoft.UI.Composition.Compositor).Assembly);

			Uno.Helpers.DispatcherTimerProxy.SetDispatcherTimerGetter(() => new DispatcherTimer());
			Uno.Helpers.VisualTreeHelperProxy.SetCloseAllFlyoutsAction(() => Media.VisualTreeHelper.CloseAllFlyouts());

			RegisterExtensions();

			InitializePartialStatic();
		}

		private static void RegisterExtensions()
		{
			ApiExtensibility.Register<MessageDialog>(typeof(IMessageDialogExtension), dialog => new MessageDialogExtension(dialog));
		}

		static partial void InitializePartialStatic();

		[Preserve]
		public static class TraceProvider
		{
			public readonly static Guid Id = new Guid(
				// {DEE07725-1CBF-4BF6-AC8A-960360CB3512}
				unchecked((int)0xdee07725), 0x1cbf, 0x4bf6, new byte[] { 0xac, 0x8a, 0x96, 0x3, 0x60, 0xcb, 0x35, 0x12 }
			);

			public const int LauchedStart = 1;
			public const int LauchedStop = 2;
		}

		public static Application Current { get; private set; }

		public DebugSettings DebugSettings { get; } = new DebugSettings();

		public ApplicationRequiresPointerMode RequiresPointerMode { get; set; } = ApplicationRequiresPointerMode.Auto;

		/// <summary>
		/// Specifies the visual feedback used to indicate the UI element
		/// with focus when navigating with a keyboard or gamepad.
		/// </summary>
		public FocusVisualKind FocusVisualKind { get; set; } = FocusVisualKind.HighVisibility;

		public ApplicationTheme RequestedTheme
		{
			get
			{
				EnsureInternalRequestedTheme();
				return InternalRequestedTheme.Value;
			}
			set
			{
				if (_initializationComplete)
				{
					throw new NotSupportedException("Operation not supported");
				}
				SetExplicitRequestedTheme(value);
			}
		}

		private void EnsureInternalRequestedTheme()
		{
			if (InternalRequestedTheme == null)
			{
				// just cache the theme, but do not notify about a change unnecessarily	
				InternalRequestedTheme = GetDefaultSystemTheme();
			}
		}

		private ApplicationTheme? InternalRequestedTheme
		{
			get => _requestedTheme;
			set
			{
				_requestedTheme = value;
				// Sync with core application's theme
				CoreApplication.RequestedTheme = value == ApplicationTheme.Dark ? SystemTheme.Dark : SystemTheme.Light;
				UpdateRequestedThemesForResources();
			}
		}

		internal static void UpdateRequestedThemesForResources()
		{
			Current.RequestedThemeForResources =
				(ApplicationHelper.RequestedCustomTheme, Current.RequestedTheme) switch
				{
					(var custom, _) when !custom.IsNullOrEmpty() => custom,
					(_, ApplicationTheme.Light) => "Light",
					(_, ApplicationTheme.Dark) => "Dark",
					_ => throw new InvalidOperationException($"Theme {Application.Current.RequestedTheme} is not valid"),
				};
		}

		internal SpecializedResourceDictionary.ResourceKey RequestedThemeForResources
		{
			get
			{
				EnsureInternalRequestedTheme();
				return _requestedThemeForResources;
			}

			private set
			{
				_requestedThemeForResources = value;
				ResourceDictionary.SetActiveTheme(value);
			}
		}

		internal ElementTheme ActualElementTheme => RequestedTheme switch
		{
			ApplicationTheme.Light => ElementTheme.Light,
			ApplicationTheme.Dark => ElementTheme.Dark,
			_ => throw new InvalidOperationException("Application's RequestedTheme is invalid."),
		};

		internal bool IsThemeSetExplicitly { get; private set; }

		internal void SetExplicitRequestedTheme(ApplicationTheme? explicitTheme)
		{
			// this flag makes sure the app will not respond to OS events
			IsThemeSetExplicitly = explicitTheme.HasValue;
			var theme = explicitTheme ?? GetDefaultSystemTheme();
			SetRequestedTheme(theme);
		}

		public ResourceDictionary Resources { get; set; } = new ResourceDictionary();

#pragma warning disable CS0067 // The event is never used
		/// <summary>
		/// Occurs when the application transitions from Suspended state to Running state.
		/// </summary>
		public event EventHandler<object> Resuming;
#pragma warning restore CS0067 // The event is never used

#pragma warning disable CS0067 // The event is never used
		/// <summary>
		/// Occurs when the application transitions to Suspended state from some other state.
		/// </summary>
		public event SuspendingEventHandler Suspending;
#pragma warning restore CS0067 // The event is never used

		/// <summary>
		/// Occurs when the app moves from the foreground to the background.
		/// </summary>
		public event EnteredBackgroundEventHandler EnteredBackground;

		/// <summary>
		/// Occurs when the app moves from the background to the foreground.
		/// </summary>
		public event LeavingBackgroundEventHandler LeavingBackground;

		/// <summary>
		/// Occurs when an exception can be handled by app code, as forwarded from a native-level Windows Runtime error.
		/// Apps can mark the occurrence as handled in event data.
		/// </summary>
		public event UnhandledExceptionEventHandler UnhandledException;

		public void OnSystemThemeChanged()
		{
			// if user overrides theme, don't apply system theme
			if (!IsThemeSetExplicitly)
			{
				var theme = GetDefaultSystemTheme();
				SetRequestedTheme(theme);
			}

			UISettings.OnColorValuesChanged();
		}

#if !__ANDROID__ && !__MACOS__ && !__SKIA__
		[NotImplemented("__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__")]
		public void Exit()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("This platform does not support application exit.");
			}
		}
#else
		/// <summary>
		/// Shuts down the app.
		/// </summary>
		public void Exit() => CoreApplication.Exit();
#endif

		public static void Start(global::Microsoft.UI.Xaml.ApplicationInitializationCallback callback)
		{
			StartPartial(callback);
		}

		partial void ObserveSystemThemeChanges();

		static partial void StartPartial(ApplicationInitializationCallback callback);

		protected internal virtual void OnActivated(IActivatedEventArgs args) { }

		protected internal virtual void OnLaunched(LaunchActivatedEventArgs args) { }

		internal void InitializationCompleted()
		{
			if (!_systemThemeChangesObserved)
			{
				ObserveSystemThemeChanges();
			}
			_initializationComplete = true;
		}

		internal void RaiseRecoverableUnhandledException(Exception e) => UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));

		private ApplicationTheme GetDefaultSystemTheme() =>
			SystemThemeHelper.SystemTheme == SystemTheme.Light ?
				ApplicationTheme.Light : ApplicationTheme.Dark;

		private IDisposable WritePhaseEventTrace(int startEventId, int stopEventId)
		{
			if (_trace.IsEnabled)
			{
				return _trace.WriteEventActivity(
					startEventId,
					stopEventId,
					Array.Empty<object>()
				);
			}
			else
			{
				return null;
			}
		}

		internal void RaiseEnteredBackground(Action onComplete)
		{
			if (!_isInBackground)
			{
				_isInBackground = true;
				var enteredEventArgs = new EnteredBackgroundEventArgs(onComplete);
				EnteredBackground?.Invoke(this, enteredEventArgs);
				CoreApplication.RaiseEnteredBackground(enteredEventArgs);
				var completedSynchronously = enteredEventArgs.DeferralManager.EventRaiseCompleted();

				// Asynchronous suspension is not supported
				if (!completedSynchronously && this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"Asynchronous entered background completion is not supported yet. " +
						"Long running operations may be terminated prematurely.");
				}
			}
			else
			{
				onComplete?.Invoke();
			}
		}

		internal void RaiseLeavingBackground(Action onComplete)
		{
			if (_isInBackground)
			{
				_isInBackground = false;
				var leavingEventArgs = new LeavingBackgroundEventArgs(onComplete);
				LeavingBackground?.Invoke(this, leavingEventArgs);
				CoreApplication.RaiseLeavingBackground(leavingEventArgs);
				var completedSynchronously = leavingEventArgs.DeferralManager.EventRaiseCompleted();

				// Asynchronous suspension is not supported
				if (!completedSynchronously && this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"Asynchronous leaving background completion is not supported yet. " +
						"Application may resume before the operation completes.");
				}
			}
			else
			{
				onComplete?.Invoke();
			}
		}

		internal void RaiseResuming()
		{
			Resuming?.Invoke(null, null);
			CoreApplication.RaiseResuming();

			OnResumingPartial();
		}

		partial void OnResumingPartial();

		internal void RaiseSuspending()
		{
			var suspendingOperation = CreateSuspendingOperation();
			var suspendingEventArgs = new SuspendingEventArgs(suspendingOperation);

			Suspending?.Invoke(this, suspendingEventArgs);
			CoreApplication.RaiseSuspending(suspendingEventArgs);
			var completedSynchronously = suspendingOperation.DeferralManager.EventRaiseCompleted();

#if !__IOS__ && !__ANDROID__
			// Asynchronous suspension is not supported on all targets, warn the user
			if (!completedSynchronously && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					"This platform does not support asynchronous Suspending deferral. " +
					"Code executed after the of the method called by Suspending may not get executed.");
			}
#endif
		}

#if !__IOS__ && !__ANDROID__ && !__MACOS__
		/// <summary>
		/// On platforms which don't support asynchronous suspension we indicate that with immediate
		/// deadline and warning in logs.
		/// </summary>
		private SuspendingOperation CreateSuspendingOperation() =>
			new SuspendingOperation(DateTimeOffset.Now.AddSeconds(0), null);
#endif

		protected virtual void OnWindowCreated(global::Microsoft.UI.Xaml.WindowCreatedEventArgs args)
		{
		}

		internal void RaiseWindowCreated(Window window)
		{
			OnWindowCreated(new WindowCreatedEventArgs(window));
		}

		private void SetRequestedTheme(ApplicationTheme requestedTheme)
		{
			if (requestedTheme != InternalRequestedTheme)
			{
				InternalRequestedTheme = requestedTheme;

				OnRequestedThemeChanged();
			}
		}

		internal void UpdateResourceBindingsForHotReload() => OnResourcesChanged(ResourceUpdateReason.HotReload);

		internal void OnRequestedThemeChanged() => OnResourcesChanged(ResourceUpdateReason.ThemeResource);

		private void OnResourcesChanged(ResourceUpdateReason updateReason)
		{
			if (GetTreeRoot() is { } root)
			{
				// Update theme bindings in application resources
				Resources?.UpdateThemeBindings(updateReason);

				// Update theme bindings in system resources
				ResourceResolver.UpdateSystemThemeBindings(updateReason);

				PropagateResourcesChanged(root, updateReason);
			}

			// Start from the real root, which may not be a FrameworkElement on some platforms
			View GetTreeRoot()
			{
				View current = Microsoft.UI.Xaml.Window.Current.Content;
				var parent = current?.GetVisualTreeParent();
				while (parent != null)
				{
					current = parent;
					parent = current?.GetVisualTreeParent();
				}
				return current;
			}
		}

		/// <summary>
		/// Propagate theme changed to <paramref name="instance"/> and its descendants, to have them update any theme bindings.
		/// </summary>
		internal static void PropagateResourcesChanged(object instance, ResourceUpdateReason updateReason)
		{

			// Update ThemeResource references that have changed
			if (instance is FrameworkElement fe)
			{
				fe.UpdateThemeBindings(updateReason);
			}

			//Try Panel.Children before ViewGroup.GetChildren - this results in fewer allocations
			if (instance is Controls.Panel p)
			{
				foreach (object o in p.Children)
				{
					PropagateResourcesChanged(o, updateReason);
				}
			}
			else if (instance is ViewGroup g)
			{
				foreach (object o in g.GetChildren())
				{
					PropagateResourcesChanged(o, updateReason);
				}
			}
		}

		private static string GetCommandLineArgsWithoutExecutable()
		{
			var args = Environment.GetCommandLineArgs();
			if (args.Length <= 1)
			{
				return "";
			}

			// The first "argument" is actually application name, needs to be removed.
			// May be wrapped in quotes.

			var executable = args[0];
			var rawCmd = Environment.CommandLine;

			var index = rawCmd.IndexOf(executable, StringComparison.Ordinal);
			if (index == 0)
			{
				rawCmd = rawCmd.Substring(executable.Length);
			}
			else if (index == 1)
			{
				// The executable is wrapped in quotes
				rawCmd = rawCmd.Substring(executable.Length + 2);
			}

			// The whitespace on the start side of Arguments
			// in UWP is trimmed whereas the ending is not.
			return rawCmd.TrimStart();
		}
	}
}
