using System;
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
using DirectUI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.Gaming.Input.Internal;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Devices.Sensors;
using Windows.Gaming.Input;
using Windows.Graphics.Display;
using Windows.Security.Authentication.Web;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class ApplicationActivity : Controls.NativePage, Uno.UI.Composition.ICompositionRoot
	{
		private bool _isContentViewSet;
		private SystemNavigationManagerBackPressedCallback _backPressedCallback;

		/// <summary>
		/// The windows model implies only one managed activity.
		/// </summary>
		internal static ApplicationActivity Instance { get; private set; }

		internal ViewGroup RootView { get; private set; } = null!;

		internal LayoutProvider LayoutProvider { get; private set; }

		private InputPane _inputPane;
		private View _content;
		private AWindow _window;

		public ApplicationActivity(IntPtr ptr, JniHandleOwnership owner) : base(ptr, owner)
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

			_inputPane = InputPane.GetForCurrentView();
			_inputPane.Showing += OnInputPaneVisibilityChanged;
			_inputPane.Hiding += OnInputPaneVisibilityChanged;
		}

		View Uno.UI.Composition.ICompositionRoot.Content => _content;
		AWindow Uno.UI.Composition.ICompositionRoot.Window => _window ??= base.Window;

		internal void EnsureContentView()
		{
			if (_isContentViewSet)
			{
				return;
			}

			SetContentView(RootView);
			_isContentViewSet = true;
		}

		public override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			StatusBar.GetForCurrentView().UpdateSystemUiVisibility();

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
			var initialWindow = Microsoft.UI.Xaml.Window.CurrentSafe ?? Microsoft.UI.Xaml.Window.InitialWindow;
			if (initialWindow?.RootElement is View content)
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

			var xamlRoot = Microsoft.UI.Xaml.Window.InitialWindow?.Content?.XamlRoot;

			try
			{
				if (FocusManager.GetFocusedElement(xamlRoot) is not FrameworkElement element)
				{
					element = WinUICoreServices.Instance.VisualRoot as FrameworkElement;
				}

				var routedArgs = new KeyRoutedEventArgs(this, virtualKey, modifiers)
				{
					CanBubbleNatively = false,
				};

				var inputManager = VisualTree.GetContentRootForElement(element)?.InputManager;
				if (inputManager is not null && XboxUtility.IsGamepadNavigationInput(virtualKey))
				{
					inputManager.LastInputDeviceType = InputDeviceType.GamepadOrRemote;
				}
				else
				{
					inputManager.LastInputDeviceType = InputDeviceType.Keyboard;
				}

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

			if (FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
			{
				EdgeToEdge.Enable(this);
			}

			base.OnCreate(bundle);

			NativeWindowWrapper.Instance.OnActivityCreated();

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.KeyboardChanged += OnKeyboardChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			// Register the OnBackPressedCallback for Android predictive back gesture support
			RegisterBackPressedCallback();

			RaiseConfigurationChanges();
		}

		private void RegisterBackPressedCallback()
		{
			_backPressedCallback = new SystemNavigationManagerBackPressedCallback(this);

			// Add the callback to the dispatcher with this activity as the lifecycle owner
			// This ensures the callback is automatically removed when the activity is destroyed
			OnBackPressedDispatcher.AddCallback(this, _backPressedCallback);

			// Subscribe to SystemNavigationManager events to update callback state
			var systemNavManager = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
			systemNavManager.BackHandlerRequired += OnBackHandlerRequiredChanged;
			systemNavManager.AppViewBackButtonVisibilityChanged += OnAppViewBackButtonVisibilityChanged;

			// Set initial enabled state
			UpdateBackPressedCallbackEnabled();
		}

		private void OnBackHandlerRequiredChanged(object sender, bool required)
		{
			UpdateBackPressedCallbackEnabled();
		}

		private void OnAppViewBackButtonVisibilityChanged(object sender, AppViewBackButtonVisibility visibility)
		{
			UpdateBackPressedCallbackEnabled();
		}

		private void UpdateBackPressedCallbackEnabled()
		{
			if (_backPressedCallback != null)
			{
				var systemNavManager = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
				// Enable the callback when there are subscribers or back button is visible
				_backPressedCallback.Enabled =
					systemNavManager.HasBackRequestedSubscribers ||
					systemNavManager.AppViewBackButtonVisibility == AppViewBackButtonVisibility.Visible;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"OnBackPressedCallback Enabled={_backPressedCallback.Enabled}");
				}
			}
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
						ContentViewAttachedToWindow?.Invoke(this, EventArgs.Empty);
						view.ViewAttachedToWindow -= handler;
					};
					view.ViewAttachedToWindow += handler;
				}
			}

			base.SetContentView(view);
		}

		internal event EventHandler ContentViewAttachedToWindow;

		protected override void OnResume()
		{
			base.OnResume();

			RaiseConfigurationChanges();

			WebAuthenticationBroker.OnResume();
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

			// Unsubscribe from SystemNavigationManager events
			var systemNavManager = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
			systemNavManager.BackHandlerRequired -= OnBackHandlerRequiredChanged;
			systemNavManager.AppViewBackButtonVisibilityChanged -= OnAppViewBackButtonVisibilityChanged;

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
			var handled = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
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
#if NET10_0_OR_GREATER
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) =>
			throw new NotSupportedException("`static` methods with [Export] are not supported on NativeAOT.");
#else   // !NET10_0_OR_GREATER
		[Java.Interop.Export(nameof(GetTypeAssemblyFullName))]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName;
#endif  // !NET10_0_OR_GREATER

		internal void SetRootElement(ViewGroup rootElement) => RootView = rootElement;

		/// <summary>
		/// Custom OnBackPressedCallback for Android's predictive back gesture support.
		/// This callback is used instead of the deprecated OnBackPressed method.
		/// </summary>
		private sealed class SystemNavigationManagerBackPressedCallback : OnBackPressedCallback
		{
			private readonly ApplicationActivity _activity;

			public SystemNavigationManagerBackPressedCallback(ApplicationActivity activity)
				: base(enabled: false) // Start disabled, will be enabled when subscribers are added
			{
				_activity = activity;
			}

			public override void HandleOnBackPressed()
			{
				var handled = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();

				if (!handled)
				{
					// If not handled by the app, let the system handle it
					// Temporarily disable ourselves and re-trigger the back press
					Enabled = false;
					try
					{
						_activity.OnBackPressedDispatcher.OnBackPressed();
					}
					finally
					{
						// Re-enable ourselves based on the current state
						_activity.UpdateBackPressedCallbackEnabled();
					}
				}
			}
		}
	}
}
