using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia.Android;
using Uno.UI.Xaml.Controls;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;


namespace Microsoft.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public partial class ApplicationActivity : Controls.NativePage
	{
		private IUnoSkiaRenderView? _renderView;
		private View? _renderViewAsView;
		private ClippedRelativeLayout? _nativeLayerHost;

		internal IUnoSkiaRenderView? RenderView => _renderView;

		private InputPane _inputPane;

		private bool _started;
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
			intent.AddFlags(ActivityFlags.NewDocument | ActivityFlags.MultipleTask);
			intent.PutExtra(WindowIdExtra, id);

			launcher.StartActivity(intent);
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
			}

			// The activity that started the app: it drives the main window, whose wrapper it created
			// before any managed Window existed, and re-adopts across re-creation.
			return Microsoft.UI.Xaml.Window.CurrentSafe?.NativeWrapper as NativeWindowWrapper
				?? new NativeWindowWrapper(this);
		}

		private int _adoptedWindowId;
		private static readonly ConcurrentDictionary<int, NativeWindowWrapper> _adoptedWindows = new();

		/// <summary>
		/// The root element of the window hosted by this activity, once the window has been created.
		/// </summary>
		internal UIElement? RootElement => _wrapper?.Window?.RootElement;

		private protected override void OnNativeActivationChanged(global::Windows.UI.Core.CoreWindowActivationState state)
			=> Wrapper.OnNativeActivated(state);

		private protected override void OnNativeVisibilityChanged(bool isVisible)
			=> Wrapper.OnNativeVisibilityChanged(isVisible);

		internal RelativeLayout RelativeLayout { get; private set; } = null!;

		internal LayoutProvider LayoutProvider { get; private set; } = null!;

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

			var nativelyHandled = false;
			if (_nativeLayerHost?.Path.Contains(ev.GetX(), ev.GetY()) ?? false)
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

		public override bool DispatchTouchEvent(MotionEvent? ev)
		{
			if (ev is null)
			{
				// Can this happen? Is Xamarin nullability annotation wrong?
				return base.DispatchTouchEvent(ev);
			}

			var nativelyHandled = false;
			if (_nativeLayerHost?.Path.Contains(ev.GetX(), ev.GetY()) ?? false)
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

			Wrapper.OnActivityCreated();

			// Track and observe this activity's window system UI visibility on its per-window wrapper.
			var decorView = this.Window!.DecorView;
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			Wrapper.SystemUiVisibility = (int)decorView.SystemUiVisibility;
			decorView.SetOnSystemUiVisibilityChangeListener(new OnSystemUiVisibilityChangeListener(this));
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			// Hold the splash on the Skia path until the first Skia frame is presented (see the render views).
			Wrapper.ArmFirstFrameGate();

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.KeyboardChanged += OnKeyboardChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			RaiseConfigurationChanges();

			InitializeBackPressedCallback();
		}

		protected override void OnStart()
		{
			// The render stack must exist before base.OnStart(): that call synchronously reaches
			// Application.Start -> OnLaunched -> CreateWindow, after which the host is registered
			// and InvalidateRender() can run against RelativeLayout. This state is per-activity, so
			// unlike the previous process-wide stack it is null again on every re-creation.
			if (!_started)
			{
				_started = true;
				RelativeLayout = new RelativeLayout(this);
				RelativeLayout.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);

				_renderView = CreateRenderView();
				_renderViewAsView = (View)_renderView;
				_renderViewAsView.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				RelativeLayout.AddView(_renderViewAsView);

				_nativeLayerHost = new ClippedRelativeLayout(this);
				_nativeLayerHost.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				RelativeLayout.AddView(NativeLayerHost);
			}

			base.OnStart();

			// A secondary window is activated before Android has given it an activity, so its show
			// was deferred until one existed with a render stack to attach to. That is now.
			Wrapper.CompleteDeferredShow();

			// On activity re-creation (deep-link, process restore) the managed Window already
			// exists with its content loaded, but CreateWindow won't run again for this new
			// activity. Attach this activity's freshly-built surface and reactivate the window.
			if (!_isContentViewSet && Wrapper.Window is { RootElement: not null } existingWindow)
			{
				EnsureContentView();
				_renderView?.ResetRendererContext();
				existingWindow.Activate();
				InvalidateRender();
			}
		}

		private IUnoSkiaRenderView CreateRenderView()
		{
			if (FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid)
			{
				if (!PackageManager?.HasSystemFeature(PackageManager.FeatureVulkanHardwareLevel) ?? true)
				{
					typeof(ApplicationActivity).Log().Warn($"Device does not support Vulkan. Falling back to OpenGL ES.");
				}
				else
				{
					// Vulkan feature flags are static device configuration and can be declared even when
					// the driver cannot actually render (common on emulators) — the view constructor
					// creates the Vulkan device and throws when the driver is unusable.
					try
					{
						return new UnoSKVulkanView(this);
					}
					catch (Exception ex)
					{
						if (typeof(ApplicationActivity).Log().IsEnabled(LogLevel.Warning))
						{
							typeof(ApplicationActivity).Log().Warn($"Vulkan rendering not available: {ex.Message}. Falling back to OpenGL ES.");
						}
					}
				}
			}

			return new UnoSKCanvasView(this);
		}

		internal void InvalidateRender()
		{
			_renderView?.InvalidateRender();
			RelativeLayout.Invalidate();
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
			base.OnResume();

			RaiseConfigurationChanges();

			//WebAuthenticationBroker.OnResume();
		}

		protected override void OnPause()
		{
			base.OnPause();

			// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13827.
			foreach (var contentRoot in WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots)
			{
				VisualTreeHelper.CloseLightDismissPopups(contentRoot.XamlRoot);
			}

			DismissKeyboard();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			LayoutProvider.Stop();
			LayoutProvider.KeyboardChanged -= OnKeyboardChanged;
			LayoutProvider.InsetsChanged -= OnInsetsChanged;

			// These are subscribed on process-wide singletons, so a missing -= keeps this activity
			// (and its render stack) alive for the life of the process, once per re-creation.
			_inputPane.Showing -= OnInputPaneVisibilityChanged;
			_inputPane.Hiding -= OnInputPaneVisibilityChanged;
			SimpleOrientationSensor.GetDefault()!.OrientationChanged -= OnSensorOrientationChanged;

			CleanupBackPressedCallback();

			// The render stack is per-activity and the peer finalizer never runs the managed
			// dispose path, so the GL/Vulkan context has to be released explicitly.
			_renderView?.TeardownRenderer();
			_renderView = null;
			_renderViewAsView = null;
			_nativeLayerHost = null;

			// Only signal the managed window as closing when this activity is not being re-created
			// and still owns the window. IsChangingConfigurations — not IsFinishing — is the
			// complement of "being re-created": a finishing activity is also the one replaced by
			// the StartActivity/Finish restart idiom, where the successor already took the wrapper.
			if (!IsChangingConfigurations && _wrapper is { } wrapper && ReferenceEquals(wrapper.CurrentActivity, this))
			{
				wrapper.OnNativeClosed();

				// The window is gone with its task, so stop holding its wrapper for re-adoption.
				if (_adoptedWindowId is > 0 and var windowId)
				{
					_adoptedWindows.TryRemove(windowId, out _);
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
		}

#pragma warning disable CS0618 // deprecated members
#pragma warning disable CS0672 // deprecated members
		public override void OnBackPressed()
		{
			var handled = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
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
			private SKPath _path = new SKPath();
			private Path _androidPath = new Path();
			private string _svgClipPath = "";

			public ClippedRelativeLayout(Context context) : base(context)
			{
				SetWillNotDraw(false);
			}

			public SKPath Path
			{
				get => _path;
				set
				{
					var svgClipPath = value.ToSvgPathData();
					if (_svgClipPath != svgClipPath)
					{
						_path = value;
						_svgClipPath = svgClipPath;
						_androidPath = PathParser.CreatePathFromPathData(_svgClipPath)!;
						_androidPath.SetFillType(value.FillType switch
						{
							SKPathFillType.Winding => APath.FillType.Winding!,
							SKPathFillType.EvenOdd => APath.FillType.EvenOdd!,
							SKPathFillType.InverseWinding => APath.FillType.InverseWinding!,
							SKPathFillType.InverseEvenOdd => APath.FillType.InverseEvenOdd!,
							_ => throw new ArgumentOutOfRangeException()
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
		}
	}
}
