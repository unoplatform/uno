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
	public class PermissionsHelper
	{
		private static FuncAsync<string, bool> _tryGetPermission;
		private static FuncAsync<string, bool> _checkPermission;

		internal static void Initialize(FuncAsync<string, bool> getter, FuncAsync<string, bool> checkPermission)
		{
			_tryGetPermission = getter;
			_checkPermission = checkPermission;
		}

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
		/// You should use the extension methods in PermissionsServiceExtensions.
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
		/// You should use the extension methods in PermissionsServiceExtensions.
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
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetFineLocationPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.AccessFineLocation);

		/// <summary>
		/// Manifest.Permission.AccessCoarseLocation
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetCoarseLocationPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.AccessCoarseLocation);

		/// <summary>
		/// Manifest.Permission.WriteExternalStorage
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetWriteExternalStoragePermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.WriteExternalStorage);

		/// <summary>
		/// Manifest.Permission.ReadContacts
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetReadContactsPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.ReadContacts);

		/// <summary>
		/// Manifest.Permission.WriteContacts
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetWriteContactsPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.WriteContacts);

		/// <summary>
		/// Manifest.Permission.Camera
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> TryGetCameraPermission(CancellationToken ct) => TryGetPermission(ct, Manifest.Permission.Camera);

		/// <summary>
		/// Manifest.Permission.AccessFineLocation
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckFineLocationPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.AccessFineLocation);

		/// <summary>
		/// Manifest.Permission.AccessCoarseLocation
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckCoarseLocationPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.AccessCoarseLocation);

		/// <summary>
		/// Manifest.Permission.WriteExternalStorage
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckWriteExternalStoragePermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.WriteExternalStorage);

		/// <summary>
		/// Manifest.Permission.ReadContacts
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckReadContactsPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.ReadContacts);

		/// <summary>
		/// Manifest.Permission.WriteContacts
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckWriteContactsPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.WriteContacts);

		/// <summary>
		/// Manifest.Permission.Camera
		/// </summary>
		/// <param name="ct">Cancellation Token</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task<bool> CheckCameraPermission(CancellationToken ct) => CheckPermission(ct, Manifest.Permission.Camera);

	}
}
#endif
