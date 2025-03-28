#nullable enable

using System;
using AppKit;
using CoreGraphics;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Foundation;
using Uno.UI;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void AttachToWindowPlatform(UIElement rootElement, Windows.UI.Xaml.Window window)
	{
		NativeWindowWrapper.Instance.MainController.View = rootElement;
		rootElement.Frame = NativeWindowWrapper.Instance.NativeWindow.Frame;
		rootElement.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		var windowSize = NativeWindowWrapper.Instance.GetWindowSize();
		// This is required to get the mouse move while not pressed!
		var options = NSTrackingAreaOptions.MouseEnteredAndExited
			| NSTrackingAreaOptions.MouseMoved
			| NSTrackingAreaOptions.ActiveInKeyWindow
			| NSTrackingAreaOptions.EnabledDuringMouseDrag // We want enter/leave events even if the button is pressed
			| NSTrackingAreaOptions.InVisibleRect; // Automagicaly syncs the bounds rect
		var trackingArea = new NSTrackingArea(new CGRect(0, 0, windowSize.Width, windowSize.Height), options, rootElement, null);

		rootElement.AddTrackingArea(trackingArea);
	}

	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement)
	{
		if (xamlRoot.HostWindow is null)
		{
			throw new InvalidOperationException("Host window must be set on macOS currently");
		}
		xamlRoot.VisualTree.ContentRoot.SetHost(xamlRoot.HostWindow);
	}
}
