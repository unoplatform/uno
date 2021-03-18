#nullable enable
#if !(__IOS__ || __ANDROID__ || __MACOS__)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Windows.ApplicationModel.Email.DataProvider;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private const string PackageManifestName = "Package.appxmanifest";
		private static Assembly? _entryAssembly;
		private string _displayName = "";
		private string _logo = "ms-appx://logo";
		private bool _manifestParsed;

		private bool GetInnerIsDevelopmentMode() => false;

		private DateTimeOffset GetInstallDate() => DateTimeOffset.Now;

		private string GetInstalledLocation()
		{
			if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
			{
				return global::System.IO.Path.GetDirectoryName(AppContext.BaseDirectory) ?? "";
			}
			else
			{
				return Environment.CurrentDirectory;
			}
		}

		public string DisplayName
		{
			get
			{
				TryParsePackageManifest();
				return _displayName;
			}
		}

		public Uri Logo
		{
			get
			{
				TryParsePackageManifest();
				return new Uri(_logo, UriKind.RelativeOrAbsolute);
			}
		}

		internal static void SetEntryAssembly(Assembly entryAssembly)
		{
			_entryAssembly = entryAssembly;
		}

		private void TryParsePackageManifest()
		{
			if(_entryAssembly != null && !_manifestParsed)
			{
				_manifestParsed = true;

				var manifest = _entryAssembly.GetManifestResourceStream(PackageManifestName);

				if (manifest != null)
				{
					try
					{
						var doc = new XmlDocument();
						doc.Load(manifest);

						var nsmgr = new XmlNamespaceManager(doc.NameTable);
						nsmgr.AddNamespace("d", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");

						_displayName = doc.SelectSingleNode("/d:Package/d:Properties/d:DisplayName", nsmgr)?.InnerText ?? "";
						_logo = doc.SelectSingleNode("/d:Package/d:Properties/d:Logo", nsmgr)?.InnerText ?? "";
					}
					catch (Exception ex)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to read manifest [{PackageManifestName}]", ex);
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Skipping manifest reading, unable to find [{PackageManifestName}]");
					}
				}
			}
		}
	}
}
#endif
