using System;
using Uno;
using Uno.UI;
using Uno.Diagnostics.Eventing;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Uno.Helpers.Theming;
using Windows.UI.ViewManagement;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

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
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		private bool _initializationComplete = false;
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private bool _themeSetExplicitly = false;
		private ApplicationTheme? _requestedTheme;
		private bool _systemThemeChangesObserved = false;
		private SpecializedResourceDictionary.ResourceKey _requestedThemeForResources;
		private bool _isInBackground = false;

		static Application()
		{
			ApiInformation.RegisterAssembly(typeof(Application).Assembly);
			ApiInformation.RegisterAssembly(typeof(Windows.Storage.ApplicationData).Assembly);

			InitializePartialStatic();
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
		/// Does not have any effect in Uno yet.
		/// </summary>
		[NotImplemented]
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

		internal ElementTheme ActualElementTheme => (_themeSetExplicitly, RequestedTheme) switch
		{
			(true, ApplicationTheme.Light) => ElementTheme.Light,
			(true, ApplicationTheme.Dark) => ElementTheme.Dark,
			_ => ElementTheme.Default
		};

		internal void SetExplicitRequestedTheme(ApplicationTheme? explicitTheme)
		{
			// this flag makes sure the app will not respond to OS events
			_themeSetExplicitly = explicitTheme.HasValue;
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
			if (!_themeSetExplicitly)
			{
				var theme = GetDefaultSystemTheme();
				SetRequestedTheme(theme);
			}

			UISettings.OnColorValuesChanged();
		}

#if !__ANDROID__ && !__MACOS__ && !__SKIA__
		[NotImplemented]
		public void Exit()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("This platform does not support application exit.");
			}
		}
#endif

		public static void Start(global::Windows.UI.Xaml.ApplicationInitializationCallback callback)
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
					new object[] { }
				);
			}
			else
			{
				return null;
			}
		}

		internal void OnEnteredBackground()
		{
			if (!_isInBackground)
			{
				_isInBackground = true;
				EnteredBackground?.Invoke(this, new EnteredBackgroundEventArgs());
			}
		}

		internal void OnLeavingBackground()
		{
			if (_isInBackground)
			{
				_isInBackground = false;
				LeavingBackground?.Invoke(this, new LeavingBackgroundEventArgs());
			}
		}

		internal void OnResuming()
		{
			CoreApplication.RaiseResuming();

			OnResumingPartial();
		}

		partial void OnResumingPartial();

		internal void OnSuspending()
		{
			var suspendingEventArgs = new SuspendingEventArgs(new SuspendingOperation(DateTime.Now.AddSeconds(30)));
			CoreApplication.RaiseSuspending(suspendingEventArgs);

			OnSuspendingPartial();
		}

		partial void OnSuspendingPartial();

		protected virtual void OnWindowCreated(global::Windows.UI.Xaml.WindowCreatedEventArgs args)
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

		private void OnRequestedThemeChanged()
		{
			if (GetTreeRoot() is { } root)
			{
				// Update theme bindings in application resources
				Resources?.UpdateThemeBindings();

				// Update theme bindings in system resources
				ResourceResolver.UpdateSystemThemeBindings();

				PropagateThemeChanged(root);
			}

			// Start from the real root, which may not be a FrameworkElement on some platforms
			View GetTreeRoot()
			{
				View current = Windows.UI.Xaml.Window.Current.Content;
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
		internal static void PropagateThemeChanged(object instance)
		{

			// Update ThemeResource references that have changed
			if (instance is FrameworkElement fe)
			{
				fe.UpdateThemeBindings();
			}

			//Try Panel.Children before ViewGroup.GetChildren - this results in fewer allocations
			if (instance is Controls.Panel p)
			{
				foreach (object o in p.Children)
				{
					PropagateThemeChanged(o);
				}
			}
			else if (instance is ViewGroup g)
			{
				foreach (object o in g.GetChildren())
				{
					PropagateThemeChanged(o);
				}
			}
		}
	}
}
