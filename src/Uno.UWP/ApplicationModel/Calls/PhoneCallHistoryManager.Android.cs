#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;
using Windows.Foundation;

namespace Windows.ApplicationModel.Calls;

/// <summary>
/// Provides APIs for the application to get access to the PhoneCallHistoryStore.
/// </summary>
public static partial class PhoneCallHistoryManager
{
	/// <summary>
	/// Requests the PhoneCallHistoryStore associated with the calling application.
	/// </summary>
	/// <remarks>
	/// AppEntriesReadWrite access type is not supported.
	/// </remarks>
	/// <param name="accessType">The type of access requested for the PhoneCallHistoryStore object.</param>
	/// <returns></returns>
	/// <exception cref="NotSupportedException">For unsupported access type.</exception>
	/// <exception cref="InvalidOperationException">When application package is invalid.</exception>
	/// <exception cref="UnauthorizedAccessException"></exception>
	public static IAsyncOperation<PhoneCallHistoryStore?> RequestStoreAsync(PhoneCallHistoryStoreAccessType accessType) =>
		RequestStoreAsyncTask(accessType).AsAsyncOperation<PhoneCallHistoryStore?>();	
	
	private static async Task<PhoneCallHistoryStore?> RequestStoreAsyncTask(PhoneCallHistoryStoreAccessType accessType)
	{
		if (accessType == PhoneCallHistoryStoreAccessType.AppEntriesReadWrite)
		{
			// Should not happen, as this option is marked as NotImplemented
			throw new NotSupportedException("PhoneCallHistoryManager.RequestStoreAsyncTask, accessType AppEntriesReadWrite is not implemented for Android");
		}

		var historyStore = new PhoneCallHistoryStore();

		// Below API 16 (JellyBean), permission are granted automatically
		if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
		{
			return historyStore;
		}

		// Since API 29, more checks are required:
		// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E

		var context = Android.App.Application.Context;
		var packageManager = context.PackageManager;
		if (packageManager is null)
		{
			throw new InvalidOperationException("Context.PackageManager was null");
		}

		var packageName = context.PackageName;
		if (packageName is null)
		{
			throw new InvalidOperationException("Context.PackageName was null");
		}

#pragma warning disable CS0618 // Type or member is obsolete
		var packageInfo = packageManager.GetPackageInfo(packageName, PackageInfoFlags.Permissions);
#pragma warning restore CS0618 // Type or member is obsolete
		var requestedPermissions = packageInfo?.RequestedPermissions;
		if (requestedPermissions is null)
		{
			throw new UnauthorizedAccessException("No permissions in Manifest declared");
		}

		if (!Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadCallLog))
		{
			throw new InvalidOperationException($"{Android.Manifest.Permission.ReadCallLog} permission is required to read call log.");
		}

		// required for contact name
		if (!Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadContacts))
		{
			throw new InvalidOperationException($"{Android.Manifest.Permission.ReadContacts} permission is required to read contacts.");
		}

		if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite &&
			!Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteCallLog))
		{
			throw new InvalidOperationException($"{Android.Manifest.Permission.WriteCallLog} permission is required to write in call log.");
		}

		List<string> requestPermission = new();

		// check what permission should be granted
		if (!await Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.ReadCallLog))
		{
			requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
		}

		if (!await Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.ReadContacts))
		{
			requestPermission.Add(Android.Manifest.Permission.ReadContacts);
		}

		if (accessType == PhoneCallHistoryStoreAccessType.AllEntriesReadWrite &&
			!await Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, Android.Manifest.Permission.WriteCallLog))
		{
			requestPermission.Add(Android.Manifest.Permission.WriteCallLog);
		}

		var allOk = true;
		if (requestPermission.Count > 0)
		{
			foreach (var sPerm in requestPermission)
			{
				if (!await Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, sPerm))
				{
					allOk = false;
					break;
				}
			}
		}

		return allOk ? historyStore : null;
	}
}
