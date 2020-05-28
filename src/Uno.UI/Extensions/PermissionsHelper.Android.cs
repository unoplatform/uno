using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Uno.Extensions;

namespace Uno.UI.Extensions
{
	internal class PermissionsHelper
	{
		public static void Initialize()
		{
			var getPermission = Funcs
					.CreateAsync<string, bool>(TryGetPermissionCore)
					.LockInvocation(InvocationLockingMode.Share);

			Windows.Extensions.PermissionsHelper.Initialize(getPermission, CheckPermission, GetMissingPermissions, TryGetPermissionsAsync);
		}

		private static int _permissionRequest;


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
					=> Windows.Extensions.PermissionsHelper.TryGetPermission(ct, permissionIdentifier);

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
		public static async Task<bool> CheckPermission(CancellationToken ct, string permissionIdentifier)
			=> ContextCompat.CheckSelfPermission(BaseActivity.Current, permissionIdentifier) == Permission.Granted;

		private static async Task<bool> TryGetPermissionCore(CancellationToken ct, string permissionIdentifier)
		{
			if (ActivityCompat.CheckSelfPermission(ContextHelper.Current, permissionIdentifier) == Permission.Granted)
			{
				return true;
			}

			var code = Interlocked.Increment(ref _permissionRequest);
			var tcs = new TaskCompletionSource<BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == code)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = BaseActivity.Current;

			try
			{
				using (ct.Register(() => tcs.TrySetCanceled()))
				{
					current.RequestPermissionsResultWithResults += handler;

					ActivityCompat.RequestPermissions(BaseActivity.Current, new[] { permissionIdentifier }, code);

					var result = await tcs.Task;

					return result.GrantResults.Length > 0 ? result.GrantResults[0] == Permission.Granted : false;
				}
			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}
		}


		#region "submethods for MissingPermissions"
		private static IList<string> GetManifestPermissions()
		{
			// get all permission declared in Manifest (can be null)
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			return packageInfo?.RequestedPermissions;
		}

		private static bool AreAllPermissionDeclared(IList<string> manifestPermissions, string[] requiredPermissions)
		{
			// check if all requiredPermissions are declared in Manifest 
			if (requiredPermissions is null)
			{
				return true;    // no required permissions, so - everything is OK
			}

			foreach (string permission in requiredPermissions)
			{
				if (!manifestPermissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}
			}

			return true;

		}

		private static List<string> PermissionsWeWant(IList<string> manifestPermissions, string[] requiredPermissions, string[] optionalPermissions)
		{
			// prepare list of all permissions
			var allPermissions = new List<string>();
			if (requiredPermissions != null)
			{
				allPermissions.AddRange(requiredPermissions.ToList());
			}

			// add all optional permissions, found in Manifest
			if (optionalPermissions != null)
			{
				foreach (string permission in optionalPermissions)
				{
					if (manifestPermissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase)))
					{
						allPermissions.Add(permission);
					}
				}
			}

			return allPermissions;
		}

		#endregion

		/// <summary>
		/// Return null on error, or array of permission to be asked for (not granted at this time). Both parameters can be null
		/// </summary>
		/// <param name="requiredPermissions">Permissions that are required. Can be null.</param>
		/// <param name="optionalPermissions">Permissions that are required only if declared in Manifest. Can be null.</param>
		/// <returns>Array of all permission that is not granted at this moment (can be empty!), or null if some error occured</returns>
		public static string[] GetMissingPermissions(string[] requiredPermissions, string[] optionalPermissions)
		{
			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			// do we have declared permissions in Manifest?
			var manifestPermissions = GetManifestPermissions();
			if (manifestPermissions is null)
			{
				return null;
			}

			// test required permissions
			if (!AreAllPermissionDeclared(manifestPermissions, requiredPermissions))
			{
				return null;
			}

			// prepare list of all permissions (all required, and all optional defined in Manifest
			var allPermissions = PermissionsWeWant(manifestPermissions, requiredPermissions, optionalPermissions);

			Android.Content.Context context = Android.App.Application.Context;

			// filter out permissions we already have
			// using variable, not simple return, because we want simpler debugging 
			var askForPermission = allPermissions.Where(
				p => ContextCompat.CheckSelfPermission(context, p) != Android.Content.PM.Permission.Granted);

			return askForPermission.ToArray();

		}


		/// <summary>
		/// Return true if granted are: all requiredPermissions, and all optionalPermissions mentioned in Manifest. Show dialog asking for missing permissions. Both parameters can be null.
		/// </summary>
		/// <param name="requiredPermissions">Permissions that are required. Can be null.</param>
		/// <param name="optionalPermissions">Permissions that are required only if declared in Manifest. Can be null.</param>
		/// <param name="ignoreErrors">if 'true', then do not throw exception on error, and simply retur false</param>
		/// <returns>Bool if all permissions are granted, false if any of them is not granted</returns>

		private static async Task<bool> TryGetPermissionsAsync(CancellationToken ct, string[] requiredPermissions, string[] optionalPermissions, bool ignoreErrors)
		{

			var askForPermission = GetMissingPermissions(requiredPermissions, optionalPermissions);
			if (askForPermission is null)
			{
				if (ignoreErrors)
				{
					return false;
				}

				throw new InvalidOperationException("MissingPermissions returned ERROR - check Manifest file");
			}

			if (askForPermission.Count() < 1)
			{
				return true;
			}

			// system dialog asking for permission

			var code = Interlocked.Increment(ref _permissionRequest);
			var tcs = new TaskCompletionSource<BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == code)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = BaseActivity.Current;

			try
			{
				using (ct.Register(() => tcs.TrySetCanceled()))
				{
					current.RequestPermissionsResultWithResults += handler;

					ActivityCompat.RequestPermissions(BaseActivity.Current, askForPermission, code);

					var result = await tcs.Task;

					if (result.GrantResults.Length > 0)
					{
						bool allGranted = result.GrantResults.All(r => r == Android.Content.PM.Permission.Granted);
						return allGranted;
					}
					return false;   // it shouldn't happen (if it happens, then there is some error (in Android?). Maybe we even should throw exception...
				}
			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}
		}


	}
}
