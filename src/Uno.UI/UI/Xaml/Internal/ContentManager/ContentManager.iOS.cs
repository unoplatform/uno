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
		NativeWindowWrapper.Instance.MainController.View!.AddSubview(rootElement);
		rootElement.Frame = NativeWindowWrapper.Instance.MainController.View.Bounds;
		rootElement.AutoresizingMask = UIViewAutoresizing.All;
	}
}
