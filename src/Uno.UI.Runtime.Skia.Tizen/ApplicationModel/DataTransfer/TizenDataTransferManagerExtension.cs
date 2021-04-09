using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tizen.Applications;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Uno.UI.Runtime.Skia.Tizen.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Skia.Tizen.ApplicationModel.DataTransfer
{
	internal class TizenDataTransferManagerExtension : IDataTransferManagerExtension
	{
		private const string LaunchAppPrivilege = "http://tizen.org/privilege/appmanager.launch";

		public TizenDataTransferManagerExtension(object owner)
		{
		}

		public bool IsSupported() => true;

		public async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			if (!PrivilegeHelper.IsDeclared(LaunchAppPrivilege))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"The Launch app privilege must be declared ({LaunchAppPrivilege})");
				}
				return false;
			}

			var appControl = new AppControl
			{
				Operation = AppControlOperations.ShareText,
			};

			var dataPackageView = dataPackage.GetView();

			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				appControl.ExtraData.Add(AppControlData.Text, text);
			}

			var uri = await DataTransferManager.GetSharedUriAsync(dataPackageView);
			if (uri != null)
			{				
				appControl.ExtraData.Add(AppControlData.Url, uri.OriginalString);
			}

			AppControl.SendLaunchRequest(appControl);

			return true;
		}
	}
}
