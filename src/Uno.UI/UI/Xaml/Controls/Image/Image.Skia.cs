using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
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

		/// <summary>
		/// When set, the resulting image is tentatively converted to Monochrome.
		/// </summary>
		internal Color? MonochromeColor { get; set; }
	}
}
