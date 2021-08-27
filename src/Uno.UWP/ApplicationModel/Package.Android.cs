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
#pragma warning disable CA1822 // Mark members as static
		public string DisplayName =>
#pragma warning restore CA1822 // Mark members as static
			Application.Context.ApplicationInfo.LoadLabel(Application.Context.PackageManager);

		private static string GetInstalledLocation()
			=> "assets://" + ContextHelper.Current.PackageCodePath;

		private static bool GetInnerIsDevelopmentMode()
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

		private static DateTimeOffset GetInstallDate()
		{
			var packageInfo = ContextHelper.Current.PackageManager.GetPackageInfo(ContextHelper.Current.PackageName, 0);

			return DateTimeOffset.FromUnixTimeMilliseconds(packageInfo.FirstInstallTime);
		}
	}
}
#endif
