using System;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		public ApplicationView()
		{
			TryInitializeSpanningRectsExtension();
		}

		public bool IsScreenCaptureEnabled
		{
			get
			{
				var activity = GetCurrentActivity();
				return !activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.Secure);
			}
			set
			{
				var activity = GetCurrentActivity();
				if (value)
				{
					activity.Window.ClearFlags(WindowManagerFlags.Secure);
				}
				else
				{
					activity.Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
				}
			}
		}

		public string Title
		{
			get
			{
				var activity = GetCurrentActivity();
				return activity.Title;
			}
			set
			{
				var activity = GetCurrentActivity();
				activity.Title = value;
			}
		}

		private Rect _trueVisibleBounds;

		internal void SetTrueVisibleBounds(Rect trueVisibleBounds) => _trueVisibleBounds = trueVisibleBounds;

		public bool TryEnterFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(true);
			return true;
		}

		public void ExitFullScreenMode()
		{
			CoreDispatcher.CheckThreadAccess();
			UpdateFullScreenMode(false);
		}

		private void UpdateFullScreenMode(bool isFullscreen)
		{
#pragma warning disable 618
			var activity = ContextHelper.Current as Activity;
#pragma warning disable CA1422 // Validate platform compatibility
			var uiOptions = (int)activity.Window.DecorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility

			if (isFullscreen)
			{
				uiOptions |= (int)SystemUiFlags.Fullscreen;
				uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
				uiOptions |= (int)SystemUiFlags.HideNavigation;
				uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
			}
			else
			{
				uiOptions &= ~(int)SystemUiFlags.Fullscreen;
				uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
				uiOptions &= ~(int)SystemUiFlags.HideNavigation;
				uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
			}

#pragma warning disable CA1422 // Validate platform compatibility
			activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618
		}


		private Activity GetCurrentActivity([CallerMemberName] string propertyName = null)
		{
			if (!(ContextHelper.Current is Activity activity))
			{
				throw new InvalidOperationException($"{propertyName} API must be called when Activity is created");
			}

			return activity;
		}
	}
}
