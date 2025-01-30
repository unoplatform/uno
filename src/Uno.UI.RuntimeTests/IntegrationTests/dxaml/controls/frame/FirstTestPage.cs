using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.IntegrationTests;

internal partial class FirstTestPage : Page
{
	public FirstTestPage()
	{
		NavigationCacheMode = CacheMode;
		InstanceCounter = Counter;
	}

	public static new NavigationCacheMode CacheMode { get; set; } = NavigationCacheMode.Disabled;

	public static int Counter { get; set; }

	public int InstanceCounter { get; set; }
}
