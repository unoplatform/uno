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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		private ScrollViewer _rootScrollViewer;
		private Border _rootBorder;
		private bool _invalidateRequested;

		partial void InitPlatform()
		{
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();
		}

		internal static void InvalidateMeasure()
		{
			Current?.InnerInvalidateMeasure();
		}

		internal static void InvalidateArrange()
		{
			// Right now, both measure & arrange invalidations
			// are done in the same loop
			Current?.InnerInvalidateMeasure();
		}

		private void InnerInvalidateMeasure()
		{
			if (!_invalidateRequested)
			{
				_invalidateRequested = true;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("DispatchInvalidateMeasure scheduled");
				}

				_ = CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.Normal,
					() =>
					{
						_invalidateRequested = false;

						Current?.DispatchInvalidateMeasure();
					}
				);
			}
		}

		private void DispatchInvalidateMeasure()
		{
			if (_rootVisual is null)
			{
				return;
			}

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				var sw = Stopwatch.StartNew();
				_rootVisual.Measure(Bounds.Size);
				_rootVisual.Arrange(Bounds);
				sw.Stop();

				this.Log().Debug($"DispatchInvalidateMeasure: {sw.Elapsed}");
			}
			else
			{
				_rootVisual.Measure(Bounds.Size);
				_rootVisual.Arrange(Bounds);
			}
		}

		[JSExport]
		[Preserve]
		internal static void Resize(double width, double height) => Current.OnNativeSizeChanged(new Size(width, height));

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

				DispatchInvalidateMeasure();
				RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

				// Note that UWP raises the ApplicationView.VisibleBoundsChanged event
				// *after* Window.SizeChanged.

				// TODO: support for "viewport-fix" on devices with a notch.
				ApplicationView.GetForCurrentView()?.SetVisibleBounds(newBounds);
			}
		}

		partial void ShowPartial() => WindowManagerInterop.WindowActivate();

		internal void DisplayFullscreen(UIElement content)
		{
			if (content == null)
			{
				FullWindowMediaRoot.Child = null;
				_rootBorder.Visibility = Visibility.Visible;
				FullWindowMediaRoot.Visibility = Visibility.Collapsed;
			}
			else
			{
				FullWindowMediaRoot.Visibility = Visibility.Visible;
				_rootBorder.Visibility = Visibility.Collapsed;
				FullWindowMediaRoot.Child = content;
			}
		}
	}
}
