using System.Linq;
using Tizen.Applications;

namespace Uno.UI.Runtime.Skia.Tizen.Helpers
{
	internal static class PrivilegeHelper
    {
		public static bool IsDeclared(string privilege)
		{
			var packageId = Application.Current.ApplicationInfo.PackageId;
			var package = PackageManager.GetPackage(packageId);

			return package.Privileges.Any(p => p == privilege);
		}
    }
}
