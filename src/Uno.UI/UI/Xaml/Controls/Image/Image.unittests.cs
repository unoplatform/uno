using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;

using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

		private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

		internal override bool IsViewHit() => Source != null || base.IsViewHit();
	}
}
