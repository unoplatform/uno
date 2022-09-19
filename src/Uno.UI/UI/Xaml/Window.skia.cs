#nullable enable

using System;
using System.Diagnostics;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window? _current;
		private RootVisual? _rootVisual;
		// private ScrollViewer _rootScrollViewer;
		private Border? _rootBorder;
		private UIElement? _content;

		public Window()
		{
			Init();

			Compositor = new Compositor();
		}

		public void Init()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

			InitializeCommon();
		}

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
				
				_rootVisual?.XamlRoot?.InvalidateMeasure();
				RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

				ApplicationView.GetForCurrentView().SetVisibleBounds(newBounds);
			}
		}

		public Compositor Compositor { get; }

		private void InternalSetContent(UIElement content)
		{
			if (_rootVisual == null)
			{
				_rootBorder = new Border();
				CoreServices.Instance.PutVisualRoot(_rootBorder);
				_rootVisual = CoreServices.Instance.MainRootVisual;

				if (_rootVisual == null)
				{
					throw new InvalidOperationException("The root visual could not be created.");
				}

				if (_rootVisual.XamlRoot is not null)
				{
					CoreWindow.SetInvalidateRender(_rootVisual.XamlRoot.QueueInvalidateRender);
				}

				UIElement.LoadingRootElement(_rootVisual);

				Compositor.RootVisual = _rootVisual.Visual;

				_rootVisual?.XamlRoot?.InvalidateMeasure();

				UIElement.RootElementLoaded(_rootVisual);
			}

			if (_rootBorder != null)
			{
				_rootBorder.Child = _content = content;
			}
		}

		private UIElement InternalGetContent() => _content!;

		private UIElement InternalGetRootElement() => _rootVisual!;

		private static Window InternalGetCurrentWindow()
		{
			if (_current == null)
			{
				_current = new Window();
			}

			return _current;
		}		
	}
}
