using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Uno.Foundation.Logging;
using Uno.Gaming.Input.Internal;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Devices.Sensors;
using Windows.Gaming.Input;
using Windows.Graphics.Display;
using Windows.Security.Authentication.Web;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;


namespace Microsoft.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class ApplicationActivity : Controls.NativePage, Uno.UI.Composition.ICompositionRoot
	{
		/// <summary>
		/// The windows model implies only one managed activity.
		/// </summary>
		internal static ApplicationActivity Instance { get; private set; }

		internal LayoutProvider LayoutProvider { get; private set; }

		private InputPane _inputPane;
		private View _content;
		private Android.Views.Window _window;

		public ApplicationActivity(IntPtr ptr, Android.Runtime.JniHandleOwnership owner) : base(ptr, owner)
		{
			Initialize();
		}

		public ApplicationActivity()
		{
			Initialize();
		}

		private void Initialize()
		{
			Instance = this;

			WinUIWindow = Windows.UI.Xaml.Window.IShouldntUseCurrentWindow;

			_inputPane = InputPane.GetForCurrentView();
			_inputPane.Showing += OnInputPaneVisibilityChanged;
			_inputPane.Hiding += OnInputPaneVisibilityChanged;
		}

		View Uno.UI.Composition.ICompositionRoot.Content => _content;
		Android.Views.Window Uno.UI.Composition.ICompositionRoot.Window => _window ??= base.Window;

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
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RaiseConfigurationChanges);
		}

		private void OnInputPaneVisibilityChanged(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			if (Window != null && !Window.Attributes.SoftInputMode.HasFlag(SoftInput.AdjustNothing))
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
			if (WinUIWindow.RootElement is View content) // TODO:MZ: Fix this!
			{
				(content.GetParent() as ViewGroup)?.RemoveView(content);
				SetContentView(content);
			}
		}

		public override bool DispatchKeyEvent(KeyEvent e)
		{
			var handled = false;

			var virtualKey = VirtualKeyHelper.FromKeyCode(e.KeyCode);
			var modifiers = VirtualKeyHelper.FromModifiers(e.Modifiers);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"DispatchKeyEvent: {e.KeyCode} -> {virtualKey}");
			}

			try
			{
				if (FocusManager.GetFocusedElement() is not FrameworkElement element)
				{
					element = WinUICoreServices.Instance.MainRootVisual;
				}

				var routedArgs = new KeyRoutedEventArgs(this, virtualKey, modifiers)
				{
					CanBubbleNatively = false,
				};

				RoutedEvent routedEvent = e.Action == KeyEventActions.Down ?
					UIElement.KeyDownEvent :
					UIElement.KeyUpEvent;

				element?.RaiseEvent(routedEvent, routedArgs);

				handled = routedArgs.Handled;

				if (CoreWindow.GetForCurrentThread() is ICoreWindowEvents ownerEvents)
				{
					var coreWindowArgs = new KeyEventArgs(
						"keyboard",
						virtualKey,
						modifiers,
						new CorePhysicalKeyStatus
						{
							ScanCode = (uint)e.KeyCode,
							RepeatCount = 1,
						})
					{
						Handled = handled
					};

					if (e.Action == KeyEventActions.Down)
					{
						ownerEvents.RaiseKeyDown(coreWindowArgs);
					}
					else if (e.Action == KeyEventActions.Up)
					{
						ownerEvents.RaiseKeyUp(coreWindowArgs);
					}

					handled = coreWindowArgs.Handled;
				}
			}
			catch (Exception ex)
			{
				Microsoft.UI.Xaml.Application.Current.RaiseRecoverableUnhandledException(ex);
			}

			if (Gamepad.TryHandleKeyEvent(e))
			{
				return true;
			}

			if (!handled)
			{
				handled = base.DispatchKeyEvent(e);
			}

			return handled;
		}

		public override bool DispatchGenericMotionEvent(MotionEvent e)
		{
			if (Gamepad.OnGenericMotionEvent(e))
			{
				return true;
			}

			return base.DispatchGenericMotionEvent(e);
		}

		public void DismissKeyboard()
		{
			var windowToken = CurrentFocus?.WindowToken;

			if (windowToken != null)
			{
				var inputManager = (InputMethodManager)GetSystemService(InputMethodService);
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
			Window.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;
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

		protected override void OnCreate(Bundle bundle)
		{
			if (Uno.CompositionConfiguration.UseCompositorThread)
			{
				Uno.UI.Composition.CompositorThread.Start(this);
			}

			base.OnCreate(bundle);
			NativeWindowWrapper.Instance.OnActivityCreated();

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.KeyboardChanged += OnKeyboardChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			RaiseConfigurationChanges();
		}

		private void OnInsetsChanged(Thickness insets)
		{
			NativeWindowWrapper.Instance.RaiseNativeSizeChanged();
		}

		public override void SetContentView(View view)
		{
			_content = view;

			if (view != null)
			{
				if (view.IsAttachedToWindow)
				{
					LayoutProvider.Start(view);
				}
				else
				{
					EventHandler<View.ViewAttachedToWindowEventArgs> handler = null;
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

			WebAuthenticationBroker.OnResume();
		}

		protected override void OnPause()
		{
			base.OnPause();

			// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #8341.
			VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot.XamlRoot);

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
			ViewHelper.RefreshFontScale();
			DisplayInformation.GetForCurrentView().HandleConfigurationChange();
			SystemThemeHelper.RefreshSystemTheme();
		}

#pragma warning disable CS0618 // deprecated members
#pragma warning disable CS0672 // deprecated members
		public override void OnBackPressed()
		{
			var handled = Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
			if (!handled)
			{
				base.OnBackPressed();
			}
		}
#pragma warning restore CS0618 // deprecated members
#pragma warning restore CS0672 // deprecated members

		protected override void OnNewIntent(Intent intent)
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

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
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
		[Java.Interop.Export]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName;
	}
}
