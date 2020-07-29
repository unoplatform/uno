#if !(__IOS__ || __ANDROID__ || __MACOS__)
using System;
using System.Collections.Generic;
using System.Reflection;
using Uno.Extensions;
using Uno.UI;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private bool GetInnerIsDevelopmentMode() => false;

		private DateTimeOffset GetInstallDate() => DateTimeOffset.Now;

		private string GetInstalledLocation()
		{
			if (Assembly.GetEntryAssembly() is Assembly assembly)
			{
				return global::System.IO.Path.GetDirectoryName(new Uri(assembly.Location).LocalPath);
			}
			else
			{
				return Environment.CurrentDirectory;
			}
		}
	}
}
#endif
