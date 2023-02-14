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
using Uno.Foundation.Logging;
using Uno.UI;
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
			Dispatcher = CoreDispatcher.Main;
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

		[Preserve]
		public static void Resize(double width, double height)
		{
			var window = Current?._rootVisual;
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

		partial void InternalActivate()
		{
			WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.activate();");
		}

		private void InternalSetContent(UIElement content)
		{
			if (_rootVisual == null)
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
				//TODO Uno: We can set and RootScrollViewer properly in case of WASM
				CoreServices.Instance.PutVisualRoot(_rootScrollViewer);
				_rootVisual = CoreServices.Instance.MainRootVisual;

				if (_rootVisual == null)
				{
					throw new InvalidOperationException("The root visual could not be created.");
				}

				// Load the root element in DOM

				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					UIElement.LoadingRootElement(_rootVisual);
				}

				WebAssemblyRuntime.InvokeJS($"Uno.UI.WindowManager.current.setRootElement({_rootVisual.HtmlId});");

				if (FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded)
				{
					UIElement.RootElementLoaded(_rootVisual);
				}

				UpdateRootAttributes();
			}

			_rootBorder.Child = _content = content;
		}

		internal void UpdateRootAttributes()
		{
			if (_rootVisual == null)
			{
				throw new InvalidOperationException("Internal window root is not yet set.");
			}

			if (FeatureConfiguration.Cursors.UseHandForInteraction)
			{
				_rootVisual.SetAttribute("data-use-hand-cursor-interaction", "true");
			}
			else
			{
				_rootVisual.RemoveAttribute("data-use-hand-cursor-interaction");
			}
		}

		internal IDisposable OpenPopup(Popup popup)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating popup");
			}

			if (PopupRoot == null)
			{
				throw new InvalidOperationException("PopupRoot is not initialized yet.");
			}

			var popupPanel = popup.PopupPanel;
			PopupRoot.Children.Add(popupPanel);

			return new CompositeDisposable(
				Disposable.Create(() =>
				{

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Closing popup");
					}

					PopupRoot.Children.Remove(popupPanel);
				}),
				VisualTreeHelper.RegisterOpenPopup(popup)
			);
		}
	}
}
#endif
