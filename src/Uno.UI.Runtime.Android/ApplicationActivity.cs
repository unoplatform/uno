using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Activity;
using AndroidX.Core.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime;
using Uno.UI.Runtime.Android;
using Uno.UI.Xaml.Controls;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;


namespace Uno.UI.Runtime.Android
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public partial class ApplicationActivity : NativePage
	{
		private IUnoRenderView? _renderView;
		private View? _renderViewAsView;
		private ClippedRelativeLayout? _nativeLayerHost;

		internal IUnoRenderView? RenderView => _renderView;

		private InputPane _inputPane;

		private bool _started;
		private bool _resumed;
		private bool _isContentViewSet;

		private NativeWindowWrapper? _wrapper;

		private const string WindowIdExtra = "__uno_window_id";
		private static int _nextWindowId;
		private static readonly ConcurrentDictionary<int, NativeWindowWrapper> _pendingWindows = new();

		/// <summary>
		/// Asks Android for a task to host <paramref name="wrapper"/>'s window. The wrapper is parked
		/// until the activity Android launches picks it up by id in <see cref="Wrapper"/>.
		/// </summary>
		/// <remarks>
		/// NewDocument|MultipleTask is what puts the activity in a task of its own, which is what makes
		/// it a separate window: the user can then place it side by side from the recents list.
		/// </remarks>
		internal static void LaunchForWindow(NativeWindowWrapper wrapper)
		{
			if (BaseActivity.Current is not { } launcher)
			{
				throw new InvalidOperationException("A secondary window can only be opened while an activity is running.");
			}

			var id = Interlocked.Increment(ref _nextWindowId);
			_pendingWindows[id] = wrapper;

			// Same activity class as the one already running: the app declares exactly one, and a
			// second instance of it is what hosts the second window.
			var intent = new Intent(launcher, launcher.GetType());
			intent.AddFlags(global::Android.Content.ActivityFlags.NewDocument | global::Android.Content.ActivityFlags.MultipleTask);
			intent.PutExtra(WindowIdExtra, id);

			launcher.StartActivity(intent);
		}

		/// <summary>
		/// Withdraws a window closed before Android started its activity. The activity may still start;
		/// it then finds nothing to adopt and finishes.
		/// </summary>
		internal static void CancelLaunch(NativeWindowWrapper wrapper)
		{
			foreach (var entry in _pendingWindows)
			{
				if (ReferenceEquals(entry.Value, wrapper))
				{
					_pendingWindows.TryRemove(entry.Key, out _);
				}
			}
		}

		/// <summary>
		/// The native wrapper for the window this activity drives. Created lazily so the early
		/// lifecycle callbacks (which run before the managed Window exists) can drive it. On
		/// activity re-creation the wrapper already bound to the window is reused and re-pointed
		/// at this activity, since the managed Window outlives individual activities.
		/// </summary>
		internal NativeWindowWrapper Wrapper
		{
			get
			{
				if (_wrapper is null)
				{
					_wrapper = ResolveWrapper();
					_wrapper.CurrentActivity = this;
				}

				return _wrapper;
			}
		}

		private NativeWindowWrapper ResolveWrapper()
		{
			// Launched to host a specific window: take the wrapper parked for it. Consumed by id so a
			// re-created activity keeps the same window rather than claiming a new one.
			if (Intent?.GetIntExtra(WindowIdExtra, 0) is > 0 and var windowId)
			{
				if (_pendingWindows.TryRemove(windowId, out var pending))
				{
					_adoptedWindowId = windowId;
					_adoptedWindows[windowId] = pending;
					return pending;
				}

				if (_adoptedWindows.TryGetValue(windowId, out var adopted))
				{
					// Re-created (configuration change, process restore): re-point the window's
					// existing wrapper at this activity instead of building a second one.
					_adoptedWindowId = windowId;
					return adopted;
				}

				// A window this process does not know: it was closed before its activity started, or
				// Android restored the task after the process died. The latter can host the main window
				// while nothing else does, but must not take it from a live activity: two activities
				// would then drive one window.
				if (GetMainWrapper() is { } main && IsDrivenByAnotherLiveActivity(main))
				{
					_isOrphaned = true;
					return new NativeWindowWrapper();
				}
			}

			// The activity that started the app: it drives the main window, whose wrapper it created
			// before any managed Window existed, and re-adopts across re-creation.
			return GetMainWrapper() ?? new NativeWindowWrapper(this);
		}

		private static NativeWindowWrapper? GetMainWrapper()
			=> Microsoft.UI.Xaml.Window.CurrentSafe?.NativeWrapper as NativeWindowWrapper;

		private bool IsDrivenByAnotherLiveActivity(NativeWindowWrapper wrapper)
			=> wrapper.CurrentActivity is { } activity
				&& !ReferenceEquals(activity, this)
				&& !activity.IsDestroyed
				&& !activity.IsFinishing;

		// Hosts no window and finishes as soon as it is created.
		private bool _isOrphaned;

		private int _adoptedWindowId;
		private static readonly ConcurrentDictionary<int, NativeWindowWrapper> _adoptedWindows = new();

		private bool IsMainWindowActivity => _wrapper is { IsMainWindow: true };

		/// <summary>
		/// The root element of the window hosted by this activity, once the window has been created.
		/// </summary>
		internal UIElement? RootElement => _wrapper?.Window?.RootElement;

		private protected override void OnNativeActivationChanged(global::Windows.UI.Core.CoreWindowActivationState state)
			=> Wrapper.OnNativeActivated(state);

		private protected override void OnNativeVisibilityChanged(bool isVisible)
			=> Wrapper.OnNativeVisibilityChanged(isVisible);

		/// <summary>
		/// False once another activity has taken over this activity's window. An outgoing activity
		/// still runs OnPause/OnStop after its replacement resumed, and forwarding those would
		/// deactivate and background the window the replacement now drives.
		/// </summary>
		private protected override bool IsDrivingWindow => DrivesWindow;

		/// <inheritdoc cref="IsDrivingWindow"/>
		internal bool DrivesWindow => _wrapper is null || ReferenceEquals(_wrapper.CurrentActivity, this);

		internal RelativeLayout RelativeLayout { get; private set; } = null!;

		private LayoutProvider? _layoutProvider;

		// Lazy: a recreated Activity that keeps its window (e.g. Activity.Recreate) attaches the reused content,
		// and so starts the provider, from InitializeComponent, before OnCreate would have created it.
		internal LayoutProvider LayoutProvider => _layoutProvider ??= CreateLayoutProvider();

		internal ClippedRelativeLayout? NativeLayerHost => _nativeLayerHost;

		public ApplicationActivity(IntPtr ptr, JniHandleOwnership owner) : base(ptr, owner)
		{
			Initialize();
		}

		public ApplicationActivity()
		{
			Initialize();
		}

		[MemberNotNull(nameof(_inputPane))]
		private void Initialize()
		{
			_inputPane = InputPane.GetForCurrentView();
			_inputPane.Showing += OnInputPaneVisibilityChanged;
			_inputPane.Hiding += OnInputPaneVisibilityChanged;
			Uno.UI.Extensions.PermissionsHelper.Initialize();
		}

		internal void EnsureContentView()
		{
			if (_isContentViewSet)
			{
				return;
			}

			SetContentView(RelativeLayout);
			_isContentViewSet = true;
		}

		public override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Cannot call this in ctor: see
			// https://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview#10603714
			RaiseConfigurationChanges();

			// OnAttachedToWindow can run more than once per activity, so keep the subscription single.
			var orientationSensor = SimpleOrientationSensor.GetDefault()!;
			orientationSensor.OrientationChanged -= OnSensorOrientationChanged;
			orientationSensor.OrientationChanged += OnSensorOrientationChanged;

			// Note: Deep-linking will cause a new instance of this Activity and its DecorView to be created.
			// This means any event handlers or listeners attached to these objects in previous instances will not be present.
			// Therefore, it is important to rewire or update any event/listener on these two here to ensure correct behavior.
			StatusBar.GetForCurrentView().ResetListener();
		}

		private void OnSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
		{
			NativeDispatcher.Main.Enqueue(RaiseConfigurationChanges);
		}

		private void OnInputPaneVisibilityChanged(InputPane sender, InputPaneVisibilityEventArgs args)
		{
		}

		// Content attach and reactivation on activity re-creation happen in OnStart, once this
		// activity has built its own render surface.
		protected override void InitializeComponent()
		{
		}

		public override bool DispatchKeyEvent(KeyEvent? e)
		{
			if (e is null)
			{
				return base.DispatchKeyEvent(e);
			}

			var handled = Wrapper.KeyboardSource.OnNativeKeyEvent(e);

			if (!handled)
			{
				handled = base.DispatchKeyEvent(e);
			}

			return handled;
		}


		private readonly int[] _locationInWindow = new int[2];

		public override bool DispatchGenericMotionEvent(MotionEvent? ev)
		{
			if (ev is null)
			{
				// Can this happen? Is Xamarin nullability annotation wrong?
				return base.DispatchGenericMotionEvent(ev);
			}

			// TalkBack's touch exploration turns finger touches into hover probes, which only the view tree routes to the
			// render view's accessibility helper. They are screen reader gestures, never XAML pointer input.
			if (IsTouchExplorationHover(ev))
			{
				base.DispatchGenericMotionEvent(ev);
				return true;
			}

			var nativelyHandled = false;
			if (_nativeLayerHost?.Path?.FillContains(new global::System.Numerics.Vector2(ev.GetX(), ev.GetY())) ?? false)
			{
				// We don't call the base method if NativeLayerHost.Path doesn't contain (X, Y).
				// This is due to the way Android handles hit-testing with Canvas.ClipPath, where even if the ClipPath
				// doesn't contain the coordinates of a touch event, it will still hit-test positively as if its clip
				// path contains the coordinates. So, we have to do our own hit-testing step where we prevent dispatching
				// the event altogether if it's not within the clip path of the native layer.
				nativelyHandled = base.DispatchTouchEvent(ev);
			}

			_renderViewAsView?.GetLocationInWindow(_locationInWindow);
			Wrapper.PointerSource.OnNativeMotionEvent(ev, _locationInWindow, nativelyHandled);

			// As the AndroidCorePointerInputSource can dispatch event asynchronously, we always return true to prevent the system from dispatching the event
			// as we assume that anyway we are the fully opaque (i.e. the pointer should not be dispatch to any element under this current ApplicationActivity).
			return true;
		}

		private bool IsTouchExplorationHover(MotionEvent ev)
			=> ev.ActionMasked is MotionEventActions.HoverEnter or MotionEventActions.HoverMove or MotionEventActions.HoverExit
				&& ev.IsFromSource(InputSourceType.Touchscreen)
				&& ev.GetToolType(0) == MotionEventToolType.Finger
				&& _renderView?.ExploreByTouchHelper.IsTouchExplorationEnabled is true;

		public override bool DispatchTouchEvent(MotionEvent? ev)
		{
			if (ev is null)
			{
				// Can this happen? Is Xamarin nullability annotation wrong?
				return base.DispatchTouchEvent(ev);
			}

			var nativelyHandled = false;
			if (_nativeLayerHost?.Path?.FillContains(new global::System.Numerics.Vector2(ev.GetX(), ev.GetY())) ?? false)
			{
				// We don't call the base method if NativeLayerHost.Path doesn't contain (X, Y).
				// This is due to the way Android handles hit-testing with Canvas.ClipPath, where even if the ClipPath
				// doesn't contain the coordinates of a touch event, it will still hit-test positively as if its clip
				// path contains the coordinates. So, we have to do our own hit-testing step where we prevent dispatching
				// the event altogether if it's not within the clip path of the native layer.
				nativelyHandled = base.DispatchTouchEvent(ev);
			}

			_renderViewAsView?.GetLocationInWindow(_locationInWindow);
			Wrapper.PointerSource.OnNativeMotionEvent(ev, _locationInWindow, nativelyHandled);

			// As the AndroidCorePointerInputSource can dispatch event asynchronously, we always return true to prevent the system from dispatching the event
			// as we assume that anyway we are the fully opaque (i.e. the pointer should not be dispatch to any element under this current ApplicationActivity).
			return true;
		}

		public void DismissKeyboard()
		{
			var windowToken = CurrentFocus?.WindowToken;

			if (windowToken != null)
			{
				var inputManager = (InputMethodManager)GetSystemService(InputMethodService)!;
				inputManager.HideSoftInputFromWindow(windowToken, HideSoftInputFlags.None);
			}
		}

		public void SetOrientation(ScreenOrientation orientation)
		{
			RequestedOrientation = orientation;
		}

		public void ExitFullscreen()
		{
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			Window!.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			Window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
			Window.ClearFlags(WindowManagerFlags.Fullscreen);
		}

		private void OnKeyboardChanged(Rect keyboard)
		{
			Wrapper.RaiseNativeSizeChanged();
			_inputPane.OccludedRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
		}

		protected override void OnCreate(Bundle? bundle)
		{
			// Once the app targets SDK 35+, edge-to-edge is enforced.
			// Calling EdgeToEdge.Enable keeps this behavior consistent on earlier SDK levels too.
			EdgeToEdge.Enable(this);

			base.OnCreate(bundle);

			_ = Wrapper;
			if (_isOrphaned)
			{
				FinishAndRemoveTask();
				return;
			}

			Wrapper.OnActivityCreated();

			// Track and observe this activity's window system UI visibility on its per-window wrapper.
			var decorView = this.Window!.DecorView;
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			Wrapper.SystemUiVisibility = (int)decorView.SystemUiVisibility;
			decorView.SetOnSystemUiVisibilityChangeListener(new OnSystemUiVisibilityChangeListener(this));
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			// Hold the window's draws until a Skia frame is presented (see the render views).
			Wrapper.ArmFirstFrameGate();

			_ = LayoutProvider;

			RaiseConfigurationChanges();

			InitializeBackPressedCallback();
		}

		private LayoutProvider CreateLayoutProvider()
		{
			LayoutProvider layoutProvider = new(this);
			layoutProvider.KeyboardChanged += OnKeyboardChanged;
			layoutProvider.InsetsChanged += OnInsetsChanged;
			return layoutProvider;
		}

		protected override void OnStart()
		{
			// The layouts must exist before base.OnStart(): that call synchronously reaches
			// Application.Start -> OnLaunched -> CreateWindow, after which the host is registered
			// and InvalidateRender() can run against RelativeLayout. This state is per-activity, so
			// unlike the previous process-wide stack it is null again on every re-creation.
			var isFirstStart = !_started;
			if (isFirstStart)
			{
				_started = true;
				RelativeLayout = new RelativeLayout(this);
				RelativeLayout.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);

				_nativeLayerHost = new ClippedRelativeLayout(this);
				_nativeLayerHost.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				RelativeLayout.AddView(NativeLayerHost);
			}

			base.OnStart();

			if (isFirstStart)
			{
				// Picked only after base.OnStart(): on first launch that call builds the host, which
				// registers the drawing backend and applies the builder's rendering options.
				_renderView = CreateRenderView();
				_renderViewAsView = (View)_renderView;
				_renderViewAsView.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);

				// Index 0 keeps it under the native layer host.
				RelativeLayout.AddView(_renderViewAsView, 0);

				// A XamlRoot host registered before OnStart can only attach its accessibility adapter
				// once this activity's render view exists.
				ConfigureAccessibilityHelpers();
			}
			else
			{
				TryReclaimWindow();
			}

			// The window was handed over in OnCreate, before this render view existed; state bound to
			// it, such as an IME session's plugin, can only rebind now.
			_wrapper?.NotifyDrivingActivityReady();

			// A secondary window is activated before Android has given it an activity, so its show
			// was deferred until one existed with a render stack to attach to. That is now.
			if (Wrapper.CompleteDeferredShow()
				&& Wrapper.XamlRoot is { } shownXamlRoot
				&& _nativeLayerHost is { } layerHost)
			{
				AndroidSkiaNativeElementHostingExtension.AdoptNativeElements(shownXamlRoot, layerHost);
			}

			// On activity re-creation (deep-link, process restore) the managed Window already
			// exists with its content loaded, but CreateWindow won't run again for this new
			// activity. Attach this activity's freshly-built surface and reactivate the window.
			if (!_isContentViewSet && Wrapper.Window is { RootElement: not null } existingWindow)
			{
				EnsureContentView();
				AdoptNativeElements(existingWindow);

				existingWindow.Activate();
				InvalidateRender();
			}
		}

		/// <summary>
		/// Called from the render thread once this activity's render view has negotiated a renderer. Any record the
		/// window attempted while it had none (between a takeover and the new surface) was skipped.
		/// </summary>
		internal void OnRendererAvailable()
			=> NativeDispatcher.Main.Enqueue(() =>
			{
				if (DrivesWindow && RootElement?.Visual.CompositionTarget is Uno.UI.Composition.ICompositionTarget compositionTarget)
				{
					// The skipped record already cleared the target's request, so a frame alone would record nothing.
					compositionTarget.RequestNewFrame();
					// A request still latched for the previous view absorbs the one above without asking this one.
					InvalidateRender();
				}
			});

		/// <summary>
		/// Takes the window back from a copy of this activity that an intent stacked on top in the standard launch
		/// mode, once that copy is finishing, rather than let the window close with it. Runs from OnStart, and from
		/// OnResume for a copy finished before this activity was stopped. Once resumed, it also takes the window from
		/// a copy that is not, such as one left in another task when the user switched back here through Recents.
		/// </summary>
		private void TryReclaimWindow(bool resuming = false)
		{
			if (!_started || _wrapper is not { CurrentActivity: { } owner } wrapper || ReferenceEquals(owner, this))
			{
				return;
			}

			if (owner.IsFinishing || (resuming && !owner._resumed))
			{
				TakeWindow(wrapper);
			}
		}

		private void TakeWindow(NativeWindowWrapper wrapper)
		{
			wrapper.CurrentActivity = this;
			ConfigureAccessibilityHelpers();
			AdoptNativeElements(wrapper.Window);
			InvalidateRender();
		}

		/// <summary>
		/// The most recently active copy of this activity, other than this one, still alive and sharing <paramref name="wrapper"/>.
		/// </summary>
		private ApplicationActivity? FindLiveSibling(NativeWindowWrapper wrapper)
			=> Instances.Values
				.OfType<ApplicationActivity>()
				.Where(activity => !ReferenceEquals(activity, this)
					&& ReferenceEquals(activity._wrapper, wrapper)
					&& !activity.IsDestroyed
					&& !activity.IsFinishing)
				.MaxBy(activity => activity.LastActivation);

		private void ConfigureAccessibilityHelpers()
		{
			foreach (var pair in XamlRootMap.Enumerate())
			{
				if (pair.Value is AndroidSkiaXamlRootHost androidHost && ReferenceEquals(androidHost.Activity, this))
				{
					androidHost.TryConfigureHelper();
				}
			}
		}

		private void AdoptNativeElements(Microsoft.UI.Xaml.Window? window)
		{
			if (window?.RootElement?.XamlRoot is { } xamlRoot && _nativeLayerHost is { } nativeLayerHost)
			{
				AndroidSkiaNativeElementHostingExtension.AdoptNativeElements(xamlRoot, nativeLayerHost);
			}
		}

		private IUnoRenderView CreateRenderView(bool allowWebGpu = true)
		{
			// Pick the Android view class matching the registered backend's context kind (host reads the neutral kind).
			if (allowWebGpu && global::Uno.UI.Composition.Drawing.GraphicsRegistry.HasBackendPreferring(
				global::Uno.UI.Composition.Drawing.GraphicsContextKind.WebGpu))
			{
				try
				{
					return new UnoWebGpuView(this);
				}
				catch (Exception ex)
				{
					typeof(ApplicationActivity).Log().Warn($"WebGPU rendering not available: {ex.Message}. Falling back.");
				}
			}

			// Default (UseVulkan): Vulkan when supported, else the canvas view. Vulkan can fail at runtime, so a
			// failed bring-up falls through to the canvas view rather than aborting.
			if (FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid)
			{
				// Gate on the declared hardware feature first: without it, loading libvulkan.so and driving device
				// creation can abort inside the driver, which is not a catchable managed exception.
				if (!PackageManager?.HasSystemFeature(PackageManager.FeatureVulkanHardwareLevel) ?? true)
				{
					typeof(ApplicationActivity).Log().Warn("Device does not support Vulkan. Falling back to OpenGL ES.");
				}
				else
				{
					// Vulkan feature flags are static device configuration and can be declared even when the driver
					// cannot actually render (common on emulators) — the view constructor creates the Vulkan device
					// and throws when the driver is unusable.
					try
					{
						return new UnoVulkanView(this);
					}
					catch (Exception ex)
					{
						typeof(ApplicationActivity).Log().Warn($"Vulkan rendering not available: {ex.Message}. Falling back.");
					}
				}
			}

			// The canvas view renders GLES or software per UseOpenGLOnSkiaAndroid (chosen in its OnSurfaceCreated).
			return new UnoCanvasView(this);
		}

		/// <summary>
		/// Replaces a render view whose GPU backend failed to initialize on its own render thread.
		/// <see cref="CreateRenderView"/>'s try/catch only covers the view constructor, so without this a failed
		/// negotiation leaves a dead render thread and a permanently black window. A failed WebGPU view goes through
		/// the rest of the view chain (Vulkan, then canvas), so a registered fallback backend gets the same view a
		/// non-WebGPU app would; any other failed view gets the GL/canvas one.
		/// </summary>
		internal void ReplaceFailedRenderView(IUnoRenderView failedView)
		{
			// A null layout means OnDestroy already tore the stack down.
			var layout = RelativeLayout;
			if (layout is null || failedView is UnoCanvasView)
			{
				return;
			}

			RunOnUiThread(() =>
			{
				if (!ReferenceEquals(_renderView, failedView))
				{
					return;
				}

				var replacement = failedView is UnoWebGpuView ? CreateRenderView(allowWebGpu: false) : new UnoCanvasView(this);
				typeof(ApplicationActivity).Log().Warn($"Falling back to the {replacement.GetType().Name} render view.");

				if (_renderViewAsView is { } failed)
				{
					layout.RemoveView(failed);
					// Deferred: removing the view still delivers surfaceDestroyed to it, and disposing the managed
					// peer before that callback lands faults inside it.
					layout.Post(() => failed.Dispose());
				}

				var replacementView = (View)replacement;
				replacementView.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				_renderView = replacement;
				_renderViewAsView = replacementView;

				// Index 0 keeps it under the native layer host, as in OnStart.
				layout.AddView(replacementView, 0);

				InvalidateRender();
			});
		}

		internal void InvalidateRender()
		{
			// The wrapper resolves this activity from OnCreate onwards, so a render request can
			// reach it before OnStart has built the render stack this invalidates.
			if (!_started)
			{
				return;
			}

			_renderView?.InvalidateRender();
		}

		private void OnInsetsChanged(Thickness insets)
		{
			Wrapper.RaiseNativeSizeChanged();
		}

		public override void SetContentView(View? view)
		{
			IsContentViewAttachedToWindow = false;

			if (view != null)
			{
				if (view.IsAttachedToWindow)
				{
					LayoutProvider.Start(view);
					RaiseContentViewAttachedToWindow();
				}
				else
				{
					EventHandler<View.ViewAttachedToWindowEventArgs>? handler = null;
					handler = (s, e) =>
					{
						LayoutProvider.Start(view);
						RaiseContentViewAttachedToWindow();
						view.ViewAttachedToWindow -= handler;
					};
					view.ViewAttachedToWindow += handler;
				}
			}

			base.SetContentView(view);
		}

		private void RaiseContentViewAttachedToWindow()
		{
			IsContentViewAttachedToWindow = true;
			ContentViewAttachedToWindow?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Whether this activity's content view is attached to the native window. The wrapper's
		/// pre-draw gate needs the state and not just <see cref="ContentViewAttachedToWindow"/>,
		/// because the attach can happen before the wrapper subscribes.
		/// </summary>
		internal bool IsContentViewAttachedToWindow { get; private set; }

		internal event EventHandler? ContentViewAttachedToWindow;

		protected override void OnResume()
		{
			_resumed = true;
			TryReclaimWindow(resuming: true);

			base.OnResume();

			RaiseConfigurationChanges();

			// Activate accessibility routing for the foreground window.
			foreach (var pair in XamlRootMap.Enumerate())
			{
				if (pair.Value is IAccessibilityOwner { Accessibility: { } } owner)
				{
					AccessibilityRouter.SetActive(owner);
					break;
				}
			}

			//WebAuthenticationBroker.OnResume();
		}

		protected override void OnPause()
		{
			_resumed = false;
			base.OnPause();

			// Only this window's popups: other windows can stay resumed while this one pauses.
			if (RootElement?.XamlRoot is { } xamlRoot)
			{
				VisualTreeHelper.CloseLightDismissPopups(xamlRoot);
			}

			DismissKeyboard();
		}

		protected override void OnDestroy()
		{
			// The render stack is per-activity and the peer finalizer never runs the managed
			// dispose path, so the GL/Vulkan context has to be released explicitly. This must
			// precede base.OnDestroy(): NativePage detaches the content view there, which stops
			// the GL thread the teardown is queued on.
			try
			{
				_renderView?.TeardownRenderer();
			}
			catch (Exception ex)
			{
				// base.OnDestroy() and the unsubscriptions below must still run, or this activity leaks.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to tear down the render view.", ex);
				}
			}

			_renderView = null;
			_renderViewAsView = null;
			_nativeLayerHost = null;

			base.OnDestroy();

			// Never created for an activity that finished from OnCreate.
			if (_layoutProvider is { } layoutProvider)
			{
				layoutProvider.Stop();
				layoutProvider.KeyboardChanged -= OnKeyboardChanged;
				layoutProvider.InsetsChanged -= OnInsetsChanged;
			}

			// These are subscribed on process-wide singletons, so a missing -= keeps this activity
			// (and its render stack) alive for the life of the process, once per re-creation.
			_inputPane.Showing -= OnInputPaneVisibilityChanged;
			_inputPane.Hiding -= OnInputPaneVisibilityChanged;
			SimpleOrientationSensor.GetDefault()!.OrientationChanged -= OnSensorOrientationChanged;

			CleanupBackPressedCallback();

			// Only a finishing activity that still owns the window closes it. The system also destroys
			// activities it re-creates later (configuration changes, reclaiming memory), and in the
			// StartActivity/Finish restart idiom the successor has already taken the wrapper.
			if (IsFinishing && _wrapper is { } wrapper && ReferenceEquals(wrapper.CurrentActivity, this))
			{
				// A copy still alive that did not reclaim the window first, such as one in another task that won't
				// come back to the foreground, takes it over rather than let it close.
				if (FindLiveSibling(wrapper) is { } sibling)
				{
					sibling.TakeWindow(wrapper);
				}
				else
				{
					wrapper.OnNativeClosed();

					// The window and its host outlive this activity: the next one adopts them in OnStart.
					// Until then the wrapper, which assumes it always has an activity, keeps pointing at this one,
					// so AndroidSkiaXamlRootHost.GetActivity filters out destroyed activities.
					wrapper.XamlRootHost?.DetachRenderView();

					// The window is gone with its task, so stop holding its wrapper for re-adoption.
					if (_adoptedWindowId is > 0 and var windowId)
					{
						_adoptedWindows.TryRemove(windowId, out _);
					}
				}
			}
		}

		public override void OnConfigurationChanged(Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);

			RaiseConfigurationChanges();
		}

		private void RaiseConfigurationChanges()
		{
			Wrapper.RaiseNativeSizeChanged();
			//ViewHelper.RefreshFontScale();
			DisplayInformation.GetForCurrentView().HandleConfigurationChange();
			SystemThemeHelper.RefreshSystemTheme();

			// Android has no accent change notification; a Material You palette change recreates or
			// resumes the activity, so re-read the accent here.
			AccentColorHelper.RefreshAccentColor();
		}

