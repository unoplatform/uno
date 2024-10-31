#nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowFactory
{
	public static bool SupportsClosingCancellation => false;

	public static bool SupportsMultipleWindows => false;

	private static INativeWindowWrapper? CreateWindowPlatform(Microsoft.UI.Xaml.Window window, XamlRoot xamlRoot) =>
		new NativeWindowWrapper(window, xamlRoot);
}
