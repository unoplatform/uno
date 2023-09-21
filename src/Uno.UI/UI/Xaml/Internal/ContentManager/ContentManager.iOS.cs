#nullable enable

using System;
using UIKit;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		if (_owner is not Window window)
		{
			throw new InvalidOperationException("The owner of the ContentManager should be a Window.");
		}

		NativeWindowWrapper.Instance.MainController.View!.AddSubview(rootVisual);
		rootVisual.Frame = NativeWindowWrapper.Instance.MainController.View.Bounds;
		rootVisual.AutoresizingMask = UIViewAutoresizing.All;
	}
}
