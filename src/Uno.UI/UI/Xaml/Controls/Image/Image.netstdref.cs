using System;
using System.IO;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;
	}
}
