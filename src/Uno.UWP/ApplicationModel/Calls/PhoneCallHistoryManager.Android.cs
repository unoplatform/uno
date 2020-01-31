
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

		private static async Task<PhoneCallHistoryStore> RequestStoreAsyncTask(PhoneCallHistoryStoreAccessType accessType)
		{
			// UWP: AppEntriesReadWrite, AllEntriesLimitedReadWrite, AllEntriesReadWrite
			// Android: Manifest has READ_CALL_LOG and WRITE_CALL_LOG, no difference between app/limited/full
			// using: AllEntriesReadWrite as ReadWrite, and AllEntriesLimitedReadWrite as ReadOnly

			if(accessType == PhoneCallHistoryStoreAccessType.AppEntriesReadWrite)
			{   // should not happen, as this option is defined as NotImplemented in enum
				throw new NotImplementedException();
			}


			var _histStore = new PhoneCallHistoryStore();

			// below API 16 (JellyBean), permission are granted
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{
				return _histStore;
			}

			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			// do we have declared this permission in Manifest?
			// it could be also Coarse, without GPS
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
			{
				throw new UnauthorizedAccessException("no permissions in Manifest defined (no permission at all)");
			}

			if(!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadCallLog))
				return null;

			// required for contact name
			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadContacts))
				return null;

			if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteCallLog))
					return null;
			}

			List<string> requestPermission = new List<string>();

			// check what permission should be granted
			if (! await Windows.Extensions.PermissionsHelper.CheckPermission(Android.Manifest.Permission.ReadCallLog))
			{
				requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
			}

			if (! await Windows.Extensions.PermissionsHelper.CheckPermission(Android.Manifest.Permission.ReadContacts))
			{
				requestPermission.Add(Android.Manifest.Permission.ReadContacts);
			}

			if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (!await Windows.Extensions.PermissionsHelper.CheckPermission(Android.Manifest.Permission.WriteCallLog))
				{
					requestPermission.Add(Android.Manifest.Permission.WriteCallLog);
				}
			}

			if (requestPermission.Count < 1)
				return _histStore;

			// in my tests, this ends with ERROR!! about null object reference inside TryGetPermission, on second call
			foreach (var sPerm in requestPermission)
				await Windows.Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, sPerm);

			// system dialog asking for permission

			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
			//	var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			//	void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			//	{

			//		if (e.RequestCode == 1)
			//		{
			//			tcs.TrySetResult(e);
			//		}
			//	}

			//	var current = Uno.UI.BaseActivity.Current;

			//	try
			//	{
			//		current.RequestPermissionsResultWithResults += handler;

			//		ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, requestPermission.ToArray(), 1);

			//		var result = await tcs.Task;
			//		if (result.GrantResults.Length < 1)
			//			return null;
			//              foreach(var oItem in result.GrantResults)
			//              {
			//                  if (oItem == Android.Content.PM.Permission.Denied)
			//                      return null;
			//              }
			//		return _histStore;

			//	}
			//	finally
			//	{
			//		current.RequestPermissionsResultWithResults -= handler;
			//	}


			//	return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;

			//}

			return _histStore;
		}

		public static IAsyncOperation<PhoneCallHistoryStore> RequestStoreAsync(PhoneCallHistoryStoreAccessType accessType) => RequestStoreAsyncTask(accessType).AsAsyncOperation<PhoneCallHistoryStore>();
	}
}

#endif
