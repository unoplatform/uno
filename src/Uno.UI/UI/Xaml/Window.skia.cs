using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml;

public sealed partial class Window
{
	// private ScrollViewer _rootScrollViewer;
	private Border? _rootBorder;

	private bool _shown;

	partial void InitPlatform()
	{
		Dispatcher = CoreDispatcher.Main;
		CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

		Compositor = new Compositor();
	}

	internal event EventHandler Shown;

	public Compositor Compositor { get; private set; }

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

	private void InternalSetContent(UIElement content)
	{
		if (_rootVisual == null)
		{
			_rootBorder = new Border();
			CoreServices.Instance.PutVisualRoot(_rootBorder);
			_rootVisual = CoreServices.Instance.MainRootVisual;

			if (_rootVisual?.XamlRoot is null)
			{
				throw new InvalidOperationException("The root visual was not created.");
			}
		}

		if (_rootBorder != null)
		{
			_rootBorder.Child = _content = content;
		}

		TryLoadRootVisual();
	}

	internal void DisplayFullscreen(UIElement content)
	{
		if (content == null)
		{
			FullWindowMediaRoot.Child = null;
			if (_rootBorder != null)
			{
				_rootBorder.Visibility = Visibility.Visible;
			}
			FullWindowMediaRoot.Visibility = Visibility.Collapsed;
		}
		else
		{
			FullWindowMediaRoot.Visibility = Visibility.Visible;
			if (_rootBorder != null)
			{
				_rootBorder.Visibility = Visibility.Collapsed;
			}
			FullWindowMediaRoot.Child = content;
		}
	}

	partial void ShowPartial()
	{
		_shown = true;
		Shown?.Invoke(this, EventArgs.Empty);

		TryLoadRootVisual();
	}

	private void TryLoadRootVisual()
	{
		if (!_shown)
		{
			return;
		}

		UIElement.LoadingRootElement(_rootVisual);

		_rootVisual.XamlRoot!.InvalidateMeasure();

		UIElement.RootElementLoaded(_rootVisual);
	}
}
