#nullable enable

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
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

// TODO:MZ: Avoid MainRootVisual
namespace Windows.UI.Xaml;

public sealed partial class Window
{
	internal event EventHandler? Shown;

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

			WinUICoreServices.Instance.MainRootVisual?.XamlRoot?.InvalidateMeasure();
			RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

			ApplicationView.GetForCurrentView().SetVisibleBounds(newBounds);
		}
	}

	internal void DisplayFullscreen(UIElement content)
	{
		if (FullWindowMediaRoot is null)
		{
			throw new InvalidOperationException("The FullWindowMediaRoot is not initialized.");
		}

		if (content == null)
		{
			FullWindowMediaRoot.Child = null;
			//TODO:MZ: Restore _rootBorder
			//if (_rootBorder != null)
			//{
			//	_rootBorder.Visibility = Visibility.Visible;
			//}
			FullWindowMediaRoot.Visibility = Visibility.Collapsed;
		}
		else
		{
			FullWindowMediaRoot.Visibility = Visibility.Visible;
			//TODO:MZ: Restore _rootBorder
			//if (_rootBorder != null)
			//{
			//	_rootBorder.Visibility = Visibility.Collapsed;
			//}
			FullWindowMediaRoot.Child = content;
		}
	}

	partial void ShowPartial()
	{
		Shown?.Invoke(this, EventArgs.Empty);

		//TryLoadRootVisual();
	}
}
