#if XAMARIN_ANDROID
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Android.App;
using Android.Content.Res;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private UIElement _content; 

		public Window()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();
		}

		private void InternalActivate()
		{

		}

		private void InternalSetContent(UIElement value)
		{
			_content = value;
			ApplicationActivity.Instance?.SetContentView(value);
		}

		private UIElement InternalGetContent()
		{
			return _content;
		}

		private static Window InternalGetCurrentWindow()
		{
			if(_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal void RaiseNativeSizeChanged(int screenWidth, int screenHeight)
		{
			var newBounds = ViewHelper.PhysicalToLogicalPixels(new Rect(0, 0, screenWidth, screenHeight));

			if (Bounds != newBounds)
			{
				Bounds = newBounds;

				var applicationView = ApplicationView.GetForCurrentView();
				if (applicationView != null)
				{
					applicationView.SetCoreBounds(newBounds);
				}

				RaiseSizeChanged(
					new WindowSizeChangedEventArgs(
						new Windows.Foundation.Size(Bounds.Width, Bounds.Height)
					)
				);
			}
		}
	}
}
#endif
