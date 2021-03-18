using System;

using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Foundation;
using AppKit;
using CoreGraphics;
using Uno.Disposables;
using Uno.Diagnostics.Eventing;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image
	{

		private void SetImage(CGImage cgImage, CGSize size) => SetImage(new NSImage(cgImage, size));

		private void UpdateContentMode(Stretch stretch)
		{
			if (_native == null)
			{
				return;
			}
			switch (stretch)
			{
				case Stretch.Uniform:
					_native.ImageScaling = NSImageScale.AxesIndependently;
					break;

				case Stretch.None:
					_native.ImageScaling = NSImageScale.None;
					break;

				case Stretch.UniformToFill:
					_native.ImageScaling = NSImageScale.ProportionallyUpOrDown;
					break;

				case Stretch.Fill:
					_native.ImageScaling = NSImageScale.ProportionallyUpOrDown;
					break;

				default:
					throw new NotSupportedException("Stretch mode {0} is not supported".InvariantCultureFormat(stretch));
			}
		}
	}
}

