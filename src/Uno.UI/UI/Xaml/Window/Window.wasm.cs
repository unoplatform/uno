using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		[JSExport]
		[Preserve]
		public static void Resize(double width, double height) => NativeWindowWrapper.Instance.RaiseNativeSizeChanged(width, height);
	}
}
