using System;
using System.Collections.Generic;
using System.Text;

namespace XamlGenerationTests.Shared
{
	public partial class PropertiesTest
	{
		public PropertiesTest()
		{
#if __IOS__
			iOSUILabel.ToString();
#endif

#if __ANDROID__
			AndroidTextView.ToString();
#endif

			GradientStopEffect.ToString();
			testRun.ToString();
			rtbRun.ToString();
		}
	}
}
