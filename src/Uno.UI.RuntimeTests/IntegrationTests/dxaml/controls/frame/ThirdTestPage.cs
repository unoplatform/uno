using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.IntegrationTests;

internal partial class ThirdTestPage : Page
{
	public ThirdTestPage()
	{
		NavigationCacheMode = CacheMode;
		InstanceCounter = Counter;
	}

	public static new NavigationCacheMode CacheMode { get; set; } = NavigationCacheMode.Disabled;

	public static int Counter { get; set; }

	public int InstanceCounter { get; set; }
}
