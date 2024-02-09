#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	public static bool SupportsMultipleWindows => true;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		return new NativeWindowWrapper(window, xamlRoot);
	}
}
