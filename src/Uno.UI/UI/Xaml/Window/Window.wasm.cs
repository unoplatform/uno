using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		[JSExport]
		[Preserve]
		public static void Resize(double width, double height) => IShouldntUseCurrentWindow.OnNativeSizeChanged(new Size(width, height));

		internal void OnNativeSizeChanged(Size size)
		{
			var newBounds = new Rect(0, 0, size.Width, size.Height);

			if (newBounds != Bounds)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"OnNativeSizeChanged: {size}");
				}

				Bounds = newBounds;

				_windowImplementation.XamlRoot?.InvalidateMeasure();
				RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

				// Note that UWP raises the ApplicationView.VisibleBoundsChanged event
				// *after* Window.SizeChanged.

				// TODO: support for "viewport-fix" on devices with a notch.
				ApplicationView.GetForCurrentView()?.SetVisibleBounds(newBounds);
			}
		}

		partial void ShowPartial() => WindowManagerInterop.WindowActivate();
	}
}