#pragma warning disable CS0618 // deprecated members
#pragma warning disable CS0672 // deprecated members
		public override void OnBackPressed()
		{
			// SystemNavigationManager is process-wide and its handlers belong to the main window, so a
			// secondary window keeps the system behavior: back closes it.
			var handled = IsMainWindowActivity
				&& global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
			if (!handled)
			{
#pragma warning disable CA1422 // Validate platform compatibility
				base.OnBackPressed();
#pragma warning restore CA1422 // Validate platform compatibility
			}
		}
#pragma warning restore CS0618 // deprecated members
#pragma warning restore CS0672 // deprecated members

		protected override void OnNewIntent(Intent? intent)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"New application activity intent received, data: {intent?.Data?.ToString() ?? "(null)"}");
			}
			base.OnNewIntent(intent);
			if (intent != null)
			{
				this.Intent = intent;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Application activity intent updated. Attempting to handle intent.");
				}

				// In case this activity is in SingleTask mode, we try to handle
				// the intent (for protocol activation scenarios).
				var handled = (Application as NativeApplication)?.TryHandleIntent(intent) ?? false;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					if (handled)
					{
						this.Log().LogDebug($"Native application handled the intent.");
					}
					else
					{
						this.Log().LogDebug($"Native application did not handle the intent.");
					}
				}
			}
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			switch (requestCode)
			{
				case FolderPicker.RequestCode:
					FolderPicker.TryHandleIntent(data, resultCode);
					break;
				case FileOpenPicker.RequestCode:
					FileOpenPicker.TryHandleIntent(data, resultCode);
					break;
			}
		}

		/// <summary>
		/// This method is used by UI Test frameworks to get
		/// the Xamarin compatible name for a control in Java.
		/// </summary>
		/// <param name="type">A type full name</param>
		/// <returns>The assembly that contains the specified type</returns>
