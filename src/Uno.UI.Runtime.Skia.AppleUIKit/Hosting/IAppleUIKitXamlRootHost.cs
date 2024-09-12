using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.AppleUIKit.Hosting;

internal interface IAppleUIKitXamlRootHost : IXamlRootHost
{
	UIView TextInputLayer { get; }
}
