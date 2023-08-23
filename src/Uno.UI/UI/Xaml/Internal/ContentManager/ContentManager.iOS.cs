#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		if (_owner is not Window window)
		{
			throw new InvalidOperationException("The owner of the ContentManager should be a Window.");
		}

		window.MainController.View!.AddSubview(rootVisual);
		rootVisual.Frame = window.MainController.View.Bounds;
		rootVisual.AutoresizingMask = UIViewAutoresizing.All;
	}
}
