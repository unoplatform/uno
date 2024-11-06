using System;
using Foundation;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using UIKit;

namespace Uno.UI.Controls;

public partial class RootViewController
{
	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}

	public override void MotionEnded(UIEventSubtype motion, UIEvent evt)
	{
		if (motion == UIEventSubtype.MotionShake)
		{
			Accelerometer.HandleShake();
		}
		base.MotionEnded(motion, evt);
	}


	public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();
}
