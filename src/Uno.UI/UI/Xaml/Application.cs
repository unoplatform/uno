using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
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
using DirectUI;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

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
		private static readonly ConditionalWeakTable<AssemblyLoadContext, Application> _applicationsByAlc = new();
		private static readonly object _applicationsByAlcSync = new();
		private static Application _current;
		private static bool _hasSecondaryApps;

		/// <summary>
		/// Indicates whether the application has secondary Application instances running
		/// in separate AssemblyLoadContexts. When true, resource resolution will use
		/// ALC-aware lookup to ensure resources are resolved from the correct ALC.
		/// </summary>
		/// <remarks>
		/// This property must be set to true by the host application before loading any secondary ALCs
		/// to ensure ALC-aware resource registration works correctly. It is also automatically set
		/// when secondary ALC Application instances are registered via <see cref="RegisterApplication"/>.
		/// </remarks>
		internal static bool HasSecondaryApps
		{
			get => _hasSecondaryApps;
			set => _hasSecondaryApps = value;
		}

		private bool _initializationComplete;
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private ApplicationTheme _requestedTheme = ApplicationTheme.Dark; // Default theme in WinUI is Dark.
		private SpecializedResourceDictionary.ResourceKey _requestedThemeForResources;
		private bool _isInBackground;
		private ResourceDictionary _resources = new ResourceDictionary();
		private DispatcherShutdownMode _dispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;

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
			var isDefaultALC = AssemblyLoadContext.GetLoadContext(GetType().Assembly) == AssemblyLoadContext.Default;

			if (isDefaultALC)
			{
				CoreApplication.StaticInitialize();

#if __SKIA__ || __WASM__
				Package.SetEntryAssembly(this.GetType().Assembly);
#endif
				Current = this;
				ApplicationLanguages.ApplyCulture();

				BackButtonIntegration.Initialize();

				InitializePartial();
			}
			else
			{
				// We only need setup the app instance for non-default ALCs.
				Current = this;
			}
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

		public static Application Current
		{
			get => _current;
			private set => SetCurrentApplication(value);
		}

		public DebugSettings DebugSettings { get; } = new DebugSettings();

		public ApplicationRequiresPointerMode RequiresPointerMode { get; set; } = ApplicationRequiresPointerMode.Auto;

		/// <summary>
		/// Specifies the visual feedback used to indicate the UI element
		/// with focus when navigating with a keyboard or gamepad.
		/// </summary>
		public FocusVisualKind FocusVisualKind { get; set; } = FocusVisualKind.HighVisibility;

		/// <summary>
		/// Gets or sets a value that specifies whether the DispatcherQueue event loop exits
		/// when all XAML windows on a thread are closed.
		/// </summary>
		/// <remarks>
		/// When Application.Start is called, the XAML runtime sets this property to
		/// <see cref="DispatcherShutdownMode.OnLastWindowClose"/> for the current thread.
		/// If Application.Start is not called (as is typically the case for XAML Islands),
		/// this property defaults to <see cref="DispatcherShutdownMode.OnExplicitShutdown"/>.
		/// </remarks>
		public DispatcherShutdownMode DispatcherShutdownMode
		{
			get => _dispatcherShutdownMode;
			set => _dispatcherShutdownMode = value;
		}

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

		internal void InitializeSystemTheme()
		{
			if (!IsThemeSetExplicitly)
			{
				// just cache the theme, but do not notify about a change unnecessarily
				InternalRequestedTheme = GetSystemTheme();
			}

			// Force-sync ResourceDictionary's static Themes.Active with the application's
			// resolved theme before any resource lookup can happen at startup.
			//
			// Themes.Active is normally updated as a side-effect of the InternalRequestedTheme
			// setter (via UpdateRequestedThemesForResources). However, there are two startup
			// paths that can leave Themes.Active at its static default ("Default") while
			// Application.RequestedTheme reports a real theme:
			//
			//   1. App.xaml declares RequestedTheme="Dark" before this method runs.
			//      SetExplicitRequestedTheme(Dark) -> SetRequestedTheme(Dark) sees that the
			//      backing field already equals Dark (because _requestedTheme's field default
			//      is ApplicationTheme.Dark) and short-circuits as a no-op, never invoking
			//      the InternalRequestedTheme setter.
			//
			//   2. IsThemeSetExplicitly was already true when this method is reached, so the
			//      `if (!IsThemeSetExplicitly)` block above is skipped and the setter never runs.
			//
			// In both cases, ThemeDictionary lookups performed while loading Application.Resources
			// (and the first frames that reference ThemeResource keys) would hit GetActiveTheme()
			// returning "Default", resolving against the wrong sub-dictionary. For a dictionary
			// that only defines "Light" and "Dark" sub-dicts, this means the lookup misses
			// entirely (and potentially poisons KeyNotFoundCache).
			//
			// This unconditional call guarantees Themes.Active is coherent with
			// InternalRequestedTheme by the time the runtime starts consuming resources.
			UpdateRequestedThemesForResources();
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
			// The application theme is strictly Light/Dark; high contrast is a separate global dimension
			// composed at the resolution leaf (ResourceDictionary.GetActiveThemeDictionary). Matches WinUI,
			// which has no custom-theme-name axis (FrameworkTheming::GetTheme, FrameworkTheming.cpp:119-136).
			// A custom palette is supplied via merged ResourceDictionaries that override specific brush/color
			// keys on top of the Light/Dark theme dictionaries.
			Current.RequestedThemeForResources = Current.RequestedTheme switch
			{
				ApplicationTheme.Light => "Light",
				ApplicationTheme.Dark => "Dark",
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

		internal void SetExplicitRequestedTheme(ApplicationTheme? explicitTheme)
		{
			// this flag makes sure the app will not respond to OS events
			IsThemeSetExplicitly = explicitTheme.HasValue;
			var theme = explicitTheme ?? GetSystemTheme();
			SetRequestedTheme(theme);
		}

#if !UNO_HAS_ENHANCED_LIFECYCLE
		internal void SyncRequestedThemeFromXamlRoot(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			if (xamlRoot.Content is FrameworkElement fe)
			{
				var theme = fe.RequestedTheme;
				SetExplicitRequestedTheme(
					Uno.UI.Extensions.ElementThemeExtensions.ToApplicationThemeOrDefault(theme));
			}
		}
#endif

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
		public void Exit()
		{
			var alc = AssemblyLoadContext.GetLoadContext(GetType().Assembly);
			if (alc is not null && alc != AssemblyLoadContext.Default)
			{
				ExitAlcApplication();
				return;
			}

			CoreApplication.Exit();
		}
#endif

		public static void Start(global::Microsoft.UI.Xaml.ApplicationInitializationCallback callback)
		{
			StartPartial(p =>
			{
				callback(p);
				return null;
			});
		}

		/// <summary>
		/// Boots an <see cref="Application"/> instance when hosting scenarios require returning the created app
		/// (e.g., secondary AssemblyLoadContext projections).
		/// This overload mirrors <see cref="Microsoft.UI.Xaml.Application.Start(ApplicationInitializationCallback)"/>
		/// but allows the callback to return the constructed <see cref="Application"/> so the caller can capture it.
		/// </summary>
		/// <param name="callback">Factory invoked with initialization parameters; must return the created application instance.</param>
		internal static void Start(Func<ApplicationInitializationCallbackParams, Application> callback)
		{
			StartPartial(callback);
		}

		private static partial Application StartPartial(Func<ApplicationInitializationCallbackParams, Application> callback);

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

			InitializeTextScaling();

			_initializationComplete = true;


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

		private void InitializeTextScaling()
		{
			InitializeTextScalingPlatform();

			// Initialize platform subscriptions first, then read the initial value so
			// asynchronously discovered scales cannot be missed during startup.
			Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(UISettings.GetTextScaleFactorValue());
		}

		partial void InitializeTextScalingPlatform();

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

		internal void UpdateResourceBindingsForHotReload() => OnResourcesChanged(ResourceUpdateReason.HotReload | ResourceUpdateReason.ThemeResource);

		// MUX Reference: FrameworkTheming::IsBaseThemeChanging — true while a base (app/system) theme switch
		// is being applied. CFrameworkElement::NotifyThemeChangedCore uses it so an element that has not yet
		// been theme-walked (m_theme == None) still raises ActualThemeChanged on the switch (framework.cpp
		// RaiseActualThemeChangedEventIfChanging). Set only around the base-theme walk below; hot reload goes
		// through OnResourcesChanged directly and never sets it.
		internal static bool IsBaseThemeChanging { get; private set; }

		internal void OnRequestedThemeChanged()
		{
			RequestedThemeChanged?.Invoke();

			var wasBaseThemeChanging = IsBaseThemeChanging;
			IsBaseThemeChanging = true;
			try
			{
				OnResourcesChanged(ResourceUpdateReason.ThemeResource);
			}
			finally
			{
				IsBaseThemeChanging = wasBaseThemeChanging;
			}
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

						// When the application theme changes, notify the visual tree to update
						// stored per-object themes (DependencyObjectStore._theme). PropagateResourcesChanged only
						// updates theme bindings but not stored themes, which causes new elements
						// entering the tree (via OnLoadingPartial) to inherit stale themes from
						// their parent.
#if UNO_HAS_ENHANCED_LIFECYCLE
						if ((updateReason & ResourceUpdateReason.ThemeResource) != 0)
						{
							// Re-apply the theme of the application that OWNS this content root, not `this`.
							// The content-root list is process-global; when apps in multiple AssemblyLoadContexts
							// share it (e.g. a host rendering a consumer app in a secondary ALC), a secondary
							// app's resource refresh — such as its hot-reload pass — must not bleed its own theme
							// onto the host's, or another app's, visual tree. Falls back to `this` when the owner
							// is indeterminate, and avoids the owning-app lookup when there are no secondary apps.
							var owningApp = (HasSecondaryApps ? GetOwningApplication(contentRoot) : null) ?? this;
							var theme = owningApp.InternalRequestedTheme == ApplicationTheme.Dark ? Theme.Dark : Theme.Light;
							var forceRefresh = (updateReason & ResourceUpdateReason.HotReload) != 0;
							var rootFe = root as FrameworkElement ?? contentRoot.XamlRoot.Content as FrameworkElement;
							rootFe?.NotifyThemeChanged(theme, forceRefresh);
						}
#endif

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
#if UNO_HAS_ENHANCED_LIFECYCLE
				// If element has explicit RequestedTheme and this is a theme change,
				// skip it - its subtree is managed by its own theme context and
				// will be updated via NotifyThemeChanged, not by propagation.
				if ((updateReason & ResourceUpdateReason.ThemeResource) != 0 &&
					fe.RequestedTheme != ElementTheme.Default)
				{
					return;
				}
#endif

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
