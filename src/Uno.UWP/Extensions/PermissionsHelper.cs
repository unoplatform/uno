#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Uno;
using Uno.Extensions;
using Uno.UI;

namespace Windows.Extensions
{
	/// <summary>
	/// A service that determines if a permission is granted and if not, request it to the user.
	/// </summary>
	public partial class PermissionsHelper
	{
		private static FuncAsync<string, bool> _tryGetPermission;
		private static FuncAsync<string, bool> _checkPermission;
		private static Func<string[], string[], string[]> _getMissingPermissions;
		private static FuncAsync<string[], string[], bool, bool> _tryGetPermissionsAsync;

		internal static void Initialize(FuncAsync<string, bool> getter, FuncAsync<string, bool> checkPermission, Func<string[], string[], string[]> getMissingPermissions, FuncAsync<string[], string[], bool, bool> tryGetPermissionsAsync)
		{
			_tryGetPermission = getter;
			_checkPermission = checkPermission;
			_getMissingPermissions = getMissingPermissions;
			_tryGetPermissionsAsync = tryGetPermissionsAsync;
		}

		/// <summary>
		/// Return null on error, or array of permission to be asked for (not granted at this time). Both parameters can be null
		/// </summary>
		/// <param name="requiredPermissions">Permissions that are required. Can be null.</param>
		/// <param name="optionalPermissions">Permissions that are required only if declared in Manifest. Can be null.</param>
		/// <returns>Array of all permission that is not granted at this moment (can be empty!), or null if some error occured</returns>
		public static string[] MissingPermissions(string[] requiredPermissions, string[] optionalPermissions)
			=> _getMissingPermissions(requiredPermissions, optionalPermissions);

		/// <summary>
		/// Return null on error, or array of permission to be asked for (not granted at this time).
		/// </summary>
		/// <param name="requiredPermission">Permission that is </param>
		/// <returns>Array with permission if it is not granted at this moment (can be empty!), or null if some error occured</returns>
		public static string[] MissingPermissions(string requiredPermission)
			=> _getMissingPermissions(new string[] {requiredPermission}, null);

		/// <summary>
		/// Return true if requiredPermission is granted. Show dialog asking for missing permission.
		/// </summary>
		/// <param name="requiredPermission">Permission that are required</param>
		/// <returns>Bool if permission is granted, false if not</returns>
		public static Task<bool> TryAskPermissionAsync(string requiredPermission)
			=> TryAskPermissionAsync(new string[] { requiredPermission }, null);

		/// <summary>
		/// Return true if granted are: requiredPermission, and optionalPermissions (if mentioned in Manifest). Show dialog asking for missing permissions..
		/// </summary>
		/// <param name="requiredPermissions">Permission that are required</param>
		/// <param name="optionalPermissions">Permission that are required only if declared in Manifest</param>
		/// <returns>Bool if all permissions are granted, false if any of them is not granted</returns>
		public static Task<bool> TryAskPermissionAsync(string requiredPermission, string optionalPermission)
			=> TryAskPermissionAsync(new string[] { requiredPermission }, new string[] { optionalPermission });


		/// <summary>
		/// Return true if granted are: all requiredPermissions, and all optionalPermissions mentioned in Manifest. Show dialog asking for missing permissions. Both parameters can be null.
		/// </summary>
		/// <param name="requiredPermissions">Permissions that are required. Can be null.</param>
		/// <param name="optionalPermissions">Permissions that are required only if declared in Manifest. Can be null.</param>
		/// <param name="ignoreErrors">if 'true', then do not throw exception on error, and simply retur false</param>
		/// <returns>Bool if all permissions are granted, false if any of them is not granted</returns>
		public static Task<bool> TryAskPermissionAsync(string[] requiredPermissions, string[] optionalPermissions, bool ignoreErrors = false)
			=> _tryGetPermissionsAsync(CancellationToken.None, requiredPermissions, optionalPermissions, ignoreErrors);


		/// <summary>
		/// Checks if the given Android permission is declared in manifest file.
		/// </summary>
		/// <param name="permission">Permission.</param>
		/// <returns></returns>
		public static bool IsDeclaredInManifest(string permission)
		{
			var context = Application.Context;
			var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;

			return requestedPermissions?.Any(r => r.Equals(permission, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		/// <summary>
		/// Validate if a given permission was granted to the app and if not, request it to the user.
		/// <remarks>
		/// This operation is not cancellable.
		/// This should not be invoked directly from the application code.
		/// You should use the extension methods in <see cref="PermissionsServiceExtensions"/>.
		/// </remarks>
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <param name="permissionIdentifier">A permission identifier defined in Manifest.Permission.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
		public static Task<bool> TryGetPermission(CancellationToken ct, string permissionIdentifier)
			=> _tryGetPermission(ct, permissionIdentifier);

		/// <summary>
		/// Validate if a given permission was granted to the app but not request it to the user
		/// <remarks>
		/// This should not be invoked directly from the application code.
		/// You should use the extension methods in <see cref="PermissionsServiceExtensions"/>.
		/// </remarks>
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <param name="permissionIdentifier">A permission identifier defined in Manifest.Permission.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
		public static Task<bool> CheckPermission(CancellationToken ct, string permissionIdentifier)
			=> _checkPermission(ct, permissionIdentifier);

		/// <summary>
		/// Manifest.Permission.AccessFineLocation
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetFineLocationPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.AccessFineLocation);

		/// <summary>
		/// Manifest.Permission.AccessCoarseLocation
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetCoarseLocationPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.AccessCoarseLocation);

		/// <summary>
		/// Manifest.Permission.WriteExternalStorage
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetWriteExternalStoragePermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.WriteExternalStorage);

		/// <summary>
		/// Manifest.Permission.ReadContacts
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetReadContactsPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.ReadContacts);

		/// <summary>
		/// Manifest.Permission.WriteContacts
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetWriteContactsPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.WriteContacts);

		/// <summary>
		/// Manifest.Permission.Camera
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetCameraPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.Camera);

		/// <summary>
		/// Manifest.Permission.AccessFineLocation
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckFineLocationPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.AccessFineLocation);

		/// <summary>
		/// Manifest.Permission.AccessCoarseLocation
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckCoarseLocationPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.AccessCoarseLocation);

		/// <summary>
		/// Manifest.Permission.WriteExternalStorage
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckWriteExternalStoragePermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.WriteExternalStorage);

		/// <summary>
		/// Manifest.Permission.ReadContacts
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckReadContactsPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.ReadContacts);

		/// <summary>
		/// Manifest.Permission.WriteContacts
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckWriteContactsPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.WriteContacts);

		/// <summary>
		/// Manifest.Permission.Camera
		/// </summary>
		/// <param name="service">Service</param>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckCameraPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.Camera);

	}
}
#endif
