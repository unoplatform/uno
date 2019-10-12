#if __ANDROID__
using Android.App;
using Android.Content.PM;
using Android.Support.V4.Content.PM;

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
	}
}
#endif
