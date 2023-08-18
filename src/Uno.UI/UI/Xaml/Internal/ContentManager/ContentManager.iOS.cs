#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	partial void SetupCoreWindowRootVisualPlatform(RootVisual rootVisual)
	{
		_mainController.View.AddSubview(_rootVisual);
		_rootVisual.Frame = _mainController.View.Bounds;
		_rootVisual.AutoresizingMask = UIViewAutoresizing.All;
	}
}
