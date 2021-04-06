#if XAMARIN_ANDROID
using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Windows.Graphics.Display;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.Devices.Sensors;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;
using Uno.AuthenticationBroker;

namespace Windows.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class ApplicationActivity : Controls.NativePage, Uno.UI.Composition.ICompositionRoot
	{

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
			RaiseConfigurationChanges();
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
			if (Xaml.Window.Current.MainContent is View content)
			{
				(content.GetParent() as ViewGroup)?.RemoveView(content);
				SetContentView(content);
			}
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
			Window.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;
#pragma warning restore 618

			Window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
			Window.ClearFlags(WindowManagerFlags.Fullscreen);
		}

		private void OnLayoutChanged(Rect statusBar, Rect keyboard, Rect navigationBar)
		{
			Xaml.Window.Current?.RaiseNativeSizeChanged();
			_inputPane.OccludedRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
		}

		protected override void OnCreate(Bundle bundle)
		{
			if (Uno.CompositionConfiguration.UseCompositorThread)
			{
				Uno.UI.Composition.CompositorThread.Start(this);
			}

			base.OnCreate(bundle);

			LayoutProvider = new LayoutProvider(this);
			LayoutProvider.LayoutChanged += OnLayoutChanged;
			LayoutProvider.InsetsChanged += OnInsetsChanged;

			RaiseConfigurationChanges();
		}

		private void OnInsetsChanged(Thickness insets)
		{
			if (Xaml.Window.Current != null)
			{
				//Set insets before raising the size changed event
				Xaml.Window.Current.Insets = insets;
				Xaml.Window.Current.RaiseNativeSizeChanged();
			}
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

			WebAuthenticationBrokerProvider.OnMainActivityResumed();
		}

		protected override void OnPause()
		{
			base.OnPause();

			VisualTreeHelper.CloseAllPopups();

			DismissKeyboard();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			LayoutProvider.Stop();
		}

		public override void OnConfigurationChanged(Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);

			RaiseConfigurationChanges();
		}

		private static void RaiseConfigurationChanges()
		{
			Xaml.Window.Current?.RaiseNativeSizeChanged();
			ViewHelper.RefreshFontScale();
			DisplayInformation.GetForCurrentView().HandleConfigurationChange();
			Windows.UI.Xaml.Application.Current.OnSystemThemeChanged();
		}

		public override void OnBackPressed()
		{
			var handled = Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
			if (!handled)
			{
				base.OnBackPressed();
			}
		}

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
		[Android.Runtime.Preserve]
		[Java.Interop.Export]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName;
	}
}
#endif
