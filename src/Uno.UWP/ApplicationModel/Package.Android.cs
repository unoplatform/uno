#if __ANDROID__
using System;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Uno.Extensions;
using Uno.UI;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		public string DisplayName =>
			Application.Context.ApplicationInfo.LoadLabel(Application.Context.PackageManager);

		private string GetInstalledLocation()
			=> "assets://" + ContextHelper.Current.PackageCodePath;

		private bool GetInnerIsDevelopmentMode()
		{
			try
			{
				var installer = ContextHelper.Current.PackageManager.GetInstallerPackageName(ContextHelper.Current.PackageName);
				return !installer.HasValue();
			}
			catch(Exception)
			{
				return false;
			}
		}

		private DateTimeOffset GetInstallDate()
		{
			var packageInfo = ContextHelper.Current.PackageManager.GetPackageInfo(ContextHelper.Current.PackageName, 0);

			return DateTimeOffset.FromUnixTimeMilliseconds(packageInfo.FirstInstallTime);
		}
	}
}
#endif
