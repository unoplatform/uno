#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using XamlGenerationTests;

namespace XamlGenerationTests.Shared
{
	public static class GlobalStaticResourcesTest
	{
		static GlobalStaticResourcesTest()
		{
			var res = GlobalStaticResources.OtherResources01.ToString();

			GlobalStaticResources.FindResource("test");
		}
	}
}

#endif
