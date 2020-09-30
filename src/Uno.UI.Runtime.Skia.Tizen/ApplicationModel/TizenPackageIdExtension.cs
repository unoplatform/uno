#nullable enable

using Tizen.Applications;
using Uno.ApplicationModel;
using Windows.ApplicationModel;
using Package = Tizen.Applications.Package;
using SystemVersion = System.Version;

namespace Uno.UI.Runtime.Skia.Tizen.ApplicationModel
{
	public class TizenPackageIdExtension : IPackageIdExtension
	{
		private Package? _package;

		public TizenPackageIdExtension(object owner)
		{
		}

		public string FamilyName => GetPackage().Label;

		public string FullName => $"{GetPackage().Label}_{GetNativeVersion()}";

		public string Name => Application.Current.ApplicationInfo.Label;

		public PackageVersion Version
		{
			get
			{
				if (SystemVersion.TryParse(GetNativeVersion(), out var userVersion))
				{
					return new PackageVersion(userVersion);
				}

				// Fallback to default
				return new PackageVersion();
			}
		}

		private string GetNativeVersion() => GetPackage().Version;

		private Package GetPackage()
		{
			if (_package == null)
			{
				var packageId = Application.Current.ApplicationInfo.PackageId;
				_package = PackageManager.GetPackage(packageId);
			}

			return _package;
		}
	}
}
