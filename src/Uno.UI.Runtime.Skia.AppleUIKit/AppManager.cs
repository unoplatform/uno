using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal static class AppManager
{
	internal static XamlRootMap<IAppleUIKitXamlRootHost> XamlRootMap { get; } = new();
}
