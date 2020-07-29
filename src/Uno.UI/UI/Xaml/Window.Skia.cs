using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private Grid _window;
		// private PopupRoot _popupRoot;
		// private ScrollViewer _rootScrollViewer;
		private Border _rootBorder;
		private UIElement _content;

		public Window()
		{
			Init();

			Compositor = new Compositor();
		}

		public void Init()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();
		}

		internal Action InvalidateRender;

		internal void QueueInvalidateRender()
		{
			if (!_isMeasuring)
			{
				CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () => { InvalidateRender(); });
			}
		}

		private static bool _isMeasureQueued = false;
		private static bool _isMeasuring = false;

		internal static void InvalidateMeasure()
		{
			Current.InnerInvalidateMeasure();
		}

		internal void InnerInvalidateMeasure()
		{
			if (_window != null)
			{
				if (!_isMeasureQueued)
				{
					_isMeasureQueued = true;

					CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () => {
						try
						{
							_isMeasureQueued = false;

							_isMeasuring = true;

							var sw = Stopwatch.StartNew();
							_window.Measure(Bounds.Size);
							_window.Arrange(Bounds);
							sw.Stop();

							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								this.Log().Debug($"DispatchInvalidateMeasure: {sw.Elapsed}");
							}

							InvalidateRender();
						}
						finally
						{
							_isMeasuring = false;
						}
					});
				}

				
			}
		}

		internal void OnNativeSizeChanged(Size size)
		{
			var newBounds = new Rect(0, 0, size.Width, size.Height);

			if (newBounds != Bounds)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"OnNativeSizeChanged: {size}");
				}

				Bounds = newBounds;

				InvalidateMeasure();
				RaiseSizeChanged(new WindowSizeChangedEventArgs(size));
			}
		}

        public Compositor Compositor { get; }

        partial void InternalActivate()
		{
			// WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.activate();");
		}

		private void InternalSetContent(UIElement content)
		{
			if (_window == null)
			{
				_rootBorder = new Border();
				//_rootScrollViewer = new ScrollViewer()
				//{
				//	VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
				//	HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				//	VerticalScrollMode = ScrollMode.Disabled,
				//	HorizontalScrollMode = ScrollMode.Disabled,
				//	Content = _rootBorder
				//};
				//// _popupRoot = new PopupRoot();
				//_window = new Grid()
				//{
				//	Visual = Compositor.CreateContainerVisual(),
				//	Children =
				//	{
				//		_rootScrollViewer,
				//		// , _popupRoot
				//	}
				//};

				_window = new Grid {
					IsLoaded = true,
					Children = { _rootBorder }
				};

				Compositor.RootVisual = _window.Visual;
			}

			_rootBorder.Child = _content = content;
		}

		private UIElement InternalGetContent() => _content;

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
			
			var popupChild = popup.Child;
			//_popupRoot.Children.Add(popupChild);

			return new CompositeDisposable(
				Disposable.Create(() => {

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Closing popup");
					}

					//_popupRoot.Children.Remove(popupChild);
				}),
				VisualTreeHelper.RegisterOpenPopup(popup)
			);
		}
	}
}
