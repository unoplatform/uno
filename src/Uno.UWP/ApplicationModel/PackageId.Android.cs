using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content.PM;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel
{
	public partial class PackageId
	{
		private PackageInfo _packageInfo;

		[MemberNotNull(nameof(_packageInfo))]
		partial void InitializePlatform()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			_packageInfo = Application.Context.PackageManager!.GetPackageInfo(
				Application.Context.PackageName!,
				PackageInfoFlags.MetaData)!;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public string? FamilyName => _packageInfo.PackageName;

		public string FullName => $"{_packageInfo.PackageName}_{GetVersionCode()}";

		public string? Name => _packageInfo.PackageName;

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
			return AndroidX.Core.Content.PM.PackageInfoCompat.GetLongVersionCode(_packageInfo);
		}
	}
}
