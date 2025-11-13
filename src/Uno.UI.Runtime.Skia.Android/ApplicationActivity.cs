using System;
using System.Diagnostics.CodeAnalysis;
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
		private static UnoSKCanvasView? _skCanvasView;
		private static ClippedRelativeLayout? _nativeLayerHost;

		private InputPane _inputPane;

		private static bool _started;
		private bool _isContentViewSet;

		/// <summary>
		/// The windows model implies only one managed activity.
		/// </summary>
		internal static ApplicationActivity Instance { get; private set; } = null!;

		internal static RelativeLayout RelativeLayout { get; private set; } = null!;

		internal LayoutProvider LayoutProvider { get; private set; } = null!;

		internal static ClippedRelativeLayout? NativeLayerHost => _nativeLayerHost;

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
			Instance = this;

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
			SimpleOrientationSensor.GetDefault()!.OrientationChanged += OnSensorOrientationChanged;
		}

		private void OnSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
		{
			NativeDispatcher.Main.Enqueue(RaiseConfigurationChanges);
		}

		private void OnInputPaneVisibilityChanged(InputPane sender, InputPaneVisibilityEventArgs args)
		{
		}

		protected override void InitializeComponent()
		{
			// The app was previously running, but application activity
			// changed. Reparent content.
			if (RelativeLayout is not null)
			{
				// Reparent the current layout to this activity
				if (RelativeLayout.Parent is ViewGroup parent)
				{
					parent.RemoveView(RelativeLayout);
				}

				this.SetContentView(RelativeLayout);

				// Ensure the SKCanvasView is reset
				_skCanvasView?.ResetRendererContext();

				var winUIWindow = Microsoft.UI.Xaml.Window.CurrentSafe ?? Microsoft.UI.Xaml.Window.InitialWindow;
				if (winUIWindow?.RootElement is { } root)
				{
					// Reactivate the window
					winUIWindow.Activate();
					InvalidateRender();
				}
			}
		}

		public override bool DispatchKeyEvent(KeyEvent? e)
		{
			if (e is null)
			{
				return base.DispatchKeyEvent(e);
			}

			var handled = AndroidKeyboardInputSource.Instance.OnNativeKeyEvent(e);

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

			_skCanvasView?.GetLocationInWindow(_locationInWindow);
			AndroidCorePointerInputSource.Instance.OnNativeMotionEvent(ev, _locationInWindow, nativelyHandled);

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

			_skCanvasView?.GetLocationInWindow(_locationInWindow);
			AndroidCorePointerInputSource.Instance.OnNativeMotionEvent(ev, _locationInWindow, nativelyHandled);

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
			NativeWindowWrapper.Instance.RaiseNativeSizeChanged();
			_inputPane.OccludedRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
		}

		protected override void OnCreate(Bundle? bundle)
		{
			if (FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
			{
				EdgeToEdge.Enable(this);
			}

			base.OnCreate(bundle);

			NativeWindowWrapper.Instance.OnActivityCreated();

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.KeyboardChanged += OnKeyboardChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			RaiseConfigurationChanges();
		}

		protected override void OnStart()
		{
			base.OnStart();

			// OnStart gets fired either after onCreate (first launch) or after onRestart
			// (go out of app then back again). We only want to do this once, hence
			// the flag.
			if (!_started)
			{
				_started = true;
				RelativeLayout = new RelativeLayout(this);
				RelativeLayout.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);

				_skCanvasView = new UnoSKCanvasView(this);
				_skCanvasView.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				RelativeLayout.AddView(_skCanvasView);

				_nativeLayerHost = new ClippedRelativeLayout(this);
				_nativeLayerHost.LayoutParameters = new ViewGroup.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);
				RelativeLayout.AddView(NativeLayerHost);
			}
		}

		internal void InvalidateRender()
		{
			_skCanvasView?.InvalidateRender();
			RelativeLayout.Invalidate();
		}

		private void OnInsetsChanged(Thickness insets)
		{
			NativeWindowWrapper.Instance.RaiseNativeSizeChanged();
		}

		public override void SetContentView(View? view)
		{
			if (view != null)
			{
				if (view.IsAttachedToWindow)
				{
					LayoutProvider.Start(view);
				}
				else
				{
					EventHandler<View.ViewAttachedToWindowEventArgs>? handler = null;
					handler = (s, e) =>
					{
						LayoutProvider.Start(view);
						ContentViewAttachedToWindow?.Invoke(this, EventArgs.Empty);
						view.ViewAttachedToWindow -= handler;
					};
					view.ViewAttachedToWindow += handler;
				}
			}

			base.SetContentView(view);
		}

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

			NativeWindowWrapper.Instance.OnNativeClosed();
		}

		public override void OnConfigurationChanged(Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);

			RaiseConfigurationChanges();
		}

		private void RaiseConfigurationChanges()
		{
			NativeWindowWrapper.Instance.RaiseNativeSizeChanged();
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