#if NET10_0_OR_GREATER
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) =>
			throw new NotSupportedException("`static` methods with [Export] are not supported on NativeAOT.");
#else   // !NET10_0_OR_GREATER
		[Java.Interop.Export]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName!;
#endif  // !NET10_0_OR_GREATER

		internal partial class ClippedRelativeLayout : RelativeLayout
		{
			private IGeometry? _path;
			private Path _androidPath = new Path();
			private string _svgClipPath = "";

			public ClippedRelativeLayout(Context context) : base(context)
			{
				SetWillNotDraw(false);
			}

			// Neutral clip geometry: SVG path data + fill rule drive the Android clip path, FillContains hit-testing.
			public IGeometry? Path
			{
				get => _path;
				set
				{
					var svgClipPath = value?.ToSvgPathData() ?? "";
					if (_svgClipPath != svgClipPath)
					{
						_path = value;
						_svgClipPath = svgClipPath;
						_androidPath = PathParser.CreatePathFromPathData(_svgClipPath)!;
						_androidPath.SetFillType((value?.FillRule ?? GeometryFillRule.NonZero) switch
						{
							GeometryFillRule.EvenOdd => APath.FillType.EvenOdd!,
							_ => APath.FillType.Winding!,
						});
						Invalidate();
					}
				}
			}

			protected override void OnDraw(Canvas canvas)
			{
				base.OnDraw(canvas);
				canvas.ClipPath(_androidPath);
			}

			// The touch that focused a hosted view went straight to it and never reached XAML.
			public override void RequestChildFocus(View? child, View? focused)
			{
				base.RequestChildFocus(child, focused);

				if (child is not null)
				{
					AndroidSkiaNativeElementHostingExtension.OnNativeElementFocused(child);
				}
			}
		}
	}
}
