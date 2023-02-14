using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class ReuseCountGrid : Grid
#if !NETFX_CORE
		, IFrameworkTemplatePoolAware
#endif
	{
		public static int GlobalReuseCount { get; private set; }

#if !NETFX_CORE
		public void OnTemplateRecycled()
		{
			GlobalReuseCount++;
		}
#endif
	}
}
