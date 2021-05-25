#nullable enable

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
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window? _current;
		private Grid? _window;
		// private PopupRoot _popupRoot;
		// private ScrollViewer _rootScrollViewer;
		private Border? _rootBorder;
		private PopupRoot? _popupRoot;
		private UIElement? _content;

		public Window()
		{
			Init();

			Compositor = new Compositor();
		}

		public void Init()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = new CoreWindow();
			CoreWindow.SetInvalidateRender(QueueInvalidateRender);
			InitDragAndDrop();
		}

		internal static Action InvalidateRender = () => { };
		private bool _renderQueued = false;

		internal void QueueInvalidateRender()
		{
			if (!_isMeasuring && !_renderQueued)
			{
				_renderQueued = true;

				CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					_renderQueued = false;
					InvalidateRender();
				});
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

					CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
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
				RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

				ApplicationView.GetForCurrentView().SetVisibleBounds(newBounds);
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
				_popupRoot = new PopupRoot();
				FocusVisualLayer = new Canvas();

				_window = new Grid
				{
					IsVisualTreeRoot = true,
					Children =
					{
						_rootBorder,
						_popupRoot,						
						// Message Dialog => Those are currently using Popup, but they be upper
						FocusVisualLayer
						// Drag and drop => Those are added only when needed (they are actually not part of the WinUI visual tree and would have a negative perf impact)
					}
				};

				UIElement.LoadingRootElement(_window);

				Compositor.RootVisual = _window.Visual;

				UIElement.RootElementLoaded(_window);
			}

			if (_rootBorder != null)
			{
				_rootBorder.Child = _content = content;
			}
		}

		private UIElement InternalGetContent() => _content!;

		private UIElement InternalGetRootElement() => _window!;

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

			if (_popupRoot != null)
			{
				var popupPanel = popup.PopupPanel;
				_popupRoot.Children.Add(popupPanel);

				return new CompositeDisposable(
					Disposable.Create(() =>
					{

						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Closing popup");
						}

						_popupRoot.Children.Remove(popupPanel);
					}),
					VisualTreeHelper.RegisterOpenPopup(popup)
				);
			}

			return new CompositeDisposable();
		}
	}
}
