#nullable enable

using System;
using System.Diagnostics;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		// private ScrollViewer _rootScrollViewer;
		private Border? _rootBorder;

		partial void InitPlatform()
		{
			Dispatcher = CoreDispatcher.Main;
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

			Compositor = new Compositor();
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

		public Compositor Compositor { get; private set; }

		private void InternalSetContent(UIElement content)
		{
			if (_rootVisual == null)
			{
				_rootBorder = new Border();
				CoreServices.Instance.PutVisualRoot(_rootBorder);
				_rootVisual = CoreServices.Instance.MainRootVisual;

				if (_rootVisual?.XamlRoot == null)
				{
					throw new InvalidOperationException("The root visual could not be created.");
				}

				CoreWindow.SetInvalidateRender(_rootVisual.XamlRoot.QueueInvalidateRender);

				UIElement.LoadingRootElement(_rootVisual);

				Compositor.RootVisual = _rootVisual.Visual;

				_rootVisual?.XamlRoot.InvalidateMeasure();

				UIElement.RootElementLoaded(_rootVisual);
			}

			if (_rootBorder != null)
			{
				_rootBorder.Child = _content = content;
			}
		}
	}
}
