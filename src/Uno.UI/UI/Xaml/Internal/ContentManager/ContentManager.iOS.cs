#nullable enable

using System;
using UIKit;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void AttachToWindowPlatform(UIElement rootElement, Windows.UI.Xaml.Window window)
	{
		if (window.NativeWrapper is not NativeWindowWrapper nativeWindowWrapper)
		{
			throw new InvalidOperationException("The window must be initialized before attaching the root element.");
		}

		if (rootElement.Superview is null)
		{
			nativeWindowWrapper.MainController.View!.AddSubview(rootElement);
		}
		rootElement.Frame = nativeWindowWrapper.MainController.View!.Bounds;
		rootElement.AutoresizingMask = UIViewAutoresizing.All;
	}
}
