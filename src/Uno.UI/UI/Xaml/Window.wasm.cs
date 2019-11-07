#if __WASM__
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private Grid _window;
		private PopupRoot _popupRoot;
		private ScrollViewer _rootScrollViewer;
		private Border _rootBorder;
		private UIElement _content;
		private bool _invalidateRequested;

		public Window()
		{
			Init();

			InitializeCommon();
		}

		private void Init()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();
		}

		internal static void InvalidateMeasure()
		{
			Current?.InnerInvalidateMeasure();
		}

		private void InnerInvalidateMeasure()
		{
			if (!_invalidateRequested)
			{
				_invalidateRequested = true;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("DispatchInvalidateMeasure scheduled");
				}

				CoreDispatcher.Main.RunAsync(
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
			if (_window != null)
			{
				var sw = Stopwatch.StartNew();
				_window.Measure(Bounds.Size);
				_window.Arrange(Bounds);
				sw.Stop();

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"DispatchInvalidateMeasure: {sw.Elapsed}");
				}
			}
		}

		[Preserve]
		public static void Resize(double width, double height)
		{
			var window = Current?._window;
			if (window == null)
			{
				typeof(Window).Log().Error($"Resize ignore, no current window defined");
				return; // nothing to measure
			}

			Current.OnNativeSizeChanged(new Size(width, height));
		}

		private void OnNativeSizeChanged(Size size)
		{
			var newBounds = new Rect(0, 0, size.Width, size.Height);

			if (newBounds != Bounds)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"OnNativeSizeChanged: {size}");
				}

				Bounds = newBounds;

				DispatchInvalidateMeasure();
				RaiseSizeChanged(new WindowSizeChangedEventArgs(size));

				// Note that UWP raises the ApplicationView.VisibleBoundsChanged event
				// *after* Window.SizeChanged.

				// TODO: support for "viewport-fix" on devices with a notch.
				ApplicationView.GetForCurrentView()?.SetVisibleBounds(newBounds);
			}
		}

		partial void InternalActivate()
		{
			WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.activate();");
		}

		private void InternalSetContent(UIElement content)
		{
			if (_window == null)
			{
				_rootBorder = new Border();
				_rootScrollViewer = new ScrollViewer()
				{
					VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
					VerticalScrollMode = ScrollMode.Disabled,
					HorizontalScrollMode = ScrollMode.Disabled,
					Content = _rootBorder
				};
				_popupRoot = new PopupRoot();
				_window = new Grid()
				{
					Children =
					{
						_rootScrollViewer,
						_popupRoot
					}
				};
			}

			_rootBorder.Child = _content = content;
			if (content != null)
			{
				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded && !_window.IsLoaded)
				{
					_window.ManagedOnLoading();
				}

				WebAssemblyRuntime.InvokeJS($"Uno.UI.WindowManager.current.setRootContent({_window.HtmlId});");

				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded && !_window.IsLoaded)
				{
					_window.ManagedOnLoaded(1);
				}
			}
			else
			{
				WebAssemblyRuntime.InvokeJS($"Uno.UI.WindowManager.current.setRootContent();");

				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded && _window.IsLoaded)
				{
					_window.ManagedOnUnloaded();
				}
			}
		}

		private UIElement InternalGetContent()
		{
			return _content;
		}

		private static Window InternalGetCurrentWindow()
		{
			if (_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal IDisposable OpenPopup(Popup popup)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating popup");
			}

			var popupPanel = popup.PopupPanel;
			_popupRoot.Children.Add(popupPanel);

			return new CompositeDisposable(
				Disposable.Create(() => {

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Closing popup");
					}

					_popupRoot.Children.Remove(popupPanel);
				}),
				VisualTreeHelper.RegisterOpenPopup(popup)
			);
		}
	}
}
#endif
