using System;
using CoreGraphics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal partial class AppleUIKitWindow : UIWindow
{
	internal event Action? FrameChanged;

	internal AppleUIKitWindow()
	{
	}

	internal AppleUIKitWindow(UIWindowScene scene) : base(scene)
	{
	}

	public override CGRect Frame
	{
		get => base.Frame;
		set
		{
			var frameChanged = base.Frame != value;

			base.Frame = value;

			if (frameChanged)
			{
				FrameChanged?.Invoke();
			}
		}
	}
}
