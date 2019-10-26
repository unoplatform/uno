#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Uno.Extensions;
using Uno.UI;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private const string BundleDisplayNameKey = "CFBundleDisplayName";
		
		public string DisplayName => NSBundle.MainBundle.InfoDictionary[BundleDisplayNameKey]?.ToString() ?? string.Empty;

		private bool GetInnerIsDevelopmentMode() => IsAdHoc;

		private string GetInstalledLocation() 
			=> NSBundle.MainBundle.BundlePath;

		private DateTimeOffset GetInstallDate()
		{
			var urlToDocumentsFolder = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentationDirectory, NSSearchPathDomain.User);

			var installDate = NSFileManager.DefaultManager.GetAttributes(urlToDocumentsFolder[0].Path, out var error).CreationDate;

			return (DateTimeOffset)(DateTime)installDate;
		}
		
		private static bool IsAdHoc
			// See https://github.com/bitstadium/HockeySDK-iOS/blob/develop/Classes/BITHockeyHelper.m
			=> NSBundle.MainBundle.PathForResource("embedded", "mobileprovision").HasValue();
	}
}
#endif
