#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace XamlGenerationTests.Shared
{
    public partial class PropertiesTest
	{
		public PropertiesTest()
		{
#if XAMARIN_IOS
			iOSUILabel.ToString();
#endif

#if XAMARIN_ANDROID
			AndroidTextView.ToString();
#endif

			GradientStopEffect.ToString();
			testRun.ToString();
			rtbRun.ToString();
		}
	}
}
