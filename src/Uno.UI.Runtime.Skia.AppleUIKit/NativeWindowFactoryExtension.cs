using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsClosingCancellation => false;

	// TODO Uno: While supported by the OS, we currently only support single window. Later switch to UIApplication.SharedApplication.SupportsMultipleScenes;
	public bool SupportsMultipleWindows => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot) =>
		new NativeWindowWrapper(window, xamlRoot);
}
