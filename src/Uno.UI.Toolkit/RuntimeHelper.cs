using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Helpers;

namespace Uno.UI.Toolkit;

public static class RuntimeHelper
{
	public static UnoRuntimePlatform CurrentPlatform => PlatformRuntimeHelper.Current;
}
