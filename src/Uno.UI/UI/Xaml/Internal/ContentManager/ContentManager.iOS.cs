#nullable enable

using System;
using UIKit;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void AttachToWindowPlatform(UIElement rootElement, Microsoft.UI.Xaml.Window window)
	{
		if (rootElement.Superview is null)
		{
			NativeWindowWrapper.Instance.MainController.View!.AddSubview(rootElement);
		}
		rootElement.Frame = NativeWindowWrapper.Instance.MainController.View!.Bounds;
		rootElement.AutoresizingMask = UIViewAutoresizing.All;
	}
}
