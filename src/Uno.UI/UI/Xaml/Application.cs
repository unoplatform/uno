using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Web;
using Microsoft.UI.Xaml.Data;
using Uno;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.WinRT.Extensions.UI.Popups;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Popups.Internal;
using Windows.UI.ViewManagement;

using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using DependencyObject = System.Object;
using Microsoft.UI.Xaml.Controls;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using UIKit;
#else
using View = Microsoft.UI.Xaml.UIElement;
using ViewGroup = Microsoft.UI.Xaml.UIElement;
using Uno.Foundation;
#endif

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Encapsulates the app and its available services.
	/// </summary>
	public partial class Application
	{
		private bool _initializationComplete;
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private ApplicationTheme _requestedTheme = ApplicationTheme.Dark; // Default theme in WinUI is Dark.
		private SpecializedResourceDictionary.ResourceKey _requestedThemeForResources;
		private bool _isInBackground;
		private ResourceDictionary _resources = new ResourceDictionary();

		static Application()
		{
			ApiInformation.RegisterAssembly(typeof(Application).Assembly);
			ApiInformation.RegisterAssembly(typeof(ApplicationData).Assembly);
			ApiInformation.RegisterAssembly(typeof(Microsoft.UI.Composition.Compositor).Assembly);

			Uno.Helpers.DispatcherTimerProxy.SetDispatcherTimerGetter(() => new DispatcherTimer());
			Uno.Helpers.VisualTreeHelperProxy.SetCloseAllFlyoutsAction(() =>
			{
				var contentRoots = WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots;
				foreach (var contentRoot in contentRoots)
				{
					Media.VisualTreeHelper.CloseAllFlyouts(contentRoot.XamlRoot);
				}
			});

			RegisterExtensions();

			InitializePartialStatic();
		}

		/// <summary>
		/// Initializes a new instance of the Application class.
		/// </summary>
		public Application()
		{
			CoreApplication.StaticInitialize();


#if __SKIA__ || __WASM__
			Package.SetEntryAssembly(this.GetType().Assembly);
#endif
			Current = this;
			ApplicationLanguages.ApplyCulture();

			InitializePartial();
		}

		internal bool InitializationComplete => _initializationComplete;

		partial void InitializePartial();

		private static void RegisterExtensions()
		{
			ApiExtensibility.Register<MessageDialog>(typeof(IMessageDialogExtension), dialog => new MessageDialogExtension(dialog));
#if __SKIA__
			ApiExtensibility.Register(typeof(Uno.UI.Graphics.SKCanvasVisualBaseFactory), _ => new Uno.UI.Graphics.SKCanvasVisualFactory());
#endif
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
			get => InternalRequestedTheme;
			set
			{
				if (_initializationComplete)
				{
					throw new NotSupportedException("Operation not supported");
				}
				SetExplicitRequestedTheme(value);
			}
		}

		internal bool IsSuspended { get; set; }

		private void InitializeSystemTheme()
		{
			if (!IsThemeSetExplicitly)
			{
				// just cache the theme, but do not notify about a change unnecessarily
				InternalRequestedTheme = GetSystemTheme();
			}
		}

		private ApplicationTheme InternalRequestedTheme
		{
			get => _requestedTheme;
			set
			{
				_requestedTheme = value;

				// Sync with core application's theme
				CoreApplication.RequestedTheme = value == ApplicationTheme.Dark ? SystemTheme.Dark : SystemTheme.Light;

				UpdateRootElementBackground();
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
			get => _requestedThemeForResources;
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

		internal void SyncRequestedThemeFromXamlRoot(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			// Sync the requested theme from the XamlRoot
			// This is an ultra-naive implementation... but nonetheless enables the common use case of overriding the system theme for
			// the entire visual tree (since Application.RequestedTheme cannot be set after launch)
			// This will also explicitly change the Application.Current.RequestedTheme, which does not happen in case of UWP.
			if (xamlRoot.Content is FrameworkElement fe)
			{
				var theme = fe.RequestedTheme;
				SetExplicitRequestedTheme(Uno.UI.Extensions.ElementThemeExtensions.ToApplicationThemeOrDefault(theme));
			}
		}

		internal void SetExplicitRequestedTheme(ApplicationTheme? explicitTheme)
		{
			// this flag makes sure the app will not respond to OS events
			IsThemeSetExplicitly = explicitTheme.HasValue;
			var theme = explicitTheme ?? GetSystemTheme();
			SetRequestedTheme(theme);
		}

		public ResourceDictionary Resources
		{
			get => _resources;
			set
			{
				_resources = value;
				_resources.InvalidateNotFoundCache(true);
			}
		}

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

#if !__ANDROID__ && !__SKIA__
		[NotImplemented("__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
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

		static partial void StartPartial(ApplicationInitializationCallback callback);

		protected internal virtual void OnActivated(IActivatedEventArgs args) { }

		protected internal virtual void OnLaunched(LaunchActivatedEventArgs args) { }

		internal void InitializationCompleted()
		{
			if (_initializationComplete)
			{
				// InitializationCompleted is currently called from NativeApplication.OnActivityStarted
				// and will be called every time the app is put to background then back to foreground.
				// Nothing in this method should really execute twice.
				return;
			}

			SystemThemeHelper.SystemThemeChanged += OnSystemThemeChanged;

			_initializationComplete = true;

#if !HAS_UNO_WINUI
			Microsoft.UI.Xaml.Window.EnsureWindowCurrent();
#endif

			// Initialize all windows that have been created before the application was initialized.
			foreach (var window in ApplicationHelper.Windows)
			{
				window.Initialize();
			}
		}

		internal void RaiseRecoverableUnhandledException(Exception e) => UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));

		private ApplicationTheme GetSystemTheme() =>
			SystemThemeHelper.SystemTheme == SystemTheme.Light ?
				ApplicationTheme.Light : ApplicationTheme.Dark;

		private void OnSystemThemeChanged(object sender, EventArgs e)
		{
			// if user overrides theme, don't apply system theme
			if (!IsThemeSetExplicitly)
			{
				var theme = GetSystemTheme();
				SetRequestedTheme(theme);
			}

			UISettings.OnColorValuesChanged();
		}

#if __WASM__ || __SKIA__
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
#endif

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
			if (!IsSuspended)
			{
				return;
			}

			Resuming?.Invoke(null, null);
			CoreApplication.RaiseResuming();
			IsSuspended = false;
		}

		internal void RaiseSuspending()
		{
			if (IsSuspended)
			{
				return;
			}

			var suspendingOperation = new SuspendingOperation(GetSuspendingOffset(), () => IsSuspended = true);
			var suspendingEventArgs = new SuspendingEventArgs(suspendingOperation);

			Suspending?.Invoke(this, suspendingEventArgs);
			CoreApplication.RaiseSuspending(suspendingEventArgs);
			var completedSynchronously = suspendingOperation.DeferralManager.EventRaiseCompleted();

#if !__APPLE_UIKIT__ && !__ANDROID__
			// Asynchronous suspension is not supported on all targets, warn the user
			if (!completedSynchronously && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					"This platform does not support asynchronous Suspending deferral. " +
					"Code executed after the of the method called by Suspending may not get executed.");
			}
#endif
		}

#if !__APPLE_UIKIT__ && !__ANDROID__
		/// <summary>
		/// On platforms which don't support asynchronous suspension we indicate that with immediate
		/// deadline and warning in logs.
		/// </summary>
		private DateTimeOffset GetSuspendingOffset() => DateTimeOffset.Now;
#endif

		private void SetRequestedTheme(ApplicationTheme requestedTheme)
		{
			if (requestedTheme != InternalRequestedTheme)
			{
				InternalRequestedTheme = requestedTheme;

				OnRequestedThemeChanged();
			}
		}

		internal void UpdateResourceBindingsForHotReload() => OnResourcesChanged(ResourceUpdateReason.HotReload);

		internal void OnRequestedThemeChanged()
		{
			RequestedThemeChanged?.Invoke();

			OnResourcesChanged(ResourceUpdateReason.ThemeResource);
		}

		internal event Action RequestedThemeChanged;

		private void UpdateRootElementBackground()
		{
			foreach (var contentRoot in WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots)
			{
				if (contentRoot.VisualTree.RootElement is IRootElement rootElement)
				{
					rootElement.SetBackgroundColor(ThemingHelper.GetRootVisualBackground());
				}
			}
		}

		private void OnResourcesChanged(ResourceUpdateReason updateReason)
		{
			try
			{
				// When we change theme, we may update properties and set them with Local precedence
				// with the newly evaluated ThemeResource value.
				// In this case, if we previously had Animation value in effect, we don't want the new Local value to take effect.
				// So, we avoid setting LocalValueNewerThanAnimationsValue
				ModifiedValue.SuppressLocalCanDefeatAnimations();
				DefaultBrushes.ResetDefaultThemeBrushes();
				foreach (var contentRoot in WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots)
				{
					if (GetTreeRoot(contentRoot) is { } root)
					{
						// Update theme bindings in application resources
						Resources?.UpdateThemeBindings(updateReason);

						// Update theme bindings in system resources
						ResourceResolver.UpdateSystemThemeBindings(updateReason);

						PropagateResourcesChanged(root, updateReason);
					}

					// Start from the real root, which may not be a FrameworkElement on some platforms
					View GetTreeRoot(ContentRoot contentRoot)
					{
						View current = contentRoot.XamlRoot.Content;
						var parent = current?.GetVisualTreeParent();
						while (parent != null)
						{
							current = parent;
							parent = current?.GetVisualTreeParent();
						}
						return current;
					}
				}
			}
			finally
			{
				ModifiedValue.ContinueLocalCanDefeatAnimations();
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
#if __ANDROID__
				// We need to propagate for list view items that were materialized but not visible.
				// Without this, theme changes will not propagate properly to all list view items.
				if (instance is NativeListViewBase nativeListViewBase)
				{
					foreach (var selectorItem in nativeListViewBase.CachedItemViews)
					{
						PropagateResourcesChanged(selectorItem, updateReason);
					}
				}
#endif
				foreach (object o in g.GetChildren())
				{
					PropagateResourcesChanged(o, updateReason);
				}
			}
		}

#if __SKIA__
		private static string GetCommandLineArgsWithoutExecutable()
		{
			if (!string.IsNullOrEmpty(_argumentsOverride))
			{
				return _argumentsOverride;
			}

			if (OperatingSystem.IsBrowser()) // Skia-WASM
			{
				return Uri.UnescapeDataString(new Uri(WebAssemblyImports.EvalString("window.location.href")).Query.TrimStart('?'));
			}
			else
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
#endif
	}
}
