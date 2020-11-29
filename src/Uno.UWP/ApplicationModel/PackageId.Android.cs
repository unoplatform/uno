#if __ANDROID__
using Android.App;
using Android.Content.PM;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private PackageInfo _packageInfo;

		partial void InitializePlatform()
		{
			_packageInfo = Application.Context.PackageManager.GetPackageInfo(
				Application.Context.PackageName,
				PackageInfoFlags.MetaData);
		}

		public string FamilyName => _packageInfo.PackageName;

		public string FullName => $"{_packageInfo.PackageName}_{GetVersionCode()}";

		public string Name => _packageInfo.PackageName;

		public PackageVersion Version
		{
			get
			{
				if (SystemVersion.TryParse(_packageInfo.VersionName, out var userVersion))
				{
					return new PackageVersion(userVersion);
				}
				var packageLongVersion = GetVersionCode();
				if (0 <= packageLongVersion && packageLongVersion <= ushort.MaxValue)
				{
					return new PackageVersion((ushort)packageLongVersion);
				}
				return new PackageVersion();
			}
		}

		private long GetVersionCode()
		{
#if __ANDROID_28__
			return AndroidX.Core.Content.PM.PackageInfoCompat.GetLongVersionCode(_packageInfo);
#else
			return _packageInfo.VersionCode;
#endif
		}
	}
}
#endif
