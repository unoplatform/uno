#if __ANDROID__
using Android.App;
using Android.Content.PM;
using Android.Support.V4.Content.PM;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private readonly PackageInfo _packageInfo;

		public PackageId()
		{
			_packageInfo = Application.Context.PackageManager.GetPackageInfo(
				Application.Context.PackageName,
				PackageInfoFlags.MetaData);
		}

		public string FamilyName => Application.Context.PackageName;

		public string FullName => $"{Application.Context.PackageName}_{PackageInfoCompat.GetLongVersionCode(_packageInfo)}";

		public string Name => _packageInfo.PackageName;

		public PackageVersion Version
		{
			get
			{
				if (SystemVersion.TryParse(_packageInfo.VersionName, out var userVersion))
				{
					return new PackageVersion(userVersion);
				}
				var packageLongVersion = PackageInfoCompat.GetLongVersionCode(_packageInfo);
				if (0 <= packageLongVersion && packageLongVersion <= ushort.MaxValue)
				{
					return new PackageVersion((ushort)packageLongVersion);
				}
				return new PackageVersion();
			}
		}
	}
}
#endif
