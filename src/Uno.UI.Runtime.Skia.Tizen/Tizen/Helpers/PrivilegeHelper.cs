#nullable enable

using System.Linq;
using Tizen.Applications;

namespace Uno.UI.Runtime.Skia.Tizen.Helpers
{
	internal static class PrivilegeHelper
	{
		/// <summary>
		/// Checks whether the given privilege is declared.
		/// </summary>
		/// <param name="privilege">Privilege.</param>
		/// <returns>A value indicating whether the privilege is declared.</returns>
		public static bool IsDeclared(string privilege)
		{
			var packageId = Application.Current.ApplicationInfo.PackageId;
			var package = PackageManager.GetPackage(packageId);

			return package.Privileges.Any(p => p == privilege);
		}
	}
}
