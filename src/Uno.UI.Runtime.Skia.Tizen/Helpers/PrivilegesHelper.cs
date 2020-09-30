#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.Security;

namespace Uno.UI.Runtime.Skia.Tizen.Helpers
{
	internal class PrivilegesHelper
	{
		internal static void EnsureDeclared(string privilege)
		{
			var packageId = Application.Current.ApplicationInfo.PackageId;
			var packageManager = PackageManager.GetPackage(packageId);

			if (!packageManager.Privileges.Contains(privilege))
			{
				throw new InvalidOperationException($"Privilege {privilege} must be declared in the Tizen application manifest.");
			}
		}

		internal static async Task<bool> RequestAsync(string privilege)
		{
			EnsureDeclared(privilege);

			var checkResult = PrivacyPrivilegeManager.CheckPermission(privilege);
			if (checkResult == CheckResult.Ask)
			{
				var completionSource = new TaskCompletionSource<bool>();
				if (PrivacyPrivilegeManager.GetResponseContext(privilege).TryGetTarget(out var context))
				{
					void OnResponseFetched(object sender, RequestResponseEventArgs e)
					{
						completionSource.TrySetResult(e.result == RequestResult.AllowForever);
					}
					context.ResponseFetched += OnResponseFetched;
					PrivacyPrivilegeManager.RequestPermission(privilege);
					var result = await completionSource.Task;
					context.ResponseFetched -= OnResponseFetched;
					return result;
				}
				return false;
			}
			else if (checkResult == CheckResult.Deny)
			{
				return false;
			}
			return true;
		}
	}
}
