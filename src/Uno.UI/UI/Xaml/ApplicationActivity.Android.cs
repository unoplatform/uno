#if XAMARIN_ANDROID
using Android.App;
using Android.Content.PM;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Android.Content.Res;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.UI;
using Android.Views.InputMethods;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Util;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml
{
	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden)]
	public class ApplicationActivity : Controls.NativePage
	{
		private InputPane _inputPane;
		private KeyboardRectProvider _keyboardRectProvider;

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

		public override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();
			// Cannot call this in ctor: see
			// https://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview#10603714
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

		/// <summary>
		/// The windows model implies only one managed activity.
		/// </summary>
		internal static ApplicationActivity Instance { get; private set; }

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
			Window.DecorView.SystemUiVisibility = StatusBarVisibility.Visible;
			Window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
			Window.ClearFlags(WindowManagerFlags.Fullscreen);
		}

		private void OnLayoutChanged(Rect keyboard, Rect navigation, Rect union)
		{
			_inputPane.KeyboardRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
			_inputPane.NavigationBarRect = ViewHelper.PhysicalToLogicalPixels(navigation);
			_inputPane.OccludedRect = ViewHelper.PhysicalToLogicalPixels(union);
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			_keyboardRectProvider = new KeyboardRectProvider(this, OnLayoutChanged);
			RaiseConfigurationChanges();
		}

		public override void SetContentView(View view)
		{
			if (view != null)
			{
				if (view.IsAttachedToWindow)
				{
					_keyboardRectProvider.Start(view);
				}
				else
				{
					EventHandler<View.ViewAttachedToWindowEventArgs> handler = null;
					handler = (s, e) =>
					{
						_keyboardRectProvider.Start(view);
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

			_keyboardRectProvider.Stop();
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
			base.OnNewIntent(intent);
			this.Intent = intent;
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
