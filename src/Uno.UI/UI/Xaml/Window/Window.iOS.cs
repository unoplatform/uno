using CoreGraphics;
using Foundation;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UIKit;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Uno.UI.Controls;
using System.Drawing;
using Windows.UI.ViewManagement;
using Uno.UI;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public sealed partial class Window
{
	/// <summary>
	/// A function to generate a custom view controller which inherits from <see cref="RootViewController"/>.
	/// This must be set before the <see cref="Window"/> is created (typically when Current is called for the first time),
	/// otherwise it will have no effect.
	/// </summary>
	public static Func<RootViewController> ViewControllerGenerator { get; set; }

	
}
