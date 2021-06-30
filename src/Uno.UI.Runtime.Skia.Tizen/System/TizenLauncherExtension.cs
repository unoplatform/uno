#nullable enable

using System;
using Tizen.Applications;
using System.Threading.Tasks;
using Uno.Extensions.System;
using Uno.UI.Runtime.Skia.Tizen.Helpers;
using Windows.System;

namespace Uno.UI.Runtime.Skia.Tizen.System
{
	internal class TizenLauncherExtension : ILauncherExtension
	{
		public TizenLauncherExtension(object owner)
		{
		}

		public Task<bool> LaunchUriAsync(Uri uri)
		{
			PrivilegesHelper.EnsureDeclared(Privileges.AppManagerLaunch);

			var appControl = new AppControl
			{
				Operation = AppControlOperations.View,
				Uri = uri.AbsoluteUri,
			};

			AppControl.SendLaunchRequest(appControl);

			return Task.FromResult(true);
		}

		public Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType)
		{
			throw new NotImplementedException("Windows.System.Launcher.QueryUriSupportAsync is not implemented on Tizen");
		}
	}
}
