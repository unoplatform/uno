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

			Windows.Extensions.PermissionsHelper.Initialize(getPermission, CheckPermission);
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

			void handler(object sender, BaseActivity.RequestPermissionsResultWithResultsEventArgs e) {

				if(e.RequestCode == code)
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
	}
}
