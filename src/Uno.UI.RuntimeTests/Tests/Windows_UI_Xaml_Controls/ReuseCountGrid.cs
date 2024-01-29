using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class ReuseCountGrid : Grid
#if !WINAPPSDK
		, IFrameworkTemplatePoolAware
#endif
	{
		public static int GlobalReuseCount { get; private set; }

#if !WINAPPSDK
		public void OnTemplateRecycled()
		{
			GlobalReuseCount++;
		}
#endif
	}
}
