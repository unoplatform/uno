#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace Windows.System
{
	public partial class Launcher
	{
		private async Task<bool> HandleSpecialUriAsync(Uri uri)
		{
			if (!uri.IsAbsoluteUri) return false;

			switch (uri.Scheme.ToLowerInvariant())
			{
				case "ms-settings":
					return await HandleSettingsUriAsync(uri);
			}
		}

		private async Task<bool> HandleSettingsUriAsync(Uri uri)
		{
			//App perfs URIs found at https://gist.github.com/deanlyoung/368e274945a6929e0ea77c4eca345560
			if (uri.Host == "")
			{
				UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString));
			}
			else if (uri.Host.Equals("network-airplanemode", StringComparison.InvariantCultureIgnoreCase))
			{

			}
		}
	}
}
#endif
