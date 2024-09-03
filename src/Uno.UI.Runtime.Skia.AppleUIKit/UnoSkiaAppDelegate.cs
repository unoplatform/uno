using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

[Register("UnoSkiaAppDelegate")]
internal partial class UnoSkiaAppDelegate : UIApplicationDelegate
{
	public override void FinishedLaunching(UIApplication application)
	{
		Application.Start(_ => PlatformHost.AppBuilder?.Invoke());
	}
}
