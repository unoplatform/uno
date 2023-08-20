#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml;

public sealed partial class Window
{
#pragma warning disable CS0067 // The field is never used
#pragma warning disable CS0414 // The field is never used
	private bool _shown;
	private bool _windowCreated;

	partial void InitPlatform()
	{
		Compositor = new Compositor();
	}

	internal event EventHandler? Showing;

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

			if (_windowImplementation is CoreWindowWindow)
			{
				WinUICoreServices.Instance.MainRootVisual?.XamlRoot?.InvalidateMeasure();
			}
			else
			{
				if (Content?.XamlRoot is { } xamlRoot && xamlRoot.VisualTree.RootElement is XamlIsland xamlIsland)
				{
					xamlIsland.SetActualSize(newBounds.Size);
					xamlRoot.InvalidateMeasure();
				}
			}

			RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));

			ApplicationView.GetForCurrentView().SetVisibleBounds(newBounds);
		}
	}

	partial void ShowPartial()
	{
		_shown = true;
		Showing?.Invoke(this, EventArgs.Empty);

		//TryLoadRootVisual();
	}

	internal void OnNativeWindowCreated()
	{
		_windowCreated = true;
		//TryLoadRootVisual();
	}
}
