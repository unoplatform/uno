using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.AppleUIKit.Hosting;

internal interface IAppleUIKitXamlRootHost : IXamlRootHost
{
	UIView TextInputLayer { get; }

	UIView? NativeOverlayLayer { get; }
}
