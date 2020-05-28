#if !(__IOS__ || __ANDROID__ || __MACOS__)
using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private bool GetInnerIsDevelopmentMode() => false;

		private DateTimeOffset GetInstallDate() => DateTimeOffset.Now;

		private string GetInstalledLocation() => Environment.CurrentDirectory;
	}
}
#endif
