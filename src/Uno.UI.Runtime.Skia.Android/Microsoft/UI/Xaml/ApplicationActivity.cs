using System;
using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Android;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia.Android;
using Uno.UI.Xaml.Controls;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.ViewManagement;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;


namespace Microsoft.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class ApplicationActivity : Controls.NativePage
	{
		private UnoSKCanvasView? _skCanvasView;

		/// <summary>
		/// The windows model implies only one managed activity.
		/// </summary>
		internal static ApplicationActivity Instance { get; private set; } = null!;

		internal RelativeLayout RelativeLayout { get; private set; } = null!;

		internal LayoutProvider LayoutProvider { get; private set; } = null!;

		private InputPane _inputPane;
		//private Android.Views.Window? _window;

		public ApplicationActivity(IntPtr ptr, Android.Runtime.JniHandleOwnership owner) : base(ptr, owner)
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
		}

		public override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Cannot call this in ctor: see
			// https://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview#10603714
			RaiseConfigurationChanges();
			SimpleOrientationSensor.GetDefault().OrientationChanged += OnSensorOrientationChanged;
		}

		private void OnSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
		{
			NativeDispatcher.Main.Enqueue(RaiseConfigurationChanges);
		}

		private void OnInputPaneVisibilityChanged(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			if (Window != null && !Window!.Attributes!.SoftInputMode.HasFlag(SoftInput.AdjustNothing))
			{
				// We assume the system already ensured the focused element is in view
				// using either SoftInput.AdjustResize or SoftInput.AdjustPan.
				args.EnsuredFocusedElementInView = true;
			}
		}

		protected override void InitializeComponent()
		{
			// Sometimes, within the same Application lifecycle, the main Activity is destroyed and a new one is created (i.e., when pressing the back button on the first page).
			// This code transfers the content from the previous activity to the new one (if applicable).
			//var initialWindow = Microsoft.UI.Xaml.Window.CurrentSafe ?? Microsoft.UI.Xaml.Window.InitialWindow;
			//if (initialWindow?.RootElement is View content)
			//{
			//	(content.GetParent() as ViewGroup)?.RemoveView(content);
			//	SetContentView(content);
			//}
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

		public override bool DispatchGenericMotionEvent(MotionEvent? e)
		{
			if (e is null)
			{
				// Can this happen? Is Xamarin nullability annotation wrong?
				return base.OnTouchEvent(e);
			}

			var correction = new int[2];
			_skCanvasView?.GetLocationInWindow(correction);
			AndroidCorePointerInputSource.Instance.OnNativeMotionEvent(e, correction);

			return base.DispatchGenericMotionEvent(e);
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (e is null)
			{
				// Can this happen? Is Xamarin nullability annotation wrong?
				return base.OnTouchEvent(e);
			}

			var correction = new int[2];
			_skCanvasView?.GetLocationInWindow(correction);
			AndroidCorePointerInputSource.Instance.OnNativeMotionEvent(e, correction);

			return base.OnTouchEvent(e);
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
			//_inputPane.OccludedRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
		}

		protected override void OnCreate(Bundle? bundle)
		{
			base.OnCreate(bundle);
			NativeWindowWrapper.Instance.OnActivityCreated();

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.KeyboardChanged += OnKeyboardChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			RaiseConfigurationChanges();

			RelativeLayout = new RelativeLayout(this);
			RelativeLayout.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent);

			SetContentView(RelativeLayout);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_skCanvasView = new UnoSKCanvasView(this, Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement!);
			_skCanvasView.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent);

			RelativeLayout.AddView(_skCanvasView);
			_skCanvasView.PaintSurface += OnPaintSurface;
		}

		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			if (Microsoft.UI.Xaml.Window.CurrentSafe is { RootElement: { } root } window)
			{
				var canvas = e.Surface.Canvas;
				canvas.Clear(SKColors.Red);
				var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
				canvas.Scale((float)scale);
				window.Compositor.RenderRootVisual(e.Surface, root.Visual);
				var helper = (UnoExploreByTouchHelper)ViewCompat.GetAccessibilityDelegate(_skCanvasView);
				helper.InvalidateRoot();
			}
		}

		internal void InvalidateRender()
			=> _skCanvasView?.Invalidate();

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
						view.ViewAttachedToWindow -= handler;
					};
					view.ViewAttachedToWindow += handler;
				}
			}

			base.SetContentView(view);
		}

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
			//DisplayInformation.GetForCurrentView().HandleConfigurationChange();
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
				//var handled = (Application as NativeApplication)?.TryHandleIntent(intent) ?? false;

				//if (this.Log().IsEnabled(LogLevel.Debug))
				//{
				//	if (handled)
				//	{
				//		this.Log().LogDebug($"Native application handled the intent.");
				//	}
				//	else
				//	{
				//		this.Log().LogDebug($"Native application did not handle the intent.");
				//	}
				//}
			}
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			//switch (requestCode)
			//{
			//	case FolderPicker.RequestCode:
			//		FolderPicker.TryHandleIntent(data, resultCode);
			//		break;
			//	case FileOpenPicker.RequestCode:
			//		FileOpenPicker.TryHandleIntent(data, resultCode);
			//		break;
			//}
		}

		/// <summary>
		/// This method is used by UI Test frameworks to get
		/// the Xamarin compatible name for a control in Java.
		/// </summary>
		/// <param name="type">A type full name</param>
		/// <returns>The assembly that contains the specified type</returns>
		[Java.Interop.Export]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName!;
	}
}
