#nullable enable

#if __ANDROID__


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallHistoryManager
	{

		private static async Task<PhoneCallHistoryStore?> RequestStoreAsyncTask(PhoneCallHistoryStoreAccessType accessType)
		{
			// UWP: AppEntriesReadWrite, AllEntriesLimitedReadWrite, AllEntriesReadWrite
			// Android: Manifest has READ_CALL_LOG and WRITE_CALL_LOG, no difference between app/limited/full
			// using: AllEntriesReadWrite as ReadWrite, and AllEntriesLimitedReadWrite as ReadOnly

			if(accessType == PhoneCallHistoryStoreAccessType.AppEntriesReadWrite)
			{   // should not happen, as this option is defined as NotImplemented in enum
				throw new NotSupportedException("PhoneCallHistoryManager.RequestStoreAsyncTask, accessType AppEntriesReadWrite is not implemented for Android");
			}


			var historyStore = new PhoneCallHistoryStore();

			// below API 16 (JellyBean), permission are granted
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{
				return historyStore;
			}

			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			var context = Android.App.Application.Context;
			var packageManager = context.PackageManager;
			if (packageManager is null)
			{
				throw new InvalidOperationException("Windows.ApplicationModel.Calls.PhoneCallHistoryManager.RequestStoreAsyncTask, PackageManager is null (impossible)");
			}

			var packageName = context.PackageName;
			if(packageName is null)
			{
				throw new InvalidOperationException("Windows.ApplicationModel.Calls.PhoneCallHistoryManager.RequestStoreAsyncTask, PackageName is null (impossible)");
			}
			var packageInfo =
				packageManager.GetPackageInfo(packageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
			{
				throw new UnauthorizedAccessException("no permissions in Manifest defined (no permission at all)");
			}

			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadCallLog))
			{
				return null;
			}

			// required for contact name
			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadContacts))
			{
				return null;
			}

			if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteCallLog))
				{
					return null;
				}
			}

			List<string> requestPermission = new ();

			// check what permission should be granted
			if (! await Windows.Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.ReadCallLog))
			{
				requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
			}

			if (! await Windows.Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.ReadContacts))
			{
				requestPermission.Add(Android.Manifest.Permission.ReadContacts);
			}

			if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (!await Windows.Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.WriteCallLog))
				{
					requestPermission.Add(Android.Manifest.Permission.WriteCallLog);
				}
			}

			if (requestPermission.Count < 1)
				return historyStore;

			foreach (var sPerm in requestPermission)
			{
				await Windows.Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, sPerm);
			}

			return historyStore;
		}

		public static IAsyncOperation<PhoneCallHistoryStore?> RequestStoreAsync(PhoneCallHistoryStoreAccessType accessType) => RequestStoreAsyncTask(accessType).AsAsyncOperation<PhoneCallHistoryStore?>();
	}
}

#endif
