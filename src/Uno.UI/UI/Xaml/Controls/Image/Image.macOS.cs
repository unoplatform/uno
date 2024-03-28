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
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls;

public partial class Image
{
	private void UpdateContentMode(Stretch stretch)
	{
		if (_nativeImageView == null)
		{
			return;
		}
		switch (stretch)
		{
			case Stretch.Uniform:
				_nativeImageView.ImageScaling = NSImageScale.AxesIndependently;
				break;

			case Stretch.None:
				_nativeImageView.ImageScaling = NSImageScale.None;
				break;

			case Stretch.UniformToFill:
				_nativeImageView.ImageScaling = NSImageScale.ProportionallyUpOrDown;
				break;

			case Stretch.Fill:
				_nativeImageView.ImageScaling = NSImageScale.ProportionallyUpOrDown;
				break;

			default:
				throw new NotSupportedException("Stretch mode {0} is not supported".InvariantCultureFormat(stretch));
		}
	}
}

