#if __IOS__
using System;
using System.Globalization;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

 namespace Windows.System.Profile
{
	public  partial class AnalyticsVersionInfo 
	{
		public  string DeviceFamily
		{
			get
			{
				return UIKit.UIDevice.CurrentDevice.UserInterfaceIdiom.ToString();
			}
		}

 		public  string DeviceFamilyVersion
		{
			get
			{
				return UIKit.UIDevice.CurrentDevice.SystemVersion;
			}
		}
	}
}
#endif
