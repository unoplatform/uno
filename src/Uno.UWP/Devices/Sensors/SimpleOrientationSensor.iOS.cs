#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Foundation;
using UIKit;
using Windows.Graphics.Display;

namespace Windows.Devices.Sensors
{
	public partial class SimpleOrientationSensor
	{
		private object _orientationDidChangeObserverToken;

		partial void Initialize()
		{
			_orientationDidChangeObserverToken = NSNotificationCenter
				.DefaultCenter
				.AddObserver(UIDevice.OrientationDidChangeNotification, n => UpdateCurrentOrientation());
			UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
			
			UpdateCurrentOrientation();
		}

		private void UpdateCurrentOrientation()
		{
			var deviceOrientation = UIDevice.CurrentDevice.Orientation;
			var simpleOrientation = ToSimpleOrientation(deviceOrientation);
			
			SetCurrentOrientation(simpleOrientation);
		}

		private static SimpleOrientation ToSimpleOrientation(UIDeviceOrientation deviceOrientation)
		{
			switch (deviceOrientation)
			{
				case UIDeviceOrientation.Portrait:
					return SimpleOrientation.NotRotated;
				case UIDeviceOrientation.PortraitUpsideDown:
					return SimpleOrientation.Rotated180DegreesCounterclockwise;
				case UIDeviceOrientation.LandscapeLeft:
					return SimpleOrientation.Rotated90DegreesCounterclockwise;
				case UIDeviceOrientation.LandscapeRight:
					return SimpleOrientation.Rotated270DegreesCounterclockwise;
				case UIDeviceOrientation.FaceUp:
					return SimpleOrientation.Faceup;
				case UIDeviceOrientation.FaceDown:
					return SimpleOrientation.Facedown;
				case UIDeviceOrientation.Unknown:
				default:
					return SimpleOrientation.NotRotated;
			}
		}
	}
}
#endif